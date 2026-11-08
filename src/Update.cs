// Laocoön 拉奥孔 - 自动更新（独立模块）
//
// 分四层，每层都能单独替换或单独测试：
//   1. BuildInfo / SemVer / UpdateInfo   纯数据：我是谁、线上是什么版本
//   2. IUpdateChannel                    「版本清单从哪来」：HTTP、本地文件、将来的镜像
//   3. UpdateDownloader / Installer      下载、校验、替换自身；完全不碰界面
//   4. UpdateForm / UpdateAgent          界面与调度；只调用上面三层
//
// 为什么清单是纯文本而不是 GitHub API：
//   API 未认证限流 60 次/小时，且要带一个 JSON 解析器；清单是一个 asset，
//   走 releases/latest/download 的稳定重定向，key=value 七行就解析完了。
//   清单由 CI 在发版时生成（见 .github/workflows/build.yml），与 exe 同源，
//   哈希当场算、当场写，不存在"清单指向的文件被人换掉"这种缝。
//
// 清单格式（# 开头为注释）：
//   version=1.6.0
//   url=https://github.com/.../releases/download/v1.6.0/Laocoon.exe
//   sha256=<64 位小写十六进制>
//   size=354816
//   page=https://github.com/.../releases/tag/v1.6.0
//   date=2026-11-09T01:00:00Z
//   notes=一行更新说明
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
// System.Threading 也有 Timer，和 WinForms 的同名。更新界面要的是走消息泵的那个。
using WinTimer = System.Windows.Forms.Timer;

namespace Laocoon
{
    #region ---------- 更新：身份与版本 ----------

    /// <summary>
    /// 程序自身的身份。版本号只从程序集读一次，别的地方不要再抄一份 ——
    /// csproj 里的 Version 经编译进程序集，抄第二份迟早对不上。
    /// </summary>
    internal static class BuildInfo
    {
        public static Version Version
        {
            get
            {
                try
                {
                    Version v = typeof(BuildInfo).Assembly.GetName().Version;
                    return v == null ? new Version(0, 0, 0, 0) : v;
                }
                catch { return new Version(0, 0, 0, 0); }
            }
        }

        /// <summary>界面上显示的 "主.次.修订"，不带第四段，也不带 v 前缀。</summary>
        public static string Short
        {
            get
            {
                Version v = Version;
                return v.Major + "." + v.Minor + "." + Math.Max(0, v.Build);
            }
        }

        /// <summary>当前 exe 的完整路径。更新要原地替换它，所以必须是真实文件路径。</summary>
        public static string ExePath
        {
            get
            {
                try
                {
                    using (Process p = Process.GetCurrentProcess())
                        return p.MainModule.FileName;
                }
                catch { }
                try { return Application.ExecutablePath; }
                catch { return ""; }
            }
        }
    }

    /// <summary>
    /// 宽松的版本解析。清单里的版本可能是 1.5.1 / v1.5.1 / 1.5.1.0 / 1.5.1-beta，
    /// 一律取开头的数字段，够比较就行 —— 更新判断不需要知道 pre-release 的语义。
    /// </summary>
    internal static class SemVer
    {
        public static Version Parse(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            s = s.Trim();
            if (s.Length > 0 && (s[0] == 'v' || s[0] == 'V')) s = s.Substring(1);

            int[] part = new int[4];
            int n = 0, i = 0;
            while (i < s.Length && n < 4)
            {
                while (i < s.Length && !char.IsDigit(s[i])) i++;
                int start = i;
                while (i < s.Length && char.IsDigit(s[i])) i++;
                if (i == start) break;

                int v;
                if (!int.TryParse(s.Substring(start, i - start), out v)) break;
                part[n++] = v;
            }
            if (n == 0) return null;

            // 必须补齐四段：Version 用 -1 表示"这一段没给"，
            // 于是 1.5.1 会被判成小于 1.5.1.0，正好在"相同版本"上出错。
            return new Version(part[0],
                               n > 1 ? part[1] : 0,
                               n > 2 ? part[2] : 0,
                               n > 3 ? part[3] : 0);
        }

        public static Version Normalize(Version v)
        {
            if (v == null) return new Version(0, 0, 0, 0);
            return new Version(v.Major, v.Minor,
                               Math.Max(0, v.Build),
                               Math.Max(0, v.Revision));
        }

        public static bool IsNewer(string remoteText, Version local)
        {
            Version r = Parse(remoteText);
            if (r == null) return false;
            return r > Normalize(local);
        }
    }

    /// <summary>一次更新所需的全部信息。构造后不可变，可以安全地跨线程传递。</summary>
    internal sealed class UpdateInfo
    {
        public readonly string VersionText;
        public readonly Version Ver;
        public readonly string Url;
        public readonly string Sha256;
        public readonly long Size;
        public readonly string Page;
        public readonly string Notes;
        public readonly string Date;

        public UpdateInfo(string version, string url, string sha256, long size,
                          string page, string notes, string date)
        {
            VersionText = version;
            Ver = SemVer.Parse(version);
            Url = url;
            Sha256 = sha256;
            Size = size;
            Page = page;
            Notes = notes;
            Date = date;
        }

        /// <summary>
        /// 解析清单。version / url / sha256 缺一不可：
        /// 没有哈希就不肯装 —— 自动替换自身这种权限，不能给一个"大概是对的"文件。
        /// </summary>
        public static UpdateInfo FromManifest(string text)
        {
            Dictionary<string, string> kv = Manifest.Parse(text);

            string version, url, sha, page, notes, date;
            kv.TryGetValue("version", out version);
            kv.TryGetValue("url", out url);
            kv.TryGetValue("sha256", out sha);
            kv.TryGetValue("page", out page);
            kv.TryGetValue("notes", out notes);
            kv.TryGetValue("date", out date);

            if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(url)) return null;
            if (string.IsNullOrEmpty(sha) || sha.Length != 64) return null;

            long size = 0;
            string sizeText;
            if (kv.TryGetValue("size", out sizeText)) long.TryParse(sizeText, out size);

            return new UpdateInfo(version.Trim(), url.Trim(), sha.Trim().ToLowerInvariant(),
                                  size, page, notes, date);
        }

        /// <summary>诊断与截图用：一份形状正确的假清单，不含任何真实下载地址。</summary>
        public static UpdateInfo Sample()
        {
            return new UpdateInfo("1.6.1",
                "https://github.com/xatwebez/Laocoon/releases/download/v1.6.1/Laocoon.exe",
                new string('0', 64), 356352,
                "https://github.com/xatwebez/Laocoon/releases/tag/v1.6.1",
                "Keep the window handle alive while the language list is open; smooth the direction flip.",
                "2026-11-09T01:00:00Z");
        }
    }

    /// <summary>清单是 key=value 的纯文本：只有第一个 '=' 是分隔符，值里可以再出现 '='。</summary>
    internal static class Manifest
    {
        public static Dictionary<string, string> Parse(string text)
        {
            Dictionary<string, string> kv =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (text == null) return kv;

            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#' || line[0] == ';') continue;

                int eq = line.IndexOf('=');
                if (eq <= 0) continue;

                string k = line.Substring(0, eq).Trim();
                string v = line.Substring(eq + 1).Trim();
                if (k.Length > 0) kv[k] = v;
            }
            return kv;
        }
    }

    #endregion

    #region ---------- 更新：清单来源 ----------

    /// <summary>
    /// 清单从哪来。抽成接口是为了让检查逻辑不绑死在 GitHub 上：
    /// 换静态托管、加内网镜像、跑离线测试，都只是再实现一个 Read()。
    /// </summary>
    internal interface IUpdateChannel
    {
        string Name { get; }
        string Read();
    }

    internal sealed class HttpChannel : IUpdateChannel
    {
        readonly string _url;
        public HttpChannel(string url) { _url = url; }
        public string Name { get { return _url; } }
        public string Read() { return Http.GetText(_url); }
    }

    internal sealed class FileChannel : IUpdateChannel
    {
        readonly string _path;
        public FileChannel(string path) { _path = path; }
        public string Name { get { return _path; } }
        public string Read()
        {
            // 清单里有中文更新说明，按 UTF-8 读；带 BOM 也能正确识别
            return File.ReadAllText(_path, new UTF8Encoding(false));
        }
    }

    /// <summary>HTTP 的唯一出口。TLS、UA、超时都收在这里，别处不要再造一份。</summary>
    internal static class Http
    {
        static Http()
        {
            try
            {
                // 老系统（Win7 默认）可能还停在 SSL3/TLS1.0，GitHub 只认 TLS 1.2 以上
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
            catch { }
            try { ServicePointManager.DefaultConnectionLimit = 8; } catch { }
        }

        public static HttpWebRequest Request(string url)
        {
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create(url);
            r.UserAgent = "Laocoon/" + BuildInfo.Short + " (+https://github.com/xatwebez/Laocoon)";
            r.Timeout = 15000;              // 建连 / 等首字节
            r.ReadWriteTimeout = 30000;     // 单次读，卡住的连接不会把线程永久占住
            r.AllowAutoRedirect = true;
            r.MaximumAutomaticRedirections = 10;
            r.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            return r;
        }

        public static string GetText(string url)
        {
            HttpWebRequest req = Request(url);
            WebResponse resp = null;
            Stream src = null;
            try
            {
                resp = req.GetResponse();
                src = resp.GetResponseStream();
                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] buf = new byte[8192];
                    int n;
                    while ((n = src.Read(buf, 0, buf.Length)) > 0) ms.Write(buf, 0, n);
                    // 清单是自己写的，永远 UTF-8；不信响应头的 charset，也不怕 BOM
                    string s = new UTF8Encoding(false).GetString(ms.ToArray());
                    return s.TrimStart('\uFEFF');
                }
            }
            finally
            {
                if (src != null) { try { src.Dispose(); } catch { } }
                if (resp != null) { try { resp.Dispose(); } catch { } }
            }
        }
    }

    #endregion

    #region ---------- 更新：检查 ----------

    /// <summary>一次检查的结论。三种状态互斥：失败 / 已最新 / 有新版。</summary>
    internal sealed class UpdateCheckResult
    {
        public UpdateInfo Info;      // 非空表示有新版
        public string Error;         // 非空表示这次没有结论（断网、404、清单不完整）
        public string Channel;

        public bool Ok { get { return Error == null; } }
        public bool HasUpdate { get { return Info != null; } }

        public static UpdateCheckResult Fail(string error)
        {
            UpdateCheckResult r = new UpdateCheckResult();
            r.Error = error;
            return r;
        }
    }

    internal static class UpdateService
    {
        /// <summary>
        /// releases/latest/download/&lt;asset&gt; 是 GitHub 的稳定直链：
        /// 永远指向"最新那个正式版"的同名附件，不用先查 API 拿 tag。
        /// </summary>
        public const string DefaultFeed =
            "https://github.com/xatwebez/Laocoon/releases/latest/download/latest.txt";

        public static IUpdateChannel ChannelFor(string feed)
        {
            if (string.IsNullOrEmpty(feed)) return new HttpChannel(DefaultFeed);
            if (feed.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return new HttpChannel(feed);
            return new FileChannel(feed);
        }

        public static UpdateCheckResult Check(IUpdateChannel channel)
        {
            UpdateCheckResult r = new UpdateCheckResult();
            if (channel == null) { r.Error = "no channel"; return r; }
            r.Channel = channel.Name;

            try
            {
                UpdateInfo info = ReadInfo(channel);
                if (info == null || info.Ver == null)
                {
                    r.Error = "manifest incomplete";
                    return r;
                }
                if (SemVer.IsNewer(info.VersionText, BuildInfo.Version)) r.Info = info;
                return r;
            }
            catch (WebException we)
            {
                HttpWebResponse hr = we.Response as HttpWebResponse;
                // 404 是最常见的情况：最新那个 release 还没带清单（旧版本发的），不是故障
                r.Error = hr != null ? ("HTTP " + (int)hr.StatusCode) : we.Message;
                return r;
            }
            catch (Exception ex)
            {
                r.Error = ex.Message;
                return r;
            }
        }

        /// <summary>只读清单、不判版本。诊断命令要能取到"和当前相同版本"的条目来验证下载。</summary>
        public static UpdateInfo ReadInfo(IUpdateChannel channel)
        {
            if (channel == null) return null;
            return UpdateInfo.FromManifest(channel.Read());
        }
    }

    #endregion

    #region ---------- 更新：下载与安装 ----------

    /// <summary>
    /// 下载 + 校验。进度用轮询读 Done/Total，不跨线程回调 ——
    /// 回调式进度在 10MB/s 的线路上会把消息队列塞爆，界面反而更卡。
    /// </summary>
    internal sealed class UpdateDownloader
    {
        readonly UpdateInfo _info;
        readonly string _dest;
        volatile bool _cancel;
        long _done;
        long _total;

        public UpdateDownloader(UpdateInfo info, string dest)
        {
            _info = info;
            _dest = dest;
        }

        public string Destination { get { return _dest; } }
        public long Done { get { return Interlocked.Read(ref _done); } }
        public long Total { get { return Interlocked.Read(ref _total); } }
        public void Cancel() { _cancel = true; }

        public void Run()
        {
            string dir = Path.GetDirectoryName(_dest);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            string part = _dest + ".part";
            TryDelete(part);
            Interlocked.Exchange(ref _done, 0);
            Interlocked.Exchange(ref _total, 0);

            HttpWebRequest req = Http.Request(_info.Url);
            WebResponse resp = null;
            Stream src = null;
            FileStream dst = null;
            try
            {
                resp = req.GetResponse();
                Interlocked.Exchange(ref _total, resp.ContentLength);
                src = resp.GetResponseStream();
                dst = new FileStream(part, FileMode.Create, FileAccess.Write, FileShare.None, 65536);

                byte[] buf = new byte[65536];
                int n;
                while ((n = src.Read(buf, 0, buf.Length)) > 0)
                {
                    if (_cancel) throw new OperationCanceledException();
                    dst.Write(buf, 0, n);
                    Interlocked.Add(ref _done, n);
                }
            }
            finally
            {
                if (dst != null) { try { dst.Dispose(); } catch { } }
                if (src != null) { try { src.Dispose(); } catch { } }
                if (resp != null) { try { resp.Dispose(); } catch { } }
            }

            long total = Total;
            if (total > 0 && Done != total)
            {
                TryDelete(part);
                throw new IOException("size mismatch");
            }
            if (_info.Size > 0 && new FileInfo(part).Length != _info.Size)
            {
                TryDelete(part);
                throw new IOException("size mismatch");
            }

            if (!string.Equals(Sha256File(part), _info.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                TryDelete(part);
                throw new InvalidDataException("sha256 mismatch");
            }

            Unblock(part);
            TryDelete(_dest);
            File.Move(part, _dest);
        }

        /// <summary>
        /// 下载来的文件在 NTFS 上带 Zone.Identifier 备用流，双击会弹 SmartScreen。
        /// 删掉这条流等价于资源管理器的「解除锁定」；替换后就地重启，不该再问一次。
        /// </summary>
        static void Unblock(string path)
        {
            try { File.Delete(path + ":Zone.Identifier"); } catch { }
        }

        public static string Sha256File(string path)
        {
            using (SHA256 sha = new SHA256Managed())
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536))
            {
                byte[] h = sha.ComputeHash(fs);
                StringBuilder sb = new StringBuilder(h.Length * 2);
                foreach (byte b in h) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }

    /// <summary>
    /// 把新文件换到旧位置。正在运行的 exe 不能被覆盖，
    /// 所以生成一个 cmd 脚本：等本进程退出 -> move -> 重新启动 -> 删掉自己。
    /// 不用"再下载一个更新器 exe"：那会在用户机器上多留一个文件，还得自己清理。
    /// </summary>
    internal static class Installer
    {
        /// <summary>
        /// 暂存目录。不能只信 %TEMP%：组策略、迁移过的用户目录、被沙箱挂到
        /// 只读盘上的 TEMP，都会让 Directory.CreateDirectory 直接抛异常。
        /// 退到 LocalAppData 基本总有一个能写。
        /// </summary>
        public static string StagingRoot()
        {
            string[] candidates = new string[]
            {
                Path.Combine(Path.GetTempPath(), "Laocoon-update"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                             "Laocoon", "update"),
            };

            string last = null;
            foreach (string c in candidates)
            {
                try
                {
                    Directory.CreateDirectory(c);
                    return c;
                }
                catch { last = c; }
            }
            throw new IOException("no writable staging directory (" + last + ")");
        }

        public static string StagingDir(UpdateInfo info)
        {
            return Path.Combine(StagingRoot(), "v" + (info == null ? "new" : info.VersionText));
        }

        public static string StagedExe(UpdateInfo info)
        {
            return Path.Combine(StagingDir(info), "Laocoon.exe");
        }

        /// <summary>
        /// 目标位置能不能被替换。装到 Program Files 时普通用户没有写权限，
        /// 与其下载完再失败，不如一开始就告诉用户去手动下载。
        /// 返回 null 表示可以，否则是原因。
        /// </summary>
        public static string CheckWritable(string exePath)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                return "current exe not found";

            try
            {
                if ((File.GetAttributes(exePath) & FileAttributes.ReadOnly) != 0)
                    return "exe is read-only";
            }
            catch { }

            string dir = Path.GetDirectoryName(exePath);
            if (string.IsNullOrEmpty(dir)) return "no directory";

            string probe = Path.Combine(dir, ".laocoon-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                // DeleteOnClose：探针文件即使中途崩了也不会留在用户目录里
                using (FileStream fs = new FileStream(probe, FileMode.CreateNew, FileAccess.Write,
                                                      FileShare.None, 1, FileOptions.DeleteOnClose))
                { }
                return null;
            }
            catch { return "no write permission"; }
        }

        public static void ApplyAndRestart(UpdateInfo info, string newExe, string oldExe, string page)
        {
            string script = WriteScript(newExe, oldExe, page == null ? "" : page);

            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c \"\"" + script + "\"\"");
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            Process.Start(psi);
        }

        /// <summary>
        /// 只生成脚本、不启动。分出来是为了能离线核对脚本内容
        /// （--updateplan 走这条路），替换逻辑本身不该只能靠"真的跑一次"来验证。
        /// </summary>
        public static string WriteScript(string newExe, string oldExe, string page)
        {
            string dir = StagingRoot();
            string script = Path.Combine(dir, "apply.cmd");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("setlocal EnableExtensions");
            sb.AppendLine("title Laocoon updater");
            sb.AppendLine("set \"OLD=" + Cmd(oldExe) + "\"");
            sb.AppendLine("set \"NEW=" + Cmd(newExe) + "\"");
            sb.AppendLine("set \"PAGE=" + Cmd(page) + "\"");
            // 绝对路径：PATH 里若先撞上同名的 Unix 工具（Git Bash 的 find.exe 就是），
            // 按名字调用会静默用错程序，脚本从此永远失败
            sb.AppendLine("set \"PING=%SystemRoot%\\System32\\ping.exe\"");
            sb.AppendLine("set \"N=0\"");
            sb.AppendLine();
            // 进程退出后文件锁才放开。move 失败本身就说明"还锁着"，
            // 这比在外面用 tasklist 对 PID 更直接 —— 既不用解析本地化的输出，
            // 也不用猜杀毒软件什么时候松手。
            sb.AppendLine(":retry");
            sb.AppendLine("\"%PING%\" -n 2 127.0.0.1 >nul");
            sb.AppendLine("move /Y \"%NEW%\" \"%OLD%\" >nul 2>&1");
            sb.AppendLine("if not errorlevel 1 goto done");
            sb.AppendLine("set /a N=N+1");
            sb.AppendLine("if %N% GEQ 15 goto giveup");
            sb.AppendLine("goto retry");
            sb.AppendLine();
            sb.AppendLine(":done");
            sb.AppendLine("start \"\" \"%OLD%\"");
            sb.AppendLine("del \"%~f0\"");
            sb.AppendLine("exit /b 0");
            sb.AppendLine();
            // 换不动就退回旧程序，并打开下载页 —— 不能让用户对着一个消失的程序发呆
            sb.AppendLine(":giveup");
            sb.AppendLine("start \"\" \"%OLD%\"");
            sb.AppendLine("if not \"%PAGE%\"==\"\" start \"\" \"%PAGE%\"");
            sb.AppendLine("del \"%~f0\"");
            sb.AppendLine("exit /b 1");

            // cmd 只认系统 ANSI 代码页。用 UTF-8 写，中文用户名的临时目录会变成乱码，
            // 表现是脚本里路径全错、move 从头到尾失败。
            File.WriteAllText(script, sb.ToString(), Encoding.Default);
            return script;
        }

        /// <summary>路径进批处理前要转义 '%'，否则会被当成变量展开。</summary>
        static string Cmd(string s)
        {
            if (s == null) return "";
            return s.Replace("%", "%%");
        }
    }

    #endregion

    #region ---------- 更新：提示窗口 ----------

    internal sealed class UpdateForm : Form
    {
        const int PhaseIdle = 0, PhaseDownload = 1, PhaseVerify = 2, PhaseApply = 3, PhaseFail = 4;

        readonly UpdateInfo _info;
        readonly TableLayoutPanel _root = new TableLayoutPanel();
        readonly Label _state = new Label();
        readonly ProgressBar _bar = new ProgressBar();
        readonly Button _go = new Button();
        readonly Button _later = new Button();
        readonly LinkLabel _link = new LinkLabel();
        readonly WinTimer _tick = new WinTimer();

        UpdateDownloader _dl;
        volatile int _phase;
        volatile string _error;
        volatile bool _busy;
        int _seenPhase = -1;
        int _applyTick;
        bool _applied;

        public UpdateForm(UpdateInfo info)
        {
            _info = info;

            Text = L.T("upd.title");
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.White;
            Font = new Font(L.UiFont, 9F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(540, 300);
            RightToLeftLayout = L.IsRtl;
            RightToLeft = L.IsRtl ? RightToLeft.Yes : RightToLeft.No;
            ApplyIcon();

            _tick.Interval = 120;
            _tick.Tick += OnTick;

            TableLayoutPanel root = _root;
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.Padding = new Padding(20, 18, 20, 14);
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 标题
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 更新说明标题
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 更新说明正文
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 体积 · 日期
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 进度条（点了更新才出现）
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 状态 / 错误
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 底部

            root.Controls.Add(Header(), 0, 0);

            bool hasNotes = !string.IsNullOrEmpty(info.Notes);
            if (hasNotes)
            {
                Label cap = new Label();
                cap.Text = L.T("upd.notes");
                cap.AutoSize = true;
                cap.Font = new Font(L.UiFont, 8F, FontStyle.Bold);
                cap.ForeColor = Color.FromArgb(140, 148, 164);
                cap.Margin = new Padding(1, 14, 0, 4);
                root.Controls.Add(cap, 0, 1);

                Label body = new Label();
                body.Text = info.Notes;
                body.AutoSize = true;
                body.MaximumSize = new Size(482, 0);
                body.ForeColor = Color.FromArgb(55, 65, 81);
                body.Margin = new Padding(1, 0, 0, 0);
                // 说明文字多半来自提交标题。阿拉伯语界面里若整段按 RTL 排，
                // 英文句子的句末句号会被 bidi 搬到行首（".open; ..."），
                // 所以按首个强字符的方向单独决定这一段的方向。
                body.RightToLeft = StartsRtl(info.Notes) ? RightToLeft.Yes : RightToLeft.No;
                root.Controls.Add(body, 0, 2);
            }
            // 说明为空时把这两行压成 0 高，不留一块空白
            else
            {
                root.RowStyles[1] = new RowStyle(SizeType.Absolute, 0F);
                root.RowStyles[2] = new RowStyle(SizeType.Absolute, 0F);
            }

            Label meta = new Label();
            meta.Text = L.F("upd.meta", SizeText(info.Size), ShortDate(info.Date));
            meta.AutoSize = true;
            meta.Font = new Font(L.UiFont, 8F);
            meta.ForeColor = Color.FromArgb(140, 148, 164);
            meta.Margin = new Padding(1, 10, 0, 0);
            root.Controls.Add(meta, 0, 3);

            _bar.Minimum = 0;
            _bar.Maximum = 100;
            _bar.Value = 0;
            _bar.Style = ProgressBarStyle.Continuous;
            _bar.Dock = DockStyle.Top;
            _bar.Height = 9;
            _bar.Margin = new Padding(0, 8, 0, 0);
            _bar.Visible = false;       // 没开始更新时不画一条空槽，那看着像分隔线
            root.Controls.Add(_bar, 0, 4);

            _state.Text = "";
            _state.AutoSize = true;
            _state.MaximumSize = new Size(494, 0);   // 系统错误信息可能很长，得能折行
            _state.ForeColor = Color.FromArgb(107, 114, 128);
            _state.Margin = new Padding(1, 6, 0, 0);
            root.Controls.Add(_state, 0, 5);

            root.Controls.Add(Footer(), 0, 6);
            Controls.Add(root);

            // 说明文字的长短随语言变，高度按实际内容量出来，别写死
            Size pref = root.GetPreferredSize(new Size(540, 0));
            if (pref.Height > 0) ClientSize = new Size(540, pref.Height);

            AcceptButton = _go;
            CancelButton = _later;
        }

        /// <summary>已经完成替换、本进程该退出了。主窗口看到它为 true 就直接关。</summary>
        public bool Applied { get { return _applied; } }

        /// <summary>诊断用：把窗口摆到某个状态再截图，免得只能测到初始态。</summary>
        internal void ShowTestState(string state)
        {
            if (state == "dl")
            {
                _bar.Visible = true;
                _bar.Value = 42;
                _state.ForeColor = Color.FromArgb(107, 114, 128);
                _state.Text = L.F("upd.dl", 42) + "   147 KB / 348 KB";
            }
            else if (state == "fail")
            {
                _bar.Visible = true;
                _bar.Value = 0;
                _state.ForeColor = Color.FromArgb(190, 44, 44);
                _state.Text = L.F("upd.err", "sha256 mismatch");
            }
            if (state == "dl" || state == "fail") Relayout();
        }

        Control Header()
        {
            TableLayoutPanel head = new TableLayoutPanel();
            head.ColumnCount = 2;
            head.AutoSize = true;
            head.Dock = DockStyle.Fill;
            head.Margin = new Padding(0, 0, 0, 0);
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54F));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            PictureBox ico = new PictureBox();
            ico.SizeMode = PictureBoxSizeMode.Zoom;
            ico.Size = new Size(46, 46);
            ico.Margin = new Padding(0, 0, 8, 0);
            try { if (Icon != null) ico.Image = new System.Drawing.Icon(Icon, 48, 48).ToBitmap(); }
            catch { }
            head.Controls.Add(ico, 0, 0);

            TableLayoutPanel txt = new TableLayoutPanel();
            txt.ColumnCount = 1;
            txt.AutoSize = true;
            txt.Dock = DockStyle.Fill;
            txt.Margin = new Padding(0);
            txt.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            txt.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label t = new Label();
            t.Text = L.T("upd.head");
            t.AutoSize = true;
            // 德语「Eine neue Version ist verfügbar」比窗口还宽，不给上限就直接被右边切掉
            t.MaximumSize = new Size(436, 0);
            t.Font = new Font(L.UiFont, 14F, FontStyle.Bold);
            t.ForeColor = Color.FromArgb(23, 35, 59);
            t.Margin = new Padding(0);
            txt.Controls.Add(t, 0, 0);

            Label v = new Label();
            v.Text = L.F("upd.ver", BuildInfo.Short, _info.VersionText);
            v.AutoSize = true;
            v.ForeColor = Color.FromArgb(120, 128, 142);
            v.Margin = new Padding(1, 4, 0, 0);
            txt.Controls.Add(v, 0, 1);

            head.Controls.Add(txt, 1, 0);
            return head;
        }

        Control Footer()
        {
            TableLayoutPanel foot = new TableLayoutPanel();
            foot.ColumnCount = 2;
            foot.AutoSize = true;
            foot.Dock = DockStyle.Fill;
            foot.Margin = new Padding(0, 10, 0, 0);
            foot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            foot.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _link.Text = L.T("upd.page");
            _link.AutoSize = true;
            _link.Anchor = AnchorStyles.Left;
            _link.Margin = new Padding(1, 8, 0, 0);
            _link.LinkColor = Color.FromArgb(37, 99, 235);
            _link.LinkClicked += delegate
            {
                try { Process.Start(string.IsNullOrEmpty(_info.Page) ? "https://github.com/xatwebez/Laocoon/releases" : _info.Page); }
                catch { }
            };
            foot.Controls.Add(_link, 0, 0);

            FlowLayoutPanel btns = new FlowLayoutPanel();
            btns.AutoSize = true;
            btns.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btns.Dock = DockStyle.Fill;
            btns.FlowDirection = FlowDirection.RightToLeft;
            btns.WrapContents = false;
            btns.Margin = new Padding(0);

            _go.Text = L.T("upd.now");
            _go.AutoSize = true;
            _go.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _go.MinimumSize = new Size(96, 30);
            _go.Padding = new Padding(14, 0, 14, 0);
            _go.FlatStyle = FlatStyle.Flat;
            _go.FlatAppearance.BorderSize = 0;
            _go.BackColor = Color.FromArgb(37, 99, 235);
            _go.ForeColor = Color.White;
            _go.Cursor = Cursors.Hand;
            _go.Margin = new Padding(6, 0, 0, 0);
            _go.Click += delegate { StartUpdate(); };
            btns.Controls.Add(_go);

            _later.Text = L.T("upd.later");
            _later.AutoSize = true;
            _later.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _later.MinimumSize = new Size(80, 30);
            _later.Padding = new Padding(12, 0, 12, 0);
            _later.FlatStyle = FlatStyle.Flat;
            _later.FlatAppearance.BorderColor = Color.FromArgb(214, 219, 227);
            _later.BackColor = Color.White;
            _later.ForeColor = Color.FromArgb(31, 41, 55);
            _later.Cursor = Cursors.Hand;
            _later.Margin = new Padding(6, 0, 0, 0);
            _later.Click += delegate
            {
                if (_busy && _dl != null) { _dl.Cancel(); return; }
                DialogResult = DialogResult.Cancel;
                Close();
            };
            btns.Controls.Add(_later);

            foot.Controls.Add(btns, 1, 0);
            return foot;
        }

        void StartUpdate()
        {
            if (_busy) return;

            // 先确认能替换再下载：装到 Program Files 里的用户不该先等 300KB
            string why = Installer.CheckWritable(BuildInfo.ExePath);
            if (why != null)
            {
                _link.Visible = true;
                Fail(L.T("upd.manual") + "  (" + why + ")");
                return;
            }

            _busy = true;
            _go.Enabled = false;
            _later.Text = L.T("upd.cancel");
            _error = null;
            _bar.Style = ProgressBarStyle.Continuous;
            _bar.Value = 0;
            _bar.Visible = true;
            _state.ForeColor = Color.FromArgb(107, 114, 128);
            _phase = PhaseDownload;
            _seenPhase = -1;
            Relayout();
            _tick.Start();

            try
            {
                _dl = new UpdateDownloader(_info, Installer.StagedExe(_info));
            }
            catch (Exception ex)
            {
                // 一个可写的暂存目录都找不到，只能原地停住，别让线程带着 null 跑
                _tick.Stop();
                _phase = PhaseIdle;
                _seenPhase = -1;
                Fail(L.F("upd.err", ex.Message));
                return;
            }
            Thread t = new Thread(Work);
            t.IsBackground = true;          // 用户直接关窗口也不要吊着一个线程
            t.Name = "laocoon-update";
            t.Start();
        }

        void Work()
        {
            try
            {
                _dl.Run();
                _phase = PhaseVerify;
                Thread.Sleep(220);          // 让「校验中」有机会被画出来，否则一闪而过
                Installer.ApplyAndRestart(_info, _dl.Destination, BuildInfo.ExePath, _info.Page);
                _applyTick = Environment.TickCount;
                _phase = PhaseApply;
            }
            catch (OperationCanceledException)
            {
                _phase = PhaseIdle;
            }
            catch (Exception ex)
            {
                _error = ex.Message;
                _phase = PhaseFail;
            }
        }

        void OnTick(object sender, EventArgs e)
        {
            int ph = _phase;

            if (ph != _seenPhase)
            {
                _seenPhase = ph;
                ApplyPhase(ph);
            }

            if (ph == PhaseDownload) UpdateProgress();
            else if (ph == PhaseApply && Environment.TickCount - _applyTick > 420)
            {
                // 脚本已经在等本进程退出了；再留半秒给用户看清那句话
                _tick.Stop();
                _applied = true;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        void ApplyPhase(int ph)
        {
            if (ph == PhaseIdle)
            {
                _busy = false;
                _bar.Style = ProgressBarStyle.Continuous;
                _bar.Value = 0;
                _state.Text = "";
                _go.Enabled = true;
                _later.Text = L.T("upd.later");
            }
            else if (ph == PhaseVerify)
            {
                _bar.Style = ProgressBarStyle.Marquee;
                _state.Text = L.T("upd.verify");
            }
            else if (ph == PhaseApply)
            {
                _bar.Style = ProgressBarStyle.Marquee;
                _state.Text = L.T("upd.apply");
                _go.Enabled = false;
                _later.Enabled = false;
            }
            else if (ph == PhaseFail)
            {
                Fail(L.F("upd.err", _error == null ? "" : _error));
            }
        }

        void UpdateProgress()
        {
            if (_dl == null) return;
            long total = _dl.Total;
            long done = _dl.Done;
            if (total <= 0) { _state.Text = L.T("upd.dlunknown"); return; }

            int pct = (int)(done * 100 / total);
            if (pct < 0) pct = 0;
            if (pct > 100) pct = 100;
            _bar.Value = pct;
            _state.Text = L.F("upd.dl", pct) + "   " + SizeText(done) + " / " + SizeText(total);
        }

        void Fail(string msg)
        {
            _busy = false;
            _bar.Style = ProgressBarStyle.Continuous;
            _bar.Value = 0;
            _state.ForeColor = Color.FromArgb(190, 44, 44);
            _state.Text = msg;
            _go.Enabled = true;
            _later.Text = L.T("upd.later");
            Relayout();
        }

        /// <summary>这段文字是以从右往左的字母开头的吗。只用来决定段落方向，不做别的判断。</summary>
        static bool StartsRtl(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s)
            {
                if (c >= '\u0590' && c <= '\u08FF') return true;    // 希伯来 / 阿拉伯 / 叙利亚…
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')) return false;
            }
            return false;
        }

        // 进度条和错误行都在 AutoSize 行里，出现或变长会改变内容高度，得重新量一次；
        // 不重量的结果是底部按钮被挤出窗口。
        void Relayout()
        {
            Size pref = _root.GetPreferredSize(new Size(ClientSize.Width, 0));
            if (pref.Height > 0) ClientSize = new Size(ClientSize.Width, pref.Height);
        }

        // 体积与日期用固定格式：日期一律 yyyy-MM-dd，别让不同语言的区域设置改写它
        internal static string SizeText(long bytes)
        {
            if (bytes <= 0) return "—";
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024) + " KB";
            return (bytes / (1024.0 * 1024.0)).ToString("0.0", CultureInfo.InvariantCulture) + " MB";
        }

        internal static string ShortDate(string iso)
        {
            if (string.IsNullOrEmpty(iso)) return "";
            DateTime t;
            if (DateTime.TryParse(iso, CultureInfo.InvariantCulture,
                                  DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out t))
                return t.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return iso;
        }

        void ApplyIcon()
        {
            try
            {
                using (Stream s = typeof(UpdateForm).Assembly.GetManifestResourceStream("Laocoon.ico"))
                {
                    if (s != null) Icon = new System.Drawing.Icon(s);
                }
            }
            catch { }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _tick.Stop();
            _tick.Dispose();
            base.OnFormClosed(e);
        }
    }

    #endregion

    #region ---------- 更新：后台调度 ----------

    /// <summary>
    /// 检查更新的调度器。主窗口只订阅 Finished，不自己管线程和句柄。
    /// 事件一定在 UI 线程上到达，回调里可以直接碰控件。
    /// </summary>
    internal sealed class UpdateAgent
    {
        readonly Control _owner;
        readonly string _feed;
        readonly WinTimer _delay = new WinTimer();
        volatile bool _busy;

        public event Action<UpdateCheckResult> Finished;

        public UpdateAgent(Control owner, string feed)
        {
            _owner = owner;
            _feed = feed;
            _delay.Tick += delegate { _delay.Stop(); Run(); };
        }

        /// <summary>启动后延迟查一次：别和窗口首次绘制、语言加载抢那几百毫秒。</summary>
        public void Start(int delayMs)
        {
            _delay.Interval = delayMs < 0 ? 0 : delayMs;
            _delay.Start();
        }

        public void CheckNow()
        {
            _delay.Stop();
            Run();
        }

        public void Dispose()
        {
            _delay.Stop();
            _delay.Dispose();
        }

        void Run()
        {
            if (_busy) return;
            _busy = true;

            Thread t = new Thread(delegate ()
            {
                UpdateCheckResult r;
                try { r = UpdateService.Check(UpdateService.ChannelFor(_feed)); }
                catch (Exception ex) { r = UpdateCheckResult.Fail(ex.Message); }
                _busy = false;
                Post(r);
            });
            t.IsBackground = true;
            t.Name = "laocoon-updatecheck";
            t.Start();
        }

        void Post(UpdateCheckResult r)
        {
            try
            {
                if (_owner == null || _owner.IsDisposed || !_owner.IsHandleCreated) return;
                _owner.BeginInvoke(new Action<UpdateCheckResult>(delegate (UpdateCheckResult rr)
                {
                    Action<UpdateCheckResult> h = Finished;
                    if (h != null) h(rr);
                }), r);
            }
            catch { }
        }
    }

    #endregion
}

// Laocoön 拉奥孔 - 窗口探针 (Window Probe)
// 名字来自特洛伊木马：所有人都在往城里拖那匹木马，只有拉奥孔一矛捅进马腹，听里面的金属声。
// 这里做的是同一件事：用准星戳一下窗口，把它藏着的一切都听出来。
// 用法：按 F2 -> 鼠标变十字靶 -> 悬停目标窗口 -> 左键锁定
// 单文件、零第三方依赖，仅需 Windows 自带 .NET Framework 4.x
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;

namespace Laocoon
{
    #region ---------- 原生互操作 ----------

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT { public int X, Y; public POINT(int x, int y) { X = x; Y = y; } }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SYSTEM_INFO
    {
        public ushort wProcessorArchitecture;
        public ushort wReserved;
        public uint dwPageSize;
        public IntPtr lpMinimumApplicationAddress;
        public IntPtr lpMaximumApplicationAddress;
        public IntPtr dwActiveProcessorMask;
        public IntPtr dwNumberOfProcessors;
        public IntPtr dwProcessorType;
        public IntPtr dwAllocationGranularity;
        public ushort wProcessorLevel;
        public ushort wProcessorRevision;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct PROCESSENTRY32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public IntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }

    internal delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);

    internal static class Nat
    {
        public const uint GA_ROOT = 2;
        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;
        public const uint GW_OWNER = 4;
        public const uint CWP_SKIPINVISIBLE = 0x0001;
        public const uint SMTO_ABORTIFHUNG = 0x0002;
        public const uint WM_GETTEXT = 0x000D;
        public const int DWMWA_CLOAKED = 14;
        public const int PROCESS_QUERY_INFORMATION = 0x0400;
        public const int PROCESS_VM_READ = 0x0010;
        public const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        public const int TOKEN_QUERY = 0x0008;
        public const uint TH32CS_SNAPPROCESS = 0x00000002;
        public const int WM_HOTKEY = 0x0312;
        public const int LIST_MODULES_ALL = 3;

        // ---- user32 ----
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool EnumWindows(EnumProc cb, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool EnumChildWindows(IntPtr parent, EnumProc cb, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder sb, int nMax);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder sb, int nMax);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT r);
        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern bool IsWindowEnabled(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetParent(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr ChildWindowFromPointEx(IntPtr hParent, POINT pt, uint flags);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ScreenToClient(IntPtr hWnd, ref POINT pt);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetCapture(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern int GetDlgCtrlID(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
        static extern int GetWindowLong32(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        public static long GetStyle(IntPtr hWnd, int index)
        {
            if (IntPtr.Size == 8) return GetWindowLongPtr64(hWnd, index).ToInt64();
            return GetWindowLong32(hWnd, index);
        }

        // ---- dwmapi ----
        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hWnd, int attr, out int value, int size);

        // ---- kernel32 ----
        // 0 = GDI 对象数，1 = USER 对象数。两者都有进程级上限（默认各 10000），
        // 高频切语言时如果只增不减，就是资源泄漏。
        [DllImport("user32.dll")]
        public static extern uint GetGuiResources(IntPtr hProcess, uint flags);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(int access, bool inherit, int pid);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool QueryFullProcessImageName(IntPtr hProcess, int flags, StringBuilder sb, ref int size);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr h);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool IsWow64Process(IntPtr hProcess, out bool wow64);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint pid);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool Process32First(IntPtr snap, ref PROCESSENTRY32 pe);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool Process32Next(IntPtr snap, ref PROCESSENTRY32 pe);
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();
        [DllImport("kernel32.dll")]
        public static extern void GetNativeSystemInfo(out SYSTEM_INFO si);
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        // ---- psapi ----
        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool EnumProcessModulesEx(IntPtr hProcess, IntPtr[] lphModule, int cb, out int lpcbNeeded, int filter);
        [DllImport("psapi.dll", CharSet = CharSet.Unicode)]
        public static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, StringBuilder lpFilename, int nSize);

        // ---- advapi32 ----
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr hProcess, int access, out IntPtr token);
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool GetTokenInformation(IntPtr token, int cls, out int info, int len, out int retLen);
    }

    #endregion

    #region ---------- 样式位名称表 ----------

    internal static class Styles
    {
        static readonly Dictionary<long, string> Ws = new Dictionary<long, string>
        {
            { 0x80000000L, "WS_POPUP" },       { 0x40000000L, "WS_CHILD" },
            { 0x20000000L, "WS_MINIMIZE" },    { 0x10000000L, "WS_VISIBLE" },
            { 0x08000000L, "WS_DISABLED" },    { 0x04000000L, "WS_CLIPSIBLINGS" },
            { 0x02000000L, "WS_CLIPCHILDREN" },{ 0x01000000L, "WS_MAXIMIZE" },
            { 0x00C00000L, "WS_CAPTION" },     { 0x00800000L, "WS_BORDER" },
            { 0x00400000L, "WS_DLGFRAME" },    { 0x00200000L, "WS_VSCROLL" },
            { 0x00100000L, "WS_HSCROLL" },     { 0x00080000L, "WS_SYSMENU" },
            { 0x00040000L, "WS_THICKFRAME" },  { 0x00020000L, "WS_MINIMIZEBOX/WS_GROUP" },
            { 0x00010000L, "WS_MAXIMIZEBOX/WS_TABSTOP" }
        };

        static readonly Dictionary<long, string> WsEx = new Dictionary<long, string>
        {
            { 0x00000001L, "WS_EX_DLGMODALFRAME" },  { 0x00000004L, "WS_EX_NOPARENTNOTIFY" },
            { 0x00000008L, "WS_EX_TOPMOST" },        { 0x00000010L, "WS_EX_ACCEPTFILES" },
            { 0x00000020L, "WS_EX_TRANSPARENT" },    { 0x00000040L, "WS_EX_MDICHILD" },
            { 0x00000080L, "WS_EX_TOOLWINDOW" },     { 0x00000100L, "WS_EX_WINDOWEDGE" },
            { 0x00000200L, "WS_EX_CLIENTEDGE" },     { 0x00000400L, "WS_EX_CONTEXTHELP" },
            { 0x00010000L, "WS_EX_CONTROLPARENT" },  { 0x00020000L, "WS_EX_STATICEDGE" },
            { 0x00040000L, "WS_EX_APPWINDOW" },      { 0x00080000L, "WS_EX_LAYERED" },
            { 0x00100000L, "WS_EX_NOINHERITLAYOUT" },{ 0x00400000L, "WS_EX_LAYOUTRTL" },
            { 0x02000000L, "WS_EX_COMPOSITED" },     { 0x08000000L, "WS_EX_NOACTIVATE" }
        };

        public static string Decode(long v, bool ex)
        {
            Dictionary<long, string> t = ex ? WsEx : Ws;
            List<string> parts = new List<string>();
            foreach (KeyValuePair<long, string> kv in t)
                if ((v & kv.Key) == kv.Key) parts.Add(kv.Value);
            if (parts.Count == 0) return L.T("rep.none").Trim();
            return string.Join(" | ", parts.ToArray());
        }
    }

    #endregion

    #region ---------- 数据结构 ----------

    internal class WinDesc
    {
        public IntPtr Hwnd;
        public IntPtr Root;
        public string ClassName = "";
        public string Title = "";
        public string Text = "";
        public int Left, Top, Right, Bottom;
        public int ClientW, ClientH;
        public long Style, ExStyle;
        public bool Visible, Enabled, Iconic, Cloaked;
        public int CtrlId;
        public int Depth;
        public uint Pid, Tid;

        public int Width { get { return Right - Left; } }
        public int Height { get { return Bottom - Top; } }
        public string HexHandle { get { return "0x" + Hwnd.ToInt64().ToString("X8"); } }
        public string SizeText { get { return Width + " x " + Height; } }
        public string PosText { get { return "(" + Left + ", " + Top + ")"; } }
        public string Display { get { return Title.Length > 0 ? Title : Text; } }
    }

    internal class Report
    {
        public DateTime CapturedAt;
        public Point HitPoint;
        public WinDesc Hit;
        public WinDesc Root;
        public string OwnerInfo = "";
        public string HitPath = "";
        public string MonitorInfo = "";
        public List<WinDesc> Children = new List<WinDesc>();
        public List<WinDesc> Siblings = new List<WinDesc>();
        public bool ChildrenTruncated;

        public uint Pid, Tid, ParentPid;
        public string ProcName = "";
        public string ProcPath = "";
        public string CmdLine = "";
        public string ParentName = "";
        public string Arch = "";
        public bool Elevated;
        public int SessionId = -1;
        public string StartTime = "";
        public string Company = "";
        public string Product = "";
        public string Description = "";
        public string FileVersion = "";
        public string Copyright = "";

        public long FileSize;
        public string FileCreated = "";
        public string FileModified = "";
        public string Md5 = "";
        public string Sha1 = "";
        public string Sha256 = "";
        public string HashNote = "";

        public bool SigPresent;
        public bool SigValid;
        public string SigSubject = "";
        public string SigIssuer = "";
        public string SigNotAfter = "";
        public string SigStatus = "";

        public List<string> Modules = new List<string>();
        public string ModulesNote = "";
        public List<string> Flags = new List<string>();

        // ---------- 文本报告 ----------
        public string ToText()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("================================================================\r\n");
            sb.Append(L.F("rep.header", CapturedAt.ToString("yyyy-MM-dd HH:mm:ss"))).Append("\r\n");
            sb.Append("================================================================\r\n\r\n");

            sb.Append(L.T("rep.sec1")).Append("\r\n");
            Line(sb, L.T("lbl.handle"), Root.HexHandle);
            Line(sb, L.T("lbl.class"), Root.ClassName);
            Line(sb, L.T("lbl.title"), Root.Title);
            if (Hit != null && Hit.Hwnd != Root.Hwnd)
            {
                Line(sb, L.T("lbl.hitctl"), Hit.ClassName + "   " + Hit.HexHandle + "   id=" + Hit.CtrlId);
                if (Hit.Text.Length > 0) Line(sb, L.T("lbl.ctltext"), Hit.Text);
                if (HitPath.Length > 0) Line(sb, L.T("lbl.hitpath"), HitPath);
            }
            Line(sb, L.T("lbl.pos"), Root.PosText + "   " + Root.SizeText + "   " + L.F("rep.client", Root.ClientW, Root.ClientH));
            Line(sb, L.T("lbl.cursor"), "(" + HitPoint.X + ", " + HitPoint.Y + ")");
            Line(sb, L.T("lbl.thread"), "PID " + Root.Pid + "   TID " + Root.Tid);
            Line(sb, L.T("lbl.monitor"), MonitorInfo);
            Line(sb, L.T("lbl.state"), L.F("fmt.state", B(Root.Visible), B(Root.Enabled), B(Root.Iconic), B(Root.Cloaked)));
            Line(sb, L.T("lbl.style"), "0x" + Root.Style.ToString("X8") + "  " + Styles.Decode(Root.Style, false));
            Line(sb, L.T("lbl.exstyle"), "0x" + Root.ExStyle.ToString("X8") + "  " + Styles.Decode(Root.ExStyle, true));
            Line(sb, L.T("lbl.owner"), OwnerInfo);
            sb.Append("\r\n");

            sb.Append(L.T("rep.sec2")).Append("\r\n");
            Line(sb, L.T("lbl.pid"), Pid.ToString());
            Line(sb, L.T("lbl.pname"), ProcName);
            Line(sb, L.T("lbl.ppath"), ProcPath);
            Line(sb, L.T("lbl.cmd"), CmdLine);
            Line(sb, L.T("lbl.parent"), ParentPid + (ParentName.Length > 0 ? "   (" + ParentName + ")" : ""));
            Line(sb, L.T("lbl.arch"), Arch);
            Line(sb, L.T("lbl.elev"), Elevated ? L.T("common.elevated") : L.T("common.normal"));
            Line(sb, L.T("lbl.session"),
                 L.F("common.session", SessionId) + (StartTime.Length > 0 ? "   " + StartTime : ""));
            Line(sb, L.T("lbl.company"), Company);
            Line(sb, L.T("lbl.product"), Product);
            Line(sb, L.T("lbl.desc"), Description);
            Line(sb, L.T("lbl.fver"), FileVersion);
            Line(sb, L.T("lbl.copyright"), Copyright);
            sb.Append("\r\n");

            sb.Append(L.T("rep.sec3")).Append("\r\n");
            Line(sb, L.T("lbl.fsize"), HumanSize(FileSize));
            Line(sb, L.T("lbl.fcreated"), FileCreated);
            Line(sb, L.T("lbl.fmodified"), FileModified);
            Line(sb, L.T("lbl.md5"), Md5);
            Line(sb, L.T("lbl.sha1"), Sha1);
            Line(sb, L.T("lbl.sha256"), Sha256);
            Line(sb, L.T("lbl.note"), HashNote);
            sb.Append("\r\n");

            sb.Append(L.T("rep.sec4")).Append("\r\n");
            if (!SigPresent)
            {
                sb.Append(L.T("rep.nosig")).Append("\r\n");
            }
            else
            {
                Line(sb, L.T("lbl.sigstatus"), SigValid ? L.T("rep.sigvalid") : L.T("rep.siginvalid"));
                Line(sb, L.T("lbl.signer"), SigSubject);
                Line(sb, L.T("lbl.issuer"), SigIssuer);
                Line(sb, L.T("lbl.expires"), SigNotAfter);
                if (SigStatus.Length > 0) Line(sb, L.T("lbl.chain"), SigStatus);
            }
            sb.Append("\r\n");

            sb.Append(L.F("rep.sec5", Siblings.Count)).Append("\r\n");
            if (Siblings.Count == 0) sb.Append(L.T("rep.none")).Append("\r\n");
            foreach (WinDesc w in Siblings)
            {
                sb.Append("  ").Append(w.HexHandle)
                  .Append(w.Visible ? L.T("rep.visible") : L.T("rep.hidden"))
                  .Append(Pad(w.SizeText, 12))
                  .Append(w.ClassName)
                  .Append("   \"").Append(Trim(w.Title, 60)).Append("\"")
                  .Append(w.Hwnd == Root.Hwnd ? L.T("rep.istarget") : "")
                  .Append("\r\n");
            }
            sb.Append("\r\n");

            sb.Append(L.F("rep.sec6", Children.Count) + (ChildrenTruncated ? L.T("rep.trunc") : "")).Append("\r\n");
            if (Children.Count == 0) sb.Append(L.T("rep.nochild")).Append("\r\n");
            foreach (WinDesc w in Children)
            {
                sb.Append("  ").Append(new string(' ', w.Depth * 2));
                sb.Append(w.Visible ? "|- " : "x- ");
                sb.Append(w.ClassName).Append("  ");
                sb.Append(w.SizeText).Append("  ");
                sb.Append(w.PosText).Append("  ");
                sb.Append("id=").Append(w.CtrlId).Append("  ");
                string t = w.Text.Length > 0 ? w.Text : w.Title;
                if (t.Length > 0) sb.Append("\"").Append(Trim(t, 70)).Append("\"");
                sb.Append("\r\n");
            }
            sb.Append("\r\n");

            sb.Append(L.F("rep.sec7", Modules.Count)).Append("\r\n");
            if (ModulesNote.Length > 0) sb.Append("  ").Append(ModulesNote).Append("\r\n");
            foreach (string m in Modules) sb.Append("  ").Append(m).Append("\r\n");
            sb.Append("\r\n");

            if (Flags.Count > 0)
            {
                sb.Append(L.T("rep.secflags")).Append("\r\n");
                foreach (string f in Flags) sb.Append("  * ").Append(f).Append("\r\n");
                sb.Append("\r\n");
            }

            return sb.ToString();
        }

        static string B(bool v) { return L.T(v ? "common.yes" : "common.no"); }

        static string HumanSize(long b)
        {
            if (b <= 0) return "";
            string u = L.T("common.bytes");
            if (b >= 1024L * 1024 * 1024) return b.ToString("N0") + " " + u + "  (" + (b / 1073741824.0).ToString("N2") + " GB)";
            if (b >= 1024L * 1024) return b.ToString("N0") + " " + u + "  (" + (b / 1048576.0).ToString("N2") + " MB)";
            return b.ToString("N0") + " " + u + "  (" + (b / 1024.0).ToString("N1") + " KB)";
        }

        static string Trim(string s, int n)
        {
            if (s == null) return "";
            s = s.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
            return s.Length <= n ? s : s.Substring(0, n) + "…";
        }

        static string Pad(string s, int n)
        {
            if (s.Length >= n) return s + " ";
            return s + new string(' ', n - s.Length);
        }

        static void Line(StringBuilder sb, string label, string value)
        {
            if (value == null || value.Length == 0) return;
            sb.Append(label);
            // 拉丁/中日韩文字宽度稳定，按当前语言最长标签算出的列宽补空格对齐；
            // 阿拉伯文和天城文没有固定字宽，补空格只会越补越歪，改用冒号分隔。
            if (!L.PadLabels) { sb.Append(L.LabelSep).Append(value).Append("\r\n"); return; }
            int pad = L.LabelCol - L.DispWidth(label);
            for (int i = 0; i < pad; i++) sb.Append(' ');
            sb.Append(value).Append("\r\n");
        }

    }

    #endregion

    #region ---------- 采集核心 ----------

    internal static class Probe
    {
        static readonly uint MyPid = (uint)Process.GetCurrentProcess().Id;

        public static IntPtr HitTest(Point p)
        {
            IntPtr result = IntPtr.Zero;
            EnumProc cb = delegate(IntPtr h, IntPtr l)
            {
                if (!Nat.IsWindowVisible(h)) return true;
                if (Nat.IsIconic(h)) return true;
                if (Nat.GetAncestor(h, Nat.GA_ROOT) != h) return true;

                uint pid;
                Nat.GetWindowThreadProcessId(h, out pid);
                if (pid == MyPid) return true;

                long ex = Nat.GetStyle(h, Nat.GWL_EXSTYLE);
                if ((ex & 0x20L) != 0) return true;   // WS_EX_TRANSPARENT 穿透窗口忽略

                RECT r;
                if (!Nat.GetWindowRect(h, out r)) return true;
                if (p.X < r.Left || p.X >= r.Right || p.Y < r.Top || p.Y >= r.Bottom) return true;

                result = h;
                return false;
            };
            Nat.EnumWindows(cb, IntPtr.Zero);
            return result;
        }

        public static IntPtr DeepestAt(IntPtr top, Point p)
        {
            IntPtr cur = top;
            for (int i = 0; i < 32; i++)
            {
                POINT pt = new POINT(p.X, p.Y);
                if (!Nat.ScreenToClient(cur, ref pt)) break;
                IntPtr child = Nat.ChildWindowFromPointEx(cur, pt, Nat.CWP_SKIPINVISIBLE);
                if (child == IntPtr.Zero || child == cur) break;
                cur = child;
            }
            return cur;
        }

        public static string GetControlText(IntPtr h, int max)
        {
            IntPtr buf = Marshal.AllocHGlobal((max + 1) * 2);
            try
            {
                byte[] zeros = new byte[(max + 1) * 2];
                Marshal.Copy(zeros, 0, buf, zeros.Length);
                IntPtr res;
                IntPtr ok = Nat.SendMessageTimeout(h, Nat.WM_GETTEXT, (IntPtr)max, buf,
                                                   Nat.SMTO_ABORTIFHUNG, 60, out res);
                if (ok == IntPtr.Zero) return "";
                string s = Marshal.PtrToStringUni(buf);
                return s == null ? "" : s.Trim();
            }
            catch { return ""; }
            finally { Marshal.FreeHGlobal(buf); }
        }

        public static WinDesc Describe(IntPtr h, bool withText)
        {
            WinDesc d = new WinDesc();
            d.Hwnd = h;
            if (h == IntPtr.Zero) return d;

            StringBuilder sb = new StringBuilder(512);
            if (Nat.GetClassName(h, sb, sb.Capacity) > 0) d.ClassName = sb.ToString();

            int tl = Nat.GetWindowTextLength(h);
            int cap = tl + 2 > 512 ? tl + 2 : 512;
            StringBuilder tb = new StringBuilder(cap);
            if (Nat.GetWindowText(h, tb, cap) > 0) d.Title = tb.ToString();

            if (withText) d.Text = GetControlText(h, 1024);

            RECT r;
            if (Nat.GetWindowRect(h, out r))
            {
                d.Left = r.Left; d.Top = r.Top; d.Right = r.Right; d.Bottom = r.Bottom;
            }
            RECT cr;
            if (Nat.GetClientRect(h, out cr)) { d.ClientW = cr.Right; d.ClientH = cr.Bottom; }

            d.Style = Nat.GetStyle(h, Nat.GWL_STYLE);
            d.ExStyle = Nat.GetStyle(h, Nat.GWL_EXSTYLE);
            d.Visible = Nat.IsWindowVisible(h);
            d.Enabled = Nat.IsWindowEnabled(h);
            d.Iconic = Nat.IsIconic(h);
            d.CtrlId = Nat.GetDlgCtrlID(h);
            d.Root = Nat.GetAncestor(h, Nat.GA_ROOT);

            uint pid;
            d.Tid = Nat.GetWindowThreadProcessId(h, out pid);
            d.Pid = pid;

            int cloaked;
            try
            {
                if (Nat.DwmGetWindowAttribute(h, Nat.DWMWA_CLOAKED, out cloaked, 4) == 0)
                    d.Cloaked = cloaked != 0;
            }
            catch { }

            int depth = 0;
            IntPtr p = Nat.GetParent(h);
            while (p != IntPtr.Zero && depth < 32) { depth++; p = Nat.GetParent(p); }
            d.Depth = depth;

            return d;
        }

        public static Report Collect(IntPtr hit, Point pt)
        {
            Report rep = new Report();
            rep.CapturedAt = DateTime.Now;
            rep.HitPoint = pt;
            rep.Hit = Describe(hit, true);

            IntPtr rootH = rep.Hit.Root != IntPtr.Zero ? rep.Hit.Root : hit;
            rep.Root = Describe(rootH, true);
            if (rep.Root.Title.Length == 0 && rep.Hit.Title.Length > 0) rep.Root.Title = rep.Hit.Title;

            // 属主窗口
            IntPtr owner = Nat.GetWindow(rootH, Nat.GW_OWNER);
            if (owner != IntPtr.Zero)
            {
                WinDesc o = Describe(owner, false);
                rep.OwnerInfo = o.HexHandle + "  " + o.ClassName + "  \"" + o.Title + "\"";
            }

            // 命中路径 root > ... > hit
            if (rep.Hit.Hwnd != rep.Root.Hwnd)
            {
                List<string> chain = new List<string>();
                IntPtr cur = rep.Hit.Hwnd;
                int guard = 0;
                while (cur != IntPtr.Zero && cur != rep.Root.Hwnd && guard++ < 32)
                {
                    WinDesc c = Describe(cur, false);
                    chain.Add(c.ClassName);
                    cur = Nat.GetParent(cur);
                }
                chain.Add(rep.Root.ClassName);
                chain.Reverse();
                rep.HitPath = string.Join(" > ", chain.ToArray());
            }

            // 显示器
            try
            {
                Screen s = Screen.FromHandle(rootH);
                Rectangle b = s.Bounds;
                rep.MonitorInfo = s.DeviceName + "  " + b.Width + "x" + b.Height + " @" + b.Left + "," + b.Top;
            }
            catch { }

            FillProcess(rep, rep.Root.Pid);
            FillFile(rep);
            FillChildren(rep);
            FillSiblings(rep);
            BuildFlags(rep);
            return rep;
        }

        static void FillProcess(Report rep, uint pid)
        {
            rep.Pid = pid;

            IntPtr hp = Nat.OpenProcess(Nat.PROCESS_QUERY_LIMITED_INFORMATION, false, (int)pid);
            if (hp != IntPtr.Zero)
            {
                try
                {
                    StringBuilder sb = new StringBuilder(1024);
                    int len = sb.Capacity;
                    if (Nat.QueryFullProcessImageName(hp, 0, sb, ref len)) rep.ProcPath = sb.ToString();

                    bool wow;
                    bool isWow = Nat.IsWow64Process(hp, out wow) && wow;
                    rep.Arch = OsIs64() ? (isWow ? "x86 (WOW64)" : "x64") : "x86";

                    IntPtr tok;
                    if (Nat.OpenProcessToken(hp, Nat.TOKEN_QUERY, out tok))
                    {
                        try
                        {
                            int elev, ret;
                            if (Nat.GetTokenInformation(tok, 20, out elev, 4, out ret)) rep.Elevated = elev != 0;
                        }
                        finally { Nat.CloseHandle(tok); }
                    }
                }
                finally { Nat.CloseHandle(hp); }
            }

            Process p = null;
            try { p = Process.GetProcessById((int)pid); }
            catch { }

            if (p != null)
            {
                if (rep.ProcPath.Length == 0)
                {
                    try { rep.ProcPath = p.MainModule.FileName; }
                    catch { }
                }
                try { rep.ProcName = p.ProcessName + ".exe"; }
                catch { }
                try { rep.SessionId = p.SessionId; } catch { }
                try { rep.StartTime = p.StartTime.ToString("yyyy-MM-dd HH:mm:ss"); } catch { }
            }

            if (rep.ProcPath.Length > 0 && rep.ProcName.Length == 0)
            {
                try { rep.ProcName = Path.GetFileName(rep.ProcPath); }
                catch { }
            }

            uint ppid; string pname;
            GetParentInfo(pid, out ppid, out pname);
            rep.ParentPid = ppid;
            rep.ParentName = pname;

            rep.CmdLine = GetCommandLine((int)pid);

            if (rep.ProcPath.Length > 0)
            {
                try
                {
                    FileVersionInfo fv = FileVersionInfo.GetVersionInfo(rep.ProcPath);
                    rep.Company = Clean(fv.CompanyName);
                    rep.Product = Clean(fv.ProductName);
                    rep.Description = Clean(fv.FileDescription);
                    rep.FileVersion = Clean(fv.FileVersion) + (Clean(fv.ProductVersion).Length > 0 && Clean(fv.ProductVersion) != Clean(fv.FileVersion) ? L.F("val.product", Clean(fv.ProductVersion)) : "");
                    rep.Copyright = Clean(fv.LegalCopyright);
                }
                catch { }
            }

            // 模块
            IntPtr hm = Nat.OpenProcess(Nat.PROCESS_QUERY_INFORMATION | Nat.PROCESS_VM_READ, false, (int)pid);
            if (hm != IntPtr.Zero)
            {
                try
                {
                    IntPtr[] mods = new IntPtr[1024];
                    int need;
                    if (Nat.EnumProcessModulesEx(hm, mods, mods.Length * IntPtr.Size, out need, Nat.LIST_MODULES_ALL))
                    {
                        int n = need / IntPtr.Size;
                        if (n > mods.Length) n = mods.Length;
                        for (int i = 0; i < n && i < 400; i++)
                        {
                            StringBuilder mb = new StringBuilder(1024);
                            if (Nat.GetModuleFileNameEx(hm, mods[i], mb, mb.Capacity) > 0)
                                rep.Modules.Add(mb.ToString());
                        }
                        if (n > 400) rep.ModulesNote = L.F("note.modtrunc", n);
                    }
                    else rep.ModulesNote = L.T("note.moddenied");
                }
                finally { Nat.CloseHandle(hm); }
            }
            else rep.ModulesNote = L.T("note.moddenied2");
        }

        static string Clean(string s)
        {
            return s == null ? "" : s.Trim();
        }

        // 从完整 DN 中取出 CN，便于阅读
        static string ShortDn(string dn)
        {
            if (dn == null || dn.Length == 0) return "";
            string[] parts = dn.Split(',');
            string first = parts[0].Trim();
            if (first.StartsWith("CN=", StringComparison.OrdinalIgnoreCase)) first = first.Substring(3);
            return first;
        }

        static bool OsIs64()
        {
            if (IntPtr.Size == 8) return true;
            bool wow;
            return Nat.IsWow64Process(Nat.GetCurrentProcess(), out wow) && wow;
        }

        static void GetParentInfo(uint pid, out uint ppid, out string pname)
        {
            ppid = 0; pname = "";
            IntPtr snap = Nat.CreateToolhelp32Snapshot(Nat.TH32CS_SNAPPROCESS, 0);
            if (snap == IntPtr.Zero || snap == (IntPtr)(-1)) return;
            try
            {
                PROCESSENTRY32 pe = new PROCESSENTRY32();
                pe.dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32));
                Dictionary<uint, string> map = new Dictionary<uint, string>();
                if (Nat.Process32First(snap, ref pe))
                {
                    do
                    {
                        if (!map.ContainsKey(pe.th32ProcessID)) map[pe.th32ProcessID] = pe.szExeFile;
                        if (pe.th32ProcessID == pid) ppid = pe.th32ParentProcessID;
                    } while (Nat.Process32Next(snap, ref pe));
                }
                if (ppid != 0 && map.ContainsKey(ppid)) pname = map[ppid];
            }
            finally { Nat.CloseHandle(snap); }
        }

        static string GetCommandLine(int pid)
        {
            try
            {
                using (System.Management.ManagementObjectSearcher q =
                       new System.Management.ManagementObjectSearcher(
                           "SELECT CommandLine FROM Win32_Process WHERE ProcessId=" + pid))
                using (System.Management.ManagementObjectCollection col = q.Get())
                {
                    foreach (System.Management.ManagementBaseObject o in col)
                    {
                        object v = o["CommandLine"];
                        if (v != null) return v.ToString();
                    }
                }
            }
            catch { }
            return "";
        }

        static void FillFile(Report rep)
        {
            string path = rep.ProcPath;
            if (path.Length == 0 || !File.Exists(path)) return;

            try
            {
                FileInfo fi = new FileInfo(path);
                rep.FileSize = fi.Length;
                rep.FileCreated = fi.CreationTime.ToString("yyyy-MM-dd HH:mm:ss");
                rep.FileModified = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch { }

            // 哈希（单次读取三算法）
            try
            {
                if (rep.FileSize > 512L * 1024 * 1024)
                {
                    rep.HashNote = L.T("note.bighash");
                }
                else
                {
                    using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (MD5 h1 = MD5.Create())
                    using (SHA1 h2 = SHA1.Create())
                    using (SHA256 h3 = SHA256.Create())
                    {
                        byte[] buf = new byte[1 << 20];
                        byte[] outBuf = new byte[1 << 20];
                        int n;
                        while ((n = fs.Read(buf, 0, buf.Length)) > 0)
                        {
                            h1.TransformBlock(buf, 0, n, outBuf, 0);
                            h2.TransformBlock(buf, 0, n, outBuf, 0);
                            h3.TransformBlock(buf, 0, n, outBuf, 0);
                        }
                        h1.TransformFinalBlock(buf, 0, 0);
                        h2.TransformFinalBlock(buf, 0, 0);
                        h3.TransformFinalBlock(buf, 0, 0);
                        rep.Md5 = Hex(h1.Hash);
                        rep.Sha1 = Hex(h2.Hash);
                        rep.Sha256 = Hex(h3.Hash);
                    }
                }
            }
            catch (Exception ex) { rep.HashNote = L.F("note.hashfail", ex.Message); }

            // 数字签名
            try
            {
                X509Certificate cert = X509Certificate.CreateFromSignedFile(path);
                X509Certificate2 c2 = new X509Certificate2(cert);
                rep.SigPresent = true;
                rep.SigSubject = c2.Subject;
                rep.SigIssuer = c2.Issuer;
                rep.SigNotAfter = c2.NotAfter.ToString("yyyy-MM-dd");
                try
                {
                    X509Chain chain = new X509Chain();
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    rep.SigValid = chain.Build(c2);
                    if (!rep.SigValid)
                    {
                        List<string> st = new List<string>();
                        foreach (X509ChainStatus s in chain.ChainStatus)
                            st.Add(s.Status.ToString());
                        rep.SigStatus = string.Join(", ", st.ToArray());
                    }
                }
                catch (Exception ex) { rep.SigStatus = L.F("note.sigerr", ex.Message); }
            }
            catch
            {
                rep.SigPresent = false;
            }
        }

        static string Hex(byte[] b)
        {
            if (b == null) return "";
            StringBuilder sb = new StringBuilder(b.Length * 2);
            foreach (byte x in b) sb.Append(x.ToString("x2"));
            return sb.ToString();
        }

        static void FillChildren(Report rep)
        {
            IntPtr rootH = rep.Root.Hwnd;
            int count = 0;
            EnumProc cb = delegate(IntPtr h, IntPtr l)
            {
                if (count >= 240) { rep.ChildrenTruncated = true; return false; }
                rep.Children.Add(Describe(h, true));
                count++;
                return true;
            };
            try { Nat.EnumChildWindows(rootH, cb, IntPtr.Zero); }
            catch { }
        }

        static void FillSiblings(Report rep)
        {
            uint pid = rep.Root.Pid;
            EnumProc cb = delegate(IntPtr h, IntPtr l)
            {
                uint p;
                Nat.GetWindowThreadProcessId(h, out p);
                if (p != pid) return true;
                rep.Siblings.Add(Describe(h, false));
                return true;
            };
            Nat.EnumWindows(cb, IntPtr.Zero);
        }

        static void BuildFlags(Report rep)
        {
            string lp = rep.ProcPath.ToLowerInvariant();

            if (lp.Length > 0)
            {
                if (lp.Contains("\\temp\\") || lp.Contains("\\tmp\\"))
                    rep.Flags.Add(L.F("flag.temp", rep.ProcPath));
                else if (lp.Contains("\\downloads\\"))
                    rep.Flags.Add(L.T("flag.downloads"));
                else if (lp.Contains("\\appdata\\") && !lp.Contains("\\programs\\"))
                    rep.Flags.Add(L.T("flag.appdata"));
            }

            if (!rep.SigPresent) rep.Flags.Add(L.T("flag.nosig"));
            else if (!rep.SigValid) rep.Flags.Add(L.T("flag.badsig"));
            else if (rep.SigSubject.Length > 0) rep.Flags.Add(L.F("flag.signer", ShortDn(rep.SigSubject)));

            if (rep.Company.Length == 0) rep.Flags.Add(L.T("flag.nocompany"));

            if ((rep.Root.ExStyle & 0x8L) != 0) rep.Flags.Add(L.T("flag.topmost"));
            if ((rep.Root.ExStyle & 0x80L) != 0) rep.Flags.Add(L.T("flag.toolwin"));
            if ((rep.Root.ExStyle & 0x80000L) != 0) rep.Flags.Add(L.T("flag.layered"));
            if (!rep.Root.Visible) rep.Flags.Add(L.T("flag.invisible"));
            if (rep.Root.Cloaked) rep.Flags.Add(L.T("flag.cloaked"));
            if (rep.Root.Title.Length == 0) rep.Flags.Add(L.T("flag.notitle"));

            int hidden = 0;
            foreach (WinDesc d in rep.Siblings) if (!d.Visible) hidden++;
            if (hidden > 0) rep.Flags.Add(L.F("flag.hidden", hidden));

            try
            {
                if (rep.FileCreated.Length > 0)
                {
                    DateTime ct = DateTime.Parse(rep.FileCreated);
                    if ((DateTime.Now - ct).TotalDays < 7)
                        rep.Flags.Add(L.F("flag.recent", rep.FileCreated));
                }
            }
            catch { }
        }
    }

    #endregion

    #region ---------- 瞄准覆盖层 ----------

    internal class FrameForm : Form
    {
        public FrameForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Magenta;
            TransparencyKey = Color.Magenta;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000020;  // WS_EX_TRANSPARENT
                cp.ExStyle |= 0x08000000;  // WS_EX_NOACTIVATE
                cp.ExStyle |= 0x00000080;  // WS_EX_TOOLWINDOW
                cp.ExStyle |= 0x00080000;  // WS_EX_LAYERED
                return cp;
            }
        }

        public void SetTarget(int l, int t, int r, int b)
        {
            int pad = 3;
            Rectangle rc = new Rectangle(l - pad, t - pad, (r - l) + pad * 2, (b - t) + pad * 2);
            if (rc.Width < 6) rc.Width = 6;
            if (rc.Height < 6) rc.Height = 6;
            if (Bounds != rc) Bounds = rc;
            if (!Visible) Show();
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Magenta);
            int w = Width - 1, h = Height - 1;
            if (w < 2 || h < 2) return;
            using (Pen p = new Pen(Color.FromArgb(37, 99, 235), 3))
            {
                e.Graphics.DrawRectangle(p, 1, 1, w - 2, h - 2);
            }
            using (Pen p2 = new Pen(Color.FromArgb(255, 255, 255), 1))
            {
                e.Graphics.DrawRectangle(p2, 4, 4, Math.Max(0, w - 8), Math.Max(0, h - 8));
            }
        }
    }

    internal class HudForm : Form
    {
        string _cls = "";
        string _title = "";
        string _root = "";

        public HudForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            ClientSize = new Size(500, 76);
            BackColor = Color.FromArgb(22, 25, 32);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000020;
                cp.ExStyle |= 0x08000000;
                cp.ExStyle |= 0x00000080;
                return cp;
            }
        }

        public void SetInfo(string cls, string title, string rootCls, bool sameRoot)
        {
            _cls = cls;
            _title = title;
            _root = sameRoot ? "" : rootCls;
            Invalidate();
        }

        public void PlaceNear(Point p)
        {
            Screen s = Screen.FromPoint(p);
            Rectangle wa = s.WorkingArea;
            int x = p.X + 22;
            int y = p.Y + 26;
            if (x + Width > wa.Right) x = p.X - Width - 22;
            if (y + Height > wa.Bottom) y = p.Y - Height - 26;
            if (x < wa.Left) x = wa.Left;
            if (y < wa.Top) y = wa.Top;
            if (Location != new Point(x, y)) Location = new Point(x, y);
            if (!Visible) Show();
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(22, 25, 32))) g.FillRectangle(bg, ClientRectangle);
            using (Pen border = new Pen(Color.FromArgb(37, 99, 235), 1)) g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);

            using (Font f1 = new Font("Consolas", 9.5F, FontStyle.Bold))
            using (Font f2 = new Font(L.UiFont, 9F))
            using (SolidBrush blue = new SolidBrush(Color.FromArgb(96, 165, 250)))
            using (SolidBrush white = new SolidBrush(Color.FromArgb(230, 233, 240)))
            using (SolidBrush dim = new SolidBrush(Color.FromArgb(140, 148, 164)))
            {
                // 类名是技术字符串，永远左对齐；标题和"父窗口"是自然语言，阿拉伯语下靠右
                TextFormatFlags dir = L.IsRtl
                    ? (TextFormatFlags.Right | TextFormatFlags.RightToLeft)
                    : TextFormatFlags.Left;

                TextRenderer.DrawText(g, _cls, f1, new Rectangle(10, 8, 480, 20), Color.FromArgb(96, 165, 250),
                    TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);

                string t = _title.Length > 0 ? _title : L.T("hud.notitle");
                TextRenderer.DrawText(g, t, f2, new Rectangle(10, 28, 480, 20), Color.FromArgb(230, 233, 240),
                    dir | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);

                string r = _root.Length > 0 ? L.F("hud.parent", _root) : L.T("hud.top");
                TextRenderer.DrawText(g, r, f2, new Rectangle(10, 48, 480, 20), Color.FromArgb(140, 148, 164),
                    dir | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            }
        }
    }

    #endregion

    #region ---------- 主窗口 ----------

    public class MainForm : Form
    {
        readonly TextBox _txt = new TextBox();
        readonly Label _st = new Label();
        readonly Button _aim = new Button();

        // 语言切换时要重新贴文字的控件，全部留成字段
        readonly Button _bCopyText = new Button();
        readonly Button _bSave = new Button();
        readonly Button _bOpenLoc = new Button();
        readonly Button _bClear = new Button();
        readonly Button _bAbout = new Button();
        readonly Label _lblLang = new Label();
        readonly ComboBox _cboLang = new ComboBox();

        readonly FrameForm _frame;
        readonly HudForm _hud;

        bool _aiming;
        bool _ready;            // 构造函数跑完才允许语言切换回调生效
        bool _syncingLang;      // 程序内部同步下拉框时抑制回调
        Report _report;

        // 用户按住方向键时，SelectedIndexChanged 会以键重复的频率连续触发。
        // 切换语言要重建句柄（RTL 翻转），绝不能在下拉框自己的消息处理里做，
        // 所以这里只记下目标，挂到消息队列之后再真正应用；多次请求合并成一次。
        int _pendingLang = -1;
        bool _langPosted;
        bool _langKeepFocus;

        // 镜像方向翻转的消抖：连续切换期间不重建句柄，静下来再翻
        bool _dirPending;
        readonly Timer _dirTimer = new Timer();

        // 字体按族名缓存：每切一次语言就 new 一个 Font 会造成不必要的 GDI 对象周转，
        // 而 Font 是不可变的，同族同号的实例可以共用（进程内最多十几种）。
        static readonly Dictionary<string, Font> _fontCache = new Dictionary<string, Font>();

        static Font CachedFont(string family, float size)
        {
            string key = family + "|" + size.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Font f;
            if (_fontCache.TryGetValue(key, out f) && f != null) return f;
            f = new Font(family, size);
            _fontCache[key] = f;
            return f;
        }

        public MainForm()
        {
            Text = L.T("app.title");
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1000, 720);
            MinimumSize = new Size(900, 520);
            Font = CachedFont(L.UiFont, 9F);
            BackColor = Color.FromArgb(244, 246, 249);
            KeyPreview = true;
            AutoScaleMode = AutoScaleMode.Dpi;
            ApplyIcon();

            _frame = new FrameForm();
            _hud = new HudForm();

            _dirTimer.Interval = 180;
            _dirTimer.Tick += DirectionTimerTick;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));

            // 顶部工具条：不写死高度，语言变长时自动折行而不是被裁掉
            FlowLayoutPanel bar = new FlowLayoutPanel();
            bar.Dock = DockStyle.Top;
            bar.FlowDirection = FlowDirection.LeftToRight;
            bar.WrapContents = true;
            bar.AutoSize = true;
            bar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            bar.BackColor = Color.White;
            bar.Padding = new Padding(10, 10, 10, 4);

            _aim.Text = L.T("btn.aim");
            _aim.MinimumSize = new Size(120, 32);
            StyleButton(_aim, true);
            _aim.Click += delegate { StartAim(); };
            bar.Controls.Add(_aim);

            _bCopyText.Text = L.T("btn.copytext");
            _bCopyText.MinimumSize = new Size(0, 32);
            StyleButton(_bCopyText, false);
            _bCopyText.Click += delegate { CopyText(_report == null ? "" : _report.ToText(), L.T("st.copiedtext")); };
            bar.Controls.Add(_bCopyText);

            _bSave.Text = L.T("btn.save");
            _bSave.MinimumSize = new Size(0, 32);
            StyleButton(_bSave, false);
            _bSave.Click += delegate { SaveReport(); };
            bar.Controls.Add(_bSave);

            _bOpenLoc.Text = L.T("btn.openloc");
            _bOpenLoc.MinimumSize = new Size(0, 32);
            StyleButton(_bOpenLoc, false);
            _bOpenLoc.Click += delegate { OpenFileLocation(); };
            bar.Controls.Add(_bOpenLoc);

            _bClear.Text = L.T("btn.clear");
            _bClear.MinimumSize = new Size(0, 32);
            StyleButton(_bClear, false);
            _bClear.Click += delegate
            {
                _report = null;
                _txt.Text = "";
                SetStatus(L.T("st.cleared"));
            };
            bar.Controls.Add(_bClear);

            // 「关于」放最右：不常用，但不能找不到
            _bAbout.Text = L.T("btn.about");
            _bAbout.MinimumSize = new Size(0, 32);
            StyleButton(_bAbout, false);
            _bAbout.Click += delegate { ShowAbout(); };
            bar.Controls.Add(_bAbout);

            // 只剩一个内容区，不必再用标签页：白底面板 + 报告框填满
            Panel body = new Panel();
            body.Dock = DockStyle.Fill;
            body.BackColor = Color.White;
            body.Padding = new Padding(14, 10, 14, 10);
            ConfigBox(_txt, true);
            body.Controls.Add(_txt);

            // 状态栏（右侧带语言切换）
            _st.Dock = DockStyle.Fill;
            _st.TextAlign = ContentAlignment.MiddleLeft;
            _st.Padding = new Padding(14, 0, 0, 0);
            _st.Text = L.T("st.ready");
            _st.ForeColor = Color.FromArgb(107, 114, 128);

            _lblLang.Text = L.T("st.lang");
            _lblLang.AutoSize = true;
            _lblLang.TextAlign = ContentAlignment.MiddleRight;
            _lblLang.Margin = new Padding(0, 8, 6, 0);
            _lblLang.ForeColor = Color.FromArgb(140, 148, 164);

            _cboLang.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboLang.Width = 112;
            _cboLang.Margin = new Padding(0, 4, 6, 0);
            foreach (LangId lid in L.All) _cboLang.Items.Add(L.Native(lid));
            _cboLang.SelectedIndex = Array.IndexOf(L.All, L.Current);
            _cboLang.SelectedIndexChanged += delegate { OnLangChanged(); };

            FlowLayoutPanel langBar = new FlowLayoutPanel();
            langBar.Dock = DockStyle.Fill;
            langBar.FlowDirection = FlowDirection.RightToLeft;
            langBar.WrapContents = false;
            langBar.Margin = new Padding(0);
            langBar.Controls.Add(_cboLang);
            langBar.Controls.Add(_lblLang);

            TableLayoutPanel statusPanel = new TableLayoutPanel();
            statusPanel.Dock = DockStyle.Fill;
            statusPanel.BackColor = Color.White;
            statusPanel.ColumnCount = 2;
            statusPanel.RowCount = 1;
            statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            statusPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            statusPanel.Controls.Add(_st, 0, 0);
            statusPanel.Controls.Add(langBar, 1, 0);

            root.Controls.Add(bar, 0, 0);
            root.Controls.Add(body, 0, 1);
            root.Controls.Add(statusPanel, 0, 2);
            Controls.Add(root);

            ApplyDirection();
            _txt.Text = Welcome();
            TextBoxDirection(true);
            _ready = true;
        }

        // 阿拉伯语界面整体镜像（从右到左），其余语言保持从左到右。
        // 报告框单独钉死为 LTR —— 里面是路径、哈希、类名这类技术数据，
        // 跟着镜像会把一行的首尾颠倒过来，反而读不了。
        //
        // 注意：翻转 RightToLeftLayout 会让窗体连同所有子控件销毁并重建窗口句柄，
        // 这是整个程序里最重、也最容易出事的一步（ComboBox 的原生窗口就在被销毁之列，
        // comctl32 里曾因此报过 0xc0000005）。所以连续切换时不要做，等按键停下来再做。
        void ApplyDirection()
        {
            bool rtl = L.IsRtl;
            RightToLeftLayout = rtl;
            RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No;
            _txt.RightToLeft = RightToLeft.No;
        }

        // 方向翻转推迟到"停止切换"之后：按住方向键时每跨一次阿拉伯语都重建一次句柄，
        // 一秒两次的频率下偶尔就会撞上原生层崩溃。这里改成消抖 —— 只有静下来 180ms
        // 才真正翻转，一次连按最多重建一次。
        void ScheduleDirection()
        {
            if (RightToLeftLayout == L.IsRtl)
            {
                _dirPending = false;
                _dirTimer.Stop();
                return;
            }
            _dirPending = true;
            _dirTimer.Stop();
            _dirTimer.Start();
        }

        void DirectionTimerTick(object sender, EventArgs e)
        {
            _dirTimer.Stop();
            if (IsDisposed || !_dirPending) return;
            if (RightToLeftLayout == L.IsRtl) { _dirPending = false; return; }
            // 列表正展开时翻转会把它的下拉窗口一起销毁，等它收起来再说
            if (_cboLang.DroppedDown) { _dirTimer.Start(); return; }

            _dirPending = false;
            bool retake = _cboLang.Focused || ActiveControl == _cboLang;
            try { ApplyDirection(); }
            catch (Exception ex) { if (Trace != null) Trace("  direction EX " + ex.GetType().Name + ": " + ex.Message); }
            if (retake) RetakeLangFocus();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _dirTimer.Stop();
            _dirTimer.Dispose();
            base.OnFormClosed(e);
        }

        // 语言切换后把所有界面文字重新贴一遍，并按新语言重排报告。
        // deferDirection=true 时只改文字，镜像方向交给 ScheduleDirection 消抖后再翻。
        internal void ApplyLanguage(bool deferDirection)
        {
            SuspendLayout();
            _syncingLang = true;
            try
            {
                Text = L.T("app.title");
                Font = CachedFont(L.UiFont, 9F);
                _txt.Font = CachedFont(L.MonoFont, 9F);
                if (deferDirection) ScheduleDirection();
                else { _dirPending = false; _dirTimer.Stop(); ApplyDirection(); }

                _aim.Text = _aiming ? L.T("btn.aim.cancel") : L.T("btn.aim");
                _bCopyText.Text = L.T("btn.copytext");
                _bSave.Text = L.T("btn.save");
                _bOpenLoc.Text = L.T("btn.openloc");
                _bClear.Text = L.T("btn.clear");
                _bAbout.Text = L.T("btn.about");
                _lblLang.Text = L.T("st.lang");

                // 下拉框跟随当前语言（选中项没变时不会触发事件，不必担心递归）
                int idx = Array.IndexOf(L.All, L.Current);
                if (idx >= 0 && _cboLang.SelectedIndex != idx) _cboLang.SelectedIndex = idx;

                if (_report == null)
                {
                    _txt.Text = Welcome();
                    TextBoxDirection(true);
                    SetStatus(L.T("st.ready"));
                }
                else
                {
                    _txt.Text = _report.ToText();
                    TextBoxDirection(false);
                    _txt.SelectionStart = 0; _txt.ScrollToCaret();
                    SetStatus(L.F("st.done", _report.ProcName, _report.Pid, _report.Root.HexHandle));
                }
            }
            finally { _syncingLang = false; ResumeLayout(true); }
        }

        // 这里只登记"想切到哪个语言"，真正的应用放到消息队列里做。
        // 原因：切到 / 切回阿拉伯语时 RightToLeftLayout 会翻转，窗体连同所有子控件都要重建句柄；
        // 而本方法是下拉框处理 CBN_SELCHANGE（按键、点选都会走到这里）的同步回调，
        // 在控件自己的消息处理里销毁它自己的窗口句柄，时序一对上就是原生层崩溃 —— 进程直接消失。
        // 按住方向键时这个回调按键重复频率连发，撞上的概率被放大，所以必须挪出去。
        void OnLangChanged()
        {
            if (!_ready || _syncingLang) return;
            int i = _cboLang.SelectedIndex;
            if (i < 0 || i >= L.All.Length) return;

            // 切到 / 切回阿拉伯语时句柄重建会让焦点掉到窗体上（甚至谁都没有），
            // 键盘切换就此断掉，而且断了就再也接不上。先记住焦点原本在下拉框上，切完再还给它。
            // 列表若正展开着，跨越边界这一下会被句柄重建收起来；这里不重开 ——
            // 鼠标点选时 DroppedDown 同样是 true，重开会让列表在被点完之后自己又弹出来。
            if (_cboLang.Focused || ActiveControl == _cboLang) _langKeepFocus = true;

            _pendingLang = i;
            if (_langPosted) return;          // 已经排过一次队，连发的这几次合并进那一趟

            _langPosted = true;
            try { BeginInvoke((MethodInvoker)ApplyPendingLang); }
            catch { _langPosted = false; }
        }

        // 把排队的语言切换真正落地。多次请求只应用最后一次 —— 中间那些谁也没看见。
        void ApplyPendingLang()
        {
            _langPosted = false;
            int i = _pendingLang;
            _pendingLang = -1;
            if (IsDisposed) return;

            bool retake = _langKeepFocus;
            _langKeepFocus = false;

            if (i >= 0 && i < L.All.Length)
            {
                try
                {
                    if (Trace != null) Trace("  apply enter " + L.Code(L.All[i]));
                    L.Set(L.All[i]);
                    ApplyLanguage(true);
                    SetStatus(L.T("st.langchanged"));
                    if (Trace != null) Trace("  apply done " + L.Code(L.Current));
                }
                catch (Exception ex)
                {
                    // 界面重贴失败不该带走整个进程；真出事也要留下痕迹
                    if (Trace != null) Trace("  apply EX " + ex.GetType().Name + ": " + ex.Message);
                }
            }

            if (retake) RetakeLangFocus();
        }

        // 诊断钩子：探针把它指向日志，正常运行时为 null，不产生任何输出
        internal static Action<string> Trace;

        static void Tr(string s) { if (Trace != null) Trace(s); }
        // 句柄重建的收尾要到这一轮消息处理完之后才结束，当场 Focus() 会被随后的收尾动作顶掉，
        // 所以补焦点必须往后挪一拍。
        void RetakeLangFocus()
        {
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    if (IsDisposed || _cboLang.IsDisposed || !_cboLang.IsHandleCreated) return;
                    if (!_cboLang.CanFocus || _cboLang.Focused) return;

                    _cboLang.Focus();
                    if (!_cboLang.Focused) ActiveControl = _cboLang;   // 抢不过窗体时再退一步
                });
            }
            catch { }
        }

        // 诊断用：模拟"键盘停在语言下拉框上连续切换"，逐个语言报出切完之后的焦点状态。
        // 切到 / 切回阿拉伯语时 RightToLeftLayout 会翻转，窗体连同子控件重建句柄，
        // 焦点就是在这里掉的 —— 这条探针专门盯这个。
        // 结果写文件而不是标准输出：真出问题时进程会被挂住/杀掉，管道里的缓冲会一起丢。
        internal void FocusSwitchProbe(string logPath, string shotPath)
        {
            Action<string> w = delegate(string s)
            {
                try { File.AppendAllText(logPath, s + "\r\n", new UTF8Encoding(false)); }
                catch { }
            };

            w("== start ==");
            w("handle before show = " + IsHandleCreated);
            try
            {
                if (!Visible) Show();
                Application.DoEvents();
                w("after show: visible=" + Visible + " form-focused=" + Focused + " combo=" + _cboLang.Focused);

                Activate();
                Application.DoEvents();
                w("after activate: form-focused=" + Focused + " combo=" + _cboLang.Focused);

                _cboLang.Focus();
                Application.DoEvents();
                w("after combo.Focus: combo=" + _cboLang.Focused + " active=" + (ActiveControl == _cboLang));

                int bad = 0;
                for (int pass = 0; pass < 2; pass++)
                {
                    foreach (LangId id in L.All)
                    {
                        int idx = Array.IndexOf(L.All, id);
                        if (idx < 0 || _cboLang.SelectedIndex == idx) continue;

                        _cboLang.SelectedIndex = idx;
                        Application.DoEvents();
                        Application.DoEvents();     // 补焦点的 BeginInvoke 要再跑一轮

                        bool ok = _cboLang.Focused;
                        if (!ok) bad++;
                        w("  " + L.Code(id) + "  combo=" + ok
                            + "  active=" + (ActiveControl == _cboLang)
                            + "  sel=" + _cboLang.SelectedIndex
                            + "  rtl=" + (RightToLeftLayout ? "y" : "n"));
                    }
                }
                w(bad == 0 ? "focustest OK" : "focustest FAILED (" + bad + " 次丢焦点)");

                // 第三阶段：列表展开着跨 RTL 边界 —— 焦点必须保住；
                // 列表本身会被句柄重建收起来（刻意不重开，见 OnLangChanged 的注释）
                w("-- 列表展开状态 --");
                _cboLang.SelectedIndex = Array.IndexOf(L.All, LangId.Zh);
                Application.DoEvents();
                _cboLang.DroppedDown = true;
                Application.DoEvents();
                w("  zh: dropped=" + _cboLang.DroppedDown + "  combo=" + _cboLang.Focused);

                _cboLang.SelectedIndex = Array.IndexOf(L.All, LangId.Ar);
                Application.DoEvents();
                Application.DoEvents();
                w("  -> ar: dropped=" + _cboLang.DroppedDown + "  combo=" + _cboLang.Focused + "  (列表收起属预期)");

                if (shotPath.Length > 0)
                {
                    try
                    {
                        Rectangle scr = Screen.PrimaryScreen.Bounds;
                        using (Bitmap bmp = new Bitmap(scr.Width, scr.Height))
                        {
                            using (Graphics g = Graphics.FromImage(bmp))
                                g.CopyFromScreen(scr.Location, Point.Empty, scr.Size);
                            bmp.Save(shotPath, System.Drawing.Imaging.ImageFormat.Png);
                        }
                        w("  shot: " + shotPath);
                    }
                    catch (Exception ex) { w("  shot failed: " + ex.Message); }
                }

                _cboLang.SelectedIndex = Array.IndexOf(L.All, LangId.Zh);
                Application.DoEvents();
                Application.DoEvents();
                w("  -> zh: dropped=" + _cboLang.DroppedDown + "  combo=" + _cboLang.Focused);
                _cboLang.DroppedDown = false;
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                w("EX " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        // 诊断用：模拟"按住方向键不放"时的高频切换（键重复约 30 次/秒）。
        // 按键走 PostMessage + DoEvents，即真的由消息泵派发进 ComboBox 的 WndProc，
        // 与真人按住不放的路径一致 —— 句柄重建就发生在 ComboBox 自己的消息处理里。
        // 同时采样 GDI / USER 句柄数：只增不减就是泄漏，撞到进程上限后程序会直接消失。
        internal void LangStressProbe(string logPath, int count, bool rtlOnly)
        {
            Action<string> w = delegate(string s)
            {
                try { File.AppendAllText(logPath, s + "\r\n", new UTF8Encoding(false)); }
                catch { }
            };

            Application.ThreadException += delegate(object s, System.Threading.ThreadExceptionEventArgs e)
            {
                w("ThreadException " + e.Exception.GetType().Name + ": " + e.Exception.Message
                    + "\r\n" + e.Exception.StackTrace);
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
            {
                Exception ex = e.ExceptionObject as Exception;
                w("Unhandled " + (ex == null ? "?" : ex.GetType().Name + ": " + ex.Message)
                    + "\r\n" + (ex == null ? "" : ex.StackTrace));
            };

            w("== langstress start: " + count + " 次  mode=" + (rtlOnly ? "rtl" : "cycle") + " ==");
            Trace = w;
            try
            {
                if (!Visible) Show();
                Application.DoEvents();
                Activate();
                Application.DoEvents();          // 等激活真正落地，否则焦点会被窗体收回去
                _cboLang.Focus();
                Application.DoEvents();

                IntPtr proc = Nat.GetCurrentProcess();
                uint gdi0 = Nat.GetGuiResources(proc, 0);
                uint usr0 = Nat.GetGuiResources(proc, 1);
                w("句柄基线 gdi=" + gdi0 + " user=" + usr0
                    + "  combo=" + _cboLang.Focused + "  handle=0x" + _cboLang.Handle.ToInt64().ToString("X"));

                int switched = 0, lost = 0;
                uint gdiMax = gdi0, usrMax = usr0;
                Stopwatch sw = Stopwatch.StartNew();

                // 按住方向键时键重复约 30 次/秒，也就是连续切换之间只隔 ~25ms；
                // 真人通常是"连按一阵、停一下、再连按"，所以按"连发 + 静默"的节奏模拟。
                const int burstSize = 25, gapMs = 25, quietMs = 450;   // 奇数：每阵结束时停在阿语，便于核对方向
                int bursts = Math.Max(1, count / burstSize);
                int idxZh = Array.IndexOf(L.All, LangId.Zh);
                int idxAr = Array.IndexOf(L.All, LangId.Ar);

                for (int b = 0; b < bursts; b++)
                {
                    long tBurst = sw.ElapsedMilliseconds;
                    // 遍历模式：每阵都从列表头开始，保证这一阵真的会跨过阿拉伯语
                    if (!rtlOnly) { _cboLang.SelectedIndex = 0; Application.DoEvents(); }
                    w("  阵前 rtl=" + (RightToLeftLayout ? "y" : "n") + " lang=" + L.Code(L.Current)
                        + " handle=0x" + _cboLang.Handle.ToInt64().ToString("X"));
                    for (int k = 0; k < burstSize; k++)
                    {
                        if (rtlOnly)
                        {
                            // 中/阿之间来回：每次切换都会改变镜像方向，是最狠的场景
                            _cboLang.SelectedIndex = L.IsRtl ? idxZh : idxAr;
                            Application.DoEvents();
                            switched++;
                        }
                        else
                        {
                            int idx = _cboLang.SelectedIndex;
                            bool down = idx < L.All.Length - 1;
                            Nat.PostMessage(_cboLang.Handle, 0x0100 /*WM_KEYDOWN*/,
                                (IntPtr)(down ? 0x28 /*VK_DOWN*/ : 0x26 /*VK_UP*/), IntPtr.Zero);
                            Application.DoEvents();
                            if (_cboLang.SelectedIndex != idx) switched++;
                        }
                        if (!_cboLang.Focused) lost++;
                        if (k == burstSize - 1)
                            w("  阵末(静默前) rtl=" + (RightToLeftLayout ? "y" : "n")
                                + " handle=0x" + _cboLang.Handle.ToInt64().ToString("X"));
                        System.Threading.Thread.Sleep(gapMs);   // 键重复间隔
                    }

                    // 松手：静默一段时间，消抖计时器应当在这时把镜像方向翻过来
                    long idle = 0;
                    while (idle < quietMs) { Application.DoEvents(); System.Threading.Thread.Sleep(15); idle += 15; }

                    uint g = Nat.GetGuiResources(proc, 0), u = Nat.GetGuiResources(proc, 1);
                    if (g > gdiMax) gdiMax = g;
                    if (u > usrMax) usrMax = u;
                    w("  第 " + (b + 1) + " 阵 @" + (sw.ElapsedMilliseconds - tBurst) + "ms  lang=" + L.Code(L.Current)
                        + "  rtl=" + (RightToLeftLayout ? "y" : "n") + " (期望 " + (L.IsRtl ? "y" : "n") + ")"
                        + "  gdi=" + g + " user=" + u + " combo=" + _cboLang.Focused);
                }

                uint gdi1 = Nat.GetGuiResources(proc, 0);
                uint usr1 = Nat.GetGuiResources(proc, 1);
                w("结束 gdi=" + gdi1 + " (峰值 " + gdiMax + ")  user=" + usr1 + " (峰值 " + usrMax + ")");
                w("切换次数=" + switched + "  丢焦点=" + lost);
                bool dirOk = RightToLeftLayout == L.IsRtl
                    && RightToLeft == (L.IsRtl ? RightToLeft.Yes : RightToLeft.No)
                    && _txt.RightToLeft == RightToLeft.No;
                w("最终方向 rtl=" + (RightToLeftLayout ? "y" : "n") + " 期望=" + (L.IsRtl ? "y" : "n")
                    + (dirOk ? "  direction OK" : "  direction FAILED"));
                bool leak = (gdi1 - gdi0) > 100 || gdiMax - gdi0 > 200;
                w(leak ? "langstress LEAK" : "langstress OK");
            }
            catch (Exception ex)
            {
                w("EX " + ex.GetType().Name + ": " + ex.Message + "\r\n" + ex.StackTrace);
                w("langstress CRASH");
            }
        }

        static string Welcome()
        {
            return L.T("welcome").Replace("\n", "\r\n");
        }

        static void StyleButton(Button b, bool primary)
        {
            b.Height = 30;
            b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b.Padding = new Padding(9, 2, 9, 2);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = primary ? 0 : 1;
            b.FlatAppearance.BorderColor = Color.FromArgb(214, 219, 227);
            b.BackColor = primary ? Color.FromArgb(37, 99, 235) : Color.White;
            b.ForeColor = primary ? Color.White : Color.FromArgb(31, 41, 55);
            b.Margin = new Padding(0, 0, 6, 6);
            b.Cursor = Cursors.Hand;
            b.TabStop = false;
        }

        static void ConfigBox(TextBox t, bool wrap)
        {
            t.Multiline = true;
            t.Dock = DockStyle.Fill;
            t.ReadOnly = true;
            t.ScrollBars = wrap ? ScrollBars.Vertical : ScrollBars.Both;
            t.WordWrap = wrap;
            t.BorderStyle = BorderStyle.None;
            t.Font = CachedFont(L.MonoFont, 9F);
            t.BackColor = Color.White;
            t.ForeColor = Color.FromArgb(23, 26, 32);
            t.MaxLength = 4000000;
            // 报告里是路径、哈希、类名这类技术数据，必须从左到右读，
            // 即使界面是阿拉伯语也不镜像（否则一行的首尾会颠倒得没法看）。
            t.RightToLeft = RightToLeft.No;
        }

        void SetStatus(string s)
        {
            _st.Text = s;
        }

        internal string ReportText() { return _txt.Text; }

        // 诊断用：把当前界面上的关键文字摊平成一行，方便自检程序比对
        internal string UiProbe()
        {
            string first = "";
            string[] lines = _txt.Text.Split('\n');
            if (lines.Length > 0) first = lines[0].Trim();
            return L.Code(L.Current)
                 + " | title=" + Text
                 + " | aim=" + _aim.Text
                 + " | copy=" + _bCopyText.Text
                 + " | save=" + _bSave.Text
                 + " | clear=" + _bClear.Text
                 + " | lang=" + _cboLang.Text
                 + " | status=" + _st.Text
                 + " | head=" + first;
        }

        // ---------- 瞄准 ----------
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _txt.Focus();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try { Nat.RegisterHotKey(Handle, 1, 0, 0x71); }     // F2
            catch { }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { Nat.UnregisterHotKey(Handle, 1); }
            catch { }
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Nat.WM_HOTKEY && m.WParam.ToInt32() == 1)
            {
                StartAim();
                return;
            }
            base.WndProc(ref m);
        }

        void StartAim()
        {
            if (_aiming) return;
            _aiming = true;
            _aim.Text = L.T("btn.aim.cancel");
            SetStatus(L.T("st.aimhint"));
            Cursor = Cursors.Cross;
            Nat.SetCapture(Handle);
        }

        void EndAim()
        {
            _aiming = false;
            Nat.ReleaseCapture();
            _frame.Hide();
            _hud.Hide();
            Cursor = Cursors.Default;
            _aim.Text = L.T("btn.aim");
        }

        void UpdateAim(Point p)
        {
            IntPtr top = Probe.HitTest(p);
            if (top == IntPtr.Zero)
            {
                _frame.Hide();
                _hud.Hide();
                return;
            }

            IntPtr deep = Probe.DeepestAt(top, p);
            WinDesc d = Probe.Describe(deep, false);
            if (d.Left == d.Right || d.Top == d.Bottom)
            {
                d = Probe.Describe(top, false);
            }

            _frame.SetTarget(d.Left, d.Top, d.Right, d.Bottom);

            WinDesc r = d.Hwnd == top ? null : Probe.Describe(top, false);
            string rootCls = r != null ? r.ClassName : "";
            _hud.SetInfo(d.ClassName, d.Display, rootCls, r == null);
            _hud.PlaceNear(p);

            SetStatus(L.F("st.target", d.ClassName, d.Display, d.Pid));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_aiming) return;
            UpdateAim(Cursor.Position);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!_aiming) return;
            if (e.Button == MouseButtons.Left) CommitAim(Cursor.Position);
            else if (e.Button == MouseButtons.Right) { EndAim(); SetStatus(L.T("st.aimcancel")); }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_aiming && keyData == Keys.Escape)
            {
                EndAim();
                SetStatus(L.T("st.aimcancel"));
                return true;
            }
            if (keyData == Keys.F1)
            {
                ShowAbout();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // 「关于」：弹出模态框，两段像素动画各播各的。
        // 瞄准过程中按 F1 也允许 —— 先把准星收回来，否则模态框一开，鼠标就再也点不到目标
        void ShowAbout()
        {
            if (!_ready) return;
            if (_aiming) EndAim();
            try
            {
                using (AboutForm f = new AboutForm()) f.ShowDialog(this);
            }
            catch (Exception ex) { SetStatus(L.F("st.fail", ex.Message)); }
        }

        void CommitAim(Point p)
        {
            EndAim();
            IntPtr top = Probe.HitTest(p);
            if (top == IntPtr.Zero)
            {
                SetStatus(L.T("st.nohit"));
                return;
            }
            IntPtr deep = Probe.DeepestAt(top, p);
            Cursor = Cursors.WaitCursor;
            SetStatus(L.T("st.collecting"));
            Application.DoEvents();
            try
            {
                ShowReport(Probe.Collect(deep, p));
                SetStatus(L.F("st.done", _report.ProcName, _report.Pid, _report.Root.HexHandle));
                Activate();
            }
            catch (Exception ex)
            {
                SetStatus(L.F("st.fail", ex.Message));
                MessageBox.Show(this, L.F("msg.captureerr", ex), L.T("app.brand"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { Cursor = Cursors.Default; }
        }

        internal void ShowReport(Report rep)
        {
            _report = rep;
            _txt.Text = rep.ToText();
            TextBoxDirection(false);
            _txt.SelectionStart = 0;
            _txt.ScrollToCaret();
        }

        // 文本框的阅读方向要跟内容走，而不是跟界面语言走：
        //   说明文字（散文）→ 阿拉伯语下整体右对齐，符合母语阅读习惯
        //   采集报告        → 永远从左到右，里面是路径、哈希、类名这类技术数据
        // 注意：必须在 Text 赋值**之后**调用。TextBox.Text 的 setter 会重置选区，
        // 先清选区再赋文本等于白清，RTL 下会整页画成蓝色选中（踩过）。
        void TextBoxDirection(bool prose)
        {
            RightToLeft rtl = (prose && L.IsRtl) ? RightToLeft.Yes : RightToLeft.No;
            if (_txt.RightToLeft != rtl) _txt.RightToLeft = rtl;
            _txt.SelectionStart = 0;
            _txt.SelectionLength = 0;
        }

        // ---------- 动作 ----------
        void CopyText(string s, string msg)
        {
            if (string.IsNullOrEmpty(s)) { SetStatus(L.T("st.nocopy")); return; }
            try
            {
                Clipboard.SetText(s);
                SetStatus(msg + L.F("st.copiedsuffix", s.Length));
            }
            catch (Exception ex) { SetStatus(L.F("st.copyfail", ex.Message)); }
        }

        void SaveReport()
        {
            if (_report == null) { SetStatus(L.T("st.empty")); return; }
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Title = L.T("dlg.save.title");
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string safe = _report.ProcName.Length > 0 ? _report.ProcName : "window";
                dlg.FileName = "Laocoon_" + safe + "_" + stamp + ".txt";
                dlg.Filter = L.T("dlg.filter");
                dlg.DefaultExt = "txt";
                dlg.AddExtension = true;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    File.WriteAllText(dlg.FileName, _report.ToText(), new UTF8Encoding(true));
                    SetStatus(L.F("st.saved", dlg.FileName));
                }
                catch (Exception ex)
                {
                    SetStatus(L.F("st.savefail", ex.Message));
                }
            }
        }

        // 窗口图标：读内嵌的多尺寸 ico，系统按需要挑 16 / 32 的帧，
        // 比把一张 32 缩成 16 干净得多（标题栏不会发糊）。
        void ApplyIcon()
        {
            try
            {
                using (Stream s = typeof(MainForm).Assembly.GetManifestResourceStream("Laocoon.ico"))
                {
                    if (s != null) Icon = new System.Drawing.Icon(s);
                }
            }
            catch { }
        }

        void OpenFileLocation()
        {
            if (_report == null || _report.ProcPath.Length == 0)
            {
                SetStatus(L.T("st.nopath"));
                return;
            }
            try
            {
                Process.Start("explorer.exe", "/select,\"" + _report.ProcPath + "\"");
                SetStatus(L.F("st.located", _report.ProcPath));
            }
            catch (Exception ex) { SetStatus(L.F("st.openfail", ex.Message)); }
        }
    }

    #endregion

    #region ---------- 关于 ----------

    // 像素动画播放器。自己逐帧画，既不用 ImageAnimator 也不用 PictureBox 的 GIF 支持：
    //   1) GDI+ 会把「帧延迟小于 2/100 秒」一律当 100ms，节奏会被抹平；
    //   2) PictureBox 缩放走双三次插值，像素画一放大就糊成水彩；
    //   3) 自己画才能按控件尺寸等比放大，并固定用最近邻，边缘永远是硬边。
    internal sealed class GifView : Control
    {
        const int DefaultDelay = 100;

        Image _img;
        Stream _src;            // GDI+ 是延迟解码的，源流必须活到图像释放为止
        int _frames;
        int[] _delay;
        int _i;
        readonly Timer _timer = new Timer();

        public GifView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.FromArgb(23, 35, 59);     // 和动画底色同色，缩放的留白处不透白
            TabStop = false;
            _timer.Tick += delegate { Next(); };
        }

        public bool Load(string resource)
        {
            try
            {
                Stream res = typeof(GifView).Assembly.GetManifestResourceStream(resource);
                if (res == null) return false;

                MemoryStream ms = new MemoryStream();
                res.CopyTo(ms);
                res.Dispose();
                ms.Position = 0;

                Image img = Image.FromStream(ms, false, true);
                int n = img.GetFrameCount(FrameDimension.Time);
                if (n <= 0) { img.Dispose(); ms.Dispose(); return false; }

                _img = img;
                _src = ms;
                _frames = n;
                _delay = ReadDelay(img, n);
                _i = 0;
                img.SelectActiveFrame(FrameDimension.Time, 0);
                return true;
            }
            catch { return false; }
        }

        // 每帧的延迟存在 0x5100 里，单位 1/100 秒。缺项、或者写成 1/100 秒的，
        // 按 100ms 处理 —— 这是各浏览器对「没写延迟」的通行解释。
        static int[] ReadDelay(Image img, int n)
        {
            int[] d = new int[n];
            for (int i = 0; i < n; i++) d[i] = DefaultDelay;
            try
            {
                PropertyItem p = img.GetPropertyItem(0x5100);
                if (p != null && p.Value != null && p.Value.Length >= n * 4)
                {
                    for (int i = 0; i < n; i++)
                    {
                        int v = BitConverter.ToInt32(p.Value, i * 4) * 10;
                        d[i] = v < 25 ? DefaultDelay : v;
                    }
                }
            }
            catch { }
            return d;
        }

        public void Start()
        {
            if (_img == null || _frames <= 1) return;
            _i = 0;
            try { _img.SelectActiveFrame(FrameDimension.Time, 0); } catch { return; }
            Invalidate();
            _timer.Interval = _delay[0];
            _timer.Start();
        }

        public void Stop() { _timer.Stop(); }

        void Next()
        {
            _timer.Stop();
            if (_img == null || _frames <= 1 || IsDisposed) return;
            _i = (_i + 1) % _frames;
            try { _img.SelectActiveFrame(FrameDimension.Time, _i); }
            catch { return; }
            Invalidate();
            _timer.Interval = _delay[_i];
            _timer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            if (_img == null) return;

            float s = Math.Min((float)Width / _img.Width, (float)Height / _img.Height);
            int w = (int)Math.Round(_img.Width * s);
            int h = (int)Math.Round(_img.Height * s);

            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;   // 不偏半像素，边缘才不糊
            e.Graphics.SmoothingMode = SmoothingMode.None;
            e.Graphics.DrawImage(_img, new Rectangle((Width - w) / 2, (Height - h) / 2, w, h));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Dispose();
                if (_img != null) { _img.Dispose(); _img = null; }
                if (_src != null) { _src.Dispose(); _src = null; }
            }
            base.Dispose(disposing);
        }
    }

    // 「关于」：两段像素动画并排放着播，一段讲名字打哪来，一段讲这程序干什么。
    // 文字全部走 L，跟随当前界面语言；阿拉伯语下整窗镜像。
    internal sealed class AboutForm : Form
    {
        const int GifW = 320;
        const int GifH = 180;

        readonly GifView _origin = new GifView();
        readonly GifView _detect = new GifView();

        public AboutForm()
        {
            Text = L.T("about.title");
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.White;
            Font = new Font(L.UiFont, 9F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(700, 404);
            // 阿拉伯语整窗镜像；句柄还没建，这里翻转是安全的
            RightToLeftLayout = L.IsRtl;
            RightToLeft = L.IsRtl ? RightToLeft.Yes : RightToLeft.No;
            ApplyIcon();

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.Padding = new Padding(18, 16, 18, 12);
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 4; i++) root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            root.Controls.Add(Header(), 0, 0);
            root.Controls.Add(Gifs(), 0, 1);

            Label story = new Label();
            story.Text = L.T("about.story");
            story.AutoSize = true;
            story.MaximumSize = new Size(660, 0);
            story.ForeColor = Color.FromArgb(90, 98, 112);
            story.Margin = new Padding(1, 12, 0, 0);
            root.Controls.Add(story, 0, 2);

            root.Controls.Add(Footer(), 0, 3);
            Controls.Add(root);

            // 说明文字的长短随语言变：德语、俄语一行放不下会折成三四行。
            // 高度按实际内容量出来再定，免得最后一行被窗口底边切掉。
            Size pref = root.GetPreferredSize(new Size(700, 0));
            if (pref.Height > 0) ClientSize = new Size(700, pref.Height);
        }

        // 两段动画并排。并排而不是上下叠：叠起来窗口要 690 高，768 的笔记本屏幕装不下。
        Control Gifs()
        {
            TableLayoutPanel row = new TableLayoutPanel();
            row.ColumnCount = 2;
            row.AutoSize = true;
            row.Dock = DockStyle.Fill;
            row.Margin = new Padding(0, 10, 0, 0);
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            row.Controls.Add(Cell(_origin, "Laocoon.origin.gif", L.T("about.cap1")), 0, 0);
            row.Controls.Add(Cell(_detect, "Laocoon.detect.gif", L.T("about.cap2")), 1, 0);
            return row;
        }

        Control Cell(GifView v, string res, string caption)
        {
            TableLayoutPanel c = new TableLayoutPanel();
            c.ColumnCount = 1;
            c.AutoSize = true;
            c.Dock = DockStyle.Fill;
            c.Margin = new Padding(0, 0, 2, 0);
            c.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            c.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            c.Controls.Add(Caption(caption), 0, 0);

            v.Size = new Size(GifW, GifH);
            v.Anchor = AnchorStyles.None;        // 宽窗口里居中，窄窗口也不会被拉伸
            v.Margin = new Padding(0);
            v.Load(res);
            c.Controls.Add(v, 0, 1);
            return c;
        }

        Control Header()
        {
            TableLayoutPanel head = new TableLayoutPanel();
            head.ColumnCount = 2;
            head.AutoSize = true;
            head.Dock = DockStyle.Fill;
            head.Margin = new Padding(0);
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52F));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            PictureBox ico = new PictureBox();
            ico.SizeMode = PictureBoxSizeMode.Zoom;
            ico.Size = new Size(44, 44);
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
            t.Text = L.T("app.brand");
            t.AutoSize = true;
            t.Font = new Font(L.UiFont, 15F, FontStyle.Bold);
            t.ForeColor = Color.FromArgb(23, 35, 59);
            t.Margin = new Padding(0);
            txt.Controls.Add(t, 0, 0);

            Label v = new Label();
            v.Text = L.F("about.version", AppVersion);
            v.AutoSize = true;
            v.ForeColor = Color.FromArgb(120, 128, 142);
            v.Margin = new Padding(1, 3, 0, 0);
            txt.Controls.Add(v, 0, 1);

            head.Controls.Add(txt, 1, 0);
            return head;
        }

        Label Caption(string s)
        {
            Label l = new Label();
            l.Text = s;
            l.AutoSize = true;
            l.Font = new Font(L.UiFont, 8F, FontStyle.Bold);
            l.ForeColor = Color.FromArgb(140, 148, 164);
            l.Margin = new Padding(2, 0, 0, 6);
            return l;
        }


        Control Footer()
        {
            TableLayoutPanel foot = new TableLayoutPanel();
            foot.ColumnCount = 2;
            foot.AutoSize = true;
            foot.Dock = DockStyle.Fill;
            foot.Margin = new Padding(0, 14, 0, 0);
            foot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            foot.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            LinkLabel link = new LinkLabel();
            link.Text = L.T("about.link");
            link.AutoSize = true;
            link.Anchor = AnchorStyles.Left;
            link.Margin = new Padding(1, 6, 0, 0);
            link.LinkColor = Color.FromArgb(37, 99, 235);
            link.LinkClicked += delegate
            {
                try { Process.Start("https://github.com/xatwebez/Laocoon"); }
                catch { }
            };
            foot.Controls.Add(link, 0, 0);

            Button ok = new Button();
            ok.Text = L.T("about.close");
            ok.DialogResult = DialogResult.OK;
            ok.AutoSize = true;                     // 德语 "Schließen" 比「关闭」宽得多
            ok.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ok.MinimumSize = new Size(88, 30);
            ok.Padding = new Padding(12, 0, 12, 0);
            ok.FlatStyle = FlatStyle.Flat;
            ok.FlatAppearance.BorderColor = Color.FromArgb(214, 219, 227);
            ok.BackColor = Color.White;
            ok.ForeColor = Color.FromArgb(31, 41, 55);
            ok.Cursor = Cursors.Hand;
            ok.Margin = new Padding(6, 0, 0, 0);
            foot.Controls.Add(ok, 1, 0);

            AcceptButton = ok;
            CancelButton = ok;
            return foot;
        }

        // 版本号取自程序集，别在这里再手写一份 —— 两处对不上比不写还糟
        static string AppVersion
        {
            get
            {
                try
                {
                    Version v = typeof(AboutForm).Assembly.GetName().Version;
                    return v.Major + "." + v.Minor + "." + v.Build;
                }
                catch { return ""; }
            }
        }

        void ApplyIcon()
        {
            try
            {
                using (Stream s = typeof(AboutForm).Assembly.GetManifestResourceStream("Laocoon.ico"))
                {
                    if (s != null) Icon = new System.Drawing.Icon(s);
                }
            }
            catch { }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _origin.Start();
            _detect.Start();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _origin.Stop();
            _detect.Stop();
            base.OnFormClosed(e);
        }
    }

    #endregion

    #region ---------- 入口 ----------

    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            BootstrapLanguage(args);

            if (args.Length > 0 && args[0] == "--i18ncheck")
            {
                I18nCheck();
                return;
            }

            if (args.Length > 0 && args[0] == "--selftest")
            {
                SelfTest(args.Length > 1 ? args[1] : "");
                return;
            }

            if (args.Length > 0 && args[0] == "--uishot")
            {
                // --welcome 表示只渲染欢迎页，不去抓别的窗口
                // --screen  表示改用屏幕抓取（DrawToBitmap 对 RTL 文本框会画出假选区）
                UiShot(args.Length > 1 ? args[1] : "",
                       Array.IndexOf(args, "--welcome") >= 0,
                       Array.IndexOf(args, "--screen") >= 0);
                return;
            }

            if (args.Length > 0 && args[0] == "--aboutshot")
            {
                AboutShot(args.Length > 1 ? args[1] : "",
                          Array.IndexOf(args, "--screen") >= 0);
                return;
            }

            if (args.Length > 0 && args[0] == "--biditest")
            {
                BidiTest(args.Length > 1 ? args[1] : "");
                return;
            }

            if (args.Length > 0 && args[0] == "--aimtest")
            {
                AimTest();
                return;
            }

            if (args.Length > 0 && args[0] == "--langswitch")
            {
                LangSwitchTest();
                return;
            }

            if (args.Length > 0 && args[0] == "--focustest")
            {
                FocusTest(args.Length > 1 ? args[1] : "", args.Length > 2 ? args[2] : "");
                return;
            }

            if (args.Length > 0 && args[0] == "--langstress")
            {
                int n = 300;
                if (args.Length > 1) { int.TryParse(args[1], out n); }
                if (n <= 0) n = 300;
                LangStress(args.Length > 2 ? args[2] : "", n,
                    Array.IndexOf(args, "--rtl") >= 0);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        // 语言优先级：--lang=xx  >  系统界面语言  >  英语
        // 不落盘：切过的语言只在本次运行有效，下次启动重新按上面的顺序判定
        static void BootstrapLanguage(string[] args)
        {
            L.Set(L.Detect());

            foreach (string a in args)
                if (a.StartsWith("--lang=", StringComparison.OrdinalIgnoreCase)) { L.Set(a.Substring(7)); break; }
        }

        // 诊断用：检查全部语言的键是否齐全、标签列宽是否合理、字体是否真的存在
        static void I18nCheck()
        {
            try { Console.OutputEncoding = Encoding.UTF8; } catch { }
            Console.WriteLine("total keys = " + L.KeyCount + "   languages = " + L.All.Length);
            bool ok = true;
            foreach (LangId id in L.All)
            {
                List<string> miss = L.Missing(id);
                L.Set(id);
                Console.WriteLine(string.Format("{0}  {1,-12}  labelCol={2,-3} font={3,-20} mono={4,-16} rtl={5,-5} pad={6,-5} missing={7}",
                    L.Code(id), L.Native(id), L.LabelCol, L.UiFont, L.MonoFont,
                    L.IsRtl, L.PadLabels, miss.Count));
                Console.WriteLine(string.Format("      title=\"{0}\"  longest=\"{1}\" ({2})",
                    L.T("app.title"), L.LongestLabel, L.LongestLabelWidth));
                if (miss.Count > 0)
                {
                    ok = false;
                    foreach (string k in miss) Console.WriteLine("      MISSING: " + k);
                }
            }
            Console.WriteLine("fonts present: YaHei=" + L.FontAvailable("Microsoft YaHei UI")
                + " YuGothic=" + L.FontAvailable("Yu Gothic UI")
                + " Malgun=" + L.FontAvailable("Malgun Gothic")
                + " Nirmala=" + L.FontAvailable("Nirmala UI")
                + " Segoe=" + L.FontAvailable("Segoe UI")
                + " MSGothic=" + L.FontAvailable("MS Gothic")
                + " GulimChe=" + L.FontAvailable("GulimChe")
                + " Consolas=" + L.FontAvailable("Consolas"));
            Console.WriteLine(ok ? "i18ncheck OK" : "i18ncheck FAILED");
            if (!ok) Environment.Exit(1);
        }

        // 诊断用：验证瞄准覆盖层与命中测试
        static void AimTest()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Point center = new Point(Screen.PrimaryScreen.Bounds.Width / 2, Screen.PrimaryScreen.Bounds.Height / 2);
            IntPtr top = Probe.HitTest(center);
            Console.WriteLine("hit at center " + center + " = 0x" + top.ToInt64().ToString("X"));
            if (top != IntPtr.Zero)
            {
                IntPtr deep = Probe.DeepestAt(top, center);
                Console.WriteLine("deep = 0x" + deep.ToInt64().ToString("X"));
                WinDesc d = Probe.Describe(deep, false);
                Console.WriteLine("deep class=" + d.ClassName + " rect=" + d.Left + "," + d.Top + "," + d.Right + "," + d.Bottom + " pid=" + d.Pid);
                WinDesc r = Probe.Describe(top, false);
                Console.WriteLine("root class=" + r.ClassName + " title=" + r.Title);
            }

            using (FrameForm ff = new FrameForm())
            using (HudForm hf = new HudForm())
            {
                ff.Show();
                hf.Show();
                ff.SetTarget(100, 100, 500, 400);
                hf.SetInfo("TestClass", "测试窗口标题", "RootClass", false);
                hf.PlaceNear(new Point(200, 200));
                Application.DoEvents();
                Console.WriteLine("frame bounds=" + ff.Bounds + " visible=" + ff.Visible);
                Console.WriteLine("hud bounds=" + hf.Bounds + " visible=" + hf.Visible);

                // 连续移动 20 次，检查跟随与隐藏不抛异常
                for (int i = 0; i < 20; i++)
                {
                    ff.SetTarget(100 + i * 10, 100 + i * 5, 500 + i * 10, 400 + i * 5);
                    hf.PlaceNear(new Point(200 + i * 10, 200 + i * 5));
                }
                Application.DoEvents();
                ff.Hide();
                hf.Hide();
                Application.DoEvents();
                Console.WriteLine("frame visible after hide = " + ff.Visible);
            }

            // 捕获 API 自检
            IntPtr cap = Nat.SetCapture(IntPtr.Zero);
            Console.WriteLine("SetCapture ok, prev=0x" + cap.ToInt64().ToString("X") + " release=" + Nat.ReleaseCapture());
            Console.WriteLine("aimtest OK");
        }

        static void FocusTest(string path, string shot)
        {
            if (path.Length == 0) path = "focus.log";
            try { File.WriteAllText(path, "", new UTF8Encoding(false)); } catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 探针会把语言切一圈；程序本身已不再写任何配置文件，跑完无需还原
            using (MainForm f = new MainForm())
            {
                f.FocusSwitchProbe(path, shot);
                f.Close();
            }

            Environment.Exit(0);
        }

        static void LangStress(string path, int count, bool rtlOnly)
        {
            if (path.Length == 0) path = "langstress.log";
            try { File.WriteAllText(path, "", new UTF8Encoding(false)); } catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (MainForm f = new MainForm())
            {
                f.LangStressProbe(path, count, rtlOnly);
                f.Close();
            }

            Environment.Exit(0);
        }

        // 诊断用：模拟用户在语言下拉框里逐个切换，检查界面文字是否全部跟着变
        static void LangSwitchTest()
        {
            try { Console.OutputEncoding = Encoding.UTF8; } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (MainForm f = new MainForm())
            {
                f.Show();
                Application.DoEvents();

                bool ok = true;
                foreach (LangId id in L.All)
                {
                    L.Set(id);
                    f.ApplyLanguage(false);
                    Application.DoEvents();
                    string probe = f.UiProbe();
                    Console.WriteLine(probe);
                    if (probe.IndexOf(L.T("btn.aim"), StringComparison.Ordinal) < 0)
                    {
                        ok = false;
                        Console.WriteLine("   ! aim button not localized");
                    }
                }

                // 有报告时切语言，正文必须按新语言重新生成
                IntPtr target = IntPtr.Zero;
                EnumProc cb = delegate(IntPtr h, IntPtr l)
                {
                    if (!Nat.IsWindowVisible(h)) return true;
                    if (Nat.GetWindowTextLength(h) == 0) return true;
                    uint pid;
                    Nat.GetWindowThreadProcessId(h, out pid);
                    if (pid == (uint)Process.GetCurrentProcess().Id) return true;
                    target = h;
                    return false;
                };
                Nat.EnumWindows(cb, IntPtr.Zero);

                if (target != IntPtr.Zero)
                {
                    try { f.ShowReport(Probe.Collect(target, new Point(10, 10))); }
                    catch (Exception ex) { Console.WriteLine("collect: " + ex.Message); }

                    foreach (LangId id in L.All)
                    {
                        L.Set(id);
                        f.ApplyLanguage(false);
                        Application.DoEvents();
                        string txt = f.ReportText();
                        bool has = txt.IndexOf(L.T("rep.sec1"), StringComparison.Ordinal) >= 0;
                        Console.WriteLine("report " + L.Code(id) + "  sec1=" + has + "  len=" + txt.Length);
                        if (!has) ok = false;
                    }
                }

                f.Close();
                Console.WriteLine(ok ? "langswitch OK" : "langswitch FAILED");
                if (!ok) Environment.Exit(1);
            }
        }

        // 诊断用：阿拉伯文标签 + 拉丁技术数据混排时，几种写法的实际渲染对照。
        // GDI 的 BiDi 行为没法靠文档推断，只能真渲染出来看哪一列是对的。
        static void BidiTest(string path)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string[][] rows = new string[][]
            {
                new string[] { "المقبض", "0x0001048E" },
                new string[] { "سطر الأوامر", "\"C:\\Windows\\notepad.exe\" --flag" },
                new string[] { "المنطقة العميلة", "2560 x 1528" },
                new string[] { "الحالة", "ظاهرة=نعم  ممكّنة=نعم  DWM=لا" },
                new string[] { "المسار الكامل", "C:\\Program Files\\App\\app.exe" }
            };

            string[] names = new string[]
            {
                "A  label + \": \" + value        [LTR box]",
                "B  label + LRM+:+LRM + value    [LTR box]",
                "C  value + \"   \" + label        [LTR box]",
                "D  label + \": \" + value        [RTL box]",
                "E  label + RLM+:+RLM + value    [RTL box]",
                "F  label \\n value (two lines)   [LTR box]",
                "G  Arabic prose  [TextBox RTL, ReadOnly]",
                "H  Arabic prose  [RichTextBox RTL, ReadOnly]"
            };

            string prose = "Laocoön · فاحص النوافذ\r\n"
                         + "أمام طروادة، أراد الجميع إدخال ذلك الحصان الخشبي إلى المدينة. وحده لاокоون رفض.\r\n"
                         + "1. اضغط F2 (أو انقر «تصويب» في أعلى اليسار)\r\n"
                         + "2. يتحوّل المؤشر إلى صليب — حرّكه فوق نافذة مشبوهة\r\n";

            using (Form f = new Form())
            {
                f.ClientSize = new Size(1560, 940);
                f.Text = "BiDi test";
                f.Font = new Font("Segoe UI", 9F);
                f.StartPosition = FormStartPosition.Manual;
                f.Location = new Point(0, 0);

                TableLayoutPanel g = new TableLayoutPanel();
                g.Dock = DockStyle.Fill;
                g.ColumnCount = 2;
                g.RowCount = 8;
                g.Padding = new Padding(6);
                g.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                g.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                for (int r = 0; r < 8; r++)
                    g.RowStyles.Add(new RowStyle(r % 2 == 0 ? SizeType.AutoSize : SizeType.Percent, r % 2 == 0 ? 0F : 25F));

                for (int i = 0; i < 8; i++)
                {
                    int col = i % 2, row = (i / 2) * 2;

                    Label lb = new Label();
                    lb.Text = names[i];
                    lb.AutoSize = true;
                    lb.Margin = new Padding(2, 4, 2, 2);
                    g.Controls.Add(lb, col, row);

                    bool proseBox = i >= 6;
                    System.Windows.Forms.TextBoxBase t;
                    if (i == 7) t = new RichTextBox();
                    else t = new TextBox();

                    t.Multiline = true;
                    t.ReadOnly = true;
                    t.WordWrap = false;
                    t.Dock = DockStyle.Fill;
                    t.Font = new Font("Segoe UI", 9F);
                    TextBox tb = t as TextBox;
                    if (tb != null) tb.ScrollBars = ScrollBars.None;
                    RichTextBox rb = t as RichTextBox;
                    if (rb != null) { rb.ScrollBars = RichTextBoxScrollBars.None; rb.WordWrap = true; }
                    if (proseBox)
                    {
                        t.Text = prose;
                        t.RightToLeft = RightToLeft.Yes;
                    }
                    else
                    {
                        t.Text = BuildBidiSample(rows, i);
                        t.RightToLeft = (i == 3 || i == 4) ? RightToLeft.Yes : RightToLeft.No;
                    }
                    // 把插入点顶到末尾，排除"残留选区"这个干扰因素
                    t.SelectionStart = t.TextLength;
                    t.SelectionLength = 0;
                    g.Controls.Add(t, col, row + 1);
                }

                f.Controls.Add(g);
                f.Show();
                Application.DoEvents();
                System.Threading.Thread.Sleep(400);
                Application.DoEvents();

                if (path.Length > 0)
                {
                    using (Bitmap bmp = new Bitmap(f.Width, f.Height))
                    {
                        f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
                        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                        Console.WriteLine("biditest saved: " + path + "  " + bmp.Width + "x" + bmp.Height);
                    }
                }
                f.Close();
            }
        }

        static string BuildBidiSample(string[][] rows, int mode)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string[] r in rows)
            {
                string lb = r[0], v = r[1];
                switch (mode)
                {
                    case 0:
                    case 3:
                        sb.Append(lb).Append(": ").Append(v); break;
                    case 1:
                        sb.Append(lb).Append('\u200E').Append(':').Append('\u200E').Append(' ').Append(v); break;
                    case 2:
                        sb.Append(v).Append("   ").Append(lb); break;
                    case 4:
                        sb.Append(lb).Append('\u200F').Append(':').Append('\u200F').Append(' ').Append(v); break;
                    default:
                        sb.Append(lb).Append("\r\n    ").Append(v); break;
                }
                sb.Append("\r\n");
            }
            return sb.ToString();
        }

        // 诊断用：把主界面渲染成 PNG（仅自身窗口内容）
        static void UiShot(string path, bool welcomeOnly = false)
        {
            UiShot(path, welcomeOnly, false);
        }

        static void UiShot(string path, bool welcomeOnly, bool screenGrab)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            IntPtr target = IntPtr.Zero;
            EnumProc cb = delegate(IntPtr h, IntPtr l)
            {
                if (!Nat.IsWindowVisible(h)) return true;
                if (Nat.GetWindowTextLength(h) == 0) return true;
                uint pid;
                Nat.GetWindowThreadProcessId(h, out pid);
                if (pid == (uint)Process.GetCurrentProcess().Id) return true;
                RECT r;
                if (!Nat.GetWindowRect(h, out r)) return true;
                if (r.Right - r.Left < 400) return true;
                target = h;
                return false;
            };
            Nat.EnumWindows(cb, IntPtr.Zero);

            using (MainForm f = new MainForm())
            {
                if (screenGrab)
                {
                    f.StartPosition = FormStartPosition.Manual;
                    f.Location = new Point(0, 0);
                }
                f.Show();
                if (screenGrab) { f.TopMost = true; f.Activate(); }
                Application.DoEvents();
                if (target != IntPtr.Zero && !welcomeOnly)
                {
                    try { f.ShowReport(Probe.Collect(target, new Point(10, 10))); }
                    catch (Exception ex) { Console.WriteLine("uishot collect: " + ex.Message); }
                }
                Application.DoEvents();
                System.Threading.Thread.Sleep(screenGrab ? 800 : 500);
                Application.DoEvents();

                if (path.Length > 0)
                {
                    using (Bitmap bmp = new Bitmap(f.Width, f.Height))
                    {
                        if (screenGrab)
                        {
                            using (Graphics g = Graphics.FromImage(bmp))
                                g.CopyFromScreen(f.Location, Point.Empty, f.Size);
                        }
                        else
                        {
                            f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
                        }
                        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                        Console.WriteLine("uishot saved: " + path + "  " + bmp.Width + "x" + bmp.Height + (screenGrab ? "  [screen]" : ""));
                    }
                }
                f.Close();
            }
        }

        // 诊断用：把「关于」窗口渲染成 PNG，用来核对排版与动画是否真的在走
        static void AboutShot(string path, bool screenGrab)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (AboutForm f = new AboutForm())
            {
                f.StartPosition = FormStartPosition.Manual;
                f.Location = new Point(0, 0);
                f.Show();
                Application.DoEvents();
                // 走几帧再抓：Sleep 期间消息泵是停的，得把等待切成小段，计时器消息才轮得上
                for (int i = 0; i < 10; i++)
                {
                    System.Threading.Thread.Sleep(80);
                    Application.DoEvents();
                }

                if (path.Length > 0)
                {
                    using (Bitmap bmp = new Bitmap(f.Width, f.Height))
                    {
                        if (screenGrab)
                        {
                            using (Graphics g = Graphics.FromImage(bmp))
                                g.CopyFromScreen(f.Location, Point.Empty, f.Size);
                        }
                        else f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
                        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                        Console.WriteLine("aboutshot saved: " + path + "  " + bmp.Width + "x" + bmp.Height
                            + (screenGrab ? "  [screen]" : ""));
                    }
                }
                f.Close();
            }
        }

        // 无界面自检：验证采集链路与报告生成
        static void SelfTest(string outPath)
        {
            try { Console.OutputEncoding = Encoding.UTF8; } catch { }

            IntPtr h = Nat.GetConsoleWindow();
            if (h == IntPtr.Zero) h = Probe.HitTest(new Point(200, 200));

            Console.WriteLine("selftest: target hwnd = 0x" + h.ToInt64().ToString("X"));

            IntPtr deep = Probe.DeepestAt(h, new Point(200, 200));
            Console.WriteLine("selftest: deepest hwnd = 0x" + deep.ToInt64().ToString("X"));

            Report rep = Probe.Collect(deep == IntPtr.Zero ? h : deep, new Point(200, 200));
            string report = rep.ToText();
            Console.WriteLine(report);
            Console.WriteLine("selftest: report length = " + report.Length);

            if (outPath.Length > 0)
            {
                File.WriteAllText(outPath, report, new UTF8Encoding(true));
                Console.WriteLine("selftest: report written to " + outPath);
            }

            // 清理自身哈希测试文件
            Console.WriteLine("selftest: exe sha256 = " + rep.Sha256);
            Console.WriteLine("selftest: OK");
        }
    }

    #endregion
}

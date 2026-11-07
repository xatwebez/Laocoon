// Laocoön - 多语言支持 / localization
//
// 全部 12 种语言都在这一个文件里：上半部分是语言机制（枚举、识别、字体、宽度、取词），
// 下半部分从「简体中文」起是 12 张文案表，顺序为中 / 英 / 俄 / 法 / 西 / 阿 / 德 / 葡 / 日 / 韩 / 印 / 意。
// 加一种语言只需照最后一张表补一张，再在 LangId 与 All 里登记，别处不用改。
//
// 这 12 种是怎么定的：五常语言打底（中/英/俄/法/西），其余按"全球影响力"补齐——
// 阿拉伯语（联合国第六种工作语言）、德语 / 葡萄牙语 / 日语 / 韩语（主要经济体与软件生态）、
// 印地语（人口最大的单一语言市场）、意大利语（欧洲文化圈）。
//
// 所有文案都编译进 exe 本身，不生成 .resources / satellite assembly，保持"单文件、零依赖"。
// 选择优先级：命令行 --lang=xx  >  Laocoon.ini 的 lang=xx  >  系统界面语言  >  英语
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;

namespace Laocoon
{
    internal enum LangId
    {
        Zh = 0, En = 1, Ru = 2, Fr = 3, Es = 4,
        Ar = 5, De = 6, Pt = 7, Ja = 8, Ko = 9, Hi = 10, It = 11
    }

    internal static partial class L
    {
        public static readonly LangId[] All = new LangId[]
        {
            LangId.Zh, LangId.En, LangId.Ru, LangId.Fr, LangId.Es,
            LangId.Ar, LangId.De, LangId.Pt, LangId.Ja, LangId.Ko, LangId.Hi, LangId.It
        };

        static readonly Dictionary<string, string> _en = En();
        static Dictionary<string, string> _cur = _en;
        static LangId _id = LangId.En;

        // 报告里"标签 + 对齐"的列宽，按当前语言最长的标签自动算，避免俄语/法语把值挤歪
        public static int LabelCol = 12;

        public static LangId Current { get { return _id; } }

        // ---------------- 语言标识 ----------------

        public static string Code(LangId id)
        {
            switch (id)
            {
                case LangId.Zh: return "zh";
                case LangId.Ru: return "ru";
                case LangId.Fr: return "fr";
                case LangId.Es: return "es";
                case LangId.Ar: return "ar";
                case LangId.De: return "de";
                case LangId.Pt: return "pt";
                case LangId.Ja: return "ja";
                case LangId.Ko: return "ko";
                case LangId.Hi: return "hi";
                case LangId.It: return "it";
                default: return "en";
            }
        }

        // 语言名一律用该语言自己的写法，切换菜单里不需要翻译
        public static string Native(LangId id)
        {
            switch (id)
            {
                case LangId.Zh: return "中文";
                case LangId.Ru: return "Русский";
                case LangId.Fr: return "Français";
                case LangId.Es: return "Español";
                case LangId.Ar: return "العربية";
                case LangId.De: return "Deutsch";
                case LangId.Pt: return "Português";
                case LangId.Ja: return "日本語";
                case LangId.Ko: return "한국어";
                case LangId.Hi: return "हिन्दी";
                case LangId.It: return "Italiano";
                default: return "English";
            }
        }

        // 接受 zh / zh-CN / zh-Hans / ru-RU / es-419 / pt-BR 等写法；无法识别返回 false
        public static bool TryParse(string s, out LangId id)
        {
            id = LangId.En;
            if (s == null) return false;
            s = s.Trim().ToLowerInvariant();
            if (s.Length == 0) return false;
            int dash = s.IndexOf('-');
            string head = dash > 0 ? s.Substring(0, dash) : s;
            if (head.Length == 0) return false;

            switch (head)
            {
                case "zh": id = LangId.Zh; return true;
                case "en": id = LangId.En; return true;
                case "ru": id = LangId.Ru; return true;
                case "fr": id = LangId.Fr; return true;
                case "es": id = LangId.Es; return true;
                case "ar": id = LangId.Ar; return true;
                case "de": id = LangId.De; return true;
                case "pt": id = LangId.Pt; return true;
                case "ja": id = LangId.Ja; return true;
                case "ko": id = LangId.Ko; return true;
                case "hi": id = LangId.Hi; return true;
                case "it": id = LangId.It; return true;
                // 未支持的语言明确拒绝，而不是悄悄回落成别的语言
                case "iw": return false;   // 希伯来语
                case "in": return false;   // 印尼语
                case "no": return false;   // 挪威语
            }
            return false;
        }

        public static LangId Detect()
        {
            try
            {
                CultureInfo ci = CultureInfo.CurrentUICulture;
                if (ci != null)
                {
                    LangId id;
                    if (TryParse(ci.Name, out id)) return id;
                    if (TryParse(ci.TwoLetterISOLanguageName, out id)) return id;
                }
            }
            catch { }
            return LangId.En;
        }

        public static void Set(LangId id)
        {
            _id = id;
            _cur = Table(id);
            RecalcLabelCol();
        }

        public static void Set(string code)
        {
            LangId id;
            if (TryParse(code, out id)) Set(id);
        }

        // ---------------- 取词 ----------------

        public static string T(string key)
        {
            string v;
            if (_cur != null && _cur.TryGetValue(key, out v) && v != null && v.Length > 0) return v;
            if (_en.TryGetValue(key, out v) && v != null && v.Length > 0) return v;
            return key;
        }

        public static string F(string key, params object[] args)
        {
            try
            {
                // 阿拉伯语的格式化参数统一用 LRM 包住：像 "目标: {类名}  PID {2}" 这种行，
                // 裸的拉丁值夹在阿拉伯文里会被重排（日期最明显，会变成 04:28:37 08-11-2026）。
                // LRM 是零宽字符，不影响显示，只是告诉渲染器"这一段按从左到右算"。
                if (_id == LangId.Ar && args != null && args.Length > 0)
                {
                    object[] w = new object[args.Length];
                    for (int i = 0; i < args.Length; i++)
                        w[i] = args[i] == null ? null : (object)("\u200E" + args[i] + "\u200E");
                    return string.Format(T(key), w);
                }
                return string.Format(T(key), args);
            }
            catch { return T(key); }
        }

        // 界面字体：拉丁语系用 Segoe UI；中日韩各自挑系统自带字体；
        // 印地语要天城文字形（Nirmala UI 是 Win8+ 自带），阿拉伯语用 Segoe UI/Tahoma。
        // 这些都做了可用性检查，装不上的老系统会退回 Segoe UI 而不是抛异常。
        public static string UiFont
        {
            get
            {
                switch (_id)
                {
                    case LangId.Zh: return Pick("Microsoft YaHei UI", "Microsoft YaHei", "SimSun", "Segoe UI");
                    case LangId.Ja: return Pick("Yu Gothic UI", "Meiryo UI", "MS UI Gothic", "Segoe UI");
                    case LangId.Ko: return Pick("Malgun Gothic", "Gulim", "Batang", "Segoe UI");
                    case LangId.Hi: return Pick("Nirmala UI", "Mangal", "Arial Unicode MS", "Segoe UI");
                    case LangId.Ar: return Pick("Segoe UI", "Tahoma", "Arial");
                    default: return Pick("Segoe UI", "Tahoma", "Arial");
                }
            }
        }

        // 报告文本框字体。拉丁语系用 Consolas 保持等宽对齐；
        // 日文、韩文换成自带的等宽东亚字体，否则汉字/谚文会被回退字体挤成不齐的宽度。
        public static string MonoFont
        {
            get
            {
                switch (_id)
                {
                    case LangId.Ja: return Pick("MS Gothic", "Yu Gothic UI", "Consolas");
                    case LangId.Ko: return Pick("GulimChe", "Malgun Gothic", "Consolas");
                    case LangId.Zh: return Pick("Consolas", "Microsoft YaHei UI", "SimSun");
                    case LangId.Hi: return Pick("Nirmala UI", "Consolas");
                    case LangId.Ar: return Pick("Consolas", "Segoe UI");
                    default: return Pick("Consolas", "Courier New");
                }
            }
        }

        // 阿拉伯语是从右到左书写，界面布局要整体镜像
        public static bool IsRtl { get { return _id == LangId.Ar; } }

        // 阿拉伯文、天城文在等宽字体里没有固定字宽，"补空格对齐"只会对得更歪，
        // 这两种语言改成 "标签: 值" 的写法，宁可不对齐也不假装对齐。
        public static bool PadLabels
        {
            get { return _id != LangId.Ar && _id != LangId.Hi; }
        }

        static HashSet<string> _fontSet;

        static string Pick(params string[] candidates)
        {
            if (_fontSet == null)
            {
                _fontSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    foreach (FontFamily ff in FontFamily.Families) _fontSet.Add(ff.Name);
                }
                catch { }
            }
            foreach (string c in candidates) if (_fontSet.Contains(c)) return c;
            return candidates[candidates.Length - 1];
        }

        // 该语言下的字体是否真的存在（诊断用）
        public static bool FontAvailable(string name)
        {
            Pick("x");
            return _fontSet.Contains(name);
        }

        // 标签和值之间的分隔符。
        // 阿拉伯文这里必须夹 LRM(U+200E)：GDI 的 BiDi 会认为 "阿拉伯标签 + 冒号 + 拉丁值"
        // 里的冒号属于右侧的 RTL 段，把冒号扔到标签**左边**，渲染成 "0 :المقبضx0001048E"。
        // 实测见 --biditest 的 B 列，加 LRM 后五条样本全部正常。
        public static string LabelSep
        {
            get { return _id == LangId.Ar ? "\u200E:\u200E " : ": "; }
        }

        // 中日韩字符占两格，其余占一格。
        // 注意：不能简单用 c > 0x2E7F 判断——阿拉伯语的呈现形式(U+FE70~U+FEFF)
        // 和全角标点混在那条线之后，会把阿拉伯文标签算成两倍宽。这里改成显式区间。
        public static int DispWidth(string s)
        {
            if (s == null) return 0;
            int w = 0;
            foreach (char c in s) w += IsWide(c) ? 2 : 1;
            return w;
        }

        static bool IsWide(char c)
        {
            return (c >= 0x1100 && c <= 0x115F)     // 谚文字母
                || (c >= 0x2E80 && c <= 0x303E)     // CJK 部首 / 中日韩标点
                || (c >= 0x3041 && c <= 0x33FF)     // 假名、注音、CJK 兼容
                || (c >= 0x3400 && c <= 0x4DBF)     // CJK 扩展 A
                || (c >= 0x4E00 && c <= 0x9FFF)     // CJK 统一表意文字
                || (c >= 0xA000 && c <= 0xA4CF)     // 彝文
                || (c >= 0xAC00 && c <= 0xD7A3)     // 谚文音节
                || (c >= 0xF900 && c <= 0xFAFF)     // CJK 兼容表意
                || (c >= 0xFE30 && c <= 0xFE6F)     // CJK 兼容形式
                || (c >= 0xFF00 && c <= 0xFF60)     // 全角形式
                || (c >= 0xFFE0 && c <= 0xFFE6);    // 全角符号
        }

        static void RecalcLabelCol()
        {
            int max = 0;
            foreach (KeyValuePair<string, string> kv in _cur)
            {
                if (!kv.Key.StartsWith("lbl.", StringComparison.Ordinal)) continue;
                int w = DispWidth(kv.Value);
                if (w > max) { max = w; LongestLabel = kv.Value; }
            }
            LabelCol = max + 2;
            if (LabelCol < 12) LabelCol = 12;
        }

        // 诊断用：当前语言最长的标签及其宽度
        public static string LongestLabel = "";
        public static int LongestLabelWidth { get { return DispWidth(LongestLabel); } }

        // ---------------- 自检 ----------------

        // 返回某语言相对英语缺失的键（英语是基准，其它语言缺词时自动回落英语）
        public static List<string> Missing(LangId id)
        {
            List<string> miss = new List<string>();
            Dictionary<string, string> t = Table(id);
            foreach (string k in _en.Keys)
            {
                string v;
                if (!t.TryGetValue(k, out v) || v == null || v.Length == 0) miss.Add(k);
            }
            return miss;
        }

        public static int KeyCount { get { return _en.Count; } }

        static Dictionary<string, string> Table(LangId id)
        {
            switch (id)
            {
                case LangId.Zh: return Zh();
                case LangId.Ru: return Ru();
                case LangId.Fr: return Fr();
                case LangId.Es: return Es();
                case LangId.Ar: return Ar();
                case LangId.De: return De();
                case LangId.Pt: return Pt();
                case LangId.Ja: return Ja();
                case LangId.Ko: return Ko();
                case LangId.Hi: return Hi();
                case LangId.It: return It();
                default: return _en;
            }
        }

        static Dictionary<string, string> D(string[] kv)
        {
            Dictionary<string, string> d = new Dictionary<string, string>(kv.Length / 2 + 1, StringComparer.Ordinal);
            for (int i = 0; i + 1 < kv.Length; i += 2) d[kv[i]] = kv[i + 1];
            return d;
        }

        // ================= 简体中文 =================
        static Dictionary<string, string> Zh()
        {
            return D(new string[] {
                "app.brand", "Laocoön 拉奥孔",
                "app.title", "Laocoön 拉奥孔 · 窗口探针",

                "btn.aim", "◎  瞄准 (F2)",
                "btn.aim.cancel", "瞄准中… Esc 取消",
                "btn.copytext", "复制文本",
                "btn.save", "保存报告",
                "btn.openloc", "打开文件位置",
                "btn.clear", "清空",
                "st.lang", "语言",

                "st.ready", "就绪 — 按 F2 开始瞄准",
                "st.cleared", "已清空",
                "st.aimhint", "移动鼠标到目标窗口，左键锁定，右键 / Esc 取消",
                "st.target", "目标：{0}   \"{1}\"   PID {2}",
                "st.aimcancel", "已取消瞄准",
                "st.nohit", "没有捕获到窗口，请重试",
                "st.collecting", "正在采集…",
                "st.done", "采集完成：{0}   PID {1}   句柄 {2}",
                "st.fail", "采集失败：{0}",
                "st.copiedtext", "报告文本已复制",
                "st.copiedsuffix", "（{0} 字符）",
                "st.nocopy", "没有可复制的内容",
                "st.copyfail", "复制失败：{0}",
                "st.empty", "还没有采集到内容",
                "st.saved", "已保存：{0}",
                "st.savefail", "保存失败：{0}",
                "st.nopath", "没有可打开的文件路径",
                "st.located", "已在资源管理器中定位：{0}",
                "st.openfail", "打开失败：{0}",
                "st.nocapture", "请先采集一个窗口",
                "st.langchanged", "界面语言已切换为中文",

                "dlg.save.title", "保存采集报告",
                "dlg.filter", "文本报告 (*.txt)|*.txt",
                "msg.captureerr", "采集时出错：\r\n{0}",
                "hud.notitle", "(无标题)",
                "hud.parent", "父窗口  {0}",
                "hud.top", "顶层窗口",

                "common.yes", "是",
                "common.no", "否",
                "common.bytes", "字节",
                "common.session", "Session {0}",
                "common.elevated", "已提权 (管理员)",
                "common.normal", "普通用户",

                "rep.header", " Laocoön 拉奥孔 · 窗口探针   采集于 {0}",
                "rep.sec1", "----- [1] 目标窗口 -----",
                "rep.sec2", "----- [2] 所属进程 -----",
                "rep.sec3", "----- [3] 文件与指纹 -----",
                "rep.sec4", "----- [4] 数字签名 -----",
                "rep.sec5", "----- [5] 该进程的全部顶层窗口 ({0}) -----",
                "rep.sec6", "----- [6] 子控件树 ({0}) -----",
                "rep.sec7", "----- [7] 加载模块 ({0}) -----",
                "rep.secflags", "----- [!] 疑点提示（仅启发式判断，非结论） -----",
                "rep.trunc", "（已截断）",
                "rep.none", "  (无)",
                "rep.nosig", "  未发现内嵌数字签名（系统自带文件常为目录签名，此处不检测）",
                "rep.sigvalid", "有效",
                "rep.siginvalid", "校验未通过",
                "rep.visible", "  [可见]  ",
                "rep.hidden", "  [隐藏]  ",
                "rep.istarget", "   <= 本次目标",
                "rep.nochild", "  (无子窗口)",
                "rep.client", "客户区 {0} x {1}",
                "val.product", "   (产品 {0})",

                "lbl.handle", "句柄",
                "lbl.class", "类名",
                "lbl.title", "标题",
                "lbl.hitctl", "命中控件",
                "lbl.ctltext", "控件文本",
                "lbl.hitpath", "命中层级",
                "lbl.pos", "位置尺寸",
                "lbl.cursor", "鼠标坐标",
                "lbl.thread", "所属线程",
                "lbl.monitor", "显示器",
                "lbl.state", "状态",
                "lbl.style", "样式",
                "lbl.exstyle", "扩展样式",
                "lbl.owner", "属主窗口",
                "lbl.pid", "PID",
                "lbl.pname", "进程名",
                "lbl.ppath", "完整路径",
                "lbl.cmd", "命令行",
                "lbl.parent", "父进程",
                "lbl.arch", "架构",
                "lbl.elev", "权限",
                "lbl.session", "会话 / 启动",
                "lbl.company", "公司",
                "lbl.product", "产品",
                "lbl.desc", "描述",
                "lbl.fver", "文件版本",
                "lbl.copyright", "版权",
                "lbl.fsize", "文件大小",
                "lbl.fcreated", "创建时间",
                "lbl.fmodified", "修改时间",
                "lbl.md5", "MD5",
                "lbl.sha1", "SHA1",
                "lbl.sha256", "SHA256",
                "lbl.note", "备注",
                "lbl.sigstatus", "状态",
                "lbl.signer", "签名者",
                "lbl.issuer", "颁发者",
                "lbl.expires", "有效期至",
                "lbl.chain", "链状态",

                "fmt.state", "可见={0}  启用={1}  最小化={2}  DWM隐身={3}",

                "note.modtrunc", "模块过多，仅显示前 400 个（共 {0}）",
                "note.moddenied", "无法枚举模块（权限不足）",
                "note.moddenied2", "无法枚举模块（需要更高权限，可右键以管理员身份运行）",
                "note.bighash", "文件超过 512MB，已跳过哈希计算",
                "note.hashfail", "哈希计算失败：{0}",
                "note.sigerr", "校验异常：{0}",

                "flag.temp", "可执行文件位于临时目录：{0}",
                "flag.downloads", "可执行文件位于「下载」目录",
                "flag.appdata", "可执行文件位于 AppData 用户数据目录",
                "flag.nosig", "未发现内嵌数字签名",
                "flag.badsig", "数字签名校验未通过",
                "flag.signer", "签名者：{0}",
                "flag.nocompany", "文件版本信息中没有公司名",
                "flag.topmost", "窗口置顶 (WS_EX_TOPMOST)",
                "flag.toolwin", "工具窗口 (WS_EX_TOOLWINDOW)，通常不出现在任务栏/Alt+Tab",
                "flag.layered", "分层窗口 (WS_EX_LAYERED)，常用于悬浮/透明界面",
                "flag.invisible", "目标窗口处于不可见状态",
                "flag.cloaked", "窗口被 DWM 隐身（Cloaked），不可见但仍在运行",
                "flag.notitle", "目标窗口没有标题",
                "flag.hidden", "该进程另有 {0} 个隐藏的顶层窗口",
                "flag.recent", "文件创建于最近 7 天内（{0}）",

                "welcome",
                    "Laocoön 拉奥孔 · 窗口探针\n" +
                    "----------------------------------------------------------------\n" +
                    "特洛伊城下，所有人都想把那匹木马拖进城，只有拉奥孔不信。\n" +
                    "他一矛捅进马腹 —— 里面传出兵器相撞的金属声。\n" +
                    "这个工具做的事一模一样：戳一下窗口，听里面藏着什么。\n" +
                    "----------------------------------------------------------------\n\n" +
                    "1. 按 F2（或点击左上角「瞄准」）\n" +
                    "2. 鼠标会变成十字靶，移动到可疑窗口上 —— 会实时显示它的类名、标题\n" +
                    "3. 左键锁定，窗口里直接列出该窗口的全部信息\n" +
                    "4. 右键或 Esc 取消瞄准\n\n" +
                    "采到的内容包括：\n" +
                    "  · 窗口：类名、标题、样式位、矩形、置顶/隐身/分层状态\n" +
                    "  · 进程：完整路径、命令行、父进程、架构、是否提权、启动时间\n" +
                    "  · 文件：大小、时间、MD5 / SHA1 / SHA256、数字签名\n" +
                    "  · 进程加载的全部 DLL\n" +
                    "  · 该进程的其它顶层窗口（含隐藏窗口）\n" +
                    "  · 目标窗口的子控件树（含控件文本）\n\n" +
                    "提示：右键以管理员身份运行，才能读取到几乎所有进程的路径与模块。\n"
            });
        }

        // ================= English =================
        static Dictionary<string, string> En()
        {
            return D(new string[] {
                "app.brand", "Laocoön",
                "app.title", "Laocoön · Window Probe",

                "btn.aim", "◎  Aim (F2)",
                "btn.aim.cancel", "Aiming… Esc to cancel",
                "btn.copytext", "Copy text",
                "btn.save", "Save report",
                "btn.openloc", "Open file location",
                "btn.clear", "Clear",
                "st.lang", "Language",

                "st.ready", "Ready — press F2 to aim",
                "st.cleared", "Cleared",
                "st.aimhint", "Move the cursor over the target window; left-click to lock, right-click / Esc to cancel",
                "st.target", "Target: {0}   \"{1}\"   PID {2}",
                "st.aimcancel", "Aiming cancelled",
                "st.nohit", "No window captured, please try again",
                "st.collecting", "Collecting…",
                "st.done", "Done: {0}   PID {1}   handle {2}",
                "st.fail", "Collection failed: {0}",
                "st.copiedtext", "Report text copied",
                "st.copiedsuffix", " ({0} chars)",
                "st.nocopy", "Nothing to copy",
                "st.copyfail", "Copy failed: {0}",
                "st.empty", "Nothing collected yet",
                "st.saved", "Saved: {0}",
                "st.savefail", "Save failed: {0}",
                "st.nopath", "No file path to open",
                "st.located", "Located in Explorer: {0}",
                "st.openfail", "Open failed: {0}",
                "st.nocapture", "Capture a window first",
                "st.langchanged", "Interface language set to English",

                "dlg.save.title", "Save capture report",
                "dlg.filter", "Text report (*.txt)|*.txt",
                "msg.captureerr", "Error while capturing:\r\n{0}",
                "hud.notitle", "(no title)",
                "hud.parent", "Parent  {0}",
                "hud.top", "Top-level window",

                "common.yes", "yes",
                "common.no", "no",
                "common.bytes", "bytes",
                "common.session", "Session {0}",
                "common.elevated", "Elevated (admin)",
                "common.normal", "Standard user",

                "rep.header", " Laocoön · Window Probe   captured at {0}",
                "rep.sec1", "----- [1] Target window -----",
                "rep.sec2", "----- [2] Owning process -----",
                "rep.sec3", "----- [3] File and fingerprints -----",
                "rep.sec4", "----- [4] Digital signature -----",
                "rep.sec5", "----- [5] All top-level windows of this process ({0}) -----",
                "rep.sec6", "----- [6] Child control tree ({0}) -----",
                "rep.sec7", "----- [7] Loaded modules ({0}) -----",
                "rep.secflags", "----- [!] Heuristic hints (suspicions, not conclusions) -----",
                "rep.trunc", " (truncated)",
                "rep.none", "  (none)",
                "rep.nosig", "  No embedded digital signature (Windows' own files use catalog signing, not checked here)",
                "rep.sigvalid", "valid",
                "rep.siginvalid", "verification failed",
                "rep.visible", "  [shown]   ",
                "rep.hidden", "  [hidden]  ",
                "rep.istarget", "   <= this capture",
                "rep.nochild", "  (no child windows)",
                "rep.client", "client {0} x {1}",
                "val.product", "   (product {0})",

                "lbl.handle", "Handle",
                "lbl.class", "Window class",
                "lbl.title", "Title",
                "lbl.hitctl", "Control under cursor",
                "lbl.ctltext", "Control text",
                "lbl.hitpath", "Control path",
                "lbl.pos", "Position & size",
                "lbl.cursor", "Cursor position",
                "lbl.thread", "Thread",
                "lbl.monitor", "Monitor",
                "lbl.state", "State",
                "lbl.style", "Style",
                "lbl.exstyle", "Extended style",
                "lbl.owner", "Owner window",
                "lbl.pid", "PID",
                "lbl.pname", "Process name",
                "lbl.ppath", "Full path",
                "lbl.cmd", "Command line",
                "lbl.parent", "Parent process",
                "lbl.arch", "Architecture",
                "lbl.elev", "Privileges",
                "lbl.session", "Session / started",
                "lbl.company", "Company",
                "lbl.product", "Product",
                "lbl.desc", "Description",
                "lbl.fver", "File version",
                "lbl.copyright", "Copyright",
                "lbl.fsize", "File size",
                "lbl.fcreated", "Created",
                "lbl.fmodified", "Modified",
                "lbl.md5", "MD5",
                "lbl.sha1", "SHA1",
                "lbl.sha256", "SHA256",
                "lbl.note", "Note",
                "lbl.sigstatus", "Status",
                "lbl.signer", "Signer",
                "lbl.issuer", "Issuer",
                "lbl.expires", "Valid until",
                "lbl.chain", "Chain status",

                "fmt.state", "visible={0}  enabled={1}  minimized={2}  DWM-cloaked={3}",

                "note.modtrunc", "Too many modules; showing the first 400 of {0}",
                "note.moddenied", "Cannot enumerate modules (access denied)",
                "note.moddenied2", "Cannot enumerate modules (higher privileges required — try running as administrator)",
                "note.bighash", "File exceeds 512 MB; hash skipped",
                "note.hashfail", "Hash computation failed: {0}",
                "note.sigerr", "Verification error: {0}",

                "flag.temp", "Executable resides in a temporary folder: {0}",
                "flag.downloads", "Executable resides in the Downloads folder",
                "flag.appdata", "Executable resides in the AppData user folder",
                "flag.nosig", "No embedded digital signature found",
                "flag.badsig", "Digital signature verification failed",
                "flag.signer", "Signer: {0}",
                "flag.nocompany", "No company name in the file version info",
                "flag.topmost", "Always-on-top window (WS_EX_TOPMOST)",
                "flag.toolwin", "Tool window (WS_EX_TOOLWINDOW) — usually hidden from taskbar / Alt+Tab",
                "flag.layered", "Layered window (WS_EX_LAYERED) — often used for overlays and transparency",
                "flag.invisible", "Target window is currently invisible",
                "flag.cloaked", "Window is DWM-cloaked: invisible but still running",
                "flag.notitle", "Target window has no title",
                "flag.hidden", "The process has {0} more hidden top-level window(s)",
                "flag.recent", "File was created within the last 7 days ({0})",

                "welcome",
                    "Laocoön · Window Probe\n" +
                    "----------------------------------------------------------------\n" +
                    "Outside Troy everyone wanted to haul that wooden horse inside. Only Laocoön refused.\n" +
                    "He drove his spear into its belly — and heard metal clashing within.\n" +
                    "This tool does the same: poke a window, and hear what it is hiding.\n" +
                    "----------------------------------------------------------------\n\n" +
                    "1. Press F2 (or click \"Aim\" in the top-left corner)\n" +
                    "2. The cursor becomes a crosshair — move it over a suspicious window; its class and title appear live\n" +
                    "3. Left-click to lock on; the full report appears right away\n" +
                    "4. Right-click or Esc cancels aiming\n\n" +
                    "What is collected:\n" +
                    "  · Window: class, title, style bits, rectangle, topmost / cloaked / layered state\n" +
                    "  · Process: full path, command line, parent process, architecture, elevation, start time\n" +
                    "  · File: size, timestamps, MD5 / SHA1 / SHA256, digital signature\n" +
                    "  · Every DLL the process has loaded\n" +
                    "  · All other top-level windows of that process (hidden ones included)\n" +
                    "  · The child control tree of the target window (with control text)\n\n" +
                    "Tip: run as administrator (right-click) to read the path and modules of almost any process.\n"
            });
        }

        // ================= Русский =================
        static Dictionary<string, string> Ru()
        {
            return D(new string[] {
                "app.brand", "Laocoön",
                "app.title", "Laocoön · Зонд окон",

                "btn.aim", "◎  Прицел (F2)",
                "btn.aim.cancel", "Прицеливание… Esc — отмена",
                "btn.copytext", "Копировать текст",
                "btn.save", "Сохранить отчёт",
                "btn.openloc", "Открыть папку файла",
                "btn.clear", "Очистить",
                "st.lang", "Язык",

                "st.ready", "Готово — нажмите F2, чтобы прицелиться",
                "st.cleared", "Очищено",
                "st.aimhint", "Наведите курсор на нужное окно; левый клик — захват, правый клик / Esc — отмена",
                "st.target", "Цель: {0}   \"{1}\"   PID {2}",
                "st.aimcancel", "Прицеливание отменено",
                "st.nohit", "Окно не захвачено, попробуйте ещё раз",
                "st.collecting", "Сбор данных…",
                "st.done", "Готово: {0}   PID {1}   дескриптор {2}",
                "st.fail", "Ошибка сбора данных: {0}",
                "st.copiedtext", "Текст отчёта скопирован",
                "st.copiedsuffix", " ({0} симв.)",
                "st.nocopy", "Нечего копировать",
                "st.copyfail", "Ошибка копирования: {0}",
                "st.empty", "Данных пока нет",
                "st.saved", "Сохранено: {0}",
                "st.savefail", "Ошибка сохранения: {0}",
                "st.nopath", "Нет пути к файлу",
                "st.located", "Показано в проводнике: {0}",
                "st.openfail", "Не удалось открыть: {0}",
                "st.nocapture", "Сначала захватите окно",
                "st.langchanged", "Язык интерфейса: русский",

                "dlg.save.title", "Сохранить отчёт",
                "dlg.filter", "Текстовый отчёт (*.txt)|*.txt",
                "msg.captureerr", "Ошибка при сборе данных:\r\n{0}",
                "hud.notitle", "(без заголовка)",
                "hud.parent", "Родитель  {0}",
                "hud.top", "Окно верхнего уровня",

                "common.yes", "да",
                "common.no", "нет",
                "common.bytes", "байт",
                "common.session", "Сеанс {0}",
                "common.elevated", "Права повышены (администратор)",
                "common.normal", "Обычный пользователь",

                "rep.header", " Laocoön · Зонд окон   снято в {0}",
                "rep.sec1", "----- [1] Целевое окно -----",
                "rep.sec2", "----- [2] Процесс-владелец -----",
                "rep.sec3", "----- [3] Файл и отпечатки -----",
                "rep.sec4", "----- [4] Цифровая подпись -----",
                "rep.sec5", "----- [5] Все окна верхнего уровня процесса ({0}) -----",
                "rep.sec6", "----- [6] Дерево дочерних элементов ({0}) -----",
                "rep.sec7", "----- [7] Загруженные модули ({0}) -----",
                "rep.secflags", "----- [!] Подозрительные признаки (эвристика, не приговор) -----",
                "rep.trunc", " (обрезано)",
                "rep.none", "  (нет)",
                "rep.nosig", "  Встроенная цифровая подпись не найдена (системные файлы подписаны каталогом — здесь не проверяется)",
                "rep.sigvalid", "действительна",
                "rep.siginvalid", "проверка не пройдена",
                "rep.visible", "  [видно]   ",
                "rep.hidden", "  [скрыто]  ",
                "rep.istarget", "   <= этот захват",
                "rep.nochild", "  (дочерних окон нет)",
                "rep.client", "клиентская область {0} x {1}",
                "val.product", "   (продукт {0})",

                "lbl.handle", "Дескриптор",
                "lbl.class", "Класс окна",
                "lbl.title", "Заголовок",
                "lbl.hitctl", "Элемент под курсором",
                "lbl.ctltext", "Текст элемента",
                "lbl.hitpath", "Путь до элемента",
                "lbl.pos", "Положение и размер",
                "lbl.cursor", "Координаты курсора",
                "lbl.thread", "Поток",
                "lbl.monitor", "Монитор",
                "lbl.state", "Состояние",
                "lbl.style", "Стиль",
                "lbl.exstyle", "Расширенный стиль",
                "lbl.owner", "Окно-владелец",
                "lbl.pid", "PID",
                "lbl.pname", "Имя процесса",
                "lbl.ppath", "Полный путь",
                "lbl.cmd", "Командная строка",
                "lbl.parent", "Родительский процесс",
                "lbl.arch", "Архитектура",
                "lbl.elev", "Права",
                "lbl.session", "Сеанс / запуск",
                "lbl.company", "Компания",
                "lbl.product", "Продукт",
                "lbl.desc", "Описание",
                "lbl.fver", "Версия файла",
                "lbl.copyright", "Авторские права",
                "lbl.fsize", "Размер файла",
                "lbl.fcreated", "Создан",
                "lbl.fmodified", "Изменён",
                "lbl.md5", "MD5",
                "lbl.sha1", "SHA1",
                "lbl.sha256", "SHA256",
                "lbl.note", "Примечание",
                "lbl.sigstatus", "Статус",
                "lbl.signer", "Подписант",
                "lbl.issuer", "Издатель",
                "lbl.expires", "Действительна до",
                "lbl.chain", "Состояние цепочки",

                "fmt.state", "видимо={0}  активно={1}  свёрнуто={2}  скрыто DWM={3}",

                "note.modtrunc", "Слишком много модулей — показаны первые 400 из {0}",
                "note.moddenied", "Не удалось перечислить модули (нет прав)",
                "note.moddenied2", "Не удалось перечислить модули (нужны права выше — запустите от администратора)",
                "note.bighash", "Файл больше 512 МБ — хеш не вычислялся",
                "note.hashfail", "Ошибка вычисления хеша: {0}",
                "note.sigerr", "Ошибка проверки: {0}",

                "flag.temp", "Исполняемый файл лежит во временной папке: {0}",
                "flag.downloads", "Исполняемый файл лежит в папке «Загрузки»",
                "flag.appdata", "Исполняемый файл лежит в пользовательской папке AppData",
                "flag.nosig", "Встроенная цифровая подпись не найдена",
                "flag.badsig", "Проверка цифровой подписи не пройдена",
                "flag.signer", "Подписант: {0}",
                "flag.nocompany", "В сведениях о файле нет названия компании",
                "flag.topmost", "Окно поверх всех (WS_EX_TOPMOST)",
                "flag.toolwin", "Окно-инструмент (WS_EX_TOOLWINDOW) — обычно не видно на панели задач / в Alt+Tab",
                "flag.layered", "Слоистое окно (WS_EX_LAYERED) — часто используется для оверлеев и прозрачности",
                "flag.invisible", "Целевое окно сейчас невидимо",
                "flag.cloaked", "Окно скрыто средствами DWM (Cloaked): невидимо, но работает",
                "flag.notitle", "У целевого окна нет заголовка",
                "flag.hidden", "У процесса есть ещё скрытых окон верхнего уровня: {0}",
                "flag.recent", "Файл создан за последние 7 дней ({0})",

                "welcome",
                    "Laocoön · Зонд окон\n" +
                    "----------------------------------------------------------------\n" +
                    "Под Троей все хотели втащить этого деревянного коня в город. Не поверил только Лаокоон.\n" +
                    "Он вонзил копьё ему в брюхо — и услышал внутри лязг оружия.\n" +
                    "Этот инструмент делает то же самое: ткни в окно и услышь, что в нём спрятано.\n" +
                    "----------------------------------------------------------------\n\n" +
                    "1. Нажмите F2 (или кнопку «Прицел» слева вверху)\n" +
                    "2. Курсор станет перекрестием — наведите его на подозрительное окно: класс и заголовок видны сразу\n" +
                    "3. Левый клик — захват; полный отчёт появится сразу\n" +
                    "4. Правый клик или Esc отменяет прицеливание\n\n" +
                    "Что собирается:\n" +
                    "  · Окно: класс, заголовок, биты стиля, прямоугольник, поверх всех / скрыто DWM / слоистое\n" +
                    "  · Процесс: полный путь, командная строка, родительский процесс, архитектура, права, время запуска\n" +
                    "  · Файл: размер, даты, MD5 / SHA1 / SHA256, цифровая подпись\n" +
                    "  · Все DLL, загруженные процессом\n" +
                    "  · Остальные окна верхнего уровня этого процесса (включая скрытые)\n" +
                    "  · Дерево дочерних элементов целевого окна (с их текстом)\n\n" +
                    "Совет: запустите от имени администратора, чтобы читать путь и модули почти любого процесса.\n"
            });
        }

        // ================= Français =================
        static Dictionary<string, string> Fr()
        {
            return D(new string[] {
                "app.brand", "Laocoön",
                "app.title", "Laocoön · Sonde de fenêtres",

                "btn.aim", "◎  Viser (F2)",
                "btn.aim.cancel", "Visée… Échap pour annuler",
                "btn.copytext", "Copier le texte",
                "btn.save", "Enregistrer le rapport",
                "btn.openloc", "Ouvrir l'emplacement",
                "btn.clear", "Effacer",
                "st.lang", "Langue",

                "st.ready", "Prêt — appuyez sur F2 pour viser",
                "st.cleared", "Effacé",
                "st.aimhint", "Placez le curseur sur la fenêtre cible ; clic gauche pour verrouiller, clic droit / Échap pour annuler",
                "st.target", "Cible : {0}   \"{1}\"   PID {2}",
                "st.aimcancel", "Visée annulée",
                "st.nohit", "Aucune fenêtre capturée, réessayez",
                "st.collecting", "Collecte…",
                "st.done", "Terminé : {0}   PID {1}   handle {2}",
                "st.fail", "Échec de la collecte : {0}",
                "st.copiedtext", "Texte du rapport copié",
                "st.copiedsuffix", " ({0} car.)",
                "st.nocopy", "Rien à copier",
                "st.copyfail", "Échec de la copie : {0}",
                "st.empty", "Aucune donnée collectée pour l'instant",
                "st.saved", "Enregistré : {0}",
                "st.savefail", "Échec de l'enregistrement : {0}",
                "st.nopath", "Aucun chemin de fichier à ouvrir",
                "st.located", "Localisé dans l'Explorateur : {0}",
                "st.openfail", "Échec de l'ouverture : {0}",
                "st.nocapture", "Capturez d'abord une fenêtre",
                "st.langchanged", "Langue de l'interface : français",

                "dlg.save.title", "Enregistrer le rapport",
                "dlg.filter", "Rapport texte (*.txt)|*.txt",
                "msg.captureerr", "Erreur pendant la capture :\r\n{0}",
                "hud.notitle", "(sans titre)",
                "hud.parent", "Parent  {0}",
                "hud.top", "Fenêtre de premier niveau",

                "common.yes", "oui",
                "common.no", "non",
                "common.bytes", "octets",
                "common.session", "Session {0}",
                "common.elevated", "Élevé (administrateur)",
                "common.normal", "Utilisateur standard",

                "rep.header", " Laocoön · Sonde de fenêtres   capture du {0}",
                "rep.sec1", "----- [1] Fenêtre cible -----",
                "rep.sec2", "----- [2] Processus propriétaire -----",
                "rep.sec3", "----- [3] Fichier et empreintes -----",
                "rep.sec4", "----- [4] Signature numérique -----",
                "rep.sec5", "----- [5] Toutes les fenêtres de premier niveau du processus ({0}) -----",
                "rep.sec6", "----- [6] Arbre des contrôles enfants ({0}) -----",
                "rep.sec7", "----- [7] Modules chargés ({0}) -----",
                "rep.secflags", "----- [!] Indices suspects (heuristique, pas une conclusion) -----",
                "rep.trunc", " (tronqué)",
                "rep.none", "  (aucun)",
                "rep.nosig", "  Aucune signature numérique intégrée (les fichiers système utilisent une signature de catalogue, non vérifiée ici)",
                "rep.sigvalid", "valide",
                "rep.siginvalid", "vérification échouée",
                "rep.visible", "  [visible]  ",
                "rep.hidden", "  [cachée]   ",
                "rep.istarget", "   <= cette capture",
                "rep.nochild", "  (aucune fenêtre enfant)",
                "rep.client", "client {0} x {1}",
                "val.product", "   (produit {0})",

                "lbl.handle", "Handle",
                "lbl.class", "Classe",
                "lbl.title", "Titre",
                "lbl.hitctl", "Contrôle ciblé",
                "lbl.ctltext", "Texte du contrôle",
                "lbl.hitpath", "Chemin du contrôle",
                "lbl.pos", "Position / taille",
                "lbl.cursor", "Coordonnées souris",
                "lbl.thread", "Thread",
                "lbl.monitor", "Moniteur",
                "lbl.state", "État",
                "lbl.style", "Style",
                "lbl.exstyle", "Style étendu",
                "lbl.owner", "Fenêtre propriétaire",
                "lbl.pid", "PID",
                "lbl.pname", "Nom du processus",
                "lbl.ppath", "Chemin complet",
                "lbl.cmd", "Ligne de commande",
                "lbl.parent", "Processus parent",
                "lbl.arch", "Architecture",
                "lbl.elev", "Privilèges",
                "lbl.session", "Session / démarrage",
                "lbl.company", "Société",
                "lbl.product", "Produit",
                "lbl.desc", "Description",
                "lbl.fver", "Version du fichier",
                "lbl.copyright", "Copyright",
                "lbl.fsize", "Taille du fichier",
                "lbl.fcreated", "Créé le",
                "lbl.fmodified", "Modifié le",
                "lbl.md5", "MD5",
                "lbl.sha1", "SHA1",
                "lbl.sha256", "SHA256",
                "lbl.note", "Remarque",
                "lbl.sigstatus", "État",
                "lbl.signer", "Signataire",
                "lbl.issuer", "Émetteur",
                "lbl.expires", "Valide jusqu'au",
                "lbl.chain", "État de la chaîne",

                "fmt.state", "visible={0}  actif={1}  réduit={2}  masqué DWM={3}",

                "note.modtrunc", "Trop de modules ; seuls les 400 premiers sur {0} sont affichés",
                "note.moddenied", "Impossible d'énumérer les modules (accès refusé)",
                "note.moddenied2", "Impossible d'énumérer les modules (privilèges insuffisants — lancez en administrateur)",
                "note.bighash", "Fichier de plus de 512 Mo ; calcul du hachage ignoré",
                "note.hashfail", "Échec du calcul du hachage : {0}",
                "note.sigerr", "Erreur de vérification : {0}",

                "flag.temp", "L'exécutable se trouve dans un dossier temporaire : {0}",
                "flag.downloads", "L'exécutable se trouve dans le dossier « Téléchargements »",
                "flag.appdata", "L'exécutable se trouve dans le dossier utilisateur AppData",
                "flag.nosig", "Aucune signature numérique intégrée trouvée",
                "flag.badsig", "La vérification de la signature numérique a échoué",
                "flag.signer", "Signataire : {0}",
                "flag.nocompany", "Aucun nom de société dans les informations de version",
                "flag.topmost", "Fenêtre toujours au premier plan (WS_EX_TOPMOST)",
                "flag.toolwin", "Fenêtre d'outil (WS_EX_TOOLWINDOW) — absente de la barre des tâches / d'Alt+Tab",
                "flag.layered", "Fenêtre superposée (WS_EX_LAYERED) — souvent utilisée pour les overlays transparents",
                "flag.invisible", "La fenêtre cible est actuellement invisible",
                "flag.cloaked", "Fenêtre masquée par DWM (Cloaked) : invisible mais toujours active",
                "flag.notitle", "La fenêtre cible n'a pas de titre",
                "flag.hidden", "Le processus possède {0} autre(s) fenêtre(s) de premier niveau cachée(s)",
                "flag.recent", "Fichier créé dans les 7 derniers jours ({0})",

                "welcome",
                    "Laocoön · Sonde de fenêtres\n" +
                    "----------------------------------------------------------------\n" +
                    "Devant Troie, tous voulaient faire entrer ce cheval de bois dans la ville. Seul Laocoon refusa.\n" +
                    "Il enfonça sa lance dans son flanc — et entendit des armes s'entrechoquer à l'intérieur.\n" +
                    "Cet outil fait exactement cela : piquer une fenêtre pour entendre ce qu'elle cache.\n" +
                    "----------------------------------------------------------------\n\n" +
                    "1. Appuyez sur F2 (ou cliquez sur « Viser » en haut à gauche)\n" +
                    "2. Le curseur devient une croix — déplacez-la sur une fenêtre suspecte : classe et titre s'affichent en direct\n" +
                    "3. Clic gauche pour verrouiller ; le rapport complet s'affiche aussitôt\n" +
                    "4. Clic droit ou Échap annule la visée\n\n" +
                    "Ce qui est collecté :\n" +
                    "  · Fenêtre : classe, titre, bits de style, rectangle, toujours au premier plan / masquée DWM / superposée\n" +
                    "  · Processus : chemin complet, ligne de commande, processus parent, architecture, élévation, heure de démarrage\n" +
                    "  · Fichier : taille, dates, MD5 / SHA1 / SHA256, signature numérique\n" +
                    "  · Toutes les DLL chargées par le processus\n" +
                    "  · Les autres fenêtres de premier niveau de ce processus (y compris cachées)\n" +
                    "  · L'arbre des contrôles enfants de la fenêtre cible (avec leur texte)\n\n" +
                    "Astuce : lancez en administrateur pour lire le chemin et les modules de presque tous les processus.\n"
            });
        }

        // ================= Español =================
        static Dictionary<string, string> Es()
        {
            return D(new string[] {
                "app.brand", "Laocoön",
                "app.title", "Laocoön · Sonda de ventanas",

                "btn.aim", "◎  Apuntar (F2)",
                "btn.aim.cancel", "Apuntando… Esc para cancelar",
                "btn.copytext", "Copiar texto",
                "btn.save", "Guardar informe",
                "btn.openloc", "Abrir ubicación",
                "btn.clear", "Limpiar",
                "st.lang", "Idioma",

                "st.ready", "Listo — pulsa F2 para apuntar",
                "st.cleared", "Borrado",
                "st.aimhint", "Mueve el cursor sobre la ventana objetivo; clic izquierdo para fijar, clic derecho / Esc para cancelar",
                "st.target", "Objetivo: {0}   \"{1}\"   PID {2}",
                "st.aimcancel", "Puntería cancelada",
                "st.nohit", "No se capturó ninguna ventana, inténtalo de nuevo",
                "st.collecting", "Recopilando…",
                "st.done", "Listo: {0}   PID {1}   handle {2}",
                "st.fail", "Error al recopilar: {0}",
                "st.copiedtext", "Texto del informe copiado",
                "st.copiedsuffix", " ({0} car.)",
                "st.nocopy", "No hay nada que copiar",
                "st.copyfail", "Error al copiar: {0}",
                "st.empty", "Aún no hay datos recopilados",
                "st.saved", "Guardado: {0}",
                "st.savefail", "Error al guardar: {0}",
                "st.nopath", "No hay ninguna ruta de archivo",
                "st.located", "Mostrado en el Explorador: {0}",
                "st.openfail", "Error al abrir: {0}",
                "st.nocapture", "Captura una ventana primero",
                "st.langchanged", "Idioma de la interfaz: español",

                "dlg.save.title", "Guardar informe",
                "dlg.filter", "Informe de texto (*.txt)|*.txt",
                "msg.captureerr", "Error durante la captura:\r\n{0}",
                "hud.notitle", "(sin título)",
                "hud.parent", "Padre  {0}",
                "hud.top", "Ventana de nivel superior",

                "common.yes", "sí",
                "common.no", "no",
                "common.bytes", "bytes",
                "common.session", "Sesión {0}",
                "common.elevated", "Elevado (administrador)",
                "common.normal", "Usuario estándar",

                "rep.header", " Laocoön · Sonda de ventanas   capturado el {0}",
                "rep.sec1", "----- [1] Ventana objetivo -----",
                "rep.sec2", "----- [2] Proceso propietario -----",
                "rep.sec3", "----- [3] Archivo y huellas -----",
                "rep.sec4", "----- [4] Firma digital -----",
                "rep.sec5", "----- [5] Todas las ventanas de nivel superior del proceso ({0}) -----",
                "rep.sec6", "----- [6] Árbol de controles hijos ({0}) -----",
                "rep.sec7", "----- [7] Módulos cargados ({0}) -----",
                "rep.secflags", "----- [!] Indicadores sospechosos (heurística, no conclusiones) -----",
                "rep.trunc", " (truncado)",
                "rep.none", "  (ninguno)",
                "rep.nosig", "  No se encontró firma digital incrustada (los archivos del sistema usan firma de catálogo, no comprobada aquí)",
                "rep.sigvalid", "válida",
                "rep.siginvalid", "verificación fallida",
                "rep.visible", "  [visible]  ",
                "rep.hidden", "  [oculta]   ",
                "rep.istarget", "   <= esta captura",
                "rep.nochild", "  (sin ventanas hijas)",
                "rep.client", "cliente {0} x {1}",
                "val.product", "   (producto {0})",

                "lbl.handle", "Handle",
                "lbl.class", "Clase",
                "lbl.title", "Título",
                "lbl.hitctl", "Control señalado",
                "lbl.ctltext", "Texto del control",
                "lbl.hitpath", "Ruta del control",
                "lbl.pos", "Posición y tamaño",
                "lbl.cursor", "Coordenadas del ratón",
                "lbl.thread", "Hilo",
                "lbl.monitor", "Monitor",
                "lbl.state", "Estado",
                "lbl.style", "Estilo",
                "lbl.exstyle", "Estilo extendido",
                "lbl.owner", "Ventana propietaria",
                "lbl.pid", "PID",
                "lbl.pname", "Nombre del proceso",
                "lbl.ppath", "Ruta completa",
                "lbl.cmd", "Línea de comandos",
                "lbl.parent", "Proceso padre",
                "lbl.arch", "Arquitectura",
                "lbl.elev", "Privilegios",
                "lbl.session", "Sesión / inicio",
                "lbl.company", "Compañía",
                "lbl.product", "Producto",
                "lbl.desc", "Descripción",
                "lbl.fver", "Versión del archivo",
                "lbl.copyright", "Copyright",
                "lbl.fsize", "Tamaño del archivo",
                "lbl.fcreated", "Creado",
                "lbl.fmodified", "Modificado",
                "lbl.md5", "MD5",
                "lbl.sha1", "SHA1",
                "lbl.sha256", "SHA256",
                "lbl.note", "Nota",
                "lbl.sigstatus", "Estado",
                "lbl.signer", "Firmante",
                "lbl.issuer", "Emisor",
                "lbl.expires", "Válido hasta",
                "lbl.chain", "Estado de la cadena",

                "fmt.state", "visible={0}  activo={1}  minimizado={2}  oculto DWM={3}",

                "note.modtrunc", "Demasiados módulos; se muestran los primeros 400 de {0}",
                "note.moddenied", "No se pueden enumerar los módulos (acceso denegado)",
                "note.moddenied2", "No se pueden enumerar los módulos (se necesitan más privilegios: ejecuta como administrador)",
                "note.bighash", "El archivo supera los 512 MB; se omitió el cálculo del hash",
                "note.hashfail", "Error al calcular el hash: {0}",
                "note.sigerr", "Error de verificación: {0}",

                "flag.temp", "El ejecutable está en una carpeta temporal: {0}",
                "flag.downloads", "El ejecutable está en la carpeta «Descargas»",
                "flag.appdata", "El ejecutable está en la carpeta de usuario AppData",
                "flag.nosig", "No se encontró firma digital incrustada",
                "flag.badsig", "La verificación de la firma digital falló",
                "flag.signer", "Firmante: {0}",
                "flag.nocompany", "No hay nombre de compañía en la información de versión",
                "flag.topmost", "Ventana siempre visible (WS_EX_TOPMOST)",
                "flag.toolwin", "Ventana de herramienta (WS_EX_TOOLWINDOW) — normalmente oculta de la barra de tareas / Alt+Tab",
                "flag.layered", "Ventana superpuesta (WS_EX_LAYERED) — habitual en superposiciones y transparencias",
                "flag.invisible", "La ventana objetivo no es visible ahora mismo",
                "flag.cloaked", "Ventana oculta por DWM (Cloaked): invisible pero en ejecución",
                "flag.notitle", "La ventana objetivo no tiene título",
                "flag.hidden", "El proceso tiene {0} ventana(s) de nivel superior ocultas más",
                "flag.recent", "El archivo se creó en los últimos 7 días ({0})",

                "welcome",
                    "Laocoön · Sonda de ventanas\n" +
                    "----------------------------------------------------------------\n" +
                    "Ante Troya todos querían meter ese caballo de madera en la ciudad. Solo Laocoonte se negó.\n" +
                    "Hundió su lanza en el vientre — y oyó armas chocando dentro.\n" +
                    "Esta herramienta hace lo mismo: pincha una ventana y escucha lo que esconde.\n" +
                    "----------------------------------------------------------------\n\n" +
                    "1. Pulsa F2 (o haz clic en «Apuntar» arriba a la izquierda)\n" +
                    "2. El cursor se vuelve una mira — llévalo sobre una ventana sospechosa: clase y título se muestran al instante\n" +
                    "3. Clic izquierdo para fijar; el informe completo aparece al instante\n" +
                    "4. Clic derecho o Esc cancela la puntería\n\n" +
                    "Qué se recopila:\n" +
                    "  · Ventana: clase, título, bits de estilo, rectángulo, siempre visible / oculta por DWM / superpuesta\n" +
                    "  · Proceso: ruta completa, línea de comandos, proceso padre, arquitectura, elevación, hora de inicio\n" +
                    "  · Archivo: tamaño, fechas, MD5 / SHA1 / SHA256, firma digital\n" +
                    "  · Todas las DLL cargadas por el proceso\n" +
                    "  · Las demás ventanas de nivel superior de ese proceso (incluidas las ocultas)\n" +
                    "  · El árbol de controles hijos de la ventana objetivo (con su texto)\n\n" +
                    "Consejo: ejecútalo como administrador para leer la ruta y los módulos de casi cualquier proceso.\n"
            });
        }

        // ================= العربية (从右到左) =================
        static Dictionary<string, string> Ar()
        {
            return D(new string[] {
                "app.brand", "Laocoön",
                "app.title", "Laocoön · فاحص النوافذ",

                "btn.aim", "◎  تصويب (F2)",
                "btn.aim.cancel", "جارٍ التصويب… Esc للإلغاء",
                "btn.copytext", "نسخ النص",
                "btn.save", "حفظ التقرير",
                "btn.openloc", "فتح موقع الملف",
                "btn.clear", "مسح",
                "st.lang", "اللغة",

                "st.ready", "جاهز — اضغط F2 للتصويب",
                "st.cleared", "تم المسح",
                "st.aimhint", "حرّك المؤشر فوق النافذة الهدف؛ الزر الأيسر للتثبيت، الزر الأيمن أو Esc للإلغاء",
                "st.target", "الهدف: {0}   \"{1}\"   PID {2}",
                "st.aimcancel", "أُلغي التصويب",
                "st.nohit", "لم تُلتقط أي نافذة، حاول مرة أخرى",
                "st.collecting", "جارٍ جمع البيانات…",
                "st.done", "تم: {0}   PID {1}   المقبض {2}",
                "st.fail", "فشل جمع البيانات: {0}",
                "st.copiedtext", "تم نسخ نص التقرير",
                "st.copiedsuffix", " ({0} حرفًا)",
                "st.nocopy", "لا يوجد ما يمكن نسخه",
                "st.copyfail", "فشل النسخ: {0}",
                "st.empty", "لم تُجمع أي بيانات بعد",
                "st.saved", "تم الحفظ: {0}",
                "st.savefail", "فشل الحفظ: {0}",
                "st.nopath", "لا يوجد مسار ملف لفتحه",
                "st.located", "حُدّد في المستكشف: {0}",
                "st.openfail", "فشل الفتح: {0}",
                "st.nocapture", "التقط نافذة أولًا",
                "st.langchanged", "لغة الواجهة: العربية",

                "dlg.save.title", "حفظ تقرير الالتقاط",
                "dlg.filter", "تقرير نصي (*.txt)|*.txt",
                "msg.captureerr", "خطأ أثناء الالتقاط:\r\n{0}",
                "hud.notitle", "(بلا عنوان)",
                "hud.parent", "الأب  {0}",
                "hud.top", "نافذة من المستوى الأعلى",

                "common.yes", "نعم",
                "common.no", "لا",
                "common.bytes", "بايت",
                "common.session", "الجلسة {0}",
                "common.elevated", "صلاحيات مرفوعة (مسؤول)",
                "common.normal", "مستخدم عادي",

                "rep.header", " Laocoön · فاحص النوافذ   التُقط في {0}",
                "rep.sec1", "----- [1] النافذة الهدف -----",
                "rep.sec2", "----- [2] العملية المالكة -----",
                "rep.sec3", "----- [3] الملف والبصمات -----",
                "rep.sec4", "----- [4] التوقيع الرقمي -----",
                "rep.sec5", "----- [5] كل نوافذ العملية من المستوى الأعلى ({0}) -----",
                "rep.sec6", "----- [6] شجرة عناصر التحكم الفرعية ({0}) -----",
                "rep.sec7", "----- [7] الوحدات المحمّلة ({0}) -----",
                "rep.secflags", "----- [!] مؤشرات مشبوهة (استدلالية، وليست حكمًا نهائيًا) -----",
                "rep.trunc", " (مقتطع)",
                "rep.none", "  (لا شيء)",
                "rep.nosig", "  لا يوجد توقيع رقمي مضمّن (ملفات النظام موقّعة عبر الكتالوج، ولا يُفحص ذلك هنا)",
                "rep.sigvalid", "صالح",
                "rep.siginvalid", "فشل التحقق",
                // 这两个值会和句柄、尺寸、类名拼在同一行，用 LRM 把阿拉伯段隔离出来，
                // 否则前后的数字/类名会被 BiDi 拖进 RTL 段里重排。
                "rep.visible", "\u200E  [ظاهرة]  \u200E",
                "rep.hidden", "\u200E  [مخفية]  \u200E",
                "rep.istarget", "   <= هذا الالتقاط",
                "rep.nochild", "  (لا توجد نوافذ فرعية)",
                "rep.client", "المنطقة العميلة {0} x {1}",
                "val.product", "   (المنتج {0})",

                "lbl.handle", "المقبض",
                "lbl.class", "فئة النافذة",
                "lbl.title", "العنوان",
                "lbl.hitctl", "التحكم تحت المؤشر",
                "lbl.ctltext", "نص التحكم",
                "lbl.hitpath", "مسار التحكم",
                "lbl.pos", "الموضع والحجم",
                "lbl.cursor", "إحداثيات المؤشر",
                "lbl.thread", "الخيط",
                "lbl.monitor", "الشاشة",
                "lbl.state", "الحالة",
                "lbl.style", "النمط",
                "lbl.exstyle", "النمط الموسّع",
                "lbl.owner", "النافذة المالكة",
                "lbl.pid", "PID",
                "lbl.pname", "اسم العملية",
                "lbl.ppath", "المسار الكامل",
                "lbl.cmd", "سطر الأوامر",
                "lbl.parent", "العملية الأب",
                "lbl.arch", "المعمارية",
                "lbl.elev", "الصلاحيات",
                "lbl.session", "الجلسة / البدء",
                "lbl.company", "الشركة",
                "lbl.product", "المنتج",
                "lbl.desc", "الوصف",
                "lbl.fver", "إصدار الملف",
                "lbl.copyright", "حقوق النشر",
                "lbl.fsize", "حجم الملف",
                "lbl.fcreated", "أُنشئ",
                "lbl.fmodified", "عُدّل",
                "lbl.md5", "MD5",
                "lbl.sha1", "SHA1",
                "lbl.sha256", "SHA256",
                "lbl.note", "ملاحظة",
                "lbl.sigstatus", "الحالة",
                "lbl.signer", "الموقّع",
                "lbl.issuer", "الجهة المُصدِرة",
                "lbl.expires", "صالح حتى",
                "lbl.chain", "حالة السلسلة",

                "fmt.state", "ظاهرة={0}  مُمكّنة={1}  مصغّرة={2}  مخفية بـ DWM={3}",

                "note.modtrunc", "وحدات كثيرة جدًا؛ تُعرض أول 400 وحدة من {0}",
                "note.moddenied", "تعذّر تعداد الوحدات (رفض الوصول)",
                "note.moddenied2", "تعذّر تعداد الوحدات (تلزم صلاحيات أعلى — شغّل البرنامج كمسؤول)",
                "note.bighash", "حجم الملف يتجاوز 512 ميغابايت؛ تم تخطي حساب البصمة",
                "note.hashfail", "فشل حساب البصمة: {0}",
                "note.sigerr", "خطأ في التحقق: {0}",

                "flag.temp", "الملف التنفيذي موجود في مجلد مؤقت: {0}",
                "flag.downloads", "الملف التنفيذي موجود في مجلد «التنزيلات»",
                "flag.appdata", "الملف التنفيذي موجود في مجلد AppData للمستخدم",
                "flag.nosig", "لم يُعثر على توقيع رقمي مضمّن",
                "flag.badsig", "فشل التحقق من التوقيع الرقمي",
                "flag.signer", "الموقّع: {0}",
                "flag.nocompany", "لا يوجد اسم شركة في معلومات إصدار الملف",
                "flag.topmost", "نافذة دائمًا في المقدمة (WS_EX_TOPMOST)",
                "flag.toolwin", "نافذة أدوات (WS_EX_TOOLWINDOW) — عادةً لا تظهر في شريط المهام أو Alt+Tab",
                "flag.layered", "نافذة طبقية (WS_EX_LAYERED) — تُستخدم كثيرًا للتراكبات والشفافية",
                "flag.invisible", "النافذة الهدف غير مرئية حاليًا",
                "flag.cloaked", "النافذة مخفية بواسطة DWM (Cloaked): غير مرئية لكنها ما زالت تعمل",
                "flag.notitle", "النافذة الهدف بلا عنوان",
                "flag.hidden", "لدى العملية {0} نافذة إضافية مخفية من المستوى الأعلى",
                "flag.recent", "أُنشئ الملف خلال آخر 7 أيام ({0})",

                "welcome",
                    "Laocoön · فاحص النوافذ\n" +
                    "----------------------------------------------------------------\n" +
                    "أمام طروادة، أراد الجميع إدخال ذلك الحصان الخشبي إلى المدينة. وحده لاوكون رفض.\n" +
                    "غرس رمحه في بطنه — فسمع داخله صوت اصطدام الحديد.\n" +
                    "هذه الأداة تفعل الشيء نفسه: تنخس نافذة، فتسمع ما تخفيه.\n" +
                    "----------------------------------------------------------------\n\n" +
                    "1. اضغط F2 (أو انقر «تصويب» في أعلى اليسار)\n" +
                    "2. يتحوّل المؤشر إلى صليب — حرّكه فوق نافذة مشبوهة، فيظهر صنفها وعنوانها فورًا\n" +
                    "3. الزر الأيسر يثبّت الهدف، ويظهر التقرير الكامل مباشرةً\n" +
                    "4. الزر الأيمن أو Esc يلغي التصويب\n\n" +
                    "ما الذي يُجمع:\n" +
                    "  · النافذة: الصنف، العنوان، بتات النمط، المستطيل، وحالة التثبيت في المقدمة/الإخفاء بواسطة DWM/الطبقات\n" +
                    "  · العملية: المسار الكامل، سطر الأوامر، العملية الأب، المعمارية، رفع الصلاحيات، وقت البدء\n" +
                    "  · الملف: الحجم، التواريخ، MD5 / SHA1 / SHA256، التوقيع الرقمي\n" +
                    "  · كل مكتبات DLL التي حمّلتها العملية\n" +
                    "  · بقية نوافذ هذه العملية من المستوى الأعلى (بما فيها المخفية)\n" +
                    "  · شجرة عناصر التحكم الفرعية للنافذة الهدف (مع نصوصها)\n\n" +
                    "نصيحة: شغّل البرنامج كمسؤول لتتمكن من قراءة مسار ووحدات أي عملية تقريبًا.\n"
            });
        }

        // ================= Deutsch =================
        static Dictionary<string, string> De()
        {
            return D(new string[] {
                "app.brand", "Laocoön",
                "app.title", "Laocoön · Fenster-Sonde",

                "btn.aim", "◎  Anvisieren (F2)",
                "btn.aim.cancel", "Zielen… Esc zum Abbrechen",
                "btn.copytext", "Text kopieren",
                "btn.save", "Bericht speichern",
                "btn.openloc", "Dateipfad öffnen",
                "btn.clear", "Leeren",
                "st.lang", "Sprache",

                "st.ready", "Bereit — F2 zum Anvisieren",
                "st.cleared", "Geleert",
                "st.aimhint", "Mauszeiger auf das Zielfenster bewegen; Linksklick fixiert, Rechtsklick / Esc bricht ab",
                "st.target", "Ziel: {0}   \"{1}\"   PID {2}",
                "st.aimcancel", "Zielen abgebrochen",
                "st.nohit", "Kein Fenster erfasst, bitte erneut versuchen",
                "st.collecting", "Sammle Daten…",
                "st.done", "Fertig: {0}   PID {1}   Handle {2}",
                "st.fail", "Erfassung fehlgeschlagen: {0}",
                "st.copiedtext", "Berichtstext kopiert",
                "st.copiedsuffix", " ({0} Zeichen)",
                "st.nocopy", "Nichts zu kopieren",
                "st.copyfail", "Kopieren fehlgeschlagen: {0}",
                "st.empty", "Noch keine Daten erfasst",
                "st.saved", "Gespeichert: {0}",
                "st.savefail", "Speichern fehlgeschlagen: {0}",
                "st.nopath", "Kein Dateipfad zum Öffnen",
                "st.located", "Im Explorer markiert: {0}",
                "st.openfail", "Öffnen fehlgeschlagen: {0}",
                "st.nocapture", "Zuerst ein Fenster erfassen",
                "st.langchanged", "Oberflächensprache: Deutsch",

                "dlg.save.title", "Erfassungsbericht speichern",
                "dlg.filter", "Textbericht (*.txt)|*.txt",
                "msg.captureerr", "Fehler bei der Erfassung:\r\n{0}",
                "hud.notitle", "(ohne Titel)",
                "hud.parent", "Elternfenster  {0}",
                "hud.top", "Fenster oberster Ebene",

                "common.yes", "ja",
                "common.no", "nein",
                "common.bytes", "Byte",
                "common.session", "Sitzung {0}",
                "common.elevated", "Erhöht (Administrator)",
                "common.normal", "Standardbenutzer",

                "rep.header", " Laocoön · Fenster-Sonde   erfasst am {0}",
                "rep.sec1", "----- [1] Zielfenster -----",
                "rep.sec2", "----- [2] Zugehöriger Prozess -----",
                "rep.sec3", "----- [3] Datei und Fingerabdrücke -----",
                "rep.sec4", "----- [4] Digitale Signatur -----",
                "rep.sec5", "----- [5] Alle Fenster oberster Ebene dieses Prozesses ({0}) -----",
                "rep.sec6", "----- [6] Baum der untergeordneten Steuerelemente ({0}) -----",
                "rep.sec7", "----- [7] Geladene Module ({0}) -----",
                "rep.secflags", "----- [!] Verdachtshinweise (Heuristik, kein Urteil) -----",
                "rep.trunc", " (gekürzt)",
                "rep.none", "  (keine)",
                "rep.nosig", "  Keine eingebettete digitale Signatur gefunden (Systemdateien sind per Katalog signiert, wird hier nicht geprüft)",
                "rep.sigvalid", "gültig",
                "rep.siginvalid", "Prüfung fehlgeschlagen",
                "rep.visible", "  [sichtbar] ",
                "rep.hidden", "  [versteckt]",
                "rep.istarget", "   <= diese Erfassung",
                "rep.nochild", "  (keine untergeordneten Fenster)",
                "rep.client", "Clientbereich {0} x {1}",
                "val.product", "   (Produkt {0})",

                "lbl.handle", "Handle",
                "lbl.class", "Fensterklasse",
                "lbl.title", "Titel",
                "lbl.hitctl", "Element unter dem Zeiger",
                "lbl.ctltext", "Text des Steuerelements",
                "lbl.hitpath", "Pfad des Steuerelements",
                "lbl.pos", "Position und Größe",
                "lbl.cursor", "Mausposition",
                "lbl.thread", "Thread",
                "lbl.monitor", "Monitor",
                "lbl.state", "Zustand",
                "lbl.style", "Stil",
                "lbl.exstyle", "Erweiterter Stil",
                "lbl.owner", "Besitzerfenster",
                "lbl.pid", "PID",
                "lbl.pname", "Prozessname",
                "lbl.ppath", "Vollständiger Pfad",
                "lbl.cmd", "Befehlszeile",
                "lbl.parent", "Elternprozess",
                "lbl.arch", "Architektur",
                "lbl.elev", "Rechte",
                "lbl.session", "Sitzung / gestartet",
                "lbl.company", "Firma",
                "lbl.product", "Produkt",
                "lbl.desc", "Beschreibung",
                "lbl.fver", "Dateiversion",
                "lbl.copyright", "Copyright",
                "lbl.fsize", "Dateigröße",
                "lbl.fcreated", "Erstellt",
                "lbl.fmodified", "Geändert",
                "lbl.md5", "MD5",
                "lbl.sha1", "SHA1",
                "lbl.sha256", "SHA256",
                "lbl.note", "Hinweis",
                "lbl.sigstatus", "Status",
                "lbl.signer", "Unterzeichner",
                "lbl.issuer", "Aussteller",
                "lbl.expires", "Gültig bis",
                "lbl.chain", "Kettenstatus",

                "fmt.state", "sichtbar={0}  aktiviert={1}  minimiert={2}  DWM-verdeckt={3}",

                "note.modtrunc", "Zu viele Module; nur die ersten 400 von {0} werden gezeigt",
                "note.moddenied", "Module lassen sich nicht auflisten (Zugriff verweigert)",
                "note.moddenied2", "Module lassen sich nicht auflisten (höhere Rechte nötig — als Administrator starten)",
                "note.bighash", "Datei über 512 MB; Hashberechnung übersprungen",
                "note.hashfail", "Hashberechnung fehlgeschlagen: {0}",
                "note.sigerr", "Fehler bei der Prüfung: {0}",

                "flag.temp", "Die ausführbare Datei liegt in einem temporären Ordner: {0}",
                "flag.downloads", "Die ausführbare Datei liegt im Ordner „Downloads“",
                "flag.appdata", "Die ausführbare Datei liegt im Benutzerordner AppData",
                "flag.nosig", "Keine eingebettete digitale Signatur gefunden",
                "flag.badsig", "Prüfung der digitalen Signatur fehlgeschlagen",
                "flag.signer", "Unterzeichner: {0}",
                "flag.nocompany", "Kein Firmenname in den Dateiversionsinformationen",
                "flag.topmost", "Immer im Vordergrund (WS_EX_TOPMOST)",
                "flag.toolwin", "Werkzeugfenster (WS_EX_TOOLWINDOW) — meist nicht in Taskleiste / Alt+Tab",
                "flag.layered", "Geschichtetes Fenster (WS_EX_LAYERED) — oft für Overlays und Transparenz",
                "flag.invisible", "Das Zielfenster ist derzeit unsichtbar",
                "flag.cloaked", "Fenster ist DWM-verdeckt (Cloaked): unsichtbar, läuft aber weiter",
                "flag.notitle", "Das Zielfenster hat keinen Titel",
                "flag.hidden", "Der Prozess hat {0} weitere versteckte Fenster oberster Ebene",
                "flag.recent", "Datei wurde in den letzten 7 Tagen erstellt ({0})",

                "welcome",
                    "Laocoön · Fenster-Sonde\n" +
                    "----------------------------------------------------------------\n" +
                    "Vor Troja wollten alle das hölzerne Pferd in die Stadt ziehen. Nur Laokoon weigerte sich.\n" +
                    "Er stieß seine Lanze in den Bauch — und hörte im Inneren Waffen aneinanderschlagen.\n" +
                    "Dieses Werkzeug tut genau das: ein Fenster anstechen und hören, was darin verborgen ist.\n" +
                    "----------------------------------------------------------------\n\n" +
                    "1. F2 drücken (oder oben links auf „Anvisieren“ klicken)\n" +
                    "2. Der Zeiger wird zum Fadenkreuz — über ein verdächtiges Fenster bewegen; Klasse und Titel erscheinen sofort\n" +
                    "3. Linksklick fixiert das Ziel; der vollständige Bericht erscheint sofort\n" +
                    "4. Rechtsklick oder Esc bricht das Zielen ab\n\n" +
                    "Was erfasst wird:\n" +
                    "  · Fenster: Klasse, Titel, Stilbits, Rechteck, Zustand „immer im Vordergrund“ / DWM-verdeckt / geschichtet\n" +
                    "  · Prozess: vollständiger Pfad, Befehlszeile, Elternprozess, Architektur, Rechte, Startzeit\n" +
                    "  · Datei: Größe, Zeitstempel, MD5 / SHA1 / SHA256, digitale Signatur\n" +
                    "  · Alle vom Prozess geladenen DLLs\n" +
                    "  · Die übrigen Fenster oberster Ebene dieses Prozesses (auch versteckte)\n" +
                    "  · Der Baum der untergeordneten Steuerelemente des Zielfensters (mit Texten)\n\n" +
                    "Tipp: als Administrator starten, um Pfad und Module fast jedes Prozesses zu lesen.\n"
            });
        }

        // ================= Português (Brasil) =================
        static Dictionary<string, string> Pt()
        {
            return D(new string[] {
                "app.brand", "Laocoön",
                "app.title", "Laocoön · Sonda de janelas",

                "btn.aim", "◎  Mirar (F2)",
                "btn.aim.cancel", "Mirando… Esc para cancelar",
                "btn.copytext", "Copiar texto",
                "btn.save", "Salvar relatório",
                "btn.openloc", "Abrir local do arquivo",
                "btn.clear", "Limpar",
                "st.lang", "Idioma",

                "st.ready", "Pronto — pressione F2 para mirar",
                "st.cleared", "Limpo",
                "st.aimhint", "Mova o cursor sobre a janela alvo; clique esquerdo fixa, clique direito / Esc cancela",
                "st.target", "Alvo: {0}   \"{1}\"   PID {2}",
                "st.aimcancel", "Mira cancelada",
                "st.nohit", "Nenhuma janela capturada, tente novamente",
                "st.collecting", "Coletando…",
                "st.done", "Concluído: {0}   PID {1}   handle {2}",
                "st.fail", "Falha na coleta: {0}",
                "st.copiedtext", "Texto do relatório copiado",
                "st.copiedsuffix", " ({0} caracteres)",
                "st.nocopy", "Nada para copiar",
                "st.copyfail", "Falha ao copiar: {0}",
                "st.empty", "Nada coletado ainda",
                "st.saved", "Salvo: {0}",
                "st.savefail", "Falha ao salvar: {0}",
                "st.nopath", "Nenhum caminho de arquivo para abrir",
                "st.located", "Localizado no Explorador: {0}",
                "st.openfail", "Falha ao abrir: {0}",
                "st.nocapture", "Capture uma janela primeiro",
                "st.langchanged", "Idioma da interface: português",

                "dlg.save.title", "Salvar relatório de captura",
                "dlg.filter", "Relatório de texto (*.txt)|*.txt",
                "msg.captureerr", "Erro durante a captura:\r\n{0}",
                "hud.notitle", "(sem título)",
                "hud.parent", "Pai  {0}",
                "hud.top", "Janela de nível superior",

                "common.yes", "sim",
                "common.no", "não",
                "common.bytes", "bytes",
                "common.session", "Sessão {0}",
                "common.elevated", "Elevado (administrador)",
                "common.normal", "Usuário padrão",

                "rep.header", " Laocoön · Sonda de janelas   capturado em {0}",
                "rep.sec1", "----- [1] Janela alvo -----",
                "rep.sec2", "----- [2] Processo proprietário -----",
                "rep.sec3", "----- [3] Arquivo e impressões -----",
                "rep.sec4", "----- [4] Assinatura digital -----",
                "rep.sec5", "----- [5] Todas as janelas de nível superior do processo ({0}) -----",
                "rep.sec6", "----- [6] Árvore de controles filhos ({0}) -----",
                "rep.sec7", "----- [7] Módulos carregados ({0}) -----",
                "rep.secflags", "----- [!] Indícios suspeitos (heurística, não conclusão) -----",
                "rep.trunc", " (truncado)",
                "rep.none", "  (nenhum)",
                "rep.nosig", "  Nenhuma assinatura digital embutida (arquivos do sistema usam assinatura de catálogo, não verificada aqui)",
                "rep.sigvalid", "válida",
                "rep.siginvalid", "verificação falhou",
                "rep.visible", "  [visível]  ",
                "rep.hidden", "  [oculta]   ",
                "rep.istarget", "   <= esta captura",
                "rep.nochild", "  (sem janelas filhas)",
                "rep.client", "área cliente {0} x {1}",
                "val.product", "   (produto {0})",

                "lbl.handle", "Handle",
                "lbl.class", "Classe",
                "lbl.title", "Título",
                "lbl.hitctl", "Controle sob o cursor",
                "lbl.ctltext", "Texto do controle",
                "lbl.hitpath", "Caminho do controle",
                "lbl.pos", "Posição e tamanho",
                "lbl.cursor", "Coordenadas do cursor",
                "lbl.thread", "Thread",
                "lbl.monitor", "Monitor",
                "lbl.state", "Estado",
                "lbl.style", "Estilo",
                "lbl.exstyle", "Estilo estendido",
                "lbl.owner", "Janela proprietária",
                "lbl.pid", "PID",
                "lbl.pname", "Nome do processo",
                "lbl.ppath", "Caminho completo",
                "lbl.cmd", "Linha de comando",
                "lbl.parent", "Processo pai",
                "lbl.arch", "Arquitetura",
                "lbl.elev", "Privilégios",
                "lbl.session", "Sessão / início",
                "lbl.company", "Empresa",
                "lbl.product", "Produto",
                "lbl.desc", "Descrição",
                "lbl.fver", "Versão do arquivo",
                "lbl.copyright", "Copyright",
                "lbl.fsize", "Tamanho do arquivo",
                "lbl.fcreated", "Criado",
                "lbl.fmodified", "Modificado",
                "lbl.md5", "MD5",
                "lbl.sha1", "SHA1",
                "lbl.sha256", "SHA256",
                "lbl.note", "Observação",
                "lbl.sigstatus", "Status",
                "lbl.signer", "Signatário",
                "lbl.issuer", "Emissor",
                "lbl.expires", "Válida até",
                "lbl.chain", "Status da cadeia",

                "fmt.state", "visível={0}  habilitada={1}  minimizada={2}  oculta por DWM={3}",

                "note.modtrunc", "Muitos módulos; exibindo os primeiros 400 de {0}",
                "note.moddenied", "Não foi possível enumerar os módulos (acesso negado)",
                "note.moddenied2", "Não foi possível enumerar os módulos (é preciso mais privilégios — execute como administrador)",
                "note.bighash", "Arquivo acima de 512 MB; cálculo do hash ignorado",
                "note.hashfail", "Falha no cálculo do hash: {0}",
                "note.sigerr", "Erro na verificação: {0}",

                "flag.temp", "O executável está em uma pasta temporária: {0}",
                "flag.downloads", "O executável está na pasta «Downloads»",
                "flag.appdata", "O executável está na pasta de usuário AppData",
                "flag.nosig", "Nenhuma assinatura digital embutida encontrada",
                "flag.badsig", "A verificação da assinatura digital falhou",
                "flag.signer", "Signatário: {0}",
                "flag.nocompany", "Não há nome de empresa nas informações de versão",
                "flag.topmost", "Janela sempre no topo (WS_EX_TOPMOST)",
                "flag.toolwin", "Janela de ferramenta (WS_EX_TOOLWINDOW) — normalmente fora da barra de tarefas / Alt+Tab",
                "flag.layered", "Janela em camadas (WS_EX_LAYERED) — comum em sobreposições e transparência",
                "flag.invisible", "A janela alvo está invisível no momento",
                "flag.cloaked", "Janela oculta pelo DWM (Cloaked): invisível, mas em execução",
                "flag.notitle", "A janela alvo não tem título",
                "flag.hidden", "O processo tem mais {0} janela(s) de nível superior oculta(s)",
                "flag.recent", "O arquivo foi criado nos últimos 7 dias ({0})",

                "welcome",
                    "Laocoön · Sonda de janelas\n" +
                    "----------------------------------------------------------------\n" +
                    "Diante de Troia, todos queriam puxar aquele cavalo de madeira para dentro. Só Laocoonte recusou.\n" +
                    "Ele cravou a lança no ventre do cavalo — e ouviu armas se chocando lá dentro.\n" +
                    "Esta ferramenta faz o mesmo: cutuca uma janela e escuta o que ela esconde.\n" +
                    "----------------------------------------------------------------\n\n" +
                    "1. Pressione F2 (ou clique em «Mirar» no canto superior esquerdo)\n" +
                    "2. O cursor vira uma mira — leve-o sobre uma janela suspeita; classe e título aparecem na hora\n" +
                    "3. Clique esquerdo fixa o alvo; o relatório completo aparece imediatamente\n" +
                    "4. Clique direito ou Esc cancela a mira\n\n" +
                    "O que é coletado:\n" +
                    "  · Janela: classe, título, bits de estilo, retângulo, sempre no topo / oculta pelo DWM / em camadas\n" +
                    "  · Processo: caminho completo, linha de comando, processo pai, arquitetura, elevação, hora de início\n" +
                    "  · Arquivo: tamanho, datas, MD5 / SHA1 / SHA256, assinatura digital\n" +
                    "  · Todas as DLL carregadas pelo processo\n" +
                    "  · As demais janelas de nível superior desse processo (inclusive ocultas)\n" +
                    "  · A árvore de controles filhos da janela alvo (com o texto deles)\n\n" +
                    "Dica: execute como administrador para ler o caminho e os módulos de quase qualquer processo.\n"
            });
        }

        // ================= 日本語 =================
        static Dictionary<string, string> Ja()
        {
            return D(new string[] {
                "app.brand", "Laocoön",
                "app.title", "Laocoön · ウィンドウ探針",

                "btn.aim", "◎  照準 (F2)",
                "btn.aim.cancel", "照準中… Esc で中止",
                "btn.copytext", "テキストをコピー",
                "btn.save", "レポートを保存",
                "btn.openloc", "ファイルの場所を開く",
                "btn.clear", "クリア",
                "st.lang", "言語",

                "st.ready", "準備完了 — F2 で照準を開始",
                "st.cleared", "クリアしました",
                "st.aimhint", "対象ウィンドウにカーソルを移動し、左クリックで確定、右クリック / Esc で中止",
                "st.target", "対象: {0}   \"{1}\"   PID {2}",
                "st.aimcancel", "照準を中止しました",
                "st.nohit", "ウィンドウを捕捉できませんでした。もう一度お試しください",
                "st.collecting", "採取中…",
                "st.done", "完了: {0}   PID {1}   ハンドル {2}",
                "st.fail", "採取に失敗しました: {0}",
                "st.copiedtext", "レポート本文をコピーしました",
                "st.copiedsuffix", "（{0} 文字）",
                "st.nocopy", "コピーする内容がありません",
                "st.copyfail", "コピーに失敗しました: {0}",
                "st.empty", "まだ何も採取していません",
                "st.saved", "保存しました: {0}",
                "st.savefail", "保存に失敗しました: {0}",
                "st.nopath", "開けるファイルパスがありません",
                "st.located", "エクスプローラーで表示しました: {0}",
                "st.openfail", "開けませんでした: {0}",
                "st.nocapture", "先にウィンドウを採取してください",
                "st.langchanged", "表示言語を日本語に切り替えました",

                "dlg.save.title", "採取レポートを保存",
                "dlg.filter", "テキストレポート (*.txt)|*.txt",
                "msg.captureerr", "採取中にエラーが発生しました:\r\n{0}",
                "hud.notitle", "（タイトルなし）",
                "hud.parent", "親ウィンドウ  {0}",
                "hud.top", "トップレベルウィンドウ",

                "common.yes", "はい",
                "common.no", "いいえ",
                "common.bytes", "バイト",
                "common.session", "セッション {0}",
                "common.elevated", "昇格済み（管理者）",
                "common.normal", "標準ユーザー",

                "rep.header", " Laocoön · ウィンドウ探針   採取日時 {0}",
                "rep.sec1", "----- [1] 対象ウィンドウ -----",
                "rep.sec2", "----- [2] 所有プロセス -----",
                "rep.sec3", "----- [3] ファイルと指紋 -----",
                "rep.sec4", "----- [4] デジタル署名 -----",
                "rep.sec5", "----- [5] このプロセスの全トップレベルウィンドウ ({0}) -----",
                "rep.sec6", "----- [6] 子コントロールツリー ({0}) -----",
                "rep.sec7", "----- [7] 読み込み済みモジュール ({0}) -----",
                "rep.secflags", "----- [!] 疑わしい点（ヒューリスティックな判断で、結論ではありません） -----",
                "rep.trunc", "（切り詰め）",
                "rep.none", "  （なし）",
                "rep.nosig", "  埋め込みデジタル署名は見つかりません（システムファイルはカタログ署名のため、ここでは検査しません）",
                "rep.sigvalid", "有効",
                "rep.siginvalid", "検証に失敗",
                "rep.visible", "  [表示]    ",
                "rep.hidden", "  [非表示]  ",
                "rep.istarget", "   <= 今回の採取対象",
                "rep.nochild", "  （子ウィンドウなし）",
                "rep.client", "クライアント領域 {0} x {1}",
                "val.product", "   （製品 {0}）",

                "lbl.handle", "ハンドル",
                "lbl.class", "ウィンドウクラス",
                "lbl.title", "タイトル",
                "lbl.hitctl", "カーソル下のコントロール",
                "lbl.ctltext", "コントロールのテキスト",
                "lbl.hitpath", "コントロールの階層",
                "lbl.pos", "位置とサイズ",
                "lbl.cursor", "マウス座標",
                "lbl.thread", "スレッド",
                "lbl.monitor", "モニター",
                "lbl.state", "状態",
                "lbl.style", "スタイル",
                "lbl.exstyle", "拡張スタイル",
                "lbl.owner", "オーナーウィンドウ",
                "lbl.pid", "PID",
                "lbl.pname", "プロセス名",
                "lbl.ppath", "フルパス",
                "lbl.cmd", "コマンドライン",
                "lbl.parent", "親プロセス",
                "lbl.arch", "アーキテクチャ",
                "lbl.elev", "権限",
                "lbl.session", "セッション / 起動",
                "lbl.company", "会社名",
                "lbl.product", "製品名",
                "lbl.desc", "説明",
                "lbl.fver", "ファイルバージョン",
                "lbl.copyright", "著作権",
                "lbl.fsize", "ファイルサイズ",
                "lbl.fcreated", "作成日時",
                "lbl.fmodified", "更新日時",
                "lbl.md5", "MD5",
                "lbl.sha1", "SHA1",
                "lbl.sha256", "SHA256",
                "lbl.note", "備考",
                "lbl.sigstatus", "状態",
                "lbl.signer", "署名者",
                "lbl.issuer", "発行者",
                "lbl.expires", "有効期限",
                "lbl.chain", "チェーンの状態",

                "fmt.state", "表示={0}  有効={1}  最小化={2}  DWM非表示={3}",

                "note.modtrunc", "モジュールが多すぎます。{0} 個のうち先頭 400 個のみ表示",
                "note.moddenied", "モジュールを列挙できません（アクセス拒否）",
                "note.moddenied2", "モジュールを列挙できません（より高い権限が必要 — 管理者として実行してください）",
                "note.bighash", "ファイルが 512MB を超えるため、ハッシュ計算を省略しました",
                "note.hashfail", "ハッシュ計算に失敗しました: {0}",
                "note.sigerr", "検証エラー: {0}",

                "flag.temp", "実行ファイルが一時フォルダーにあります: {0}",
                "flag.downloads", "実行ファイルが「ダウンロード」フォルダーにあります",
                "flag.appdata", "実行ファイルが AppData ユーザーフォルダーにあります",
                "flag.nosig", "埋め込みデジタル署名が見つかりません",
                "flag.badsig", "デジタル署名の検証に失敗しました",
                "flag.signer", "署名者: {0}",
                "flag.nocompany", "バージョン情報に会社名がありません",
                "flag.topmost", "最前面固定のウィンドウ (WS_EX_TOPMOST)",
                "flag.toolwin", "ツールウィンドウ (WS_EX_TOOLWINDOW) — 通常タスクバーや Alt+Tab に出ません",
                "flag.layered", "レイヤードウィンドウ (WS_EX_LAYERED) — 半透明のオーバーレイでよく使われます",
                "flag.invisible", "対象ウィンドウは現在非表示です",
                "flag.cloaked", "ウィンドウは DWM により隠されています (Cloaked): 見えませんが動作中です",
                "flag.notitle", "対象ウィンドウにタイトルがありません",
                "flag.hidden", "このプロセスには他に {0} 個の非表示トップレベルウィンドウがあります",
                "flag.recent", "ファイルは直近 7 日以内に作成されています（{0}）",

                "welcome",
                    "Laocoön · ウィンドウ探針\n" +
                    "----------------------------------------------------------------\n" +
                    "トロイアの城門前で、誰もがあの木馬を城内へ引き入れようとした。ただラオコーンだけが信じなかった。\n" +
                    "彼は槍を木馬の腹に突き立て —— 中から金属のぶつかる音を聞いた。\n" +
                    "この道具がやることも同じです。ウィンドウをつついて、中に何が隠れているかを聞き取る。\n" +
                    "----------------------------------------------------------------\n\n" +
                    "1. F2 を押す（または左上の「照準」をクリック）\n" +
                    "2. カーソルが十字照準になります。怪しいウィンドウに合わせると、クラス名とタイトルがその場で表示されます\n" +
                    "3. 左クリックで確定。全情報がすぐに表示されます\n" +
                    "4. 右クリックまたは Esc で照準を中止\n\n" +
                    "採取できる内容:\n" +
                    "  · ウィンドウ: クラス名、タイトル、スタイルビット、矩形、最前面 / DWM 非表示 / レイヤードの状態\n" +
                    "  · プロセス: フルパス、コマンドライン、親プロセス、アーキテクチャ、昇格の有無、起動時刻\n" +
                    "  · ファイル: サイズ、日時、MD5 / SHA1 / SHA256、デジタル署名\n" +
                    "  · プロセスが読み込んだすべての DLL\n" +
                    "  · このプロセスの他のトップレベルウィンドウ（非表示のものも含む）\n" +
                    "  · 対象ウィンドウの子コントロールツリー（テキスト付き）\n\n" +
                    "ヒント: 管理者として実行すると、ほとんどすべてのプロセスのパスとモジュールを読めます。\n"
            });
        }

        // ================= 한국어 =================
        static Dictionary<string, string> Ko()
        {
            return D(new string[] {
                "app.brand", "Laocoön",
                "app.title", "Laocoön · 창 프로브",

                "btn.aim", "◎  조준 (F2)",
                "btn.aim.cancel", "조준 중… Esc 로 취소",
                "btn.copytext", "텍스트 복사",
                "btn.save", "보고서 저장",
                "btn.openloc", "파일 위치 열기",
                "btn.clear", "지우기",
                "st.lang", "언어",

                "st.ready", "준비됨 — F2 를 눌러 조준 시작",
                "st.cleared", "지웠습니다",
                "st.aimhint", "대상 창 위로 커서를 옮기세요. 왼쪽 클릭으로 고정, 오른쪽 클릭 / Esc 로 취소",
                "st.target", "대상: {0}   \"{1}\"   PID {2}",
                "st.aimcancel", "조준을 취소했습니다",
                "st.nohit", "창을 잡지 못했습니다. 다시 시도하세요",
                "st.collecting", "수집 중…",
                "st.done", "완료: {0}   PID {1}   핸들 {2}",
                "st.fail", "수집 실패: {0}",
                "st.copiedtext", "보고서 본문을 복사했습니다",
                "st.copiedsuffix", " ({0} 자)",
                "st.nocopy", "복사할 내용이 없습니다",
                "st.copyfail", "복사 실패: {0}",
                "st.empty", "아직 수집한 내용이 없습니다",
                "st.saved", "저장했습니다: {0}",
                "st.savefail", "저장 실패: {0}",
                "st.nopath", "열 수 있는 파일 경로가 없습니다",
                "st.located", "탐색기에서 표시했습니다: {0}",
                "st.openfail", "열기 실패: {0}",
                "st.nocapture", "먼저 창을 수집하세요",
                "st.langchanged", "인터페이스 언어를 한국어로 바꿨습니다",

                "dlg.save.title", "수집 보고서 저장",
                "dlg.filter", "텍스트 보고서 (*.txt)|*.txt",
                "msg.captureerr", "수집 중 오류가 발생했습니다:\r\n{0}",
                "hud.notitle", "(제목 없음)",
                "hud.parent", "부모 창  {0}",
                "hud.top", "최상위 창",

                "common.yes", "예",
                "common.no", "아니오",
                "common.bytes", "바이트",
                "common.session", "세션 {0}",
                "common.elevated", "권한 상승됨 (관리자)",
                "common.normal", "일반 사용자",

                "rep.header", " Laocoön · 창 프로브   수집 시각 {0}",
                "rep.sec1", "----- [1] 대상 창 -----",
                "rep.sec2", "----- [2] 소유 프로세스 -----",
                "rep.sec3", "----- [3] 파일과 지문 -----",
                "rep.sec4", "----- [4] 디지털 서명 -----",
                "rep.sec5", "----- [5] 이 프로세스의 모든 최상위 창 ({0}) -----",
                "rep.sec6", "----- [6] 자식 컨트롤 트리 ({0}) -----",
                "rep.sec7", "----- [7] 로드된 모듈 ({0}) -----",
                "rep.secflags", "----- [!] 의심 지점 (휴리스틱 판단이며 결론이 아닙니다) -----",
                "rep.trunc", " (잘림)",
                "rep.none", "  (없음)",
                "rep.nosig", "  내장 디지털 서명이 없습니다 (시스템 파일은 카탈로그 서명이라 여기서 검사하지 않습니다)",
                "rep.sigvalid", "유효",
                "rep.siginvalid", "검증 실패",
                "rep.visible", "  [표시]    ",
                "rep.hidden", "  [숨김]    ",
                "rep.istarget", "   <= 이번 수집 대상",
                "rep.nochild", "  (자식 창 없음)",
                "rep.client", "클라이언트 영역 {0} x {1}",
                "val.product", "   (제품 {0})",

                "lbl.handle", "핸들",
                "lbl.class", "창 클래스",
                "lbl.title", "제목",
                "lbl.hitctl", "커서 아래 컨트롤",
                "lbl.ctltext", "컨트롤 텍스트",
                "lbl.hitpath", "컨트롤 경로",
                "lbl.pos", "위치와 크기",
                "lbl.cursor", "마우스 좌표",
                "lbl.thread", "스레드",
                "lbl.monitor", "모니터",
                "lbl.state", "상태",
                "lbl.style", "스타일",
                "lbl.exstyle", "확장 스타일",
                "lbl.owner", "소유자 창",
                "lbl.pid", "PID",
                "lbl.pname", "프로세스 이름",
                "lbl.ppath", "전체 경로",
                "lbl.cmd", "명령줄",
                "lbl.parent", "부모 프로세스",
                "lbl.arch", "아키텍처",
                "lbl.elev", "권한",
                "lbl.session", "세션 / 시작",
                "lbl.company", "회사",
                "lbl.product", "제품",
                "lbl.desc", "설명",
                "lbl.fver", "파일 버전",
                "lbl.copyright", "저작권",
                "lbl.fsize", "파일 크기",
                "lbl.fcreated", "만든 날짜",
                "lbl.fmodified", "수정한 날짜",
                "lbl.md5", "MD5",
                "lbl.sha1", "SHA1",
                "lbl.sha256", "SHA256",
                "lbl.note", "비고",
                "lbl.sigstatus", "상태",
                "lbl.signer", "서명자",
                "lbl.issuer", "발급자",
                "lbl.expires", "유효 기한",
                "lbl.chain", "체인 상태",

                "fmt.state", "표시={0}  사용={1}  최소화={2}  DWM 숨김={3}",

                "note.modtrunc", "모듈이 너무 많습니다. {0} 개 중 앞의 400 개만 표시",
                "note.moddenied", "모듈을 나열할 수 없습니다 (액세스 거부)",
                "note.moddenied2", "모듈을 나열할 수 없습니다 (더 높은 권한 필요 — 관리자로 실행하세요)",
                "note.bighash", "파일이 512MB 를 넘어 해시 계산을 건너뛰었습니다",
                "note.hashfail", "해시 계산 실패: {0}",
                "note.sigerr", "검증 오류: {0}",

                "flag.temp", "실행 파일이 임시 폴더에 있습니다: {0}",
                "flag.downloads", "실행 파일이 «다운로드» 폴더에 있습니다",
                "flag.appdata", "실행 파일이 AppData 사용자 폴더에 있습니다",
                "flag.nosig", "내장 디지털 서명을 찾지 못했습니다",
                "flag.badsig", "디지털 서명 검증에 실패했습니다",
                "flag.signer", "서명자: {0}",
                "flag.nocompany", "버전 정보에 회사 이름이 없습니다",
                "flag.topmost", "항상 위 창 (WS_EX_TOPMOST)",
                "flag.toolwin", "도구 창 (WS_EX_TOOLWINDOW) — 보통 작업 표시줄이나 Alt+Tab 에 나오지 않습니다",
                "flag.layered", "계층 창 (WS_EX_LAYERED) — 반투명 오버레이에 자주 쓰입니다",
                "flag.invisible", "대상 창이 현재 보이지 않습니다",
                "flag.cloaked", "창이 DWM 에 의해 숨겨졌습니다 (Cloaked): 보이지 않지만 실행 중입니다",
                "flag.notitle", "대상 창에 제목이 없습니다",
                "flag.hidden", "이 프로세스에는 숨겨진 최상위 창이 {0} 개 더 있습니다",
                "flag.recent", "파일이 최근 7 일 안에 만들어졌습니다 ({0})",

                "welcome",
                    "Laocoön · 창 프로브\n" +
                    "----------------------------------------------------------------\n" +
                    "트로이 성문 앞에서 모두가 그 목마를 성 안으로 끌어들이려 했습니다. 라오콘만이 믿지 않았습니다.\n" +
                    "그는 창을 말의 배에 찔러 넣었고 — 안에서 쇠붙이 부딪히는 소리를 들었습니다.\n" +
                    "이 도구가 하는 일도 같습니다. 창을 한 번 찔러, 그 안에 무엇이 숨었는지 듣습니다.\n" +
                    "----------------------------------------------------------------\n\n" +
                    "1. F2 를 누릅니다 (또는 왼쪽 위 «조준» 클릭)\n" +
                    "2. 커서가 십자선이 됩니다. 수상한 창 위로 옮기면 클래스와 제목이 바로 표시됩니다\n" +
                    "3. 왼쪽 클릭으로 고정하면 전체 정보가 바로 표시됩니다\n" +
                    "4. 오른쪽 클릭 또는 Esc 로 조준을 취소합니다\n\n" +
                    "수집 항목:\n" +
                    "  · 창: 클래스, 제목, 스타일 비트, 사각형, 항상 위 / DWM 숨김 / 계층 상태\n" +
                    "  · 프로세스: 전체 경로, 명령줄, 부모 프로세스, 아키텍처, 권한 상승 여부, 시작 시각\n" +
                    "  · 파일: 크기, 날짜, MD5 / SHA1 / SHA256, 디지털 서명\n" +
                    "  · 프로세스가 로드한 모든 DLL\n" +
                    "  · 이 프로세스의 나머지 최상위 창 (숨겨진 것 포함)\n" +
                    "  · 대상 창의 자식 컨트롤 트리 (텍스트 포함)\n\n" +
                    "팁: 관리자 권한으로 실행하면 거의 모든 프로세스의 경로와 모듈을 읽을 수 있습니다.\n"
            });
        }

        // ================= हिन्दी =================
        static Dictionary<string, string> Hi()
        {
            return D(new string[] {
                "app.brand", "Laocoön",
                "app.title", "Laocoön · विंडो प्रोब",

                "btn.aim", "◎  निशाना (F2)",
                "btn.aim.cancel", "निशाना लगा रहे हैं… Esc से रद्द करें",
                "btn.copytext", "टेक्स्ट कॉपी करें",
                "btn.save", "रिपोर्ट सहेजें",
                "btn.openloc", "फ़ाइल का स्थान खोलें",
                "btn.clear", "साफ़ करें",
                "st.lang", "भाषा",

                "st.ready", "तैयार — निशाना लगाने के लिए F2 दबाएँ",
                "st.cleared", "साफ़ कर दिया",
                "st.aimhint", "कर्सर को लक्ष्य विंडो पर ले जाएँ; बायाँ क्लिक स्थिर करता है, दायाँ क्लिक / Esc रद्द करता है",
                "st.target", "लक्ष्य: {0}   \"{1}\"   PID {2}",
                "st.aimcancel", "निशाना रद्द किया गया",
                "st.nohit", "कोई विंडो नहीं पकड़ी गई, फिर से प्रयास करें",
                "st.collecting", "संग्रह हो रहा है…",
                "st.done", "पूर्ण: {0}   PID {1}   हैंडल {2}",
                "st.fail", "संग्रह विफल: {0}",
                "st.copiedtext", "रिपोर्ट टेक्स्ट कॉपी हो गया",
                "st.copiedsuffix", " ({0} अक्षर)",
                "st.nocopy", "कॉपी करने के लिए कुछ नहीं है",
                "st.copyfail", "कॉपी विफल: {0}",
                "st.empty", "अभी कुछ भी संग्रह नहीं हुआ",
                "st.saved", "सहेजा गया: {0}",
                "st.savefail", "सहेजना विफल: {0}",
                "st.nopath", "खोलने के लिए कोई फ़ाइल पथ नहीं",
                "st.located", "एक्सप्लोरर में दिखाया गया: {0}",
                "st.openfail", "खोलना विफल: {0}",
                "st.nocapture", "पहले कोई विंडो पकड़ें",
                "st.langchanged", "इंटरफ़ेस की भाषा: हिन्दी",

                "dlg.save.title", "संग्रह रिपोर्ट सहेजें",
                "dlg.filter", "टेक्स्ट रिपोर्ट (*.txt)|*.txt",
                "msg.captureerr", "संग्रह के दौरान त्रुटि:\r\n{0}",
                "hud.notitle", "(शीर्षक नहीं)",
                "hud.parent", "जनक  {0}",
                "hud.top", "शीर्ष-स्तरीय विंडो",

                "common.yes", "हाँ",
                "common.no", "नहीं",
                "common.bytes", "बाइट",
                "common.session", "सत्र {0}",
                "common.elevated", "उन्नत (व्यवस्थापक)",
                "common.normal", "सामान्य उपयोगकर्ता",

                "rep.header", " Laocoön · विंडो प्रोब   संग्रह समय {0}",
                "rep.sec1", "----- [1] लक्ष्य विंडो -----",
                "rep.sec2", "----- [2] स्वामी प्रोसेस -----",
                "rep.sec3", "----- [3] फ़ाइल और फ़िंगरप्रिंट -----",
                "rep.sec4", "----- [4] डिजिटल हस्ताक्षर -----",
                "rep.sec5", "----- [5] इस प्रोसेस की सभी शीर्ष-स्तरीय विंडो ({0}) -----",
                "rep.sec6", "----- [6] चाइल्ड कंट्रोल वृक्ष ({0}) -----",
                "rep.sec7", "----- [7] लोड किए गए मॉड्यूल ({0}) -----",
                "rep.secflags", "----- [!] संदिग्ध संकेत (अनुमान, अंतिम निर्णय नहीं) -----",
                "rep.trunc", " (काटा गया)",
                "rep.none", "  (कोई नहीं)",
                "rep.nosig", "  कोई अंतर्निहित डिजिटल हस्ताक्षर नहीं मिला (सिस्टम फ़ाइलें कैटलॉग हस्ताक्षर से हस्ताक्षरित होती हैं, यहाँ जाँची नहीं जाती)",
                "rep.sigvalid", "वैध",
                "rep.siginvalid", "सत्यापन विफल",
                "rep.visible", "  [दृश्य]   ",
                "rep.hidden", "  [छिपी]    ",
                "rep.istarget", "   <= यह संग्रह",
                "rep.nochild", "  (कोई चाइल्ड विंडो नहीं)",
                "rep.client", "क्लाइंट क्षेत्र {0} x {1}",
                "val.product", "   (उत्पाद {0})",

                "lbl.handle", "हैंडल",
                "lbl.class", "विंडो क्लास",
                "lbl.title", "शीर्षक",
                "lbl.hitctl", "कर्सर के नीचे कंट्रोल",
                "lbl.ctltext", "कंट्रोल टेक्स्ट",
                "lbl.hitpath", "कंट्रोल पथ",
                "lbl.pos", "स्थिति और आकार",
                "lbl.cursor", "माउस निर्देशांक",
                "lbl.thread", "थ्रेड",
                "lbl.monitor", "मॉनिटर",
                "lbl.state", "स्थिति",
                "lbl.style", "शैली",
                "lbl.exstyle", "विस्तारित शैली",
                "lbl.owner", "स्वामी विंडो",
                "lbl.pid", "PID",
                "lbl.pname", "प्रोसेस नाम",
                "lbl.ppath", "पूरा पथ",
                "lbl.cmd", "कमांड लाइन",
                "lbl.parent", "जनक प्रोसेस",
                "lbl.arch", "आर्किटेक्चर",
                "lbl.elev", "अनुमतियाँ",
                "lbl.session", "सत्र / प्रारंभ",
                "lbl.company", "कंपनी",
                "lbl.product", "उत्पाद",
                "lbl.desc", "विवरण",
                "lbl.fver", "फ़ाइल संस्करण",
                "lbl.copyright", "कॉपीराइट",
                "lbl.fsize", "फ़ाइल आकार",
                "lbl.fcreated", "बनाई गई",
                "lbl.fmodified", "बदली गई",
                "lbl.md5", "MD5",
                "lbl.sha1", "SHA1",
                "lbl.sha256", "SHA256",
                "lbl.note", "टिप्पणी",
                "lbl.sigstatus", "स्थिति",
                "lbl.signer", "हस्ताक्षरकर्ता",
                "lbl.issuer", "जारीकर्ता",
                "lbl.expires", "वैधता समाप्ति",
                "lbl.chain", "श्रृंखला स्थिति",

                "fmt.state", "दृश्य={0}  सक्षम={1}  न्यूनतम={2}  DWM छिपी={3}",

                "note.modtrunc", "बहुत अधिक मॉड्यूल; {0} में से केवल पहले 400 दिखाए गए",
                "note.moddenied", "मॉड्यूल सूचीबद्ध नहीं हो सके (पहुँच अस्वीकृत)",
                "note.moddenied2", "मॉड्यूल सूचीबद्ध नहीं हो सके (अधिक अनुमतियाँ चाहिए — व्यवस्थापक के रूप में चलाएँ)",
                "note.bighash", "फ़ाइल 512MB से बड़ी है; हैश गणना छोड़ी गई",
                "note.hashfail", "हैश गणना विफल: {0}",
                "note.sigerr", "सत्यापन त्रुटि: {0}",

                "flag.temp", "निष्पादन योग्य फ़ाइल अस्थायी फ़ोल्डर में है: {0}",
                "flag.downloads", "निष्पादन योग्य फ़ाइल «डाउनलोड» फ़ोल्डर में है",
                "flag.appdata", "निष्पादन योग्य फ़ाइल AppData उपयोगकर्ता फ़ोल्डर में है",
                "flag.nosig", "कोई अंतर्निहित डिजिटल हस्ताक्षर नहीं मिला",
                "flag.badsig", "डिजिटल हस्ताक्षर का सत्यापन विफल",
                "flag.signer", "हस्ताक्षरकर्ता: {0}",
                "flag.nocompany", "संस्करण जानकारी में कंपनी का नाम नहीं है",
                "flag.topmost", "हमेशा सामने रहने वाली विंडो (WS_EX_TOPMOST)",
                "flag.toolwin", "टूल विंडो (WS_EX_TOOLWINDOW) — सामान्यतः टास्कबार / Alt+Tab में नहीं दिखती",
                "flag.layered", "लेयर्ड विंडो (WS_EX_LAYERED) — पारदर्शी ओवरले के लिए आम",
                "flag.invisible", "लक्ष्य विंडो इस समय अदृश्य है",
                "flag.cloaked", "विंडो DWM द्वारा छिपाई गई (Cloaked): दिखती नहीं, पर चल रही है",
                "flag.notitle", "लक्ष्य विंडो का कोई शीर्षक नहीं",
                "flag.hidden", "इस प्रोसेस की {0} और छिपी शीर्ष-स्तरीय विंडो हैं",
                "flag.recent", "फ़ाइल पिछले 7 दिनों में बनाई गई ({0})",

                "welcome",
                    "Laocoön · विंडो प्रोब\n" +
                    "----------------------------------------------------------------\n" +
                    "ट्रॉय के फाटक पर सब उस लकड़ी के घोड़े को नगर में खींच लाना चाहते थे। केवल लाओकोन ने मना किया।\n" +
                    "उसने अपना भाला घोड़े के पेट में घोंपा — और भीतर धातु के टकराने की आवाज़ सुनी।\n" +
                    "यह उपकरण वही करता है: किसी विंडो को छेदो और सुनो कि उसके भीतर क्या छिपा है।\n" +
                    "----------------------------------------------------------------\n\n" +
                    "1. F2 दबाएँ (या ऊपर बाएँ «निशाना» पर क्लिक करें)\n" +
                    "2. कर्सर क्रॉसहेयर बन जाएगा — किसी संदिग्ध विंडो पर ले जाएँ; क्लास और शीर्षक तुरंत दिखेंगे\n" +
                    "3. बाएँ क्लिक से स्थिर करें; पूरी रिपोर्ट तुरंत दिखेगी\n" +
                    "4. दायाँ क्लिक या Esc से निशाना रद्द करें\n\n" +
                    "क्या-क्या एकत्र होता है:\n" +
                    "  · विंडो: क्लास, शीर्षक, शैली बिट, आयत, हमेशा सामने / DWM छिपी / लेयर्ड स्थिति\n" +
                    "  · प्रोसेस: पूरा पथ, कमांड लाइन, जनक प्रोसेस, आर्किटेक्चर, उन्नयन, प्रारंभ समय\n" +
                    "  · फ़ाइल: आकार, तिथियाँ, MD5 / SHA1 / SHA256, डिजिटल हस्ताक्षर\n" +
                    "  · प्रोसेस द्वारा लोड की गई सभी DLL\n" +
                    "  · इस प्रोसेस की अन्य शीर्ष-स्तरीय विंडो (छिपी हुई सहित)\n" +
                    "  · लक्ष्य विंडो का चाइल्ड कंट्रोल वृक्ष (टेक्स्ट के साथ)\n\n" +
                    "सुझाव: व्यवस्थापक के रूप में चलाएँ, तभी लगभग हर प्रोसेस का पथ और मॉड्यूल पढ़ सकेंगे।\n"
            });
        }

        // ================= Italiano =================
        static Dictionary<string, string> It()
        {
            return D(new string[] {
                "app.brand", "Laocoön",
                "app.title", "Laocoön · Sonda finestre",

                "btn.aim", "◎  Mira (F2)",
                "btn.aim.cancel", "Mira… Esc per annullare",
                "btn.copytext", "Copia testo",
                "btn.save", "Salva rapporto",
                "btn.openloc", "Apri percorso file",
                "btn.clear", "Pulisci",
                "st.lang", "Lingua",

                "st.ready", "Pronto — premi F2 per mirare",
                "st.cleared", "Pulito",
                "st.aimhint", "Sposta il cursore sulla finestra bersaglio; clic sinistro fissa, clic destro / Esc annulla",
                "st.target", "Bersaglio: {0}   \"{1}\"   PID {2}",
                "st.aimcancel", "Mira annullata",
                "st.nohit", "Nessuna finestra catturata, riprova",
                "st.collecting", "Raccolta in corso…",
                "st.done", "Fatto: {0}   PID {1}   handle {2}",
                "st.fail", "Raccolta non riuscita: {0}",
                "st.copiedtext", "Testo del rapporto copiato",
                "st.copiedsuffix", " ({0} caratteri)",
                "st.nocopy", "Niente da copiare",
                "st.copyfail", "Copia non riuscita: {0}",
                "st.empty", "Nessun dato raccolto finora",
                "st.saved", "Salvato: {0}",
                "st.savefail", "Salvataggio non riuscito: {0}",
                "st.nopath", "Nessun percorso file da aprire",
                "st.located", "Individuato in Esplora file: {0}",
                "st.openfail", "Apertura non riuscita: {0}",
                "st.nocapture", "Cattura prima una finestra",
                "st.langchanged", "Lingua dell'interfaccia: italiano",

                "dlg.save.title", "Salva rapporto di cattura",
                "dlg.filter", "Rapporto di testo (*.txt)|*.txt",
                "msg.captureerr", "Errore durante la cattura:\r\n{0}",
                "hud.notitle", "(senza titolo)",
                "hud.parent", "Finestra padre  {0}",
                "hud.top", "Finestra di primo livello",

                "common.yes", "sì",
                "common.no", "no",
                "common.bytes", "byte",
                "common.session", "Sessione {0}",
                "common.elevated", "Elevato (amministratore)",
                "common.normal", "Utente standard",

                "rep.header", " Laocoön · Sonda finestre   catturato il {0}",
                "rep.sec1", "----- [1] Finestra bersaglio -----",
                "rep.sec2", "----- [2] Processo proprietario -----",
                "rep.sec3", "----- [3] File e impronte -----",
                "rep.sec4", "----- [4] Firma digitale -----",
                "rep.sec5", "----- [5] Tutte le finestre di primo livello del processo ({0}) -----",
                "rep.sec6", "----- [6] Albero dei controlli figli ({0}) -----",
                "rep.sec7", "----- [7] Moduli caricati ({0}) -----",
                "rep.secflags", "----- [!] Indizi sospetti (euristica, non una conclusione) -----",
                "rep.trunc", " (troncato)",
                "rep.none", "  (nessuno)",
                "rep.nosig", "  Nessuna firma digitale incorporata (i file di sistema usano la firma di catalogo, non verificata qui)",
                "rep.sigvalid", "valida",
                "rep.siginvalid", "verifica non riuscita",
                "rep.visible", "  [visibile] ",
                "rep.hidden", "  [nascosta] ",
                "rep.istarget", "   <= questa cattura",
                "rep.nochild", "  (nessuna finestra figlia)",
                "rep.client", "area cliente {0} x {1}",
                "val.product", "   (prodotto {0})",

                "lbl.handle", "Handle",
                "lbl.class", "Classe finestra",
                "lbl.title", "Titolo",
                "lbl.hitctl", "Controllo sotto cursore",
                "lbl.ctltext", "Testo del controllo",
                "lbl.hitpath", "Percorso del controllo",
                "lbl.pos", "Posizione e dimensioni",
                "lbl.cursor", "Coordinate del mouse",
                "lbl.thread", "Thread",
                "lbl.monitor", "Monitor",
                "lbl.state", "Stato",
                "lbl.style", "Stile",
                "lbl.exstyle", "Stile esteso",
                "lbl.owner", "Finestra proprietaria",
                "lbl.pid", "PID",
                "lbl.pname", "Nome processo",
                "lbl.ppath", "Percorso completo",
                "lbl.cmd", "Riga di comando",
                "lbl.parent", "Processo padre",
                "lbl.arch", "Architettura",
                "lbl.elev", "Privilegi",
                "lbl.session", "Sessione / avvio",
                "lbl.company", "Azienda",
                "lbl.product", "Prodotto",
                "lbl.desc", "Descrizione",
                "lbl.fver", "Versione file",
                "lbl.copyright", "Copyright",
                "lbl.fsize", "Dimensione file",
                "lbl.fcreated", "Creato",
                "lbl.fmodified", "Modificato",
                "lbl.md5", "MD5",
                "lbl.sha1", "SHA1",
                "lbl.sha256", "SHA256",
                "lbl.note", "Nota",
                "lbl.sigstatus", "Stato",
                "lbl.signer", "Firmatario",
                "lbl.issuer", "Emittente",
                "lbl.expires", "Valida fino al",
                "lbl.chain", "Stato della catena",

                "fmt.state", "visibile={0}  attiva={1}  ridotta={2}  nascosta DWM={3}",

                "note.modtrunc", "Troppi moduli; mostrati solo i primi 400 di {0}",
                "note.moddenied", "Impossibile enumerare i moduli (accesso negato)",
                "note.moddenied2", "Impossibile enumerare i moduli (servono privilegi più alti — avvia come amministratore)",
                "note.bighash", "File oltre 512 MB; calcolo dell'hash saltato",
                "note.hashfail", "Calcolo dell'hash non riuscito: {0}",
                "note.sigerr", "Errore di verifica: {0}",

                "flag.temp", "L'eseguibile si trova in una cartella temporanea: {0}",
                "flag.downloads", "L'eseguibile si trova nella cartella «Download»",
                "flag.appdata", "L'eseguibile si trova nella cartella utente AppData",
                "flag.nosig", "Nessuna firma digitale incorporata trovata",
                "flag.badsig", "Verifica della firma digitale non riuscita",
                "flag.signer", "Firmatario: {0}",
                "flag.nocompany", "Nessun nome azienda nelle informazioni sulla versione",
                "flag.topmost", "Finestra sempre in primo piano (WS_EX_TOPMOST)",
                "flag.toolwin", "Finestra strumento (WS_EX_TOOLWINDOW) — di solito assente dalla barra delle applicazioni / Alt+Tab",
                "flag.layered", "Finestra a livelli (WS_EX_LAYERED) — usata spesso per sovrapposizioni e trasparenze",
                "flag.invisible", "La finestra bersaglio è attualmente invisibile",
                "flag.cloaked", "Finestra nascosta da DWM (Cloaked): invisibile ma ancora in esecuzione",
                "flag.notitle", "La finestra bersaglio non ha titolo",
                "flag.hidden", "Il processo ha altre {0} finestre di primo livello nascoste",
                "flag.recent", "Il file è stato creato negli ultimi 7 giorni ({0})",

                "welcome",
                    "Laocoön · Sonda finestre\n" +
                    "----------------------------------------------------------------\n" +
                    "Davanti a Troia tutti volevano trascinare quel cavallo di legno dentro le mura. Solo Laocoonte si rifiutò.\n" +
                    "Confisse la lancia nel ventre — e sentì armi urtarsi all'interno.\n" +
                    "Questo strumento fa lo stesso: punge una finestra e ascolta cosa nasconde.\n" +
                    "----------------------------------------------------------------\n\n" +
                    "1. Premi F2 (oppure clicca «Mira» in alto a sinistra)\n" +
                    "2. Il cursore diventa un mirino — spostalo su una finestra sospetta: classe e titolo compaiono subito\n" +
                    "3. Clic sinistro per fissare; il rapporto completo appare subito\n" +
                    "4. Clic destro o Esc annulla la mira\n\n" +
                    "Cosa viene raccolto:\n" +
                    "  · Finestra: classe, titolo, bit di stile, rettangolo, sempre in primo piano / nascosta DWM / a livelli\n" +
                    "  · Processo: percorso completo, riga di comando, processo padre, architettura, elevazione, ora di avvio\n" +
                    "  · File: dimensione, date, MD5 / SHA1 / SHA256, firma digitale\n" +
                    "  · Tutte le DLL caricate dal processo\n" +
                    "  · Le altre finestre di primo livello di quel processo (comprese quelle nascoste)\n" +
                    "  · L'albero dei controlli figli della finestra bersaglio (con i loro testi)\n\n" +
                    "Suggerimento: avvia come amministratore per leggere percorso e moduli di quasi ogni processo.\n"
            });
        }
    }
}

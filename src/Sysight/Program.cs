using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Win32;
using EnumerationOptions = System.IO.EnumerationOptions;

namespace Sysight;

static class App
{
    public const string Name = "Sysight";
    public const string Version = "1.0.0";
    public static string DataDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Name);
}

static class Program
{
    [STAThread]
    static void Main(string[] argv)
    {
        if (argv.Length > 0 && argv[0] == "--selftest")
        {
            var sb = new System.Text.StringBuilder();
            foreach (var (c, a) in new[] { ("sys", "{}"), ("procs", "{}"), ("services", "{}"), ("drivers", "{}"), ("startup", "{}"), ("net", "{}"), ("apps", "{}"), ("cleanTargets", "{}"), ("scan", "{\"path\":\"C:\\\\Windows\\\\Web\"}"), ("changes", "{}"), ("kill", "{\"pid\":4}"), ("svcAction", "{\"name\":\"wuauserv\",\"action\":\"stop\"}") })
            {
                var sw = Stopwatch.StartNew();
                try { var r = JsonSerializer.Serialize(Api.Handle(c, JsonNode.Parse(a).AsObject())); sb.AppendLine($"OK   {c,-13} {sw.ElapsedMilliseconds,6}ms {r.Length,8}b  {r[..Math.Min(220, r.Length)]}"); }
                catch (Exception ex) { sb.AppendLine($"FAIL {c,-13} {ex}"); }
            }
            File.WriteAllText(argv.Length > 1 ? argv[1] : "selftest.txt", sb.ToString());
            return;
        }
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(argv.Length > 1 && argv[0] == "--shots" ? argv[1] : null));
    }
}

class MainForm : Form
{
    readonly WebView2 web = new() { Dock = DockStyle.Fill };
    public MainForm(string shotsDir)
    {
        Text = App.Name + (Sys.IsAdmin ? " (Administrator)" : "");
        Width = 1400; Height = 880; StartPosition = FormStartPosition.CenterScreen;
        if (shotsDir != null) { FormBorderStyle = FormBorderStyle.None; ClientSize = new Size(1366, 768); }
        BackColor = Color.FromArgb(11, 13, 18);
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
        Controls.Add(web);
        Load += async (_, _) =>
        {
            var env = await CoreWebView2Environment.CreateAsync(null, Path.Combine(App.DataDir, "WebView"));
            await web.EnsureCoreWebView2Async(env);
            var st = web.CoreWebView2.Settings;
            st.AreDevToolsEnabled = false; st.IsStatusBarEnabled = false; st.AreDefaultContextMenusEnabled = false;
            web.CoreWebView2.WebMessageReceived += OnMessage;
            var ui = new Uri(Path.Combine(AppContext.BaseDirectory, "ui.html")).AbsoluteUri;
            if (shotsDir == null) { web.CoreWebView2.Navigate(ui); return; }
            web.ZoomFactor = 96.0 / DeviceDpi;
            Directory.CreateDirectory(shotsDir);
            int n = 0;
            foreach (var v in new[] { "overview", "procs", "drivers", "services", "startup", "storage", "clean", "changes" })
            {
                var done = new TaskCompletionSource();
                void h(object o, CoreWebView2NavigationCompletedEventArgs e) { web.CoreWebView2.NavigationCompleted -= h; done.TrySetResult(); }
                web.CoreWebView2.NavigationCompleted += h;
                web.CoreWebView2.Navigate($"{ui}?demo&n={n++}#{v}");
                await done.Task; await Task.Delay(1800);
                using var fs = File.Create(Path.Combine(shotsDir, $"{n:00}-{v}.png"));
                await web.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, fs);
            }
            Close();
        };
    }

    async void OnMessage(object s, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var msg = JsonNode.Parse(e.WebMessageAsJson);
        var id = msg["id"]?.GetValue<int>() ?? 0;
        var cmd = msg["cmd"]?.GetValue<string>();
        var args = msg["args"] as JsonObject ?? new JsonObject();
        object result; string error = null;
        try { result = await Task.Run(() => Api.Handle(cmd, args)); }
        catch (Exception ex) { result = null; error = ex.Message; }
        if (cmd == "elevate" && error == null) { Application.Exit(); return; }
        var json = JsonSerializer.Serialize(new { id, result, error });
        if (!IsDisposed) BeginInvoke(() => web.CoreWebView2?.PostWebMessageAsJson(json));
    }
}

static class Api
{
    public static object Handle(string cmd, JsonObject a) => cmd switch
    {
        "sys" => Sys.Info(),
        "procs" => Procs.List(),
        "kill" => Procs.Kill(a["pid"].GetValue<int>()),
        "services" => Svc.List(),
        "svcAction" => Svc.Action(a["name"].GetValue<string>(), a["action"].GetValue<string>()),
        "drivers" => Drv.List(),
        "startup" => Startup.List(),
        "startupToggle" => Startup.Toggle(a["hive"].GetValue<string>(), a["group"].GetValue<string>(), a["name"].GetValue<string>(), a["enable"].GetValue<bool>()),
        "net" => Net.List(),
        "apps" => Apps.List(),
        "uninstall" => Apps.Uninstall(a["cmd"].GetValue<string>()),
        "scan" => Scanner.Scan(a["path"].GetValue<string>(), a["fresh"]?.GetValue<bool>() ?? false),
        "cleanTargets" => Cleaner.Targets(),
        "clean" => Cleaner.Clean(a["id"].GetValue<string>()),
        "open" => Sys.Open(a["path"].GetValue<string>()),
        "elevate" => Sys.Elevate(),
        "changes" => Changes.List(),
        "undo" => Changes.Undo(a["id"].GetValue<string>()),
        "about" => new { name = App.Name, version = App.Version, admin = Sys.IsAdmin },
        _ => throw new Exception("unknown command " + cmd)
    };
}

static class Util
{
    static readonly ConcurrentDictionary<string, string> companyCache = new(StringComparer.OrdinalIgnoreCase);
    public static string Company(string path)
    {
        if (string.IsNullOrEmpty(path)) return "";
        return companyCache.GetOrAdd(path, p => { try { return FileVersionInfo.GetVersionInfo(p).CompanyName ?? ""; } catch { return ""; } });
    }
    public static string ExeFromCmd(string cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd)) return "";
        cmd = Environment.ExpandEnvironmentVariables(cmd.Trim());
        if (cmd.StartsWith("\"")) { var i = cmd.IndexOf('"', 1); return i > 0 ? cmd[1..i] : cmd.Trim('"'); }
        var idx = cmd.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        return idx > 0 ? cmd[..(idx + 4)] : cmd.Split(' ')[0];
    }
    public static string NormalizeDriverPath(string p)
    {
        if (string.IsNullOrEmpty(p)) return "";
        p = p.Replace(@"\??\", "");
        if (p.StartsWith(@"\SystemRoot\", StringComparison.OrdinalIgnoreCase)) p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), p[12..]);
        else if (p.StartsWith(@"System32\", StringComparison.OrdinalIgnoreCase)) p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), p);
        return p;
    }
    public static (string output, int code) Run(string exe, string args)
    {
        var psi = new ProcessStartInfo(exe, args) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        using var p = Process.Start(psi);
        var o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit(30000);
        return (o, p.ExitCode);
    }
}

static class Sys
{
    public static bool IsAdmin => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

    [StructLayout(LayoutKind.Sequential)] struct MEMSTAT { public uint len, load; public ulong total, avail, totPage, availPage, totVirt, availVirt, ext; }
    [DllImport("kernel32")] static extern bool GlobalMemoryStatusEx(ref MEMSTAT m);
    [DllImport("kernel32")] static extern bool GetSystemTimes(out long idle, out long kernel, out long user);
    static long pIdle, pKernel, pUser;
    static string cpuName; static int cores;

    public static double CpuPct()
    {
        GetSystemTimes(out var i, out var k, out var u);
        long di = i - pIdle, dt = (k - pKernel) + (u - pUser);
        pIdle = i; pKernel = k; pUser = u;
        return dt > 0 ? Math.Round(100.0 * (dt - di) / dt, 1) : 0;
    }

    public static object Info()
    {
        var m = new MEMSTAT { len = (uint)Marshal.SizeOf<MEMSTAT>() };
        GlobalMemoryStatusEx(ref m);
        if (cpuName == null)
        {
            try { using var q = new ManagementObjectSearcher("select Name,NumberOfCores from Win32_Processor"); foreach (var o in q.Get()) { cpuName = o["Name"]?.ToString().Trim(); cores = Convert.ToInt32(o["NumberOfCores"]); } } catch { cpuName = "CPU"; }
        }
        var disks = DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => new { name = d.Name, label = d.VolumeLabel, total = d.TotalSize, free = d.AvailableFreeSpace, fs = d.DriveFormat });
        return new
        {
            machine = Environment.MachineName, user = Environment.UserName, os = RuntimeInformation.OSDescription,
            cpuName, cores, threads = Environment.ProcessorCount, cpu = CpuPct(),
            ramTotal = m.total, ramUsed = m.total - m.avail, commitTotal = m.totPage, commitUsed = m.totPage - m.availPage,
            uptime = Environment.TickCount64 / 1000, isAdmin = IsAdmin, procCount = Process.GetProcesses().Length, disks
        };
    }

    public static object Open(string path)
    {
        if (File.Exists(path) || Directory.Exists(path)) Process.Start("explorer.exe", $"/select,\"{path}\"");
        return true;
    }

    public static object Elevate()
    {
        Process.Start(new ProcessStartInfo(Environment.ProcessPath) { UseShellExecute = true, Verb = "runas" });
        return true;
    }
}

static class Procs
{
    static Dictionary<int, (TimeSpan cpu, DateTime at)> last = new();
    public static object List()
    {
        var now = DateTime.UtcNow; var next = new Dictionary<int, (TimeSpan, DateTime)>();
        var list = new List<object>();
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                double cpu = 0; TimeSpan t = TimeSpan.Zero; string path = null;
                try { t = p.TotalProcessorTime; next[p.Id] = (t, now); if (last.TryGetValue(p.Id, out var l)) cpu = (t - l.cpu).TotalMilliseconds / Math.Max(1, (now - l.at).TotalMilliseconds) / Environment.ProcessorCount * 100; } catch { }
                try { path = p.MainModule?.FileName; } catch { }
                list.Add(new { pid = p.Id, name = p.ProcessName, mem = p.WorkingSet64, priv = p.PrivateMemorySize64, cpu = Math.Round(cpu, 1), cpuTotal = Math.Round(t.TotalSeconds), critical = IsCritical(p.Id, p.ProcessName), threads = p.Threads.Count, path, company = Util.Company(path), title = p.MainWindowTitle, session = p.SessionId });
            }
            catch { }
            finally { p.Dispose(); }
        }
        last = next;
        return list;
    }
    static readonly HashSet<string> critical = new(StringComparer.OrdinalIgnoreCase) {
        "System", "Idle", "Registry", "smss", "csrss", "wininit", "winlogon", "services", "lsass", "lsaiso", "svchost", "fontdrvhost",
        "dwm", "Memory Compression", "MemCompression", "sihost", "ctfmon", "MsMpEng", "NisSrv", "SecurityHealthService", "audiodg", "spoolsv", "WUDFHost", "dllhost", "conhost" };
    public static bool IsCritical(int pid, string name) => pid <= 4 || pid == Environment.ProcessId || critical.Contains(name);
    public static object Kill(int pid)
    {
        using var p = Process.GetProcessById(pid);
        if (IsCritical(pid, p.ProcessName)) throw new Exception($"{p.ProcessName} is a core Windows process. Ending it would crash or log off the PC, so Sysight blocks it.");
        p.Kill(); return true;
    }
}

static class Svc
{
    public static object List()
    {
        var list = new List<object>();
        using var q = new ManagementObjectSearcher("select Name,DisplayName,State,StartMode,StartName,PathName,ProcessId,Description from Win32_Service");
        foreach (ManagementObject o in q.Get())
        {
            var path = Util.ExeFromCmd(o["PathName"]?.ToString());
            var co = Util.Company(path);
            list.Add(new { name = o["Name"], display = o["DisplayName"], state = o["State"], start = o["StartMode"], account = o["StartName"], path, pid = o["ProcessId"], company = co, desc = o["Description"], thirdParty = !IsProtected(path, co) });
        }
        return list;
    }
    static readonly Dictionary<string, string> scStart = new(StringComparer.OrdinalIgnoreCase) { ["Auto"] = "auto", ["Manual"] = "demand", ["Disabled"] = "disabled", ["Boot"] = "boot", ["System"] = "system" };
    public static bool IsProtected(string path, string company) =>
        company.Contains("Microsoft") || (company == "" && path.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows), StringComparison.OrdinalIgnoreCase));

    public static object Action(string name, string action)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Any(c => "\"&|<>'%^".Contains(c))) throw new Exception("Invalid service name");
        using var q = new ManagementObjectSearcher($"select Name,DisplayName,State,StartMode,PathName from Win32_Service where Name='{name}'");
        var o = q.Get().Cast<ManagementObject>().FirstOrDefault() ?? throw new Exception("Service not found");
        var path = Util.ExeFromCmd(o["PathName"]?.ToString());
        if (IsProtected(path, Util.Company(path))) throw new Exception("This is a Windows service. Sysight only changes third-party services, so Windows stays bootable.");
        string display = o["DisplayName"]?.ToString(), state = o["State"]?.ToString(), mode = o["StartMode"]?.ToString();
        var args = action switch
        {
            "start" => $"start \"{name}\"", "stop" => $"stop \"{name}\"",
            "manual" => $"config \"{name}\" start= demand", "disable" => $"config \"{name}\" start= disabled", "auto" => $"config \"{name}\" start= auto",
            _ => throw new Exception("bad action")
        };
        var (outp, code) = Util.Run("sc.exe", args);
        if (code != 0) throw new Exception(code == 5 ? "Access denied. Use \u201cRestart as admin\u201d first." : outp.Trim().Split('\n').LastOrDefault(l => l.Trim().Length > 0)?.Trim() ?? "failed");
        var undo = action switch
        {
            "start" => new[] { $"sc\u001fstop \"{name}\"" },
            "stop" => new[] { $"sc\u001fstart \"{name}\"" },
            _ => new[] { $"sc\u001fconfig \"{name}\" start= {scStart.GetValueOrDefault(mode ?? "", "demand")}" }
        };
        if (action == "disable" && state == "Running") undo = undo.Append($"sc\u001fstart \"{name}\"").ToArray();
        Changes.Add("Service", display, action switch { "manual" => "Set to Manual", "disable" => "Disabled", "auto" => "Set to Automatic", "start" => "Started", _ => "Stopped" } + $" (was {mode}, {state})", undo);
        return outp;
    }
}

static class Drv
{
    static readonly string[] ring0Hw = { "msr", "winring0", "rtcore", "gdrv", "cpuz", "inpout", "physmem", "asio", "iqvw", "hwinfo", "aida", "speedfan", "throttlestop", "ene", "atillk", "nvoclock" };
    public static object List()
    {
        var list = new List<object>();
        using var q = new ManagementObjectSearcher("select Name,DisplayName,State,StartMode,PathName,ServiceType,Description from Win32_SystemDriver");
        foreach (ManagementObject o in q.Get())
        {
            var path = Util.NormalizeDriverPath(o["PathName"]?.ToString());
            var co = Util.Company(path);
            string signer = "";
            try { signer = X509Certificate.CreateFromSignedFile(path).Subject; var cn = signer.Split(',').FirstOrDefault(x => x.Trim().StartsWith("CN=")); signer = cn?.Trim()[3..] ?? signer; } catch { signer = File.Exists(path) ? "(catalog-signed)" : "(file missing)"; }
            var n = (o["Name"]?.ToString() ?? "").ToLowerInvariant();
            var flags = new List<string>();
            if (ring0Hw.Any(k => n.Contains(k) || Path.GetFileName(path).ToLowerInvariant().Contains(k))) flags.Add("direct hardware/MSR access");
            if (n.Contains("fs") || (o["Description"]?.ToString() ?? "").Contains("File System", StringComparison.OrdinalIgnoreCase)) flags.Add("filesystem");
            if (n.Contains("npcap") || n.Contains("pcap") || n.Contains("ndis") || n.Contains("wfp")) flags.Add("network filter");
            if (!File.Exists(path)) flags.Add("orphan");
            list.Add(new { name = o["Name"], display = o["DisplayName"], state = o["State"], start = o["StartMode"], path, company = co, signer, type = o["ServiceType"], thirdParty = !co.Contains("Microsoft"), flags });
        }
        return list;
    }
}

static class Startup
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string Run32Key = @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run";
    const string Approved = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\";

    static bool Enabled(RegistryKey hive, string group, string name)
    {
        using var k = hive.OpenSubKey(Approved + group);
        if (k?.GetValue(name) is byte[] b && b.Length > 0) return (b[0] & 1) == 0;
        return true;
    }
    public static object List()
    {
        var list = new List<object>();
        void reg(RegistryKey hive, string hiveName, string key, string group)
        {
            using var k = hive.OpenSubKey(key); if (k == null) return;
            foreach (var n in k.GetValueNames())
            {
                var cmd = k.GetValue(n)?.ToString(); var exe = Util.ExeFromCmd(cmd);
                list.Add(new { name = n, cmd, hive = hiveName, group, source = $"{hiveName}\\{key}", enabled = Enabled(hive, group, n), company = Util.Company(exe), exists = File.Exists(exe) });
            }
        }
        reg(Registry.CurrentUser, "HKCU", RunKey, "Run");
        reg(Registry.LocalMachine, "HKLM", RunKey, "Run");
        reg(Registry.LocalMachine, "HKLM", Run32Key, "Run32");
        void folder(string dir, RegistryKey hive, string hiveName)
        {
            if (!Directory.Exists(dir)) return;
            foreach (var f in Directory.GetFiles(dir).Where(f => !f.EndsWith("desktop.ini", StringComparison.OrdinalIgnoreCase)))
                list.Add(new { name = Path.GetFileName(f), cmd = f, hive = hiveName, group = "StartupFolder", source = dir, enabled = Enabled(hive, "StartupFolder", Path.GetFileName(f)), company = "", exists = true });
        }
        folder(Environment.GetFolderPath(Environment.SpecialFolder.Startup), Registry.CurrentUser, "HKCU");
        folder(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), Registry.LocalMachine, "HKLM");
        return list;
    }
    public static object Toggle(string hive, string group, string name, bool enable)
    {
        Set(hive, group, name, enable);
        Changes.Add("Startup", name, enable ? "Enabled at sign-in" : "Disabled at sign-in", $"startup\u001f{hive}\u001f{group}\u001f{name}\u001f{(enable ? 0 : 1)}");
        return true;
    }
    public static void Set(string hive, string group, string name, bool enable)
    {
        var root = hive == "HKLM" ? Registry.LocalMachine : Registry.CurrentUser;
        using var k = root.CreateSubKey(Approved + group, true) ?? throw new Exception("cannot open key (admin needed for HKLM)");
        var b = new byte[12]; b[0] = (byte)(enable ? 2 : 3);
        if (!enable) BitConverter.GetBytes(DateTime.Now.ToFileTimeUtc()).CopyTo(b, 4);
        k.SetValue(name, b, RegistryValueKind.Binary);
    }
}

static class Net
{
    public static object List()
    {
        var names = new Dictionary<int, string>();
        foreach (var p in Process.GetProcesses()) { names[p.Id] = p.ProcessName; p.Dispose(); }
        var (o, _) = Util.Run("netstat.exe", "-ano");
        var list = new List<object>();
        foreach (var line in o.Split('\n'))
        {
            var t = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (t.Length < 4 || (t[0] != "TCP" && t[0] != "UDP")) continue;
            int.TryParse(t[^1], out var pid);
            var state = t[0] == "TCP" && t.Length >= 5 ? t[3] : "";
            if (t[0] == "TCP" && (t[2] == "0.0.0.0:0" || t[2] == "[::]:0")) state = "LISTENING"; // some builds print CLOSED for listeners
            list.Add(new { proto = t[0], local = t[1], remote = t[2], state, pid, proc = names.GetValueOrDefault(pid, "?") });
        }
        return list;
    }
}

static class Apps
{
    public static object List()
    {
        var list = new List<object>(); var seen = new HashSet<string>();
        void scan(RegistryKey root, string path, string scope)
        {
            using var k = root.OpenSubKey(path); if (k == null) return;
            foreach (var sk in k.GetSubKeyNames())
            {
                using var a = k.OpenSubKey(sk); var name = a?.GetValue("DisplayName")?.ToString();
                if (string.IsNullOrEmpty(name) || a.GetValue("SystemComponent") is int sc && sc == 1 || !seen.Add(name)) continue;
                long size = a.GetValue("EstimatedSize") is int kb ? kb * 1024L : 0;
                list.Add(new { name, publisher = a.GetValue("Publisher"), version = a.GetValue("DisplayVersion"), size, installed = a.GetValue("InstallDate"), location = a.GetValue("InstallLocation"), uninstall = a.GetValue("QuietUninstallString") ?? a.GetValue("UninstallString"), scope });
            }
        }
        scan(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Uninstall", "machine");
        scan(Registry.LocalMachine, @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", "machine32");
        scan(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall", "user");
        return list;
    }
    public static object Uninstall(string cmd)
    {
        Process.Start(new ProcessStartInfo("cmd.exe", "/c \"" + cmd + "\"") { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden });
        return true;
    }
}

static class Scanner
{
    static readonly ConcurrentDictionary<string, long> dirSize = new(StringComparer.OrdinalIgnoreCase);

    static long Size(string dir)
    {
        long total = 0;
        try
        {
            var di = new DirectoryInfo(dir);
            foreach (var f in di.EnumerateFileSystemInfos("*", new EnumerationOptions { IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint }))
            {
                if (f is FileInfo fi) total += fi.Length;
                else total += Size(f.FullName);
            }
        }
        catch { }
        dirSize[dir] = total;
        return total;
    }

    public static object Scan(string path, bool fresh)
    {
        path = Path.GetFullPath(path);
        var di = new DirectoryInfo(path);
        var opts = new EnumerationOptions { IgnoreInaccessible = true, AttributesToSkip = 0 };
        var items = new ConcurrentBag<object>();
        var dirs = di.EnumerateDirectories("*", opts).ToList();
        Parallel.ForEach(dirs, new ParallelOptions { MaxDegreeOfParallelism = 8 }, d =>
        {
            bool reparse = d.Attributes.HasFlag(FileAttributes.ReparsePoint);
            long s = reparse ? 0 : (!fresh && dirSize.TryGetValue(d.FullName, out var c) ? c : Size(d.FullName));
            items.Add(new { name = d.Name, path = d.FullName, size = s, dir = true, modified = d.LastWriteTime, link = reparse });
        });
        foreach (var f in di.EnumerateFiles("*", opts))
            items.Add(new { name = f.Name, path = f.FullName, size = f.Length, dir = false, modified = f.LastWriteTime, link = false });
        return new { path, parent = di.Parent?.FullName, items };
    }
}

static class Cleaner
{
    [DllImport("shell32", CharSet = CharSet.Unicode)] static extern int SHEmptyRecycleBin(IntPtr h, string root, uint flags);
    [StructLayout(LayoutKind.Sequential)] struct RBINFO { public int cb; public long size; public long items; }
    [DllImport("shell32", CharSet = CharSet.Unicode)] static extern int SHQueryRecycleBin(string root, ref RBINFO i);

    static string L(string p) => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), p);
    static string R(string p) => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), p);
    static string U(string p) => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), p);

    static readonly (string id, string label, string why, string[] paths, bool admin)[] targets =
    {
        ("usertemp", "User temp files", "Leftovers from installers and apps", new[]{ Path.GetTempPath() }, false),
        ("wintemp", "Windows temp", "System temp files", new[]{ @"C:\Windows\Temp" }, true),
        ("wu", "Windows Update download cache", "Already-installed update packages", new[]{ @"C:\Windows\SoftwareDistribution\Download" }, true),
        ("npm", "npm cache", "Re-downloaded on demand", new[]{ L(@"npm-cache"), R(@"npm-cache") }, false),
        ("pip", "pip cache", "Re-downloaded on demand", new[]{ L(@"pip\cache") }, false),
        ("yarn", "Yarn / pnpm cache", "Re-downloaded on demand", new[]{ L(@"Yarn\Cache"), L(@"pnpm-cache"), L(@"pnpm\store") }, false),
        ("nuget", "NuGet HTTP cache", "Re-downloaded on demand", new[]{ L(@"NuGet\v3-cache"), L(@"NuGet\Cache") }, false),
        ("gradle", "Gradle caches", "Rebuilt on next build", new[]{ U(@".gradle\caches") }, false),
        ("hf", "Hugging Face cache", "Model downloads — re-downloaded on demand", new[]{ U(@".cache\huggingface") }, false),
        ("chrome", "Chrome cache", "Browser cache (not history/passwords)", new[]{ L(@"Google\Chrome\User Data\Default\Cache"), L(@"Google\Chrome\User Data\Default\Code Cache") }, false),
        ("edge", "Edge cache", "Browser cache (not history/passwords)", new[]{ L(@"Microsoft\Edge\User Data\Default\Cache"), L(@"Microsoft\Edge\User Data\Default\Code Cache") }, false),
        ("operagx", "Opera GX cache", "Browser cache", new[]{ L(@"Opera Software\Opera GX Stable\Cache"), R(@"Opera Software\Opera GX Stable\Cache") }, false),
        ("discord", "Discord cache", "App cache", new[]{ R(@"discord\Cache"), R(@"discord\Code Cache") }, false),
        ("crash", "Crash dumps & error reports", "Diagnostic leftovers", new[]{ L(@"CrashDumps"), @"C:\ProgramData\Microsoft\Windows\WER\ReportArchive", @"C:\ProgramData\Microsoft\Windows\WER\ReportQueue" }, false),
        ("thumbs", "Thumbnail cache", "Rebuilt automatically", new[]{ L(@"Microsoft\Windows\Explorer") }, false),
    };

    static long DirSize(string p)
    {
        if (!Directory.Exists(p)) return 0;
        try { return new DirectoryInfo(p).EnumerateFiles("*", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint }).Sum(f => { try { return f.Length; } catch { return 0L; } }); } catch { return 0; }
    }

    public static object Targets()
    {
        var list = targets.AsParallel().Select(t => (object)new { t.id, t.label, t.why, t.admin, paths = t.paths.Where(Directory.Exists).ToArray(), size = t.paths.Sum(DirSize) }).ToList();
        var rb = new RBINFO { cb = Marshal.SizeOf<RBINFO>() };
        SHQueryRecycleBin(null, ref rb);
        list.Add(new { id = "recycle", label = "Recycle Bin", why = "Files you already deleted", admin = false, paths = Array.Empty<string>(), size = rb.size });
        return list;
    }

    public static object Clean(string id)
    {
        if (id == "recycle") { SHEmptyRecycleBin(IntPtr.Zero, null, 0x7); return new { freed = 0L, failed = 0 }; }
        var t = targets.First(x => x.id == id);
        long freed = 0; int failed = 0;
        foreach (var root in t.paths.Where(Directory.Exists))
        {
            foreach (var f in new DirectoryInfo(root).EnumerateFiles("*", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint }))
            {
                try { var l = f.Length; if (id == "thumbs" && !f.Name.StartsWith("thumbcache_")) continue; f.Delete(); freed += l; } catch { failed++; }
            }
            if (id != "thumbs")
                foreach (var d in Directory.GetDirectories(root)) { try { Directory.Delete(d, true); } catch { } }
        }
        return new { freed, failed };
    }
}

record Change(string id, DateTime time, string kind, string target, string detail, string[] undo, bool undone);

static class Changes
{
    static readonly object gate = new();
    static string FilePath => Path.Combine(App.DataDir, "changes.json");
    static List<Change> Load() { try { return JsonSerializer.Deserialize<List<Change>>(File.ReadAllText(FilePath)) ?? new(); } catch { return new(); } }
    static void Save(List<Change> l) { Directory.CreateDirectory(App.DataDir); File.WriteAllText(FilePath, JsonSerializer.Serialize(l.Take(500))); }
    public static void Add(string kind, string target, string detail, params string[] undo)
    {
        lock (gate) { var l = Load(); l.Insert(0, new Change(Guid.NewGuid().ToString("N")[..8], DateTime.Now, kind, target, detail, undo, false)); Save(l); }
    }
    public static object List() { lock (gate) return Load(); }
    public static object Undo(string id)
    {
        lock (gate)
        {
            var l = Load(); var i = l.FindIndex(c => c.id == id);
            if (i < 0 || l[i].undone) throw new Exception("Nothing to undo");
            foreach (var u in l[i].undo)
            {
                var p = u.Split('\u001f');
                if (p[0] == "sc")
                {
                    var (o, code) = Util.Run("sc.exe", p[1]);
                    if (code != 0 && code != 1056 && code != 1062) throw new Exception(code == 5 ? "Access denied. Use \u201cRestart as admin\u201d first." : o.Trim());
                }
                else if (p[0] == "startup") Startup.Set(p[1], p[2], p[3], p[4] == "1");
            }
            l[i] = l[i] with { undone = true }; Save(l);
            return true;
        }
    }
}

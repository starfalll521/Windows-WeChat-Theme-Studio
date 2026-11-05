using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

internal static class ThemeReset {
    internal static bool NeedsElevation() {
        try { return Engine.Hash(File.ReadAllBytes(Engine.Target)) != Engine.OriginalHash; }
        catch (FileNotFoundException) { return false; }
    }

    internal static void Run() {
        Engine.RequireStopped();
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string store = Path.Combine(local, "WeixinThemeStudio");
        if (!String.Equals(store, Engine.Store, StringComparison.OrdinalIgnoreCase))
            throw new IOException("只允许清理本机调色室配置目录。");
        var oldFolders = Directory.GetDirectories(local, "WeixinThemeStudio.reset-*");
        AutoBackground.SetEnabled(false);
        foreach (string name in new[] {"WeixinThemeStudioProcessMonitor", "WeixinThemeStudioBackgroundWatcher"})
            using (var mutex = new Mutex(false, @"Local\" + name)) {
                bool held = false;
                try {
                    try { held = mutex.WaitOne(20000); } catch (AbandonedMutexException) { held = true; }
                    if (!held) throw new IOException("旧版主题后台尚未退出，请稍后再试。");
                } finally { if (held) mutex.ReleaseMutex(); }
            }
        Engine.RequireStopped();
        if (NeedsElevation()) Engine.Apply(null, true);
        RemoveShortcuts(store);
        // Detach the entire store so new versions' files are covered without maintaining a filename list.
        string detached = store + ".reset-" + Guid.NewGuid().ToString("N");
        Directory.Move(store, detached);
        foreach (string path in oldFolders.Concat(new[] {detached})) DeleteTree(path);
        WeixinLocator.Reset();
    }

    static void DeleteTree(string path) {
        var attrs = File.GetAttributes(path);
        if ((attrs & FileAttributes.Directory) == 0) { File.Delete(path); return; }
        if ((attrs & FileAttributes.ReparsePoint) == 0)
            foreach (string child in Directory.GetFileSystemEntries(path)) DeleteTree(child);
        Directory.Delete(path);
    }

    static void RemoveShortcuts(string store) {
        const string name = "微信（主题模式）.lnk";
        var links = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in new[] {Environment.SpecialFolder.DesktopDirectory, Environment.SpecialFolder.Programs, Environment.SpecialFolder.Startup}) {
            string dir = Environment.GetFolderPath(folder);
            if (Directory.Exists(dir)) links.Add(Path.Combine(dir, name));
        }
        foreach (string config in new[] {"state.json", "prepared.json"}) {
            string path = Path.Combine(NativeDeployment.Home, config);
            if (!File.Exists(path)) continue;
            NativeDeploymentState state;
            try { state = NativeDeployment.Read<NativeDeploymentState>(path); }
            catch (System.Runtime.Serialization.SerializationException) { continue; }
            if (state != null && state.Shortcuts != null)
                foreach (string link in state.Shortcuts)
                    if (Path.IsPathRooted(link) && Path.GetFileName(link) == name) links.Add(link);
        }
        object shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
        try {
            foreach (string link in links.Where(File.Exists)) {
                object shortcut = shell.GetType().InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[]{link});
                try {
                    string target = (string)shortcut.GetType().InvokeMember("TargetPath", BindingFlags.GetProperty, null, shortcut, null);
                    if (target.StartsWith(store + @"\native-deployment\", StringComparison.OrdinalIgnoreCase)
                        && Path.GetFileName(target) == "微信主题启动器.exe") File.Delete(link);
                } finally { Marshal.FinalReleaseComObject(shortcut); }
            }
        } finally { Marshal.FinalReleaseComObject(shell); }
    }
}

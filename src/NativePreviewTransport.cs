using System;using System.IO;using System.Diagnostics;using System.Runtime.InteropServices;
internal static partial class NativeBackground{
 [DllImport("user32.dll",SetLastError=true)]static extern bool PostMessageW(IntPtr window,uint message,IntPtr wparam,IntPtr lparam);
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
 static readonly object nativePresenceGate=new object();static DateTime nativePresenceAt;static bool nativePresence;
 internal static bool InternalModulePresent(){lock(nativePresenceGate){if(DateTime.UtcNow-nativePresenceAt<TimeSpan.FromSeconds(1))return nativePresence;nativePresenceAt=DateTime.UtcNow;nativePresence=false;foreach(var p in Process.GetProcessesByName("Weixin"))using(p)try{foreach(ProcessModule m in p.Modules)if(m.ModuleName=="qttestability.dll"){nativePresence=true;return true;}}catch{}return false;}}
 internal static void RequestNativePreview(string runtimeDirectory){
  foreach(var p in Process.GetProcessesByName("Weixin"))using(p){if(p.MainWindowHandle==IntPtr.Zero)continue;foreach(ProcessModule m in p.Modules){if(m.ModuleName!="qttestability.dll"||!String.Equals(Path.GetDirectoryName(m.FileName),runtimeDirectory,StringComparison.OrdinalIgnoreCase))continue;
   string ready=Path.Combine(runtimeDirectory,"control-ready.txt");if(!File.Exists(ready))throw new IOException("微信尚未建立配置消息通道，请保持主窗口打开后重试。");
   int owner=0;long handle=0;foreach(string line in File.ReadAllLines(ready)){if(line.StartsWith("pid="))int.TryParse(line.Substring(4),out owner);if(line.StartsWith("window="))long.TryParse(line.Substring(7),out handle);}
   uint actual;var window=new IntPtr(handle);GetWindowThreadProcessId(window,out actual);if(owner!=p.Id||actual!=(uint)p.Id)throw new IOException("配置通道已失效，请重新连接预览。");
   if(!PostMessageW(window,0x84e2,IntPtr.Zero,IntPtr.Zero))throw Error("发送主题配置消息");return;
  }}throw new IOException("预览进程已退出，请重新试启动并连接。");
 }
}

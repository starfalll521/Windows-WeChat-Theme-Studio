using System;using System.IO;using System.Runtime.InteropServices;
internal static partial class AutoBackground {
 [DllImport("kernel32.dll")]static extern ulong GetTickCount64();
 static string ExitMarker{get{return Path.Combine(Engine.Store,"exit-this-session.txt");}}
 internal static bool ExitApplies(long exitedTicks,long nowTicks,ulong uptimeMilliseconds){return exitedTicks<=nowTicks&&nowTicks-exitedTicks<(double)uptimeMilliseconds*TimeSpan.TicksPerMillisecond;}
 internal static bool SessionExitRequested{get{try{return ExitApplies(long.Parse(File.ReadAllText(ExitMarker)),DateTime.UtcNow.Ticks,GetTickCount64());}catch{return false;}}}
 internal static void RequestSessionExit(){Directory.CreateDirectory(Engine.Store);File.WriteAllText(ExitMarker,DateTime.UtcNow.Ticks.ToString());AppLog.Event("EXIT","Exit for this session; auto-start preference preserved");}
 internal static void ResumeSession(){if(File.Exists(ExitMarker))File.Delete(ExitMarker);launchAttempt=DateTime.MinValue;}
}

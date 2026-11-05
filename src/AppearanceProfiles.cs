using System;using System.IO;using System.Text;using System.Linq;using System.Drawing;
internal static class AppearanceProfiles{
 public static readonly string[] Keys={"ink-text.txt","ink-chat.txt","ink-secondary.txt","ink-canvas.txt","ink-icons.txt","ink-accent.txt","ink-link.txt","icon-effects.txt","input-glass.txt","input-border.txt","background-effects.txt"};
 [ThreadStatic]static int? overrideMode;
 public static int? EditingMode;
 static string ObservedFile{get{return System.IO.Path.Combine(Engine.Store,"appearance-observed.txt");}}
 public static int Mode{get{if(overrideMode.HasValue)return overrideMode.Value;if(EditingMode.HasValue)return EditingMode.Value;try{string s=File.ReadAllText(ObservedFile).Trim();if(s=="0"||s=="1")return int.Parse(s);}catch{}return LegacyMode;}}
 static int LegacyMode{get{try{return File.ReadAllText(System.IO.Path.Combine(Engine.Store,"runtime-appearance.txt")).Trim()=="light"?0:1;}catch{return 1;}}}
 public static string DirectoryFor(int mode){if(mode!=0&&mode!=1)throw new ArgumentOutOfRangeException("mode");return System.IO.Path.Combine(Engine.Store,"appearance-"+(mode==0?"light":"dark"));}
 public static string Path(string key){if(!Keys.Contains(key))throw new ArgumentException("Unknown appearance setting");string dir=DirectoryFor(Mode);Directory.CreateDirectory(dir);string marker=System.IO.Path.Combine(dir,"profile.ready");if(!File.Exists(marker))return System.IO.Path.Combine(Engine.Store,key);return System.IO.Path.Combine(dir,key);}
 public static void Observe(int mode){if(mode<0||mode>1)return;Directory.CreateDirectory(Engine.Store);string s=mode.ToString();if(!File.Exists(ObservedFile)||File.ReadAllText(ObservedFile).Trim()!=s)File.WriteAllText(ObservedFile,s,Encoding.UTF8);}
 sealed class Scope:IDisposable{readonly int? before;public Scope(int mode){before=overrideMode;overrideMode=mode;}public void Dispose(){overrideMode=before;}}
 public static IDisposable Use(int mode){return new Scope(mode);}
 public static void EnsureProfiles(){for(int mode=0;mode<2;mode++){string dir=DirectoryFor(mode);Directory.CreateDirectory(dir);string marker=System.IO.Path.Combine(dir,"profile.ready");if(File.Exists(marker))continue;foreach(string key in Keys){string source=System.IO.Path.Combine(Engine.Store,key);if(mode==LegacyMode&&File.Exists(source))File.Copy(source,System.IO.Path.Combine(dir,key),true);}
   if(mode!=LegacyMode){Color ink=mode==0?Color.FromArgb(27,30,34):Color.FromArgb(244,245,247);foreach(string key in new[]{"ink-text.txt","ink-chat.txt"})File.WriteAllText(System.IO.Path.Combine(dir,key),SmartPalette.Hex(ink));File.WriteAllText(System.IO.Path.Combine(dir,"ink-icons.txt"),mode==0?"#343B44":"#E0E6ED");File.WriteAllText(System.IO.Path.Combine(dir,"ink-accent.txt"),mode==0?"#285B8A":"#8EBFF0");File.WriteAllText(System.IO.Path.Combine(dir,"ink-link.txt"),mode==0?"#245D9C":"#A7CCFF");File.WriteAllText(System.IO.Path.Combine(dir,"icon-effects.txt"),"2|100|100|100|85|25");File.WriteAllText(System.IO.Path.Combine(dir,"input-border.txt"),mode==0?"1|72|10|#65758A|1":"1|72|10|#B8C9DD|1");File.WriteAllText(System.IO.Path.Combine(dir,"background-effects.txt"),new EffectSettings().Key);}
   File.WriteAllText(marker,"1");}}
 public static void Clear(){foreach(int mode in new[]{0,1}){string dir=DirectoryFor(mode);foreach(string key in Keys.Concat(new[]{"profile.ready"})){string p=System.IO.Path.Combine(dir,key);if(File.Exists(p))File.Delete(p);}}if(File.Exists(ObservedFile))File.Delete(ObservedFile);}
}

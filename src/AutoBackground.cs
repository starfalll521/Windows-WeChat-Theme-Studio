using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

internal static partial class AutoBackground {
 static string Flag {get{return Path.Combine(Engine.Store,"auto-background.enabled");}}
 public static bool Enabled {get{return File.Exists(Flag);}}
 public static string SavedImage(){string file=Path.Combine(Engine.Store,"background.txt");if(!File.Exists(file))return null;string path=File.ReadAllText(file,Encoding.UTF8).Trim(),dir=Path.GetFullPath(Path.Combine(Engine.Store,"backgrounds"))+Path.DirectorySeparatorChar;if(!Path.GetFullPath(path).StartsWith(dir,StringComparison.OrdinalIgnoreCase)||!Regex.IsMatch(Path.GetFileName(path),"^[a-f0-9]{64}\\.png$")||!File.Exists(path))return null;return path;}
 public static void SetEnabled(bool enabled){Directory.CreateDirectory(Engine.Store);using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")){if(enabled){ResumeSession();key.SetValue("WeixinThemeStudioBackground","\""+Application.ExecutablePath+"\" --monitor",RegistryValueKind.String);File.WriteAllText(Flag,"1");}else{key.DeleteValue("WeixinThemeStudioBackground",false);if(File.Exists(Flag))File.Delete(Flag);}}if(enabled)EnsureStarted();}
 public static void Run(){using(var single=new Mutex(false,"Local\\WeixinThemeStudioBackgroundWatcher")){bool held=false;try{try{held=single.WaitOne(30000);}catch(AbandonedMutexException){held=true;}if(!held){AppLog.Event("WORKER","Startup timeout waiting for previous worker; reopening Studio can retry");return;}using(var context=new WatchContext())Application.Run(context);}finally{if(held)single.ReleaseMutex();}}}
 sealed class WatchContext:ApplicationContext {
  readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();readonly NotifyIcon tray;readonly TrayRecovery trayRecovery;bool busy,stopping;string observed,lastApplied;DateTime stableAt,retryAfter,nextHealth;string serviceState="正在等待微信打开";
  public WatchContext(){var menu=new ContextMenuStrip();menu.Items.Add("打开微信调色室",null,delegate{Process.Start(Application.ExecutablePath);});menu.Items.Add("退出主题后台（下次打开微信恢复）",null,delegate{RequestStop(RequestSessionExit);});tray=new NotifyIcon{Icon=SystemIcons.Application,Text="微信调色室 · 自动原生背景",Visible=true,ContextMenuStrip=menu};trayRecovery=new TrayRecovery(tray);SaveHealth("后台已启动，正在等待微信");AppLog.Event("TRAY","Created independent background tray");tray.DoubleClick+=delegate{Process.Start(Application.ExecutablePath);};timer.Interval=200;timer.Tick+=Tick;timer.Start();}
  async void Tick(object sender,EventArgs args){if(stopping){if(!busy)ExitThread();return;}if(busy)return;if(WeixinIdentity()==""||SessionExitRequested||!Enabled||!String.Equals(ActiveWorkerPath,Application.ExecutablePath,StringComparison.OrdinalIgnoreCase)){ExitThread();return;}SaveHealth(serviceState);if(DateTime.UtcNow<retryAfter)return;busy=true;try{await Task.Run(()=>{int? actual=NativeBackground.ActualAppearance();if(actual.HasValue)AppearanceProfiles.Observe(actual.Value);string view=NativeBackground.Fingerprint();NativeBackground.SyncNativePalette(true);string source=SavedImage();if(source==null){observed=null;lastApplied=null;serviceState="没有保存的壁纸，请先选图并应用";return;}if(view==null){NativeBackground.SyncNativePalette(false);observed=null;lastApplied=null;serviceState="等待微信主窗口就绪";return;}string key=ConfigurationKey(source)+"|"+view;if(observed!=key){observed=key;stableAt=DateTime.UtcNow;if(lastApplied!=null)return;}if(lastApplied==key){if(DateTime.UtcNow<nextHealth)return;nextHealth=DateTime.UtcNow.AddSeconds(1);if(NativeBackground.RuntimeReady(source))return;lastApplied=null;}if(lastApplied!=null&&DateTime.UtcNow-stableAt<TimeSpan.FromMilliseconds(150))return;if(!Enabled)return;serviceState="正在应用保存的壁纸与文字设置";SaveHealth(serviceState);string result=NativeBackground.Apply(source);lastApplied=key;serviceState="已应用到微信 · "+DateTime.Now.ToString("HH:mm:ss");SaveHealth(serviceState);WriteLog("APPLIED "+view+" whole="+NativeBackground.WholeEnabled+"\n"+result);});}catch(Exception e){try{NativeBackground.SyncNativePalette(false);}catch{}retryAfter=DateTime.UtcNow.AddSeconds(e is InvalidDataException?30:2);serviceState="等待重试："+e.Message;SaveHealth(serviceState);WriteLog("WAITING: "+e.Message);}finally{busy=false;if(stopping)ExitThread();}}
  void RequestStop(Action disable){if(stopping)return;try{disable();}catch(Exception e){AppLog.Error("STOP",e);MessageBox.Show("退出失败，后台尚未退出："+e.Message,"微信调色室");return;}stopping=true;timer.Stop();tray.Visible=false;AppLog.Event("STOP",busy?"Stop requested; waiting for in-flight operation before exit":"Stop requested; exiting worker");if(!busy)ExitThread();}
  static void WriteLog(string text){AppLog.Event("WORKER",text);try{File.WriteAllText(Path.Combine(Engine.Store,"auto-background.log"),DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+"\n"+text,Encoding.UTF8);}catch{}}
  protected override void ExitThreadCore(){try{NativeBackground.SyncNativePalette(false);}catch{}timer.Stop();tray.Visible=false;base.ExitThreadCore();}
  protected override void Dispose(bool disposing){if(disposing){timer.Dispose();trayRecovery.Dispose();tray.Dispose();}base.Dispose(disposing);}
 }
}







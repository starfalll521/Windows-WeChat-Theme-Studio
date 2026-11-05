using System;using System.IO;using System.Linq;using System.Text;using System.Diagnostics;using System.Threading;using System.Windows.Forms;
internal static class NativePreviewBridge{
 static readonly object gate=new object();static string runtime,ownPreview;
 internal static string LastSnapshot{get;private set;}
 internal static bool Connected{get{return runtime!=null;}}
 internal static void Connect(){
  string expected=File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"native-runtime","module.sha256")).Trim();
  foreach(var p in Process.GetProcessesByName("Weixin"))using(p){if(p.MainWindowHandle==IntPtr.Zero)continue;foreach(ProcessModule m in p.Modules)if(m.ModuleName=="qttestability.dll"){
   var bytes=File.ReadAllBytes(m.FileName);if(Engine.Hash(bytes)!=expected||!Encoding.ASCII.GetString(bytes).Contains("ThemePreviewProtocol\0"))throw new IOException("当前微信加载的是旧模块，请从托盘退出微信后，用新版第 1 步重新试启动。");
   if(AutoBackground.Enabled)AutoBackground.SetEnabled(false);runtime=Path.GetDirectoryName(m.FileName);return;
  }}throw new IOException("微信主窗口没有加载新版主题模块，请先完成试启动并登录。");
 }
 internal static bool TryApply(string source,out string message){
  message=null;if(!Connected)return false;
  lock(gate){
   if(AutoBackground.Enabled)AutoBackground.SetEnabled(false);
   string snapshot=NativeDeployment.Capture(Engine.Current(),source,true),dir=Path.Combine(NativeDeployment.Home,"previews",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
   var info=new ProcessStartInfo(Application.ExecutablePath,"--native-export \""+dir+"\" \""+snapshot+"\""){UseShellExecute=false,CreateNoWindow=true};
   using(var child=Process.Start(info)){if(!child.WaitForExit(60000)||child.ExitCode!=0)throw new IOException("整套预览配置生成失败；未发送给微信。");}
   string control=Path.Combine(runtime,"preview.path"),previous=File.Exists(control)?File.ReadAllText(control):null;
   try{
    SetControl(control,Path.Combine(dir,"runtime.ini"));
    NativeBackground.RequestNativePreview(runtime);
    string receipt=Path.Combine(dir,"applied.receipt");bool acknowledged=false;int actualMode=-1;
    for(int i=0;i<50;i++){
     if(File.Exists(receipt)){string ack=File.ReadAllText(receipt);if(ack.Contains("state=failed"))throw new IOException("微信未完整应用主题，已退回上一次配置；请导出诊断报告。");if(ack.Contains("state=applied")&&ack.Contains("paletteMatches=8")){actualMode=ack.Contains("mode=1")?1:0;acknowledged=true;break;}}
     Thread.Sleep(200);
    }
    if(!acknowledged)throw new IOException("微信没有确认应用，不能将发送成功当作应用成功。请保持主窗口可见后重试。");
    RememberWallpaper(source);ownPreview=Path.Combine(dir,"runtime.ini");LastSnapshot=snapshot;AppLog.Event("NATIVE-PREVIEW","Applied and acknowledged "+Path.GetFileName(dir));
    message=(source==null?"本次未设置壁纸，仅应用配色与其他设置。":"")+"微信已确认"+(actualMode==0?"浅色":"深色")+"外观的 8 项基础颜色和壁纸资源，并已请求刷新界面。已部署方案保持不变。";string canvasFile=Path.Combine(snapshot,"appearance-"+(actualMode==0?"light":"dark"),"ink-canvas.txt");if(File.Exists(canvasFile)&&File.ReadAllText(canvasFile).Trim()=="auto")message+=" 智能昵称取色尚未迁入，目前使用主文字色。";if(AppearanceProfiles.EditingMode.HasValue&&AppearanceProfiles.EditingMode.Value!=actualMode)message+=" 本次编辑的是另一种外观，请切换微信外观后查看该组设置。";return true;
   }catch(Exception e){
    LastSnapshot=null;if(previous==null){if(File.Exists(control))File.Delete(control);}else SetControl(control,previous);
    try{NativeBackground.RequestNativePreview(runtime);}catch{}AppLog.Error("NATIVE-PREVIEW",e);throw;
   }
  }
 }
 internal static void RememberWallpaper(string source){
  string cfg=Path.Combine(Engine.Store,"background.txt");
  if(source==null){if(File.Exists(cfg))File.Delete(cfg);return;}
  string cached=NativeBackground.CacheImage(source);Directory.CreateDirectory(Engine.Store);
  File.WriteAllText(cfg,cached,Encoding.UTF8);
 }
 static void SetControl(string file,string text){string temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";File.WriteAllText(temp,text,Encoding.Unicode);if(File.Exists(file))File.Replace(temp,file,null);else File.Move(temp,file);}
 internal static void Disconnect(){lock(gate){if(runtime!=null){string path=Path.Combine(runtime,"preview.path");if(File.Exists(path)&&File.ReadAllText(path).Trim()==ownPreview){File.Delete(path);try{NativeBackground.RequestNativePreview(runtime);}catch{}}}runtime=null;ownPreview=null;LastSnapshot=null;}}
}

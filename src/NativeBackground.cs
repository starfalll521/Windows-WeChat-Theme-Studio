using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

// Version-scoped runtime module. Uses Weixin's own Qt image/brush constructors.
// No overlay, screen capture, global filter, account access, or disk binary patches.
internal static partial class NativeBackground {
 const long CallRva=0x20ff70a, BrushColorRva=0xcda290, BrushImageRva=0xcda080,
 ImageFileRva=0x4fd250, RefreshRva=0x20ff6c0, VtableRva=0x93ef288, FilterRva=0x3d2ec30;
 const int BlockSize=786432;
 [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr a,byte[] b,int n,out IntPtr count);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h,IntPtr a,byte[] b,int n,out IntPtr count);
 [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr VirtualAllocEx(IntPtr h,IntPtr a,UIntPtr size,uint type,uint protect);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool VirtualProtectEx(IntPtr h,IntPtr a,UIntPtr size,uint protect,out uint old);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool FlushInstructionCache(IntPtr h,IntPtr a,UIntPtr size);
 [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
 [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr CreateRemoteThread(IntPtr h,IntPtr attributes,UIntPtr stackSize,IntPtr start,IntPtr argument,uint flags,out uint threadId);
 [DllImport("kernel32.dll")] static extern uint WaitForSingleObject(IntPtr h,uint milliseconds);
 [DllImport("kernel32.dll")] static extern bool GetExitCodeThread(IntPtr h,out uint code);
 static long cachedInstance,cachedVtable,cachedImageBase;static DateTime verifiedWrite;static bool verified;static string verifiedPath;
 static void ValidateFile(){var info=new FileInfo(Engine.Target);if(!verified||verifiedPath!=info.FullName||info.LastWriteTimeUtc!=verifiedWrite){Engine.Normalize(File.ReadAllBytes(Engine.Target));verifiedWrite=info.LastWriteTimeUtc;verifiedPath=info.FullName;verified=true;}}
 [StructLayout(LayoutKind.Sequential)] struct MemoryInfo {public long BaseAddress,AllocationBase;public uint AllocationProtect,Alignment;public ulong RegionSize;public uint State,Protect,Type,Alignment2;}
 [DllImport("kernel32.dll",SetLastError=true)] static extern UIntPtr VirtualQueryEx(IntPtr h,IntPtr at,out MemoryInfo info,UIntPtr size);
 static Exception Error(string op){return new IOException(op+": Win32 "+Marshal.GetLastWin32Error());}
 static void RuntimeLog(string path,string message,bool append){try{if(append)File.AppendAllText(path,message,Encoding.UTF8);else File.WriteAllText(path,message,Encoding.UTF8);}catch{}}
 static byte[] Read(IntPtr h,long a,int n){var b=new byte[n];IntPtr count;if(!ReadProcessMemory(h,new IntPtr(a),b,n,out count)||count.ToInt64()!=n)throw Error("ReadProcessMemory");return b;}
 static void Write(IntPtr h,long a,byte[] b){IntPtr count;if(!WriteProcessMemory(h,new IntPtr(a),b,b.Length,out count)||count.ToInt64()!=b.Length)throw Error("WriteProcessMemory");}
 static void Patch(IntPtr h,long a,byte[] b){uint old;if(!VirtualProtectEx(h,new IntPtr(a),(UIntPtr)b.Length,0x40,out old))throw Error("VirtualProtectEx");try{Write(h,a,b);if(!FlushInstructionCache(h,new IntPtr(a),(UIntPtr)b.Length))throw Error("FlushInstructionCache");}finally{uint ignored;VirtualProtectEx(h,new IntPtr(a),(UIntPtr)b.Length,old,out ignored);}}
 static byte[] Call(long from,long to){long delta=to-from-5;if(delta<int.MinValue||delta>int.MaxValue)throw new Exception("Relative call outside range");return new[]{(byte)0xe8}.Concat(BitConverter.GetBytes((int)delta)).ToArray();}
 static bool Equal(byte[] a,byte[] b){return a.SequenceEqual(b);}
 static long FindLiveVtable(IntPtr h,long image,out long instance){
  instance=0;
  if(cachedImageBase==image&&cachedInstance!=0&&ValidChatViewport(h,image,cachedInstance,cachedVtable)){instance=cachedInstance;return cachedVtable;}if(cachedImageBase!=image)cachedChatRoot=0;long treeTable,treeInstance;if(FindChatInTree(h,image,out treeInstance,out treeTable)){instance=treeInstance;cachedInstance=instance;cachedVtable=treeTable;cachedImageBase=image;return treeTable;}cachedInstance=0;long cursor=0,scanned=0;var chunk=new byte[262144];MemoryInfo info;
  while(VirtualQueryEx(h,new IntPtr(cursor),out info,(UIntPtr)Marshal.SizeOf(typeof(MemoryInfo))).ToUInt64()!=0){
   long end=checked(info.BaseAddress+(long)info.RegionSize);if(end<=cursor)break;
   if(info.State==0x1000&&info.Type==0x20000&&(info.Protect&0x100)==0&&(info.Protect&0xcc)!=0){
    for(long at=info.BaseAddress;at<end;at+=chunk.Length){int count=(int)Math.Min(chunk.Length,end-at);IntPtr got;if(!ReadProcessMemory(h,new IntPtr(at),chunk,count,out got))continue;scanned+=count;
     for(int i=0;i+8<=got.ToInt64();i+=8){long self=BitConverter.ToInt64(chunk,i);if(self!=at+i-0x288||self<info.BaseAddress)continue;
      long table=BitConverter.ToInt64(Read(h,self,8),0);if(table<image+0x7000000||table>image+0xc000000)continue;
      if(BitConverter.ToInt64(Read(h,table+5*8,8),0)==image+FilterRva&&ValidChatViewport(h,image,self,table)){instance=self;cachedInstance=self;cachedVtable=table;cachedImageBase=image;return table;}
     }
     if(scanned>1024L*1024*1024)throw new IOException("在受限扫描范围内未找到消息视口对象。");
    }
   }
   cursor=end;
  }
  throw new IOException("未找到具有已验证结构的消息列表对象，请先进入一个聊天。");
 }
 internal sealed class Code {
  internal List<byte> B=new List<byte>();Dictionary<string,int> labels=new Dictionary<string,int>();List<Tuple<int,string>> fix=new List<Tuple<int,string>>();
  internal void Emit(params byte[] b){B.AddRange(b);}internal void Imm(long n){Emit(BitConverter.GetBytes(n));}
  internal void Rax(long n){Emit(0x48,0xb8);Imm(n);}internal void Rcx(long n){Emit(0x48,0xb9);Imm(n);}internal void Rdx(long n){Emit(0x48,0xba);Imm(n);}
  internal void AbsCall(long n){Rax(n);Emit(0xff,0xd0);}internal void Label(string s){labels.Add(s,B.Count);}
  internal void Jz(string s){Emit(0x0f,0x84);fix.Add(Tuple.Create(B.Count,s));Emit(0,0,0,0);} internal void Jae(string s){Emit(0x0f,0x83);fix.Add(Tuple.Create(B.Count,s));Emit(0,0,0,0);}internal void Jnz(string s){Emit(0x0f,0x85);fix.Add(Tuple.Create(B.Count,s));Emit(0,0,0,0);}internal void Jmp(string s){Emit(0xe9);fix.Add(Tuple.Create(B.Count,s));Emit(0,0,0,0);}
  internal byte[] Finish(){foreach(var p in fix){var d=BitConverter.GetBytes(labels[p.Item2]-(p.Item1+4));for(int i=0;i<4;i++)B[p.Item1+i]=d[i];}return B.ToArray();}
 }
 static byte[] BrushCode(long image,long block){var c=new Code();
  c.Emit(0x48,0x83,0xec,0x58, 0x48,0x89,0x4c,0x24,0x20, 0x48,0x89,0x54,0x24,0x28, 0x4c,0x89,0x44,0x24,0x30);
  c.Rax(block+8);c.Emit(0x80,0x38,0);c.Jnz("ready");c.Emit(0xc6,0x00,1);
  c.Rcx(block+0x100);c.Rdx(block+0x200);c.Emit(0x45,0x31,0xc0);c.AbsCall(image+ImageFileRva);
  c.Rax(block+8);c.Emit(0xc6,0x00,2);
  c.Label("ready");c.Rax(block+0x118);c.Emit(0x48,0x83,0x38,0);c.Jnz("texture");
  c.Emit(0x48,0x8b,0x4c,0x24,0x20, 0x48,0x8b,0x54,0x24,0x28, 0x4c,0x8b,0x44,0x24,0x30);c.AbsCall(image+BrushColorRva);c.Jmp("done");
  c.Label("texture");c.Emit(0x48,0x8b,0x4c,0x24,0x20);c.Rdx(block+0x100);c.AbsCall(image+BrushImageRva);
  c.Label("done");c.Emit(0x48,0x83,0xc4,0x58,0xc3);return c.Finish();
 }
 static byte[] AdaptiveBrushCode(long image,long block){var c=new Code();
  c.Emit(0x48,0x83,0xec,0x58, 0x48,0x89,0x4c,0x24,0x20, 0x48,0x89,0x54,0x24,0x28, 0x4c,0x89,0x44,0x24,0x30);
  c.Rax(block+8);c.Emit(0x80,0x38,0);c.Jnz("ready");c.Emit(0xc6,0x00,1);
  c.Rcx(block+0x100);c.Rdx(block+0x200);c.Emit(0x45,0x31,0xc0);c.AbsCall(image+ImageFileRva);
  c.Rax(block+8);c.Emit(0xc6,0x00,2);
  c.Label("ready");c.Rax(block+10);c.Emit(0x80,0x38,0);c.Jnz("clearWhole");c.Rax(block+0x118);c.Emit(0x48,0x83,0x38,0);c.Jnz("texture");
  c.Emit(0x48,0x8b,0x4c,0x24,0x20, 0x48,0x8b,0x54,0x24,0x28, 0x4c,0x8b,0x44,0x24,0x30);c.AbsCall(image+BrushColorRva);c.Jmp("done");
  c.Label("texture");c.Emit(0x48,0x8b,0x4c,0x24,0x20);c.Rdx(block+0x100);c.AbsCall(image+BrushImageRva);
  c.Jmp("done");c.Label("clearWhole");c.Emit(0x66,0x0f,0xef,0xc0,0x0f,0x11,0x44,0x24,0x40,0xc7,0x44,0x24,0x40,1,0,0,0,0x48,0x8b,0x4c,0x24,0x20,0x48,0x8d,0x54,0x24,0x40,0x4c,0x8b,0x44,0x24,0x30);c.AbsCall(image+BrushColorRva);c.Label("done");c.Emit(0x48,0x83,0xc4,0x58,0xc3);return c.Finish();
 }
 static byte[] FilterCode(long image,long block,long originalEvent,long previousImage,bool removing,bool wholeRunner=false){var c=new Code();
  c.Emit(0x48,0x83,0xec,0x58, 0x48,0x89,0x4c,0x24,0x20, 0x48,0x89,0x54,0x24,0x28, 0x4c,0x89,0x44,0x24,0x30, 0x4c,0x89,0x4c,0x24,0x38);
  c.Rax(block+9);c.Emit(0x80,0x38,0);c.Jnz("done");c.Emit(0xc6,0x00,1);c.AbsCall(image+RefreshRva);
  if(wholeRunner)c.AbsCall(block+0x4000);c.AbsCall(block+0x20000);c.AbsCall(block+0x60000);
  if(previousImage!=0){if(!removing){c.Rax(block+0x118);c.Emit(0x48,0x83,0x38,0);c.Emit(0x0f,0x84);int skip=c.B.Count;c.Emit(0,0,0,0);c.Rcx(previousImage);c.AbsCall(image+0x4fdbc0);int delta=c.B.Count-(skip+4);var d=BitConverter.GetBytes(delta);for(int i=0;i<4;i++)c.B[skip+i]=d[i];}else{c.Rcx(previousImage);c.AbsCall(image+0x4fdbc0);}}
  c.Rax(block+9);c.Emit(0xc6,0x00,2);
  c.Label("done");c.Emit(0x48,0x8b,0x4c,0x24,0x20, 0x48,0x8b,0x54,0x24,0x28, 0x4c,0x8b,0x44,0x24,0x30, 0x4c,0x8b,0x4c,0x24,0x38, 0x48,0x83,0xc4,0x58);c.Rax(originalEvent);c.Emit(0xff,0xe0);return c.Finish();
 }
 static byte[] PostEventCode(long image,long instance){var c=new Code();c.Emit(0x48,0x83,0xec,0x38);c.Rcx(32);c.AbsCall(image+0x754bcdc);c.Emit(0x48,0x89,0x44,0x24,0x20,0x48,0x89,0xc1,0xba,0xe8,0x03,0x00,0x00);c.AbsCall(image+0x81acb0);c.Rcx(instance);c.Emit(0x48,0x8b,0x54,0x24,0x20,0x45,0x31,0xc0);c.AbsCall(image+0x1581f0);c.Emit(0x31,0xc0,0x48,0x83,0xc4,0x38,0xc3);return c.Finish();}
 static void QueueRefresh(IntPtr h,long block){uint tid;IntPtr thread=CreateRemoteThread(h,IntPtr.Zero,UIntPtr.Zero,new IntPtr(block+0x3000),IntPtr.Zero,0,out tid);if(thread==IntPtr.Zero)throw Error("Queue native Qt event");try{if(WaitForSingleObject(thread,5000)!=0)throw new IOException("原生事件投递未及时完成。");uint exit;if(!GetExitCodeThread(thread,out exit)||exit!=0)throw new IOException("原生事件投递失败。");}finally{CloseHandle(thread);}}
 public static string Fingerprint(){ValidateFile();var candidates=Process.GetProcessesByName("Weixin").Where(candidate=>candidate.MainWindowHandle!=IntPtr.Zero).ToArray();if(candidates.Length!=1)return null;var p=candidates[0];long image=0;foreach(ProcessModule m in p.Modules)if(m.ModuleName.Equals("Weixin.dll",StringComparison.OrdinalIgnoreCase))image=m.BaseAddress.ToInt64();if(image==0)return null;var h=OpenProcess(0x410,false,p.Id);if(h==IntPtr.Zero)return null;try{long instance;FindLiveVtable(h,image,out instance);long d=BitConverter.ToInt64(Read(h,instance+0x28,8),0);var r=Read(h,d+0x14,16);int w=BitConverter.ToInt32(r,8)-BitConverter.ToInt32(r,0)+1,ht=BitConverter.ToInt32(r,12)-BitConverter.ToInt32(r,4)+1;var layout=new StringBuilder();long node=instance;for(int level=0;level<32;level++){var rect=WidgetRect(h,node);long parent=Pointer(h,Pointer(h,node+8)+0x10);layout.Append(parent==0?rect.Size.ToString():rect.ToString());if(parent==0)break;node=parent;}if(WholeEnabled)return "appearance="+AppearanceProfiles.Mode+":"+p.Id+":"+p.StartTime.ToUniversalTime().Ticks+":native-cover:"+node.ToString("x");return "appearance="+AppearanceProfiles.Mode+":"+p.Id+":"+p.StartTime.ToUniversalTime().Ticks+":"+w+"x"+ht+":"+layout;}finally{CloseHandle(h);}}
 internal const int MaxWallpaperBytes = 80 * 1024 * 1024;
 public static string CacheImage(string path){return CacheImage(path,0,0);}
 static string CacheImage(string path,int width,int height){
  if(new FileInfo(path).Length>MaxWallpaperBytes)throw new InvalidDataException("图片不能超过 80 MB。");
  string dir=Path.Combine(Engine.Store,width==0?"backgrounds":"runtime-cache");Directory.CreateDirectory(dir);
  using(var stream=File.OpenRead(path))using(var source=Image.FromStream(stream,true,true)){
   if(source.Width>4096||source.Height>4096||source.Width<1||source.Height<1)throw new InvalidDataException("图片尺寸不能超过 4096 × 4096。");
   if(width==0){width=source.Width;height=source.Height;}
   using(var bitmap=new Bitmap(width,height,PixelFormat.Format32bppArgb)){using(var g=Graphics.FromImage(bitmap)){g.Clear(Color.Transparent);if(width==source.Width&&height==source.Height)g.DrawImageUnscaled(source,0,0);else{g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;double scale=Math.Max(width/(double)source.Width,height/(double)source.Height);float w=(float)(source.Width*scale),h=(float)(source.Height*scale);g.DrawImage(source,(width-w)/2,(height-h)/2,w,h);}}using(var data=new MemoryStream()){bitmap.Save(data,ImageFormat.Png);var bytes=data.ToArray();string dest=Path.Combine(dir,Engine.Hash(bytes)+".png");if(!File.Exists(dest))File.WriteAllBytes(dest,bytes);return dest;}}
  }
 }
 public static string Apply(string file){
  string previewResult;if(NativePreviewBridge.TryApply(file,out previewResult))return previewResult;if(InternalModulePresent())throw new IOException("当前微信已加载进程内主题，外置绘制已暂停。请先连接预览，再应用配置。");
  using(var mutex=new Mutex(false,"Local\\WeixinThemeStudioNativeBackgroundOperation")){bool held=false;try{try{held=mutex.WaitOne(0);}catch(AbandonedMutexException){held=true;}if(!held)throw new IOException("另一项背景操作正在进行，请稍后重试。");return ApplyCore(file);}finally{if(held)mutex.ReleaseMutex();}}
 }
 static string ApplyCore(string file){
  if(file!=null&&AppearanceProfiles.EditingMode.HasValue&&ActualAppearance()!=AppearanceProfiles.EditingMode.Value){Directory.CreateDirectory(Engine.Store);File.WriteAllText(Path.Combine(Engine.Store,"background.txt"),CacheImage(file),Encoding.UTF8);return "已保存"+(AppearanceProfiles.EditingMode.Value==0?"浅色":"深色")+"配置；微信切换到对应外观后由后台应用。";}
  bool removing=file==null;ValidateFile();string source=removing?null:CacheImage(file),cached=null;
  var processes=Process.GetProcessesByName("Weixin").Where(p=>p.MainWindowHandle!=IntPtr.Zero).ToArray();
  if(processes.Length!=1)throw new InvalidOperationException("请先打开微信主窗口；需要且只允许一个微信主进程。");
  var process=processes[0];long image=0;foreach(ProcessModule m in process.Modules)if(m.ModuleName.Equals("Weixin.dll",StringComparison.OrdinalIgnoreCase))image=m.BaseAddress.ToInt64();if(image==0)throw new InvalidOperationException("未找到当前版本的微信模块。");
  var h=OpenProcess(0x43a,false,process.Id);if(h==IntPtr.Zero)throw Error("OpenProcess");long block=0,previous=0;bool callPatched=false,filterPatched=false;byte[] original=Call(image+CallRva,image+BrushColorRva),priorCall=null,priorRow=null;bool rowPatched=false,resizePatched=false,textSlotPatched=false,iconCallPatched=false;byte[][] priorOutline=null;bool outlinePatched=false;byte[] priorIconCall=null;byte[] priorTextSlot=null;long textSlot=image+0x93ab420;byte[] priorResize=null;long resizeSlot=image+0x97bec40;long filterSlot=0,originalEvent=0;
  try{
   long instance;filterSlot=FindLiveVtable(h,image,out instance)+4*8; originalEvent=BitConverter.ToInt64(Read(h,filterSlot,8),0); if(originalEvent<image+0x1000||originalEvent>image+0x7800000)throw new IOException("未知的消息列表事件入口。");
   priorCall=Read(h,image+CallRva,5);
   if(!Equal(priorCall,original)){if(priorCall[0]!=0xe8)throw new IOException("未知的背景入口修改。");previous=image+CallRva+5+BitConverter.ToInt32(priorCall,1)-0x1000;if(Encoding.ASCII.GetString(Read(h,previous,7))!="WXBG001"||(!Equal(Read(h,previous+0x1000,BrushCode(image,previous).Length),BrushCode(image,previous))&&!Equal(Read(h,previous+0x1000,AdaptiveBrushCode(image,previous).Length),AdaptiveBrushCode(image,previous))))throw new IOException("背景入口不属于当前模块，已拒绝修改。");}
   priorRow=Read(h,image+RowColorCall,5);var originalRow=Call(image+RowColorCall,image+RowColorFunction);if(!Equal(priorRow,originalRow)&&(previous==0||!Equal(priorRow,Call(image+RowColorCall,previous+0xd000))))throw new IOException("会话行绘制入口已有未知修改。");if(removing&&previous==0)return "当前微信没有启用图片背景。";
   priorResize=Read(h,resizeSlot,8);long resize=BitConverter.ToInt64(priorResize,0);if(Read(h,image+0xaf20,1)[0]!=0xc3||(resize!=image+0xaf20&&(previous==0||resize!=previous+0xd900)))throw new IOException("主视图尺寸事件入口未通过校验。");
   priorTextSlot=Read(h,textSlot,8);long textPaint=BitConverter.ToInt64(priorTextSlot,0);if(textPaint!=image+0x1ea62d0&&(previous==0||textPaint!=previous+0x36000))throw new IOException("文字首次绘制入口未通过校验。");
   priorIconCall=Read(h,image+IconColorCall,5);if(!Equal(priorIconCall,Call(image+IconColorCall,image+IconColorGetter))&&(previous==0||!Equal(priorIconCall,Call(image+IconColorCall,previous+0x70000))))throw new IOException("SVG着色入口已有未知修改。");
   priorOutline=ValidateOutlineCalls(h,image,previous);int width=0,height=0;if(!removing){long widgetData=BitConverter.ToInt64(Read(h,instance+0x28,8),0);var rect=Read(h,widgetData+0x14,16);width=BitConverter.ToInt32(rect,8)-BitConverter.ToInt32(rect,0)+1;height=BitConverter.ToInt32(rect,12)-BitConverter.ToInt32(rect,4)+1;if(width<100||width>8192||height<80||height>8192)throw new IOException("聊天区域尺寸未通过校验。");if(!WholeEnabled)cached=CacheImage(BackgroundEffects.Prepare(source),width,height);}
   SyncNativePalette(!removing);
   var whole=BuildWholePlan(h,image,instance,source,previous,removing);var ink=BuildInkPlan(h,image,instance,previous,removing);var uiIcons=BuildUiIconPlan(h,image,instance,previous);if(whole.ChatImage!=null)cached=whole.ChatImage;if(!removing&&cached==null)cached=CacheImage(source,width,height);
   for(long d=0x1000000;d<0x70000000&&block==0;d+=0x1000000){foreach(long a in new[]{image+d,image-d}){var allocated=VirtualAllocEx(h,new IntPtr(a),(UIntPtr)BlockSize,0x3000,0x04);if(allocated!=IntPtr.Zero){block=allocated.ToInt64();break;}}}
   if(block==0)throw Error("VirtualAllocEx");
   var bytes=new byte[BlockSize];Encoding.ASCII.GetBytes("WXBG001").CopyTo(bytes,0);BitConverter.GetBytes(block+0x300).CopyTo(bytes,0x200);
   if(!removing){BitConverter.GetBytes(-1).CopyTo(bytes,0x300);BitConverter.GetBytes(cached.Length).CopyTo(bytes,0x304);BitConverter.GetBytes((long)24).CopyTo(bytes,0x310);Encoding.Unicode.GetBytes(cached+"\0").CopyTo(bytes,0x318);}
   bytes[10]=(byte)(!removing&&WholeEnabled?1:0);AdaptiveBrushCode(image,block).CopyTo(bytes,0x1000);FilterCode(image,block,originalEvent,previous==0?0:previous+0x100,removing,whole.HasWork).CopyTo(bytes,0x2000);PostEventCode(image,instance).CopyTo(bytes,0x3000);WriteWholePlan(bytes,whole,image,block);WriteInkPlan(bytes,ink,block,image);WriteUiIcons(bytes,uiIcons,image,block);RowColorCode(image,block).CopyTo(bytes,0xd000);bytes[0xd800]=0xc3;var resizeCode=new Code();resizeCode.Rax(image+0x5b7260);resizeCode.Emit(0xff,0xe0);resizeCode.Finish().CopyTo(bytes,0xd900);CoverPaintCode(image).CopyTo(bytes,0xda00);InputBorderPaintCode().CopyTo(bytes,0x3f000);WriteSettingsSignature(bytes,source,removing);Write(h,block,bytes);
   uint outlineProtection;if(!VirtualProtectEx(h,new IntPtr(block+0x80000),(UIntPtr)0x1000,0x20,out outlineProtection))throw Error("Protect icon outline code");FlushInstructionCache(h,new IntPtr(block+0x80000),(UIntPtr)0x1000);uint iconProtection;if(!VirtualProtectEx(h,new IntPtr(block+0x60000),(UIntPtr)0x11000,0x20,out iconProtection))throw Error("Protect SVG code");FlushInstructionCache(h,new IntPtr(block+0x60000),(UIntPtr)0x11000);uint borderProtection;if(!VirtualProtectEx(h,new IntPtr(block+0x3f000),(UIntPtr)0x1000,0x20,out borderProtection))throw Error("Protect input inputBorder code");FlushInstructionCache(h,new IntPtr(block+0x3f000),(UIntPtr)0x1000);uint firstPaintProtection;if(!VirtualProtectEx(h,new IntPtr(block+0x36000),(UIntPtr)0x1000,0x20,out firstPaintProtection))throw Error("Protect first text paint");FlushInstructionCache(h,new IntPtr(block+0x36000),(UIntPtr)0x1000);uint paintProtection;if(!VirtualProtectEx(h,new IntPtr(block+0x40000),(UIntPtr)0x20000,0x20,out paintProtection))throw Error("Protect text paint code");FlushInstructionCache(h,new IntPtr(block+0x40000),(UIntPtr)0x20000);uint inkProtection;if(!VirtualProtectEx(h,new IntPtr(block+0x20000),(UIntPtr)0x10000,0x20,out inkProtection))throw Error("Protect ink code");FlushInstructionCache(h,new IntPtr(block+0x20000),(UIntPtr)0x10000);uint old;if(!VirtualProtectEx(h,new IntPtr(block+0x1000),(UIntPtr)0x5000,0x20,out old))throw Error("Protect code");if(!FlushInstructionCache(h,new IntPtr(block+0x1000),(UIntPtr)0x5000))throw Error("Flush code");
   uint rowProtect;if(!VirtualProtectEx(h,new IntPtr(block+0xd000),(UIntPtr)0x1000,0x20,out rowProtect))throw Error("Protect row code");FlushInstructionCache(h,new IntPtr(block+0xd000),(UIntPtr)0x1000);Patch(h,image+CallRva,removing?original:Call(image+CallRva,block+0x1000));callPatched=true;Patch(h,image+RowColorCall,!removing&&WholeEnabled?Call(image+RowColorCall,block+0xd000):originalRow);rowPatched=true;Patch(h,image+IconColorCall,Call(image+IconColorCall,!removing&&IconsActive?block+0x70000:image+IconColorGetter));iconCallPatched=true;PatchOutlineCalls(h,image,!removing&&uiIcons.Routes.Count>0?block+0x80000:image+0x19de30);outlinePatched=true;Patch(h,textSlot,BitConverter.GetBytes(!removing&&(InkText!="original"||InkChat!="original"||InkLink!="original")?block+0x36000:image+0x1ea62d0));textSlotPatched=true;Patch(h,resizeSlot,BitConverter.GetBytes(!removing&&WholeEnabled?block+0xd900:image+0xaf20));resizePatched=true;
   Patch(h,filterSlot,BitConverter.GetBytes(block+0x2000));filterPatched=true;
   QueueRefresh(h,block);
   string log=Path.Combine(Engine.Store,"background-runtime.txt");RuntimeLog(log,"pid="+process.Id+"\nimage=0x"+image.ToString("x")+"\nblock=0x"+block.ToString("x")+"\nvtable_event_slot=0x"+filterSlot.ToString("x")+"\noriginal_event=0x"+originalEvent.ToString("x")+"\nsize="+width+"x"+height+"\nremoving="+removing+"\nwaiting for chat viewport event\n",false);
   for(int n=0;n<1200;n++){if(process.HasExited)throw new IOException("微信在验证过程中退出。");if(Read(h,block+9,1)[0]==2){bool loaded=removing||BitConverter.ToInt64(Read(h,block+0x118,8),0)!=0;if(loaded)callPatched=false;Patch(h,filterSlot,BitConverter.GetBytes(originalEvent));filterPatched=false;
     RuntimeLog(log,"event_refresh=completed\nimage_decoded="+loaded+"\n",true);if(!loaded)throw new InvalidDataException("微信图片解码未成功，已撤销背景入口。");callPatched=false;int liveWhole=whole.Targets.Count(t=>Pointer(h,t.Widget+0x160)!=0);int clearedWhole=whole.Targets.Count==0?whole.Clear.Count(t=>Pointer(h,t.Widget+0x160)==0):0;RuntimeLog(log,"whole_live="+liveWhole+"/"+whole.Targets.Count+"\nwhole_cleared="+clearedWhole+"/"+whole.Clear.Count+"\n",true);if(removing){string config=Path.Combine(Engine.Store,"background.txt");if(File.Exists(config))File.Delete(config);}else File.WriteAllText(Path.Combine(Engine.Store,"background.txt"),source,Encoding.UTF8);PruneRuntimeCache(whole.Targets.Select(t=>t.File).Concat(new[]{cached}));return removing?"图片背景已移除，当前配色保留。":"图片背景已应用（"+width+" × "+height+"），头像、表情仍由微信原样绘制。";}
    Thread.Sleep(100);
   }
   throw new TimeoutException("120 秒内未收到聊天视口事件，已撤销入口。请先打开聊天再重试。");
  }finally{try{if(!process.HasExited){if(filterPatched)Patch(h,filterSlot,BitConverter.GetBytes(originalEvent));if(callPatched){Patch(h,image+CallRva,priorCall);if(rowPatched)Patch(h,image+RowColorCall,priorRow);if(resizePatched)Patch(h,resizeSlot,priorResize);if(textSlotPatched)Patch(h,textSlot,priorTextSlot);if(iconCallPatched)Patch(h,image+IconColorCall,priorIconCall);if(outlinePatched)RestoreOutlineCalls(h,image,priorOutline);}}}finally{CloseHandle(h);}}
 }
 public static string SelfTest(){long image=0x180000000,block=0x190000000;var a=BrushCode(image,block);var b=FilterCode(image,block,image+FilterRva,0,false);if(a.Length>4096||b.Length>4096||a[a.Length-1]!=0xc3||b[b.Length-1]!=0xe0)throw new Exception("Invalid code layout");var path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"research","runtime-test","background-grid.png");string cached=CacheImage(path);using(var test=new Bitmap(cached)){if(test.Width!=160||test.Height!=160||test.GetPixel(1,1).ToArgb()!=Color.FromArgb(120,171,211).ToArgb())throw new Exception("Image cache altered colors");}File.WriteAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"research","brush-code.bin"),a);File.WriteAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"research","filter-code.bin"),b);return "Image cache color readback and code layout PASS; runtime unverified.";}
}











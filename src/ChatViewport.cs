using System;using System.Collections.Generic;using System.IO;
internal static partial class NativeBackground{
 static long cachedChatRoot;
 static bool ValidChatViewport(IntPtr h,long image,long obj,long table){try{if(Pointer(h,obj)!=table||Pointer(h,obj+0x288)!=obj||Pointer(h,table+40)!=image+FilterRva)return false;long data=Pointer(h,obj+0x28);if((BitConverter.ToUInt32(Read(h,data+8,4),0)&32768)==0)return false;var rect=WidgetRect(h,obj);return rect.Width>=100&&rect.Height>=80;}catch{return false;}}
 static bool FindChatInTree(IntPtr h,long image,out long instance,out long table){instance=table=0;try{long root=cachedChatRoot;if(root==0&&cachedInstance!=0){root=cachedInstance;for(int i=0;i<32;i++){long parent=Pointer(h,Pointer(h,root+8)+16);if(parent==0)break;root=parent;}}if(root==0||WidgetClass(h,image,root)!="mmui::MainWindow"){cachedChatRoot=0;return false;}cachedChatRoot=root;var queue=new Queue<long>();var seen=new HashSet<long>();queue.Enqueue(root);while(queue.Count>0&&seen.Count<6000){long obj=queue.Dequeue();if(!seen.Add(obj))continue;try{long vt=Pointer(h,obj);if(ValidChatViewport(h,image,obj,vt)){instance=obj;table=vt;return true;}foreach(long child in WidgetChildren(h,obj))queue.Enqueue(child);}catch{}}throw new IOException("等待当前聊天消息区域展开，请保持一个聊天窗口打开。");}catch(IOException){throw;}catch{cachedChatRoot=0;return false;}}
}


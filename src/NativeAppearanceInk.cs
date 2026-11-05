using System;
internal static partial class NativeBackground{
 static string ProfileInk(string key,int mode){using(AppearanceProfiles.Use(mode))return ReadInk(key);}
 // Select the active WeChat appearance at paint time, so an old module cannot force
 // dark-mode white onto a newly light-mode widget while the watcher rebuilds it.
 static void ModeInk(Code c,long image,string key,string originalLabel,string tag,bool argb){string light=ProfileInk(key,0),dark=ProfileInk(key,1);c.Rax(image+AppearanceFlag+2);c.Emit(0x80,0x38,0);c.Jnz(tag+"Dark");if(light=="original")c.Jmp(originalLabel);else{c.Emit(0xba);c.Emit(BitConverter.GetBytes(argb?Convert.ToUInt32(light.Substring(1),16)|0xff000000:InkValue(light)));c.Jmp(tag+"Color");}c.Label(tag+"Dark");if(dark=="original")c.Jmp(originalLabel);else{c.Emit(0xba);c.Emit(BitConverter.GetBytes(argb?Convert.ToUInt32(dark.Substring(1),16)|0xff000000:InkValue(dark)));}c.Label(tag+"Color");}
 static void ModeIconRgb(Code c,long image,string key,string tag){string light=ProfileInk(key,0),dark=ProfileInk(key,1);c.Emit(0x49,0xbb);c.Imm(image+AppearanceFlag+2);c.Emit(0x41,0x80,0x3b,0);c.Jnz(tag+"Dark");if(light!="original")IconRgb(c,light);c.Jmp(tag+"Done");c.Label(tag+"Dark");if(dark!="original")IconRgb(c,dark);c.Label(tag+"Done");}
}

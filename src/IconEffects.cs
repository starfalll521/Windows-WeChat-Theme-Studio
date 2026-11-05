using System;using System.IO;using System.Text;
internal sealed class IconSettings{
 public int Mode=1,Brightness=110,Saturation=115,Contrast=105,Visibility=85,Outline=35;
 public string Key{get{return Mode+"|"+Brightness+"|"+Saturation+"|"+Contrast+"|"+Visibility+"|"+Outline;}}
 public void Validate(){if(Mode<0||Mode>2||Brightness<50||Brightness>160||Saturation<0||Saturation>180||Contrast<50||Contrast>150||Visibility<0||Visibility>100||Outline<0||Outline>100)throw new InvalidDataException("图标增强参数超出范围。");}
}
internal static class IconEffects{
 static string FileName{get{return AppearanceProfiles.Path("icon-effects.txt");}}
 public static IconSettings Load(){try{var p=File.ReadAllText(FileName).Trim().Split('|');var s=new IconSettings{Mode=int.Parse(p[0]),Brightness=int.Parse(p[1]),Saturation=int.Parse(p[2]),Contrast=int.Parse(p[3]),Visibility=int.Parse(p[4]),Outline=int.Parse(p[5])};s.Validate();return s;}catch{return new IconSettings{Mode=0,Visibility=0,Outline=0};}}
 public static void Save(IconSettings s){s.Validate();Directory.CreateDirectory(Engine.Store);File.WriteAllText(FileName,s.Key,Encoding.UTF8);}
}
internal static partial class NativeBackground{
 static bool IconsActive{get{return IconEffects.Load().Mode!=0||InkIcons!="original"||InkAccent!="original";}}
 static void IconFloats(byte[] b,IconSettings s){float[] a={s.Saturation/100f,s.Contrast/100f,s.Brightness/100f,.2126f,.7152f,.0722f,32767.5f,65535f,0};for(int i=0;i<a.Length;i++)BitConverter.GetBytes(a[i]).CopyTo(b,0x7a000+i*4);}
 static void EmitIconEnhance(Code c,long block){c.Emit(0x49,0xbb);c.Imm(block+0x7a000);for(int i=0;i<3;i++){c.Emit(0x0f,0xb7,0x48,(byte)(6+i*2),0xf3,0x0f,0x2a,(byte)(0xc1+i*8));}c.Emit(0x0f,0x28,0xd8,0xf3,0x41,0x0f,0x59,0x5b,12,0x0f,0x28,0xe1,0xf3,0x41,0x0f,0x59,0x63,16,0xf3,0x0f,0x58,0xdc,0x0f,0x28,0xe2,0xf3,0x41,0x0f,0x59,0x63,20,0xf3,0x0f,0x58,0xdc);
  for(int i=0;i<3;i++){byte reg=(byte)(i*8);c.Emit(0xf3,0x0f,0x5c,(byte)(0xc3+reg),0xf3,0x41,0x0f,0x59,(byte)(0x03+reg),0xf3,0x0f,0x58,(byte)(0xc3+reg),0xf3,0x41,0x0f,0x5c,(byte)(0x43+reg),24,0xf3,0x41,0x0f,0x59,(byte)(0x43+reg),4,0xf3,0x41,0x0f,0x58,(byte)(0x43+reg),24,0xf3,0x41,0x0f,0x59,(byte)(0x43+reg),8,0xf3,0x41,0x0f,0x5f,(byte)(0x43+reg),32,0xf3,0x41,0x0f,0x5d,(byte)(0x43+reg),28,0xf3,0x0f,0x2d,(byte)(0xc8+i),0x66,0x89,0x48,(byte)(6+i*2));}
 }
 static void EmitIconVisibility(Code c,IconSettings s){if(s.Mode==0||s.Visibility==0)return;c.Emit(0x48,0x8b,0x44,0x24,0x28,0x0f,0xb7,0x48,4,0x69,0xc9);c.Emit(BitConverter.GetBytes(100+s.Visibility));c.Emit(0x89,0xc8,0x31,0xd2,0xb9,100,0,0,0,0xf7,0xf1,0xb9,255,255,0,0,0x39,0xc8,0x0f,0x47,0xc1,0x48,0x8b,0x54,0x24,0x28,0x66,0x89,0x42,4);}
}

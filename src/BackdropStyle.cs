using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;using System.Runtime.InteropServices;
// All treatment is baked into one master background. No foreground pixels are processed.
internal static class BackdropStyle {
 static double Clamp(double x){return Math.Max(0,Math.Min(1,x));}
 static double Smooth(double x){x=Clamp(x);return x*x*(3-2*x);}
 public static Bitmap Compose(Image source,int width,int height,Rectangle chat,int strength,bool uniform){
  var result=new Bitmap(width,height,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(result)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.CompositingQuality=CompositingQuality.HighQuality;double scale=Math.Max(width/(double)source.Width,height/(double)source.Height);float w=(float)(source.Width*scale),h=(float)(source.Height*scale);g.DrawImage(source,(width-w)/2,(height-h)/2,w,h);}
  if(strength>=100)return result;
  double opacity=1-Math.Max(10,Math.Min(100,strength))/100d;
  if(uniform){using(var g=Graphics.FromImage(result))using(var b=new SolidBrush(Color.FromArgb((int)Math.Round(opacity*255),247,251,248)))g.FillRectangle(b,0,0,width,height);return result;}
  int feather=Math.Max(24,Math.Min(72,width/24));double sidebar=1-Math.Pow(1-opacity,2);
  using(var mask=new Bitmap(width,height,PixelFormat.Format32bppArgb)){var data=mask.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);try{var pixels=new byte[data.Stride*height];for(int y=0;y<height;y++)for(int x=0;x<width;x++){double left=sidebar*(1-Smooth((x-chat.Left)/(double)feather));double header=opacity*(1-Smooth((y-chat.Top)/(double)Math.Max(24,feather)));double input=Math.Min(.96,sidebar+.04)*Smooth((y-(chat.Bottom-feather))/(double)feather);double a=Math.Max(left,Math.Max(header,input));int p=y*data.Stride+x*4;pixels[p]=248;pixels[p+1]=251;pixels[p+2]=247;pixels[p+3]=(byte)Math.Round(255*a);}Marshal.Copy(pixels,0,data.Scan0,pixels.Length);}finally{mask.UnlockBits(data);}using(var g=Graphics.FromImage(result))g.DrawImageUnscaled(mask,0,0);}
  return result;
 }
 public static void SelfTest(){using(var source=new Bitmap(160,100)){using(var g=Graphics.FromImage(source))g.Clear(Color.FromArgb(30,60,90));using(var image=Compose(source,160,100,new Rectangle(50,20,110,55),35,false)){if(image.GetPixel(120,45).ToArgb()!=Color.FromArgb(30,60,90).ToArgb())throw new Exception("Clear chat background changed");if(image.GetPixel(5,45).R<200)throw new Exception("Sidebar readability wash missing");int seam=Math.Abs(image.GetPixel(50,45).R-image.GetPixel(51,45).R);if(seam>4)throw new Exception("Hard pane seam");}using(var image=Compose(source,160,100,new Rectangle(50,20,110,55),35,true)){if(image.GetPixel(5,45).ToArgb()!=image.GetPixel(120,45).ToArgb())throw new Exception("Uniform style mismatch");}}}
}

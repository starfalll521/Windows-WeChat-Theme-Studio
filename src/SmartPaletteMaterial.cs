using System;using System.Drawing;using System.Linq;using System.Collections.Generic;
internal static partial class SmartPalette{
 internal static string LinkFor(SmartColors p){
  var seed=ToOk(ColorTranslator.FromHtml(p.Seed));bool light=p.RuntimeMode==0;
  Color c=Tone(seed.Hue,Math.Min(.09,seed.C),light?.35:.84);
  Color a=ColorTranslator.FromHtml(p.Theme.Colors[12+p.RuntimeMode]),b=ColorTranslator.FromHtml(p.Theme.Colors[14+p.RuntimeMode]);
  for(int i=0;i<50&&(Contrast(c,a)<4.5||Contrast(c,b)<4.5);i++)c=Tone(seed.Hue,Math.Min(.09,seed.C),Clamp((light?.35:.84)+(light?-1:1)*i*.004,.05,.98));
  return Hex(c);
 }
 static Color Composite(Color front,Color back,double alpha){return Color.FromArgb((int)Math.Round(front.R*alpha+back.R*(1-alpha)),(int)Math.Round(front.G*alpha+back.G*(1-alpha)),(int)Math.Round(front.B*alpha+back.B*(1-alpha)));}
 static int RegionOpacity(Bitmap image,Rectangle area,Color surface,Color ink){
  var samples=new List<Color>();for(int y=area.Top;y<area.Bottom;y+=2)for(int x=area.Left;x<area.Right;x+=2){var c=image.GetPixel(x,y);if(c.A>=200)samples.Add(c);}
  if(samples.Count==0)return 45;
  for(int opacity=18;opacity<=80;opacity+=3){int readable=0;foreach(var c in samples)if(Contrast(ink,Composite(surface,c,opacity/100d))>=4.5)readable++;if(readable>=samples.Count*.94)return opacity;}
  return 80;
 }
 static void TuneMaterial(string path,SmartColors p){
  bool light=p.RuntimeMode==0;var seed=ToOk(ColorTranslator.FromHtml(p.Seed));double chroma=seed.C<.01?0:.009;
  Color surface=Tone(seed.Hue,chroma,light?.97:.17),ink=ColorTranslator.FromHtml(SecondaryFor(p));
  using(var source=Image.FromFile(path))using(var sample=new Bitmap(96,64)){using(var g=Graphics.FromImage(sample))g.DrawImage(source,0,0,96,64);
   p.Effects.Color=Hex(surface);p.Effects.Friends=RegionOpacity(sample,new Rectangle(5,7,23,57),surface,ink);
   p.Effects.Header=RegionOpacity(sample,new Rectangle(0,0,96,7),surface,ink);
   p.Effects.Navigation=RegionOpacity(sample,new Rectangle(0,0,5,64),surface,ink);
   int input=RegionOpacity(sample,new Rectangle(28,51,68,13),surface,ColorTranslator.FromHtml(p.Text));
   p.Effects.Input=0;p.InputOpacity=0;p.Effects.Chat=0;
  }
  p.Description=(light?"浅表面＋深字":"深表面＋浅字")+"；重点色用于选中项与发送气泡。按区域估算衬底，保留聊天原图；复杂背景可继续微调。";
 }
}

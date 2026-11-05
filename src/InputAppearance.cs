using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;using System.IO;using System.Text;
internal sealed class InputSettings{
 public bool Enabled;public int Edge=68,Radius=10,LineWidth=1;public string BorderColor="#B8C9DD";
 public string Key{get{return (Enabled?"1":"0")+"|"+Edge+"|"+Radius+"|"+BorderColor+"|"+LineWidth;}}
 public void Validate(){if(Edge<20||Edge>100||Radius<8||Radius>20||LineWidth<1||LineWidth>3||!Palette.IsColor(BorderColor))throw new InvalidDataException("输入区边框参数超出范围。");}
}
internal static class InputAppearance{
 static string FileName{get{return AppearanceProfiles.Path("input-border.txt");}}
 internal static InputSettings Parse(string text){var p=text.Trim().TrimStart('\uFEFF').Split('|');if(p.Length!=5&&p.Length!=7)throw new InvalidDataException("输入区边框设置格式不完整。");int offset=p.Length==7?1:0;var s=new InputSettings{Enabled=p[0]=="1",Edge=int.Parse(p[1+offset]),Radius=int.Parse(p[2+offset]),BorderColor=p[3+offset],LineWidth=int.Parse(p[4+offset])};s.Validate();return s;}
 public static InputSettings Load(){try{return Parse(File.ReadAllText(FileName));}catch{return new InputSettings();}}
 public static void Save(InputSettings s){s.Validate();Directory.CreateDirectory(Engine.Store);string stage=FileName+"."+Guid.NewGuid().ToString("N")+".tmp";File.WriteAllText(stage,s.Key,Encoding.UTF8);if(File.Exists(FileName))File.Replace(stage,FileName,null);else File.Move(stage,FileName);}
 public static Bitmap Texture(InputSettings s,EffectSettings tint=null){s.Validate();var image=new Bitmap(64,64,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(image)){g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.Clear(Color.Transparent);using(var shape=Round(new RectangleF(4,4,56,56),s.Radius)){if(tint!=null&&tint.Input>0)using(var fill=new SolidBrush(Color.FromArgb((int)Math.Round(tint.Input*2.55),ColorTranslator.FromHtml(BackgroundEffects.RegionColor(tint,2)))))g.FillPath(fill,shape);using(var outline=new Pen(Color.FromArgb((int)(s.Edge*2.55),ColorTranslator.FromHtml(s.BorderColor)),s.LineWidth))g.DrawPath(outline,shape);}}return image;}
 static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 public static string Prepare(InputSettings s,EffectSettings tint=null){string key=Engine.Hash(Encoding.UTF8.GetBytes("input-layer-v2|"+s.Key+(tint!=null&&tint.Input>0?"|"+BackgroundEffects.RegionColor(tint,2)+"|"+tint.Input:"")));string dir=Path.Combine(Engine.Store,"input-border-cache");Directory.CreateDirectory(dir);string path=Path.Combine(dir,key+".png");if(!File.Exists(path))using(var bitmap=Texture(s,tint))bitmap.Save(path,ImageFormat.Png);return path;}
 public static void DrawNine(Graphics g,Image image,RectangleF target){float[] a={0,24,40,64},x={target.Left,target.Left+24,target.Right-24,target.Right},y={target.Top,target.Top+24,target.Bottom-24,target.Bottom};for(int row=0;row<3;row++)for(int col=0;col<3;col++)g.DrawImage(image,new RectangleF(x[col],y[row],x[col+1]-x[col],y[row+1]-y[row]),new RectangleF(a[col],a[row],a[col+1]-a[col],a[row+1]-a[row]),GraphicsUnit.Pixel);}
}


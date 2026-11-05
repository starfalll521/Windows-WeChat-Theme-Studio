using System;using System.IO;using System.Text;using System.Collections.Generic;
internal static partial class NativeBackground{
 internal static void ExportNativeConfiguration(string directory){
  Directory.CreateDirectory(directory);var b=new StringBuilder();var theme=Engine.Current();
  var files=SvgFiles();var routes=new List<string>(files.Keys);routes.Sort(StringComparer.Ordinal);if(routes.Count>4096)throw new InvalidDataException("SVG routes exceed native capacity");
  b.AppendLine("[runtime]").AppendLine("enabled=1").AppendLine("routes="+routes.Count);
  b.AppendLine("[palette]");for(int i=0;i<16;i++)b.AppendLine("color"+i+"="+theme.Colors[i]);
  string source=AutoBackground.SavedImage();if(File.Exists(Path.Combine(Engine.Store,"background.txt"))&&source==null)throw new InvalidDataException("方案壁纸无效，已拒绝将其当作无背景导出。");var routeFiles=new string[2,routes.Count];var preserveColors=new bool[2,routes.Count];var routeEnabled=new bool[2];
  for(int mode=0;mode<2;mode++)using(AppearanceProfiles.Use(mode)){
   b.AppendLine(mode==0?"[light]":"[dark]");foreach(string role in new[]{"text","chat","secondary","canvas","link","icons","accent"})b.AppendLine(role+"="+ReadInk(role));
   b.AppendLine("selectedText="+SmartPalette.SelectionInk(theme,mode,ReadInk("text")));
   var s=IconEffects.Load();routeEnabled[mode]=s.Mode!=0;b.AppendLine("iconMode="+s.Mode).AppendLine("brightness="+s.Brightness).AppendLine("saturation="+s.Saturation).AppendLine("contrast="+s.Contrast).AppendLine("visibility="+s.Visibility);
   var f=BackgroundEffects.Load();var g=InputAppearance.Load();
   b.AppendLine("wall="+(source==null?"":BackgroundEffects.Prepare(source))).AppendLine("inputBorder="+(g.Enabled?InputAppearance.Prepare(g,f):""));
   b.AppendLine("inputFull="+(mode==0&&f.Input>0?"1":"0"));
   int[] opacity={f.Friends,f.Chat,g.Enabled?0:f.Input,f.Header,f.Navigation};for(int i=0;i<5;i++)b.AppendLine("mask"+i+"="+(opacity[i]==0?"":BackgroundEffects.Mask(BackgroundEffects.RegionColor(f,i),opacity[i])));
   int[] regions={0,4,2};string processed=source==null?null:BackgroundEffects.Prepare(source);for(int i=0;i<3;i++)b.AppendLine("frost"+i+"="+(processed==null?"":BackgroundEffects.PrepareFrost(processed,BackgroundEffects.RegionBlur(f,regions[i]))));
   for(int i=0;i<routes.Count;i++){string file=s.Mode!=0&&s.Outline>0?StrongSvg(routes[i],s.Outline):files[routes[i]];string foreground=ReadInk("icons");if(s.Mode!=0&&s.Outline>0&&Palette.IsColor(foreground)){string contrast=ContrastIconSvg(routes[i],file,foreground);preserveColors[mode,i]=contrast!=file;file=contrast;}routeFiles[mode,i]=file;}
  }
  string binary=Path.Combine(directory,"routes.bin"),stage=binary+".tmp";
  using(var writer=new BinaryWriter(File.Create(stage))){writer.Write(Encoding.ASCII.GetBytes("WXRTE002"));writer.Write(routes.Count);for(int i=0;i<routes.Count;i++)for(int mode=0;mode<2;mode++){foreach(string text in new[]{routeEnabled[mode]?routes[i]:"",routeFiles[mode,i]}){if(text.Length>1023)throw new InvalidDataException("Native asset path too long");writer.Write(text.Length);writer.Write(Encoding.Unicode.GetBytes(text));}writer.Write(preserveColors[mode,i]?1:0);}}
  if(File.Exists(binary))File.Replace(stage,binary,null);else File.Move(stage,binary);
  string target=Path.Combine(directory,"runtime.ini"),tmp=target+".tmp";File.WriteAllText(tmp,b.ToString(),Encoding.Unicode);if(File.Exists(target))File.Replace(tmp,target,null);else File.Move(tmp,target);
 }
}

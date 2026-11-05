using System;using System.Drawing;
internal static partial class SmartPalette{
 static string ReferenceTone(string value,string seed,string anchor){var tone=ToOk(ColorTranslator.FromHtml(value));var chosen=ToOk(ColorTranslator.FromHtml(seed));var original=ToOk(ColorTranslator.FromHtml(anchor));double angle=(chosen.Hue-original.Hue+540)%360-180;return Math.Abs(angle)<8?value:Hex(Tone(tone.Hue+angle,tone.C,tone.L));}
 internal static void ApplyImageHarmony(SmartColors p,PaletteAssignment map){
  ApplyReferenceHarmony(p,map);int m=p.RuntimeMode;var primary=ToOk(ColorTranslator.FromHtml(map.Primary));var secondary=ToOk(ColorTranslator.FromHtml(map.Secondary));var neutral=ToOk(ColorTranslator.FromHtml(map.Neutral));p.Text=Hex(Tone(primary.Hue,.006,m==1?.97:.2));p.Icons=Hex(Tone(primary.Hue,.012,m==1?.91:.32));p.Border=Hex(Tone(primary.Hue,.02,m==1?.74:.5));p.Effects.Color=Hex(Tone(primary.Hue,.008,m==1?.18:.97));Color ink=ColorTranslator.FromHtml(p.Text);
  for(int i=0;i<8;i++){var reference=ToOk(ColorTranslator.FromHtml(p.Theme.Colors[i*2+m]));double h=i==7?(secondary.C<.04?primary.Hue:secondary.Hue):i>=5?primary.Hue:neutral.Hue;double c=i==7?Math.Max(.008,Math.Min(.035,secondary.C*.28)):i>=5?Math.Min(reference.C,primary.C*.55):Math.Min(.018,neutral.C*.25);Color color=m==1?Surface(h,c,reference.L,ink):VividLightSurface(h,c,reference.L,ink);p.Theme.Colors[i*2+m]=Hex(color);}
  var accent=ToOk(ColorTranslator.FromHtml(map.Accent));double angle=(accent.Hue-primary.Hue+540)%360-180;double hue=primary.Hue+Clamp(angle,-28,28);Color accentColor=Tone(hue,Math.Min(.095,Math.Max(.04,accent.C)),m==1?.78:.43);p.Accent=Hex(Readable(accentColor,ColorTranslator.FromHtml(p.Theme.Colors[10+m]),3,m==1));p.Theme.Name="智能协调 · 参考层级";if(m==0)ApplyLightHierarchy(p,map);
 }

 internal static void ApplyLightHierarchy(SmartColors p,PaletteAssignment map){
  var primary=ToOk(ColorTranslator.FromHtml(map.Primary));double anchor=ToOk(ColorTranslator.FromHtml("#4B93C8")).Hue,angle=(primary.Hue-anchor+540)%360-180;
  Func<string,string> adapt=value=>{var color=ToOk(ColorTranslator.FromHtml(value));return Hex(Tone(color.Hue+angle,primary.C<.015?0:color.C*(p.Variant==2?.7:1),color.L));};
  string[] values={"#E8F4FC","#E5F1F8","#F1F5F7","#DFEEF7","#DCEEF8","#527F9C","#D6EDF7","#F1F5F7"};for(int i=0;i<8;i++)p.Theme.Colors[i*2]=adapt(values[i]);
  p.Text=adapt("#172D3B");p.SecondaryText=adapt("#526775");p.Icons=adapt("#081B2B");p.Border="#FFFFFF";p.Accent=adapt("#527F9C");
  Color selected=ColorTranslator.FromHtml(p.Theme.Colors[10]);if(Contrast(Color.White,selected)<4.5)p.Theme.Colors[10]=Hex(Readable(selected,Color.White,4.5,false));
  p.Effects=new EffectSettings{Brightness=108,Saturation=75,Contrast=85,Tint=28,Blur=0,Color=adapt("#E8F4FC"),Friends=72,Navigation=50,Input=40,Header=0,Chat=0,ListBlur=18,NavigationBlur=12,InputBlur=0,ListColor=adapt("#E5F1F8"),NavigationColor=adapt("#DCEEF8"),InputColor=adapt("#A9D4EE")};p.InputOpacity=0;p.Theme.Name="浅色参考 · 分区壁纸层级";
 }
 internal static string SelectionInk(Theme theme,int mode,string normal){if(theme==null||theme.Colors==null||theme.Colors.Length<16)return Palette.IsColor(normal)?normal:mode==0?"#171B20":"#FFFFFF";Color background=ColorTranslator.FromHtml(theme.Colors[10+mode]);Color ink=Palette.IsColor(normal)?ColorTranslator.FromHtml(normal):mode==0?Color.Black:Color.White;return Hex(Contrast(Color.White,background)>Contrast(ink,background)?Color.White:ink);}
 internal static string CanvasFor(SmartColors p){return p.Text;}
 internal static void ApplyReferenceHarmony(SmartColors p,PaletteAssignment map){
  int m=p.RuntimeMode;string[] dark={"#1B110F","#221614","#150D0C","#2E1A17","#140B09","#77251C","#611E16","#35221F"};string[] light={"#F4EEEA","#F6F0EC","#FAF5F1","#EEE4DE","#E8DDD5","#DAB5AA","#E9C8BE","#F1E6E0"};var source=m==1?dark:light;
  for(int i=0;i<8;i++){string seed=i==7?map.Secondary:i>=5?map.Primary:map.Neutral;string anchor=i==7?"#C97B7C":i>=5?"#F91111":"#530C06";p.Theme.Colors[i*2+m]=source[i];}
  p.Text=m==1?"#FAF3F2":"#2A211E";p.Icons=m==1?"#F0E6E2":"#4C3C36";
  // Accent belongs to the same hue family as the main surface; preserve a
  // softer lightness/chroma hierarchy instead of making every control vivid.
  p.Accent=m==1?"#F49889":"#9C493C";p.Border=m==1?"#BFA9A0":"#8D7168";
  p.Effects=new EffectSettings{Brightness=100,Blur=0,Tint=0,Color=m==1?"#1B110F":"#F4EEEA",Friends=m==1?30:68,Chat=0,Header=m==1?18:64,Navigation=m==1?20:65,Input=0};p.InputOpacity=0;p.Theme.Name="参考图协调 · 暖红层次";
 }
}

using System;using System.Drawing;using System.Linq;using System.Collections.Generic;
internal static partial class SmartPalette{
 internal static void TuneUiContrast(string path,SmartColors p,PaletteAssignment map){
  if(p.RuntimeMode==0){p.Description="参考参数：壁纸亮度108%、饱和度75%、对比度85%，整体薄蒙版28%；列表背景78%/模糊18px，导航50%/12px，输入75%/20px（以1600px窗口为基准）。只处理背景，不降低文字、图标、头像或消息图片的不透明度；顶部无底板，原图标无独立衬底。颜色随壁纸同源变化，参数可调，均为建议起点。";return;}
  var seed=ToOk(ColorTranslator.FromHtml(map.Primary));p.Icons=Hex(Tone(seed.Hue,.008,.97));p.InputOpacity=0;p.Effects.Input=0;p.Description="深色角色层级保持独立。";
 }
}
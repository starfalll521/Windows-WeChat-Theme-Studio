using System;using System.IO;using System.Linq;using System.Text;
internal static partial class NativeDeployment{
 internal static void NormalizeProfiles(string dir){bool legacyLight=File.Exists(Path.Combine(dir,"runtime-appearance.txt"))&&File.ReadAllText(Path.Combine(dir,"runtime-appearance.txt")).Trim()=="light";
  for(int mode=0;mode<2;mode++){string profile=Path.Combine(dir,"appearance-"+(mode==0?"light":"dark"));bool explicitProfile=File.Exists(Path.Combine(profile,"profile.ready"));Directory.CreateDirectory(profile);
   foreach(string key in AppearanceProfiles.Keys){if(key=="input-glass.txt")continue;string target=Path.Combine(profile,key);string raw=File.Exists(target)?File.ReadAllText(target):null;
    if(raw==null&&!explicitProfile&&mode==(legacyLight?0:1)&&File.Exists(Path.Combine(dir,key)))raw=File.ReadAllText(Path.Combine(dir,key));
    if(raw==null)raw=key=="background-effects.txt"?new EffectSettings().Key:key=="input-border.txt"?new InputSettings().Key:key=="icon-effects.txt"?"0|100|100|100|0|0":"original";
    raw=raw.Trim().TrimStart('\uFEFF');if(key=="background-effects.txt")raw=BackgroundEffects.Parse(raw).Key;else if(key=="input-border.txt")raw=InputAppearance.Parse(raw).Key;else if(key=="icon-effects.txt"){var f=raw.Split('|');if(f.Length!=6)throw new InvalidDataException("图标设置格式不完整。");new IconSettings{Mode=int.Parse(f[0]),Brightness=int.Parse(f[1]),Saturation=int.Parse(f[2]),Contrast=int.Parse(f[3]),Visibility=int.Parse(f[4]),Outline=int.Parse(f[5])}.Validate();}else if(raw!="original"&&!Palette.IsColor(raw)&&!(key=="ink-canvas.txt"&&raw=="auto"))throw new InvalidDataException("方案颜色无效："+key);
    File.WriteAllText(target,raw,new UTF8Encoding(false));
   }File.WriteAllText(Path.Combine(profile,"profile.ready"),"1",new UTF8Encoding(false));
  }
  // Completed mode profiles are authoritative; legacy root defaults cannot leak in.
  foreach(string key in AppearanceProfiles.Keys){string file=Path.Combine(dir,key);if(File.Exists(file))File.Delete(file);}
  foreach(string mode in new[]{"light","dark"}){string file=Path.Combine(dir,"appearance-"+mode,"input-glass.txt");if(File.Exists(file))File.Delete(file);}
  File.WriteAllText(Path.Combine(dir,"snapshot-format.txt"),"single-draft-v1",Encoding.ASCII);
 }
 internal static bool SameContent(string first,string second){var a=ThemeTransfer.Read(Path.Combine(first,"approved.wxtheme"));var b=ThemeTransfer.Read(Path.Combine(second,"approved.wxtheme"));if(Palette.Pack(a.Theme)!=Palette.Pack(b.Theme)||(a.Wallpaper==null)!=(b.Wallpaper==null)||a.Settings.Count!=b.Settings.Count)return false;if(a.Wallpaper!=null&&!a.Wallpaper.SequenceEqual(b.Wallpaper))return false;return a.Settings.All(pair=>b.Settings.ContainsKey(pair.Key)&&pair.Value.SequenceEqual(b.Settings[pair.Key]));}
 internal static string SnapshotSummary(string dir){return "方案 "+Path.GetFileName(dir)+" · "+(File.Exists(Path.Combine(dir,"background.txt"))?"壁纸已包含":"本次未选择壁纸")+" · 浅深色配置完整";}
}

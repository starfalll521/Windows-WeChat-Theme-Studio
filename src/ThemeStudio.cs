using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

[DataContract] public sealed class Theme {
    [DataMember] public string Name;
    [DataMember] public string[] Colors;
    public Theme Copy() {return new Theme {Name=Name,Colors=(string[])Colors.Clone()};}
    public override string ToString() {return Name;}
}
internal static class Palette {
    public static readonly string[] Keys={"bg0","bg1","bg2","bg3","navigation_bar","bg_selection","chat_right_bubble_color","chat_left_bubble_color"};
    public static readonly string[] Labels={"窗口底色","会话列表","聊天背景","辅助面板","左侧导航","选中的会话","发送气泡","接收气泡"};
    public static readonly int[] Offsets={163350140,163350188,163350236,163350284,163350956,163350380,163361996,163362044};
    public static readonly string[] Originals={"ffe8e6e6","ff3d3b3b","fff0eeee","ff302f2f","fffafafa","ff1f1e1e","fff7f7f7","ff242424","ffe0e0e0","ff262525","ff70ac15","ff69a80d","ff9ff29d","ff8dd235","fff0eeee","ff302f2f"};
    public static bool IsColor(string c) {return c!=null&&Regex.IsMatch(c,"^#[0-9A-Fa-f]{6}$");}
    public static void Validate(Theme t) {
        if(t==null||String.IsNullOrWhiteSpace(t.Name)||t.Name.Length>40||t.Colors==null||t.Colors.Length!=16||t.Colors.Any(c=>!IsColor(c))) throw new InvalidDataException("主题需要名称和 16 个 #RRGGBB 颜色值。");
    }
    public static byte[] Hex(string s) {return Enumerable.Range(0,s.Length/2).Select(i=>Convert.ToByte(s.Substring(i*2,2),16)).ToArray();}
    public static byte[] Encode(string c) {if(!IsColor(c))throw new InvalidDataException("颜色格式应为 #RRGGBB");return new[]{(byte)255,Convert.ToByte(c.Substring(5,2),16),Convert.ToByte(c.Substring(3,2),16),Convert.ToByte(c.Substring(1,2),16)};}
    public static string Decode(byte[] b,int offset) {return String.Format("#{0:X2}{1:X2}{2:X2}",b[offset+3],b[offset+2],b[offset+1]);}
    public static Theme Make(string name,string[] light,string[] dark) {var a=new string[16];for(int i=0;i<8;i++){a[i*2]=light[i];a[i*2+1]=dark[i];}var t=new Theme{Name=name,Colors=a};Validate(t);return t;}
    public static List<Theme> Presets() {return new List<Theme>{
        Make("雾蓝",new[]{"#DFE5EB","#EAF0F5","#F7FAFC","#F1F5F9","#D5DFE9","#347CB7","#BDDDF4","#E5EDF5"},new[]{"#252F3A","#202A35","#17212B","#1B2632","#141E29","#4B93C8","#315D7C","#293848"}),
        Make("海盐绿",new[]{"#DDE8E2","#EAF3ED","#F7FBF8","#F0F6F2","#D2E1D8","#31745D","#BCE3CD","#E1EEE6"},new[]{"#25362E","#202F28","#17261F","#1C2C24","#14211B","#33765F","#315D49","#293D32"}),
        Make("樱花",new[]{"#EADFE4","#F6EDF1","#FDF9FB","#F9F1F5","#E5D3DD","#9A5278","#F0CDDE","#F1E5EC"},new[]{"#352831","#2F222B","#251A22","#2B1F28","#21161E","#914C71","#603950","#3C2C36"}),
        Make("暖砂",new[]{"#E8E0D5","#F3EEE6","#FCFAF6","#F7F2EA","#DED3C4","#85613B","#EAD4B7","#EFE8DC"},new[]{"#352E26","#2F2922","#252019","#2B251E","#211C16","#88613A","#604A31","#3D3428"}),
        Make("石墨",new[]{"#DFE1E4","#EDEEF0","#FAFAFB","#F3F4F5","#D4D7DB","#535D6B","#D4DDE9","#E6E8EC"},new[]{"#292D33","#24282E","#1A1D22","#202329","#16191E","#505B6A","#3D4C60","#2D333C"})};}
    public static string Pack(Theme t) {Validate(t);return String.Join(",",t.Colors.Select(c=>c.Substring(1)));}
    public static Theme Unpack(string s) {var t=new Theme{Name="应用主题",Colors=s.Split(',').Select(c=>"#"+c).ToArray()};Validate(t);return t;}
}
internal static class Engine {
    public static string Target {get{return WeixinLocator.Current().Dll;}}
    public static string Exe {get{return WeixinLocator.Current().Launcher;}}
    public const string OriginalHash="e0428a7b691853de496532523c2c09aa095f90941c37c9f4cfb3f137f3c66556";
    public const int Length=204305968;
    public static string Backup {get{return Target+".theme-original-4.1.15.56.bak";}}
    internal static string StoreOverride;
    public static string Store {get{return StoreOverride??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"WeixinThemeStudio");}}
    public static string Hash(byte[] b) {using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
    public static bool Admin {get{return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);}}
    public static void RequireStopped() {if(Process.GetProcessesByName("Weixin").Length>0||Process.GetProcessesByName("WeChat").Length>0)throw new InvalidOperationException("请先从系统托盘完全退出微信，再应用或恢复。工具不会强制结束微信。");}
    public static byte[] Normalize(byte[] source) {
        if(source.Length!=Length)throw new InvalidDataException("微信版本或文件大小不匹配，仅支持 4.1.15.56。");
        var b=(byte[])source.Clone();
        for(int i=0;i<8;i++)for(int mode=0;mode<2;mode++) Buffer.BlockCopy(Palette.Hex(Palette.Originals[i*2+mode]),0,b,Palette.Offsets[i]+mode*4,4);
        if(Hash(b)!=OriginalHash)throw new InvalidDataException("微信程序已更新或存在其他修改，已拒绝操作。请使用未修改的 4.1.15.56。");
        return b;
    }
    public static byte[] Build(byte[] original,Theme t) {
        Palette.Validate(t); if(Hash(original)!=OriginalHash)throw new InvalidDataException("原始文件校验失败。");
        var b=(byte[])original.Clone();for(int i=0;i<8;i++)for(int m=0;m<2;m++) Buffer.BlockCopy(Palette.Encode(t.Colors[i*2+m]),0,b,Palette.Offsets[i]+4*m,4);return b;
    }
    public static string ActiveThemePath {get{return Path.Combine(Store,"theme-active.json");}}
    public static Theme ActiveTheme {get{return File.Exists(ActiveThemePath)?Load(ActiveThemePath):null;}}
    static void SaveActive(Theme t,bool restore){Directory.CreateDirectory(Store);if(restore){if(File.Exists(ActiveThemePath))File.Delete(ActiveThemePath);AppearanceProfiles.Clear();return;}Palette.Validate(t);string temp=ActiveThemePath+".tmp";Save(temp,t);if(File.Exists(ActiveThemePath))File.Replace(temp,ActiveThemePath,null);else File.Move(temp,ActiveThemePath);AppearanceProfiles.EnsureProfiles();}
    public static void SaveRuntimeTheme(Theme t){Normalize(File.ReadAllBytes(Target));SaveActive(t,false);}
    public static bool RepairLegacyAppearance(){var theme=ActiveTheme;if(theme==null||!Enumerable.Range(0,8).All(i=>theme.Colors[i*2]==theme.Colors[i*2+1]))return false;string source=AutoBackground.SavedImage();if(source==null)return false;bool changed=false;for(int mode=0;mode<2;mode++){using(AppearanceProfiles.Use(mode)){string value=NativeBackground.InkChat;var ink=Palette.IsColor(value)?ColorTranslator.FromHtml(value):(mode==0?Color.Black:Color.White);if(new[]{12+mode,14+mode}.All(i=>SmartPalette.Contrast(ink,ColorTranslator.FromHtml(theme.Colors[i]))>=4.5))continue;}var recipe=SmartPalette.Generate(source,0,mode);for(int i=0;i<8;i++)theme.Colors[i*2+mode]=recipe.Theme.Colors[i*2+mode];changed=true;}if(changed){string dir=Path.Combine(Store,"migration-6.3");Directory.CreateDirectory(dir);string before=Path.Combine(dir,"theme-before-appearance-repair.json");if(!File.Exists(before))Save(before,ActiveTheme);SaveActive(theme,false);}return changed;}
    public static byte[] BuildLoginSafe(byte[] original,Theme theme){Palette.Validate(theme);if(Hash(original)!=OriginalHash)throw new InvalidDataException("原始文件校验失败。");return (byte[])original.Clone();}
    public static Theme Current() {var b=File.ReadAllBytes(Target);Normalize(b);if(ActiveTheme!=null)return ActiveTheme;var colors=new string[16];for(int i=0;i<8;i++)for(int m=0;m<2;m++)colors[2*i+m]=Palette.Decode(b,Palette.Offsets[i]+4*m);return new Theme{Name="当前微信配色",Colors=colors};}
    public static void Apply(Theme theme,bool restore) {
        if(!Admin)throw new UnauthorizedAccessException("写入微信安装目录需要管理员权限。");
        RequireStopped();var current=File.ReadAllBytes(Target);var original=Normalize(current);
        if(File.Exists(Backup)) {if(Hash(File.ReadAllBytes(Backup))!=OriginalHash)throw new InvalidDataException("原始备份校验失败，已拒绝修改。");}
        else {WriteNew(Backup,original);}
        if(!restore)Palette.Validate(theme);var next=restore?original:BuildLoginSafe(original,theme);
        if(next.SequenceEqual(current)){SaveActive(theme,restore);return;}
        string stage=Path.Combine(Path.GetDirectoryName(Target),"Weixin.theme-stage-"+Guid.NewGuid().ToString("N")+".dll");
        string rollback=stage+".previous";
        try {
            WriteNew(stage,next);RequireStopped();
            if(Hash(File.ReadAllBytes(Target))!=Hash(current))throw new IOException("操作期间微信文件发生变化，已停止。");
            File.Replace(stage,Target,rollback,true);
            try {if(Hash(File.ReadAllBytes(Target))!=Hash(next))throw new IOException("应用后的文件校验失败。");}
            catch {File.Replace(rollback,Target,null,true);throw;}
        } finally {if(File.Exists(stage))File.Delete(stage);if(File.Exists(rollback))File.Delete(rollback);}
        SaveActive(theme,restore);
    }
    static void WriteNew(string p,byte[] b) {using(var f=new FileStream(p,FileMode.CreateNew,FileAccess.Write,FileShare.None)){f.Write(b,0,b.Length);f.Flush(true);}}
    public static void Save(string path,Theme t) {Palette.Validate(t);using(var f=File.Create(path))new DataContractJsonSerializer(typeof(Theme)).WriteObject(f,t);}
    public static Theme Load(string path) {if(new FileInfo(path).Length>32768)throw new InvalidDataException("主题文件过大。");using(var f=File.OpenRead(path)){var t=(Theme)new DataContractJsonSerializer(typeof(Theme)).ReadObject(f);Palette.Validate(t);return t;}}
    public static int SelfTest() {
        var current=File.ReadAllBytes(Target);var original=Normalize(current);var entries=new List<string>();
        foreach(var t in Palette.Presets()) {
            var next=Build(original,t);var allowed=new HashSet<int>();foreach(int o in Palette.Offsets)for(int k=0;k<8;k++)allowed.Add(o+k);
            for(int i=0;i<next.Length;i++)if(next[i]!=original[i]&&!allowed.Contains(i))throw new Exception("Unexpected modified byte");
            for(int i=0;i<8;i++)for(int m=0;m<2;m++)if(Palette.Decode(next,Palette.Offsets[i]+4*m)!=t.Colors[i*2+m])throw new Exception("Color round-trip failed");
            if(Hash(Normalize(next))!=OriginalHash)throw new Exception("Restore failed");entries.Add(t.Name+": whitelist + restore + color readback PASS");
        }
        bool rejected=false;try{Palette.Unpack("000000");}catch{rejected=true;}if(!rejected)throw new Exception("Invalid theme accepted");
        var bad=(byte[])current.Clone();bad[100]^=1;rejected=false;try{Normalize(bad);}catch{rejected=true;}if(!rejected)throw new Exception("Unknown binary accepted");
        Directory.CreateDirectory(Store);string temp=Path.Combine(Store,"self-test-theme.json");Engine.Save(temp,Palette.Presets()[0]);if(Palette.Pack(Load(temp))!=Palette.Pack(Palette.Presets()[0]))throw new Exception("JSON round-trip failed");File.Delete(temp);
        entries.Add("Malformed input / foreign binary rejection / JSON round-trip PASS");entries.Add("Original installation untouched: "+Hash(File.ReadAllBytes(Target)));File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"self-test.txt"),entries,Encoding.UTF8);return 0;
    }
}

internal sealed class Preview : Control {
    public Theme Theme; public int Mode; public Image ChatBackground;
    public Preview() {DoubleBuffered=true;ResizeRedraw=true;}
    Color C(int i){return ColorTranslator.FromHtml(Theme.Colors[i*2+Mode]);}
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);if(Theme==null)return;var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        float scale=Math.Min(Width/580f,Height/470f);g.ScaleTransform(scale,scale);
        Color ink=Mode==0?Color.FromArgb(35,43,52):Color.FromArgb(231,236,242);Color muted=Mode==0?Color.FromArgb(110,120,130):Color.FromArgb(157,170,184);
        Fill(g,C(0),0,0,580,470);Fill(g,C(4),0,30,42,440);Fill(g,C(1),42,30,156,440);Fill(g,C(2),198,30,382,440);
        if(ChatBackground!=null){if(NativeBackground.WholeEnabled){using(var background=BackdropStyle.Compose(ChatBackground,580,470,new Rectangle(198,82,382,282),100,true))g.DrawImageUnscaled(background,0,0);}else{var saved=g.Save();var region=new Rectangle(198,82,382,282);g.SetClip(region);double fit=Math.Max(region.Width/(double)ChatBackground.Width,region.Height/(double)ChatBackground.Height);float w=(float)(ChatBackground.Width*fit),h=(float)(ChatBackground.Height*fit);g.DrawImage(ChatBackground,region.X+(region.Width-w)/2,region.Y+(region.Height-h)/2,w,h);g.Restore(saved);}}
        Text(g,"微信配色预览",ink,14,6,10);Text(g,"—   □   ×",muted,500,6,9);
        Fill(g,Mode==0?Color.White:Color.FromArgb(48,58,69),54,43,130,26);Text(g,"搜索",muted,64,48,9);
        Fill(g,C(5),42,84,156,59);Text(g,"文件传输助手",ink,88,94,10);Text(g,"主题已经准备好了",ink,88,117,8);
        Avatar(g,58,99,Color.FromArgb(0,192,104),"文");
        Text(g,"朋友",ink,88,159,10);Text(g,"头像和图片保留原色",muted,55,184,8);Avatar(g,58,158,Color.Coral,"友");
        Text(g,"文件传输助手",ink,215,47,11);
        using(var pen=new Pen(Mode==0?Color.FromArgb(230,234,239):Color.FromArgb(49,60,72)))g.DrawLine(pen,198,81,580,81);
        Text(g,"11:30",muted,365,101,8);Avatar(g,211,139,Color.Coral,"友");
        Bubble(g,C(7),249,137,245,48);Text(g,"这里是接收到的消息",ink,262,151,10);
        Bubble(g,C(6),315,208,208,48);Text(g,"发送气泡可以单独选色",ink,326,223,10);Avatar(g,536,214,Color.SteelBlue,"我");
        Fill(g,C(3),249,279,190,56);Text(g,"原色示意",ink,260,284,9);
        Color[] dots={Color.FromArgb(0,193,96),Color.Coral,Color.RoyalBlue,Color.Gold};for(int i=0;i<4;i++)Fill(g,dots[i],260+i*38,307,28,16);
        Fill(g,C(3),198,364,382,106);Text(g,"☺   □   ✂   ♫",muted,215,374,12);Text(g,"在右侧观察配色，应用后重启微信生效。",ink,214,407,9);
        Fill(g,Color.FromArgb(0,193,96),505,437,57,23);Text(g,"发送",Color.White,518,441,9);
        Text(g,"●",Color.FromArgb(0,193,96),12,61,15);Text(g,"☷",muted,12,111,15);Text(g,"□",muted,12,156,15);Text(g,"☰",muted,12,431,15);
    }
    static void Fill(Graphics g,Color c,float x,float y,float w,float h){using(var b=new SolidBrush(c))g.FillRectangle(b,x,y,w,h);}
    new static void Text(Graphics g,string t,Color c,float x,float y,float size){using(var f=new Font("Microsoft YaHei UI",size))using(var b=new SolidBrush(c))g.DrawString(t,f,b,x,y);}
    static void Avatar(Graphics g,float x,float y,Color c,string s){Fill(g,c,x,y,28,28);Text(g,s,Color.White,x+5,y+5,9);}
    static void Bubble(Graphics g,Color c,float x,float y,float w,float h){using(var p=new GraphicsPath()){p.AddArc(x,y,10,10,180,90);p.AddArc(x+w-10,y,10,10,270,90);p.AddArc(x+w-10,y+h-10,10,10,0,90);p.AddArc(x,y+h-10,10,10,90,90);p.CloseFigure();using(var b=new SolidBrush(c))g.FillPath(b,p);}}
}

internal sealed partial class Studio : Form {
    readonly ComboBox presets=new ComboBox(),mode=new ComboBox(); readonly TextBox themeName=new TextBox();
    readonly TextBox[] hex=new TextBox[8]; readonly Button[] swatches=new Button[8];
    readonly Preview preview=new Preview(); readonly Label status=new Label(),contrast=new Label();
    readonly Button apply=new Button(),restore=new Button(); Theme current; bool syncing;
    string backgroundSource; bool backgroundBusy; readonly List<Button> backgroundButtons=new List<Button>();
    readonly List<Theme> themes=Palette.Presets();
    public Studio() {
        Text="微信调色室 4.2.1 · 图标与轮廓";Font=new Font("Microsoft YaHei UI",10);BackColor=Color.FromArgb(246,248,250);ClientSize=new Size(1040,735);MinimumSize=new Size(1056,774);StartPosition=FormStartPosition.CenterScreen;AutoScaleMode=AutoScaleMode.Dpi;
        var title=new Label {Text="微信调色室",Font=new Font(Font.FontFamily,23,FontStyle.Bold),ForeColor=Color.FromArgb(31,49,66),Location=new Point(24,17),Size=new Size(280,43)};Controls.Add(title);
        AddLabel("整窗原图 · 不淡化、不加渐变",24,67,450,24,Color.FromArgb(100,113,126));
        var auto=new CheckBox{Text="自动跟随微信启动和窗口尺寸",Checked=AutoBackground.Enabled,Location=new Point(532,34),Size=new Size(470,26),AutoSize=false};Controls.Add(auto);auto.CheckedChanged+=delegate{try{AutoBackground.SetEnabled(auto.Checked);status.Text=auto.Checked?"自动背景已启用，调色室托盘服务会在微信启动及尺寸稳定后自动适配。":"自动背景已停用；当前图片保留，可点移除背景。";}catch(Exception e){AppLog.Error("UI",e);status.Text=e.Message;}};
        var whole=new CheckBox{Text="整窗连续原图背景",Checked=NativeBackground.WholeEnabled,Location=new Point(532,66),Size=new Size(470,26),AutoSize=false};Controls.Add(whole);whole.CheckedChanged+=delegate{NativeBackground.WholeEnabled=whole.Checked;preview.Invalidate();status.Text=AutoBackground.Enabled?"背景范围已更改，自动服务将刷新微信。":"背景范围已更改，点击应用背景生效。";};
        AddLabel("主题",24,112,60,24,Color.FromArgb(49,65,81));presets.SetBounds(90,108,240,29);presets.DropDownStyle=ComboBoxStyle.DropDownList;Controls.Add(presets);
        AddLabel("编辑",24,154,60,24,Color.FromArgb(49,65,81));mode.Items.AddRange(new object[]{"浅色外观","深色外观"});mode.SelectedIndex=0;mode.DropDownStyle=ComboBoxStyle.DropDownList;mode.SetBounds(90,150,240,29);Controls.Add(mode);
        for(int i=0;i<8;i++) {
            int index=i,y=198+i*42;AddLabel(Palette.Labels[i],24,y+5,116,25,Color.FromArgb(49,65,81));
            var sw=new Button{FlatStyle=FlatStyle.Flat,AccessibleName=Palette.Labels[i]+"选色"};sw.FlatAppearance.BorderColor=Color.FromArgb(210,217,224);sw.SetBounds(145,y,42,30);sw.Click+=delegate {Pick(index);};Controls.Add(sw);swatches[i]=sw;
            var box=new TextBox {CharacterCasing=CharacterCasing.Upper,MaxLength=7,AccessibleName=Palette.Labels[i]+"色值"};box.SetBounds(199,y,131,30);Controls.Add(box);hex[i]=box;box.TextChanged+=delegate {Edit(index);};
            box.Leave+=delegate {if(!Palette.IsColor(box.Text)) {status.Text="颜色格式应为 #RRGGBB，请修正标记为红色的色值。";}};
        }
        AddLabel("主题名称",24,552,85,24,Color.FromArgb(49,65,81));themeName.SetBounds(112,547,218,29);themeName.MaxLength=40;Controls.Add(themeName);
        Button save=Btn("保存主题",24,590,96),export=Btn("导出",130,590,94),import=Btn("导入",234,590,96);save.Click+=delegate {SaveTheme();};export.Click+=delegate {ExportTheme();};import.Click+=delegate {ImportTheme();};
        var inkButton=Btn("文字颜色",24,634,96);inkButton.Click+=delegate{using(var dialog=new InkDialog(SetBackground))dialog.ShowDialog(this);};
        var iconButton=Btn("图标轮廓",130,634,94);iconButton.Click+=delegate{using(var dialog=new IconDialog())dialog.ShowDialog(this);};var effectsButton=Btn("背景调节",234,634,96);effectsButton.Click+=delegate{try{using(var dialog=new EffectsDialog())dialog.ShowDialog(this);string selected=AutoBackground.SavedImage();if(selected!=null)SetBackground(selected);}catch(Exception e){AppLog.Error("UI",e);status.Text=e.Message;}};
        var panel=new Panel {BackColor=Color.White,Location=new Point(356,108),Size=new Size(660,538),Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Bottom};Controls.Add(panel);
        preview.SetBounds(18,18,624,486);preview.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Bottom;panel.Controls.Add(preview);
        var pickImage=BackgroundButton(panel,"选择背景图片",18,138);var useImage=BackgroundButton(panel,"应用／重新适配",164,148);var removeImage=BackgroundButton(panel,"移除图片背景",320,140);var launchImage=BackgroundButton(panel,"启动微信并应用",468,174);
        pickImage.Click+=delegate{ChooseBackground();};useImage.Click+=async delegate{await ApplyBackground(false,false);};removeImage.Click+=async delegate{await ApplyBackground(true,false);};launchImage.Click+=async delegate{await ApplyBackground(false,true);};
        contrast.SetBounds(375,650,640,22);contrast.Anchor=AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;contrast.ForeColor=Color.FromArgb(111,92,48);Controls.Add(contrast);
        status.SetBounds(24,680,660,42);status.Anchor=AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;status.ForeColor=Color.FromArgb(85,102,120);Controls.Add(status);
        apply.Text="应用到微信";apply.SetBounds(708,682,145,35);apply.Anchor=AnchorStyles.Bottom|AnchorStyles.Right;apply.BackColor=Color.FromArgb(48,110,158);apply.ForeColor=Color.White;apply.FlatStyle=FlatStyle.Flat;Controls.Add(apply);
        restore.Text="一键恢复原状";restore.SetBounds(866,682,150,35);restore.Anchor=AnchorStyles.Bottom|AnchorStyles.Right;Controls.Add(restore);
        apply.Click+=async delegate {await RunOperation(false);};restore.Click+=async delegate {await RunOperation(true);};
        presets.SelectedIndexChanged+=delegate {if(!syncing&&presets.SelectedItem is Theme)SelectTheme((Theme)presets.SelectedItem);};mode.SelectedIndexChanged+=delegate {RefreshColors();};
        LoadSaved();foreach(var t in themes)presets.Items.Add(t);presets.SelectedIndex=0;
        try{string saved=Path.Combine(Engine.Store,"background.txt");if(File.Exists(saved)){string path=File.ReadAllText(saved,Encoding.UTF8).Trim();string dir=Path.GetFullPath(Path.Combine(Engine.Store,"backgrounds"))+Path.DirectorySeparatorChar;if(Path.GetFullPath(path).StartsWith(dir,StringComparison.OrdinalIgnoreCase)&&Regex.IsMatch(Path.GetFileName(path),"^[a-f0-9]{64}\\.png$"))SetBackground(path);}}catch{}
        FormClosed+=delegate{if(preview.ChatBackground!=null){preview.ChatBackground.Dispose();preview.ChatBackground=null;}};
        FormClosing+=delegate(object sender,FormClosingEventArgs e){if(backgroundBusy){e.Cancel=true;status.Text="背景操作正在等待微信刷新，请完成刷新或等候超时撤回后关闭。";}};
        Shown+=delegate {try{var t=Engine.Current();presets.Items.Insert(0,t);presets.SelectedIndex=0;status.Text="当前设置已载入；应用方式见各页面说明。";}catch(Exception e){AppLog.Error("UI",e);status.Text=e.Message;apply.Enabled=false;restore.Enabled=false;}};OrganizeWorkspace(auto,whole);
    }
    Button BackgroundButton(Panel panel,string text,int x,int width){var b=new Button{Text=text,FlatStyle=FlatStyle.Flat,Anchor=AnchorStyles.Bottom|AnchorStyles.Left};b.SetBounds(x,506,width,28);b.FlatAppearance.BorderColor=Color.FromArgb(204,213,222);panel.Controls.Add(b);backgroundButtons.Add(b);return b;}
    void SetBackground(string path){visualRemoveWallpaper=false;using(var image=Image.FromFile(path)){var next=BackgroundEffects.Transform(image,BackgroundEffects.Load());var old=preview.ChatBackground;preview.ChatBackground=next;backgroundSource=path;if(old!=null)old.Dispose();preview.Invalidate();RefreshVisualCanvas();}}
    void ChooseBackground(){try{using(var dialog=new OpenFileDialog{Filter="图片 (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",Title="选择聊天背景图片"})if(dialog.ShowDialog(this)==DialogResult.OK){SetBackground(NativeBackground.CacheImage(dialog.FileName));status.Text="图片已选择。自动背景开启后会跟随窗口尺寸重新适配；较小原图放大后细节有限。";}}catch(Exception e){AppLog.Error("UI",e);status.Text=e.Message;}}
    async Task ApplyBackground(bool remove,bool launch){try{if(!remove&&backgroundSource==null)throw new InvalidOperationException("请先选择背景图片。");backgroundBusy=true;apply.Enabled=restore.Enabled=false;foreach(var b in backgroundButtons)b.Enabled=false;
        if(launch){Process.Start(new ProcessStartInfo(Engine.Exe){UseShellExecute=true});status.Text="正在打开微信；如出现登录页，请先完成登录。";for(int n=0;n<30;n++){if(Process.GetProcessesByName("Weixin").Any(p=>p.MainWindowHandle!=IntPtr.Zero))break;await Task.Delay(500);}await Task.Delay(1500);}
        status.Text="正在应用原生背景并自动刷新微信…";
        string result=await Task.Run(()=>NativeBackground.Apply(remove?null:backgroundSource));status.Text=result;
        if(remove){var old=preview.ChatBackground;preview.ChatBackground=null;backgroundSource=null;if(old!=null)old.Dispose();preview.Invalidate();}
      }catch(Exception e){AppLog.Error("UI",e);status.Text=e.Message;}finally{backgroundBusy=false;apply.Enabled=restore.Enabled=true;foreach(var b in backgroundButtons)b.Enabled=true;}}
    void AddLabel(string text,int x,int y,int w,int h,Color color){Controls.Add(new Label{Text=text,Location=new Point(x,y),Size=new Size(w,h),ForeColor=color});}
    Button Btn(string text,int x,int y,int w){var b=new Button{Text=text,FlatStyle=FlatStyle.Flat};b.FlatAppearance.BorderColor=Color.FromArgb(204,213,222);b.SetBounds(x,y,w,32);Controls.Add(b);return b;}
    public void RenderArtwork(string path){StartPosition=FormStartPosition.Manual;Location=new Point(-30000,-30000);ShowInTaskbar=false;Show();Application.DoEvents();using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(0,0,Width,Height));bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);}Close();}
    void SelectTheme(Theme t){current=t.Copy();themeName.Text=t.Name;RefreshColors();}
    void RefreshColors(){if(current==null)return;syncing=true;for(int i=0;i<8;i++){hex[i].Text=current.Colors[i*2+mode.SelectedIndex];hex[i].BackColor=Color.White;swatches[i].BackColor=ColorTranslator.FromHtml(hex[i].Text);}syncing=false;UpdatePreview();}
    void Edit(int index){if(syncing||current==null)return;bool valid=Palette.IsColor(hex[index].Text);hex[index].BackColor=valid?Color.White:Color.MistyRose;if(valid){current.Colors[index*2+mode.SelectedIndex]=hex[index].Text;swatches[index].BackColor=ColorTranslator.FromHtml(hex[index].Text);UpdatePreview();}}
    void Pick(int i){using(var d=new StudioColorPicker{FullOpen=true,Color=ColorTranslator.FromHtml(current.Colors[i*2+mode.SelectedIndex])})if(d.ShowDialog(this)==DialogResult.OK)hex[i].Text=String.Format("#{0:X2}{1:X2}{2:X2}",d.Color.R,d.Color.G,d.Color.B);}
    static double Lum(Color c){Func<double,double> f=v=>v<=.04045?v/12.92:Math.Pow((v+.055)/1.055,2.4);return .2126*f(c.R/255d)+.7152*f(c.G/255d)+.0722*f(c.B/255d);}
    static double Ratio(string a,Color b){double x=Lum(ColorTranslator.FromHtml(a)),y=Lum(b);return (Math.Max(x,y)+.05)/(Math.Min(x,y)+.05);}
    void UpdatePreview(){RefreshVisualCanvas();preview.Theme=current;preview.Mode=mode.SelectedIndex;preview.Invalidate();var ink=mode.SelectedIndex==0?Color.FromArgb(35,43,52):Color.FromArgb(231,236,242);var issues=new List<string>();if(Ratio(current.Colors[4+mode.SelectedIndex],ink)<4.5)issues.Add("聊天背景");if(Ratio(current.Colors[12+mode.SelectedIndex],ink)<4.5)issues.Add("发送气泡");if(Ratio(current.Colors[10+mode.SelectedIndex],ink)<4.5)issues.Add("选中会话");contrast.Text=issues.Count>0?"可读性提示："+String.Join("、",issues)+"的文字对比度偏低。":"模拟预览 · 实际分区和文字颜色以微信为准";}
    Theme Edited(){if(hex.Any(b=>!Palette.IsColor(b.Text)))throw new InvalidDataException("请先修正红色标记的颜色值。");var t=current.Copy();t.Name=themeName.Text.Trim();Palette.Validate(t);return t;}
    string ThemesDir{get{return Path.Combine(Engine.Store,"themes");}}
    void LoadSaved(){Directory.CreateDirectory(ThemesDir);foreach(var p in Directory.GetFiles(ThemesDir,"*.json").Take(100)){try{themes.Add(Engine.Load(p));}catch{}}}
    void SaveTheme(){try{var t=Edited();string p=Path.Combine(ThemesDir,Engine.Hash(Encoding.UTF8.GetBytes(t.Name)).Substring(0,20)+".json");Engine.Save(p,t);for(int i=presets.Items.Count-1;i>=0;i--)if(((Theme)presets.Items[i]).Name==t.Name)presets.Items.RemoveAt(i);presets.Items.Add(t);presets.SelectedItem=t;status.Text="主题已保存在本机，下次打开仍可选择。";}catch(Exception e){AppLog.Error("UI",e);status.Text=e.Message;}}
    void ExportTheme(){try{var t=Edited();using(var d=new SaveFileDialog{Filter="主题文件 (*.json)|*.json",FileName="微信主题.json"})if(d.ShowDialog(this)==DialogResult.OK){Engine.Save(d.FileName,t);status.Text="主题已导出。";}}catch(Exception e){AppLog.Error("UI",e);status.Text=e.Message;}}
    void ImportTheme(){try{using(var d=new OpenFileDialog{Filter="主题文件 (*.json)|*.json"})if(d.ShowDialog(this)==DialogResult.OK){var t=Engine.Load(d.FileName);presets.Items.Add(t);presets.SelectedItem=t;status.Text="主题已导入；点击保存主题可留在本机。";}}catch(Exception e){AppLog.Error("UI",e);status.Text=e.Message;}}
    async Task RunOperation(bool isRestore){
        try{if(!isRestore){if(!NativePreviewBridge.Connected&&NativeBackground.InternalModulePresent()){NativePreviewBridge.Connect();nativeTrialStarted=true;RefreshNativeSteps();}if(!NativePreviewBridge.Connected)Engine.RequireStopped();PrepareSmartBackup(false);PersistVisualDraft();}var theme=isRestore?null:Edited();if(!isRestore&&NativePreviewBridge.Connected){ValidateSmartDraft(theme);Engine.SaveRuntimeTheme(theme);CommitSmartDraft(false);status.Text=await Task.Run(()=>NativeBackground.Apply(DesiredBackground()));return;}Engine.RequireStopped();if(!isRestore)ValidateSmartDraft(theme);apply.Enabled=restore.Enabled=false;status.Text="等待 Windows 管理员授权…";
            var info=new ProcessStartInfo {FileName=Application.ExecutablePath,Arguments=isRestore?"--restore":"--apply "+Palette.Pack(theme),UseShellExecute=true,Verb=(!isRestore||ThemeReset.NeedsElevation())?"runas":"",WorkingDirectory=AppDomain.CurrentDomain.BaseDirectory};
            using(var p=Process.Start(info)){await Task.Run(()=>p.WaitForExit());if(p.ExitCode!=0)throw new Exception("操作未完成；原文件校验或权限检查未通过。请查看弹出的说明。");}
            if(isRestore){MessageBox.Show(this,"已清空调色室的配置、壁纸和启动方案。可从原来的微信入口正常启动。","微信调色室",MessageBoxButtons.OK,MessageBoxIcon.Information);Close();return;}
            status.Text=isRestore?"已恢复微信原文件，可正常启动微信。":"主题已应用。请正常启动微信查看，切换主题同样需要退出后再应用。";CommitSmartDraft(isRestore);
        }catch(System.ComponentModel.Win32Exception e){status.Text=e.NativeErrorCode==1223?"已取消管理员授权，微信文件未修改。":e.Message;}
        catch(Exception e){AppLog.Error("UI",e);status.Text=e.Message;}finally{apply.Enabled=restore.Enabled=true;}
    }
}
internal static class Program {
    static void RepairAppearanceForProgram(){Engine.RepairLegacyAppearance();}
    [STAThread] static int Main(string[] args){
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        AppLog.Event("START","8.5-test "+String.Join(" ",args.Select(a=>a.StartsWith("--")?a:"[argument]")));
        try{
            if(args.Length==3&&args[0]=="--native-export"){Engine.StoreOverride=Path.GetFullPath(args[2]);NativeBackground.ExportNativeConfiguration(Path.GetFullPath(args[1]));return 0;}
            if(args.Length==1&&args[0]=="--native-launch")return NativeDeployment.Launch();
            if(args.Length==1&&args[0]=="--native-test-launch")return NativeDeployment.Launch(true);
            if(args.Length==1&&args[0]=="--native-test-stop"){NativeDeployment.StopPreparedTest();return 0;}
            if(args.Length==2&&args[0]=="--support-report"){SupportReport.Export(args[1]);return 0;}
            if(args.Length==1&&args[0]=="--diagnose-render"){RenderDiagnostic.Run();return 0;}
            if(args.Length==1&&args[0]=="--light-design"){VisualStyle.ApplyLight();return 0;}
            if(args.Length==1&&args[0]=="--visual-style"){VisualStyle.Apply();return 0;}
            if(args.Length==1&&args[0]=="--repair-appearance"){AppearanceProfiles.EnsureProfiles();RepairAppearanceForProgram();return 0;}
            if(args.Length==1&&args[0]=="--migrate-login"){
                if(!Engine.Admin){using(var helper=Process.Start(new ProcessStartInfo(Application.ExecutablePath,"--migrate-login"){UseShellExecute=true,Verb="runas"})){helper.WaitForExit();return helper.ExitCode;}}
                Engine.RequireStopped();var theme=Engine.Current();string dir=Path.Combine(Engine.Store,"migration-6.3");Directory.CreateDirectory(dir);Engine.Save(Path.Combine(dir,"theme-before.json"),theme);ThemeTransfer.Export(Path.Combine(dir,"settings-before.wxtheme"),theme,Engine.Store);string before=Engine.Hash(File.ReadAllBytes(Engine.Target));Engine.Apply(theme,false);File.WriteAllText(Path.Combine(dir,"result.txt"),"before="+before+"\nafter="+Engine.Hash(File.ReadAllBytes(Engine.Target))+"\nactiveTheme="+Engine.ActiveThemePath+"\n");return 0;
            }
            if(args.Length==1&&args[0]=="--self-test")return Engine.SelfTest();
            if(args.Length==1&&args[0]=="--monitor"){AutoBackground.RunMonitor();return 0;}
            if(args.Length==1&&args[0]=="--watch"){AutoBackground.Run();return 0;}
            if(args.Length==1&&args[0]=="--enable-auto"){AppearanceProfiles.EnsureProfiles();AutoBackground.SetEnabled(true);return 0;}
            if(args.Length==1&&args[0]=="--render"){using(var form=new Studio())form.RenderArtwork(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"studio-preview.png"));return 0;}
            if(args.Length==1&&args[0]=="--restore"){
                if(!Engine.Admin&&ThemeReset.NeedsElevation()){using(var helper=Process.Start(new ProcessStartInfo(Application.ExecutablePath,"--restore"){UseShellExecute=true,Verb="runas"})){helper.WaitForExit();return helper.ExitCode;}}
                ThemeReset.Run();return 0;
            }
            if(args.Length==2&&args[0]=="--apply"){Engine.Apply(Palette.Unpack(args[1]),false);return 0;}
            if(args.Length>0)throw new ArgumentException("不支持的启动参数。");
            AppearanceProfiles.EnsureProfiles();AutoBackground.ResumeSession();AutoBackground.EnsureStarted();Application.Run(new Studio());return 0;
        }catch(Exception e){AppLog.Error("PROGRAM",e);if(args.Length==3&&args[0]=="--native-export"){try{File.WriteAllText(Path.Combine(Path.GetFullPath(args[1]),"export-error.txt"),e.Message);}catch{}return 1;}if(args.Contains("--self-test"))File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"self-test-error.txt"),e.ToString());else MessageBox.Show(e.Message,"微信调色室",MessageBoxButtons.OK,MessageBoxIcon.Information);return 1;}
    }
}








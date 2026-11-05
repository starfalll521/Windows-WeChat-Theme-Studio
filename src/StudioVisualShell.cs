using System;using System.Drawing;using System.IO;using System.Linq;using System.Threading.Tasks;using System.Windows.Forms;
internal sealed partial class Studio{
 Panel advancedHost,visualRoot,visualProperties;ThemeDesignCanvas visualCanvas;Label visualStatus;string visualModule="wallpaper",visualImageKey;bool visualApplying,visualRemoveWallpaper;readonly System.Collections.Generic.Dictionary<int,string> visualLinks=new System.Collections.Generic.Dictionary<int,string>();readonly System.Collections.Generic.Dictionary<int,EffectSettings> visualEffects=new System.Collections.Generic.Dictionary<int,EffectSettings>();
 Panel visualInspector;
 readonly System.Collections.Generic.Dictionary<string,Button> visualSections=
     new System.Collections.Generic.Dictionary<string,Button>();

 void EnableVisualShell(){
  Text="微信调色室";
  var old=Controls.Cast<Control>().ToArray();
  advancedHost=new Panel{Dock=DockStyle.Fill,Size=ClientSize,Visible=false};
  foreach(var control in old)advancedHost.Controls.Add(control);
  Controls.Add(advancedHost);

  var paper=Color.FromArgb(238,243,241);
  var surface=Color.FromArgb(249,251,250);
  visualRoot=new Panel{Dock=DockStyle.Fill,BackColor=paper,Padding=new Padding(22,0,22,0)};
  var top=new Panel{Dock=DockStyle.Top,Height=88,BackColor=paper};
  top.Controls.Add(StudioUi.Label("微信调色室",new Rectangle(0,15,260,35),21,true));
  var subtitle=StudioUi.Label("壁纸、配色与聊天外观",new Rectangle(2,55,260,23),9);
  subtitle.ForeColor=StudioUi.Muted;
  top.Controls.Add(subtitle);

  var actions=new FlowLayoutPanel{Dock=DockStyle.Right,Width=484,
      FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(0,24,0,0),WrapContents=false};
  var applyVisual=StudioUi.Button("应用到微信",new Rectangle(0,0,116,38),true);
  applyVisual.Click+=async delegate{await ApplyVisualDraft();};
  actions.Controls.Add(applyVisual);
  var menuButton=StudioUi.Button("管理 ▾",new Rectangle(0,0,88,38));
  var manage=new ContextMenuStrip();
  manage.Items.Add("预览与部署",null,delegate{OpenVisualAdvanced("deployment");});
  manage.Items.Add("启动、迁移与恢复",null,delegate{OpenVisualAdvanced("recovery");});
  manage.Items.Add("全部外观设置",null,delegate{OpenVisualAdvanced("palette");});
  menuButton.Click+=delegate{manage.Show(menuButton,new Point(0,menuButton.Height));};
  FormClosed+=delegate{manage.Dispose();};
  actions.Controls.Add(menuButton);
  var appearance=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat,
      Width=136,Margin=new Padding(6,8,10,0),BackColor=surface,ForeColor=StudioUi.Ink,
      AccessibleName="预览外观"};
  appearance.Items.AddRange(new object[]{"浅色外观","深色外观"});
  appearance.SelectedIndex=editingAppearance;
  appearance.SelectedIndexChanged+=delegate{
      if(visualApplying)return;
      ChangeEditingAppearance(appearance.SelectedIndex);
      RefreshVisualCanvas();
      ShowVisualModule(visualModule);
  };
  actions.Controls.Add(appearance);
  top.Controls.Add(actions);

  var bottom=new Panel{Dock=DockStyle.Bottom,Height=38,BackColor=paper};
  visualStatus=StudioUi.Label("就绪",Rectangle.Empty,9);
  visualStatus.Dock=DockStyle.Fill;
  visualStatus.AutoEllipsis=true;
  visualStatus.TextAlign=ContentAlignment.MiddleLeft;
  visualStatus.ForeColor=StudioUi.Muted;
  bottom.Controls.Add(visualStatus);
  status.TextChanged+=delegate{visualStatus.Text=status.Text;};

  var center=new Panel{Dock=DockStyle.Fill};
  visualInspector=new Panel{Dock=DockStyle.Right,Width=320,BackColor=surface};
  var sections=new Panel{Dock=DockStyle.Top,Height=140,Padding=new Padding(16,10,16,8)};
  var sectionTitle=StudioUi.Label("编辑外观",new Rectangle(16,11,240,23),10,true);
  sections.Controls.Add(sectionTitle);
  AddVisualSection(sections,"wallpaper","壁纸",16,39);
  AddVisualSection(sections,"list","会话列表",164,39);
  AddVisualSection(sections,"send","发送气泡",16,72);
  AddVisualSection(sections,"receive","接收气泡",164,72);
  AddVisualSection(sections,"icons","界面图标",16,105);
  AddVisualSection(sections,"input","输入区域",164,105);
  visualProperties=new Panel{Dock=DockStyle.Fill,BackColor=surface,AutoScroll=true};
  visualInspector.Controls.Add(visualProperties);
  visualInspector.Controls.Add(sections);

  var previewFrame=new Panel{Dock=DockStyle.Fill,BackColor=surface,Padding=new Padding(9,0,9,9)};
  var previewHeader=new Panel{Dock=DockStyle.Top,Height=42};
  previewHeader.Controls.Add(StudioUi.Label("聊天预览",new Rectangle(8,11,120,24),10,true));
  var hint=StudioUi.Label("点击预览中的区域可直接调整",new Rectangle(104,12,245,23),9);
  hint.ForeColor=StudioUi.Muted;
  previewHeader.Controls.Add(hint);
  visualCanvas=new ThemeDesignCanvas{Dock=DockStyle.Fill,AccessibleName="聊天主题预览"};
  visualCanvas.ModuleClicked+=ShowVisualModule;
  previewFrame.Controls.Add(visualCanvas);
  previewFrame.Controls.Add(previewHeader);
  center.Controls.Add(previewFrame);
  center.Controls.Add(new Panel{Dock=DockStyle.Right,Width=14,BackColor=paper});
  center.Controls.Add(visualInspector);
  visualRoot.Controls.Add(center);
  visualRoot.Controls.Add(bottom);
  visualRoot.Controls.Add(top);
  Controls.Add(visualRoot);
  visualRoot.BringToFront();
  RefreshVisualCanvas();
  ShowVisualModule("wallpaper");
 }
 void AddVisualSection(Panel parent,string key,string title,int x,int y){
  var button=StudioUi.Button(title,new Rectangle(x,y,138,29));
  button.FlatAppearance.BorderSize=0;
  button.Click+=delegate{ShowVisualModule(key);};
  visualSections[key]=button;
  parent.Controls.Add(button);
 }
 string VisualInk(string key){EnsureWorkspaceForm("messages");return workspaceInk.Draft(key);}
 void RefreshVisualCanvas(){if(visualCanvas==null||current==null)return;try{EnsureWorkspaceForm("messages");EnsureWorkspaceForm("icons");EnsureWorkspaceForm("input");visualCanvas.Theme=Edited();visualCanvas.Mode=editingAppearance;visualCanvas.Ink=VisualInk("text");visualCanvas.ChatInk=VisualInk("chat");string link;visualCanvas.LinkInk=visualLinks.TryGetValue(editingAppearance,out link)?link:NativeBackground.InkLink;visualCanvas.Secondary=VisualInk("secondary");visualCanvas.Icons=workspaceIcons.DraftColor(false);visualCanvas.Accent=workspaceIcons.DraftColor(true);var inputBorder=workspaceInput.Draft();visualCanvas.Border=inputBorder.BorderColor;visualCanvas.InputOpacity=0;visualCanvas.InputBorder=inputBorder.Enabled;string source=DesiredBackground();EffectSettings draftEffects;var effects=visualEffects.TryGetValue(editingAppearance,out draftEffects)?draftEffects:workspaceEffects==null?BackgroundEffects.Load():workspaceEffects.Draft();visualCanvas.Effects=effects;visualCanvas.InputOpacity=0;string key=(source??"")+"|"+effects.Key;if(key!=visualImageKey){Bitmap next=null;if(source!=null&&File.Exists(source))using(var image=Image.FromFile(source))next=BackgroundEffects.Transform(image,effects);var old=visualCanvas.Wallpaper;visualCanvas.Wallpaper=next;visualImageKey=key;if(old!=null)old.Dispose();}visualCanvas.Invalidate();}catch(Exception e){AppLog.Error("VISUAL-REFRESH",e);if(visualStatus!=null)visualStatus.Text=e.Message;}}
 void OpenVisualAdvanced(string key){if(visualApplying||workspaceBusy)return;using(var dialog=new Form{Text="主题详细设置",ClientSize=new Size(1260,800),MinimumSize=new Size(1036,739),StartPosition=FormStartPosition.CenterParent,Font=Font}){advancedHost.Visible=true;advancedHost.Parent=dialog;advancedHost.Dock=DockStyle.Fill;SelectWorkspace(key);EffectSettings draft;if(key=="wallpaper"&&workspaceEffects!=null&&visualEffects.TryGetValue(editingAppearance,out draft)){workspaceEffects.SetDraft(draft);visualEffects.Remove(editingAppearance);}dialog.FormClosing+=delegate(object sender,FormClosingEventArgs e){if(workspaceBusy||workspaceForms.Any(WorkspaceFormBusy))e.Cancel=true;};try{dialog.ShowDialog(this);}finally{dialog.Controls.Remove(advancedHost);Controls.Add(advancedHost);advancedHost.Visible=false;visualRoot.BringToFront();}}RefreshVisualCanvas();ShowVisualModule(visualModule);}
 void VisualColorRow(string label,string value,int y,Action<string> changed){var title=StudioUi.Label(label,new Rectangle(18,y,260,24),9.5f,true);visualProperties.Controls.Add(title);var b=StudioUi.Button(Palette.IsColor(value)?value:"微信原色",new Rectangle(18,y+28,262,36));if(Palette.IsColor(value)){b.BackColor=ColorTranslator.FromHtml(value);b.ForeColor=SmartPalette.Luminance(b.BackColor)>.45?StudioUi.Ink:Color.White;}b.Click+=delegate{using(var d=new StudioColorPicker{Color=Palette.IsColor(value)?ColorTranslator.FromHtml(value):Color.White})if(d.ShowDialog(this)==DialogResult.OK){changed(SmartPalette.Hex(d.Color));RefreshVisualCanvas();ShowVisualModule(visualModule);status.Text="设计稿已更新，尚未应用到微信。";}};visualProperties.Controls.Add(b);}
 void VisualPaletteRow(string label,int slot,int y){VisualColorRow(label,current.Colors[slot*2+editingAppearance],y,value=>{current.Colors[slot*2+editingAppearance]=value;RefreshColors();});}
 void VisualInkRow(string label,string role,int y){VisualColorRow(label,VisualInk(role),y,value=>workspaceInk.SetDraft(role,value));}
 void ShowVisualModule(string key){if(visualProperties==null)return;visualModule=key;string section=key.StartsWith("receive")?"receive":key.StartsWith("send")||key=="chat"?"send":key=="selection"||key=="secondary"||key=="title"?"list":key;foreach(var entry in visualSections){bool active=entry.Key==section;entry.Value.BackColor=active?Color.FromArgb(221,238,231):visualInspector.BackColor;entry.Value.ForeColor=active?StudioUi.Accent:StudioUi.Ink;}visualCanvas.Selected=key;visualCanvas.Invalidate();foreach(Control old in visualProperties.Controls.Cast<Control>().ToArray())old.Dispose();visualProperties.Controls.Clear();string title=key=="send"||key=="send-text"?"自己发送的消息":key=="receive"||key=="receive-text"?"别人发送的消息":key=="chat"?"消息正文":key=="icons"?"图标与按钮":key=="input"?"输入区域":key=="title"?"聊天标题":key=="secondary"?"时间与辅助文字":key=="selection"?"选中的会话":key=="list"?"会话列表":"壁纸与背景";visualProperties.Controls.Add(StudioUi.Label(title,new Rectangle(18,13,264,36),16,true));string advancedKey="palette";int y=66;
  if(key=="send"||key=="send-text"||key=="receive"||key=="receive-text"||key=="chat"){bool own=key.StartsWith("send");VisualPaletteRow("气泡底色",own?6:7,y);VisualInkRow("正文颜色 · 双方共用","chat",y+88);string link;VisualColorRow("链接颜色 · 双方共用",visualLinks.TryGetValue(editingAppearance,out link)?link:NativeBackground.InkLink,y+176,v=>visualLinks[editingAppearance]=v);visualProperties.Controls.Add(StudioUi.Label("双方气泡底色独立；当前正文与链接色仍共用。",new Rectangle(18,y+264,264,50),9));advancedKey="messages";y+=330;}
  else if(key=="list"||key=="selection"){VisualPaletteRow("会话列表底色",1,y);VisualPaletteRow("选中行底色",5,y+88);VisualInkRow("会话名称","text",y+176);VisualInkRow("摘要与时间","secondary",y+264);y+=360;}
  else if(key=="title"){VisualInkRow("标题文字","text",y);VisualPaletteRow("窗口基础底色",0,y+88);advancedKey="messages";y+=190;}
  else if(key=="secondary"){VisualInkRow("次级文字颜色","secondary",y);VisualInkRow("壁纸上的说明文字","canvas",y+88);advancedKey="messages";y+=190;}
  else if(key=="icons"){EnsureWorkspaceForm("icons");VisualColorRow("普通图标",workspaceIcons.DraftColor(false),y,v=>workspaceIcons.SetDraftColor(false,v));VisualColorRow("强调图标",workspaceIcons.DraftColor(true),y+88,v=>workspaceIcons.SetDraftColor(true,v));advancedKey="icons";y+=190;}
  else if(key=="input"){EnsureWorkspaceForm("input");VisualColorRow("输入区边框",workspaceInput.Draft().BorderColor,y,v=>workspaceInput.SetDraftBorder(v));visualProperties.Controls.Add(StudioUi.Label("输入区边框在此设置；底色、透明度和背景模糊在壁纸详细设置中调整。",new Rectangle(18,y+90,264,60),10));advancedKey="input";y+=170;}
  else{var choose=StudioUi.Button("选择壁纸…",new Rectangle(18,y,262,38),true);choose.Click+=delegate{ChooseBackground();RefreshVisualCanvas();ShowVisualModule("wallpaper");};visualProperties.Controls.Add(choose);var clear=StudioUi.Button("移除壁纸",new Rectangle(18,y+50,262,36));clear.Click+=delegate{visualRemoveWallpaper=true;backgroundSource=null;visualImageKey=null;if(visualCanvas.Wallpaper!=null){visualCanvas.Wallpaper.Dispose();visualCanvas.Wallpaper=null;}visualCanvas.Invalidate();status.Text="壁纸已从设计稿移除。";ShowVisualModule("wallpaper");};clear.Enabled=DesiredBackground()!=null;visualProperties.Controls.Add(clear);var smart=StudioUi.Button("从壁纸生成配色…",new Rectangle(18,y+97,262,36));smart.Enabled=DesiredBackground()!=null;smart.Click+=delegate{ChooseSmartPalette();RefreshVisualCanvas();ShowVisualModule("send");};visualProperties.Controls.Add(smart);VisualPaletteRow("没有图片时的基础背景",2,y+151);advancedKey="wallpaper";y+=235;}
  var more=StudioUi.Button("详细设置…",new Rectangle(18,y+12,262,36));string page=advancedKey;more.Click+=delegate{OpenVisualAdvanced(page);};visualProperties.Controls.Add(more);}
 async Task ApplyVisualDraft(){if(visualApplying)return;visualApplying=true;visualRoot.Enabled=false;try{await RunOperation(false);RefreshVisualCanvas();ShowVisualModule(visualModule);visualStatus.Text=status.Text;}catch(Exception e){status.Text=e.Message;visualStatus.Text=e.Message;}finally{visualApplying=false;visualRoot.Enabled=true;}}
 string DesiredBackground(){return visualRemoveWallpaper?null:backgroundSource??AutoBackground.SavedImage();}
 void SaveVisualEditors(){
  if(visualCanvas==null)return;AppearanceProfiles.EnsureProfiles();var forms=new System.Collections.Generic.Dictionary<string,Form>(appearanceForms);if(workspaceInk!=null)forms["messages:"+editingAppearance]=workspaceInk;if(workspaceIcons!=null)forms["icons:"+editingAppearance]=workspaceIcons;if(workspaceInput!=null)forms["input:"+editingAppearance]=workspaceInput;if(workspaceEffects!=null)forms["wallpaper:"+editingAppearance]=workspaceEffects;
  for(int m=0;m<2;m++)using(AppearanceProfiles.Use(m)){Form form;if(forms.TryGetValue("messages:"+m,out form)&&!form.IsDisposed)((InkDialog)form).SaveDraftOnly();if(forms.TryGetValue("icons:"+m,out form)&&!form.IsDisposed)((IconDialog)form).SaveDraftOnly();if(forms.TryGetValue("input:"+m,out form)&&!form.IsDisposed)((InputDialog)form).SaveDraftOnly();EffectSettings effects;if(visualEffects.TryGetValue(m,out effects)){effects=effects.Copy();if(forms.TryGetValue("wallpaper:"+m,out form)&&!form.IsDisposed)((EffectsDialog)form).SetDraft(effects);BackgroundEffects.Save(effects);}else if(forms.TryGetValue("wallpaper:"+m,out form)&&!form.IsDisposed)((EffectsDialog)form).SaveDraftOnly();string link;if(visualLinks.TryGetValue(m,out link))NativeBackground.InkLink=link;}
  visualEffects.Clear();visualLinks.Clear();
 }
 string PersistVisualDraft(){StageVisualSmartRuntime();string source=DesiredBackground();if(source==null&&!visualRemoveWallpaper&&visualCanvas!=null&&visualCanvas.Wallpaper!=null)throw new IOException("设计稿显示了壁纸，但找不到原图；请重新选图后再保存。");SaveVisualEditors();NativePreviewBridge.RememberWallpaper(source);if(source!=null)backgroundSource=AutoBackground.SavedImage();return source;}
 void StageVisualSmartRuntime(){if(visualCanvas==null||pendingSmart==null||!pendingSmart.IncludeRuntime)return;EnsureWorkspaceForm("messages");EnsureWorkspaceForm("icons");EnsureWorkspaceForm("input");workspaceInk.SetDraft("text",pendingSmart.Text);workspaceInk.SetDraft("chat",pendingSmart.Text);workspaceInk.SetDraft("secondary",SmartPalette.SecondaryFor(pendingSmart));workspaceInk.SetDraft("canvas",SmartPalette.CanvasFor(pendingSmart));workspaceIcons.SetDraftColor(false,pendingSmart.Icons);workspaceIcons.SetDraftColor(true,pendingSmart.Accent);workspaceInput.SetDraftBorder(pendingSmart.Border,pendingSmart.RuntimeMode==0?(int?)55:null);visualLinks[editingAppearance]=SmartPalette.LinkFor(pendingSmart);visualEffects[editingAppearance]=pendingSmart.Effects.Copy();pendingSmart.IncludeRuntime=false;}
 internal void RenderVisualCanvas(string path){if(visualRoot==null)return;RefreshVisualCanvas();using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(0,0,Width,Height));image.Save(path);}}
}

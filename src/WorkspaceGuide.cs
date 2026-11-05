using System;using System.Drawing;using System.Windows.Forms;
internal sealed partial class Studio{
 Label workspaceGuide;
 string GuideSummary(string key){switch(key){
 case "wallpaper":return "先选背景与覆盖范围，再调亮度、模糊和局部衬底；这一步决定整个主题的基调。";
 case "palette":return "再定页面、列表、选中行与双方气泡底色；文字和图标颜色在后续页面单独调整。";
 case "messages":return "调整文字层级与链接可读性。当前正文与链接为双方共用，气泡底色已在上一页分开设置。";
 case "icons":return "调整导航、工具栏等原生图标的颜色、亮度和轮廓；不处理头像、表情或照片。";
 case "input":return "最后细调打字框的边界与底色，帮助定位输入区域；不会改变输入、选字和发送行为。";
 case "deployment":return "先连接微信并应用预览，满意后固定方案，再部署；保存方案不等于部署成功。";
 default:return "管理启动方式、路径、迁移和诊断；局部移除与完整恢复分开操作。";}}
 string GuideDetails(string key){switch(key){
 case "wallpaper":return "1. 图片与范围\r\n选择并应用图片：替换壁纸并立即请求预览。整窗背景：覆盖导航、会话列表、聊天及输入区域；关闭后仅用于聊天区域。移除壁纸：去掉图片，保留配色。\r\n\r\n2. 图片处理\r\n亮度：让背景更亮或更暗。模糊：减弱细节干扰，0 保留清晰原图。蒙版：把所选颜色混入背景；强度越高，颜色越明显。原图保留，头像、表情和聊天图片不参与处理。\r\n\r\n3. 局部可读性\r\n分区蒙版：分别调整会话列表、聊天、输入区、顶栏及导航的衬底强度。统一界面材质与层级：会同时调整两套外观的材质与文字层级，不只是修改图片。\r\n\r\n4. 智能配色\r\n从整张壁纸提取多种代表色并标出位置。先选择主色、副色、基底色或点缀色，再点击色块或原图分配颜色；每种用途可独立编辑。载入后到整体配色页应用和微调。选择图片按钮会立即应用；其他修改按页面按钮说明保存。";
 case "palette":return "按大面积到小面积调整：\r\n\r\n窗口底色：主窗口的基础背景。\r\n会话列表：左侧聊天列表的底色。\r\n聊天背景：消息区域的基础底色。\r\n辅助面板：弹出或附属面板的基础底色。\r\n左侧导航：最左侧功能栏底色。\r\n选中的会话：当前选中聊天的行底色。\r\n发送气泡：自己发出的消息底色。\r\n接收气泡：别人发来的消息底色。\r\n\r\n这些是基础底色；开启整窗壁纸后，图片及局部衬底可能覆盖部分基础底色。\r\n\r\n右上角选择编辑浅色或深色配置，两套独立保存。预设载入颜色草稿；主题名称用于保存与导出。按壁纸生成配色后仍可逐项修改。\r\n\r\n连接进程内预览时可在微信运行中应用；未连接时，基础配色要求先从托盘完全退出微信。";
 case "messages":return "主文字：会话名称、标题等主要文字。\r\n聊天正文：消息气泡中的正文；目前发送和接收共用。\r\n链接：聊天中的链接颜色；目前发送和接收共用，并非已经分开支持。\r\n次级文字：摘要、时间等辅助信息。\r\n壁纸上的说明文字：昵称、时间或浮动说明的文字颜色。\r\n\r\n黑色／白色／自选色：指定颜色。原色：交回微信原本颜色。智能选项仅在标明支持的范围内使用；未迁入进程内模块的能力不等于已经生效。\r\n\r\n先让正文与双方气泡都清楚，再调整次级文字。头像与表情保留原色。修改后应用本页；切换页面会保留尚未提交的草稿。";
 case "icons":return "模式先于数值：\r\n原色：沿用微信原图标。\r\n保留原色并增强：保持色相，调节亮度、饱和度与对比度。\r\n自选颜色：分别指定普通图标与强调图标颜色。\r\n\r\n普通图标：导航和工具栏中的一般图标。强调图标：选中或强调状态的图标。\r\n亮度：整体明暗。饱和度：颜色鲜艳程度。对比度：加强图标内部明暗差异。可见性增强：保留透明形状，提高可辨识度。轮廓加粗：沿原有路径增强线条，不把透明区域填成色块。\r\n\r\n这些设置作用于支持的原生图标，不处理联系人头像、表情、照片；并非所有第三方内容图标都会变色。";
 case "input":return "显示输入区边框：标出打字区域；关闭时恢复无边框主题效果。\r\n底色浓度：输入区衬底的可见程度，越高越能隔开壁纸细节。\r\n边缘对比：提高圆框边缘的辨识度。\r\n圆角大小：调整圆框转角弧度。\r\n边框颜色：指定边缘色。\r\n线宽：边缘粗细。\r\n\r\n此效果依赖已应用的壁纸与整窗背景。上方示意用于观察边框，实际微信还需检查多行输入与窗口缩放。修改后应用本页；不改变工具栏、输入法和发送行为。";
 case "deployment":return "配置到部署的顺序：\r\n\r\n1. 完全退出微信后准备并试启动。\r\n2. 正常登录，再连接预览。\r\n3. 按左侧从壁纸到输入区域逐页配置并应用；总应用会使用当前基础配色、已选壁纸及其他页面已保存的设置。\r\n4. 在微信中确认满意，固定实际预览方案。未应用的其他页面草稿不在固定方案内。\r\n5. 从托盘完全退出微信，结束试用。\r\n6. 部署已确认方案，使用创建的主题启动入口。\r\n\r\n预览修改与已部署方案隔离。部署后继续调整，需要重新应用、确认及部署。普通原版启动入口不会自动变成主题入口。";
 default:return "启动监听：外置模式使用轻量监听器检测微信启动，与进程内方案部署是两种运行方式，避免同时开启。\r\n路径检测：重新识别本机微信安装位置；找不到时手动选择。\r\n迁移：导出／导入保存的配色、设置及壁纸，不包含微信安装文件或聊天记录。\r\n诊断：导出技术报告，或打开本地日志目录排查问题。\r\n移除运行时效果：移除图片和绘制效果，保留基础配色。\r\n撤销上次界面风格：回退统一材质操作。\r\n一键恢复全部原状：先完全退出微信，再恢复原文件并关闭自动服务；不会删除聊天记录。";}}
 void BuildWorkspaceGuide(Panel header){
  header.Height=144;
  var help=new Button{Text="设置说明",FlatStyle=FlatStyle.Flat,Location=new Point(24,92),Size=new Size(138,34),ForeColor=workspaceText,BackColor=Color.FromArgb(240,243,247)};help.FlatAppearance.BorderSize=0;
  help.Click+=delegate{using(var dialog=new Form{Text=workspaceTitle.Text+" · 设置说明",StartPosition=FormStartPosition.CenterParent,Size=new Size(740,620),MinimumSize=new Size(540,420),Font=Font}){var text=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,BackColor=Color.FromArgb(247,249,251),ForeColor=workspaceText,Text=GuideDetails(workspacePage)};var panel=new Panel{Dock=DockStyle.Fill,Padding=new Padding(24)};panel.Controls.Add(text);dialog.Controls.Add(panel);dialog.ShowDialog(this);}};
  workspaceGuide=new Label{Location=new Point(178,87),Size=new Size(720,50),ForeColor=workspaceMuted,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right};header.Controls.Add(help);header.Controls.Add(workspaceGuide);
 }
}

using System;using System.IO;using System.Drawing;using System.Threading.Tasks;using System.Windows.Forms;
internal sealed partial class Studio{
 string nativeSnapshot;NativeDeploymentState nativePrepared;Label nativeDeploymentStatus;
 readonly Button[] nativeSteps=new Button[6];
 bool nativeTrialStarted,nativeApproved,nativeTrialFinished,nativeFlowBusy,nativeDeployed;
 void BuildNativeDeploymentPage(){
  var p=workspacePages["deployment"];p.Height=610;
  WorkspaceNote(p,"按编号从上到下操作。试用不安装启动入口；确认满意后才部署。",0,0,900,35);
  string[] titles={"1. 准备并试启动微信","2. 登录后连接预览","3. 应用当前配置到微信","4. 确认效果并保存方案","5. 完成试用（微信已退出）","6. 部署已确认方案"};
  string[] hints={"前提：关闭旧外置监听，从托盘完全退出微信。自动保存初始方案、准备模块并请求启动，不必再找其他按钮。","登录并进入微信主窗口后连接。","把当前工作台设计稿同步到微信预览。","实际微信效果满意后再核对保存。未应用到预览的基础配色不能进入部署方案。","先从微信托盘完全退出，再点击此步。清理试用控制，保留已经确认的固定方案。","自动准备固定方案并创建桌面主题入口。以后从该入口启动，调色室无需常驻。"};
  for(int i=0;i<6;i++){int step=i;var b=WorkspaceButton(p,titles[i],0,48+i*58,300);b.TabIndex=i;nativeSteps[i]=b;WorkspaceNote(p,hints[i],320,48+i*58,510,52);b.Click+=async delegate{await RunNativeStep(step);};}
  nativeDeploymentStatus=WorkspaceNote(p,"当前：先进行第 1 步。启动性能仍在验证中，卡顿时请立即停止试用。",0,402,770,62);
  var stop=WorkspaceButton(p,"停止试用（先退出微信）",0,478,246);stop.TabIndex=6;stop.Click+=delegate{if(nativeFlowBusy){nativeDeploymentStatus.Text="当前步骤仍在执行；请先从托盘退出微信，待步骤结束后再停止试用。";return;}try{Engine.RequireStopped();NativePreviewBridge.Disconnect();NativeDeployment.StopPreparedTest();nativeTrialStarted=nativeApproved=nativeTrialFinished=nativeDeployed=false;nativePrepared=null;nativeSnapshot=null;nativeDeploymentStatus.Text="试用已停止，请使用原微信入口。重试时从第 1 步开始。";}catch(Exception e){NativeStepFailure("停止试用",e);}RefreshNativeSteps();};
  var off=WorkspaceButton(p,"停用已部署方案",260,478,226);off.TabIndex=7;off.Click+=delegate{if(nativeFlowBusy)return;try{NativeDeployment.Disable();nativeDeployed=false;RefreshNativeSteps();nativeDeploymentStatus.Text="主题入口已移除，请使用原来的微信入口。";}catch(Exception e){NativeStepFailure("停用部署",e);}};
  var report=WorkspaceButton(p,"导出诊断报告…",500,478,246);report.TabIndex=8;report.Click+=async delegate{using(var d=new SaveFileDialog{Filter="诊断报告 (*.zip)|*.zip",FileName="微信调色室-测试诊断.zip"})if(d.ShowDialog(this)==DialogResult.OK){try{await Task.Run(()=>SupportReport.Export(d.FileName));nativeDeploymentStatus.Text="报告已导出；请说明失败的步骤编号和现象。";}catch(Exception e){NativeStepFailure("导出诊断",e);}}};
  WorkspaceNote(p,"仅适配微信 4.1.15.56 x64。只有专用主题入口加载模块；原 EXE 和原快捷方式使用原启动方式。灰色按钮表示前一步尚未完成。",0,526,752,64);
  FormClosed+=delegate{try{NativePreviewBridge.Disconnect();}catch(Exception e){AppLog.Error("NATIVE-PREVIEW",e);}};RefreshNativeSteps();
 }
 void RefreshNativeSteps(){if(nativeApproved&&!nativeTrialFinished&&NativePreviewBridge.LastSnapshot!=nativeSnapshot){nativeApproved=false;nativeSnapshot=null;}bool connected=NativePreviewBridge.Connected,previewReady=connected&&NativePreviewBridge.LastSnapshot!=null;bool[] ready={!nativeTrialStarted&&!nativeTrialFinished,nativeTrialStarted&&!nativeTrialFinished&&!connected,nativeTrialStarted&&!nativeTrialFinished&&connected,previewReady&&!nativeTrialFinished,nativeApproved&&!nativeTrialFinished,nativeApproved&&nativeTrialFinished&&!nativeDeployed};for(int i=0;i<6;i++)if(nativeSteps[i]!=null)nativeSteps[i].Enabled=!nativeFlowBusy&&ready[i];}
 void NativeStepFailure(string step,Exception e){AppLog.Error("NATIVE-FLOW "+step,e);nativeDeploymentStatus.Text=step+"失败："+e.Message+"\n没有自动进入下一步。卡顿时请退出微信后停止试用，并导出诊断报告。";}
 async Task RunNativeStep(int step){
  if(nativeFlowBusy)return;nativeFlowBusy=true;workspaceBusy=true;RefreshNativeSteps();AppLog.Event("NATIVE-FLOW","step="+(step+1)+" begin");
  try{
   if(step==0){
    Engine.RequireStopped();if(AutoBackground.Enabled)throw new IOException("请先在外置版的启动与恢复中停用监听，避免两套后台同时应用主题。");
    nativeApproved=nativeTrialFinished=false;nativeSnapshot=null;var theme=Edited();nativeDeploymentStatus.Text="第 1 步：准备初始方案和模块，还没有启动微信…";
    ValidateSmartDraft(theme);PrepareSmartBackup(false);string wallpaper=PersistVisualDraft();Engine.SaveRuntimeTheme(theme);CommitSmartDraft(false);if(AutoBackground.Enabled)AutoBackground.SetEnabled(false);string trial=NativeDeployment.Capture(theme,wallpaper,true);nativePrepared=await Task.Run(()=>NativeDeployment.Prepare(trial,Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"native-runtime")));
    await Task.Run(()=>NativeDeployment.StartPreparedTest(nativePrepared));nativeTrialStarted=true;AppLog.Event("NATIVE-DRAFT",NativeDeployment.SnapshotSummary(trial));
    nativeDeploymentStatus.Text="第 1 步：试启动请求已发出，尚未判定启动成功。"+NativeDeployment.SnapshotSummary(trial)+"。请正常登录；主窗口出现且不卡顿后再点第 2 步。";
   }else if(step==1){NativePreviewBridge.Connect();nativeDeploymentStatus.Text="第 2 步完成：已连接。到左侧配置并应用，或直接进行第 3 步。";
   }else if(step==2){
    if(!NativePreviewBridge.Connected)throw new IOException("连接已断开，请重新进行第 2 步。");PersistVisualDraft();var theme=Edited();ValidateSmartDraft(theme);Engine.SaveRuntimeTheme(theme);CommitSmartDraft(false);nativeApproved=false;nativeSnapshot=null;
    nativeDeploymentStatus.Text="第 3 步：发送预览配置…";string result=await Task.Run(()=>NativeBackground.Apply(DesiredBackground()));nativeDeploymentStatus.Text="第 3 步："+result+"\n请在微信中确认响应及时、操作流畅后再进入第 4 步。";
   }else if(step==3){
    nativeApproved=false;nativeSnapshot=null;string snapshot=NativePreviewBridge.LastSnapshot;if(!NativePreviewBridge.Connected||snapshot==null)throw new IOException("请先应用配置到微信预览。");NativeDeployment.VerifySnapshot(snapshot);
    if(Palette.Pack(Edited())!=Palette.Pack(Engine.Load(Path.Combine(snapshot,"theme-active.json"))))throw new IOException("基础配色还有未应用的修改，请回到第 3 步。");
    PersistVisualDraft();string currentDraft=NativeDeployment.Capture(Edited(),DesiredBackground(),true);if(!NativeDeployment.SameContent(snapshot,currentDraft))throw new IOException("壁纸、文字、图标或分区还有未应用的修改，请回到第 3 步；不会混入已确认方案。");
    if(!ReviewNativeSnapshot(snapshot)){nativeDeploymentStatus.Text="未确认保存。继续调整后重新应用和核对；不会进入部署。";return;}nativeSnapshot=snapshot;nativeApproved=true;nativeDeploymentStatus.Text="第 4 步完成：实际预览方案已固定。请从托盘退出微信，再进行第 5 步。";
   }else if(step==4){if(!nativeApproved||nativeSnapshot==null)throw new IOException("请先确认保存方案。");Engine.RequireStopped();NativePreviewBridge.Disconnect();nativeTrialFinished=true;nativeDeploymentStatus.Text="第 5 步完成：试用结束，固定方案仍保留。现在可选择第 6 步部署。";
   }else if(step==5){
    if(!nativeApproved||!nativeTrialFinished||nativeSnapshot==null)throw new IOException("请先完成方案确认和试用结束。");Engine.RequireStopped();nativeDeploymentStatus.Text="第 6 步：准备已确认方案…";
    var prepared=await Task.Run(()=>NativeDeployment.Prepare(nativeSnapshot,Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"native-runtime")));NativeDeployment.Activate(prepared,Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));nativePrepared=prepared;nativeDeployed=true;
    nativeDeploymentStatus.Text="第 6 步完成：桌面已创建「微信（主题模式）」。关闭调色室后从该入口测试；原入口保持原样。";
   }
   AppLog.Event("NATIVE-FLOW","step="+(step+1)+" complete");
  }catch(Exception e){NativeStepFailure("第 "+(step+1)+" 步",e);}finally{nativeFlowBusy=false;workspaceBusy=false;RefreshNativeSteps();}
 }
 bool ReviewNativeSnapshot(string directory){
  var theme=Engine.Load(Path.Combine(directory,"theme-active.json"));using(var form=new Form{Text="确认实际预览方案 · "+theme.Name,ClientSize=new Size(1040,520),StartPosition=FormStartPosition.CenterParent,Font=Font}){
   for(int m=0;m<2;m++){form.Controls.Add(new Label{Text=m==0?"浅色配置":"深色配置",Location=new Point(20+m*510,14),Size=new Size(480,28)});form.Controls.Add(new Preview{Theme=theme,Mode=m,Location=new Point(20+m*510,48),Size=new Size(490,360)});}
   form.Controls.Add(new Label{Text="此处是配置示意。仅在实际微信预览满意、操作流畅后确认；保存后不会再次智能取色。",Location=new Point(22,420),Size=new Size(990,38)});
   var back=new Button{Text="继续调整",DialogResult=DialogResult.Cancel,Location=new Point(680,470),Size=new Size(150,32)};var save=new Button{Text="确认保存此方案",DialogResult=DialogResult.OK,Location=new Point(850,470),Size=new Size(165,32)};form.Controls.Add(back);form.Controls.Add(save);form.CancelButton=back;return form.ShowDialog(this)==DialogResult.OK;
  }
 }
}

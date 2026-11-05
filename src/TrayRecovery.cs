using System;using System.Windows.Forms;using System.Runtime.InteropServices;
internal sealed class TrayRecovery:NativeWindow,IDisposable {
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern uint RegisterWindowMessage(string name);
 readonly NotifyIcon icon;readonly uint taskbarCreated;
 internal TrayRecovery(NotifyIcon value){icon=value;taskbarCreated=RegisterWindowMessage("TaskbarCreated");CreateHandle(new CreateParams());}
 protected override void WndProc(ref Message m){base.WndProc(ref m);if((uint)m.Msg==taskbarCreated){icon.Visible=false;icon.Visible=true;AppLog.Event("TRAY","Explorer restart: tray icon registered again");}}
 public void Dispose(){DestroyHandle();}
}
internal sealed partial class Studio {
 void WatchServiceLifetime(){var t=new System.Windows.Forms.Timer{Interval=1000};t.Tick+=delegate{if(AutoBackground.SessionExitRequested){if(!workspaceBusy&&!backgroundBusy&&!workspaceForms.Exists(WorkspaceFormBusy))Close();return;}if(AutoBackground.Enabled)try{AutoBackground.EnsureStarted();}catch(Exception e){AppLog.Error("SERVICE",e);}};t.Start();FormClosed+=delegate{t.Stop();t.Dispose();};}
}

param([ValidateSet('Launcher','Start','Stop','Restart')][string]$Action='Launcher')
. "$PSScriptRoot\common.ps1"
try {
    Test-TaskExecutable
    Add-Type -AssemblyName System.Windows.Forms
    function Start-TaskServer {
        $running = $false
        try { $r=Invoke-WebRequest -UseBasicParsing "$TaskUrl/Account/Login" -TimeoutSec 2; $running=$r.StatusCode -eq 200 } catch { }
        if (-not $running) {
            $taskProcess=Start-Process -FilePath $TaskExe -WorkingDirectory (Split-Path $TaskExe) -WindowStyle Hidden -PassThru
            $deadline=[DateTime]::UtcNow.AddSeconds(30)
            do { Start-Sleep -Milliseconds 400; if($taskProcess.HasExited){throw 'El servidor no inició. Revise los registros de SCSTData\Logs.'}; try { $r=Invoke-WebRequest -UseBasicParsing "$TaskUrl/Account/Login" -TimeoutSec 1; $running=$r.StatusCode -eq 200 } catch { } } while (-not $running -and [DateTime]::UtcNow -lt $deadline)
            if(-not $running){throw 'El servidor no respondió en el tiempo esperado.'}
        }
        Start-Process $TaskUrl
    }
    switch($Action){'Start'{Start-TaskServer;exit 0};'Stop'{Stop-TaskServer;exit 0};'Restart'{Stop-TaskServer;Start-TaskServer;exit 0}}
    $form=New-Object Windows.Forms.Form
    $form.Text='SCST · Lanzador local';$form.Width=430;$form.Height=215;$form.StartPosition='CenterScreen';$form.FormBorderStyle='FixedDialog';$form.MaximizeBox=$false
    $label=New-Object Windows.Forms.Label;$label.Text='Sistema de Control y Seguimiento de Trámites';$label.SetBounds(20,20,380,30);$form.Controls.Add($label)
    $start=New-Object Windows.Forms.Button;$start.Text='Abrir sistema';$start.SetBounds(20,65,115,35);$start.Add_Click({try{Start-TaskServer}catch{[Windows.Forms.MessageBox]::Show($_.Exception.Message,'SCST')}});$form.Controls.Add($start)
    $restart=New-Object Windows.Forms.Button;$restart.Text='Reiniciar';$restart.SetBounds(145,65,115,35);$restart.Add_Click({try{Stop-TaskServer;Start-TaskServer}catch{[Windows.Forms.MessageBox]::Show($_.Exception.Message,'SCST')}});$form.Controls.Add($restart)
    $stop=New-Object Windows.Forms.Button;$stop.Text='Detener sistema';$stop.SetBounds(270,65,125,35);$stop.Add_Click({try{Stop-TaskServer;[Windows.Forms.MessageBox]::Show('El servidor se detuvo.','SCST')}catch{[Windows.Forms.MessageBox]::Show($_.Exception.Message,'SCST')}});$form.Controls.Add($stop)
    $note=New-Object Windows.Forms.Label;$note.Text='Cerrar este lanzador no detiene el servidor.';$note.SetBounds(20,125,380,30);$form.Controls.Add($note)
    [void]$form.ShowDialog();exit 0
} catch { Write-Error "SCST: $($_.Exception.Message)";exit 1 }

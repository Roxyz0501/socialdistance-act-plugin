$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path "$PSScriptRoot\..\AetherRange\bin\Release\net48\SocialDistance.dll"))
$settings = New-Object SocialDistance.PluginSettings
$settings.Language = 'ja'
$type = $assembly.GetType('SocialDistance.SettingsControl', $true)
$ui = [Activator]::CreateInstance($type, [object[]]@($settings.PSObject.BaseObject))
$flags = [Reflection.BindingFlags]'NonPublic,Instance'
$tabs = $type.GetField('tabs',$flags).GetValue($ui)
$page = $type.GetField('languagePage',$flags).GetValue($ui)
$update = $type.GetField('updateTab',$flags).GetValue($ui)
try {
    $tabs.AddContentPage((New-Object Windows.Forms.TabPage))
    if ($tabs.TabPages[$tabs.TabCount - 1] -ne $type.GetField('supportTab',$flags).GetValue($ui)) { throw 'Support order' }
    foreach ($width in 320,760) {
        $ui.Size = New-Object Drawing.Size($width,480)
        foreach ($index in 0,1,2,3,3,2,1,0) {
            $page.Controls[0].Controls[0].SelectedIndex = $index
            $update.SetLanguage($ui.CurrentLanguage)
            $tabs.SelectedTab = $update
            $ui.ShowUpdateChecking()
            $ui.ShowUpdateResult($null)
            $ui.ShowUpdatePreparing()
            $ui.ShowUpdatePrepared($false,'test','')
            $ui.ShowUpdatePrepared($true,'','test')
            $bitmap = New-Object Drawing.Bitmap($width,480)
            try { $ui.DrawToBitmap($bitmap,$ui.ClientRectangle) } finally { $bitmap.Dispose() }
        }
    }
    $page.Controls[0].Controls[0].SelectedIndex = 1
    foreach ($code in 'en','ja','zh-CN','ko','ko','en') {
        $type.GetMethod('SetHostedLanguage',$flags).Invoke($ui,@($code))
        $ui.ApplyTo($settings)
        if ($ui.CurrentLanguage -ne $code -or $settings.Language -ne 'ja' -or $tabs.TabCount -ne 3) { throw 'Hosted language isolation' }
    }
} finally { $ui.Dispose() }
$standalone = [Activator]::CreateInstance($type, [object[]]@($settings.PSObject.BaseObject))
try { if ($standalone.CurrentLanguage -ne 'ja') { throw 'Standalone language restore' } } finally { $standalone.Dispose() }
Write-Host 'Tab integration passed.'

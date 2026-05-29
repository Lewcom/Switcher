$ErrorActionPreference = "Stop"

Write-Host "Switcher manual smoke test (interactive session required)"
Write-Host "1) Build app"
& "C:\Program Files\dotnet\dotnet.exe" build "C:\Users\Lewcom\Documents\Switcher\Switcher.sln" --no-restore | Out-Host

Write-Host "2) Start Switcher tray app"
$switcher = Start-Process -FilePath "C:\Users\Lewcom\Documents\Switcher\src\Switcher.App\bin\Debug\net8.0-windows\Switcher.App.exe" -PassThru
Start-Sleep -Seconds 2

Write-Host "3) Open Notepad"
$notepad = Start-Process -FilePath "notepad.exe" -PassThru
Start-Sleep -Seconds 1

Write-Host ""
Write-Host "Run the following manual checks now:"
Write-Host "- AC1: Type ghbdsn, press Ctrl+Alt+L, expect: привіт"
Write-Host "- AC2: Type ghbdsn 123!, select ghbdsn, press Ctrl+Alt+L, expect: привіт 123!"
Write-Host "- AC6: punctuation/digits stay unchanged"
Write-Host "- Empty context: press Ctrl+Alt+L on blank line, app should not crash"
Write-Host ""
Read-Host "Press Enter after you finish manual checks"

Write-Host "4) Cleanup"
if (!$notepad.HasExited) {
    $notepad.CloseMainWindow() | Out-Null
    Start-Sleep -Milliseconds 500
    if (!$notepad.HasExited) { Stop-Process -Id $notepad.Id -Force }
}
if (!$switcher.HasExited) {
    Stop-Process -Id $switcher.Id -Force
}

Write-Host "Done. Update verify/feature-hotkey-convert/verify.md with your pass/fail results."

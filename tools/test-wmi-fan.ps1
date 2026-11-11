param([switch]$Execute)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
$OutputEncoding = [Console]::OutputEncoding
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $Execute) {
    Write-Output 'Plan: Fan_Get_FullSpeed -> Fan_Set_FullSpeed(true) -> verify -> wait 5 seconds -> Fan_Set_FullSpeed(false) -> verify.'
    Write-Output 'Execute only after approval, from an administrator terminal with -Execute.'
    exit 0
}
# 仅在显式选择执行时做一次5秒WMI周期，控制器负责异常停止。
& (Join-Path $taskRoot 'build\LiteLX.Cli.exe') fan run 5 --execute --confirm-hardware 2>&1 |
    Tee-Object -FilePath (Join-Path $taskRoot 'analysis\evidence\fan-wmi-live-cycle.txt')
$cycleExit = $LASTEXITCODE
& (Join-Path $taskRoot 'build\LiteLX.Cli.exe') fan status --confirm-hardware-query 2>&1 |
    Tee-Object -FilePath (Join-Path $taskRoot 'analysis\evidence\fan-wmi-after-cycle.txt')
if ($cycleExit -ne 0) { exit $cycleExit }
exit $LASTEXITCODE

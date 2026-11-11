$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
$OutputEncoding = [Console]::OutputEncoding
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
# 仅验证用户已批准的 FullSpeed 逻辑查询，经自编适配器调用。
& (Join-Path $taskRoot 'build\LiteLX.Cli.exe') fan status --confirm-hardware-query 2>&1 |
    Tee-Object -FilePath (Join-Path $taskRoot 'analysis\evidence\fan-wmi-adapter-query.txt')
exit $LASTEXITCODE

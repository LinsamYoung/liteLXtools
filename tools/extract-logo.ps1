param([string]$InstallDirectory = 'C:\Program Files (x86)\Lenovo\PCManager\5.1.210.10222')
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$moduleRoot = Join-Path $taskRoot 'modules\LenovoLogo'
$yogaRoot = Join-Path $InstallDirectory 'Modules\Setings\yoga'
New-Item -ItemType Directory -Force -Path $moduleRoot | Out-Null
# 仅提取开机画面后台执行器使用的原生接口。
$files = @(
    @{ Source = 'AIToolAPI.dll'; Target = 'AIToolAPI.dll' }
)
$manifest = foreach ($entry in $files) {
    $source = Join-Path $yogaRoot $entry.Source
    $target = Join-Path $moduleRoot $entry.Target
    $sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
    New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($target)) | Out-Null
    if (Test-Path -LiteralPath $target) {
        if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $sourceHash) {
            throw ('目标存在不同文件，请先核对 ' + $target)
        }
    }
    else { Copy-Item -LiteralPath $source -Destination $target }
    if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $sourceHash) { throw '提取校验失败。' }
    [pscustomobject]@{ Source = $source; RelativePath = $entry.Target; SHA256 = $sourceHash.ToLowerInvariant(); Bytes = (Get-Item -LiteralPath $target).Length }
}
ConvertTo-Json -InputObject @($manifest) -Depth 4 | Set-Content -LiteralPath (Join-Path $moduleRoot 'manifest.json') -Encoding utf8
Write-Output ('原厂 Logo 子模块已提取并校验 ' + $moduleRoot)

param(
    [string]$SourceRoot = 'C:\Program Files (x86)\Lenovo\PCManager\5.1.210.10222'
)

$ErrorActionPreference = 'Stop'
$workspaceRoot = Split-Path -Parent $PSScriptRoot
$destinationRoot = Join-Path $workspaceRoot 'modules\LenovoBattery'
$expectedFiles = @(
    [pscustomobject]@{
        RelativePath = 'BatterySetting.exe'
        SHA256 = '05ea2a5407ee2ce919e71982861454535e736129ac0bbb2d9f7e8f01c99b1acd'
        Bytes = 2713936
    }
    [pscustomobject]@{
        RelativePath = 'WrapPlugin.dll'
        SHA256 = '806f7d5f76fda7921089bad2469976e7754df99ed6f2b0f0ccc26539f41e68ab'
        Bytes = 3211088
    }
    [pscustomobject]@{
        RelativePath = 'libmcfcamera.dll'
        SHA256 = 'ac004bf8700363731b0e4d960c0578486a25db67b61e5eab0b96a3a4d4d2d3bc'
        Bytes = 51744
    }
    [pscustomobject]@{
        RelativePath = 'libcurl.dll'
        SHA256 = 'e820c3fc10de2d2999b84a669a8c9c64d99cdb3159118cf701bb7d29cde8ac1f'
        Bytes = 708360
    }
    [pscustomobject]@{
        RelativePath = 'libmcfcore.dll'
        SHA256 = '0f2e42f68830701fda325e57b5535537b4fbca9ec8e2ea102e87887d8537e757'
        Bytes = 129568
    }
    [pscustomobject]@{
        RelativePath = 'VCRUNTIME140.dll'
        SHA256 = '2f7ac533571f27e358605a047eb9a226f52d9997d075cb83107e47b5ec8d4831'
        Bytes = 67584
    }
    [pscustomobject]@{
        RelativePath = 'MSVCP140.dll'
        SHA256 = '85aa8c8ab4cbf1ff9ae5c7bde1bf6da2e18a570e36e2d870b88536b8658c5ba8'
        Bytes = 446840
    }
)

# 先核对全部源文件及已有目标，避免覆盖不同版本的本地模块。
$manifest = @(
    foreach ($expected in $expectedFiles) {
        $sourcePath = (Get-Item -LiteralPath (Join-Path $SourceRoot $expected.RelativePath)).FullName
        $sourceInfo = Get-Item -LiteralPath $sourcePath
        $sourceHash = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($sourceInfo.Length -ne $expected.Bytes -or $sourceHash -ne $expected.SHA256) {
            throw "源文件与已分析版本不符 $sourcePath"
        }
        $targetPath = Join-Path $destinationRoot $expected.RelativePath
        if (Test-Path -LiteralPath $targetPath) {
            $targetHash = (Get-FileHash -LiteralPath $targetPath -Algorithm SHA256).Hash.ToLowerInvariant()
            if ($targetHash -ne $expected.SHA256) {
                throw "目标文件已存在且内容不同 $targetPath"
            }
        }
        [pscustomobject]@{
            Source = $sourcePath
            RelativePath = $expected.RelativePath
            SHA256 = $sourceHash
            Bytes = $sourceInfo.Length
        }
    }
)

New-Item -ItemType Directory -Path $destinationRoot -Force | Out-Null
foreach ($entry in $manifest) {
    $targetPath = Join-Path $destinationRoot $entry.RelativePath
    Copy-Item -LiteralPath $entry.Source -Destination $targetPath -Force
    $targetHash = (Get-FileHash -LiteralPath $targetPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($targetHash -ne $entry.SHA256) { throw "复制后校验失败 $targetPath" }
}

# 清单仅记录经过校验的七个文件，程序启动由单独入口处理。
$manifestPath = Join-Path $destinationRoot 'manifest.json'
$json = ConvertTo-Json -InputObject $manifest -Depth 4
[IO.File]::WriteAllText($manifestPath, $json, (New-Object Text.UTF8Encoding($false)))
Write-Host "已提取并校验 $($manifest.Count) 个文件到 $destinationRoot"

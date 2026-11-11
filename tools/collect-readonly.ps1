param(
    [string]$InstallDirectory = 'C:\Program Files (x86)\Lenovo\PCManager\5.1.210.10222',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\analysis\evidence')
)
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
# 仅枚举文件、服务、驱动和 CIM 类架构，所有 WMI 方法都不调用。
Get-ChildItem -LiteralPath $InstallDirectory -File -Recurse |
    Select-Object FullName, Length, LastWriteTimeUtc |
    ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'install-inventory.json') -Encoding utf8
Get-CimInstance Win32_Service |
    Where-Object { $_.PathName -match 'Lenovo|PCManager' -or $_.Name -match 'Lenovo|Energy|ImController' } |
    Select-Object Name, State, StartMode, PathName |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDirectory 'services.json') -Encoding utf8
Get-CimInstance Win32_SystemDriver |
    Where-Object { $_.Name -match 'ACPIVPC|LnCoolit|LnvDrvFdn|lnvscenter' } |
    Select-Object Name, State, StartMode, PathName |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDirectory 'drivers.json') -Encoding utf8
$classes = foreach ($name in @('LENOVO_GAMEZONE_DATA', 'LENOVO_FAN_METHOD', 'LENOVO_UTILITY_DATA')) {
    $class = Get-CimClass -Namespace root/wmi -ClassName $name
    [pscustomobject]@{
        Namespace = 'root/wmi'
        Class = $name
        Methods = @($class.CimClassMethods | ForEach-Object {
            [pscustomobject]@{
                Name = $_.Name
                Parameters = @($_.Parameters | ForEach-Object {
                    [pscustomobject]@{
                        Name = $_.Name
                        Type = $_.CimType.ToString()
                        Direction = @($_.Qualifiers | Where-Object Name -in 'in', 'out' | Select-Object -ExpandProperty Name)
                    }
                })
            }
        })
    }
}
$classes | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'wmi-schemas.json') -Encoding utf8
$config = Get-Content -Raw -Encoding utf8 -LiteralPath (Join-Path $InstallDirectory 'skin\dist\json\defaultToolList.json') | ConvertFrom-Json
$config | Where-Object { $_.id -in 'lenovologdiy', 'coolclean', 'rightclickmanagement', 'battery' -or $_.name -match '电池' } |
    ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'feature-entries.json') -Encoding utf8
Write-Output ('只读采集完成 ' + [IO.Path]::GetFullPath($OutputDirectory))

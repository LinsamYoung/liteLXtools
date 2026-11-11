param([switch]$Test)
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '需要 Windows .NET Framework 4.x 编译器。' }
$output = Join-Path $taskRoot 'build'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$sourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $taskRoot 'src\LiteLX') -Filter '*.cs' | Select-Object -ExpandProperty FullName)
$appIcon = Join-Path $taskRoot 'src\LiteLX\Assets\LiteLX.ico'
$options = @('/nologo', '/utf8output', '/optimize+', '/platform:x64', ('/win32manifest:' + (Join-Path $taskRoot 'src\LiteLX\app.manifest')), ('/win32icon:' + $appIcon), ('/resource:' + $appIcon + ',LiteLX.App.ico'), '/r:System.Core.dll', '/r:System.Management.dll', '/r:System.Drawing.dll', '/r:System.Web.Extensions.dll', '/r:System.Xml.dll')
$desktopWpf = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\WPF'
$options += @(
    ('/r:' + (Join-Path $desktopWpf 'PresentationFramework.dll')),
    ('/r:' + (Join-Path $desktopWpf 'PresentationCore.dll')),
    ('/r:' + (Join-Path $desktopWpf 'WindowsBase.dll')),
    '/r:System.Xaml.dll',
    ('/resource:' + (Join-Path $taskRoot 'src\LiteLX\ModernWindow.xaml') + ',LiteLX.ModernWindow.xaml'))
$guiOptions = @($options | Where-Object { -not $_.StartsWith('/win32manifest:') })
$guiOptions += '/win32manifest:' + (Join-Path $taskRoot 'src\LiteLX\gui.manifest')
& $compiler @guiOptions '/target:winexe' ('/out:' + (Join-Path $output 'LiteLX.exe')) @sourceFiles
if ($LASTEXITCODE -ne 0) { throw 'GUI 编译失败。' }
& $compiler @options '/target:exe' ('/out:' + (Join-Path $output 'LiteLX.Cli.exe')) @sourceFiles
if ($LASTEXITCODE -ne 0) { throw 'CLI 编译失败。' }
Copy-Item -LiteralPath (Join-Path $taskRoot 'src\LiteLX\app.config') -Destination (Join-Path $output 'LiteLX.exe.config') -Force
Copy-Item -LiteralPath (Join-Path $taskRoot 'src\LiteLX\app.config') -Destination (Join-Path $output 'LiteLX.Cli.exe.config') -Force
$logoRoot = Join-Path $taskRoot 'modules\LenovoLogo'
if (Test-Path -LiteralPath (Join-Path $logoRoot 'AIToolAPI.dll')) {
    $operationOptions = $options | Where-Object { $_ -ne '/platform:x64' }
    & $compiler @operationOptions '/platform:x86' '/target:winexe' '/main:LogoOperationHost' ('/out:' + (Join-Path $logoRoot 'LogoOperationHost.exe')) @sourceFiles (Join-Path $taskRoot 'src\LogoOperationHost.cs')
    if ($LASTEXITCODE -ne 0) { throw '开机画面后台执行器编译失败。' }
}
if ($Test) {
    & $compiler @options '/target:exe' '/main:HardwareSelfTest' ('/out:' + (Join-Path $output 'HardwareSelfTest.exe')) @sourceFiles (Join-Path $taskRoot 'tools\HardwareSelfTest.cs')
    if ($LASTEXITCODE -ne 0) { throw '模拟测试编译失败。' }
    & (Join-Path $output 'HardwareSelfTest.exe')
    if ($LASTEXITCODE -ne 0) { throw '模拟测试失败。' }
    & $compiler @options '/target:exe' '/main:LogoSelfTest' ('/out:' + (Join-Path $output 'LogoSelfTest.exe')) @sourceFiles (Join-Path $taskRoot 'tools\LogoSelfTest.cs')
    if ($LASTEXITCODE -ne 0) { throw '开机画面模拟测试编译失败。' }
    & (Join-Path $output 'LogoSelfTest.exe')
    if ($LASTEXITCODE -ne 0) { throw '开机画面模拟测试失败。' }
    & $compiler @options '/target:exe' '/main:MenuSelfTest' ('/out:' + (Join-Path $output 'MenuSelfTest.exe')) @sourceFiles (Join-Path $taskRoot 'tools\MenuSelfTest.cs')
    if ($LASTEXITCODE -ne 0) { throw '右键菜单测试编译失败。' }
    & (Join-Path $output 'MenuSelfTest.exe')
    if ($LASTEXITCODE -ne 0) { throw '右键菜单测试失败。' }
}
Write-Output ('构建完成 ' + $output)

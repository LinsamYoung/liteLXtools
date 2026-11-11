param([string]$Output = (Join-Path $PSScriptRoot '..\analysis\evidence\fan-wmi-elevated-getters.json'))
$ErrorActionPreference = 'Stop'
# 仅调用用户明确批准的三个逻辑查询接口，调用集合固定。
$requests = @(
    @{ Class = 'LENOVO_UTILITY_DATA'; Method = 'GetIfSupportOrVersion'; Arguments = @{ datatype = [uint32]0x90000 } },
    @{ Class = 'LENOVO_GAMEZONE_DATA'; Method = 'GetFanCoolingStatus'; Arguments = @{} },
    @{ Class = 'LENOVO_FAN_METHOD'; Method = 'Fan_Get_FullSpeed'; Arguments = @{} }
)
$results = foreach ($request in $requests) {
    try {
        $instances = @(Get-CimInstance -Namespace root/wmi -ClassName $request.Class -ErrorAction Stop)
        if ($instances.Count -ne 1) { throw ('Unexpected instance count ' + $instances.Count) }
        $response = Invoke-CimMethod -InputObject $instances[0] -MethodName $request.Method -Arguments $request.Arguments -ErrorAction Stop
        [pscustomobject]@{ Class = $request.Class; Method = $request.Method; Result = $response; Error = $null; CapturedUtc = [DateTime]::UtcNow.ToString('o') }
    }
    catch {
        [pscustomobject]@{ Class = $request.Class; Method = $request.Method; Error = $_.Exception.Message; CapturedUtc = [DateTime]::UtcNow.ToString('o') }
    }
}
$results | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Output -Encoding utf8

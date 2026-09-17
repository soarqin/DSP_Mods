#Requires -Version 7.0
param(
    [Parameter(Mandatory)]
    [string] $ServerUri,
    [switch] $RequireGame,
    [int] $TimeoutSec = 8
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$failed = 0
$script:seq = 0
$utf8 = [Text.UTF8Encoding]::new($false)

function New-Id {
    $script:seq++
    "t-$script:seq"
}

function Connect-Api {
    param([string] $Uri)
    $ws = [Net.WebSockets.ClientWebSocket]::new()
    $cts = [Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds($TimeoutSec))
    $ws.ConnectAsync([Uri]$Uri, $cts.Token).GetAwaiter().GetResult()
    return @{ Ws = $ws; Cts = $cts }
}

function Close-Api {
    param($Conn)
    if (-not $Conn) { return }
    try {
        if ($Conn.Ws.State -eq [Net.WebSockets.WebSocketState]::Open) {
            $Conn.Ws.CloseAsync([Net.WebSockets.WebSocketCloseStatus]::NormalClosure, '', $Conn.Cts.Token).GetAwaiter().GetResult()
        }
    } catch { }
    try { $Conn.Ws.Dispose() } catch { }
    try { $Conn.Cts.Dispose() } catch { }
}

function Send-Text {
    param($Conn, [string] $Text, [bool] $End = $true)
    $bytes = $utf8.GetBytes($Text)
    $seg = [ArraySegment[byte]]::new($bytes)
    $Conn.Ws.SendAsync($seg, [Net.WebSockets.WebSocketMessageType]::Text, $End, $Conn.Cts.Token).GetAwaiter().GetResult()
}

function Receive-Message {
    param($Conn)
    $buffer = [byte[]]::new(65536)
    $ms = [IO.MemoryStream]::new()
    do {
        $seg = [ArraySegment[byte]]::new($buffer)
        $result = $Conn.Ws.ReceiveAsync($seg, $Conn.Cts.Token).GetAwaiter().GetResult()
        if ($result.MessageType -eq [Net.WebSockets.WebSocketMessageType]::Close) {
            return @{ Closed = $true; CloseStatus = [int]$Conn.Ws.CloseStatus; Text = $null }
        }
        $ms.Write($buffer, 0, $result.Count)
    } while (-not $result.EndOfMessage)
    return @{ Closed = $false; Text = $utf8.GetString($ms.ToArray()) }
}

function Invoke-Rpc {
    param($Conn, [string] $Method, $Params = @{}, [string] $Id = $(New-Id))
    $body = [ordered]@{ jsonrpc = '2.0'; id = $Id; method = $Method; params = $Params }
    Send-Text $Conn ($body | ConvertTo-Json -Compress -Depth 16)
    $msg = Receive-Message $Conn
    if ($msg.Closed) { throw "Connection closed while waiting for $Method (close $($msg.CloseStatus))" }
    return ($msg.Text | ConvertFrom-Json)
}

function Assert-True {
    param([bool] $Cond, [string] $Name)
    if ($Cond) { Write-Host "ok  $Name" }
    else { Write-Host "fail $Name"; $script:failed++ }
}

function Assert-Kind {
    param($Resp, [string] $Kind, [string] $Name)
    $actual = $null
    if ($Resp.error -and $Resp.error.data) { $actual = [string]$Resp.error.data.kind }
    Assert-True ($actual -eq $Kind) "$Name (kind=$actual)"
}

$conn = $null
try {
    $conn = Connect-Api $ServerUri

    $ping = Invoke-Rpc $conn 'system.ping'
    Assert-True ($null -ne $ping.result.serverTimeUtc) 'system.ping'

    $info = Invoke-Rpc $conn 'system.info'
    Assert-True ($info.result.apiVersion -eq '1.0.0') 'system.info apiVersion'
    Assert-True ($info.result.limits.maxRequestBytes -eq 65536) 'system.info limits'
    Assert-True ($info.result.capabilities.'api.stats' -eq $false) 'api.stats false'
    Assert-True ($info.result.capabilities.subscriptions -eq $false) 'subscriptions false'
    Assert-True ($info.result.PSObject.Properties.Name -contains 'gameVersionReady') 'gameVersionReady present'

    $ok = Invoke-Rpc $conn 'system.validate' @{ apiMajor = 1; requiredCapabilities = @('reflection.read') }
    Assert-True ($ok.result.compatible -eq $true) 'validate compatible'

    $statsCap = Invoke-Rpc $conn 'system.validate' @{ apiMajor = 1; requiredCapabilities = @('api.stats') }
    Assert-True ($statsCap.result.compatible -eq $false -and $statsCap.result.missingCapabilities[0] -eq 'api.stats') 'validate missing api.stats'

    $major = Invoke-Rpc $conn 'system.validate' @{ apiMajor = 2 }
    Assert-True ($major.result.compatible -eq $false -and $major.result.apiMajorCompatible -eq $false) 'validate major mismatch'

    $stats = Invoke-Rpc $conn 'system.stats'
    Assert-True ($stats.result.available -eq $false -and $null -eq $stats.result.metrics) 'system.stats placeholder'

    Send-Text $conn '{not-json'
    $parse = (Receive-Message $conn).Text | ConvertFrom-Json
    Assert-Kind $parse 'PARSE_ERROR' 'malformed JSON'

    Send-Text $conn '{"jsonrpc":"2.0","method":"system.ping","params":{}}'
    $noId = (Receive-Message $conn).Text | ConvertFrom-Json
    Assert-Kind $noId 'INVALID_REQUEST' 'missing id'

    Send-Text $conn '{"jsonrpc":"2.0","id":1,"method":"system.ping","params":{}}'
    $numId = (Receive-Message $conn).Text | ConvertFrom-Json
    Assert-Kind $numId 'INVALID_REQUEST' 'numeric id'

    $unknown = Invoke-Rpc $conn 'system.nope'
    Assert-Kind $unknown 'METHOD_NOT_FOUND' 'unknown method'

    $extra = Invoke-Rpc $conn 'system.ping' @{ foo = 1 }
    Assert-Kind $extra 'INVALID_PARAMS' 'unknown ping param'

    Send-Text $conn '[{"jsonrpc":"2.0","id":"b","method":"system.ping"}]'
    $batch = (Receive-Message $conn).Text | ConvertFrom-Json
    Assert-Kind $batch 'INVALID_REQUEST' 'batch array'

    Send-Text $conn '{"jsonrpc":"2.0","id":"dup","method":"data.read","params":{"root":"history","path":["currentTech"]}}'
    Send-Text $conn '{"jsonrpc":"2.0","id":"dup","method":"data.read","params":{"root":"history","path":["currentTech"]}}'
    $dup = Receive-Message $conn
    if ($dup.Closed) {
        Assert-True ($dup.CloseStatus -eq 1008) "duplicate outstanding id close $($dup.CloseStatus)"
    } else {
        Assert-True $false 'duplicate outstanding id should close'
    }

    Close-Api $conn
    $conn = Connect-Api $ServerUri
    $ping2 = Invoke-Rpc $conn 'system.ping'
    Assert-True ($null -ne $ping2.result.serverTimeUtc) 'recover after duplicate-id close'

    $conn.Ws.SendAsync(
        [ArraySegment[byte]]::new([byte[]](1, 2, 3)),
        [Net.WebSockets.WebSocketMessageType]::Binary,
        $true,
        $conn.Cts.Token).GetAwaiter().GetResult()
    $bin = Receive-Message $conn
    Assert-True ($bin.Closed -and $bin.CloseStatus -eq 1003) "binary rejected $($bin.CloseStatus)"

    Close-Api $conn
    $conn = Connect-Api $ServerUri
    $big = 'x' * 70000
    Send-Text $conn $big
    $over = Receive-Message $conn
    Assert-True ($over.Closed -and $over.CloseStatus -eq 1009) "oversized close $($over.CloseStatus)"

    Close-Api $conn
    $uri = [Uri]$ServerUri
    $badPath = Connect-Api "$($uri.Scheme)://$($uri.Authority)/nope"
    $bad = Receive-Message $badPath
    Assert-True ($bad.Closed -and $bad.CloseStatus -eq 1008) "wrong path close $($bad.CloseStatus)"
    Close-Api $badPath

    $conn = Connect-Api $ServerUri
    $part1 = '{"jsonrpc":"2.0","id":"frag","method":'
    $part2 = '"system.ping","params":{}}'
    Send-Text $conn $part1 $false
    Send-Text $conn $part2
    $frag = (Receive-Message $conn).Text | ConvertFrom-Json
    Assert-True ($null -ne $frag.result.serverTimeUtc) 'fragmented text message'

    $unknownRoot = Invoke-Rpc $conn 'data.read' @{ root = 'no-such-root' }
    Assert-Kind $unknownRoot 'ROOT_NOT_FOUND' 'unknown root'

    if ($RequireGame) {
        $roots = Invoke-Rpc $conn 'data.roots'
        Assert-True ($roots.result.gameReady -eq $true) 'data.roots gameReady'
        Assert-True ($null -ne $roots.result.sessionId) 'sessionId'
        $tech = Invoke-Rpc $conn 'data.read' @{ root = 'history'; path = @('currentTech') }
        Assert-True ($null -eq $tech.error) 'read history.currentTech'
        $queue = Invoke-Rpc $conn 'data.read' @{ root = 'history'; path = @('techQueue'); offset = 0; limit = 8 }
        Assert-True ($null -eq $queue.error) 'read history.techQueue'
        $techId = [int]$tech.result.value
        if ($techId -eq 0 -and $queue.result.value.items) {
            foreach ($item in @($queue.result.value.items)) {
                if ([int]$item -ne 0) { $techId = [int]$item; break }
            }
        }
        if ($techId -eq 0) {
            throw 'No research ID from currentTech or techQueue'
        }
        $state = Invoke-Rpc $conn 'data.read' @{
            root = 'history'
            path = @('techStates', @{ key = $techId })
            select = @('curLevel', 'hashUploaded', 'hashNeeded')
        }
        Assert-True ($null -eq $state.error) "read techStates[$techId]"
        Assert-True ($null -ne $state.result.value.curLevel) 'research curLevel'
    } else {
        $roots = Invoke-Rpc $conn 'data.roots'
        Assert-True ($roots.result.PSObject.Properties.Name -contains 'gameReady') 'data.roots without save'
        if ($roots.result.gameReady -eq $true) {
            Write-Host 'note game is ready; -RequireGame would exercise research reads'
        }
    }

    $still = Invoke-Rpc $conn 'system.ping'
    Assert-True ($null -ne $still.result.serverTimeUtc) 'ping after protocol errors'
}
finally {
    Close-Api $conn
}

if ($failed -gt 0) {
    Write-Error "FAILED $failed assertion(s)"
    exit 1
}
Write-Host 'OK'
exit 0

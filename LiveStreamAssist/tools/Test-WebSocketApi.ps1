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

function New-OpCts {
    [Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds($TimeoutSec))
}

function Connect-Api {
    param([string] $Uri)
    $ws = [Net.WebSockets.ClientWebSocket]::new()
    $cts = New-OpCts
    try {
        $ws.ConnectAsync([Uri]$Uri, $cts.Token).GetAwaiter().GetResult()
    } catch {
        $ws.Dispose()
        throw
    } finally {
        $cts.Dispose()
    }
    return @{ Ws = $ws }
}

function Close-Api {
    param($Conn)
    if (-not $Conn) { return }
    $cts = New-OpCts
    try {
        if ($Conn.Ws.State -eq [Net.WebSockets.WebSocketState]::Open -or
            $Conn.Ws.State -eq [Net.WebSockets.WebSocketState]::CloseReceived) {
            $Conn.Ws.CloseAsync([Net.WebSockets.WebSocketCloseStatus]::NormalClosure, '', $cts.Token).GetAwaiter().GetResult()
        }
    } catch { }
    finally { $cts.Dispose() }
    try { $Conn.Ws.Dispose() } catch { }
}

function Send-Text {
    param($Conn, [string] $Text, [bool] $End = $true)
    $bytes = $utf8.GetBytes($Text)
    $seg = [ArraySegment[byte]]::new($bytes)
    $cts = New-OpCts
    try {
        $Conn.Ws.SendAsync($seg, [Net.WebSockets.WebSocketMessageType]::Text, $End, $cts.Token).GetAwaiter().GetResult()
    } finally { $cts.Dispose() }
}

function Receive-Message {
    param($Conn)
    $buffer = [byte[]]::new(65536)
    $ms = [IO.MemoryStream]::new()
    $cts = New-OpCts
    try {
        do {
            $seg = [ArraySegment[byte]]::new($buffer)
            $result = $Conn.Ws.ReceiveAsync($seg, $cts.Token).GetAwaiter().GetResult()
            if ($result.MessageType -eq [Net.WebSockets.WebSocketMessageType]::Close) {
                return @{ Closed = $true; CloseStatus = [int]$Conn.Ws.CloseStatus; Text = $null }
            }
            if ($result.MessageType -ne [Net.WebSockets.WebSocketMessageType]::Text -or $ms.Length + $result.Count -gt 262144) {
                throw 'Expected a text response within maxResponseBytes'
            }
            $ms.Write($buffer, 0, $result.Count)
        } while (-not $result.EndOfMessage)
        return @{ Closed = $false; Text = $utf8.GetString($ms.ToArray()) }
    } finally {
        $ms.Dispose()
        $cts.Dispose()
    }
}

function Invoke-Rpc {
    param($Conn, [string] $Method, $Params = @{}, [string] $Id = $(New-Id))
    $body = [ordered]@{ jsonrpc = '2.0'; id = $Id; method = $Method; params = $Params }
    Send-Text $Conn ($body | ConvertTo-Json -Compress -Depth 16)
    $msg = Receive-Message $Conn
    if ($msg.Closed) { throw "Connection closed while waiting for $Method (close $($msg.CloseStatus))" }
    $resp = $msg.Text | ConvertFrom-Json
    $respId = $null
    if ($resp.PSObject.Properties.Name -contains 'id') { $respId = [string]$resp.id }
    if ($respId -ne $Id) { throw "Response id '$respId' did not match '$Id'" }
    return $resp
}

function Has-Prop {
    param($Obj, [string] $Name)
    return $null -ne $Obj -and $Obj.PSObject.Properties.Name -contains $Name
}

function Assert-True {
    param([bool] $Cond, [string] $Name)
    if ($Cond) { Write-Host "ok  $Name" }
    else { Write-Host "fail $Name"; $script:failed++ }
}

function Assert-Kind {
    param($Resp, [string] $Kind, [string] $Name)
    $actual = $null
    if ((Has-Prop $Resp 'error') -and $Resp.error -and (Has-Prop $Resp.error 'data') -and $Resp.error.data -and (Has-Prop $Resp.error.data 'kind')) {
        $actual = [string]$Resp.error.data.kind
    }
    Assert-True ($actual -eq $Kind) "$Name (kind=$actual)"
}

$conn = $null
try {
    $conn = Connect-Api $ServerUri

    $ping = Invoke-Rpc $conn 'system.ping'
    Assert-True ((Has-Prop $ping 'result') -and (Has-Prop $ping.result 'serverTimeUtc') -and $null -ne $ping.result.serverTimeUtc) 'system.ping'

    $info = Invoke-Rpc $conn 'system.info'
    Assert-True ($info.result.apiVersion -eq '1.0.0') 'system.info apiVersion'
    Assert-True ($info.result.limits.maxRequestBytes -eq 65536) 'system.info limits'
    Assert-True ($info.result.capabilities.'api.stats' -eq $false) 'api.stats false'
    Assert-True ($info.result.capabilities.subscriptions -eq $false) 'subscriptions false'
    Assert-True ((Has-Prop $info.result 'gameVersionReady')) 'gameVersionReady present'

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

    $cts = New-OpCts
    try {
        $conn.Ws.SendAsync(
            [ArraySegment[byte]]::new([byte[]](1, 2, 3)),
            [Net.WebSockets.WebSocketMessageType]::Binary,
            $true,
            $cts.Token).GetAwaiter().GetResult()
    } finally { $cts.Dispose() }
    $bin = Receive-Message $conn
    Assert-True ($bin.Closed -and $bin.CloseStatus -eq 1003) "binary rejected $($bin.CloseStatus)"

    Close-Api $conn
    $conn = Connect-Api $ServerUri
    $big = 'x' * 70000
    Send-Text $conn $big
    $over = Receive-Message $conn
    Assert-True ($over.Closed -and $over.CloseStatus -eq 1009) "oversized close $($over.CloseStatus)"

    Close-Api $conn
    $wrong = [UriBuilder]::new($ServerUri)
    $wrong.Scheme = if ($wrong.Scheme -eq 'wss') { 'https' } else { 'http' }
    $wrong.Path = '/nope'
    $wrong.Query = ''
    $http = [Net.Http.HttpClient]::new()
    $request = [Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::Get, $wrong.Uri)
    $request.Headers.Connection.Add('Upgrade')
    $request.Headers.Upgrade.Add([Net.Http.Headers.ProductHeaderValue]::new('websocket'))
    [void]$request.Headers.TryAddWithoutValidation('Sec-WebSocket-Version', '13')
    [void]$request.Headers.TryAddWithoutValidation('Sec-WebSocket-Key', 'dGhlIHNhbXBsZSBub25jZQ==')
    $cts = New-OpCts
    $response = $null
    try {
        $response = $http.SendAsync($request, [Net.Http.HttpCompletionOption]::ResponseHeadersRead, $cts.Token).GetAwaiter().GetResult()
        Assert-True ([int]$response.StatusCode -eq 404) 'wrong path HTTP rejection'
    } finally {
        if ($null -ne $response) { $response.Dispose() }
        $request.Dispose()
        $http.Dispose()
        $cts.Dispose()
    }

    $conn = Connect-Api $ServerUri
    $part1 = '{"jsonrpc":"2.0","id":"frag","method":'
    $part2 = '"system.ping","params":{}}'
    Send-Text $conn $part1 $false
    Send-Text $conn $part2
    $frag = (Receive-Message $conn).Text | ConvertFrom-Json
    Assert-True ((Has-Prop $frag 'result') -and (Has-Prop $frag.result 'serverTimeUtc')) 'fragmented text message'

    $unknownRoot = Invoke-Rpc $conn 'data.read' @{ root = 'no-such-root' }
    Assert-Kind $unknownRoot 'ROOT_NOT_FOUND' 'unknown root'

    if ($RequireGame) {
        $roots = Invoke-Rpc $conn 'data.roots'
        if (-not (Has-Prop $roots 'result') -or -not $roots.result.gameReady) {
            throw '-RequireGame needs a ready player save'
        }
        Assert-True ($roots.result.gameReady -eq $true) 'data.roots gameReady'
        Assert-True ((Has-Prop $roots.result 'sessionId') -and $null -ne $roots.result.sessionId) 'sessionId'
        $tech = Invoke-Rpc $conn 'data.read' @{ root = 'history'; path = @('currentTech') }
        Assert-True (-not (Has-Prop $tech 'error') -or $null -eq $tech.error) 'read history.currentTech'
        $queue = Invoke-Rpc $conn 'data.read' @{ root = 'history'; path = @('techQueue'); offset = 0; limit = 8 }
        Assert-True (-not (Has-Prop $queue 'error') -or $null -eq $queue.error) 'read history.techQueue'
        $techId = [int]$tech.result.value
        if ($techId -eq 0 -and (Has-Prop $queue.result.value 'items') -and $queue.result.value.items) {
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
        Assert-True (-not (Has-Prop $state 'error') -or $null -eq $state.error) "read techStates[$techId]"
        Assert-True ((Has-Prop $state.result.value 'curLevel') -and $null -ne $state.result.value.curLevel) 'research curLevel'
    } else {
        $roots = Invoke-Rpc $conn 'data.roots'
        Assert-True ((Has-Prop $roots.result 'gameReady')) 'data.roots without save'
        if ((Has-Prop $roots.result 'gameReady') -and $roots.result.gameReady -eq $true) {
            Write-Host 'note game is ready; -RequireGame would exercise research reads'
        }
    }

    $still = Invoke-Rpc $conn 'system.ping'
    Assert-True ((Has-Prop $still.result 'serverTimeUtc') -and $null -ne $still.result.serverTimeUtc) 'ping after protocol errors'
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

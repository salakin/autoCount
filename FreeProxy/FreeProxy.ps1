<#
.SYNOPSIS
  Finds a working free public proxy and routes Windows traffic through it.

.EXAMPLE
  .\FreeProxy.ps1              # find fastest proxy and enable it system-wide
  .\FreeProxy.ps1 -NoApply     # only find/test proxies, don't change settings
  .\FreeProxy.ps1 -Off         # turn the system proxy off again

.NOTES
  This is NOT a real VPN: traffic between you and the proxy is not encrypted
  by the proxy, and only apps that honour the Windows proxy setting use it.
  Public proxies are run by unknown people - stick to HTTPS sites and don't
  log in to anything important while it is on.
#>
param(
    [switch]$Off,
    [switch]$NoApply,
    [int]$MaxToTest = 400,
    [int]$TimeoutMs = 6000,
    [int]$Threads = 64
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$RegPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Internet Settings'
$TestUrl = 'https://api.ipify.org'

$Sources = @(
    'https://api.proxyscrape.com/v2/?request=displayproxies&protocol=http&timeout=5000&country=all&ssl=yes&anonymity=all',
    'https://raw.githubusercontent.com/TheSpeedX/PROXY-List/master/http.txt',
    'https://raw.githubusercontent.com/monosans/proxy-list/main/proxies/http.txt'
)

Add-Type -Namespace Win32 -Name WinInet -MemberDefinition @'
[DllImport("wininet.dll", SetLastError = true)]
public static extern bool InternetSetOption(System.IntPtr hInternet, int dwOption, System.IntPtr lpBuffer, int dwBufferLength);
'@

function Update-SystemProxy {
    # INTERNET_OPTION_SETTINGS_CHANGED (39) + INTERNET_OPTION_REFRESH (37)
    [void][Win32.WinInet]::InternetSetOption([IntPtr]::Zero, 39, [IntPtr]::Zero, 0)
    [void][Win32.WinInet]::InternetSetOption([IntPtr]::Zero, 37, [IntPtr]::Zero, 0)
}

function Get-PublicIp {
    try { (New-Object Net.WebClient).DownloadString($TestUrl).Trim() } catch { '?' }
}

if ($Off) {
    Set-ItemProperty $RegPath -Name ProxyEnable -Value 0
    Update-SystemProxy
    Write-Host 'Proxy disabled. Your public IP:' (Get-PublicIp) -ForegroundColor Green
    return
}

# --- 1. Collect proxies -------------------------------------------------------
Write-Host 'Downloading proxy lists...' -ForegroundColor Cyan
$all = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($src in $Sources) {
    try {
        $text = (New-Object Net.WebClient).DownloadString($src)
        $n = 0
        foreach ($line in $text -split "`r?`n") {
            $p = $line.Trim()
            if ($p -match '^\d{1,3}(\.\d{1,3}){3}:\d{2,5}$' -and $all.Add($p)) { $n++ }
        }
        Write-Host "  $n from $src"
    } catch {
        Write-Host "  failed: $src" -ForegroundColor DarkYellow
    }
}
if ($all.Count -eq 0) { throw 'No proxies downloaded.' }

$candidates = $all | Get-Random -Count ([Math]::Min($MaxToTest, $all.Count))
Write-Host "Testing $($candidates.Count) of $($all.Count) proxies (HTTPS through each)..." -ForegroundColor Cyan

# --- 2. Test in parallel ------------------------------------------------------
$test = {
    param($proxy, $url, $timeout)
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $sw = [Diagnostics.Stopwatch]::StartNew()
    try {
        $req = [Net.HttpWebRequest]::Create($url)
        $req.Proxy = New-Object Net.WebProxy("http://$proxy")
        $req.Timeout = $timeout
        $req.ReadWriteTimeout = $timeout
        $resp = $req.GetResponse()
        $body = (New-Object IO.StreamReader($resp.GetResponseStream())).ReadToEnd().Trim()
        $resp.Close()
        if ($body -match '^\d{1,3}(\.\d{1,3}){3}$') {
            [pscustomobject]@{ Proxy = $proxy; Ms = $sw.ElapsedMilliseconds; ExitIp = $body }
        }
    } catch { }
}

$pool = [RunspaceFactory]::CreateRunspacePool(1, $Threads)
$pool.Open()
$jobs = foreach ($p in $candidates) {
    $ps = [PowerShell]::Create().AddScript($test).AddArgument($p).AddArgument($TestUrl).AddArgument($TimeoutMs)
    $ps.RunspacePool = $pool
    [pscustomobject]@{ PS = $ps; Handle = $ps.BeginInvoke() }
}

$working = @()
$done = 0
foreach ($j in $jobs) {
    $working += $j.PS.EndInvoke($j.Handle)
    $j.PS.Dispose()
    $done++
    Write-Progress -Activity 'Testing proxies' -Status "$done / $($jobs.Count)  working: $($working.Count)" -PercentComplete ($done * 100 / $jobs.Count)
}
Write-Progress -Activity 'Testing proxies' -Completed
$pool.Close()

if ($working.Count -eq 0) { throw 'No working proxies found. Try again or raise -MaxToTest.' }

$working = $working | Sort-Object Ms
Write-Host "`n$($working.Count) working proxies. Fastest:" -ForegroundColor Green
$working | Select-Object -First 10 | Format-Table -AutoSize | Out-Host
$working | ForEach-Object { $_.Proxy } | Set-Content (Join-Path $PSScriptRoot 'working-proxies.txt')

if ($NoApply) { return }

# --- 3. Apply system-wide -----------------------------------------------------
$best = $working[0]
Set-ItemProperty $RegPath -Name ProxyServer -Value $best.Proxy
Set-ItemProperty $RegPath -Name ProxyOverride -Value '<local>;localhost;127.*;10.*;192.168.*'
Set-ItemProperty $RegPath -Name ProxyEnable -Value 1
Update-SystemProxy

Write-Host "Proxy ON: $($best.Proxy)  (exit IP $($best.ExitIp))" -ForegroundColor Green
Write-Host 'Run  .\FreeProxy.ps1 -Off  to turn it off.'

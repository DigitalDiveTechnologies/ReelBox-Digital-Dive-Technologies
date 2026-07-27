# Verifies the built debug APK contains ACTION_SEND intent filters.
# Usage (from repo root):
#   .\mobile\tool\verify_share_apk.ps1
# Requires: flutter build apk --debug, Android SDK build-tools (aapt or aapt2).

$ErrorActionPreference = "Stop"

$mobileRoot = Split-Path -Parent $PSScriptRoot
$apk = Join-Path $mobileRoot "build\app\outputs\flutter-apk\app-debug.apk"

if (-not (Test-Path $apk)) {
    Write-Host "APK not found: $apk"
    Write-Host "Run: cd mobile; flutter build apk --debug"
    exit 1
}

$apkItem = Get-Item $apk
Write-Host "APK: $($apkItem.FullName)"
Write-Host "Size: $($apkItem.Length) bytes"
Write-Host "Modified: $($apkItem.LastWriteTime)"

$sdkRoot = $env:ANDROID_HOME
if (-not $sdkRoot) {
    $sdkRoot = $env:ANDROID_SDK_ROOT
}
if (-not $sdkRoot) {
    Write-Host "ANDROID_HOME / ANDROID_SDK_ROOT not set — cannot dump manifest."
    exit 2
}

$buildTools = Get-ChildItem (Join-Path $sdkRoot "build-tools") -Directory |
    Sort-Object Name -Descending |
    Select-Object -First 1

if (-not $buildTools) {
    Write-Host "No build-tools under $sdkRoot"
    exit 2
}

$aapt = Join-Path $buildTools.FullName "aapt.exe"
$aapt2 = Join-Path $buildTools.FullName "aapt2.exe"
$dumpTool = if (Test-Path $aapt) { $aapt } elseif (Test-Path $aapt2) { $aapt2 } else { $null }

if (-not $dumpTool) {
    Write-Host "aapt/aapt2 not found in $($buildTools.FullName)"
    exit 2
}

Write-Host "`n--- badging ---"
& $dumpTool dump badging $apk 2>&1 | Select-String -Pattern "package:|launchable-activity|sdkVersion"

Write-Host "`n--- manifest (SEND / MainActivity) ---"
$manifestDump = & $dumpTool dump xmltree $apk AndroidManifest.xml 2>&1 | Out-String
$manifestDump -split "`n" | Where-Object {
    $_ -match "SEND|MainActivity|text/plain|mimeType|launchMode"
}

if ($manifestDump -notmatch "android.intent.action.SEND") {
    Write-Host "`nFAIL: No ACTION_SEND in packaged manifest."
    exit 3
}

Write-Host "`nOK: APK contains ACTION_SEND filters."

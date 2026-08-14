param(
    [string] $Configuration = "Release",
    [string] $OutputDirectory = (Join-Path $PSScriptRoot "..\artifacts")
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$appProject = Join-Path $repoRoot "PrivacyDot\PrivacyDot.csproj"
$payloadDir = Join-Path $PSScriptRoot "payload"
$targetDir = Resolve-Path -Path (New-Item -ItemType Directory -Force -Path $OutputDirectory)
$targetExe = Join-Path $targetDir "PrivacyDotSetup.exe"

dotnet build $appProject -c $Configuration

if (Test-Path $payloadDir) {
    Remove-Item -Recurse -Force $payloadDir
}

New-Item -ItemType Directory -Force -Path $payloadDir | Out-Null

$buildDir = Join-Path $repoRoot "PrivacyDot\bin\$Configuration\net48"
Copy-Item -Force (Join-Path $buildDir "PrivacyDot.exe") $payloadDir
Copy-Item -Force (Join-Path $buildDir "PrivacyDot.exe.config") $payloadDir
Copy-Item -Force (Join-Path $PSScriptRoot "InstallPrivacyDot.cmd") $payloadDir
Copy-Item -Force (Join-Path $PSScriptRoot "CreateShortcuts.ps1") $payloadDir
Copy-Item -Force (Join-Path $PSScriptRoot "UninstallPrivacyDot.cmd") $payloadDir

$tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd("\")
$packagingDir = Join-Path $tempRoot ("PrivacyDot-IExpress-{0:N}" -f [Guid]::NewGuid())
$resolvedPackagingDir = [System.IO.Path]::GetFullPath($packagingDir)

if (-not $resolvedPackagingDir.StartsWith(
    $tempRoot + [System.IO.Path]::DirectorySeparatorChar,
    [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "The temporary packaging directory was not trusted."
}

New-Item -ItemType Directory -Path $packagingDir | Out-Null

try {
    $packagingPayloadDir = Join-Path $packagingDir "payload"
    New-Item -ItemType Directory -Path $packagingPayloadDir | Out-Null

    foreach ($fileName in @(
        "PrivacyDot.exe",
        "PrivacyDot.exe.config",
        "InstallPrivacyDot.cmd",
        "CreateShortcuts.ps1",
        "UninstallPrivacyDot.cmd")) {
        Copy-Item `
            -LiteralPath (Join-Path $payloadDir $fileName) `
            -Destination $packagingPayloadDir
    }

    $payloadPath = $packagingPayloadDir
    if (-not $payloadPath.EndsWith("\")) {
        $payloadPath += "\"
    }

    $packagingTargetExe = Join-Path $packagingDir "PrivacyDotSetup.exe"
    $sedPath = Join-Path $packagingDir "PrivacyDotSetup.SED"
    $sed = @"
[Version]
Class=IEXPRESS
SEDVersion=3

[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=1
HideExtractAnimation=1
UseLongFileName=1
InsideCompressed=0
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=%InstallPrompt%
DisplayLicense=%DisplayLicense%
FinishMessage=%FinishMessage%
TargetName=%TargetName%
FriendlyName=%FriendlyName%
AppLaunched=%AppLaunched%
PostInstallCmd=%PostInstallCmd%
AdminQuietInstCmd=%AdminQuietInstCmd%
UserQuietInstCmd=%UserQuietInstCmd%
SourceFiles=SourceFiles

[Strings]
InstallPrompt=
DisplayLicense=
FinishMessage=PrivacyDot has been installed.
TargetName=$packagingTargetExe
FriendlyName=PrivacyDot Setup
AppLaunched=InstallPrivacyDot.cmd
PostInstallCmd=<None>
AdminQuietInstCmd=
UserQuietInstCmd=
FILE0="PrivacyDot.exe"
FILE1="PrivacyDot.exe.config"
FILE2="InstallPrivacyDot.cmd"
FILE3="CreateShortcuts.ps1"
FILE4="UninstallPrivacyDot.cmd"

[SourceFiles]
SourceFiles0=$payloadPath

[SourceFiles0]
%FILE0%=
%FILE1%=
%FILE2%=
%FILE3%=
%FILE4%=
"@

    $sed = $sed -replace "`r?`n", "`r`n"
    Set-Content -LiteralPath $sedPath -Value $sed -Encoding ASCII

    if (Test-Path -LiteralPath $targetExe) {
        Remove-Item -LiteralPath $targetExe -Force
    }

    $iexpressExitCode = $null
    $iexpressPaths = @(
        (Join-Path $env:SystemRoot "SysWOW64\iexpress.exe"),
        (Join-Path $env:SystemRoot "System32\iexpress.exe")) | Select-Object -Unique

    foreach ($iexpressPath in $iexpressPaths) {
        if (-not (Test-Path -LiteralPath $iexpressPath)) {
            continue
        }

        $iexpressProcess = Start-Process `
            -FilePath $iexpressPath `
            -ArgumentList @("/N", $sedPath) `
            -WindowStyle Hidden `
            -Wait `
            -PassThru
        $iexpressExitCode = $iexpressProcess.ExitCode

        if ($iexpressExitCode -eq 0 -and (Test-Path -LiteralPath $packagingTargetExe)) {
            break
        }
    }

    if ($iexpressExitCode -ne 0) {
        throw "IExpress failed with exit code $iexpressExitCode."
    }

    if (-not (Test-Path -LiteralPath $packagingTargetExe)) {
        throw "IExpress did not create the setup executable."
    }

    Move-Item -LiteralPath $packagingTargetExe -Destination $targetExe
}
finally {
    if (Test-Path -LiteralPath $resolvedPackagingDir) {
        Remove-Item -LiteralPath $resolvedPackagingDir -Recurse -Force
    }
}

Write-Host "Created $targetExe"

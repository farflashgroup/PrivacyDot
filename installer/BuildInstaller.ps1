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
$sedPath = Join-Path $PSScriptRoot "PrivacyDotSetup.SED"

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

$payloadPath = $payloadDir
if (-not $payloadPath.EndsWith("\")) {
    $payloadPath += "\"
}

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
DisplayLicense=
FinishMessage=%FinishMessage%
TargetName=%TargetName%
FriendlyName=%FriendlyName%
AppLaunched=%AppLaunched%
PostInstallCmd=<None>
AdminQuietInstCmd=
UserQuietInstCmd=
SourceFiles=SourceFiles

[Strings]
InstallPrompt=
FinishMessage=PrivacyDot has been installed.
TargetName=$targetExe
FriendlyName=PrivacyDot Setup
AppLaunched=InstallPrivacyDot.cmd
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

Set-Content -Path $sedPath -Value $sed -Encoding ASCII

iexpress.exe /N /Q $sedPath

if (-not (Test-Path $targetExe)) {
    throw "IExpress did not create $targetExe"
}

Write-Host "Created $targetExe"

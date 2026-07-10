param(
    [Parameter(Mandatory = $true)]
    [string] $TargetPath
)

$shell = New-Object -ComObject WScript.Shell
$startupDir = [Environment]::GetFolderPath("Startup")
$programsDir = [Environment]::GetFolderPath("Programs")
$appDir = Split-Path -Parent $TargetPath

function New-PrivacyDotShortcut {
    param(
        [string] $ShortcutPath
    )

    $shortcut = $shell.CreateShortcut($ShortcutPath)
    $shortcut.TargetPath = $TargetPath
    $shortcut.WorkingDirectory = $appDir
    $shortcut.IconLocation = $TargetPath
    $shortcut.Save()
}

if (-not (Test-Path $programsDir)) {
    New-Item -ItemType Directory -Path $programsDir | Out-Null
}

New-PrivacyDotShortcut (Join-Path $startupDir "Privacy Dot.lnk")
New-PrivacyDotShortcut (Join-Path $programsDir "Privacy Dot.lnk")

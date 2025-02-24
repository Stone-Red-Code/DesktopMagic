param(
    [string]$AppPath,
    [switch]$Enable,
    [switch]$Disable
)

# Validate input
if ([string]::IsNullOrWhiteSpace($AppPath) -or !(Test-Path $AppPath)) {
    exit 5  # Invalid app path
}

$AppName = [System.IO.Path]::GetFileNameWithoutExtension($AppPath)
$StartupPath = [Environment]::GetFolderPath('Startup')
$ShortcutPath = Join-Path $StartupPath "$AppName.lnk"

try {
    if ($Enable) {
        if (Test-Path $ShortcutPath) {
            exit 2  # Already enabled
        }

        $WshShell = New-Object -ComObject WScript.Shell
        $Shortcut = $WshShell.CreateShortcut($ShortcutPath)
        $Shortcut.TargetPath = $AppPath
        $Shortcut.Save()
        exit 0  # Success
    }
    elseif ($Disable) {
        if (!(Test-Path $ShortcutPath)) {
            exit 3  # Already disabled
        }

        Remove-Item $ShortcutPath -Force
        exit 0  # Success
    }
    else {
        exit 1  # Invalid parameters
    }
}
catch {
    exit 4  # Unexpected error
}
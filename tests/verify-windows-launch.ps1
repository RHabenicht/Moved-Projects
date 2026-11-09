param(
    [Parameter(Mandatory = $true)]
    [string] $Executable
)

$ErrorActionPreference = 'Stop'
$path = (Resolve-Path $Executable).Path
if (-not (Test-Path $path)) { throw "Executable does not exist: $path" }

$process = Start-Process -FilePath $path -PassThru
try {
    $ready = $false
    for ($i = 0; $i -lt 80; $i++) {
        Start-Sleep -Milliseconds 250
        $process.Refresh()
        if ($process.HasExited) {
            throw "NSRCG II exited during startup (exit code: $($process.ExitCode))"
        }
        if ($process.MainWindowHandle -ne [IntPtr]::Zero) {
            $title = $process.MainWindowTitle
            if ($title -notlike 'NSRCG II*') {
                throw "Unexpected main window title: $title"
            }
            Write-Host "Windows startup passed: PID $($process.Id); title: $title"
            $ready = $true
            break
        }
    }
    if (-not $ready) {
        throw 'NSRCG II failed to open a main window within 20 seconds.'
    }
}
finally {
    $process.Refresh()
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        $process.WaitForExit(5000) | Out-Null
    }
}

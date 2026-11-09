$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src/NSRCGII.vbproj'
$output = Join-Path $root 'publish'

& dotnet publish $project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o $output
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)" }

$catalog = Join-Path $root 'LegacyData'
if (Test-Path $catalog) {
    Copy-Item -Path $catalog -Destination (Join-Path $output 'LegacyData') -Recurse -Force
}
Write-Host "Build output: $output"

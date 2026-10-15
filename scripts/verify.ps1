$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    dotnet build -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    dotnet run --project tests/SimpsonParadox.Tests -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Numerical/report tests failed.' }
    $app = 'src/SimpsonParadox/bin/Release/net10.0/SimpsonParadox.dll'
    function Run-App([string[]] $Arguments, [int] $Expected = 0) {
        & dotnet $app @Arguments
        if ($LASTEXITCODE -ne $Expected) { throw "Unexpected exit code for $Arguments" }
    }
    Run-App @('demo', '--output', 'artifacts/verify-demo')
    $first = Get-Content artifacts/verify-demo/report.json -Raw
    $firstSvg = Get-Content artifacts/verify-demo/report.svg -Raw
    Run-App @('demo', '--output', 'artifacts/verify-demo')
    if ($first -cne (Get-Content artifacts/verify-demo/report.json -Raw) -or $firstSvg -cne (Get-Content artifacts/verify-demo/report.svg -Raw)) { throw 'Reports are not deterministic.' }
    Run-App @('search', '--output', 'artifacts/verify-search')
    $report = Get-Content artifacts/verify-search/report.json -Raw | ConvertFrom-Json
    if ($report.search.examined -ne 3748096 -or $report.search.reversals -ne 19020 -or !$report.search.exhausted -or $report.analysis.reversalMagnitude.fraction -ne '5/9') { throw 'Default search regression.' }
    Run-App @('search', '--max-configurations', '100', '--output', 'artifacts/verify-prefix')
    $prefix = Get-Content artifacts/verify-prefix/report.json -Raw | ConvertFrom-Json
    if ($prefix.search.examined -ne 100 -or $prefix.search.exhausted) { throw 'Prefix coverage incorrect.' }
    Run-App @('search', '--max-total', '1', '--output', 'artifacts/verify-empty')
    $empty = Get-Content artifacts/verify-empty/report.json -Raw | ConvertFrom-Json
    if ($null -ne $empty.analysis -or !$empty.search.exhausted) { throw 'Empty search incorrect.' }
    Run-App @('demo', '--weights', 'pooled', '--output', 'artifacts/verify-pooled')
    Run-App @('--help')
    Run-App @('unknown') 2
    Run-App @('demo', '--weights', 'bad') 2
    Run-App @('demo', '--output') 2
    Run-App @('demo', '--max-total', '4') 2
    Run-App @('search', '--max-total', '0') 2
    Run-App @('search', '--max-total', '1001') 2
    Run-App @('search', '--max-configurations', '0') 2
    Run-App @('search', '--max-total', 'eight') 2
    Run-App @('demo', '--weights', 'equal', '--weights', 'pooled') 2
    Write-Host 'PASS: CLI integration checks'
}
finally { Pop-Location }

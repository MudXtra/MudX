param (
    [string]$PackageDirectory,
    [string[]]$ExpectedFiles
)

# Show all files in the package directory for debugging
Write-Host "📦 Package contents:"
Get-ChildItem -Recurse -Path $PackageDirectory | ForEach-Object {
    Write-Host "• $($_.FullName)"
}

$missing = @()

foreach ($file in $ExpectedFiles) {
    $found = Get-ChildItem -Recurse -Path $PackageDirectory -Filter $file
    if (-not $found) {
        Write-Host "❌ Missing: $file"
        $missing += $file
    } else {
        Write-Host "✅ Found: $file"
    }
}

if ($missing.Count -gt 0) {
    Write-Error "Missing static assets: $($missing -join ', ')"
    exit 1
}

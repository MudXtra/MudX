param (
    [string]$PackageFile,
    [string[]]$ExpectedFiles
)

# Load ZIP handling library
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Open the nupkg file as a zip archive
$zip = [System.IO.Compression.ZipFile]::OpenRead($PackageFile)

# Display all files in the archive
Write-Host "📦 Package contents:"
$allEntries = $zip.Entries | ForEach-Object {
    Write-Host "• $($_.FullName)"
    $_.FullName
}

# Track missing files
$missing = @()

foreach ($file in $ExpectedFiles) {
    # Check for exact match (case-insensitive)
    $match = $allEntries | Where-Object { [System.IO.Path]::GetFileName($_) -ieq $file }
    if (-not $match) {
        Write-Host "❌ Missing: $file"
        $missing += $file
    } else {
        Write-Host "✅ Found: $file"
    }
}

# Close ZIP archive
$zip.Dispose()

# Fail the script if any assets are missing
if ($missing.Count -gt 0) {
    Write-Error "Missing static assets: $($missing -join ', ')"
    exit 1
}

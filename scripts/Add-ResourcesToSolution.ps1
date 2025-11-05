# Add Resources folder to Visual Studio Solution
# This script modifies the .sln file to include Resources folder as a Solution Folder

param(
    [Parameter(Mandatory=$false)]
    [string]$SolutionFile = ".\StampService.sln"
)

$ErrorActionPreference = "Stop"

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "   Add Resources Folder to Solution       " -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $SolutionFile)) {
    Write-Host "ERROR: Solution file not found: $SolutionFile" -ForegroundColor Red
    exit 1
}

# Check if Resources folder exists
if (-not (Test-Path ".\Resources")) {
    Write-Host "ERROR: Resources folder not found" -ForegroundColor Red
    exit 1
}

Write-Host "Reading solution file..." -ForegroundColor Yellow
$slnContent = Get-Content $SolutionFile -Raw

# Check if Resources folder already exists in solution
if ($slnContent -match 'Project\(".*?"\)\s*=\s*"Resources"') {
    Write-Host "Resources folder already exists in solution!" -ForegroundColor Green
    Write-Host "Opening Visual Studio to view..." -ForegroundColor Yellow
    Start-Process $SolutionFile
    exit 0
}

# Generate GUID for the solution folder
$guid = [guid]::NewGuid().ToString().ToUpper()

# Get all .md files in Resources folder
$resourceFiles = Get-ChildItem ".\Resources\*.md" | Sort-Object Name

Write-Host "Found $($resourceFiles.Count) documentation files" -ForegroundColor Cyan

# Build the project section
$projectSection = @"
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "Resources", "Resources", "{$guid}"
	ProjectSection(SolutionItems) = preProject

"@

foreach ($file in $resourceFiles) {
    $relativePath = "Resources\$($file.Name)"
    $projectSection += "`t`t$relativePath = $relativePath`r`n"
}

$projectSection += @"
	EndProjectSection
EndProject

"@

# Find the insertion point (before "Global" section)
$insertionPoint = $slnContent.IndexOf("Global")
if ($insertionPoint -eq -1) {
    Write-Host "ERROR: Could not find 'Global' section in solution file" -ForegroundColor Red
    exit 1
}

# Backup original solution file
$backupFile = "$SolutionFile.backup"
Copy-Item $SolutionFile $backupFile -Force
Write-Host "Created backup: $backupFile" -ForegroundColor Yellow

# Insert the new project section
$newContent = $slnContent.Insert($insertionPoint, $projectSection)

# Write the modified content
$newContent | Out-File -FilePath $SolutionFile -Encoding UTF8 -NoNewline

Write-Host ""
Write-Host "? Resources folder added to solution!" -ForegroundColor Green
Write-Host ""
Write-Host "Files added:" -ForegroundColor Cyan
foreach ($file in $resourceFiles) {
    Write-Host "  - $($file.Name)" -ForegroundColor White
}
Write-Host ""
Write-Host "Opening Visual Studio..." -ForegroundColor Yellow
Start-Process $SolutionFile

Write-Host ""
Write-Host "If something goes wrong, restore from backup:" -ForegroundColor Yellow
Write-Host "  Copy-Item '$backupFile' '$SolutionFile' -Force" -ForegroundColor White
Write-Host ""

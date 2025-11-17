# Documentation Consolidation Script
# Removes redundant documentation files

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Documentation Consolidation" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Files to delete (redundant/superseded documentation)
$filesToDelete = @(
    "ADMINGUI-IMPLEMENTATION.md",
    "ADMINGUI-PROJECT-COMPLETE.md",
    "POLISH-IMPLEMENTATION-COMPLETE.md",
    "ADMIN-CHECK-IMPLEMENTATION-COMPLETE.md",
    "CONDITIONAL-ADMIN-CHECK.md",
    "NEXT-INTEGRATION-COMPLETE.md",
  "EMOJI-FIXES-COMPLETE.md",
    "EMOJI-FIX-QUICK-REF.md",
    "INSTALLER-IMPLEMENTATION-COMPLETE.md",
    "UNINSTALLER-IMPLEMENTATION-SUMMARY.md",
    "UNINSTALLER-QUICK-REF.md",
    "OPTION3-IMPLEMENTATION-COMPLETE.md"
)

$ResourcesDir = "Resources"

Write-Host "Files to be removed:" -ForegroundColor Yellow
foreach ($file in $filesToDelete) {
    Write-Host "  • $file" -ForegroundColor Gray
}
Write-Host ""

$confirmation = Read-Host "Proceed with deletion? (Y/N)"

if ($confirmation -ne "Y" -and $confirmation -ne "y") {
    Write-Host "Cancelled." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Removing files..." -ForegroundColor Cyan

$deletedCount = 0
$notFoundCount = 0

foreach ($file in $filesToDelete) {
  $path = Join-Path $ResourcesDir $file
    
    if (Test-Path $path) {
        try {
            Remove-Item $path -Force
   Write-Host "  ? Deleted: $file" -ForegroundColor Green
            $deletedCount++
        }
        catch {
   Write-Host "  ? Error deleting: $file - $($_.Exception.Message)" -ForegroundColor Red
 }
    }
    else {
        Write-Host "  ??  Not found: $file" -ForegroundColor Gray
        $notFoundCount++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Consolidation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Files deleted: $deletedCount" -ForegroundColor White
Write-Host "  Files not found: $notFoundCount" -ForegroundColor White
Write-Host ""

Write-Host "Remaining documentation files:" -ForegroundColor Cyan
$remainingFiles = Get-ChildItem "$ResourcesDir\*.md" | Sort-Object Name
Write-Host "  Total: $($remainingFiles.Count) files" -ForegroundColor White
Write-Host ""

Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Review Resources\INDEX.md and update if needed" -ForegroundColor Gray
Write-Host "  2. Update main README.md links if needed" -ForegroundColor Gray
Write-Host "  3. Commit changes to git" -ForegroundColor Gray
Write-Host ""

Write-Host "? Documentation consolidation complete!" -ForegroundColor Green
Write-Host ""

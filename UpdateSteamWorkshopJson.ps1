param(
    [Parameter(Mandatory = $true)]
    [string]$WorkshopPath,

    [Parameter(Mandatory = $true)]
    [string]$ChangelogPath
)

if (-not (Test-Path $WorkshopPath)) {
    Write-Error "Unable to find workshop.json at $WorkshopPath"
    exit 1
}

$workshopJson = Get-Content $WorkshopPath -Raw | ConvertFrom-Json -AsHashtable
$workshopJson["changeNote"] = [string](Get-Content $ChangelogPath -Raw)
$workshopJson | ConvertTo-Json -Depth 10 | Set-Content $WorkshopPath
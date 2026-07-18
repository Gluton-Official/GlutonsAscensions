param(
    [Parameter(Position = 0, Mandatory = $true)]
    [ValidateSet("MAJOR", "MINOR", "PATCH")]
    [string]$Increment,

    [Parameter(Position = 1, Mandatory = $true)]
    [ValidateScript({ if (Test-Path $_) { $true } else { throw "File $_ does not exist" } })]
    [string]$JsonFile
)

$Increment = $Increment.ToUpperInvariant()

$content = Get-Content $JsonFile -Raw
$jsonContent = $content | ConvertFrom-Json -AsHashtable

$semVer = [semver]($jsonContent.version -replace '^v')

$newSemVer = switch ($Increment) {
    "MAJOR" { [semver]::new($semVer.Major + 1, 0, 0) }
    "MINOR" { [semver]::new($semVer.Major, $semVer.Minor + 1, 0) }
    "PATCH" { [semver]::new($semVer.Major, $semVer.Minor, $semVer.Patch + 1) }
}

Write-Host "Bumped version from $semVer to $newSemVer"

$content = $content -replace '(?<="version": "v).+?(?=")', $newSemVer

$content | Set-Content $JsonFile -Encoding UTF8 -NoNewline
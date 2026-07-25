
$imagePath = (Get-Item -LiteralPath "sample.png").FullName

$originalHash =
    Get-FileHash $imagePath -Algorithm SHA256

$apiImagePath = Join-Path -Path $PSScriptRoot -ChildPath "..\..\CodeOcr.Api\App_Data\Images\29b706bb0c514bf2843290651c804224.png" | Resolve-Path

$storedHash = Get-FileHash $apiImagePath -Algorithm SHA256

$originalHash.Hash
$storedHash.Hash

if ($originalHash.Hash -eq $storedHash.Hash) {
    Write-Host "hash ok" -ForegroundColor Green
} else {
    Write-Host "hash nok" -ForegroundColor Red
}
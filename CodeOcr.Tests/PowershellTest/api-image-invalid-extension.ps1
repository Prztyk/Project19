$baseUrl = "https://localhost:7207"
$form = @{
    file = Get-Item "E:\Repo\Project19\CodeOcr.Tests\PowershellTest\sample.txt"
}

Invoke-WebRequest `
    -Uri "$baseUrl/api/images/validate" `
    -Method Post `
    -Form $form `
    -SkipHttpErrorCheck


$response = Invoke-WebRequest `
    -Uri "$baseUrl/api/images/validate" `
    -Method Post `
    -Form $form `
    -SkipHttpErrorCheck

Write-Host "Decoded content:"
Write-Host ""
$jsonText = [System.Text.Encoding]::UTF8.GetString($response.Content)

$jsonText | ConvertFrom-Json | ConvertTo-Json -Depth 10
$baseUrl = "https://localhost:7207"
$imagePath = "sample.png"
$form = @{
    file = Get-Item $imagePath
}

Invoke-RestMethod `
    -Uri "$baseUrl/api/images/validate" `
    -Method Post `
    -Form $form
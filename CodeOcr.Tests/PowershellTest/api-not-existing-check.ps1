$baseUrl = "https://localhost:7207"

$response = Invoke-WebRequest `
    -Uri "$baseUrl/api/does-not-exist" `
    -Method Get `
    -SkipHttpErrorCheck

if ($response.Content -is [byte[]]) {
    $rawResponse =
        [System.Text.Encoding]::UTF8.GetString(
            $response.Content)
}
else {
    $rawResponse = [string] $response.Content
}

Write-Host "HTTP status: $($response.StatusCode)"
Write-Host ""
Write-Host "Raw response:"
Write-Host $rawResponse
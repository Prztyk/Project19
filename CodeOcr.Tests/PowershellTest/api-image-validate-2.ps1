$baseUrl = "https://localhost:7207"
$imagePath = "sample.png"

if (-not (Test-Path -LiteralPath $imagePath -PathType Leaf)) {
    throw "Image file was not found: $imagePath"
}

$extension = [System.IO.Path]::GetExtension($imagePath).ToLowerInvariant()

$contentType = switch ($extension) {
    ".png"  { "image/png" }
    ".jpg"  { "image/jpeg" }
    ".jpeg" { "image/jpeg" }
    ".webp" { "image/webp" }
    default {
        throw "Unsupported image extension: $extension"
    }
}

$httpClient = [System.Net.Http.HttpClient]::new()
$multipartContent = [System.Net.Http.MultipartFormDataContent]::new()
$fileStream = $null
$fileContent = $null

try {
    $fileStream = [System.IO.File]::OpenRead($imagePath)
    $fileContent = [System.Net.Http.StreamContent]::new($fileStream)

    $fileContent.Headers.ContentType =
        [System.Net.Http.Headers.MediaTypeHeaderValue]::new($contentType)

    $fileName = [System.IO.Path]::GetFileName($imagePath)

    $multipartContent.Add(
        $fileContent,
        "file",
        $fileName)

    $response = $httpClient.PostAsync(
        "$baseUrl/api/images/validate",
        $multipartContent).GetAwaiter().GetResult()

    $responseBody = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()

    Write-Host "Status: $([int]$response.StatusCode) $($response.StatusCode)"
    Write-Host $responseBody

    if (-not $response.IsSuccessStatusCode) {
        exit 1
    }
}
finally {
    $multipartContent.Dispose()
    $httpClient.Dispose()

    # Disposing multipartContent also disposes its nested content and stream.
}
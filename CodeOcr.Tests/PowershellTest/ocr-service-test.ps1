$baseUrl = "http://127.0.0.1:8000"
$imagePath = (Get-Item -LiteralPath "sample.png").FullName

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ImagePath -PathType Leaf)) {
    throw "The image file does not exist: '$ImagePath'."
}

$contentTypes = @{
    ".png"  = "image/png"
    ".jpg"  = "image/jpeg"
    ".jpeg" = "image/jpeg"
    ".webp" = "image/webp"
}

$extension = [System.IO.Path]::GetExtension(
    $ImagePath
).ToLowerInvariant()

if (-not $contentTypes.ContainsKey($extension)) {
    throw "Unsupported image extension: '$extension'."
}

$contentType = $contentTypes[$extension]
$fileName = [System.IO.Path]::GetFileName($ImagePath)
$requestUri = "$($BaseUrl.TrimEnd('/'))/api/ocr"

$httpClient = [System.Net.Http.HttpClient]::new()
$multipartContent =
    [System.Net.Http.MultipartFormDataContent]::new()

try {
    $fileStream = [System.IO.File]::OpenRead($ImagePath)
    $fileContent =
        [System.Net.Http.StreamContent]::new($fileStream)

    $fileContent.Headers.ContentType =
        [System.Net.Http.Headers.MediaTypeHeaderValue]::new(
            $contentType
        )

    $multipartContent.Add(
        $fileContent,
        "file",
        $fileName
    )

    $response = $httpClient.PostAsync($requestUri, $multipartContent).GetAwaiter().GetResult()

    $rawResponse = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()

    Write-Host (
        "HTTP status: {0} {1}" -f
        [int] $response.StatusCode,
        $response.ReasonPhrase
    )

    Write-Host ""
    Write-Host "Raw response:"
    Write-Host $rawResponse

    if (-not [string]::IsNullOrWhiteSpace($rawResponse)) {
        Write-Host ""
        Write-Host "Parsed response:"

        $rawResponse |
            ConvertFrom-Json |
            Format-List
    }
}
finally {
    $multipartContent.Dispose()
    $httpClient.Dispose()
}
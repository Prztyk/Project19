$baseUrl = "https://127.0.0.1:7207"
$imagePath = (Get-Item -LiteralPath "sample.png").FullName

$ErrorActionPreference = "Stop"

if (-not (
    Test-Path `
        -LiteralPath $ImagePath `
        -PathType Leaf
)) {
    throw "The image file does not exist: '$ImagePath'."
}

$contentTypes = @{
    ".png"  = "image/png"
    ".jpg"  = "image/jpeg"
    ".jpeg" = "image/jpeg"
    ".webp" = "image/webp"
}

$extension =
    [System.IO.Path]::GetExtension(
        $ImagePath
    ).ToLowerInvariant()

if (-not $contentTypes.ContainsKey($extension)) {
    throw "Unsupported image extension: '$extension'."
}

$contentType = $contentTypes[$extension]
$fileName =
    [System.IO.Path]::GetFileName($ImagePath)

$requestUri =
    "$($BaseUrl.TrimEnd('/'))/api/ocr/recognize"

$httpClient =
    [System.Net.Http.HttpClient]::new()

$multipartContent =
    [System.Net.Http.MultipartFormDataContent]::new()

try {
    $fileBytes = [System.IO.File]::ReadAllBytes($ImagePath)
    $fileContent = [System.Net.Http.ByteArrayContent]::new($fileBytes)
    $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::new($contentType)

    $multipartContent.Add(
        $fileContent,
        "file",
        $fileName
    )

    Write-Host "Sending image to .NET OCR endpoint:"
    Write-Host "  Path:         $ImagePath"
    Write-Host "  Content type: $contentType"
    Write-Host "  Endpoint:     $requestUri"
    Write-Host ""

    $response = $httpClient.PostAsync($requestUri,$multipartContent).GetAwaiter().GetResult()

    $rawResponse = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()

    Write-Host (
        "HTTP status: {0} {1}" -f
        [int] $response.StatusCode,
        $response.ReasonPhrase
    )

    Write-Host ""
    Write-Host "Raw response:"
    Write-Host $rawResponse

    if (-not [string]::IsNullOrWhiteSpace(
            $rawResponse)) {
        try {
            Write-Host ""
            Write-Host "Parsed response:"

            $rawResponse |
                ConvertFrom-Json |
                Format-List
        }
        catch {
            Write-Warning (
                "The response body is not valid JSON."
            )
        }
    }

    if (-not $response.IsSuccessStatusCode) {
        exit 1
    }
}
catch {
    Write-Host ""
    Write-Host "Request failed." -ForegroundColor Red

    $currentException = $_.Exception
    $level = 0

    while ($null -ne $currentException) {
        Write-Host ""
        Write-Host "Exception level ${level}:"
        Write-Host "Type:    $($currentException.GetType().FullName)"
        Write-Host "Message: $($currentException.Message)"

        $currentException = $currentException.InnerException
        $level++
    }

    exit 1
}
finally {
    $multipartContent.Dispose()
    $httpClient.Dispose()
}
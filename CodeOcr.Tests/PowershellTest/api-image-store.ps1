$baseUrl = "https://localhost:7207"
$ImagePath = (Get-Item -LiteralPath "sample.png").FullName

function Get-ImageContentType {
    param(
        [Parameter(Mandatory)]
        [string] $FilePath
    )

    $extension = [System.IO.Path]::GetExtension(
        $FilePath
    ).ToLowerInvariant()

    switch ($extension) {
        ".png" {
            return "image/png"
        }

        ".jpg" {
            return "image/jpeg"
        }

        ".jpeg" {
            return "image/jpeg"
        }

        ".webp" {
            return "image/webp"
        }

        default {
            throw "Unsupported image extension: '$extension'."
        }
    }
}

if (-not (Test-Path -LiteralPath $ImagePath -PathType Leaf)) {
    throw "The image file does not exist: '$ImagePath'."
}

$contentType = Get-ImageContentType -FilePath $ImagePath
$fileName = [System.IO.Path]::GetFileName($ImagePath)
$requestUri = "$($BaseUrl.TrimEnd('/'))/api/images"

$httpClient = [System.Net.Http.HttpClient]::new()
$multipartContent = [System.Net.Http.MultipartFormDataContent]::new()
$fileStream = $null
$fileContent = $null

try {
    $fileStream = [System.IO.File]::OpenRead($ImagePath)

    $fileContent = [System.Net.Http.StreamContent]::new($fileStream)

    $fileContent.Headers.ContentType =
        [System.Net.Http.Headers.MediaTypeHeaderValue]::new($contentType)

    $multipartContent.Add(
        $fileContent,
        "file",
        $fileName
    )

    Write-Host "Sending file:"
    Write-Host "  Path:         $ImagePath"
    Write-Host "  File name:    $fileName"
    Write-Host "  Content type: $contentType"
    Write-Host "  Endpoint:     $requestUri"
    Write-Host ""

    $response = $httpClient.PostAsync($requestUri, $multipartContent).GetAwaiter().GetResult()

    $rawResponse = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()

    Write-Host "HTTP status: $([int] $response.StatusCode) $($response.ReasonPhrase)"
    Write-Host ""
    Write-Host "Raw response:"
    Write-Host $rawResponse

    if (-not [string]::IsNullOrWhiteSpace($rawResponse)) {
        try {
            $parsedResponse = $rawResponse | ConvertFrom-Json

            Write-Host ""
            Write-Host "Parsed response:"
            $parsedResponse | Format-List
        }
        catch {
            Write-Warning "The response body is not valid JSON."
        }
    }

    if (-not $response.IsSuccessStatusCode) {
        exit 1
    }
}
finally {
    # Disposing multipartContent also disposes fileContent and fileStream.
    $multipartContent.Dispose()
    $httpClient.Dispose()
}
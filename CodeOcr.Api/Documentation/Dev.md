## Requirements
- Visual Studio 2026
- VS extension: Markdown Editor v2 2.0.405
- Powershell 7.6.3

## How to run

- from visual studio
```text
Click run button
```
- from the command line
```cmd
c:\> dotnet restore
c:\> dotnet build
c:\> dotnet run --project .\CodeOcr.Api
```

## How to test

- Visual studio tests from the command line

```cmd
c:\> dotnet test
```

- Powershell tests (PowershellTest)
    - api/diagnostic check (api-diagnostic-check.ps1)  
      *expected output:*
      ```powershell
      applicationName status  timestampUtc        environment
      --------------- ------  ------------        -----------
      Code OCR        Healthy 19/07/2026 19:55:09 Development
      ```  
    - multipart `POST` request (fail) (api-image-invalid-extension.ps1)  
    - multipart `POST` request (fail) (api-image-validate-1.ps1)  
      `API` correctly rejected the request because PowerShell’s `-Form` parameter sends FileInfo values using `application/octet-stream`  
      *expected output:*
      ```json
      {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Image upload validation failed.","status":400,"detail":"The content
      type 'application/octet-stream' is not supported.","errorCode":"unsupported_content_type"}
      ```  
    - multipart `POST` request (success) (api-image-validate-2.ps1)  
      *expected output:*
      ```json
      Status: 200 OK
      {"fileName":"sample.png","extension":".png","contentType":"image/png","sizeBytes":164497}
      ```
- visual studio .http tests (Tests/HttpTests)
    - api-image-validate-1.http

## Powershell issues
- how to check current ps version
```powershell
PS E:\> $PSVersionTable
```
- how to install powershell (latest version)
```powershell
PS E:\> winget install --id Microsoft.PowerShell --source winget
```  
- how to install powershell (specific version)
```powershell
PS E:\> winget install --id Microsoft.PowerShell --version 7.6.3.0
```  
- how to switch between powershell 5.1 (desktop) and powershell 7.6 (core)
```powershell
PS E:\> pwsh
PS E:\> powershell
```
- how to locate powershell
```powershell
PS E:\> where pwsh
PS E:\> where powershell
```

## Python virtual environment installation
1. check you have correct version of python installed in the system
```powershell
 PS E:\> py -0p
 ```
2. go to directory where you want to install python virtual environment
```powershell
PS E:\Repo\Project19\CodeOcr.OcrService> py -3.13 -m venv .venv
```
3. create requirements.txt & requirements-dev.txt files in main project directory which contains all the dependencies for the project
4. install all dependencies
```powershell
PS E:\Repo\Project19\CodeOcr.OcrService> .\.venv\Scripts\python.exe -m pip install --upgrade pip

PS E:\Repo\Project19\CodeOcr.OcrService> .\.venv\Scripts\python.exe -m pip install -r requirements-dev.txt
```

## Python issues
- how to check exact python version
```powershell
 PS E:\> py -3.13 --version
 ```
- how to check if installed python is 64-bit
```powershell
PS E:\> py -3.13 -c "import platform; print(platform.architecture())"
```
- how to verify installed packages
```powershell
PS E:\Repo\Project19\CodeOcr.OcrService> .\.venv\Scripts\python.exe -m pip list
```

## Patterns
`Result Object / Notification Pattern`  
Wrapping the outcome of an operation (status, errors, data) in a designated object rather than throwing exceptions or returning raw values.

`Dependency Injection`  
The endpoint should not know how physical paths are constructed or how files are written. The endpoint depends on: `IImageFileStorage`. The implementation is: `LocalImageFileStorage`.

`Options Pattern`  
The storage directory should not be hard-coded in the storage service. The `ImageStorage` configuration section is mapped to: `ImageStorageOptions`

## Design patterns
`Centralized Exception Handler`  
`Typed Client Pattern`  
PaddleOcrClient wraps the service-specific use of HttpClient  
`Exception Translation`  
Low-level exceptions such as:
```text
HttpRequestException
OperationCanceledException
JsonException
```
do not communicate what failed from the application’s perspective.
## Best practices
`Design for inheritance or else prohibit it`  
All classes are sealed by default. If a class is designed to be inherited, it should be explicitly marked as `abstract` or `virtual`. If a class is not designed for inheritance, it should be marked as `sealed`.
## To do
- on webpage i want to have icon green when ocr service is working, red when not
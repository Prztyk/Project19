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

## How to initialize database
- open Package Manager Console
- set default project to CodeOcr.Api
- run
```powershell
Add-Migration InitialOcrPersistence -OutputDir Persistence\Migrations
```
- check generated migrations
- remove generated migrations
```powershell
Remove-Migration
```
- apply migrations to database
```powershell
Update-Database
```
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

`Compensating Action` (often called a `Compensating Transaction`)  
Architectural / enterprise integration pattern. When database persistence fails after file creation, the workflow attempts to delete that file.

`Dependency Injection`  
The endpoint should not know how physical paths are constructed or how files are written. The endpoint depends on: `IImageFileStorage`. The implementation is: `LocalImageFileStorage`.

`Options Pattern`  
The storage directory should not be hard-coded in the storage service. The `ImageStorage` configuration section is mapped to: `ImageStorageOptions`

`Repository Pattern`  
IImageOcrRepository separates the workflow from EF Core details.

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
## Best practices
`Design for inheritance or else prohibit it`  
All classes are sealed by default. If a class is designed to be inherited, it should be explicitly marked as `abstract` or `virtual`. If a class is not designed for inheritance, it should be marked as `sealed`.
## To do
- on webpage i want to have icon green when ocr service is working, red when not

## Issues
- can not add migration (Package Manager console)
```log
PM> Add-Migration InitialOcrPersistence -OutputDir Persistence\Migrations
Build started...
Build succeeded.
The running command stopped because the preference variable "ErrorActionPreference" or common parameter is set to Stop: E:\Repo\Project19\CodeOcr.Api\CodeOcr.Api.csproj : warning NU1903: Package 'SQLitePCLRaw.lib.e_sqlite3' 2.1.11 has a known high severity vulnerability, https://github.com/advisories/GHSA-2m69-gcr7-jv3q
```
- check vulnerabilities (Powershell console)
```log
PS E:\Repo\Project19> dotnet list package --vulnerable
Restore succeeded with 2 warning(s) in 0,7s
    E:\Repo\Project19\CodeOcr.Tests\CodeOcr.Tests.csproj : warning NU1903: Package 'SQLitePCLRaw.lib.e_sqlite3' 2.1.11 has a known high severity vulnerability, https://github.com/advisories/GHSA-2m69-gcr7-jv3q
    E:\Repo\Project19\CodeOcr.Api\CodeOcr.Api.csproj : warning NU1903: Package 'SQLitePCLRaw.lib.e_sqlite3' 2.1.11 has a known high severity vulnerability, https://github.com/advisories/GHSA-2m69-gcr7-jv3q

Build succeeded with 2 warning(s) in 0,8s

The following sources were used:
   https://api.nuget.org/v3/index.json
   D:\TMP\NugetPackageRepo

The given project `CodeOcr.Api` has no vulnerable packages given the current sources.
The given project `CodeOcr.Tests` has no vulnerable packages given the current sources.
```
- there is vulnerability and there is no vulnerability in the same time. it means vulnerability is not detected in package but in dependencies, so check vulnerabilities inside dependencies as well
```log
PS E:\Repo\Project19> dotnet list package --vulnerable --include-transitive
Restore succeeded with 2 warning(s) in 0,7s
    E:\Repo\Project19\CodeOcr.Tests\CodeOcr.Tests.csproj : warning NU1903: Package 'SQLitePCLRaw.lib.e_sqlite3' 2.1.11 has a known high severity vulnerability, https://github.com/advisories/GHSA-2m69-gcr7-jv3q
    E:\Repo\Project19\CodeOcr.Api\CodeOcr.Api.csproj : warning NU1903: Package 'SQLitePCLRaw.lib.e_sqlite3' 2.1.11 has a known high severity vulnerability, https://github.com/advisories/GHSA-2m69-gcr7-jv3q

Build succeeded with 2 warning(s) in 0,8s

The following sources were used:
   https://api.nuget.org/v3/index.json
   D:\TMP\NugetPackageRepo

Project `CodeOcr.Api` has the following vulnerable packages
   [net10.0]:
   Transitive Package                Resolved   Severity   Advisory URL
   > SQLitePCLRaw.lib.e_sqlite3      2.1.11     High       https://github.com/advisories/GHSA-2m69-gcr7-jv3q

Project `CodeOcr.Tests` has the following vulnerable packages
   [net10.0]:
   Transitive Package                Resolved   Severity   Advisory URL
   > SQLitePCLRaw.lib.e_sqlite3      2.1.11     High       https://github.com/advisories/GHSA-2m69-gcr7-jv3q
```
- update dependency directly (Powershell console)
```log
PS E:\Repo\Project19> cd .\CodeOcr.Api\
PS E:\Repo\Project19\CodeOcr.Api> dotnet add package SQLitePCLRaw.lib.e_sqlite3 --version 2.1.12
```
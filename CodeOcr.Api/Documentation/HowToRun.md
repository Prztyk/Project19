#### Run app from visual studio

```text
Click run button
```


#### Run app from the command line

```cmd
c:\> dotnet restore
c:\> dotnet build
c:\> dotnet run --project .\CodeOcr.Api
```

#### Run tests from the command line

```cmd
c:\> dotnet test
```

#### Test API through powershell
```powershell
Invoke-RestMethod -Method Get -Uri "https://localhost:7207/api/diagnostics"
```
Wersja języka `C# 12` wprowadziła mechanizm o nazwie `Primary Constructors`. Before `C# 12`, you had to write a lot of boilerplate code to achieve the same result: declaring private fields, creating a constructor, and assigning the parameters to those fields.

With `primary constructors`, the compiler automatically handles that for you. The parameters (applicationOptions, hostEnvironment, logger) are in scope for the entire class body and can be used directly in your methods or to initialize properties.

However, unlike records, primary constructor parameters in a standard class do not automatically become public properties; they act as private fields.

<details>
<summary>Kod dla C# < 12 </summary>

```csharp
public sealed class DiagnosticService : IDiagnosticService
{
    private readonly IOptions<ApplicationOptions> _applicationOptions;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<DiagnosticService> _logger;

    public DiagnosticService(
        IOptions<ApplicationOptions> applicationOptions,
        IHostEnvironment hostEnvironment,
        ILogger<DiagnosticService> logger)
    {
        _applicationOptions = applicationOptions;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }
}
```

</details>

```csharp
public sealed class DiagnosticService(
    IOptions<ApplicationOptions> applicationOptions,
    IHostEnvironment hostEnvironment,
    ILogger<DiagnosticService> logger)
    : IDiagnosticService
{
    // The parameters are immediately available anywhere in the class!
    public void DoSomething()
    {
        logger.LogInformation("Service is running...");
    }
}
```
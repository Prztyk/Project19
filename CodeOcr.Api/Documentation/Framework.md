## Exception handling
Validating with reduced boilerplate code, make your codebase cleaner, and optimize performance.
<details>
<summary>Kod dla C# < 6 </summary>

```csharp
public void ProcessFile(string file)
{
    if (file == null)
    {
        throw new ArgumentNullException(nameof(file));
    }
    
    // Process the file...
}
```

</details>

```csharp
public void ProcessFile(string file)
{
    ArgumentNullException.ThrowIfNull(file);
    
    // Process the file...
}
```

## Primary Constructors
`C# 12` introduced `Primary Constructors`. Before `C# 12`, you had to write a lot of boilerplate code to achieve the same result: declaring private fields, creating a constructor, and assigning the parameters to those fields.

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

<details>
<summary>Kod dla C# < 12 </summary>

```csharp
old code
```

</details>

```csharp
new code
```

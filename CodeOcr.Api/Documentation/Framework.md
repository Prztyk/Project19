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

## Range Operator (..)

<details>
<summary>Kod dla C# < 8 </summary>

```csharp
var numbers = new int[] { 10, 20, 30, 40, 50, 60 };

// Get elements from index 1 taking 3 items (20, 30, 40)
var slice = numbers.Skip(1).Take(3).ToArray();

// Get the last item (60)
var lastItem = numbers[numbers.Length - 1];

// Get a substring (first 3 chars)
var text = "Hello World";
var sub = text.Substring(0, 3); // "Hel"
```

</details>

```csharp
var numbers = new int[] { 10, 20, 30, 40, 50, 60 };

// Get elements from index 1 up to (but excluding) index 4 (20, 30, 40)
var slice = numbers[1..4]; 

// ..3 means "from the start up to index 3".

// 2.. means "from index 2 to the very end".

// .. means "the entire collection" (a full copy).

// Get the last item using the ^ (hat) operator
var lastItem = numbers[^1]; 

// Get a slice of a string
var text = "Hello World";
var sub = text[0..3]; // "Hel"
```

## UTF-8 String Literals

<details>
<summary>Kod dla C# < 11 </summary>

```csharp
// Option 1: Allocates a new byte array at runtime via Encoding
byte[] riffHeader = Encoding.UTF8.GetBytes("RIFF"); 

// Option 2: Verbose, hard-to-read hex/decimal bytes
byte[] riffHeaderBytes = new byte[] { 0x52, 0x49, 0x46, 0x46 };
```

</details>

```csharp
// Compiled directly into the assembly as a UTF-8 byte span!
ReadOnlySpan<byte> riffHeader = "RIFF"u8;
```

## Template

<details>
<summary>Kod dla C# < 11 </summary>

```csharp
old code
```

</details>

```csharp
new code
```

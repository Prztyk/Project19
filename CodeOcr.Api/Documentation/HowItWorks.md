**Metadata validation**

The existing checks still run first:
```text
size → extension → content type
```
This avoids opening the stream for requests that have already failed basic validation.

**Metadata consistency**

The validator maps the extension and content type to an internal enum:
```text
.png       → Png
image/png  → Png
```
If they represent different formats, the request fails with:
```text
file_metadata_mismatch
```
**Signature detection**

The validator reads at most 12 bytes and compares them against known signatures.

It then compares the detected value with the expected format:
```text
extension format == content-type format == detected format
```
Only then is the request accepted.

**Never use the original filename for storage**
The server creates a name similar to: 92b2629d9aa34ac59893eb2f98422af0.png

**Use the detected format for the extension**

**Do not expose the physical path**

**Error handling**  

1. Expected invalid input
    ```text
        → validation result
        → 400 ProblemDetails
    ```
2. Unexpected technical failure
    ```text
        → exception
        → global exception handler
        → 500 ProblemDetails
    ```
3. Include traceId  
Every problem response should include a request identifier:

    "traceId": "0HNF..."

    The same identifier can be found in logs, making it easier to connect a client-visible error with server diagnostics.
4. Do not return exception messages  
The application will log the complete exception but return a generic detail:
    ```text
    The uploaded image could not be stored.
    ```
    It will not return information such as:
    ```text
    Access denied to E:\Repo\Project19\App_Data\Images
    ```
5. Exception middleware  
These lines add middleware:
    ```text
    app.UseExceptionHandler();
    app.UseStatusCodePages();
    ```
    Middleware is code that surrounds endpoint execution.

    ```text
    Request
      → exception middleware
        → status-code middleware
          → endpoint
    ```
    When the endpoint or one of its services throws an exception, control returns to the exception middleware.

    The middleware invokes registered implementations of:
    ```text
    IExceptionHandler
    ```
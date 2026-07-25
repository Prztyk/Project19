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
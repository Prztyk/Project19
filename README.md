# Project19 — Code OCR

Project19 is a local application for extracting source code from images.

The application will eventually:

1. accept images containing source code,
2. preprocess images,
3. send images to a local PaddleOCR service,
4. preserve raw OCR results,
5. detect the programming or data language,
6. validate recognized code,
7. allow manual corrections,
8. store results,
9. support later searches.

## Current status

## Current status

The API currently provides:

- a diagnostic endpoint,
- single-image multipart upload,
- configurable file-size validation,
- filename-extension validation,
- declared content-type validation,
- PNG, JPEG, and WebP signature detection,
- consistency checks between extension, content type, and file bytes.

Uploaded files are not saved or processed with OCR yet.

## Planned next step

Introduce temporary local file storage using application-generated filenames,
without adding database persistence or OCR processing.

## Requirements

- Visual Studio 2026 Community
- ASP.NET and web development workload
- .NET 10 SDK

## Solution structure

```text
Project19
├── CodeOcr.Api
└── CodeOcr.Tests
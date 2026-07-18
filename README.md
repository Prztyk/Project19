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

Step 1 provides:

- an ASP.NET Core API,
- basic application configuration,
- dependency injection,
- a diagnostic endpoint,
- an integration test.

The project does not yet provide image upload or OCR functionality.

## Requirements

- Visual Studio 2026 Community
- ASP.NET and web development workload
- .NET 10 SDK

## Solution structure

```text
Project19
├── CodeOcr.Api
└── CodeOcr.Tests
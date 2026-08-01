# CodeOcr.OcrService

Local Python HTTP service used by Project19 for OCR.

The current implementation is a mock service. It accepts an uploaded image but
does not execute PaddleOCR yet.

## Requirements
- Python 3.13, 64-bit

## Create the virtual environment
From this directory:

```powershell
py -3.13 -m venv .venv
```

## Install dependencies
From this directory:

```powershell
.\.venv\Scripts\python.exe -m pip install --upgrade pip
.\.venv\Scripts\python.exe -m pip install -r requirements-dev.txt
```
## Run tests
```powershell
.\.venv\Scripts\python.exe -m pytest
```
more detailed output
```powershell
.\.venv\Scripts\python.exe -m pytest--verbose
```
## Start the service
```powershell
.\.venv\Scripts\python.exe -m uvicorn app.main:app --host 127.0.0.1 --port 8000 --reload
```
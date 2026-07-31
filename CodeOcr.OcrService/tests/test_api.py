from fastapi.testclient import TestClient

from app.main import (
    MAXIMUM_FILE_SIZE_BYTES,
    app,
)


client = TestClient(app)


def test_health_returns_mock_engine_status() -> None:
    response = client.get("/health")

    assert response.status_code == 200
    assert response.json() == {
        "status": "healthy",
        "engine": "mock",
    }


def test_recognize_returns_dotnet_compatible_contract() -> None:
    image_content = bytes(
        [
            0x89,
            0x50,
            0x4E,
            0x47,
            0x0D,
            0x0A,
            0x1A,
            0x0A,
        ]
    )

    response = client.post(
        "/api/ocr",
        files={
            "file": (
                "sample.png",
                image_content,
                "image/png",
            ),
        },
    )

    assert response.status_code == 200
    assert response.json() == {
        "lines": [
            {
                "text": "public class Customer",
                "confidence": 0.97,
            },
        ],
        "fullText": "public class Customer",
        "processingTimeMs": 0,
    }


def test_recognize_without_file_returns_validation_error() -> None:
    response = client.post("/api/ocr")

    assert response.status_code == 422


def test_recognize_with_empty_file_returns_bad_request() -> None:
    response = client.post(
        "/api/ocr",
        files={
            "file": (
                "empty.png",
                b"",
                "image/png",
            ),
        },
    )

    assert response.status_code == 400
    assert response.json() == {
        "detail": "The uploaded file is empty.",
    }


def test_recognize_with_unsupported_content_type_returns_415() -> None:
    response = client.post(
        "/api/ocr",
        files={
            "file": (
                "sample.txt",
                b"not an image",
                "text/plain",
            ),
        },
    )

    assert response.status_code == 415
    assert response.json() == {
        "detail": (
            "The content type 'text/plain' "
            "is not supported."
        ),
    }


def test_recognize_with_oversized_file_returns_413() -> None:
    oversized_content = (
        b"x" * (MAXIMUM_FILE_SIZE_BYTES + 1)
    )

    response = client.post(
        "/api/ocr",
        files={
            "file": (
                "large.png",
                oversized_content,
                "image/png",
            ),
        },
    )

    assert response.status_code == 413
    assert response.json() == {
        "detail": (
            "The uploaded file exceeds the "
            "maximum allowed size."
        ),
    }
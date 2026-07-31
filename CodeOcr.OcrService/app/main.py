from typing import Annotated

from fastapi import (
    FastAPI,
    File,
    HTTPException,
    UploadFile,
    status,
)

from app.contracts import (
    HealthResponse,
    OcrLine,
    OcrResponse,
)


MAXIMUM_FILE_SIZE_BYTES = 10 * 1024 * 1024

ALLOWED_CONTENT_TYPES = {
    "image/jpeg",
    "image/png",
    "image/webp",
}

MOCK_RECOGNIZED_TEXT = "public class Customer"


app = FastAPI(
    title="Code OCR PaddleOCR Service",
    description=(
        "Local technical service used by Project19 "
        "for source-code OCR."
    ),
    version="0.1.0",
)


@app.get(
    "/health",
    response_model=HealthResponse,
)
async def get_health() -> HealthResponse:
    return HealthResponse(
        status="healthy",
        engine="mock",
    )


@app.post(
    "/api/ocr",
    response_model=OcrResponse,
    response_model_by_alias=True,
    responses={
        status.HTTP_400_BAD_REQUEST: {
            "description": "The uploaded file is empty.",
        },
        status.HTTP_413_CONTENT_TOO_LARGE: {
            "description": "The uploaded file is too large.",
        },
        status.HTTP_415_UNSUPPORTED_MEDIA_TYPE: {
            "description": "The content type is not supported.",
        },
    },
)
async def recognize_code(
    file: Annotated[
        UploadFile,
        File(
            description=(
                "A PNG, JPEG, or WebP image containing "
                "source code."
            ),
        ),
    ],
) -> OcrResponse:
    try:
        content_type = (
            file.content_type or ""
        ).lower()

        if content_type not in ALLOWED_CONTENT_TYPES:
            raise HTTPException(
                status_code=(
                    status.HTTP_415_UNSUPPORTED_MEDIA_TYPE
                ),
                detail=(
                    f"The content type '{content_type}' "
                    "is not supported."
                ),
            )

        image_content = await file.read(
            MAXIMUM_FILE_SIZE_BYTES + 1
        )

        if not image_content:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="The uploaded file is empty.",
            )

        if len(image_content) > MAXIMUM_FILE_SIZE_BYTES:
            raise HTTPException(
                status_code=(
                    status.HTTP_413_CONTENT_TOO_LARGE
                ),
                detail=(
                    "The uploaded file exceeds the "
                    "maximum allowed size."
                ),
            )

        return OcrResponse(
            lines=[
                OcrLine(
                    text=MOCK_RECOGNIZED_TEXT,
                    confidence=0.97,
                ),
            ],
            full_text=MOCK_RECOGNIZED_TEXT,
            processing_time_ms=0,
        )
    finally:
        await file.close()
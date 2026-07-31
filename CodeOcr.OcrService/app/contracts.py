from pydantic import BaseModel, ConfigDict, Field


class ApiModel(BaseModel):
    model_config = ConfigDict(
        extra="forbid",
        populate_by_name=True,
    )


class HealthResponse(ApiModel):
    status: str
    engine: str


class OcrLine(ApiModel):
    text: str
    confidence: float | None = Field(
        default=None,
        ge=0.0,
        le=1.0,
    )


class OcrResponse(ApiModel):
    lines: list[OcrLine]

    full_text: str = Field(
        alias="fullText",
    )

    processing_time_ms: int = Field(
        alias="processingTimeMs",
        ge=0,
    )
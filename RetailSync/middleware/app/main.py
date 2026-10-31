"""FastAPI application entry point."""

from fastapi import FastAPI

from app.api.admin import router as admin_router


app = FastAPI(
    title="RetailSync Middleware",
    version="0.1.0",
)

app.include_router(admin_router)


@app.get("/health", tags=["Health"])
def health_check() -> dict[str, str]:
    """Return the service health status."""
    return {"status": "ok"}

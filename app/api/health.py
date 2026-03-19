"""Health check endpoints."""

from datetime import datetime

from fastapi import APIRouter

router = APIRouter(tags=["health"])


@router.get("/health")
async def health_check() -> dict:
    """Verify the server is running and responsive."""
    return {
        "status": "healthy",
        "timestamp": datetime.utcnow().isoformat() + "Z",
        "service": "table-tennis-referee",
    }

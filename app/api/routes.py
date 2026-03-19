"""API route aggregation."""

from fastapi import APIRouter

from app.api import health

api_router = APIRouter(prefix="/api")

# Include health router under /api
api_router.include_router(health.router)

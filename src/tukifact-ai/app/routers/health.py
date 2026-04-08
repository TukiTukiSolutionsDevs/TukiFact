from fastapi import APIRouter
from app.nats_bridge import nats_manager

router = APIRouter()


@router.get("/health")
async def health():
    return {
        "status": "healthy",
        "service": "TukiFact AI",
        "version": "0.1.0",
        "nats_connected": nats_manager.is_connected,
    }


@router.get("/health/ready")
async def ready():
    return {"ready": nats_manager.is_connected}

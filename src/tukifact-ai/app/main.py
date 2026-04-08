"""TukiFact AI — Agentes de IA para Facturación Electrónica."""

from contextlib import asynccontextmanager
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.config import settings
from app.nats_bridge import nats_manager
from app.routers import agents, health


@asynccontextmanager
async def lifespan(app: FastAPI):
    # Startup: connect to NATS
    await nats_manager.connect()
    yield
    # Shutdown: disconnect
    await nats_manager.disconnect()


app = FastAPI(
    title="TukiFact AI",
    description="Agentes de IA para validación, clasificación y extracción de comprobantes electrónicos",
    version="0.1.0",
    lifespan=lifespan,
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(health.router, tags=["Health"])
app.include_router(agents.router, prefix="/v1/ai", tags=["AI Agents"])

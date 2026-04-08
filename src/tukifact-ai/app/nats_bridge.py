"""NATS JetStream bridge for .NET <-> Python communication."""

import json
import nats
from nats.aio.client import Client as NatsClient
from nats.js.api import StreamConfig

from app.config import settings


class NatsManager:
    def __init__(self):
        self._nc: NatsClient | None = None
        self._js = None

    async def connect(self):
        self._nc = await nats.connect(settings.nats_url)
        self._js = self._nc.jetstream()

        # Create streams for AI events
        try:
            await self._js.add_stream(
                StreamConfig(
                    name="TUKIFACT_AI",
                    subjects=["ai.validate.*", "ai.classify.*", "ai.extract.*", "ai.result.*"],
                    retention="limits",
                    max_msgs=10000,
                )
            )
        except Exception:
            pass  # Stream may already exist

    async def disconnect(self):
        if self._nc:
            await self._nc.drain()

    async def publish(self, subject: str, data: dict):
        if self._js:
            await self._js.publish(subject, json.dumps(data).encode())

    async def subscribe(self, subject: str, handler):
        if self._js:
            return await self._js.subscribe(subject, cb=handler)

    @property
    def is_connected(self) -> bool:
        return self._nc is not None and self._nc.is_connected


nats_manager = NatsManager()

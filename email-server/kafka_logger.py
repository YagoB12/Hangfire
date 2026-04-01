import json
import os
from datetime import datetime, timezone
from kafka import KafkaProducer

class KafkaLogger:
    def __init__(self):
        self.topic = os.getenv("KAFKA_TOPIC", "logs-email")
        servers = os.getenv("KAFKA_BOOTSTRAP_SERVERS")
        self.service = os.getenv("SERVICE_NAME", "EmailServer")
        self._producer = None
        if servers:
            self._producer = KafkaProducer(
                bootstrap_servers=[s.strip() for s in servers.split(",")],
                value_serializer=lambda v: json.dumps(v).encode("utf-8"),
                linger_ms=10,
            )

    def log(self, level, message, **meta):
        payload = {
            "ts": datetime.now(timezone.utc).isoformat(),
            "service": self.service,
            "level": level,
            "message": message,
            **meta,
        }
        if self._producer:
            self._producer.send(self.topic, payload)
        else:
            print("[KAFKA-FAKE]", payload)

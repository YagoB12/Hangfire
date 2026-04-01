from kafka import KafkaProducer
import json
import logging
import time

class KafkaLogger:
    def __init__(self, topic: str = "logs-storage", brokers: list[str] = ["localhost:9092"]):
        self.topic = topic
        self.producer = None
        for i in range(5):
            try:
                self.producer = KafkaProducer(
                    bootstrap_servers=brokers,
                    value_serializer=lambda v: json.dumps(v).encode("utf-8"),
                )
                logging.info(f"Conectado a Kafka en {brokers}")
                break
            except Exception as e:
                logging.warning(f"Intento {i+1}/5: Error al conectar a Kafka → {e}")
                time.sleep(3)
        if not self.producer:
            logging.error("No se pudo conectar a Kafka después de varios intentos")

    def send_log(self, message: dict):
        if not self.producer:
            logging.warning(" Kafka no inicializado. No se puede enviar log.")
            return
        try:
            self.producer.send(self.topic, message)
            self.producer.flush()
            logging.info(f" Log enviado a Kafka → {self.topic}")
        except Exception as e:
            logging.error(f" Error enviando log a Kafka: {e}")

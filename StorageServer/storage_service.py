from kafka_producer import KafkaLogger
import os
import datetime
import logging
import json

class StorageService:
    """Servicio encargado de guardar archivos y enviar logs a Kafka."""

    def __init__(self):
        self.logger = KafkaLogger(topic="logs-storage")
        self.base_path = os.path.join(os.getcwd(), "storage_files")
        os.makedirs(self.base_path, exist_ok=True)
        self.registry_file = os.path.join(self.base_path, "file_registry.json")

        # Crear el archivo de registro si no existe
        if not os.path.exists(self.registry_file):
            with open(self.registry_file, "w", encoding="utf-8") as f:
                json.dump({}, f)

        logging.info(f" Carpeta base de almacenamiento: {self.base_path}")

    def _load_registry(self):
        """Lee el archivo JSON de registro."""
        with open(self.registry_file, "r", encoding="utf-8") as f:
            return json.load(f)

    def _save_registry(self, data):
        """Guarda el archivo JSON de registro."""
        with open(self.registry_file, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=4)

    def save_file(self, filename: str, content: bytes, correlation_id: str | None = None):
        """Guarda un archivo localmente en una carpeta por fecha y registra su correlación."""
        try:
            today = datetime.date.today().strftime("%Y-%m-%d")
            folder_path = os.path.join(self.base_path, today)
            os.makedirs(folder_path, exist_ok=True)

            safe_name = f"{today}_{filename}"
            full_path = os.path.join(folder_path, safe_name)

            # Guardar el archivo físico
            with open(full_path, "wb") as f:
                f.write(content)

            # Registrar la correlación
            registry = self._load_registry()
            if correlation_id:
                registry[correlation_id] = full_path
                self._save_registry(registry)

            # Enviar log a Kafka
             # self.logger.send_log({
               #   "event": "file_saved",
                 # "filename": safe_name,
                 # "path": full_path,
                 # "correlationId": correlation_id or "N/A",
                 # "timestamp": datetime.datetime.now().isoformat()
             # })

            logging.info(f"  Archivo guardado correctamente: {full_path}")
            return full_path

        except Exception as e:
            logging.error(f"Error al guardar archivo: {e}")
            self.logger.send_log({
                "event": "error",
                "error": str(e)
            })
            raise

    def find_file_by_correlation(self, correlation_id: str) -> str | None:
        """Busca un archivo por su correlationId."""
        registry = self._load_registry()
        return registry.get(correlation_id)

from fastapi import FastAPI, UploadFile, File, HTTPException, Query
from fastapi.responses import FileResponse
from storage_service import StorageService
import logging
import os
import requests  # Para comunicar con Hangfire

app = FastAPI(title="StorageServer", version="3.1")
service = StorageService()

# === Configuración de logging ===
logging.basicConfig(
    level=logging.INFO,
    format="[%(asctime)s] %(levelname)s - %(message)s"
)

# === Endpoint del Hangfire ===
HANGFIRE_NOTIFY_URL = "http://localhost:5296/api/notifications"  #  HTTP

@app.get("/")
def root():
    return {"message": "StorageServer en funcionamiento"}

# === Subida de archivos y notificación a Hangfire ===
@app.post("/upload")
async def upload_file(file: UploadFile = File(...), correlationId: str | None = Query(None)):
    if not correlationId:
        raise HTTPException(status_code=400, detail="Debe incluir un correlationId")

    # Guardar físicamente el archivo
    content = await file.read()
    saved_path = service.save_file(file.filename, content, correlation_id=correlationId)
    logging.info(f" Archivo guardado en: {saved_path}")

    # Notificar a Hangfire para agendar el job de notificación
    try:
        payload = {
            "CorrelationId": correlationId,
            "DelaySeconds": 30  # segundos de espera antes de enviar a Discord
        }
        logging.info(f" Enviando notificación a Hangfire → {HANGFIRE_NOTIFY_URL}")
        res = requests.post(HANGFIRE_NOTIFY_URL, json=payload, timeout=5, verify=False)

        if res.status_code == 200:
            logging.info(f" Notificación registrada en Hangfire para CID={correlationId}")
        else:
            logging.warning(f" Hangfire devolvió {res.status_code}: {res.text}")
    except Exception as e:
        logging.error(f" Error notificando a Hangfire: {e}")

    return {"message": "Archivo guardado exitosamente", "path": saved_path}

# === Recibir logs desde PdfServer ===
@app.post("/logs")
async def receive_log(log: dict):
    """Recibe logs JSON desde PdfServer."""
    try:
        service.logger.send_log(log)
        logging.info(f" Log recibido y enviado a Kafka: {log}")
        return {"status": "ok", "received": log}
    except Exception as e:
        logging.error(f" Error procesando log: {e}")
        return {"status": "error", "detail": str(e)}

# === Descargar archivo por nombre ===
@app.get("/download/{filename}")
def download_file(filename: str):
    """Permite descargar un archivo PDF almacenado."""
    for root, _, files in os.walk(service.base_path):
        if filename in files:
            file_path = os.path.join(root, filename)
            logging.info(f" Descargando archivo: {file_path}")
            return FileResponse(file_path, media_type="application/pdf", filename=filename)

    raise HTTPException(status_code=404, detail="Archivo no encontrado")

# === Descargar archivo por correlationId ===
@app.get("/download/by-correlation/{cid}")
def download_by_correlation(cid: str):
    """Descarga un archivo asociado a un correlationId."""
    file_path = service.find_file_by_correlation(cid)
    if not file_path or not os.path.exists(file_path):
        raise HTTPException(status_code=404, detail=f"No se encontró archivo para CorrelationId={cid}")

    logging.info(f"Descargando por CorrelationId {cid}: {file_path}")
    filename = os.path.basename(file_path)
    return FileResponse(file_path, media_type="application/pdf", filename=filename)

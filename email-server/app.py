import os
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, EmailStr
from dotenv import load_dotenv

from email_service import EmailService
from kafka_logger import KafkaLogger

load_dotenv()
app = FastAPI(title="Email Server - FastAPI")

email_service = EmailService()
klogger = KafkaLogger()

class EmailSendRequest(BaseModel):
    correlationId: str
    to: EmailStr
    subject: str | None = None
    message: str | None = None
    filename: str | None = None   # opcional para sobreescribir nombre

@app.post("/api/email/send")
async def send_email(req: EmailSendRequest):
    cid = req.correlationId
    klogger.log("INFO", "Solicitan envío de email", correlationId=cid, to=req.to)

    try:
        fname, pdf = await email_service.fetch_pdf(cid)
        if req.filename:
            fname = req.filename

        body = req.message or f"Adjunto el reporte generado.\nCorrelation ID: {cid}"
        msg = email_service.build_message(
            to=req.to,
            subject=req.subject or f"Reporte {cid}",
            body=body,
            filename=fname,
            pdf_bytes=pdf
        )
        email_service.send_email(msg)

        klogger.log("INFO", "Email enviado OK", correlationId=cid, to=req.to, filename=fname, bytes=len(pdf))
        return {"success": True, "correlationId": cid, "to": req.to, "filename": fname}
    except Exception as ex:
        klogger.log("ERROR", "Fallo enviando email", correlationId=cid, to=req.to, error=str(ex))
        raise HTTPException(status_code=500, detail=f"Error enviando email: {ex}")

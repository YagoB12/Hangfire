import os
import re
import smtplib
from email.message import EmailMessage
from datetime import datetime
import httpx

class EmailService:
    def __init__(self):
        self.storage_base = (
            os.getenv("STORAGE_SERVER_URL")
            or os.getenv("STORAGE_BASE_URL")
            or "http://localhost:8001"
        )
        self.smtp_host = os.getenv("SMTP_HOST", "localhost")
        self.smtp_port = int(os.getenv("SMTP_PORT", "1025"))
        self.smtp_tls  = os.getenv("SMTP_TLS", "false").lower() == "true"
        self.smtp_user = os.getenv("SMTP_USER", "")
        self.smtp_pass = os.getenv("SMTP_PASS", "")
        self.smtp_from = os.getenv("SMTP_FROM", "noreply@example.com")

    async def fetch_pdf(self, correlation_id: str, timeout_sec: int = 15):
        url = f"{self.storage_base}/download/by-correlation/{correlation_id}"
        async with httpx.AsyncClient(timeout=timeout_sec, follow_redirects=True) as client:
            resp = await client.get(url)
            if resp.status_code != 200:
                raise RuntimeError(f"Storage respondió {resp.status_code}")
            content = resp.content
            cd = resp.headers.get("content-disposition", "")
            m = re.search(r'filename="?([^"]+)"?', cd)
            filename = m.group(1) if m else f"reporte_{correlation_id}.pdf"
            return filename, content

    def build_message(self, to: str, subject: str, body: str, filename: str, pdf_bytes: bytes):
        msg = EmailMessage()
        msg["From"] = self.smtp_from
        msg["To"] = to
        msg["Subject"] = subject or "Reporte generado"
        msg["Date"] = datetime.now().strftime("%a, %d %b %Y %H:%M:%S")
        msg.set_content(body)
        msg.add_attachment(
            pdf_bytes,
            maintype="application",
            subtype="pdf",
            filename=filename,
        )
        return msg

    def send_email(self, msg: EmailMessage):
        if self.smtp_tls:
            with smtplib.SMTP(self.smtp_host, self.smtp_port) as s:
                s.starttls()
                if self.smtp_user:
                    s.login(self.smtp_user, self.smtp_pass)
                s.send_message(msg)
        else:
            with smtplib.SMTP(self.smtp_host, self.smtp_port) as s:
                if self.smtp_user:
                    s.login(self.smtp_user, self.smtp_pass)
                s.send_message(msg)

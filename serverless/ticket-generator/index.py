import os
import json
import io
import requests
import boto3
import qrcode
from reportlab.pdfgen import canvas
from reportlab.lib.pagesizes import A4
from reportlab.lib.utils import ImageReader

S3_ENDPOINT = "https://storage.yandexcloud.net"

def handler(event, context):
    """
    Генерирует PDF-талон: берёт данные из .NET, создаёт PDF с QR,
    кладёт в Object Storage и возвращает 302 на pre-signed URL
    """
    
    # Достаём appointmentId из path parameters
    appt_id = None
    if isinstance(event, dict):
        appt_id = (event.get("pathParameters") or {}).get("appointmentId")
    
    if not appt_id:
        return {
            "statusCode": 400,
            "body": json.dumps({"error": "missing appointmentId"})
        }
    
    app_url = os.environ.get("APP_URL")
    secret = os.environ.get("TICKET_INTERNAL_SECRET")
    bucket = os.environ["TICKETS_BUCKET"]
    
    if not app_url or not secret:
        return {
            "statusCode": 500,
            "body": json.dumps({"error": "APP_URL or SECRET not configured"})
        }
    
    # 1. Берём данные талона из .NET internal API
    try:
        r = requests.get(
            f"{app_url.rstrip('/')}/internal/ticket/{appt_id}",
            headers={"X-Internal-Secret": secret},
            timeout=10
        )
    except Exception as e:
        return {
            "statusCode": 502,
            "body": json.dumps({"error": f"failed to reach app: {str(e)}"})
        }
    
    if r.status_code == 404:
        return {"statusCode": 404, "body": "appointment not found"}
    if r.status_code != 200:
        return {
            "statusCode": 502,
            "body": json.dumps({"error": f"app returned {r.status_code}"})
        }
    
    ticket = r.json()
    
    # 2. Генерим PDF
    pdf_bytes = make_pdf(ticket)
    
    # 3. Пишем в Object Storage
    s3 = boto3.client(
        "s3",
        endpoint_url=S3_ENDPOINT,
        aws_access_key_id=os.environ["AWS_ACCESS_KEY_ID"],
        aws_secret_access_key=os.environ["AWS_SECRET_ACCESS_KEY"],
        region_name="ru-central1"
    )
    
    key = f"tickets/{appt_id}.pdf"
    s3.put_object(
        Bucket=bucket,
        Key=key,
        Body=pdf_bytes,
        ContentType="application/pdf"
    )
    
    # 4. Генерим pre-signed URL
    url = s3.generate_presigned_url(
        ClientMethod="get_object",
        Params={"Bucket": bucket, "Key": key},
        ExpiresIn=300
    )
    
    # 5. Возвращаем 302
    return {
        "statusCode": 302,
        "headers": {"Location": url},
        "body": ""
    }

def make_pdf(ticket: dict) -> bytes:
    """Создаёт PDF с данными талона"""
    buf = io.BytesIO()
    c = canvas.Canvas(buf, pagesize=A4)
    w, h = A4
    
    cabinet = ticket.get("cabinet", "К-000")
    appt_id = ticket["id"]
    fullname = ticket.get("fullname", "")
    animal = ticket.get("animalType", "")
    nickname = ticket.get("nickname", "")
    date_utc = ticket.get("dateUtc", "")
    
    # Заголовок
    c.setFont("Helvetica-Bold", 20)
    c.drawString(50, h - 80, "Талон на приём")
    
    # Кабинет крупно
    c.setFont("Helvetica-Bold", 32)
    c.drawString(50, h - 120, cabinet)
    
    # QR-код
    qr_payload = ticket.get("qrPayload", appt_id)
    qr_img = qrcode.make(qr_payload)
    qr_buf = io.BytesIO()
    qr_img.save(qr_buf, format="PNG")
    qr_buf.seek(0)
    c.drawImage(ImageReader(qr_buf), 50, h - 320, width=180, height=180)
    
    # Детали записи
    c.setFont("Helvetica", 12)
    y = h - 360
    c.drawString(50, y, f"Запись: {appt_id}")
    y -= 20
    c.drawString(50, y, f"Питомец: {animal} «{nickname}»")
    y -= 20
    c.drawString(50, y, f"Владелец: {fullname}")
    y -= 20
    c.drawString(50, y, f"Дата (UTC): {date_utc}")
    
    c.showPage()
    c.save()
    return buf.getvalue()

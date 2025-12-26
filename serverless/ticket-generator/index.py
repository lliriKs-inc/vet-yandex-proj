import os
import json
import io
import boto3
from datetime import datetime
from reportlab.pdfgen import canvas
from reportlab.lib.pagesizes import A4

S3_ENDPOINT = "https://storage.yandexcloud.net"

def handler(event, context):
    """
    Пока без обращения к .NET — просто генерим test PDF,
    кладём в Object Storage и возвращаем 302 на pre-signed URL
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
    
    bucket = os.environ["TICKETS_BUCKET"]
    
    # Генерим простой PDF
    pdf_bytes = make_test_pdf(appt_id)
    
    # Пишем в Object Storage
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
    
    # Генерим pre-signed URL (действует 5 минут)
    url = s3.generate_presigned_url(
        ClientMethod="get_object",
        Params={"Bucket": bucket, "Key": key},
        ExpiresIn=300
    )
    
    # Возвращаем 302 redirect
    return {
        "statusCode": 302,
        "headers": {"Location": url},
        "body": ""
    }

def make_test_pdf(appt_id):
    """Создаёт простой PDF для теста"""
    buf = io.BytesIO()
    c = canvas.Canvas(buf, pagesize=A4)
    w, h = A4
    
    c.setFont("Helvetica-Bold", 20)
    c.drawString(50, h - 100, "Тестовый талон")
    
    c.setFont("Helvetica", 14)
    c.drawString(50, h - 140, f"ID записи: {appt_id}")
    c.drawString(50, h - 160, f"Сгенерирован: {datetime.utcnow().isoformat()}")
    
    c.showPage()
    c.save()
    return buf.getvalue()

# Gunakan Python versi ringan
FROM python:3.9-slim

WORKDIR /app

# Copy requirements dan install
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy source code
COPY app.py .

# Expose port Flask
EXPOSE 5000

CMD ["python", "app.py"]
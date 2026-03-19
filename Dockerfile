# Table Tennis Referee API - Production Dockerfile
# Optimized for Vultr deployment

FROM python:3.11-slim

# Prevent Python from writing pyc files and buffering stdout/stderr
ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1

WORKDIR /app

# Install dependencies and curl for healthcheck
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy application code
COPY . .

# Non-root user for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser:appuser /app
USER appuser

# Expose port
EXPOSE 8000

# Run with Uvicorn - workers for production load
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]

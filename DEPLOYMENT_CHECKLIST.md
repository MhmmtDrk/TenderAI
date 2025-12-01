# ✅ TenderAI - Production Deployment Checklist

## 🎯 Deployment Öncesi Kontrol Listesi

### 1. Kod Kalitesi

- [x] Solution başarıyla build oluyor
- [x] Migration'lar oluşturuldu
- [ ] Unit testler yazıldı
- [ ] Integration testler çalıştırıldı
- [ ] Code review yapıldı

### 2. Güvenlik

- [ ] **OpenAI API Key** environment variable olarak saklanıyor (`.env` dosyası)
- [ ] **PostgreSQL şifresi** güvenli (en az 16 karakter, özel karakterler)
- [ ] **Connection strings** secrets manager'da (Azure Key Vault / AWS Secrets Manager)
- [ ] HTTPS zorunlu (production'da)
- [ ] CORS ayarları yapılandırıldı
- [ ] Authentication/Authorization eklendi (gelecek)

### 3. Veritabanı

- [ ] Production PostgreSQL instance hazır
- [ ] Backup stratejisi belirlendi (otomatik daily backup)
- [ ] Migration'lar production'a uygulandı
- [ ] Index'ler oluşturuldu (performans için)
- [ ] Connection pool ayarları yapıldı

### 4. Altyapı

- [ ] Docker images build edildi
- [ ] Docker Compose production override hazır
- [ ] Server kapasitesi yeterli (RAM: 4GB+, CPU: 2+ cores)
- [ ] Disk alanı (min. 50GB)
- [ ] Log rotation kuruldu

### 5. Monitoring & Logging

- [ ] Application Insights / CloudWatch entegre edildi
- [ ] Error tracking (Sentry / Raygun)
- [ ] Health check endpoints eklendi (`/health`)
- [ ] Uptime monitoring (UptimeRobot / Pingdom)

---

## 🚀 Production Deployment Adımları

### Azure App Service Deployment

#### 1. Azure Resources Oluştur

```bash
# Azure CLI ile
az group create --name tenderai-rg --location westeurope

az postgres flexible-server create \
  --resource-group tenderai-rg \
  --name tenderai-db \
  --location westeurope \
  --admin-user tenderadmin \
  --admin-password 'YourSecurePassword123!' \
  --sku-name Standard_B1ms

az webapp create \
  --resource-group tenderai-rg \
  --plan tenderai-plan \
  --name tenderai-web \
  --runtime "DOTNET|8.0"
```

#### 2. Connection String Ayarla

```bash
az webapp config connection-string set \
  --resource-group tenderai-rg \
  --name tenderai-web \
  --connection-string-type PostgreSQL \
  --settings DefaultConnection="Host=tenderai-db.postgres.database.azure.com;..."
```

#### 3. Deploy Et

```bash
# Publish
dotnet publish -c Release -o ./publish

# ZIP oluştur
cd publish
zip -r ../deploy.zip .

# Azure'a yükle
az webapp deployment source config-zip \
  --resource-group tenderai-rg \
  --name tenderai-web \
  --src deploy.zip
```

---

### Docker Deployment (Generic)

#### 1. Production Docker Compose

**docker-compose.prod.yml** oluşturun:

```yaml
version: '3.8'

services:
  web:
    image: tenderai-web:latest
    restart: always
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
    ports:
      - "80:8080"
    depends_on:
      - postgres
    networks:
      - tenderai-network

  postgres:
    image: postgres:16-alpine
    restart: always
    environment:
      POSTGRES_PASSWORD_FILE: /run/secrets/db_password
    secrets:
      - db_password
    volumes:
      - postgres_data_prod:/var/lib/postgresql/data
    networks:
      - tenderai-network

secrets:
  db_password:
    file: ./secrets/db_password.txt

volumes:
  postgres_data_prod:

networks:
  tenderai-network:
    driver: bridge
```

#### 2. Build & Deploy

```bash
# Docker image build
docker build -t tenderai-web:latest -f TenderAI.Web/Dockerfile .

# Production'da çalıştır
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Migration'ları çalıştır
docker-compose exec web dotnet ef database update
```

---

### AWS EC2 Deployment

#### 1. EC2 Instance Hazırla

```bash
# SSH ile bağlan
ssh -i your-key.pem ec2-user@your-instance-ip

# Docker kur
sudo yum update -y
sudo yum install docker -y
sudo systemctl start docker
sudo usermod -a -G docker ec2-user

# Docker Compose kur
sudo curl -L "https://github.com/docker/compose/releases/download/v2.20.0/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose
```

#### 2. Projeyi Deploy Et

```bash
# Git clone
git clone https://github.com/your-repo/TenderAI.git
cd TenderAI

# Environment variables
cp .env.example .env
nano .env  # API keys ekle

# Başlat
docker-compose up -d

# Migration
docker-compose exec web dotnet ef database update
```

#### 3. Nginx Reverse Proxy (Opsiyonel)

```nginx
server {
    listen 80;
    server_name tenderai.yourdomain.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

---

## 🔐 Environment Variables (Production)

**ZORUNLU:**
```env
OPENAI_API_KEY=sk-proj-your-real-key
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Host=your-db-host;...
```

**Opsiyonel:**
```env
TENDERAI_EKAP_API_URL=http://ihale-mcp:8000
TENDERAI_DATA_SYNC_INTERVAL_HOURS=6
REDIS_CONNECTION=your-redis-host:6379
ELASTICSEARCH_URL=http://your-es-host:9200
```

---

## 📊 Health Check

Production'da health check endpoint ekleyin:

**Program.cs:**
```csharp
app.MapHealthChecks("/health");
```

Test:
```bash
curl http://your-domain/health
```

---

## 🔄 CI/CD Pipeline (GitHub Actions)

**.github/workflows/deploy.yml:**

```yaml
name: Deploy to Production

on:
  push:
    branches: [ main ]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x

    - name: Build
      run: dotnet build --configuration Release

    - name: Test
      run: dotnet test

    - name: Publish
      run: dotnet publish -c Release -o ./publish

    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: tenderai-web
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

---

## ⚡ Performance Tuning

### 1. Database Optimization

```sql
-- Index'ler ekle
CREATE INDEX idx_tenders_biddeadline ON "Tenders"("BidDeadline");
CREATE INDEX idx_tenders_status ON "Tenders"("Status");
CREATE INDEX idx_tenders_province ON "Tenders"("Province");
```

### 2. Response Caching

**Program.cs:**
```csharp
builder.Services.AddResponseCaching();
app.UseResponseCaching();
```

### 3. Redis Caching

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetConnectionString("Redis");
});
```

---

## 📈 Scaling Strategy

### Horizontal Scaling

```bash
# Docker Swarm
docker swarm init
docker stack deploy -c docker-compose.yml tenderai

# Kubernetes (gelecek)
kubectl apply -f k8s/
```

### Vertical Scaling

- **Web**: 2GB RAM → 4GB RAM
- **Database**: Standard_B1ms → Standard_B2s
- **Redis**: 1GB → 2GB

---

## 🐛 Production Troubleshooting

### Logs

```bash
# Docker logs
docker-compose logs -f web

# Azure logs
az webapp log tail --name tenderai-web --resource-group tenderai-rg

# Elasticsearch error search
curl -X GET "http://localhost:9200/_search" -H 'Content-Type: application/json' -d'
{
  "query": {
    "match": { "level": "ERROR" }
  }
}'
```

### Database Connection Issues

```bash
# Test PostgreSQL bağlantısı
psql -h your-host -U tenderadmin -d tenderai -c "SELECT 1"

# Connection pool stats
docker-compose exec web dotnet-counters monitor -n TenderAI.Web --counters Microsoft.EntityFrameworkCore
```

---

## ✅ Post-Deployment Checklist

- [ ] Web app erişilebilir mi? (http://your-domain)
- [ ] Dashboard yükleniyor mu?
- [ ] Veritabanı bağlantısı çalışıyor mu?
- [ ] Migration'lar uygulandı mı?
- [ ] Logs akıyor mu?
- [ ] Health check yanıt veriyor mu?
- [ ] OpenAI API çalışıyor mu? (test ile doğrula)
- [ ] SSL sertifikası geçerli mi? (HTTPS)
- [ ] Backup çalışıyor mu?

---

## 📞 Acil Durum Planı

### Rollback Prosedürü

```bash
# Docker ile
docker-compose down
docker-compose -f docker-compose.old.yml up -d

# Azure ile
az webapp deployment slot swap \
  --resource-group tenderai-rg \
  --name tenderai-web \
  --slot staging
```

### Database Restore

```bash
# Backup'tan geri yükle
pg_restore -h your-host -U tenderadmin -d tenderai backup.dump
```

---

**🎉 Production'a başarıyla deploy ettiniz!**

**İzleme:** Her gün logları kontrol edin, haftalık performans raporu çıkarın.

# NotMarket Admin Starter

Bu başlangıç paketi NotMarket projesinin ilk geliştirme fazıdır.

## İçerik

- `backend/NotMarket.Api`: ASP.NET Core 10 Web API
- `frontend`: React 19 + TypeScript + Vite admin paneli
- PostgreSQL veritabanı
- JWT tabanlı admin girişi
- Dashboard, kullanıcılar, öğrenci doğrulamaları ve not moderasyonu ekranları

## Gereksinimler

- .NET 10 SDK
- Node.js 22+
- PostgreSQL 16+
- VS Code
- Docker Desktop (isteğe bağlı)

## Hızlı başlangıç

### 1. PostgreSQL'i Docker ile başlat

```bash
docker compose up -d
```

### 2. Backend'i çalıştır

```bash
cd backend/NotMarket.Api

dotnet tool install --global dotnet-ef
dotnet restore
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run
```

API varsayılan olarak terminalde gösterilen `https://localhost:...` adresinde çalışır.

### 3. Frontend'i çalıştır

Yeni terminal aç:

```bash
cd frontend
cp .env.example .env
npm install
npm run dev
```

Tarayıcı:

```text
http://localhost:5173
```

### Geliştirme admin hesabı

```text
E-posta: admin@notmarket.local
Şifre: ChangeMe123!
```

Bu hesap yalnızca geliştirme ortamı içindir. Canlıya çıkmadan önce `SeedAdmin` ayarlarını ortam değişkenleriyle değiştir.

## Ortam değişkenleri

Backend için örnek:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=notmarket;Username=notmarket;Password=notmarket_dev"
export Jwt__Key="en-az-32-karakterlik-guclu-bir-anahtar"
export SeedAdmin__Email="admin@example.com"
export SeedAdmin__Password="guclu-parola"
```

## İlk faz kapsamı

- Admin giriş
- Yetkili endpoint koruması
- Dashboard metrikleri
- Kullanıcı listeleme ve hesap durumu değiştirme
- Öğrenci doğrulamalarını onaylama/reddetme
- Not başvurularını onaylama/reddetme
- Audit log altyapısı

# NotificationService

.NET 10, EF Core 10, PostgreSQL 17 ve Swagger kullanan basit Onion / Clean Architecture başlangıç projesi.

## Katmanlar

- **Domain:** Template, Notification ve kanal/durum enum'ları. Dış bağımlılık yok.
- **Application → Domain:** Push gönderimi için IPushNotificationSender sözleşmesi.
- **Infrastructure → Application:** PostgreSQL DbContext, entity eşlemeleri ve EF migration'ları.
- **Api → Infrastructure:** Uygulamanın bağımlılıklarını birleştiren giriş noktası, Swagger ve sağlık endpoint'i.

API'nin Infrastructure referansı bağımlılık kaydı içindir. İş mantığı sonraki adımda Application katmanına eklenecek.

## Çalışan adresler

- Swagger: http://127.0.0.1:5080/swagger
- Sağlık: http://127.0.0.1:5080/health
- PostgreSQL: 127.0.0.1:5432
- Veritabanı: notification_db
- Uygulama kullanıcısı: notification_app

PostgreSQL proje altında `.local/pgsql` içinde kurulu; verisi `.local/pgdata` içinde kalıcıdır. Windows servisi veya Docker kullanılmaz. Yalnızca localhost dinlenir ve SCRAM parola doğrulaması kullanılır.

Yerel rastgele parolalar ve connection string `.local/settings.json` dosyasındadır. Bu klasör Git dışında tutulur. Verileri korumak için `.local/pgdata` klasörünü silmeyin. Başka bir bilgisayara kaynak kodu taşırken PostgreSQL'i ayrıca hazırlamak gerekir.

## Yeniden başlatma

Proje klasöründe PowerShell açın:

```powershell
.\scripts\Start-Local.ps1
```

Script gerektiğinde PostgreSQL'i başlatır ve backend'i 5080 portunda çalıştırır. Önceden çalışan backend varsa önce ilgili terminalde Ctrl+C ile durdurun. Backend'i durdurmak PostgreSQL'i kapatmaz.

PostgreSQL'i durdurmak için:

```powershell
.\scripts\Stop-Database.ps1
```

## Migration

`InitialCreate` migration'ı oluşturuldu ve yerel veritabanına uygulandı. Tablolar `Templates`, `Notifications` ve EF'nin `__EFMigrationsHistory` tablosudur.

Sonraki migration'ları uygulamak için PostgreSQL çalışırken:

```powershell
.\scripts\Update-Database.ps1
```

Yeni migration oluşturmak için:

```powershell
$root = $PWD.Path
$env:DOTNET_CLI_HOME = "$root/.local/dotnet-home"
$env:APPDATA = "$root/.local/appdata"
$env:NUGET_PACKAGES = "$root/.local/nuget"
$env:ConnectionStrings__NotificationDb = (Get-Content .local/settings.json | ConvertFrom-Json).ConnectionString
dotnet tool run dotnet-ef migrations add MigrationAdi --project src/NotificationService.Infrastructure --startup-project src/NotificationService.Api --output-dir Persistence/Migrations
```

Yerel dotnet-ef aracı 10.0.0 sürümüne sabitlenmiştir. Yeni bilgisayarda `dotnet tool restore` gerekir.

## Şema

Template: Id, Code (benzersiz), Title, Body, IsActive, CreatedAtUtc.

Notification: Id, TemplateId (opsiyonel FK), Channel, Status, Recipient, Title, Body, ProviderMessageId, ErrorMessage, CreatedAtUtc, SentAtUtc.

Bildirim geçmişindeki başlık ve içerik bağımsız tutulur. Bağlı bildirim varken template silinmesi engellenir. Zaman alanları UTC'dir. Push için Recipient alanı cihaz token'ını tutacak.

## Sonraki adım

Henüz CRUD endpoint'i veya bildirim gönderme uygulaması yoktur. Firebase Admin SDK ve gerçek push gönderimi birlikte eklenecek. `IPushNotificationSender` sözleşmesi ve boş Firebase ayarları hazırdır; anahtar gerekmediği için eklenmemiştir. SMS ve e-posta yalnızca kanal enum'unda bulunur; sağlayıcı entegrasyonları yoktur.

Firebase servis hesabı dosyasını kaynak kontrolüne eklemeyin. İleride Firebase ayarlarını environment variable veya user-secrets üzerinden sağlayabiliriz.

## Doğrulama

Proje derlendi; migration uygulandı; PostgreSQL üzerinden tablolar ve migration kaydı sorgulandı; Swagger HTTP 200 ve `/health` Healthy / Connected yanıtı doğrulandı.

Bu oturumda Windows TLS katmanı NuGet bağlantısında hata verdi. Paketler, sertifika doğrulaması yapan Python üzerinden geçici localhost aracısıyla resmi NuGet kaynağından indirildi. Projenin NuGet.Config dosyası standart resmi kaynağı kullanır; mevcut paketler `.local/nuget` önbelleğindedir. NuGet güvenlik açığı sorgusu bu ortamda tamamlanamadı.

Kaynaklar: [Npgsql EF Core 10](https://www.npgsql.org/efcore/release-notes/10.0.html), [PostgreSQL Windows dağıtımı](https://www.postgresql.org/download/windows/).

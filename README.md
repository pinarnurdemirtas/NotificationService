# NotificationService

.NET 10, EF Core, PostgreSQL, MediatR ve Firebase Admin SDK ile kullanıcılar arasında push bildirim gönderme örneği. Kullanıcı kaydı/girişi, kullanıcıya bağlı cihazlar, template ve gönderim geçmişi bulunur. SMS ve e-posta henüz uygulanmaz.

## Pınar → Ayşe testi

1. Visual Studio’da çalışan backend'i durdurup yeniden F5 ile **http** profilinde başlatın. Güncel controller'ların yüklenmesi için yeniden başlatmak gerekir.
2. **Chrome:** http://localhost:5009/push-test.html → Yeni hesap oluştur → kullanıcı adı `pinar`, görünen ad `Pınar`, kendi belirlediğiniz en az 10 karakterli parola.
3. **Edge:** aynı adres → Yeni hesap oluştur → kullanıcı adı `ayse`, görünen ad `Ayşe`, ayrı bir parola.
4. **Ayşe’nin Edge ekranında** “Bildirim izni ver ve cihazımı kaydet” butonuna basın ve izni onaylayın.
5. **Pınar’ın Chrome ekranında** kullanıcı listesini yenileyin; alıcı olarak Ayşe’yi seçip gönderin. Pınar’ın kendi tarayıcısını kaydetmesi, gönderim yapabilmesi için zorunlu değildir.
6. Ayşe’nin sayfası ön plandaysa mesaj sayfada görünür. Edge başka sekmedeyken sistem bildirimi gösterilir. Windows bildirim/rahatsız etmeyin ayarları görüntülemeyi etkileyebilir.
7. Karşılıklı göndermek için Pınar’ın tarayıcısını da kaydedin.

Hazır demo hesap veya varsayılan parola yoktur. Hesaplar Users tablosunda kalıcıdır; tekrar kullanımda Giriş yap seçilir. Aynı tarayıcıdaki iki sekme ayrı cihaz değildir. Ayrı tarayıcı veya ayrı tarayıcı profili kullanın.

## Mimari ve CQRS

```text
Api → ISender.Send(Command / Query) → Application Handler
                                        ↓
                         Repository / Firebase / Token sözleşmeleri
                                        ↑
                                 Infrastructure
                                        ↓
                                      Domain
```

Domain: User, UserDevice, Template, Notification; dış bağımlılık yoktur.

Application: MediatR command/query handler'ları, doğrulama ve kullanım senaryoları. EF Core, HttpContext ve Firebase SDK bağımlılığı yoktur. MediatR 12.5.0 kullanılır.

Infrastructure: PostgreSQL repository'leri, EF eşlemeleri/migration'lar, Firebase adaptörü, ASP.NET Identity PasswordHasher ve JWT üretimi.

Api: JWT doğrulama, HttpContext üzerinden ICurrentUser, controller'lar, HTTP hata eşlemeleri, Swagger ve yalnızca Development ortamındaki test ekranı.

Application/Users, Devices, Notifications ve Templates altında Commands ve Queries klasörleri bulunur. AddApplication handler'ları assembly üzerinden kaydeder. SendUserPushCommandHandler, alıcının aktif cihazlarını bulur ve her biri için dahili SendPushNotificationCommand akışını çağırır.

## Kimlik ve cihaz güvenliği

- Parolalar ASP.NET Core Identity PasswordHasher ile hash'lenir; düz metin parola saklanmaz veya yanıtlarda dönmez.
- Register/login JWT verir. Issuer, audience, imza, algoritma ve süre doğrulanır. Oturum süresi yerel ayarda 60 dakikadır; auth endpoint'leri IP başına dakikada 20 istekle sınırlıdır.
- Test sayfası erişim token'ını sessionStorage içinde saklar. Cihaz token'ı JWT değildir.
- Gönderen ID'si istek gövdesinden alınmaz; doğrulanmış JWT içindeki sub claim'inden gelir.
- /api/devices işlemleri yalnızca giriş yapan kişinin cihazlarına erişir. Aktif cihaz token'ı başka bir kullanıcı tarafından devralınamaz.
- Çıkış, bu tarayıcının cihaz kaydını pasifleştirir, Firebase registration token'ını siler ve yerel oturumu temizler. Diğer cihazlar etkilenmez. JWT stateless olduğundan kopyalanmış bir erişim token'ı süresi dolana kadar geçerlidir; refresh/revocation sistemi bu öğrenme sürümünde yoktur.
- Aktif cihaz token hash'i üzerinde filtreli benzersiz indeks bulunur. Çok uzun FCM token'ı indekslemek yerine SHA-256 hash'i indekslenir. Pasif bir token başka kullanıcı tarafından yeniden kaydedilirse yeni cihaz kaydı açılır; geçmiş sahiplik değiştirilmez.
- Bildirim okuma/listeleme yalnızca gönderenin veya alıcının kayıtlarını döndürür. Eski kullanıcı ilişkisi olmayan test kayıtları kullanıcı geçmişinde görünmez.
- Kullanıcı listesi yalnızca ID, kullanıcı adı ve görünen ad döndürür. Cihaz ve bildirim yanıtları hedef token'ları veya parola hash'lerini içermez.

## Endpoint'ler

| Metot | Yol | İşlem |
|---|---|---|
| POST | /api/auth/register | Kullanıcı oluştur ve oturum aç |
| POST | /api/auth/login | Kullanıcı adı/parola ile giriş |
| GET | /api/auth/me | Oturumdaki kullanıcı |
| GET | /api/users | İlk 100 kullanıcı, alıcı seçimi |
| POST | /api/devices | Mevcut kullanıcıya cihaz kaydet/güncelle |
| GET | /api/devices | Kendi cihazlarını listele |
| DELETE | /api/devices/{id} | Kendi cihazını pasifleştir |
| POST | /api/push-notifications | Alıcının tüm aktif cihazlarına gönder |
| GET | /api/push-notifications/{id} | Kendi bildirim kaydını oku |
| GET | /api/push-notifications?skip=0&take=20 | Kendi geçmişini listele |
| POST / GET | /api/templates | Template oluştur/listele |
| GET | /api/templates/{id} | Template oku |

Auth register/login ve health dışındaki API işlemleri giriş gerektirir. Swagger'da register/login yanıtındaki accessToken değerini **Authorize** bölümüne yapıştırın; bearer prefix'ini Swagger ekler. `requests/push.http` dosyası da örnek akışı içerir.

Gönderim gövdesi artık cihaz token'ı yerine kullanıcı ID'si alır:

```json
{
  "recipientUserId": "AYSE_USER_GUID",
  "title": "Merhaba Ayşe",
  "body": "Pınar’dan bir bildirim",
  "data": { "screen": "notifications" }
}
```

Template için title/body yerine templateId ve variables gönderilir. Örnek template başlığı `Merhaba {{name}}`, gövdesi `{{orderNumber}} numaralı siparişin alındı.` olabilir.

```json
{
  "recipientUserId": "AYSE_USER_GUID",
  "templateId": "TEMPLATE_GUID",
  "variables": { "name": "Ayşe", "orderNumber": "1234" }
}
```

## Gönderim davranışı

Her cihaz için önce Pending Notification oluşturulur; Firebase sonucu aynı kayda yazılır. SenderUserId, RecipientUserId, UserDeviceId ve ortak BatchId saklanır. Başlık/içerik, template değişse de geçmişte bağımsız kalır.

Firebase kabul ederse Sent + provider message ID + UTC zaman kaydedilir. Sent, cihazda gösterildiğinin kanıtı değildir. Reddedilen gönderim Failed; timeout/bağlantı kesilmesi Unknown olarak tutulur. Unregistered dönen cihaz pasifleştirilir; diğer cihazlara gönderim devam eder. Gönderim sonucu sentCount, failedCount, unknownCount, totalDevices ve cihaz başına kayıtları içerir.

Tüm cihazlar başarılıysa 200, kısmi sonuç varsa 207 döner. Alıcının aktif cihazı yoksa 409; eksik alıcı 404; geçersiz istek 400; oturumsuz istek 401'dir. Tüm gönderimler Firebase kapalı/erişilemez olduğu için başarısızsa 503, sağlayıcı reddiyse 422, sonuç belirsizse 504 döner.

Gönderim senkrondur. Otomatik retry, kuyruk, idempotency, kullanıcı silme/parola sıfırlama, refresh token ve sohbet sistemi bu sürüme dahil değildir. Aynı POST tekrar yeni gönderim oluşturur. Süreç sağlayıcı kabulünden sonra kapanırsa Pending kalabilir; kontrol etmeden tekrar göndermeyin. Arka plan işçisine geçiş ileride eklenebilir.

## Firebase ve yerel ayarlar

Bu bilgisayarda ConnectionStrings, Firebase, FirebaseWeb ve Jwt ayarları `src/NotificationService.Api/appsettings.Development.json` içinde hazırdır. Dosya Git dışında tutulur. Servis hesabı repo kökünde `.local/firebase-service-account.json` içindedir; statik dosya alanının dışındadır.

Yeni kurulumda appsettings.Development.example.json dosyasını yerel appsettings.Development.json adına kopyalayıp connection string, Firebase ayarları ve rastgele en az 64 karakterli JWT SigningKey girin. SigningKey'i paylaşmayın veya örnek sabit bir değer kullanmayın. Firebase CredentialsPath mutlak servis hesabı JSON yoludur. FirebaseWeb yalnızca public client config ve public VAPID key içerir.

Test sayfası /push-test.html, service worker ve public config endpoint'i sadece Development ortamında sunulur. Web SDK 12.8.0'a sabitlenmiştir. Ön planda onMessage; arka planda SDK'nın otomatik notification gösterimi kullanılır. Aynı mesaj için ikinci bir showNotification çağrısı yapılmaz.

## PostgreSQL ve çalıştırma

Mevcut PostgreSQL 127.0.0.1:5432 üzerinde, notification_db veritabanıyla çalışır. İlk taşınabilir kurulumun ikili/veri dosyaları Documents/Codex altındaki ilk projenin .local klasöründedir; klona dahil edilmez.

```powershell
dotnet restore
dotnet tool restore
dotnet ef database update --project src/NotificationService.Infrastructure --startup-project src/NotificationService.Api -- --environment Development
dotnet run --project src/NotificationService.Api --launch-profile http
```

Users ve UserDevices tabloları AddUsersAndDevices migration'ı ile oluşturulmuştur; Notifications kullanıcı/cihaz ilişkileri aynı migration'dadır. Önceki bildirimler korunur. Veritabanı uygulama kapandığında otomatik durmaz.

## Doğrulama

`dotnet test` ile 31 test: parola hash'i ve JWT, login hataları, cihaz sahipliği, tekrar kayıt, güvenli hesap değişimi, Pınar → Ayşe çoklu cihaz akışı, gönderici kimliği, geçmiş izolasyonu ve önceki Firebase/template senaryoları.

Yerel HTTP/PostgreSQL testi: ayrı oturumlar, sahte JWT reddi, yetkisiz cihaz işlemi, gönderen spoof girişimi, alıcı cihazlarına kayıt oluşturma, geçmiş erişimi ve cihaz pasifleştirme doğrulandı. Testte Firebase yalnızca test sürecinin environment override'ı ile kapatıldı; gerçek yerel Firebase ayarı korunur. Geçici test kullanıcıları/cihazları/kayıtları temizlendi. Gerçek browser teslimatı bildirim izni ve kayıtlı cihazla kullanıcı tarafından tamamlanır.

Kaynaklar: [Firebase gönderim](https://firebase.google.com/docs/cloud-messaging/send/admin-sdk), [ASP.NET Identity PasswordHasher](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1?view=aspnetcore-10.0).

# VetRandevu.Api

Veteriner randevu, klinik admin, aşı kartı ve kullanıcı yönetimi için ASP.NET Core API.

## Özellikler
- Kullanıcı kayıt/giriş ve profil
- Klinik başvurusu ve onay süreci
- Klinik admin paneli (randevu, ajanda, hizmetler, aşı kartı, pet kartları)
- Randevu oluşturma, iptal ve yeniden planlama
- Yorumlar + şikayet + AI moderasyon
- E-posta hatırlatmaları (SMTP)

## Kurulum
1) Örnek ayar dosyalarını kopyala:
   - `appsettings.example.json` → `appsettings.json`
   - `appsettings.Development.example.json` → `appsettings.Development.json`
2) Gerekli alanları doldur:
   - `ConnectionStrings:DefaultConnection`
   - `SeedAdmin` ve `SeedClinicAdmin`
   - `Smtp` (mail gönderimi için)
   - `Gemini:ApiKey` (yorum moderasyonu için)
3) DB migration:
```
dotnet ef migrations add InitialLocal
dotnet ef database update
```
4) Çalıştır:
```
dotnet run
```

## Notlar
- Secrets içeren dosyalar gitignore’da tutulur.
- Klinik başvuruları `POST /clinics/apply` ile açılır ve admin onayı bekler.

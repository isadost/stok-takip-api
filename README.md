# Stok Takip API

[![Tests](https://github.com/isadost/stok-takip-api/actions/workflows/tests.yml/badge.svg)](https://github.com/isadost/stok-takip-api/actions/workflows/tests.yml)

ASP.NET Core (.NET 10) ile geliştirilmiş stok takip Web API'si. Ürün ve kategori yönetimi, arama, kategori filtresi, sayfalama ve düşük stok işaretlemesi içerir. Entegrasyon testleriyle doğrulanmıştır.

Aynı problemin PHP/Laravel ile yazılmış sürümü: [laravel-stok-takip](https://github.com/isadost/laravel-stok-takip)

## Özellikler

- Ürün ekleme, listeleme, güncelleme ve silme
- Kategori ekleme, listeleme, güncelleme ve silme (ürünü olan kategori silinemez)
- SKU ve kategori adı benzersizliği (hem doğrulamada hem veritabanında)
- Negatif adet ve fiyat reddi
- Ad veya SKU ile arama, kategoriye göre filtreleme
- Sayfalama (`page`, `pageSize`)
- Adedi minimum stoğa eşit veya altında olan ürünler için `isLowStock` alanı

## Teknolojiler

.NET 10, ASP.NET Core Web API (controller tabanlı), Entity Framework Core, SQLite, xUnit, GitHub Actions

## Kurulum

```bash
git clone https://github.com/isadost/stok-takip-api.git
cd stok-takip-api
dotnet run --project src/StokTakip.Api
```

Geliştirme ortamında uygulama ilk açılışta veritabanını (`stoktakip.db`) oluşturur ve örnek veri ekler (5 kategori, 50 ürün). Dinlenen adres terminalde `Now listening on:` satırında yazar.

## Uç Noktalar

| Yöntem | Yol | Açıklama |
|---|---|---|
| GET | `/api/categories` | Kategorileri ürün sayılarıyla listeler |
| POST | `/api/categories` | Kategori ekler (yinelenen ad: 409) |
| PUT | `/api/categories/{id}` | Kategoriyi günceller |
| DELETE | `/api/categories/{id}` | Kategoriyi siler (ürünü varsa: 409) |
| GET | `/api/products` | Ürünleri listeler: `search`, `categoryId`, `page`, `pageSize` |
| GET | `/api/products/{id}` | Tek ürünü getirir |
| POST | `/api/products` | Ürün ekler (yinelenen SKU: 409, geçersiz veri: 400) |
| PUT | `/api/products/{id}` | Ürünü günceller |
| DELETE | `/api/products/{id}` | Ürünü siler |

Örnek:

```bash
curl "http://localhost:<port>/api/products?search=SKU-0007&pageSize=5"
```

## Testler

```bash
dotnet test
```

15 entegrasyon testi vardır. Her test, hafızada geçici bir SQLite veritabanı kurar ve gerçek `stoktakip.db` dosyasına dokunmaz. Testler yinelenen SKU'nun reddini, negatif adedin reddini, güncellemede kendi SKU'sunu koruma ile başka ürünün SKU'suyla çakışmanın engellenmesini, kategori silme kısıtını, arama, filtre ve düşük stok işaretlemesini kapsar. Her push'ta GitHub Actions ile otomatik çalışır.

## Tasarım Kararları

- **Silme kısıtı:** Ürünü olan kategori silinemez (`DeleteBehavior.Restrict`). Kontrol controller'da da yapılır, böylece kullanıcıya veritabanı hatası yerine anlamlı bir 409 yanıtı döner.
- **Fiyat tipi:** `decimal` ve `HasPrecision(10, 2)` kullanılır. SQLite ondalık değerleri metin olarak sakladığı için, fiyata göre veritabanı tarafında sıralama gerekirse ayrıca ele alınmalıdır.
- **Doğrulama:** İstek modellerinde `DataAnnotations` kullanılır, `[ApiController]` geçersiz veri için otomatik 400 döner.
- **Test yaklaşımı:** `WebApplicationFactory` ile uygulama gerçek HTTP katmanıyla test edilir.

## Laravel Sürümüyle Karşılaştırma

| Laravel | ASP.NET Core |
|---|---|
| Model + migration | Varlık sınıfı + EF Core migration |
| `restrictOnDelete` | `DeleteBehavior.Restrict` |
| Form doğrulama | `DataAnnotations` + `[ApiController]` |
| Seeder | `DbSeeder` |
| PHPUnit feature testleri | xUnit + `WebApplicationFactory` |
| `paginate()` | `Skip` / `Take` |

## Yapılacaklar

- [ ] Kimlik doğrulama (JWT)
- [ ] Swagger/OpenAPI arayüzü
- [ ] Docker ile çalıştırma
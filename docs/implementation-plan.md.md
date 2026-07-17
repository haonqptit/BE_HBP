Gmail	Hảo Nguyễn Quang <haonqptit@gmail.com>
(no subject)
Hảo Nguyễn Quang <haonqptit@gmail.com>	Fri, Jul 17, 2026 at 6:59 PM
To: Nguyễn Quang Hảo <haooila7@gmail.com>
   


# HBP Backend — Kế hoạch triển khai chi tiết (Implementation Plan)

| Thông tin | Giá trị |
|---|---|
| Phiên bản | 1.0 — 17/07/2026 |
| Nguồn yêu cầu | `docs/schema.sql` (DDL chuẩn) + `docs/SRS.md` (addendum quyết định kỹ thuật) |
| Trạng thái DB | **Đã migrate** (`InitialCreate` + `AddLoginLockout`) trên máy deploy — bảng đã đủ |
| Đối tượng đọc | Dev triển khai các phase còn lại. Đọc xong tài liệu này phải code được ngay, đúng convention |

**Cách dùng tài liệu:** làm theo thứ tự ở [§10 Thứ tự thực hiện](#10-thứ-tự-thực-hiện). Mỗi phase có mục *Deliverables* (file phải tạo/sửa), *Quy tắc nghiệp vụ*, và *Definition of Done*. Mọi đoạn code mẫu trong tài liệu là **đặc tả hành vi** — được phép chỉnh cú pháp, không được đổi hành vi.

---

## 0. Hiện trạng repo (đã xong — KHÔNG làm lại)

| Phase | Trạng thái | Bằng chứng trong repo |
|---|---|---|
| 0B Toolchain | ✅ | `.config/dotnet-tools.json` (dotnet-ef 8.0.11), `Directory.Build.props`, `.editorconfig`, `.dockerignore` |
| 1 Migration + Seed | ✅ | `src/HBP.Infrastructure/Persistence/Migrations/` (2 migration), `Seed/SeedData.cs`, `HbpDbContextFactory.cs` |
| 2 Cross-cutting | ✅ | `GlobalExceptionHandler`, `ValidationActionFilter`, `LanguageResolutionMiddleware`, Serilog JSON, CORS, ProblemDetails, JSON camelCase + enum-as-string |
| 3 Auth | ✅ | `AuthController` (csrf/login/logout/me), `AuthService` (lockout 5 lần/15p → khóa 15p), `AdminCsrfMiddleware` (double-submit), cookie 8h absolute |
| 4 Media | ✅ | `MediaController`, `MediaService` (chặn xóa in-use → 409), `ImageSharpImageProcessor` (3 biến thể WebP q82), `LocalFileMediaStorage` (`{yyyy}/{MM}/{id}/`) |

**Còn lại (tài liệu này đặc tả):** Phase F (sửa finding) → 5 (Public API) → 6 (Booking/Contact) → 8 (Email) → 7 (Admin CRUD) → 9 (Testing) → 10 (Docker/Ops).

---

## 1. Quy ước code BẮT BUỘC (trích từ code hiện có — mọi code mới phải khớp)

### 1.1. Kiến trúc & vị trí file

```
Api → Application → Domain
Api → Infrastructure → Domain
Infrastructure → Application   (implement abstraction)
```

- **Interface + DTO + validator** đặt ở `HBP.Application/{Feature}/` — 1 file contracts gộp record + interface theo mẫu `Auth/AuthContracts.cs`, validator tách file riêng (`XxxValidator.cs`).
- **Implementation** đặt ở `HBP.Infrastructure/{Feature}/` (mẫu: `Infrastructure/Auth/AuthService.cs`, `Infrastructure/Media/MediaService.cs`). Impl nhận `HbpDbContext` qua **primary constructor**, class `sealed`.
- **Controller** đặt ở `HBP.Api/Controllers/` (public) hoặc `Controllers/Admin/` (admin, `[Authorize]` ở class). Controller **mỏng**: gọi service, trả kết quả; không chứa logic nghiệp vụ, không truy cập DbContext trực tiếp.
- **Middleware/filter** đặt ở `HBP.Api/Infrastructure/`.
- Abstraction dùng chung (IClock, IEmailSender…) ở `HBP.Application/Abstractions/`.

### 1.2. DTO & JSON

- Record `sealed`, đặt tên: request vào suffix **`Request`**, response ra suffix **`Response`**. Mapping **thủ công** (không AutoMapper).
- JSON: camelCase, enum serialize **theo tên member C#** (`"SHOW_PRICE"`, `"CONTACT"`, `"RECEIVED"`, `"PENDING"`, `"Vi"`, `"Ja"`). Input enum **case-insensitive** → FE gửi `"vi"`/`"ja"` vẫn parse được; output là `"Vi"`/`"Ja"` (FE lowercase nếu cần).
- `DateOnly` serialize `"yyyy-MM-dd"`; timestamp là UTC ISO-8601.
- Phân trang: **luôn** dùng `Application/Common/PagedResult<T>` → envelope `{ items, page, pageSize, totalCount, totalPages }`. Query string: `?page=1&pageSize=20` (default 20, **max 100** — clamp như `MediaService.ListAsync`).

### 1.3. Lỗi & exception

Ném exception nghiệp vụ từ `Application.Common` — `GlobalExceptionHandler` đã map sẵn:

| Exception | HTTP | Ghi chú |
|---|---|---|
| `NotFoundException` | 404 | |
| `ValidationException` | 400 | có `extensions.errors` dạng `{ field: [messages] }` |
| `ConflictException` | 409 | trùng slug/code/unique |
| `MediaInUseException` | 409 | có `extensions.references` |

Validator FluentValidation tự chạy qua `ValidationActionFilter` cho **mọi** action argument có validator đăng ký — chỉ cần tạo validator trong Application, không cần gọi tay.

### 1.4. Truy vấn EF

- Đọc: `AsNoTracking()` + `Select` projection. Client-eval chỉ được ở **Select cuối** (top-level projection).
- Detail nhiều collection: `AsSplitQuery()`.
- Search admin: `EF.Functions.ILike(col, pattern)` với `pattern = $"%{escaped}%"` — escape `%`, `_`, `\` trong input trước khi ghép.
- Bắt trùng unique: catch `DbUpdateException` có inner `PostgresException { SqlState: "23505" }` → ném `ConflictException` (đọc `ConstraintName` để biết trường nào).

### 1.5. i18n (quy tắc chọn ngôn ngữ)

- `LanguageResolutionMiddleware` đã set `IRequestLanguageAccessor.Language` (ưu tiên `?lang=`, rồi `Accept-Language`, default `Vi`).
- **Fallback bắt buộc:** khi lang = Ja mà cột `*_ja` NULL → dùng `*_vi` (`*_vi` NOT NULL với name, nullable với mô tả). Dùng helper chung (tạo ở Phase F):

```csharp
// HBP.Application/Common/Localized.cs
public static class Localized
{
    public static string Pick(LanguageCode lang, string vi, string? ja)
        => lang == LanguageCode.Ja && !string.IsNullOrEmpty(ja) ? ja : vi;
    public static string? Pick(LanguageCode lang, string? vi, string? ja)
        => lang == LanguageCode.Ja && !string.IsNullOrEmpty(ja) ? ja : vi;
}
```

### 1.6. Thời gian

Mọi logic thời gian lấy từ `IClock.UtcNow` (đã có `SystemClock`) — **không** gọi `DateTime.UtcNow` trực tiếp trong service (để test được).

---

## 2. Phase F — Sửa các finding hiện có (làm TRƯỚC tiên, ~0.5 ngày)

### F1. `AuthService` — chống dò username qua timing

File: `src/HBP.Infrastructure/Auth/AuthService.cs` (dòng 20–22). Comment nói "always perform a hash verification" nhưng code return sớm. Sửa cho khớp comment:

```csharp
private static string? _dummyHash; // PasswordHasherAdapter là singleton, hash 1 lần là đủ

public async Task<LoginResult> LoginAsync(...)
{
    ...
    if (user is null || !user.IsActive)
    {
        _dummyHash ??= hasher.Hash("hbp-timing-equalizer");
        hasher.Verify(_dummyHash, request.Password); // đốt thời gian tương đương verify thật
        return new LoginResult(false, false, null);
    }
    ...
}
```

### F2. `MediaService.DeleteAsync` — log orphan thay vì 500

File: `src/HBP.Infrastructure/Media/MediaService.cs` (dòng 55–57). Sau khi DB commit, xóa file lỗi thì **không được** ném ra ngoài (client đã xóa thành công về mặt nghiệp vụ). Inject `ILogger<MediaService>`:

```csharp
db.MediaFiles.Remove(entity);
await db.SaveChangesAsync(cancellationToken);
try { await storage.DeleteAsync(paths, cancellationToken); }
catch (Exception ex)
{
    logger.LogWarning(ex, "Media {MediaId} removed from DB but files orphaned at {Path}", id, entity.StoragePath);
}
```

### F3. `Program.cs` — ForwardedHeaders + bỏ HTTPS redirect + dời CORS

Chạy sau Traefik/Coolify (TLS terminate ở proxy) nên:

1. **Thêm** `UseForwardedHeaders` (đầu pipeline):

```csharp
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Container sau Traefik: không biết trước IP proxy → clear để tin header từ mạng nội bộ Coolify.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
```

2. **Xóa** `app.UseHttpsRedirection();` — Traefik chịu trách nhiệm redirect HTTPS; giữ lại sẽ gây redirect sai với health check nội bộ.
3. **Dời** `app.UseCors("Frontend")` lên **trước** `UseStaticFiles` để file `/media/**` có CORS header (Next.js Image optimizer cần fetch cross-origin).

**Thứ tự middleware ĐÍCH sau toàn bộ kế hoạch** (Phase F + 5 + 6 thêm dần — đây là trạng thái cuối để đối chiếu):

```
UseForwardedHeaders
UseExceptionHandler
UseSerilogRequestLogging
Swagger (Development only)
UseCors("Frontend")
UseStaticFiles(/media)
LanguageResolutionMiddleware
UseRateLimiter                  ← Phase 6
UseAuthentication
AdminCsrfMiddleware
UseAuthorization
PublicCacheMiddleware           ← Phase 5
MapControllers, /health, /health/ready
```

### F4. `MediaResponse` — trả đủ 3 biến thể URL

File: `src/HBP.Application/Media/MediaContracts.cs`. Thêm helper dùng chung + mở rộng DTO:

```csharp
// HBP.Application/Common/MediaUrl.cs — convention: .../{id}/original.webp → medium.webp / thumbnail.webp
public static class MediaUrl
{
    public static string Variant(string originalUrl, string variant)
        => originalUrl[..(originalUrl.LastIndexOf('/') + 1)] + variant + ".webp";
}

public sealed record MediaResponse(Guid Id, string OriginalFileName, string PublicUrl, string MediumUrl,
    string ThumbnailUrl, string MimeType, long SizeBytes, int? Width, int? Height,
    string? AltTextVi, string? AltTextJa, DateTime CreatedAt);
```

Sửa `MediaService.Map` tương ứng (`MediumUrl = MediaUrl.Variant(x.PublicUrl, "medium")`, …).

### F5. `launchSettings.json` — dọn scaffold

File: `src/HBP.Api/Properties/launchSettings.json`: profile `http` đổi `applicationUrl` → `http://localhost:5099` (khớp `Media:BaseUrl` trong `appsettings.Development.json`), `launchUrl` → `swagger`. Xóa profile `https` và `IIS Express` (không dùng).

### F6. `MediaController` — nới RequestSizeLimit

File: `src/HBP.Api/Controllers/Admin/MediaController.cs`. `[RequestSizeLimit(5MB)]` áp cho **cả multipart overhead** → ảnh đúng 5MB bị Kestrel trả 413 thô. Đổi thành `[RequestSizeLimit(6 * 1024 * 1024)]` — rule nghiệp vụ 5MB đã enforce trong `MediaService.UploadAsync` (trả 400 ProblemDetails đẹp).

### F7. Tạo `Localized` helper (§1.5) + `MediaUrl` (F4) trong `Application/Common/`

**DoD Phase F:** `dotnet build HBP.slnx` 0 error; login sai username và sai password có thời gian phản hồi tương đương; xóa media khi file FS đã mất vẫn trả 204 + log warning.

---

## 3. Phase 5 — Public Read API (no auth)

### 3.1. Deliverables

| File | Nội dung |
|---|---|
| `Application/Public/PublicContracts.cs` | Toàn bộ DTO + 4 interface query service |
| `Infrastructure/Public/PublicRoomTypeQueryService.cs` | Impl |
| `Infrastructure/Public/PublicServiceQueryService.cs` | Impl |
| `Infrastructure/Public/PublicGalleryQueryService.cs` | Impl |
| `Infrastructure/Public/PublicAmenityQueryService.cs` | Impl |
| `Api/Controllers/RoomTypesController.cs` | `GET /api/rooms`, `GET /api/rooms/{slug}` |
| `Api/Controllers/ServicesController.cs` | `GET /api/services`, `GET /api/services/{slug}` |
| `Api/Controllers/GalleryController.cs` | `GET /api/gallery?category=` |
| `Api/Controllers/AmenitiesController.cs` | `GET /api/amenities` |
| `Api/Infrastructure/PublicCacheMiddleware.cs` | Cache-Control + ETag |
| `Infrastructure/DependencyInjection.cs` | đăng ký 4 service (Scoped) |

### 3.2. DTO (copy nguyên shape)

```csharp
public sealed record ImageResponse(string Original, string Medium, string Thumbnail, string? Alt);
public sealed record SeoResponse(string? Title, string? Description);
public sealed record AmenityResponse(Guid Id, string Name, string? Icon);

public sealed record RoomTypeListItemResponse(
    Guid Id, string Slug, string Name, string? ShortDescription,
    int Capacity, decimal? AreaSquareMeters, string? BedDescription,
    PriceDisplayMode PriceDisplayMode,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? PriceVnd,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? PriceUsd,
    ImageResponse? FeaturedImage, int DisplayOrder);

public sealed record RoomTypeDetailResponse(
    Guid Id, string Code, string Slug, string Name, string? ShortDescription, string? Description,
    int Capacity, decimal? AreaSquareMeters, string? BedDescription,
    PriceDisplayMode PriceDisplayMode,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? PriceVnd,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? PriceUsd,
    ImageResponse? FeaturedImage,
    IReadOnlyList<AmenityResponse> Amenities,
    IReadOnlyList<ImageResponse> Media,
    SeoResponse Seo);

public sealed record ServiceListItemResponse(Guid Id, string Slug, string Name,
    string? ShortDescription, string? PriceNote, ImageResponse? FeaturedImage, int DisplayOrder);
public sealed record ServiceDetailResponse(Guid Id, string Slug, string Name,
    string? ShortDescription, string? Description, string? PriceNote, ImageResponse? FeaturedImage);

public sealed record GalleryItemResponse(Guid Id, ImageResponse Image, string? Caption, int DisplayOrder);
public sealed record GalleryCategoryResponse(Guid Id, string Slug, string Name, int DisplayOrder,
    IReadOnlyList<GalleryItemResponse> Items);
```

### 3.3. Quy tắc nghiệp vụ (bắt buộc)

1. **Chỉ trả bản ghi visible**: list lọc `is_visible = true`; detail theo slug mà `is_visible = false` → `NotFoundException` (404, không lộ tồn tại). Gallery: category visible **và** item visible.
2. **Sắp xếp**: `ORDER BY display_order ASC, name_vi ASC` (tie-break tất định). Room detail: amenities theo `room_type_amenities.display_order ?? 0` rồi `amenities.display_order`; media theo `room_type_media.display_order` rồi `created_at`. Amenities trong detail cũng phải lọc `amenity.is_visible = true`.
3. **Giá (FR pricing)**: `price_display_mode == CONTACT` → **set `PriceVnd = PriceUsd = null`** trong mapping (bất kể DB có giá) → JSON bỏ hẳn field nhờ `WhenWritingNull`. `SHOW_PRICE` → trả nguyên giá.
4. **Ngôn ngữ**: mọi field song ngữ đi qua `Localized.Pick(lang, vi, ja)`; `Alt` của ảnh = `Localized.Pick(lang, alt_text_vi, alt_text_ja)`. Lang lấy từ `IRequestLanguageAccessor` inject vào query service.
5. **Ảnh**: build `ImageResponse` từ `media_files.public_url` + `MediaUrl.Variant(...)`. `FeaturedImage` null khi `featured_media_id` null.
6. **Gallery**: `GET /api/gallery` trả mọi category visible kèm items; `?category={slug}` lọc 1 category (không tìm thấy/ẩn → 404).
7. Query dùng `AsNoTracking()`, detail dùng `AsSplitQuery()`; **không** trả field `*_vi`/`*_ja` thô ra public API.

### 3.4. `PublicCacheMiddleware` (Cache-Control + ETag hỗ trợ ISR)

Chỉ áp cho **GET** và path bắt đầu `/api/rooms`, `/api/services`, `/api/gallery`, `/api/amenities`:

- Buffer response body (swap `Response.Body` bằng `MemoryStream`), sau `next()`: nếu status 200 → tính `ETag = W/"' + base64(SHA256(body)) + '"`.
- Request có `If-None-Match` khớp → trả **304**, body rỗng.
- Set header: `Cache-Control: public, max-age=60`, `Vary: Accept-Language` (nội dung phụ thuộc lang query + header).

### 3.5. DoD Phase 5

- `GET /api/rooms` trả list đúng thứ tự, room CONTACT **không có key** `priceVnd`/`priceUsd` trong JSON.
- `GET /api/rooms/{slug}?lang=ja` với bản ghi thiếu `name_ja` → trả `name_vi` (fallback).
- Room ẩn → 404 cả list (vắng mặt) lẫn detail.
- Response lần 2 với `If-None-Match` → 304.
- Unit test cho `Localized.Pick` + mapping bỏ giá (xem Phase 9).

---

## 4. Phase 6 — Booking & Contact submission (public)

### 4.1. Deliverables

| File | Nội dung |
|---|---|
| `Application/Requests/RequestContracts.cs` | DTO + `IBookingRequestService`, `IContactRequestService` |
| `Application/Requests/CreateBookingRequestValidator.cs` | validator |
| `Application/Requests/CreateContactRequestValidator.cs` | validator |
| `Infrastructure/Requests/ReferenceCodeGenerator.cs` | impl `IReferenceCodeGenerator` |
| `Infrastructure/Requests/BookingRequestService.cs` | impl |
| `Infrastructure/Requests/ContactRequestService.cs` | impl |
| `Api/Controllers/BookingRequestsController.cs` | `POST /api/booking-requests` |
| `Api/Controllers/ContactRequestsController.cs` | `POST /api/contact-requests` |
| `Program.cs` | AddRateLimiter + `app.UseRateLimiter()` |

### 4.2. DTO

```csharp
public sealed record CreateBookingRequestRequest(
    string FullName, string Email, string PhoneNumber,
    Guid? RoomTypeId, DateOnly? CheckInDate, DateOnly? CheckOutDate,
    int Adults, int? Children, int? NumberOfRooms, string? CustomerMessage,
    LanguageCode LanguageCode,
    string? Website);            // ← honeypot, form thật KHÔNG render field này

public sealed record CreateContactRequestRequest(
    string FullName, string Email, string PhoneNumber,
    string Subject, string Message, LanguageCode LanguageCode,
    string? Website);            // ← honeypot

public sealed record SubmissionResponse(string ReferenceCode);
```

### 4.3. Validator rules

| Field | Rule |
|---|---|
| FullName | NotEmpty, ≤255 |
| Email | NotEmpty, ≤255, `EmailAddress()` |
| PhoneNumber | NotEmpty, ≤30, regex `^[0-9+\-\s().]{6,30}$` |
| Adults | ≥1 (booking) |
| Children | null hoặc ≥0 |
| NumberOfRooms | null hoặc ≥1 |
| CustomerMessage | ≤4000 |
| Subject (contact) | NotEmpty, ≤255 |
| Message (contact) | NotEmpty, ≤8000 |
| CheckIn/CheckOut | **KHÔNG validate quan hệ** — để comment: `// BR-BOOK-014 / FR-BOOK-003: chủ ý KHÔNG kiểm tra check_out > check_in ở MVP. Đừng "sửa" điều này.` |
| RoomTypeId | không validate ở validator — check trong service (cần DB) |

`LanguageCode` sai (vd `"en"`) → System.Text.Json ném lỗi binding → MVC trả 400 sẵn, không cần xử lý thêm.

### 4.4. `ReferenceCodeGenerator`

- Format: `BK-{yyMMdd}-{XXXXXX}` / `CT-{yyMMdd}-{XXXXXX}`; ngày theo `IClock.UtcNow`.
- 6 ký tự từ bảng **Crockford base32**: `0123456789ABCDEFGHJKMNPQRSTVWXYZ` (32 ký tự, bỏ I/L/O/U tránh nhầm), sinh bằng `RandomNumberGenerator.GetInt32(32)` — **không** dùng `Random`.
- Không dùng số tuần tự (không lộ volume). Uniqueness dựa unique constraint + retry (bên dưới).

### 4.5. Luồng transaction (đặc tả — cả booking lẫn contact)

```
for attempt in 1..3:                                  // retry-on-conflict tối đa 3
  begin transaction
  try:
    request.ReferenceCode = generator.Generate...()
    request.Email = email.Trim().ToLowerInvariant()   // khớp normalize_email trigger
    db.Add(request); await db.SaveChangesAsync()      // ← 23505 uq_..._reference_code thì catch, rollback, retry
    recipients = đọc system_settings["notification_emails"] (JSON array)
    if recipients rỗng → log Warning "notification_emails chưa cấu hình" (vẫn tiếp tục)
    foreach r in recipients:
        db.EmailDeliveries.Add(new {
            RelatedEntityType = "BookingRequest" | "ContactRequest",   // đúng CHECK constraint
            RelatedEntityId   = request.Id,
            EmailType   = "BOOKING_ADMIN_NOTIFICATION" | "CONTACT_ADMIN_NOTIFICATION",
            Recipient   = r,
            LanguageCode= Vi,                          // admin luôn nhận tiếng Việt
            Status      = PENDING })
    db.EmailDeliveries.Add(new {                       // xác nhận cho khách
            ..., EmailType = "BOOKING_GUEST_CONFIRMATION" | "CONTACT_GUEST_CONFIRMATION",
            Recipient = request.Email, LanguageCode = request.LanguageCode, Status = PENDING })
    await db.SaveChangesAsync(); commit
    return SubmissionResponse(request.ReferenceCode)
  catch DbUpdateException(PostgresException 23505 trên uq_*_reference_code):
    rollback; continue                                 // hết 3 lần → ném lại (500)
```

Quy tắc bổ sung trong service:

- `RoomTypeId` có giá trị → phải tồn tại **và** `is_visible = true`, sai → `ValidationException("roomTypeId", ...)`.
- **Honeypot:** `Website` không rỗng → log Warning, **KHÔNG persist**, trả `201` với reference code sinh format hợp lệ (không lưu) — bot không phát hiện bị chặn.
- **KHÔNG gửi email inline** — chỉ ghi row `email_deliveries`, worker Phase 8 xử lý.
- Controller trả `StatusCode(201, new SubmissionResponse(code))` (không có Location — không có public GET theo id).

### 4.6. Rate limiting (anti-spam)

`Program.cs`:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (ctx, _) => { ctx.HttpContext.Response.Headers.RetryAfter = "60"; return ValueTask.CompletedTask; };
    options.AddPolicy("public-submit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",   // đúng IP thật nhờ ForwardedHeaders (F3)
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
```

`app.UseRateLimiter()` đặt sau `LanguageResolutionMiddleware`, trước `UseAuthentication` (xem thứ tự F3). Gắn `[EnableRateLimiting("public-submit")]` lên **2 controller** booking/contact. CAPTCHA ngoài phạm vi MVP.

### 4.7. DoD Phase 6

- POST booking hợp lệ → 201 `{referenceCode}` dạng `BK-260717-XXXXXX`; DB có 1 row booking + N row admin + 1 row guest `email_deliveries` (PENDING, đúng `related_entity_*`).
- Request thứ 6 trong 1 phút từ cùng IP → 429.
- `Website` có giá trị → 201 nhưng DB không có row nào.
- `roomTypeId` không tồn tại/ẩn → 400 có `errors.roomTypeId`.
- Email lưu trong DB đã lowercase+trim.

---

## 5. Phase 8 — Email subsystem (làm ngay sau Phase 6 để khép luồng M2)

> Làm trước Phase 7 vì M2 (thu lead end-to-end) có giá trị nghiệp vụ cao hơn admin CRUD.

### 5.1. Packages (thêm vào `HBP.Infrastructure.csproj`)

```xml
<PackageReference Include="MailKit" Version="4.8.0" />
<PackageReference Include="Scriban" Version="5.10.0" />
```

(Chốt version mới nhất 4.x/5.x tại thời điểm cài; `System.Net.Mail.SmtpClient` bị obsolete — không dùng.)

### 5.2. Deliverables

| File | Nội dung |
|---|---|
| `Application/Email/EmailContracts.cs` | `EmailTypes` (const strings), `IEmailTemplateRenderer`, record `RenderedEmail(string Subject, string HtmlBody)` |
| `Infrastructure/Email/SmtpOptions.cs` | options bind section `Smtp` |
| `Infrastructure/Email/SmtpEmailSender.cs` | impl `IEmailSender` bằng MailKit |
| `Infrastructure/Email/ScribanEmailTemplateRenderer.cs` | render template embedded |
| `Infrastructure/Email/Templates/**` | file `.sbn` (embedded resource) |
| `Api/HostedServices/EmailDispatchBackgroundService.cs` | worker poll + retry + retention |
| `Api/HostedServices/EmailDispatchOptions.cs` | `PollIntervalSeconds=30, BatchSize=10, MaxAttempts=6, RetentionDays=90` |

### 5.3. Cấu hình

```csharp
public sealed class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }          // CHỈ từ env: Smtp__Password
    public string Security { get; set; } = "StartTls";   // None | StartTls | SslOnConnect
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "";
}
```

Provider SMTP chưa chốt (TBD-TECH-005) → mọi thứ generic qua env, không code riêng cho provider nào. `Host` rỗng → worker log warning 1 lần rồi idle (giữ nguyên PENDING), **không crash**.

### 5.4. Template (Scriban, embedded resource)

- Đường dẫn: `Infrastructure/Email/Templates/{EmailType}/{lang}/subject.sbn` + `body.sbn`. Csproj: `<EmbeddedResource Include="Email\Templates\**\*.sbn" />`.
- Cần 6 bộ: `BOOKING_ADMIN_NOTIFICATION/vi`, `CONTACT_ADMIN_NOTIFICATION/vi`, `BOOKING_GUEST_CONFIRMATION/{vi,ja}`, `CONTACT_GUEST_CONFIRMATION/{vi,ja}`. Renderer fallback `ja` → `vi` nếu thiếu file.
- Model truyền vào template (Scriban object): `reference_code`, `full_name`, `email`, `phone_number`, `check_in_date`, `check_out_date`, `adults`, `children`, `number_of_rooms`, `room_type_name` (đã resolve theo lang), `customer_message` / `subject`, `message`, `site_name` (đọc `system_settings["site_metadata"].name`, fallback "HBP").
- Nội dung/brand thật chưa có (TBD-TECH-017/018) → viết template **placeholder sạch sẽ, song ngữ đúng ngôn ngữ**, một chỗ duy nhất để thay nội dung sau.

### 5.5. Worker — thuật toán dispatch (đặc tả chính xác)

`BackgroundService.ExecuteAsync` lặp mỗi `PollIntervalSeconds` (dùng `PeriodicTimer`), mỗi vòng tạo `IServiceScope` mới:

```
1. begin transaction
2. claim batch — raw SQL (EF8 SqlQuery cần alias "Value"):
   SELECT id AS "Value" FROM email_deliveries
   WHERE status = 'PENDING' OR (status = 'RETRYING' AND next_retry_at <= now())
   ORDER BY created_at
   LIMIT {BatchSize}
   FOR UPDATE SKIP LOCKED
   -- SKIP LOCKED: hiện 1 instance nhưng an toàn nếu sau này scale ngang / chạy 2 replica lúc deploy
3. load các EmailDelivery theo ids (tracking)
4. với từng row:
   a. load related entity theo (RelatedEntityType, RelatedEntityId)
      → không còn (đã xóa) → Status=FAILED, LastError="Related entity missing", continue
   b. render subject+body theo (EmailType, LanguageCode)
   c. IEmailSender.SendAsync(recipient, subject, body)
   d. thành công: Status=SENT, SentAt=now, LastAttemptAt=now
      thất bại (exception): AttemptCount++, LastAttemptAt=now,
         LastError = ex.Message cắt 1000 ký tự,
         nếu AttemptCount < MaxAttempts(6):
             Status=RETRYING, NextRetryAt = now + Backoff[AttemptCount-1]
         ngược lại: Status=FAILED, NextRetryAt=null
5. SaveChanges, commit
6. retention: nếu lần dọn gần nhất > 24h →
   DELETE FROM email_deliveries WHERE created_at < now() - interval '90 days'
```

**Bảng backoff (5 mốc, tổng 6 lần thử):**

| Lần thất bại thứ | 1 | 2 | 3 | 4 | 5 | 6 |
|---|---|---|---|---|---|---|
| Hành động | +1m | +5m | +30m | +2h | +6h | **FAILED** |

`Backoff = [1m, 5m, 30m, 2h, 6h]`. Mọi mốc thời gian từ `IClock`. Exception toàn vòng lặp phải catch + log — worker **không được chết** vì 1 email lỗi. Query claim trúng index `idx_email_deliveries_status` + partial `idx_email_deliveries_next_retry` (đã có).

### 5.6. MailKit sender (khung)

```csharp
using var client = new MailKit.Net.Smtp.SmtpClient();
await client.ConnectAsync(o.Host, o.Port, o.Security switch {
    "SslOnConnect" => SecureSocketOptions.SslOnConnect,
    "None"         => SecureSocketOptions.None,
    _              => SecureSocketOptions.StartTls }, ct);
if (!string.IsNullOrEmpty(o.Username)) await client.AuthenticateAsync(o.Username, o.Password, ct);
await client.SendAsync(message, ct);   // MimeMessage: From = FromName<FromAddress>, To, Subject, HtmlBody
await client.DisconnectAsync(true, ct);
```

Đăng ký: `services.Configure<SmtpOptions>(config.GetSection("Smtp"))`, `IEmailSender` **Scoped**, `IEmailTemplateRenderer` Singleton, `AddHostedService<EmailDispatchBackgroundService>()`.

### 5.7. DoD Phase 8

- Compose dev (Phase 10) có Mailpit: POST booking → ≤60s sau 2 email xuất hiện trong Mailpit UI, row chuyển `PENDING → SENT` + `sent_at` set.
- Tắt Mailpit → row chuyển `RETRYING`, `attempt_count=1`, `next_retry_at ≈ now+1m`; bật lại → SENT ở lần retry.
- Row RETRYING đủ 6 lần thất bại → FAILED, dashboard (Phase 7) đếm được.
- Email ja render template ja; admin luôn nhận vi.

---

## 6. Phase 7 — Admin CRUD API

Tất cả controller trong `Api/Controllers/Admin/`, `[Authorize]` class-level, route prefix `api/admin/...`. FE phải gửi header `X-HBP-CSRF` (đã có middleware). Contract + validator ở `Application/Admin{Feature}/` hoặc gộp `Application/Catalog/`, impl ở `Infrastructure/Catalog|Requests|Settings|Dashboard/`.

### 6.1. Convention chung cho list admin

`?page=&pageSize=&search=&sort=` — pageSize default 20 max 100. `sort` theo **whitelist từng resource** (map string→expression, ví dụ `displayOrder`, `-createdAt`; prefix `-` = DESC); giá trị ngoài whitelist → `ValidationException`. Search rỗng → bỏ filter.

### 6.2. Room types — `api/admin/rooms`

| Route | Hành vi |
|---|---|
| `GET` | Paged. Search ILike trên `code/slug/name_vi/name_ja` (bảng nhỏ, seq scan chấp nhận được — không cần thêm index). Sort whitelist: `displayOrder` (default), `createdAt`, `nameVi`. Filter `?isVisible=` optional |
| `GET {id}` | Full record song ngữ + `amenities: [{amenityId, displayOrder}]` + `media: [{mediaFileId, displayOrder, urls}]` + featured media summary |
| `POST` | Tạo mới. Trùng `code`/`slug` → 409 (`ConflictException`, catch 23505 hoặc pre-check) |
| `PUT {id}` | Full update (thay toàn bộ field scalar) |
| `DELETE {id}` | 204. FK tự lo: junction CASCADE, booking `SET NULL`. UI nên khuyến khích ẩn thay vì xóa (`is_visible=false`) |
| `PUT {id}/amenities` | **Replace-set**: body `[{amenityId, displayOrder}]` — trong txn xóa hết junction cũ, insert mới. Amenity id lạ → 400; trùng id trong body → 400 |
| `PUT {id}/media` | **Replace-set** tương tự với `[{mediaFileId, displayOrder}]` (unique (room, media) — dedupe trước) |

**Upsert request record:**

```csharp
public sealed record RoomTypeUpsertRequest(
    string Code, string Slug, string NameVi, string? NameJa,
    string? ShortDescriptionVi, string? ShortDescriptionJa,
    string? DescriptionVi, string? DescriptionJa,
    decimal? PriceVnd, decimal? PriceUsd, PriceDisplayMode PriceDisplayMode,
    int Capacity, decimal? AreaSquareMeters,
    string? BedDescriptionVi, string? BedDescriptionJa,
    Guid? FeaturedMediaId, int DisplayOrder, bool IsVisible,
    string? SeoTitleVi, string? SeoTitleJa, string? SeoDescriptionVi, string? SeoDescriptionJa);
```

**Validator:** `NameVi` NotEmpty ≤255; `Code` NotEmpty ≤50 regex `^[A-Z0-9_-]+$`; `Slug` NotEmpty ≤150 regex `^[a-z0-9]+(-[a-z0-9]+)*$`; `Capacity ≥1`; `DisplayOrder ≥0`; giá `≥0`; **`PriceDisplayMode == SHOW_PRICE` → `PriceVnd` bắt buộc** (`PriceUsd` optional); độ dài các field khớp cột schema.

**Rule featured image (TBD-TECH-007 — enforce tại đây, KHÔNG enforce lúc upload):** `FeaturedMediaId` có giá trị → load media, yêu cầu `Width >= 1200 && Height >= 800` (null coi như không đạt) → sai thì `ValidationException("featuredMediaId", "Featured image must be at least 1200x800.")`. Áp dụng cho cả Services.

### 6.3. Amenities — `api/admin/amenities`

CRUD chuẩn (`GET` paged + search `name_vi/name_ja`, `GET {id}`, `POST`, `PUT {id}`, `DELETE {id}` → 204, junction CASCADE tự gỡ khỏi rooms). Fields: `NameVi` req ≤150, `NameJa` ≤150, `Icon` ≤100, `DisplayOrder ≥0`, `IsVisible`.

### 6.4. Services — `api/admin/services`

Như rooms nhưng không có amenities/media junction/capacity/prices; có `PriceNoteVi/Ja` ≤255. `Slug` unique → 409. Featured image rule 1200×800 như §6.2.

### 6.5. Gallery — `api/admin/gallery-categories` + `api/admin/gallery-items`

- Categories: CRUD; `Slug` unique → 409; `NameVi` req ≤150. **DELETE cascade toàn bộ items** (DB đã CASCADE) — FE phải confirm.
- Items: `GET ?categoryId=&page=` , `POST`, `PUT {id}`, `DELETE {id}`. `MediaFileId` phải tồn tại (400 nếu không), `GalleryCategoryId` phải tồn tại, `CaptionVi/Ja` ≤255. Không giới hạn kích thước ảnh gallery.

### 6.6. Booking requests — `api/admin/booking-requests` (read-only)

| Route | Hành vi |
|---|---|
| `GET` | Paged, default `ORDER BY created_at DESC`. `?search=` ILike **OR** trên `full_name/email/phone_number` (trúng GIN trgm). `?from=&to=` (DateOnly, lọc `created_at`), `?roomTypeId=` |
| `GET {id}` | Detail + room type summary (id, code, nameVi) + `emailDeliveries[]` (query `related_entity_type='BookingRequest' AND related_entity_id=id`, order `created_at`) |

```csharp
public sealed record EmailDeliveryResponse(Guid Id, string EmailType, string Recipient,
    LanguageCode LanguageCode, EmailStatus Status, int AttemptCount, DateTime? NextRetryAt,
    DateTime? LastAttemptAt, DateTime? SentAt, string? LastError, DateTime CreatedAt);
```

Không có endpoint đổi status (enum chỉ có `RECEIVED` — chờ chốt câu hỏi mở §11; nếu chốt có, cần migration mở rộng enum riêng).

### 6.7. Contact requests — `api/admin/contact-requests`

List/search/detail y hệt booking (search trên `full_name/email`, thêm `subject` ILike không cần index — chấp nhận).

### 6.8. Settings — `api/admin/settings`

- `GET` → toàn bộ rows `[{key, value, description, updatedAt}]`.
- `PUT {key}` body `{value}` — **chỉ cho key nằm trong whitelist** `["notification_emails", "site_metadata"]`, key khác → 404. Validate: `notification_emails` = JSON array ≤20 email hợp lệ; `site_metadata` = JSON object hợp lệ. Không có secret nào trong bảng này (secret chỉ ở env — theo SRS).

### 6.9. Dashboard — `GET api/admin/dashboard`

```csharp
public sealed record DashboardResponse(EmailStatsResponse Emails,
    RequestStatsResponse Bookings, RequestStatsResponse Contacts);
public sealed record EmailStatsResponse(long Failed, long Pending, long Retrying);   // Failed = FR-DASH-001
public sealed record RequestStatsResponse(long Total, long Last7Days, long Last30Days);
```

Mốc 7/30 ngày tính từ `IClock.UtcNow` (`created_at >= now - 7d`). 6 count queries, `idx_*_created_at` + `idx_email_deliveries_status` cover đủ.

### 6.10. DoD Phase 7

- CRUD room đầy đủ vòng: tạo (409 khi trùng slug), gán amenities replace-set, gán media, set featured (ảnh nhỏ hơn 1200×800 → 400), ẩn/hiện phản ánh ngay ở public API (sau max-age 60s).
- Search booking theo số điện thoại một phần → ra kết quả (ILIKE `%…%`).
- `PUT settings/notification_emails` với email sai → 400; đúng → booking mới sinh row admin theo danh sách mới.
- Dashboard đếm khớp dữ liệu thật.

---

## 7. Phase 9 — Testing

### 7.1. `tests/HBP.UnitTests` (project đã có, đang RỖNG — thêm file test)

| File test | Nội dung assert |
|---|---|
| `ReferenceCodeGeneratorTests` | format `^BK-\d{6}-[0-9A-HJKMNP-TV-Z]{6}$`; alphabet đúng Crockford; 2 lần gọi khác nhau; prefix CT |
| `PasswordHasherAdapterTests` | hash→verify round-trip true; sai pass → false; 2 hash cùng pass khác nhau (salt) |
| `LocalizedTests` | Pick(Ja, vi, ja)=ja; Pick(Ja, vi, null)=vi; Pick(Ja, vi, "")=vi; Pick(Vi,...)=vi |
| `MediaUrlTests` | original.webp → medium/thumbnail đúng; URL có nhiều `/` |
| `EmailBackoffTests` | mốc 1m/5m/30m/2h/6h; lần 6 → FAILED (tách hàm backoff thuần để test) |
| `RoomTypeMappingTests` | mode CONTACT → PriceVnd/Usd null; SHOW_PRICE giữ giá (tách hàm mapping thuần) |
| `Validator tests` | booking: adults=0 fail, email sai fail, **check_out < check_in KHÔNG fail** (chốt BR-BOOK-014 bằng test); room: SHOW_PRICE thiếu PriceVnd fail; slug hoa fail |

> Thiết kế cho testability: backoff, mapping, localized để dạng **hàm thuần static** trong Application — không cần DB/mock.

### 7.2. `tests/HBP.IntegrationTests` (project MỚI — thêm vào `HBP.slnx`)

Packages: `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.PostgreSql`, `xunit`, `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`. Reference `HBP.Api` (có `public partial class Program` sẵn).

**Fixture chung (collection fixture):** `PostgreSqlContainer("postgres:16-alpine")` → `WebApplicationFactory<Program>` override config: `ConnectionStrings:HbpDatabase` = container; `Database:SeedOnStartup=false`; `Auth:CookieSecure=false`; `Media:StorageRoot` = thư mục temp; apply migration bằng `db.Database.MigrateAsync()` 1 lần.

| Test | Assert |
|---|---|
| `SchemaParityTests` | Sau migrate, query catalog: (1) `pg_indexes` chứa đúng tập tên index của `schema.sql` (uq_*, idx_*, kể cả trgm + partial); (2) `information_schema.triggers` chứa 10 trigger `trg_*`; (3) `pg_proc` có `set_updated_at`, `normalize_email`; (4) `pg_enum` đúng label 4 enum; (5) `admin_users` có 3 cột lockout. → **tự động hóa cổng parity Phase 1** |
| `AuthFlowTests` | login sai 5 lần trong 15p → lần 6 trả 423; login đúng → Set-Cookie `hbp.admin`; `GET me` với cookie → 200; mutation admin thiếu `X-HBP-CSRF` → 400; đủ → qua |
| `MediaUploadTests` | upload PNG thật → 201, 3 file .webp trên disk, response đủ 3 URL; upload file text → 400; xóa media đang gắn gallery item → 409 + `references`; gỡ item → xóa 204 + folder biến mất |
| `BookingSubmissionTests` | POST → 201; đếm đúng row email_deliveries (settings có 2 admin email → 3 row); email lowercase; honeypot → 201 nhưng 0 row; request 6 trong 1 phút → 429 |
| `PublicApiTests` | room ẩn không xuất hiện; `lang=ja` fallback vi; CONTACT không có key giá trong raw JSON; ETag → 304 |
| `AdminSearchTests` | ILike một phần tên/sđt ra đúng row; phân trang totalCount đúng |
| `EmailDispatchTests` | gọi trực tiếp 1 vòng xử lý của worker với `IEmailSender` fake: thành công → SENT; fake ném lỗi → RETRYING attempt=1 next_retry≈+1m; chạy đủ 6 lần lỗi → FAILED; related entity bị xóa → FAILED |
| `DashboardTests` | seed dữ liệu có kiểm soát → count khớp |

**Ưu tiên viết trước:** SchemaParity → BookingSubmission → AuthFlow → MediaUpload (in-use) → EmailDispatch. Test viết **ngay trong phase tương ứng**, không dể dồn cuối.

### 7.3. DoD Phase 9

`dotnet test` xanh toàn bộ trên máy có Docker (Testcontainers tự kéo image). Không test nào phụ thuộc thứ tự chạy.

---

## 8. Phase 10 — Containerization & Ops

### 8.1. `src/HBP.Api/Dockerfile` (multi-stage, non-root, port 8080)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY HBP.slnx Directory.Build.props ./
COPY src/HBP.Domain/HBP.Domain.csproj        src/HBP.Domain/
COPY src/HBP.Application/HBP.Application.csproj src/HBP.Application/
COPY src/HBP.Infrastructure/HBP.Infrastructure.csproj src/HBP.Infrastructure/
COPY src/HBP.Api/HBP.Api.csproj              src/HBP.Api/
RUN dotnet restore src/HBP.Api/HBP.Api.csproj
COPY src/ src/
RUN dotnet publish src/HBP.Api/HBP.Api.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
# user 'app' (uid 1654) có sẵn trong image .NET 8; volume /data/media phải chown 1654 khi mount lần đầu
USER app
ENTRYPOINT ["dotnet", "HBP.Api.dll"]
```

Build bằng image **sdk:8.0** (khớp target net8.0 — máy dev có SDK 9/10 vẫn OK, container là chuẩn).

### 8.2. `docker-compose.yml` (root — môi trường dev)

```yaml
services:
  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: hbp
      POSTGRES_USER: hbp
      POSTGRES_PASSWORD: hbp_dev_password
    ports: ["5432:5432"]
    volumes: [pgdata:/var/lib/postgresql/data]
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U hbp -d hbp"]
      interval: 5s
      timeout: 3s
      retries: 10

  mailpit:                       # SMTP sink dev (thay Mailhog — còn được maintain, UI :8025)
    image: axllent/mailpit
    ports: ["1025:1025", "8025:8025"]

  api:
    build: { context: ., dockerfile: src/HBP.Api/Dockerfile }
    depends_on:
      db: { condition: service_healthy }
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__HbpDatabase: Host=db;Port=5432;Database=hbp;Username=hbp;Password=hbp_dev_password
      Database__SeedOnStartup: "true"
      Database__MigrateOnStartup: "true"        # CHỈ dev/staging
      HBP_SEED_ADMIN_USERNAME: admin
      HBP_SEED_ADMIN_EMAIL: admin@example.com
      HBP_SEED_ADMIN_PASSWORD: ChangeMe123!
      Smtp__Host: mailpit
      Smtp__Port: "1025"
      Smtp__Security: None
      Smtp__FromAddress: no-reply@hbp.local
      Smtp__FromName: HBP Dev
      Media__StorageRoot: /data/media
      Media__BaseUrl: http://localhost:8080/media
      Cors__AllowedOrigins__0: http://localhost:3000
      Auth__CookieSecure: "false"
    ports: ["8080:8080"]
    volumes: [mediadata:/data/media]

volumes:
  pgdata:
  mediadata:
```

### 8.3. Migration khi deploy

- **Production (Coolify): KHÔNG auto-migrate-on-startup.** Bước pre-deploy chạy bundle:
  `dotnet ef migrations bundle --project src/HBP.Infrastructure --startup-project src/HBP.Api --self-contained -r linux-x64 -o artifacts/efbundle`
  rồi `./efbundle --connection "$ConnectionStrings__HbpDatabase"` (runtime image không cần SDK).
- **Dev/staging:** thêm block gated vào `Program.cs` (trước seed):

```csharp
if (app.Configuration.GetValue("Database:MigrateOnStartup", false))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<HbpDbContext>().Database.MigrateAsync();
}
```

### 8.4. Health & monitoring

Đã có sẵn: `/health` (liveness, không chạm DB) và `/health/ready` (Npgsql). Coolify healthcheck + Uptime Kuma trỏ `/health`. Log JSON ra console (Serilog CompactJsonFormatter) — Coolify đọc trực tiếp.

### 8.5. Backup (TBD-TECH-014)

- DB: Coolify Scheduled Backup cho Postgres resource — **daily, giữ ≥7 bản**, lưu ngoài container (S3-compatible nếu có).
- Media: cron `tar -czf /backups/media-$(date +%F).tar.gz -C /data media` + xoay 7 bản, đẩy ra ngoài VPS.
- **Quy trình restore (viết vào `docs/runbook.md` khi làm phase này):** (1) restore dump vào container Postgres mới, (2) untar media vào volume, (3) trỏ env connection string, (4) verify `/health/ready` + smoke §9.

### 8.6. Bảng env production (Coolify) — nguồn tham chiếu duy nhất

| Env | Ví dụ / ghi chú |
|---|---|
| `ConnectionStrings__HbpDatabase` | `Host=db;Port=5432;Database=hbp;Username=hbp;Password=***` |
| `Database__MigrateOnStartup` | **không set** ở prod (dùng bundle) |
| `Database__SeedOnStartup` | `true` lần đầu để tạo admin + settings, sau đó tắt |
| `HBP_SEED_ADMIN_USERNAME/EMAIL/PASSWORD` | seed lần đầu; **đổi mật khẩu sau lần login đầu** |
| `Smtp__Host/Port/Username/Password/Security/FromAddress/FromName` | theo provider chốt ở TBD-TECH-005 |
| `Cors__AllowedOrigins__0` | `https://www.<domain>` (TBD-TECH-015) |
| `Media__StorageRoot` | `/data/media` (volume) |
| `Media__BaseUrl` | `https://api.<domain>/media` |
| `Auth__CookieSecure` | `true` |

### 8.7. DoD Phase 10

`docker compose up --build` từ repo sạch → tự migrate + seed; smoke pass toàn bộ §9 checklist; image chạy non-root; restart container không mất media/DB.

---

## 9. Xác minh end-to-end (smoke checklist cuối)

1. `dotnet build HBP.slnx` → 0 error; `dotnet test` → xanh.
2. `docker compose up` → `/health` 200, `/health/ready` 200.
3. `POST /api/booking-requests` → 201 + 2..N row `email_deliveries` PENDING → ≤60s chuyển SENT, thấy mail trong Mailpit (vi/ja đúng ngôn ngữ).
4. Admin: `GET /csrf` → login → tạo room (SHOW_PRICE + giá) → upload ảnh (3 biến thể trên volume) → gán featured → `GET /api/rooms?lang=ja` thấy room + fallback + giá; đổi mode CONTACT → JSON mất field giá.
5. Xóa ảnh đang dùng → 409 kèm `references`; gỡ tham chiếu → xóa OK.
6. Search booking theo phone một phần → ra; dashboard đếm khớp; 6 request submit/phút → 429.

---

## 10. Thứ tự thực hiện

| Bước | Việc | Phụ thuộc | Ước lượng |
|---|---|---|---|
| 1 | **Phase F** — sửa 7 finding | — | 0.5 ngày |
| 2 | **Phase 9a** — dựng skeleton 2 project test + fixture Testcontainers + SchemaParityTests | F | 0.5–1 ngày |
| 3 | **Phase 5** — Public API + cache middleware (+ unit/integration test của phase) | F | 1.5–2 ngày |
| 4 | **Phase 6** — Booking/Contact + rate limit (+ test) | F | 1–1.5 ngày |
| 5 | **Phase 8** — Email worker + templates (+ test) | 6 | 1.5–2 ngày |
| 6 | **Phase 7** — Admin CRUD (+ test) | 4 (media đã có) | 2.5–3 ngày |
| 7 | **Phase 9b** — phủ nốt test còn thiếu | tất cả | 1 ngày |
| 8 | **Phase 10** — Dockerfile/compose/bundle/backup/runbook | tất cả | 1 ngày |

Mapping milestone: bước 1–3 ≈ **M1** (public đọc được), bước 4–5 ≈ **M2** (thu lead end-to-end), bước 6 ≈ **M3** (admin vận hành), bước 7–8 ≈ **M4** (hardening).

---

## 11. Câu hỏi mở — CẦN CHỐT trước phase liên quan

| # | Câu hỏi | Chặn phase | Ghi chú |
|---|---|---|---|
| 1 | `booking_request_status` chỉ có `RECEIVED` — admin có cần đánh dấu "đã xử lý/đóng"? | 7 (endpoint đổi status) | Nếu có: migration riêng `ALTER TYPE ... ADD VALUE` + endpoint `PATCH status` |
| 2 | SMTP provider (TBD-TECH-005) | 8 (chỉ chặn config, không chặn code) | Code generic qua env rồi |
| 3 | Domain thật (TBD-TECH-015) | 10 | Điền `Cors__AllowedOrigins`, `Media__BaseUrl`, cookie domain |
| 4 | Nội dung/brand email (TBD-TECH-017/018) | 8 (template placeholder trước) | Chỉ thay file `.sbn` |
| 5 | Admin nhận email thông báo bằng tiếng Việt (đang giả định) hay song ngữ? | 8 | Hiện đặc tả: admin luôn `vi` |

---

## Phụ lục A — Catalog endpoint đầy đủ

### Public (no auth, có rate-limit ở POST)

| Method | Route | Trả về |
|---|---|---|
| GET | `/api/rooms?lang=` | `RoomTypeListItemResponse[]` |
| GET | `/api/rooms/{slug}?lang=` | `RoomTypeDetailResponse` / 404 |
| GET | `/api/services?lang=` | `ServiceListItemResponse[]` |
| GET | `/api/services/{slug}?lang=` | `ServiceDetailResponse` / 404 |
| GET | `/api/gallery?lang=&category=` | `GalleryCategoryResponse[]` |
| GET | `/api/amenities?lang=` | `AmenityResponse[]` |
| POST | `/api/booking-requests` | 201 `{referenceCode}` / 400 / 429 |
| POST | `/api/contact-requests` | 201 `{referenceCode}` / 400 / 429 |
| GET | `/health`, `/health/ready` | liveness / readiness |

### Admin (`[Authorize]` + CSRF header cho mutation)

| Method | Route | Ghi chú |
|---|---|---|
| GET | `/api/admin/auth/csrf` | phát token double-submit |
| POST | `/api/admin/auth/login` | 200 / 401 / **423** locked |
| POST | `/api/admin/auth/logout` · GET `/me` | |
| POST/GET/GET{id}/DELETE{id} | `/api/admin/media` | upload multipart / paged / detail / 409 in-use |
| GET, GET{id}, POST, PUT{id}, DELETE{id} | `/api/admin/rooms` | 409 trùng code/slug |
| PUT | `/api/admin/rooms/{id}/amenities` | replace-set |
| PUT | `/api/admin/rooms/{id}/media` | replace-set |
| GET, GET{id}, POST, PUT{id}, DELETE{id} | `/api/admin/amenities` | |
| GET, GET{id}, POST, PUT{id}, DELETE{id} | `/api/admin/services` | 409 trùng slug |
| GET, GET{id}, POST, PUT{id}, DELETE{id} | `/api/admin/gallery-categories` | DELETE cascade items |
| GET, GET{id}, POST, PUT{id}, DELETE{id} | `/api/admin/gallery-items` | `?categoryId=` |
| GET, GET{id} | `/api/admin/booking-requests` | search/from/to/roomTypeId |
| GET, GET{id} | `/api/admin/contact-requests` | search/from/to |
| GET, PUT{key} | `/api/admin/settings` | whitelist key |
| GET | `/api/admin/dashboard` | FR-DASH-001 |

## Phụ lục B — Cấu trúc thư mục đích (file mới in đậm)

```
src/HBP.Application/
  Abstractions/            (đã có — không đổi)
  Auth/                    (đã có)
  Common/  ApiError, Exceptions, PagedResult, **Localized.cs**, **MediaUrl.cs**
  **Public/PublicContracts.cs**
  **Requests/RequestContracts.cs, CreateBookingRequestValidator.cs, CreateContactRequestValidator.cs**
  **Email/EmailContracts.cs**
  **Catalog/**  (RoomTypeAdminContracts, AmenityAdminContracts, ServiceAdminContracts, GalleryAdminContracts + validators)
  **Settings/SettingsContracts.cs**
  **Dashboard/DashboardContracts.cs**
  Media/                   (sửa MediaResponse — F4)

src/HBP.Infrastructure/
  Auth/ Common/ Media/ Persistence/   (đã có)
  **Public/**      (4 query services)
  **Requests/**    (ReferenceCodeGenerator, BookingRequestService, ContactRequestService + admin read services)
  **Email/**       (SmtpOptions, SmtpEmailSender, ScribanEmailTemplateRenderer, Templates/**)
  **Catalog/**     (RoomTypeAdminService, AmenityAdminService, ServiceAdminService, GalleryAdminService)
  **Settings/** **Dashboard/**

src/HBP.Api/
  Controllers/  AuthController (đã có), **RoomTypesController, ServicesController, GalleryController, AmenitiesController, BookingRequestsController, ContactRequestsController**
  Controllers/Admin/  MediaController (đã có), **RoomsController, AmenitiesController, ServicesController, GalleryCategoriesController, GalleryItemsController, BookingRequestsController, ContactRequestsController, SettingsController, DashboardController**
  Infrastructure/  (đã có) + **PublicCacheMiddleware.cs**
  **HostedServices/EmailDispatchBackgroundService.cs, EmailDispatchOptions.cs**
  **Dockerfile**

tests/
  HBP.UnitTests/           (đã có project — **thêm toàn bộ file test §7.1**)
  **HBP.IntegrationTests/** (project mới — §7.2, nhớ thêm vào HBP.slnx)
docs/implementation-plan.md (tài liệu này)
```


# BE_HBP — Deployment Verification & Acceptance Checklist

Tài liệu nghiệm thu cho back end Hotel Booking Portal (HBP). Dùng trực tiếp làm checklist khi
triển khai lên một máy đã cài đủ môi trường (Docker, PostgreSQL 16, .NET SDK 8, EF Core CLI,
Mailhog). Mỗi hạng mục ghi rõ mục đích, các bước, kết quả mong đợi, tiêu chí Pass/Fail, mức ưu
tiên và cách xác minh.

---

## 1. Tổng quan

### 1.1 Phạm vi hệ thống được nghiệm thu

| Thành phần | Nội dung |
|---|---|
| Solution | `HBP.slnx` — 4 project `src` (Domain, Application, Infrastructure, Api) + 2 project `tests` |
| Database | PostgreSQL 16, 13 bảng, 4 enum, 2 function, 10 trigger, 5 GIN pg_trgm index |
| Migration | `InitialCreate`, `AddLoginLockout` |
| API công khai | 6 endpoint đọc + 2 endpoint submit |
| API admin | 8 nhóm endpoint dưới `/api/admin` (cookie auth + CSRF) |
| Middleware | ForwardedHeaders, ExceptionHandler, SerilogRequestLogging, CORS, StaticFiles(`/media`), LanguageResolution, RateLimiter, Authentication, AdminCsrf, Authorization, PublicCache |
| Background job | `EmailDispatchBackgroundService` — gửi email có retry + dọn dữ liệu quá 90 ngày |
| Hạ tầng | `src/HBP.Api/Dockerfile`, `docker-compose.yml` (api + postgres + mailhog) |

### 1.2 Ký hiệu cách xác minh

| Mã | Ý nghĩa |
|---|---|
| **CR** | Xác nhận được bằng **code review**, không cần chạy hệ thống |
| **AUTO** | Có **test tự động**; ghi rõ test đã tồn tại hay cần viết thêm |
| **ENV** | **Bắt buộc chạy trên môi trường thật** (cần Docker/PostgreSQL/SMTP) |
| **MAN** | **Kiểm thử thủ công** (curl/Postman/Swagger/trình duyệt) |

Một hạng mục có thể mang nhiều mã. Ví dụ `CR + AUTO(có)` nghĩa là đọc code xác nhận được và đã
có test tự động phủ.

### 1.3 Mức ưu tiên

| Mức | Nghĩa khi Fail |
|---|---|
| **Critical** | Chặn bàn giao. Không được go-live. |
| **High** | Chặn bàn giao trừ khi có phương án xử lý được chấp thuận bằng văn bản. |
| **Medium** | Ghi nhận, xử lý trong sprint kế tiếp. |
| **Low** | Cải thiện, không chặn. |

### 1.4 Cách ghi kết quả

Mỗi dòng checklist điền: `Pass / Fail / N-A`, người kiểm tra, ngày, và link bằng chứng (log,
ảnh chụp màn hình, output lệnh). Một hạng mục chỉ được tính Pass khi **có bằng chứng đính kèm**.

---

## 2. Điều kiện tiên quyết

Xác nhận trước khi bắt đầu, nếu thiếu thì dừng.

| ID | Điều kiện | Cách xác minh | Kết quả mong đợi | Ưu tiên |
|---|---|---|---|---|
| PRE-01 | .NET SDK 8.0 | `dotnet --list-sdks` | Có dòng `8.0.xxx` | Critical |
| PRE-02 | Docker + Compose | `docker --version`, `docker compose version` | Cả hai trả về phiên bản, daemon đang chạy | Critical |
| PRE-03 | EF Core CLI khớp provider | `dotnet tool restore` rồi `dotnet ef --version` | `8.0.11` (pin trong `.config/dotnet-tools.json`) | Critical |
| PRE-04 | PostgreSQL 16 truy cập được | `psql "$CONN" -c "select version()"` | Trả về PostgreSQL 16.x | Critical |
| PRE-05 | Quyền tạo extension | `psql "$CONN" -c "create extension if not exists pgcrypto"` | Không lỗi permission | Critical |
| PRE-06 | Mailhog (hoặc SMTP sink) | Mở `http://localhost:8025` | Giao diện Mailhog hiển thị | High |
| PRE-07 | Cổng trống | `8080` (api), `5432` (db), `1025`/`8025` (mailhog) | Không bị chiếm bởi tiến trình khác | High |
| PRE-08 | Dung lượng đĩa cho volume media | `df -h` | Còn tối thiểu 5 GB | Medium |
| PRE-09 | Biến bí mật đã chuẩn bị | Danh sách ở mục 3.2 | Có đủ giá trị thật, không dùng giá trị mẫu | Critical |
| PRE-10 | Đã chốt domain/origin thật | Xác nhận với chủ dự án | Có giá trị cho `Cors__AllowedOrigins` | High |

> **Lưu ý:** hai điểm còn treo trong Plan — nhà cung cấp SMTP thật (TBD-TECH-005) và nội dung/brand
> email (TBD-TECH-017/018) — không chặn nghiệm thu kỹ thuật, nhưng phải được ghi nhận là "chấp nhận
> có điều kiện" nếu chưa chốt.

---

## 3. Checklist trước khi triển khai (Pre-deployment)

### 3.1 Build, restore, publish

| ID | Mục đích | Các bước | Kết quả mong đợi | Pass/Fail | Ưu tiên | Xác minh |
|---|---|---|---|---|---|---|
| BLD-01 | Restore đầy đủ package | `dotnet restore HBP.slnx` | Kết thúc không lỗi, không cảnh báo về package thiếu/xung đột phiên bản | Fail nếu có bất kỳ lỗi restore nào | Critical | ENV |
| BLD-02 | Build sạch toàn solution | `dotnet build HBP.slnx -c Release` | `0 Error(s)`, `0 Warning(s)` | Fail nếu có error; Warning > 0 → ghi nhận High | Critical | ENV |
| BLD-03 | Publish được artefact chạy | `dotnet publish src/HBP.Api/HBP.Api.csproj -c Release -o out` | Thư mục `out` có `HBP.Api.dll` và các `.sbn` được nhúng trong assembly | Fail nếu publish lỗi | Critical | ENV |
| BLD-04 | Target framework đúng | Đọc các `.csproj` | Tất cả `net8.0` | Fail nếu lệch | High | CR |
| BLD-05 | Build tất định | `Directory.Build.props` có `Deterministic=true`, `Nullable=enable`, `ImplicitUsings=enable` | Đủ 3 thuộc tính | Fail nếu thiếu | Medium | CR |
| BLD-06 | Template email được nhúng | `HBP.Infrastructure.csproj` có `<EmbeddedResource Include="Email\Templates\**\*.sbn" />` | Có, và assembly xuất bản chứa 12 resource `.sbn` | Fail nếu renderer không tìm thấy template lúc chạy | High | CR + ENV |
| BLD-07 | Unit test xanh | `dotnet test tests/HBP.UnitTests` | 34/34 pass (hoặc nhiều hơn nếu bổ sung) | Fail nếu có test đỏ | Critical | AUTO (có) |
| BLD-08 | `.dockerignore` loại trừ rác | Đọc `.dockerignore` | Có `**/bin`, `**/obj`, `.git`, `artifacts` | Fail nếu thiếu → image phình và có nguy cơ lộ file | Medium | CR |

### 3.2 Cấu hình `appsettings` và biến môi trường

| ID | Mục đích | Các bước | Kết quả mong đợi | Pass/Fail | Ưu tiên | Xác minh |
|---|---|---|---|---|---|---|
| CFG-01 | Không có secret trong repo | `git grep -nE "Password=|Smtp__Password" -- '*.json'` | Chỉ thấy placeholder `<password>` trong `appsettings.json` và mật khẩu dev trong `appsettings.Development.json` | Fail nếu có secret production bị commit | Critical | CR |
| CFG-02 | Connection string production | Đặt `ConnectionStrings__HbpDatabase` qua env | API khởi động không ném `InvalidOperationException` | Fail nếu app crash lúc boot | Critical | ENV |
| CFG-03 | CORS đúng origin thật | Đặt `Cors__AllowedOrigins__0` (và `__1`, …) | Danh sách khớp domain front end | **Fail nếu để rỗng** — policy sẽ không cho phép origin nào, front end mất toàn bộ API | Critical | CR + ENV |
| CFG-04 | Cookie bảo mật ở production | `Auth__CookieSecure=true` | Cookie `hbp.admin` có cờ `Secure` | Fail nếu `false` trên môi trường HTTPS | Critical | CR + MAN |
| CFG-05 | Đường dẫn media | `Media__StorageRoot=/data/media`, `Media__BaseUrl` là URL tuyệt đối tới domain API | Ảnh tải về được từ front end khác origin | Fail nếu `BaseUrl` còn là `/media` tương đối trong khi FE khác domain | High | CR + MAN |
| CFG-06 | Cấu hình SMTP | `Smtp__Host/Port/Security/FromAddress/FromName`, `Smtp__Username/Password` chỉ qua env | Worker gửi được email | Fail nếu `Smtp__Host` rỗng (worker sẽ đứng yên và chỉ log cảnh báo một lần) | Critical | CR + ENV |
| CFG-07 | Tham số worker email | `EmailDispatch__PollIntervalSeconds/BatchSize/MaxAttempts/RetentionDays` | Mặc định 30 / 10 / 6 / 90 | Fail nếu `RetentionDays` khác 90 mà không có phê duyệt | Medium | CR |
| CFG-08 | Tắt auto-migrate ở production | `RUN_MIGRATIONS_ON_STARTUP` **không** đặt hoặc `false` | Log khởi động không có câu lệnh migration | Fail nếu `true` trên production | High | CR + ENV |
| CFG-09 | Seed admin lần đầu | `HBP_SEED_ADMIN_USERNAME/EMAIL/PASSWORD` + `Database__SeedOnStartup=true` cho lần chạy đầu | Tạo đúng 1 admin | Fail nếu không tạo được, hoặc nếu vẫn bật seed sau khi đã có admin | High | ENV |
| CFG-10 | Seed chỉ chạy khi bảng rỗng | Chạy lại container lần 2 | Không tạo thêm admin, không nhân bản dữ liệu mẫu | Fail nếu dữ liệu bị nhân đôi | High | CR + ENV |
| CFG-11 | Connection string design-time | `HBP_DESIGN_CONNECTION` | `dotnet ef` chạy được độc lập với host Api | Fail nếu `dotnet ef` đòi khởi động Api | Medium | ENV |
| CFG-12 | `AllowedHosts` | Đọc `appsettings.json` | `*` — chấp nhận vì đã có reverse proxy chặn phía trước | Fail nếu triển khai không qua proxy | Low | CR |

### 3.3 Dependency Injection và cấu hình pipeline

| ID | Mục đích | Các bước | Kết quả mong đợi | Pass/Fail | Ưu tiên | Xác minh |
|---|---|---|---|---|---|---|
| DI-01 | Mọi interface đều có đăng ký | Khởi động API và gọi thử **mỗi** controller một lần | Không có `InvalidOperationException: Unable to resolve service` | Fail nếu bất kỳ endpoint nào lỗi 500 do DI | Critical | ENV + MAN |
| DI-02 | Đủ 8 service admin | `AddInfrastructure` đăng ký `IAdminRoomTypeService`, `IAdminAmenityService`, `IAdminServiceCatalogService`, `IAdminGalleryService`, `IAdminBookingRequestService`, `IAdminContactRequestService`, `IAdminSystemSettingService`, `IAdminDashboardService` | Đủ 8 dòng `AddScoped` | Fail nếu thiếu | Critical | CR |
| DI-03 | Vòng đời đúng | `HbpDbContext` và service dùng DbContext là `Scoped`; `IClock`, `IPasswordHasher`, `IImageProcessor`, `IMediaStorage`, `IEmailTemplateRenderer` là `Singleton` | Không có captive dependency (singleton giữ scoped) | Fail nếu một singleton nhận `HbpDbContext` | Critical | CR |
| DI-04 | Worker dùng scope riêng | `EmailDispatchBackgroundService` tạo `IServiceScopeFactory.CreateAsyncScope()` mỗi vòng lặp | Không dùng DbContext dài hạn | Fail nếu resolve DbContext ở constructor | Critical | CR |
| DI-05 | Validator được nạp tự động | `AddValidatorsFromAssemblyContaining` trong `HBP.Application` | Mọi validator trong assembly được đăng ký | Fail nếu một DTO có validator nhưng không được áp dụng | High | CR + AUTO (có) |
| DI-06 | Thứ tự middleware | Đọc `Program.cs` | ForwardedHeaders → ExceptionHandler → SerilogRequestLogging → (Swagger) → CORS → StaticFiles → Language → RateLimiter → Authentication → AdminCsrf → Authorization → PublicCache → MapControllers | Fail nếu `UseAuthentication` sau `AdminCsrfMiddleware` (CSRF sẽ luôn bỏ qua) hoặc `UseCors` sau endpoint | Critical | CR |
| DI-07 | Options binding | `MediaOptions` ← `Media`, `SmtpOptions` ← `Smtp`, `EmailDispatchOptions` ← `EmailDispatch` | Giá trị env phản ánh đúng lúc chạy | Fail nếu đổi env không có tác dụng | High | CR + ENV |

### 3.4 Migration và schema

| ID | Mục đích | Các bước | Kết quả mong đợi | Pass/Fail | Ưu tiên | Xác minh |
|---|---|---|---|---|---|---|
| MIG-01 | Sinh script migration | `dotnet ef migrations script --project src/HBP.Infrastructure --startup-project src/HBP.Api -o artifacts/initialcreate.sql` | Sinh file, không lỗi | Fail nếu lỗi | Critical | ENV |
| MIG-02 | Không còn migration chờ tạo | `dotnet ef migrations has-pending-model-changes` | Không có thay đổi model chưa được đưa vào migration | Fail nếu báo có → snapshot lệch entity | Critical | ENV |
| MIG-03 | Áp migration lên DB trắng | `dotnet ef database update` | Tạo đủ 13 bảng + `__EFMigrationsHistory` với 2 dòng | Fail nếu lỗi hoặc thiếu bảng | Critical | ENV |
| MIG-04 | Migration reversible | `dotnet ef database update 0` rồi update lại | `Down()` chạy sạch, update lại thành công | Fail nếu `Down()` để sót function/trigger | High | ENV |
| MIG-05 | **Cổng parity với `schema.sql`** | Theo `docs/migration-verification.md`: dựng 2 DB, một áp `docs/schema.sql`, một áp script EF, `pg_dump --schema-only --no-owner --no-privileges` cả hai rồi `diff -u` | Chỉ còn các khác biệt đã liệt kê trong mục "Accepted differences" | **Fail nếu có bất kỳ khác biệt nào ngoài danh sách** — phải sửa ở `Persistence/Configurations`, không vá bằng raw SQL | Critical | ENV |
| MIG-06 | Extension | `select extname from pg_extension` | Có `pgcrypto` và `pg_trgm` | Fail nếu thiếu | Critical | AUTO (có: `SchemaParityTests`) |
| MIG-07 | Function + trigger | `select proname from pg_proc where proname in ('set_updated_at','normalize_email')`; đếm trigger `trg_%` | 2 function, 10 trigger | Fail nếu thiếu | Critical | AUTO (có) |
| MIG-08 | Functional unique index | `\d admin_users` | Có `uq_admin_users_username_lower` và `uq_admin_users_email_lower` trên `lower(...)` | Fail nếu là unique index thường | High | ENV |
| MIG-09 | GIN pg_trgm index | `select indexname from pg_indexes where indexname like '%_trgm'` | Đủ 5 index | Fail nếu thiếu | High | AUTO (có) |
| MIG-10 | Cột lockout | `\d admin_users` | Có `failed_count`, `first_failed_at`, `locked_until` | Fail nếu thiếu → login luôn lỗi cột không tồn tại | Critical | AUTO (có) |
| MIG-11 | Enum PostgreSQL | `select typname from pg_type where typtype='e'` | `price_display_mode`, `booking_request_status`, `email_status`, `language_code_enum` | Fail nếu thiếu → mọi query enum lỗi | Critical | ENV |
| MIG-12 | Bundle migration cho deploy | `dotnet ef migrations bundle --self-contained -r linux-x64 -o artifacts/migrate` | Sinh file thực thi chạy được không cần SDK | Fail nếu không build được bundle | High | ENV |

### 3.5 Docker và Compose

| ID | Mục đích | Các bước | Kết quả mong đợi | Pass/Fail | Ưu tiên | Xác minh |
|---|---|---|---|---|---|---|
| DOC-01 | Build image | `docker build -f src/HBP.Api/Dockerfile -t hbp-api .` (context = thư mục gốc repo) | Build thành công | Fail nếu lỗi; **lưu ý sai context là lỗi thường gặp nhất** | Critical | ENV |
| DOC-02 | Multi-stage đúng base | Đọc Dockerfile | `sdk:8.0` để build, `aspnet:8.0` để chạy | Fail nếu runtime image chứa SDK | High | CR |
| DOC-03 | Chạy non-root | `docker run --rm hbp-api id` | UID không phải 0 (user `app`) | Fail nếu chạy bằng root | High | ENV |
| DOC-04 | Quyền ghi volume media | Mount volume rồi upload ảnh | Ghi được vào `/data/media` | Fail nếu `UnauthorizedAccessException` | Critical | ENV |
| DOC-05 | Cổng expose | `docker inspect` | Expose 8080, `ASPNETCORE_HTTP_PORTS=8080` | Fail nếu lệch | High | CR + ENV |
| DOC-06 | Compose khởi động đủ | `docker compose up -d` | 3 service `db`, `mailhog`, `api` đều `running`; `db` `healthy` trước khi `api` start | Fail nếu api start trước khi db sẵn sàng | Critical | ENV |
| DOC-07 | Volume bền vững | `docker compose down` (không `-v`) rồi `up -d` | Dữ liệu DB và ảnh còn nguyên | Fail nếu mất dữ liệu | Critical | ENV |
| DOC-08 | Kích thước image hợp lý | `docker images hbp-api` | Dưới ~350 MB | Fail nếu vượt xa → kiểm tra `.dockerignore` | Low | ENV |

---

## 4. Checklist sau khi triển khai (Post-deployment smoke test)

Chạy tuần tự ngay sau khi container lên. Nếu bất kỳ mục Critical nào Fail thì rollback.

| ID | Mục đích | Các bước | Kết quả mong đợi | Pass/Fail | Ưu tiên | Xác minh |
|---|---|---|---|---|---|---|
| SMK-01 | Liveness | `curl -i http://localhost:8080/health` | `200`, body `{"status":"healthy"}`, **không chạm DB** | Fail nếu khác 200 | Critical | ENV + MAN |
| SMK-02 | Readiness | `curl -i http://localhost:8080/health/ready` | `200 Healthy` khi DB sống | Fail nếu 503 trong khi DB đang chạy | Critical | ENV + MAN |
| SMK-03 | Readiness phản ánh sự cố DB | `docker compose stop db` rồi gọi `/health/ready`; sau đó `start db` | Trả `503` khi DB tắt, `/health` vẫn `200` | Fail nếu `/health/ready` vẫn 200 khi DB chết, hoặc `/health` cũng chết theo | High | ENV + MAN |
| SMK-04 | Log khởi động sạch | `docker compose logs api` | Không có exception; log ở dạng JSON một dòng | Fail nếu có exception chưa xử lý | Critical | ENV |
| SMK-05 | Swagger chỉ ở Development | Gọi `/swagger` ở môi trường Production | `404` ở Production, hiển thị đầy đủ ở Development | Fail nếu Production lộ Swagger | High | CR + MAN |
| SMK-06 | Swagger liệt kê đủ endpoint | Ở Development mở `/swagger` | Thấy đủ nhóm public + 8 nhóm admin; không có schema id trùng | Fail nếu SwaggerGen ném lỗi conflicting schemaIds | High | MAN |
| SMK-07 | Public đọc được | `GET /api/rooms`, `/api/services`, `/api/gallery`, `/api/amenities` | `200`, trả mảng JSON camelCase | Fail nếu 500 | Critical | ENV + AUTO (có: `PublicApiTests`) |
| SMK-08 | Submit hoạt động | `POST /api/booking-requests` hợp lệ | `201` kèm `referenceCode` | Fail nếu khác 201 | Critical | AUTO (có: `BookingSubmissionTests`) |
| SMK-09 | Admin login | Lấy CSRF rồi `POST /api/admin/auth/login` | `200` + cookie `hbp.admin` | Fail nếu 401 với credential đúng | Critical | AUTO (có: `AdminAuthTests`) |
| SMK-10 | Upload ảnh | `POST /api/admin/media` với ảnh JPEG ≥1200×800 | `201`, sinh 3 file trên volume | Fail nếu lỗi hoặc thiếu biến thể | Critical | AUTO (có: `AdminContentTests`) |
| SMK-11 | Phục vụ ảnh tĩnh | Mở `publicUrl` trả về từ SMK-10 | `200`, `Content-Type: image/webp`, `Cache-Control: public,max-age=31536000,immutable` | Fail nếu 404 | Critical | MAN |
| SMK-12 | Worker email đang chạy | Xem log sau ~1 phút | Không có exception lặp lại từ `EmailDispatchBackgroundService` | Fail nếu vòng lặp liên tục ném lỗi | Critical | ENV |
| SMK-13 | Email tới Mailhog | Submit booking, đợi 1 chu kỳ poll | Mailhog nhận đủ email | Fail nếu sau 3 chu kỳ vẫn `PENDING` | Critical | ENV + MAN |

---

## 5. Checklist kiểm thử chức năng

### 5.1 Xác thực và phân quyền (Authentication / Authorization)

| ID | Mục đích | Các bước | Kết quả mong đợi | Pass/Fail | Ưu tiên | Xác minh |
|---|---|---|---|---|---|---|
| AUTH-01 | Đăng nhập bằng username | `POST /api/admin/auth/login` với username đúng | `200` + thông tin admin | Fail nếu 401 | Critical | AUTO (có) |
| AUTH-02 | Đăng nhập bằng email | Dùng email thay username | `200` — service so khớp cả hai, không phân biệt hoa thường | Fail nếu 401 | High | CR + AUTO (cần bổ sung) |
| AUTH-03 | Sai mật khẩu | Mật khẩu sai | `401`, **không tiết lộ** tài khoản có tồn tại hay không | Fail nếu thông điệp phân biệt "sai user" và "sai pass" | High | CR + MAN |
| AUTH-04 | Chống dò tài khoản theo thời gian | Đăng nhập với user không tồn tại | Vẫn thực hiện một lần verify hash giả (`_dummyHash`) | Fail nếu thời gian phản hồi chênh lệch rõ rệt | Medium | CR |
| AUTH-05 | Tài khoản bị vô hiệu hóa | Đặt `is_active=false` rồi đăng nhập | `401` | Fail nếu vào được | High | CR + MAN |
| AUTH-06 | Khóa sau 5 lần sai | 5 lần sai liên tiếp trong 15 phút | Lần thứ 5 trả `423`; sau đó **mật khẩu đúng cũng bị `423`** | Fail nếu vẫn đăng nhập được | Critical | AUTO (có) |
| AUTH-07 | Cửa sổ đếm 15 phút | Đặt `first_failed_at` lùi hơn 15 phút rồi sai tiếp | Bộ đếm reset về 1, không khóa | Fail nếu vẫn cộng dồn | Medium | ENV |
| AUTH-08 | Reset khi đăng nhập thành công | Sai 2 lần rồi đăng nhập đúng | `failed_count=0`, `first_failed_at=null`, `locked_until=null`, `last_login_at` cập nhật | Fail nếu còn số dư | High | ENV |
| AUTH-09 | Thuộc tính cookie | Kiểm tra `Set-Cookie` | `hbp.admin`; `HttpOnly`; `SameSite=Lax`; `Secure` khi `Auth__CookieSecure=true` | Fail nếu thiếu `HttpOnly` | Critical | MAN |
| AUTH-10 | Hết hạn tuyệt đối 8 giờ | `ExpireTimeSpan=8h`, `SlidingExpiration=false` | Phiên hết hạn đúng 8 giờ kể từ lúc đăng nhập, không gia hạn khi hoạt động | Fail nếu phiên trượt | High | CR |
| AUTH-11 | `me` và `logout` | `GET /api/admin/auth/me`, `POST .../logout`, rồi `me` lại | `200` → `204` → `401` | Fail nếu vẫn 200 sau logout | High | AUTO (có) |
| AUTH-12 | Chặn ẩn danh | Gọi mọi endpoint `/api/admin/**` khi chưa đăng nhập | `401` (không phải 302 redirect) | Fail nếu bất kỳ endpoint nào trả 200 | Critical | AUTO (có, một phần) |
| AUTH-13 | **Mọi controller admin có `[Authorize]`** | `git grep -L "\[Authorize\]" src/HBP.Api/Controllers/Admin/` | Không controller nào bị bỏ sót | Fail nếu có → đây là rủi ro hồi quy chính khi thêm controller mới | Critical | CR |
| AUTH-14 | Băm mật khẩu | Kiểm tra `password_hash` trong DB | Chuỗi PBKDF2 của ASP.NET Identity, dài < 255, không phải plaintext | Fail nếu thấy plaintext | Critical | AUTO (có) + ENV |

### 5.2 CSRF

| ID | Mục đích | Các bước | Kết quả mong đợi | Pass/Fail | Ưu tiên | Xác minh |
|---|---|---|---|---|---|---|
| CSRF-01 | Lấy token | `GET /api/admin/auth/csrf` | `200` + cookie `hbp.csrf` (không HttpOnly) + token trong body | Fail nếu không set cookie | High | AUTO (có) |
| CSRF-02 | Thiếu header bị chặn | POST/PUT/DELETE admin khi đã đăng nhập nhưng không gửi `X-HBP-CSRF` | `400 Invalid CSRF token` | Fail nếu thực hiện được thao tác | Critical | AUTO (có) |
| CSRF-03 | Header sai giá trị | Gửi header khác cookie | `400` | Fail nếu qua được | Critical | MAN |
| CSRF-04 | GET không bị ảnh hưởng | `GET /api/admin/rooms` không kèm header | `200` | Fail nếu bị chặn | High | AUTO (có) |
| CSRF-05 | Login được miễn trừ | Đăng nhập không có cookie CSRF trước đó | Không bị chặn bởi middleware | Fail nếu không đăng nhập được lần đầu | Critical | CR + AUTO (có) |
| CSRF-06 | Ẩn danh không bị 400 nhầm | POST admin khi chưa đăng nhập | `401` (do `[Authorize]`), không phải 400 | Fail nếu trả 400 gây nhầm lẫn chẩn đoán | Low | CR |

### 5.3 Đa ngôn ngữ (i18n)

| ID | Mục đích | Các bước | Kết quả mong đợi | Pass/Fail | Ưu tiên | Xác minh |
|---|---|---|---|---|---|---|
| I18N-01 | Mặc định tiếng Việt | `GET /api/rooms` không có header | Trả trường `*_vi` | Fail nếu trả tiếng Nhật hoặc null | High | AUTO (có) |
| I18N-02 | `Accept-Language: ja` | Gửi header | Trả `*_ja` khi có | Fail nếu bỏ qua header | High | MAN |
| I18N-03 | `?lang=` ghi đè header | `Accept-Language: vi` + `?lang=ja` | Ưu tiên query, trả tiếng Nhật | Fail nếu header thắng | High | AUTO (có) |
| I18N-04 | Fallback khi thiếu bản Nhật | Bản ghi có `name_ja = null`, gọi `?lang=ja` | Trả `name_vi` (vì `*_vi` NOT NULL) | Fail nếu trả null hoặc chuỗi rỗng | Critical | AUTO (có) |
| I18N-05 | Giá trị lạ | `?lang=fr` | Rơi về `vi`, không lỗi | Fail nếu 500 | Medium | MAN |
| I18N-06 | Ngôn ngữ ảnh hưởng cache | Phản hồi có `Vary: Accept-Language` | Có header `Vary` | Fail nếu thiếu → proxy trả sai ngôn ngữ cho người dùng khác | High | CR + MAN |

### 5.4 Media

| ID | Mục đích | Các bước | Kết quả mong đợi | Pass/Fail | Ưu tiên | Xác minh |
|---|---|---|---|---|---|---|
| MED-01 | Upload hợp lệ | JPEG/PNG/WebP ≤5 MB | `201`, DB có `media_files`, sinh `original/medium/thumbnail.webp` | Fail nếu thiếu biến thể | Critical | AUTO (có) |
| MED-02 | Kích thước biến thể | Kiểm tra file trên đĩa | `medium` rộng tối đa 800px, `thumbnail` tối đa 400px, `original` giữ nguyên kích thước gốc, tất cả định dạng WebP | Fail nếu sai kích thước hoặc sai định dạng | High | ENV + MAN |
| MED-03 | Quá 5 MB | Upload file 6 MB | `400` (hoặc `413` do `RequestSizeLimit`) | Fail nếu chấp nhận | High | MAN |
| MED-04 | MIME không hỗ trợ | Upload PDF | `400 Unsupported image type` | Fail nếu chấp nhận | High | MAN |
| MED-05 | File giả mạo phần mở rộng | Đặt `Content-Type: image/png` cho nội dung không phải ảnh | `400` (bắt `UnknownImageFormatException`) | Fail nếu 500 | High | AUTO (có) |
| MED-06 | Cấu trúc thư mục | Xem volume | `/data/media/{yyyy}/{MM}/{mediaId:N}/` | Fail nếu khác | Medium | ENV |
| MED-07 | `public_url` khớp `storage_path` | So sánh 2 cột trong DB với file thật | Mở `public_url` tải được đúng file | Fail nếu lệch | Critical | MAN |
| MED-08 | Danh sách phân trang | `GET /api/admin/media?page=1&pageSize=20` | `PagedResult`, sắp xếp `created_at` giảm dần | Fail nếu sai thứ tự | Medium | MAN |
| MED-09 | **Không xóa ảnh đang dùng** | Gán ảnh làm featured / đưa vào `room_type_media` / `gallery_items` rồi xóa | `409` kèm mảng `references` liệt kê đúng nơi đang dùng | Fail nếu xóa được → dữ liệu hỏng | Critical | AUTO (có) |
| MED-10 | Lớp bảo vệ 2 ở DB | Xóa trực tiếp bằng SQL một `media_files` đang được tham chiếu | PostgreSQL chặn bằng `ON DELETE RESTRICT` | Fail nếu xóa được | High | ENV |
| MED-11 | Xóa ảnh tự do | Xóa ảnh không ai dùng | `204`, xóa cả row và 3 file | Fail nếu còn file mồ côi | High | AUTO (có) |
| MED-12 | Xóa file lỗi không làm hỏng giao dịch | Khóa quyền thư mục rồi xóa | Row bị xóa, ghi log cảnh báo "files orphaned", API vẫn `204` | Fail nếu 500 | Medium | ENV |
| MED-13 | Ảnh featured tối thiểu 1200×800 | Gán ảnh 400×300 làm `featuredMediaId` của room/service | `400` với lỗi ở trường `featuredMediaId` | Fail nếu chấp nhận | High | AUTO (có) |
| MED-14 | Ảnh gallery không bị giới hạn kích thước | Thêm ảnh nhỏ vào gallery item | `201` | Fail nếu bị chặn — Plan chỉ giới hạn ảnh featured | Medium | AUTO (có) |
| MED-15 | Không stream ảnh qua controller | Kiểm tra `Program.cs` | Phục vụ bằng `UseStaticFiles` trên volume | Fail nếu có controller trả file | Medium | CR |

### 5.5 Booking / Contact submission

| ID | Mục đích | Các bước | Kết quả mong đợi | Pass/Fail | Ưu tiên | Xác minh |
|---|---|---|---|---|---|---|
| SUB-01 | Tạo booking | POST hợp lệ | `201 { referenceCode }` khớp `^BK-\d{6}-[0-9A-HJKMNP-TV-Z]{6}$` | Fail nếu sai định dạng | Critical | AUTO (có) |
| SUB-02 | Tạo contact | POST hợp lệ | `201` với tiền tố `CT-` | Fail nếu sai | Critical | AUTO (cần bổ sung khẳng định định dạng) |
| SUB-03 | Bảng chữ cái không nhập nhằng | Đọc `ReferenceCodeGenerator` | Không chứa `I`, `L`, `O`, `U` | Fail nếu có | Low | CR |
| SUB-04 | Chống trùng mã | Chèn sẵn một mã rồi ép sinh trùng | Thử lại tối đa 3 lần, cuối cùng vẫn `201` | Fail nếu trả 500 ngay lần đầu | Medium | AUTO (có, unit) |
| SUB-05 | Tính nguyên tử của giao dịch | Gây lỗi khi ghi `email_deliveries` | **Không** còn `booking_requests` mồ côi | Fail nếu request được lưu mà không có email | Critical | ENV |
| SUB-06 | Chuẩn hóa email | Gửi `"  GUEST@EXAMPLE.COM "` | DB lưu `guest@example.com` (trigger `normalize_email` + trim ở app) | Fail nếu lưu nguyên | High | AUTO (có) |
| SUB-07 | **Không** validate ngày trả > ngày nhận | Gửi `checkOut` trước `checkIn` | `201` — đúng BR-BOOK-014, cố ý không kiểm tra | **Fail nếu bị chặn** (ai đó "sửa nhầm") | High | CR + AUTO (cần bổ sung) |
| SUB-08 | `roomTypeId` phải tồn tại và hiển thị | Gửi id của phòng `is_visible=false` | `400` | Fail nếu chấp nhận | High | MAN |
| SUB-09 | Honeypot | Gửi kèm `website` có giá trị | `201` giả (mã ngẫu nhiên), **không** ghi DB, có log cảnh báo | Fail nếu ghi DB hoặc trả lỗi lộ cơ chế | High | AUTO (có) |
| SUB-10 | Rate limit theo IP | 6 lần POST trong 1 phút từ cùng IP | Lần thứ 6 trả `429` + `Retry-After: 60` | Fail nếu không giới hạn | High | AUTO (có) |
| SUB-11 | Rate limit tách theo IP | Hai IP khác nhau cùng gửi | Không ảnh hưởng lẫn nhau | Fail nếu chặn nhầm | Medium | AUTO (có) |
| SUB-12 | IP thật sau proxy | Kiểm tra `ForwardedHeaders` | Rate limit phân vùng theo `X-Forwarded-For`, không phải IP proxy | Fail nếu mọi người dùng chung một hạn mức | Critical | CR + ENV |
| SUB-13 | Trạng thái mặc định | Kiểm tra DB | `status = 'RECEIVED'` | Fail nếu khác | Medium | AUTO (có) |

### 5.6 Validation

| ID | Mục đích | Các bước | Kết quả mong đợi | Pass/Fail | Ưu tiên | Xác minh |
|---|---|---|---|---|---|---|
| VAL-01 | Định dạng lỗi thống nhất | Gửi payload thiếu trường bắt buộc | `400` RFC7807 ProblemDetails, có `errors` dạng `{ field: [messages] }` | Fail nếu định dạng khác | High | AUTO (có) |
| VAL-02 | Booking: trường bắt buộc | Thiếu `fullName`/`email`/`phoneNumber` | `400` liệt kê đủ các trường sai | Fail nếu chỉ báo trường đầu tiên | High | AUTO (có) |
| VAL-03 | Booking: ràng buộc số | `adults=0`, `children=-1`, `numberOfRooms=0` | `400` | Fail nếu chấp nhận | High | AUTO (có) |
| VAL-04 | Email và số điện thoại | Email sai định dạng, phone chứa chữ | `400` | Fail nếu chấp nhận | High | AUTO (có) |
| VAL-05 | Room: bắt buộc có giá khi `SHOW_PRICE` | Tạo room `SHOW_PRICE` không có `priceVnd` lẫn `priceUsd` | `400` | Fail nếu chấp nhận | High | AUTO (có) |
| VAL-06 | Room: `CONTACT` không cần giá | Tạo room `CONTACT` không giá | `201` | Fail nếu bị chặn | High | AUTO (có) |
| VAL-07 | Slug đúng kebab-case | `Phong-Deluxe`, `phong deluxe`, `phong--deluxe`, `-phong` | Cả 4 đều `400` | Fail nếu lọt | Medium | AUTO (có) |
| VAL-08 | Giới hạn độ dài khớp DDL | Gửi `nameVi` 300 ký tự (cột `varchar(255)`) | `400` từ validator, **không** để lỗi 500 từ PostgreSQL | Fail nếu 500 | High | AUTO (một phần) |
| VAL-09 | `displayOrder` không âm | Gửi `-1` | `400` | Fail nếu chấp nhận | Medium | AUTO (có) |
| VAL-10 | Replace-set kiểm tra id | `PUT rooms/{id}/amenities` với id không tồn tại | `400` liệt kê id thiếu, **không** xóa mất liên kết cũ | Fail nếu xóa sạch rồi mới lỗi | High | CR + AUTO (cần bổ sung) |
| VAL-11 | Body rỗng / JSON hỏng | Gửi `{}` và JSON sai cú pháp | `400`, không phải 500 | Fail nếu 500 | Medium | MAN |
| VAL-12 | Enum sai giá trị | `priceDisplayMode: "FREE"` | `400` | Fail nếu 500 | Medium | MAN |

---

## 6. Checklist kiểm thử API

### 6.1 Bảng liệt kê endpoint

Mọi endpoint dưới đây phải được gọi ít nhất một lần trong quá trình nghiệm thu.

**Công khai — không cần xác thực**

| ID | Method + Path | Kết quả mong đợi | Ưu tiên | Xác minh |
|---|---|---|---|---|
| API-01 | `GET /api/rooms` | `200`, chỉ phòng `is_visible=true`, sắp theo `display_order`; `CONTACT` **không có** trường giá | Critical | AUTO (có) |
| API-02 | `GET /api/rooms/{slug}` | `200` với amenities và media đã sắp thứ tự, đủ 3 biến thể ảnh, khối `seo`; slug ẩn/không tồn tại → `404` | Critical | AUTO (có) |
| API-03 | `GET /api/services` | `200`, sắp theo `display_order` | High | MAN |
| API-04 | `GET /api/services/{slug}` | `200`; không tồn tại → `404` | High | MAN |
| API-05 | `GET /api/gallery` | `200`, chỉ category hiển thị và item hiển thị, đúng thứ tự | High | MAN |
| API-06 | `GET /api/gallery?category={slug}` | `200`, lọc đúng category | Medium | MAN |
| API-07 | `GET /api/amenities` | `200` | Medium | MAN |
| API-08 | `POST /api/booking-requests` | `201 { referenceCode }` | Critical | AUTO (có) |
| API-09 | `POST /api/contact-requests` | `201 { referenceCode }` | Critical | AUTO (có) |
| API-10 | `GET /health` | `200` | Critical | ENV |
| API-11 | `GET /health/ready` | `200` / `503` | Critical | ENV |
| API-12 | `GET /media/{...}.webp` | `200 image/webp` | Critical | MAN |

**Admin — yêu cầu cookie + CSRF cho mọi thao tác thay đổi dữ liệu**

| ID | Method + Path | Kết quả mong đợi | Ưu tiên | Xác minh |
|---|---|---|---|---|
| API-20 | `GET /api/admin/auth/csrf` | `200` + cookie | High | AUTO (có) |
| API-21 | `POST /api/admin/auth/login` | `200` / `401` / `423` | Critical | AUTO (có) |
| API-22 | `POST /api/admin/auth/logout` | `204` | High | AUTO (có) |
| API-23 | `GET /api/admin/auth/me` | `200` / `401` | High | AUTO (có) |
| API-24 | `POST /api/admin/media` (multipart, field `file`) | `201` | Critical | AUTO (có) |
| API-25 | `GET /api/admin/media` | `200 PagedResult` | Medium | MAN |
| API-26 | `GET /api/admin/media/{id}` | `200` / `404` | Medium | MAN |
| API-27 | `DELETE /api/admin/media/{id}` | `204` / `409` | Critical | AUTO (có) |
| API-28 | `GET /api/admin/rooms` | `200 PagedResult` | Critical | AUTO (có) |
| API-29 | `GET /api/admin/rooms/{id}` | `200` (có amenities + media) / `404` | Critical | AUTO (có) |
| API-30 | `POST /api/admin/rooms` | `201` + `Location`; trùng `code`/`slug` → `409` | Critical | AUTO (có) |
| API-31 | `PUT /api/admin/rooms/{id}` | `200`; trùng với bản ghi khác → `409` | Critical | MAN |
| API-32 | `DELETE /api/admin/rooms/{id}` | `204` | High | AUTO (có) |
| API-33 | `PUT /api/admin/rooms/{id}/amenities` | `200`, thay toàn bộ tập liên kết kèm `displayOrder` | High | AUTO (có) |
| API-34 | `PUT /api/admin/rooms/{id}/media` | `200`, thay toàn bộ danh sách ảnh có thứ tự; gọi 2 lần liên tiếp vẫn `200` | High | AUTO (có) |
| API-35 | `GET/POST/PUT/DELETE /api/admin/amenities[/{id}]` | CRUD đầy đủ | High | AUTO (một phần) |
| API-36 | `GET/POST/PUT/DELETE /api/admin/services[/{id}]` | CRUD đầy đủ; trùng slug → `409` | High | MAN |
| API-37 | `GET/POST/PUT/DELETE /api/admin/gallery/categories[/{id}]` | CRUD đầy đủ; trùng slug → `409` | High | AUTO (một phần) |
| API-38 | `GET/POST/PUT/DELETE /api/admin/gallery/items[/{id}]`, lọc `?categoryId=` | CRUD đầy đủ; id tham chiếu sai → `400` | High | AUTO (có) |
| API-39 | `GET /api/admin/booking-requests` | `200 PagedResult`, mặc định `created_at DESC` | Critical | AUTO (có) |
| API-40 | `GET /api/admin/booking-requests/{id}` | `200` kèm room type và `emailDeliveries` | Critical | AUTO (có) |
| API-41 | `GET /api/admin/contact-requests[/{id}]` | Tương tự | High | AUTO (một phần) |
| API-42 | `GET /api/admin/settings`, `GET/PUT /api/admin/settings/{key}` | `200`; key dạng secret → `400`; key lạ khi GET → `404` | High | AUTO (có) |
| API-43 | `GET /api/admin/dashboard` | `200` với 5 chỉ số | High | AUTO (có) |

### 6.2 Quy ước chung của API

| ID | Mục đích | Các bước | Kết quả mong đợi | Pass/Fail | Ưu tiên | Xác minh |
|---|---|---|---|---|---|---|
| CVT-01 | JSON camelCase | Xem bất kỳ phản hồi nào | Tên trường camelCase | Fail nếu PascalCase | High | AUTO (có) |
| CVT-02 | Enum dạng chuỗi | Xem `priceDisplayMode`, `status`, `languageCode` | Chuỗi (`"CONTACT"`), không phải số | Fail nếu là số | High | AUTO (có) |
| CVT-03 | Phân trang | `?page=0&pageSize=5000` | Trả về `page=1`, `pageSize=100` | Fail nếu nhận nguyên giá trị | High | AUTO (có) |
| CVT-04 | `PagedResult` đủ trường | Xem body | `items`, `page`, `pageSize`, `totalCount`, `totalPages` | Fail nếu thiếu | Medium | CR |
| CVT-05 | Sort theo whitelist | `?sort=<chuỗi rác>` | Rơi về thứ tự mặc định, không lỗi, **không** cho phép SQL injection qua tên cột | Fail nếu 500 hoặc nhận cột tùy ý | Critical | CR + MAN |
| CVT-06 | Tìm kiếm không phân biệt hoa thường | `?search=NGUYEN` | Khớp cả `nguyen` | Fail nếu không khớp | High | AUTO (có) |
| CVT-07 | Tìm kiếm ký tự đặc biệt | `?search=%` và `?search=_` | Không lỗi, không trả toàn bộ bảng ngoài ý muốn | Ghi nhận nếu ký tự wildcard của `ILIKE` không được escape | Medium | MAN |
| CVT-08 | ETag và cache cho endpoint public | Gọi 2 lần, lần 2 kèm `If-None-Match` | Lần 1 `200` + `ETag` + `Cache-Control: public,max-age=60`; lần 2 `304` body rỗng | Fail nếu không có ETag → Next.js ISR kém hiệu quả | High | AUTO (có) |
| CVT-09 | Endpoint admin **không** bị cache | Xem header phản hồi admin | Không có `Cache-Control: public` | Fail nếu dữ liệu admin bị cache công khai | High | CR + MAN |
| CVT-10 | CORS preflight | `OPTIONS` từ origin hợp lệ | `204` + `Access-Control-Allow-Credentials: true` + đúng origin | Fail nếu trả `*` cùng credentials | Critical | MAN |
| CVT-11 | CORS chặn origin lạ | Gọi từ origin không có trong danh sách | Không có header `Access-Control-Allow-Origin` | Fail nếu cho qua | Critical | MAN |

### 6.3 Xử lý lỗi (Exception Handling)

| ID | Mục đích | Các bước | Kết quả mong đợi | Pass/Fail | Ưu tiên | Xác minh |
|---|---|---|---|---|---|---|
| ERR-01 | 404 nghiệp vụ | Lấy bản ghi không tồn tại | `404` ProblemDetails, `title = "Resource not found"` | Fail nếu 500 | High | AUTO (có) |
| ERR-02 | 400 validation | Payload sai | `400`, `title = "Validation failed"`, có `errors` | Fail nếu khác | High | AUTO (có) |
| ERR-03 | 409 xung đột | Trùng `code`/`slug` | `409`, `title = "Conflict"` | Fail nếu 500 | High | AUTO (có) |
| ERR-04 | 409 media đang dùng | Xóa ảnh đang dùng | `409` + mảng `references` | Fail nếu thiếu `references` | High | AUTO (có) |
| ERR-05 | **Không rò rỉ chi tiết nội bộ** | Gây lỗi 500 (ví dụ tắt DB rồi gọi API) | `500` với `detail = null`, **không** có stack trace hay câu SQL | Fail nếu lộ nội bộ | Critical | CR + ENV |
| ERR-06 | Lỗi 500 vẫn được ghi log | Xem log sau ERR-05 | Có bản ghi lỗi kèm đầy đủ exception phía server | Fail nếu nuốt lỗi im lặng | High | ENV |
| ERR-07 | Content-Type chuẩn | Xem phản hồi lỗi | `application/problem+json` | Fail nếu `text/html` | Medium | MAN |

---

## 7. Checklist kiểm thử Database

| ID | Mục đích | Các bước | Kết quả mong đợi | Pass/Fail | Ưu tiên | Xác minh |
|---|---|---|---|---|---|---|
| DB-01 | Đủ bảng | `\dt` | 13 bảng nghiệp vụ + `__EFMigrationsHistory` | Fail nếu thiếu | Critical | ENV |
| DB-02 | Trigger `updated_at` | `UPDATE room_types SET name_vi=name_vi WHERE id=...` | `updated_at` tự tăng | Fail nếu không đổi | High | ENV |
| DB-03 | Trigger chuẩn hóa email | `INSERT` với email viết hoa và có khoảng trắng | Lưu về chữ thường đã trim | Fail nếu giữ nguyên | High | AUTO (có) |
| DB-04 | Unique reference code | Chèn 2 booking cùng `reference_code` | Bản thứ hai bị chặn | Fail nếu chèn được | High | ENV |
| DB-05 | Unique không phân biệt hoa thường của admin | Chèn `Admin` khi đã có `admin` | Bị chặn bởi index `lower(username)` | Fail nếu chèn được | High | ENV |
| DB-06 | Ràng buộc CHECK | Chèn `adults=0`, `capacity=0`, `display_order=-1`, `size_bytes=-1` | Đều bị chặn | Fail nếu lọt | High | ENV |
| DB-07 | CHECK loại thực thể của email | Chèn `related_entity_type='Order'` | Bị chặn | Fail nếu lọt | Medium | ENV |
| DB-08 | Cascade khi xóa room type | Xóa một room type có amenities và media | `room_type_amenities`, `room_type_media` bị xóa theo; `booking_requests.room_type_id` chuyển `NULL` | Fail nếu lỗi FK hoặc còn bản ghi rác | High | ENV |
| DB-09 | RESTRICT bảo vệ media | Xem mục MED-10 | Chặn xóa | High | ENV |
| DB-10 | Kiểu timestamptz và múi giờ | Chèn qua API rồi đọc `created_at` | Lưu ở UTC, khớp thời điểm thực tế | Fail nếu lệch múi giờ | High | ENV |
| DB-11 | Bản ghi email đúng số lượng | Submit 1 booking khi `notification_emails` có 2 địa chỉ | **3 dòng** `email_deliveries`: 1 dòng cho **mỗi** người nhận admin + 1 dòng cho khách | Fail nếu khác — lưu ý Plan viết "2 bản ghi" vì giả định chỉ 1 người nhận | High | AUTO (có) |
| DB-12 | Liên kết đa hình | Xem `email_deliveries` | `related_entity_type` + `related_entity_id` trỏ đúng bản ghi (không có FK ở DB, do tầng ứng dụng đảm bảo) | Fail nếu trỏ sai | High | AUTO (có) |
| DB-13 | Ngôn ngữ email khách | Submit với `languageCode=ja` | Dòng khách có `language_code='ja'`, dòng admin `'vi'` | Fail nếu sai | Medium | ENV |
| DB-14 | Seed idempotent | Khởi động lại app 3 lần với `SeedOnStartup=true` | Số lượng admin, setting và dữ liệu mẫu không đổi | Fail nếu nhân bản | High | ENV |
| DB-15 | Seed không ghi đè sửa đổi | Sửa dữ liệu mẫu rồi khởi động lại | Thay đổi được giữ nguyên | Fail nếu bị ghi đè | High | ENV |
| DB-16 | Setting mặc định tồn tại | `SELECT * FROM system_settings` | Có `notification_emails` và `site_metadata` | Fail nếu thiếu → không gửi được email admin | Critical | ENV |
| DB-17 | Index được sử dụng | `EXPLAIN ANALYZE SELECT ... WHERE full_name ILIKE '%nguyen%'` (sau khi có đủ dữ liệu) | Kế hoạch dùng `Bitmap Index Scan` trên index trgm | Ghi nhận Fail (Medium) nếu `Seq Scan` trên bảng lớn | Medium | ENV |
| DB-18 | Không có N+1 ở public detail | Bật log SQL rồi gọi `GET /api/rooms/{slug}` | Số câu lệnh cố định (dùng `AsSplitQuery`), không tăng theo số amenity | Fail nếu số query tỉ lệ với số bản ghi con | Medium | ENV |
| DB-19 | Backup phục hồi được | Theo `docs/deployment.md`: `pg_dump` → tạo DB mới → `pg_restore` | Ứng dụng chạy bình thường trên DB phục hồi | **Fail nếu chưa từng diễn tập restore** | Critical | ENV |
| DB-20 | Backup media đồng bộ với DB | Restore cặp DB + media cùng ngày | Ảnh của mọi `media_files` đều tồn tại | Fail nếu ảnh 404 | High | ENV |

---

## 8. Checklist kiểm thử Background Job

| ID | Mục đích | Các bước | Kết quả mong đợi | Pass/Fail | Ưu tiên | Xác minh |
|---|---|---|---|---|---|---|
| JOB-01 | Worker khởi động cùng API | Xem log lúc boot | `EmailDispatchBackgroundService` chạy, không tách container (đúng SRS) | Fail nếu không chạy | Critical | CR + ENV |
| JOB-02 | Chu kỳ poll | Đặt `PollIntervalSeconds=15`, quan sát | Quét đúng chu kỳ | Fail nếu không tuân theo cấu hình | Medium | ENV |
| JOB-03 | Lấy đúng tập cần gửi | Chuẩn bị các dòng `PENDING`, `RETRYING` (đến hạn), `RETRYING` (chưa đến hạn), `SENT`, `FAILED` | Chỉ xử lý `PENDING` và `RETRYING` đã đến hạn | Fail nếu gửi lại `SENT` | Critical | ENV |
| JOB-04 | Giới hạn lô | Tạo 25 dòng `PENDING`, `BatchSize=10` | Mỗi vòng xử lý tối đa 10 | Fail nếu xử lý hết một lần | Medium | ENV |
| JOB-05 | An toàn khi chạy nhiều instance | Chạy 2 container API cùng DB, tạo nhiều dòng chờ | `FOR UPDATE SKIP LOCKED` đảm bảo **không email nào bị gửi 2 lần** | Fail nếu người nhận nhận trùng | Critical | ENV |
| JOB-06 | Gửi thành công | Mailhog nhận được thư | `status=SENT`, `sent_at` có giá trị, `last_error=null`, `next_retry_at=null` | Fail nếu trạng thái không đổi | Critical | ENV |
| JOB-07 | Thất bại chuyển RETRYING | Tắt Mailhog rồi submit | `status=RETRYING`, `attempt_count=1`, `next_retry_at ≈ now+1 phút`, `last_error` có nội dung | Fail nếu chuyển thẳng `FAILED` | Critical | ENV |
| JOB-08 | Backoff lũy tiến | Quan sát qua các lần thử | 1m → 5m → 30m → 2h → 6h | Fail nếu sai chuỗi | High | AUTO (có, unit) |
| JOB-09 | Dừng ở lần thứ 6 | Ép lỗi đủ số lần (hoặc đặt `attempt_count=5` rồi để chạy) | Sang `FAILED`, `next_retry_at=null`, không thử lại nữa | Fail nếu lặp vô hạn | High | ENV |
| JOB-10 | Cắt ngắn thông điệp lỗi | Gây lỗi có thông điệp rất dài | `last_error` ≤ 1000 ký tự, không vi phạm `varchar(1000)` | Fail nếu lỗi ghi DB | Medium | CR |
| JOB-11 | Thiếu thực thể liên quan | Xóa booking nhưng giữ dòng email | Dòng chuyển `FAILED` với lý do "Related entity missing", worker không sập | Fail nếu vòng lặp chết | High | ENV |
| JOB-12 | Worker chịu lỗi | Tắt DB trong lúc worker chạy | Ghi log lỗi rồi tiếp tục ở vòng sau, tiến trình không thoát | Fail nếu ứng dụng crash | Critical | ENV |
| JOB-13 | SMTP chưa cấu hình | Để `Smtp__Host` rỗng | Worker đứng yên và chỉ log cảnh báo **một lần**, không spam log | Fail nếu log tràn hoặc crash | Medium | CR + ENV |
| JOB-14 | Dọn dữ liệu 90 ngày | Chèn `email_deliveries` với `created_at` cách đây 100 ngày, khởi động lại app | Bị xóa trong lần chạy retention đầu tiên | Fail nếu còn | High | ENV |
| JOB-15 | Retention không xóa nhầm | Chèn bản ghi 80 ngày tuổi | Vẫn còn | Fail nếu bị xóa | High | ENV |
| JOB-16 | Retention chỉ chạy mỗi 24 giờ | Quan sát log | Không chạy mỗi vòng poll | Fail nếu chạy liên tục gây tải DB | Medium | CR |
| JOB-17 | Tắt máy êm | `docker compose stop api` giữa chừng | Thoát trong thời gian chờ mặc định, không để giao dịch treo | Fail nếu phải kill -9 | Medium | ENV |

> **Ghi chú về "Scheduled Jobs":** hệ thống không dùng cron/Quartz. Chỉ có một tác vụ định kỳ duy
> nhất là retention 90 ngày, nằm trong cùng `BackgroundService` (JOB-14 → JOB-16). Việc sao lưu
> `pg_dump` hằng ngày là job **bên ngoài** do Coolify lập lịch — nghiệm thu ở DB-19.

---

## 9. Checklist kiểm thử Email

| ID | Mục đích | Các bước | Kết quả mong đợi | Pass/Fail | Ưu tiên | Xác minh |
|---|---|---|---|---|---|---|
| EML-01 | Gửi hai chiều | Submit 1 booking | Admin nhận thông báo **và** khách nhận xác nhận | Fail nếu thiếu một chiều | Critical | ENV + MAN |
| EML-02 | Người nhận admin lấy từ DB | Đổi `notification_emails` qua `PUT /api/admin/settings/notification_emails` rồi submit | Thư đi tới danh sách mới, không cần khởi động lại | Fail nếu dùng giá trị cũ | High | ENV |
| EML-03 | Nhiều người nhận admin | Đặt 3 địa chỉ | Sinh 3 dòng thông báo admin, cả 3 đều nhận được | Fail nếu chỉ 1 | High | ENV |
| EML-04 | Danh sách rỗng | Đặt `notification_emails = []` | Vẫn gửi cho khách, ghi log cảnh báo, **không** ném lỗi ra API | Fail nếu submit thất bại | High | CR + ENV |
| EML-05 | Khử trùng lặp và chuẩn hóa | Đặt `["A@x.com","a@x.com"," a@x.com "]` | Chỉ 1 dòng `a@x.com` | Fail nếu gửi 3 lần | Medium | CR |
| EML-06 | Template tiếng Việt | Submit `languageCode=vi` | Nội dung tiếng Việt, biến đã được thay thế (không còn `{{ }}`) | Fail nếu còn placeholder thô | Critical | MAN |
| EML-07 | Template tiếng Nhật | Submit `languageCode=ja` | Xác nhận cho khách bằng tiếng Nhật | Fail nếu ra tiếng Việt | High | MAN |
| EML-08 | Fallback template | Thông báo admin (chỉ có bản `vi`) | Render bằng `vi`, không lỗi | Fail nếu `NotFoundException` | High | AUTO (có) |
| EML-09 | Đủ 4 loại email | Submit cả booking và contact | `BOOKING_ADMIN_NOTIFICATION`, `BOOKING_GUEST_CONFIRMATION`, `CONTACT_ADMIN_NOTIFICATION`, `CONTACT_GUEST_CONFIRMATION` | Fail nếu thiếu loại nào | High | ENV |
| EML-10 | Nội dung có mã tham chiếu | Xem thư trong Mailhog | Chứa đúng `referenceCode` trả về cho khách | Fail nếu sai mã | Critical | MAN |
| EML-11 | Thông tin đặt phòng đầy đủ | Xem thư thông báo admin | Có tên, email, điện thoại, ngày nhận/trả, lời nhắn | Fail nếu thiếu trường quan trọng | High | MAN |
| EML-12 | Tên site từ setting | Đặt `site_metadata = {"name":"Khách sạn X"}` | `site_name` trong thư hiển thị đúng | Fail nếu vẫn là "HBP" | Medium | ENV |
| EML-13 | `site_metadata` hỏng | Đặt giá trị JSON không hợp lệ | Rơi về mặc định "HBP", không sập worker | Fail nếu crash | Medium | CR |
| EML-14 | From address | Xem header thư | Khớp `Smtp__FromAddress` / `FromName` | Fail nếu rỗng | High | MAN |
| EML-15 | Thân thư HTML | Xem trong Mailhog | Hiển thị đúng HTML, không bị escape thành chữ | Fail nếu hiện thẻ thô | Medium | MAN |
| EML-16 | Chế độ bảo mật SMTP | Thử `StartTls` với SMTP thật | Kết nối thành công | Fail nếu chỉ chạy được với `None` | High | ENV |
| EML-17 | Xác thực SMTP | Đặt `Smtp__Username/Password` | Xác thực thành công | Fail nếu bị từ chối | High | ENV |
| EML-18 | Mật khẩu SMTP không bị log | `grep` toàn bộ log | Không thấy mật khẩu | Fail nếu lộ | Critical | ENV |

> **Điều kiện chấp nhận có bảo lưu:** nội dung và thương hiệu email (TBD-TECH-017/018) chưa chốt.
> Cơ chế render đã hoàn chỉnh; nội dung marketing có thể thay sau bằng cách sửa file `.sbn` mà
> không đụng tới mã nguồn. Nếu chấp nhận trong tình trạng này, phải ghi vào biên bản nghiệm thu.

---

## 10. Checklist kiểm thử Logging

| ID | Mục đích | Các bước | Kết quả mong đợi | Pass/Fail | Ưu tiên | Xác minh |
|---|---|---|---|---|---|---|
| LOG-01 | Định dạng JSON một dòng | `docker compose logs api` | Mỗi dòng là JSON hợp lệ (`RenderedCompactJsonFormatter`) — Coolify đọc được | Fail nếu log nhiều dòng/định dạng văn bản | High | CR + ENV |
| LOG-02 | Có request logging | Gọi vài API | Mỗi request có một dòng với method, path, status code, thời gian xử lý | Fail nếu thiếu | High | ENV |
| LOG-03 | Truy vết theo request | Xem log của một request lỗi | Có thể lần ra toàn bộ dòng log thuộc cùng một request (`RequestId`/correlation) | Fail nếu không tương quan được | Medium | ENV |
| LOG-04 | Mức log hợp lý | Chạy tải bình thường | Không spam `Information` cho từng truy vấn SQL ở Production | Fail nếu log phình nhanh | Medium | ENV |
| LOG-05 | Không log dữ liệu nhạy cảm | `grep` mật khẩu, hash, cookie, nội dung email trong log | Không xuất hiện | Fail nếu có | Critical | ENV |
| LOG-06 | Cảnh báo nghiệp vụ được ghi | Kích hoạt honeypot và `notification_emails` rỗng | Có dòng `Warning` tương ứng | Fail nếu im lặng | Medium | ENV |
| LOG-07 | Cảnh báo file mồ côi | Kích hoạt MED-12 | Có `Warning` kèm `MediaId` và đường dẫn | Fail nếu không ghi | Medium | CR |
| LOG-08 | Ghi log lỗi worker | Xem JOB-12 | Có `Error` kèm exception đầy đủ | Fail nếu nuốt lỗi | High | ENV |
| LOG-09 | Log tồn tại sau khi container khởi động lại | Restart api | Log cũ vẫn truy được qua Coolify/driver log | Fail nếu mất sạch | Medium | ENV |

---

## 11. Checklist kiểm thử bảo mật

| ID | Mục đích | Các bước | Kết quả mong đợi | Pass/Fail | Ưu tiên | Xác minh |
|---|---|---|---|---|---|---|
| SEC-01 | Không endpoint admin nào lộ | Quét toàn bộ route admin ở trạng thái ẩn danh | Tất cả `401` | Fail nếu bất kỳ endpoint nào trả dữ liệu | Critical | AUTO + MAN |
| SEC-02 | CSRF bảo vệ mọi thao tác ghi | Xem CSRF-02 | Chặn | Critical | AUTO (có) |
| SEC-03 | Cookie không đọc được bằng JavaScript | Kiểm tra `HttpOnly` trên `hbp.admin` | Có | Fail nếu thiếu | Critical | MAN |
| SEC-04 | CORS không dùng wildcard cùng credentials | Xem cấu hình và phản hồi preflight | Chỉ origin cụ thể | Fail nếu `*` | Critical | CR + MAN |
| SEC-05 | Chống SQL injection | Gửi `'; DROP TABLE room_types; --` vào `search`, `slug`, `key` | Xử lý như chuỗi thường, không thực thi | Fail nếu có dấu hiệu injection | Critical | MAN |
| SEC-06 | Sort không nhận cột tùy ý | Xem CVT-05 | Chỉ whitelist | Fail nếu nhận tên cột từ người dùng | Critical | CR |
| SEC-07 | Không lộ secret qua API settings | `GET /api/admin/settings` | Không trả về key chứa `secret`/`password`/`token`/`credential`/`api_key`; `PUT` các key đó → `400` | Fail nếu đọc/ghi được | High | AUTO (có) |
| SEC-08 | Chống path traversal khi upload | Đặt tên file `../../evil.png` | Chỉ lấy phần tên file, ghi vào thư mục theo `mediaId` | Fail nếu ghi ra ngoài thư mục media | Critical | CR + MAN |
| SEC-09 | Giới hạn kích thước request | Upload file rất lớn | Bị chặn ở `RequestSizeLimit` 6 MB | Fail nếu chấp nhận | High | MAN |
| SEC-10 | Rate limit chống lạm dụng | Xem SUB-10 | Chặn | High | AUTO (có) |
| SEC-11 | Đổi mật khẩu admin sau lần đăng nhập đầu | Quy trình bàn giao | Mật khẩu seed đã được thay | **Fail nếu vẫn dùng mật khẩu seed lúc go-live** | Critical | MAN |
| SEC-12 | Không có tài khoản mặc định thừa | `SELECT username FROM admin_users` | Chỉ những tài khoản đã thống nhất | Fail nếu có tài khoản lạ | High | ENV |
| SEC-13 | Container chạy non-root | Xem DOC-03 | UID ≠ 0 | High | ENV |
| SEC-14 | HTTPS đầu vào | Gọi qua domain thật | Chuyển hướng/chỉ phục vụ HTTPS ở tầng proxy | Fail nếu HTTP thuần ra Internet | Critical | MAN |
| SEC-15 | Forwarded headers chỉ tin proxy | Xem cấu hình | `KnownNetworks`/`KnownProxies` đang bị xóa trắng — chấp nhận **chỉ khi** API không bao giờ tiếp nhận truy cập trực tiếp từ Internet | Fail nếu API expose trực tiếp (kẻ tấn công giả mạo IP để né rate limit) | High | CR + ENV |
| SEC-16 | Thông báo lỗi không tiết lộ nội bộ | Xem ERR-05 | Không lộ | Critical | ENV |
| SEC-17 | Swagger tắt ở Production | Xem SMK-05 | `404` | High | MAN |
| SEC-18 | Phụ thuộc không có CVE nghiêm trọng | `dotnet list package --vulnerable --include-transitive` | Không có mức High/Critical | Fail nếu có | High | ENV |

---

## 12. Checklist kiểm thử hiệu năng cơ bản

Chạy sau khi đã nạp dữ liệu mẫu: ≥50 room types, ≥200 media, ≥5.000 booking requests.

| ID | Mục đích | Các bước | Kết quả mong đợi | Pass/Fail | Ưu tiên | Xác minh |
|---|---|---|---|---|---|---|
| PRF-01 | Thời gian phản hồi public list | 100 request tuần tự tới `GET /api/rooms` | p95 < 300 ms | Fail nếu p95 > 1 s | High | ENV |
| PRF-02 | Thời gian phản hồi public detail | `GET /api/rooms/{slug}` | p95 < 400 ms | Fail nếu > 1 s | High | ENV |
| PRF-03 | Hiệu quả của ETag | Lần gọi thứ hai có `If-None-Match` | `304` nhanh hơn rõ rệt và không đọc DB nặng | Fail nếu không có lợi ích | Medium | ENV |
| PRF-04 | Tìm kiếm admin trên bảng lớn | `GET /api/admin/booking-requests?search=nguyen` trên 5.000 bản ghi | < 500 ms, dùng index trgm | Fail nếu > 2 s | High | ENV |
| PRF-05 | Phân trang sâu | `?page=50&pageSize=100` | Vẫn phản hồi < 1 s | Ghi nhận nếu chậm (OFFSET lớn) | Medium | ENV |
| PRF-06 | Thời gian xử lý ảnh | Upload ảnh 4 MB | Hoàn tất < 5 s | Fail nếu > 15 s hoặc timeout | High | ENV |
| PRF-07 | Tải đồng thời | 20 người dùng đồng thời trong 60 s trên endpoint public | Không lỗi 5xx, không cạn connection pool | Fail nếu có 5xx | High | ENV |
| PRF-08 | Rò rỉ bộ nhớ | Chạy tải 30 phút, theo dõi `docker stats` | RAM ổn định, không tăng tuyến tính | Fail nếu tăng không dừng | Medium | ENV |
| PRF-09 | Connection pool | Theo dõi `pg_stat_activity` trong lúc tải | Số kết nối ổn định, không chạm giới hạn | Fail nếu cạn | High | ENV |
| PRF-10 | Worker không cản trở request | Chạy tải trong khi có nhiều email chờ | Độ trễ API không tăng đáng kể | Fail nếu API chậm rõ rệt | Medium | ENV |
| PRF-11 | Ảnh tĩnh được cache | Tải lại ảnh | Trình duyệt dùng cache nhờ `max-age=31536000,immutable` | Fail nếu tải lại từ server | Medium | MAN |
| PRF-12 | Thời gian khởi động | Đo từ lúc start container tới khi `/health` trả 200 | < 15 s | Fail nếu > 60 s (Coolify có thể đánh dấu unhealthy) | Medium | ENV |

---

## 13. Regression test các chức năng chính

Bộ tối thiểu phải chạy lại sau **mỗi** lần triển khai.

| ID | Kịch bản | Các bước | Kết quả mong đợi | Ưu tiên | Xác minh |
|---|---|---|---|---|---|
| REG-01 | Hành trình khách đặt phòng | Xem danh sách phòng → mở chi tiết → gửi yêu cầu đặt phòng → nhận email xác nhận | Hoàn tất, khách nhận thư có mã tham chiếu | Critical | MAN + AUTO |
| REG-02 | Hành trình khách liên hệ | Gửi form liên hệ → nhận email | Hoàn tất | Critical | MAN + AUTO |
| REG-03 | Hành trình admin tạo nội dung | Đăng nhập → upload ảnh → tạo room type → gán amenities → gán bộ ảnh → bật hiển thị → kiểm tra trên API public | Phòng mới xuất hiện đúng thứ tự với ảnh và tiện nghi | Critical | MAN + AUTO |
| REG-04 | Hành trình admin xử lý lead | Đăng nhập → mở dashboard → tìm lead theo tên → mở chi tiết → xem trạng thái email | Thông tin khớp dữ liệu thật | Critical | MAN + AUTO |
| REG-05 | Ẩn thay vì xóa | Đặt `isVisible=false` cho một phòng | Biến mất khỏi API public nhưng vẫn còn trong admin | High | MAN |
| REG-06 | Song ngữ đầu-cuối | Xem toàn bộ trang public với `?lang=ja` | Không có trường nào rỗng hoặc lẫn ngôn ngữ sai | High | MAN |
| REG-07 | Chu trình vòng đời ảnh | Upload → gán → thử xóa (409) → gỡ gán → xóa (204) | Đúng như mô tả, không còn file mồ côi | High | AUTO |
| REG-08 | Chu trình email lỗi rồi phục hồi | Tắt SMTP → submit → thấy `RETRYING` → bật SMTP → chờ tới hạn | Chuyển sang `SENT`, khách nhận được thư | High | ENV |
| REG-09 | Khởi động lại giữ nguyên trạng thái | Restart toàn bộ stack | Dữ liệu, ảnh và email đang chờ đều còn, worker tiếp tục xử lý | Critical | ENV |
| REG-10 | Bộ test tự động | `dotnet test HBP.slnx` | Toàn bộ unit test và integration test (Testcontainers) đều xanh | Critical | AUTO |

---

## 14. Lỗi thường gặp và cách xác minh

| # | Hiện tượng | Nguyên nhân nhiều khả năng | Cách xác minh nhanh | Cách xử lý |
|---|---|---|---|---|
| 1 | Front end nhận `401` ở mọi API admin dù đã đăng nhập | Trình duyệt không gửi cookie: thiếu origin trong CORS, thiếu `credentials: 'include'`, hoặc `Secure=true` trên kết nối HTTP | Xem tab Network có `Set-Cookie` và `Cookie` không; kiểm tra `Cors__AllowedOrigins` | Bổ sung origin thật, đặt `Auth__CookieSecure` khớp giao thức |
| 2 | `400 Invalid CSRF token` khi lưu dữ liệu | Chưa gọi `/api/admin/auth/csrf` hoặc thiếu header `X-HBP-CSRF` | `curl -i` xem cookie `hbp.csrf` và header gửi lên | Lấy token trước, gắn header cho mọi POST/PUT/DELETE |
| 3 | Lỗi `column "failed_count" does not exist` | Mới áp `InitialCreate`, thiếu `AddLoginLockout` | `SELECT * FROM "__EFMigrationsHistory"` | Chạy `dotnet ef database update` hoặc migration bundle |
| 4 | Lỗi `type "language_code_enum" does not exist` | Chưa áp migration lên đúng database | `\dT` trong psql | Kiểm tra lại connection string đang trỏ đúng DB |
| 5 | Email mãi ở trạng thái `PENDING` | `Smtp__Host` rỗng → worker chủ động đứng yên | Tìm log `SMTP host is not configured` | Cấu hình SMTP rồi khởi động lại |
| 6 | Chỉ khách nhận thư, admin không nhận | `notification_emails` rỗng hoặc JSON sai | `SELECT value FROM system_settings WHERE key='notification_emails'` | Đặt lại qua `PUT /api/admin/settings/notification_emails` với mảng JSON hợp lệ |
| 7 | Ảnh trả về `404` dù upload thành công | Volume không được mount, `Media__StorageRoot` lệch, hoặc `Media__BaseUrl` sai domain | So sánh `storage_path` trong DB với `docker exec ls` | Sửa biến môi trường và mount lại volume |
| 8 | Ảnh hiện ở API nhưng front end khác domain không tải được | `Media__BaseUrl` còn là đường dẫn tương đối `/media` | Xem `publicUrl` trong phản hồi | Đặt `Media__BaseUrl` thành URL tuyệt đối |
| 9 | `429` liên tục khi kiểm thử | Rate limit 5 request/phút/IP trên 2 endpoint submit | Xem header `Retry-After` | Đổi `X-Forwarded-For` giữa các lần thử hoặc chờ hết cửa sổ |
| 10 | Tất cả người dùng dùng chung hạn mức rate limit | Reverse proxy không truyền `X-Forwarded-For` | Log IP thấy toàn địa chỉ nội bộ của proxy | Cấu hình proxy truyền header |
| 11 | Tài khoản admin bị khóa lúc đang kiểm thử | Đã sai mật khẩu 5 lần | `SELECT locked_until FROM admin_users` | Chờ 15 phút hoặc `UPDATE admin_users SET locked_until=NULL, failed_count=0` |
| 12 | Docker build lỗi "file not found" ở bước COPY | Build context không phải thư mục gốc repo | Xem lệnh build | Dùng `docker build -f src/HBP.Api/Dockerfile .` |
| 13 | API khởi động trước khi DB sẵn sàng | Thiếu `depends_on: condition: service_healthy` | Xem log api lúc boot | Compose đã cấu hình sẵn; nếu tự deploy thì bổ sung retry |
| 14 | Không có admin nào sau lần chạy đầu | Thiếu một trong 3 biến `HBP_SEED_ADMIN_*`, hoặc `Database__SeedOnStartup=false` | `SELECT count(*) FROM admin_users` | Đặt đủ 3 biến và bật seed cho lần chạy đầu |
| 15 | Seed không cập nhật dù đã đổi biến | Seed chỉ chạy khi bảng đang rỗng (cố ý, để idempotent) | Kiểm tra bảng đã có dữ liệu | Sửa trực tiếp qua API admin thay vì trông chờ seed |
| 16 | Swagger báo lỗi `conflicting schemaIds` | Hai DTO trùng tên ở namespace khác nhau | Mở `/swagger/v1/swagger.json` | Đổi tên DTO hoặc cấu hình `CustomSchemaIds` |
| 17 | `pg_dump` diff ra khác biệt lạ | Configuration lệch so với `docs/schema.sql` | Xem `docs/migration-verification.md` | Sửa ở `Persistence/Configurations`, tạo migration mới — **không** vá bằng raw SQL |
| 18 | Integration test treo khi chạy | Testcontainers không kéo được image `postgres:16-alpine` | `docker pull postgres:16-alpine` | Kéo image trước hoặc kiểm tra mạng |
| 19 | Thời gian trong DB lệch giờ | Nhầm giữa `DateTime.Now` và UTC | So `created_at` với `select now()` | Toàn hệ thống dùng UTC qua `IClock`; quy đổi ở tầng hiển thị |
| 20 | Ảnh mồ côi trên đĩa | Xóa file thất bại sau khi đã xóa row | `grep "files orphaned"` trong log | Dọn thủ công theo đường dẫn ghi trong log |

---

## 15. Tiêu chí nghiệm thu cuối cùng (Acceptance Criteria)

### 15.1 Điều kiện bắt buộc — thiếu bất kỳ mục nào thì **không** bàn giao

- [ ] **AC-01** Toàn bộ hạng mục **Critical** trong tài liệu này đạt Pass, có bằng chứng đính kèm.
- [ ] **AC-02** `dotnet build HBP.slnx -c Release` → 0 error, 0 warning.
- [ ] **AC-03** `dotnet test HBP.slnx` → toàn bộ unit test và integration test xanh trên môi trường có Docker.
- [ ] **AC-04** Cổng parity schema (MIG-05) đạt: diff `pg_dump` chỉ còn các khác biệt đã được ghi nhận trong `docs/migration-verification.md`.
- [ ] **AC-05** Smoke test end-to-end trong Plan chạy đủ: `/health` + `/health/ready`; `POST /api/booking-requests` sinh bản ghi `email_deliveries`; worker chuyển `PENDING → SENT` qua Mailhog; admin đăng nhập, CRUD room, upload ảnh sinh 3 biến thể; xóa ảnh đang dùng trả `409`; `GET /api/rooms?lang=ja` trả nội dung tiếng Nhật.
- [ ] **AC-06** Đã diễn tập khôi phục backup thành công (DB-19, DB-20) và có biên bản.
- [ ] **AC-07** Không còn secret nào nằm trong source control; toàn bộ bí mật cấp qua biến môi trường.
- [ ] **AC-08** Mật khẩu admin seed đã được đổi; không còn tài khoản mặc định.
- [ ] **AC-09** `RUN_MIGRATIONS_ON_STARTUP` tắt ở production; quy trình migration pre-deploy đã chạy thử thành công.
- [ ] **AC-10** Swagger không truy cập được ở production.
- [ ] **AC-11** `Cors__AllowedOrigins` chứa đúng domain thật; đã kiểm chứng front end gọi API kèm cookie thành công.
- [ ] **AC-12** Log ở dạng JSON, đọc được trên Coolify, không chứa dữ liệu nhạy cảm.

### 15.2 Điều kiện nên có — Fail thì ghi nhận nhưng có thể bàn giao kèm cam kết xử lý

- [ ] **AC-13** Toàn bộ hạng mục **High** đạt Pass, hoặc mỗi mục Fail có phương án và thời hạn khắc phục được chấp thuận.
- [ ] **AC-14** Các ngưỡng hiệu năng cơ bản (mục 12) đạt.
- [ ] **AC-15** Đã cấu hình giám sát `/health` (Uptime Kuma hoặc tương đương) và lịch backup hằng ngày giữ ≥7 bản.

### 15.3 Bảo lưu đã biết — phải ghi rõ trong biên bản nghiệm thu

| # | Nội dung bảo lưu | Ảnh hưởng | Hướng xử lý |
|---|---|---|---|
| 1 | Nhà cung cấp SMTP thật chưa chốt (TBD-TECH-005) | Chỉ mới kiểm chứng với Mailhog | Trừu tượng hóa đã xong; chỉ cần đổi biến môi trường và chạy lại EML-16, EML-17 |
| 2 | Nội dung và thương hiệu email chưa chốt (TBD-TECH-017/018) | Thư gửi đi dùng nội dung tạm | Sửa file `.sbn` rồi build lại, không đụng mã nguồn |
| 3 | `booking_request_status` chỉ có `RECEIVED` | Admin không đánh dấu được lead đã xử lý | Nếu cần thì mở rộng enum bằng migration riêng và bổ sung endpoint đổi trạng thái |
| 4 | Thông báo cho admin chỉ có template tiếng Việt | Admin luôn nhận thư tiếng Việt | Renderer đã có fallback; thêm thư mục `ja` khi cần |
| 5 | `KnownProxies`/`KnownNetworks` bị xóa trắng | Tin tưởng mọi `X-Forwarded-For` | Chấp nhận được **chỉ khi** API không tiếp nhận truy cập trực tiếp từ Internet (SEC-15) |

### 15.4 Chữ ký nghiệm thu

| Vai trò | Họ tên | Ngày | Kết luận (Chấp thuận / Chấp thuận có điều kiện / Từ chối) | Chữ ký |
|---|---|---|---|---|
| Người kiểm thử | | | | |
| Phụ trách kỹ thuật | | | | |
| Đại diện nghiệp vụ | | | | |

---

## Phụ lục A — Trình tự triển khai đầy đủ trên môi trường mới

```bash
# 1. Lấy mã nguồn và khôi phục công cụ
git clone <repo> && cd BE_HBP
dotnet tool restore
dotnet restore HBP.slnx

# 2. Kiểm tra build và unit test trước khi đụng tới hạ tầng
dotnet build HBP.slnx -c Release
dotnet test tests/HBP.UnitTests/HBP.UnitTests.csproj

# 3. Dựng PostgreSQL và SMTP sink
docker compose up -d db mailhog

# 4. Áp migration (một trong hai cách)
export HBP_DESIGN_CONNECTION="Host=localhost;Port=5432;Database=hbp;Username=hbp;Password=hbp_dev_password"
dotnet ef database update --project src/HBP.Infrastructure --startup-project src/HBP.Api
#    hoặc dùng bundle cho môi trường không có SDK:
dotnet ef migrations bundle --project src/HBP.Infrastructure --startup-project src/HBP.Api \
  --self-contained -r linux-x64 -o artifacts/migrate

# 5. Chạy cổng parity schema (mục MIG-05)
dotnet ef migrations script --project src/HBP.Infrastructure --startup-project src/HBP.Api \
  -o artifacts/initialcreate.sql

# 6. Build image và khởi động toàn bộ stack
docker compose up -d --build

# 7. Chạy integration test trên máy có Docker
dotnet test tests/HBP.IntegrationTests/HBP.IntegrationTests.csproj

# 8. Smoke test (mục 4)
curl -i http://localhost:8080/health
curl -i http://localhost:8080/health/ready
curl -i http://localhost:8080/api/rooms
```

## Phụ lục B — Kịch bản kiểm thử thủ công cho luồng admin

```bash
BASE=http://localhost:8080
JAR=$(mktemp)

# Lấy CSRF token
TOKEN=$(curl -s -c $JAR $BASE/api/admin/auth/csrf | sed -E 's/.*"token":"([^"]+)".*/\1/')

# Đăng nhập
curl -s -b $JAR -c $JAR -X POST $BASE/api/admin/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin","password":"<mật khẩu>"}'

# Gọi endpoint đọc
curl -s -b $JAR $BASE/api/admin/dashboard

# Upload ảnh (cần header CSRF)
curl -s -b $JAR -X POST $BASE/api/admin/media \
  -H "X-HBP-CSRF: $TOKEN" -F "file=@sample-1600x1000.jpg"

# Tạo room type
curl -s -b $JAR -X POST $BASE/api/admin/rooms \
  -H "X-HBP-CSRF: $TOKEN" -H 'Content-Type: application/json' \
  -d '{"code":"DLX","slug":"phong-deluxe","nameVi":"Phòng Deluxe","priceDisplayMode":"CONTACT","capacity":2,"displayOrder":0,"isVisible":true}'
```

## Phụ lục C — Truy vấn kiểm tra dữ liệu nhanh

```sql
-- Migration đã áp
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";

-- Trạng thái hàng đợi email
SELECT status, count(*) FROM email_deliveries GROUP BY status;

-- Email sắp được thử lại
SELECT reference_code, e.status, e.attempt_count, e.next_retry_at, e.last_error
FROM email_deliveries e JOIN booking_requests b ON b.id = e.related_entity_id
WHERE e.status IN ('PENDING','RETRYING') ORDER BY e.created_at;

-- Ảnh đang được tham chiếu
SELECT m.id, m.original_file_name,
       (SELECT count(*) FROM room_types r WHERE r.featured_media_id = m.id) AS as_room_featured,
       (SELECT count(*) FROM services s WHERE s.featured_media_id = m.id)  AS as_service_featured,
       (SELECT count(*) FROM room_type_media rm WHERE rm.media_file_id = m.id) AS in_room_gallery,
       (SELECT count(*) FROM gallery_items g WHERE g.media_file_id = m.id) AS in_gallery
FROM media_files m;

-- Tình trạng khóa tài khoản
SELECT username, is_active, failed_count, first_failed_at, locked_until, last_login_at FROM admin_users;

-- Lead 7 ngày gần nhất
SELECT date_trunc('day', created_at) AS ngay, count(*) FROM booking_requests
WHERE created_at >= now() - interval '7 days' GROUP BY 1 ORDER BY 1;
```

# Báo cáo xác minh triển khai HBP

- Ngày kiểm tra: 2026-07-25
- Môi trường: Windows, .NET SDK 8.0.423, EF CLI 8.0.11, Docker Desktop 29.6.1 / Compose 5.3.0
- Checklist chuẩn: `Deployment_Verification_Checklist.md`
- Kết luận: **CHƯA SẴN SÀNG GO-LIVE**

## 1. Tóm tắt điều hành

Project build sạch và toàn bộ 52 test tự động hiện có đều xanh:

- Solution tương thích SDK 8 (`HBP.sln`): **0 error, 0 warning**.
- Unit tests: **34/34 Pass**.
- Integration tests (PostgreSQL Testcontainers): **18/18 Pass**.
- Publish API, sinh migration script, kiểm tra pending model changes và tạo Linux migration bundle: **Pass**.
- Quét NuGet sau sửa: **không còn package bị báo vulnerable**.

Chưa thể nghiệm thu cuối vì:

1. Lệnh bắt buộc với `HBP.slnx` không chạy trên SDK 8 (`MSB4068`). Đã bổ sung `HBP.sln` tương thích, nhưng checklist/CI/deployment phải thống nhất dùng `.sln` hoặc nâng SDK.
2. Docker Desktop bị lỗi BuildKit `read-only file system`, sau đó daemon dừng và tài khoản hiện tại không có quyền khởi động service. Vì vậy smoke test Compose, email Mailhog, volume persistence, backup/restore và performance chưa hoàn tất.
3. Chưa có secret, domain/CORS, SMTP và HTTPS production thật; các tiêu chí go-live phụ thuộc môi trường không thể xác nhận.
4. DB-19 (diễn tập backup/restore) là Critical và chưa thực hiện; theo checklist, riêng mục này đã chặn bàn giao.

## 2. Thay đổi đã thực hiện

| Thay đổi | Nguyên nhân | Xác minh lại |
|---|---|---|
| Thêm `HBP.sln` chứa đủ 6 project | `.slnx` không được .NET SDK 8.0.423 hỗ trợ | restore/build/test bằng `.sln` thành công |
| Khởi tạo `RoomType.PriceDisplayMode = CONTACT` và cấu hình sentinel ngoài miền enum | Tránh EF thay giá trị `SHOW_PRICE` hợp lệ bằng database default | EF không còn cảnh báo sau Debug rebuild; không có pending model changes |
| Ghim `System.Net.Http 4.3.4` và `System.Text.RegularExpressions 4.3.1` trong unit-test project | Loại hai advisory High từ dependency test cũ | `dotnet list ... --vulnerable --include-transitive`: không còn vulnerable package |

## 3. Bằng chứng lệnh

| Kiểm tra | Kết quả |
|---|---|
| `dotnet --list-sdks` | 8.0.423 |
| `dotnet tool restore`; `dotnet ef --version` | Pass; 8.0.11 |
| `dotnet restore HBP.sln` | Pass, 6/6 project |
| `dotnet build HBP.sln -c Release --no-restore` | Pass, 0 warning, 0 error |
| `dotnet publish src/HBP.Api/HBP.Api.csproj -c Release -o artifacts/publish` | Pass; có `HBP.Api.dll` |
| `dotnet test HBP.sln -c Release --no-build` | 34 unit + 18 integration Pass |
| `dotnet ef migrations script ...` | Pass; `artifacts/initialcreate.sql` |
| `dotnet ef migrations has-pending-model-changes ...` | Pass; no changes |
| `dotnet ef migrations bundle ... -r linux-x64` | Pass; `artifacts/migrate` |
| `dotnet list HBP.sln package --vulnerable --include-transitive` | Pass sau sửa; không có advisory |
| `docker compose config` | Pass |
| `docker compose up -d --build` | Fail do Docker Desktop/BuildKit I/O, không phải compile error |

## 4. Kết quả theo từng hạng mục checklist

Quy ước: **PASS** = có bằng chứng CR/AUTO/ENV trong phiên kiểm tra; **FAIL** = sai tiêu chí; **BLOCKED** = cần môi trường thật nhưng chưa xác minh; **N/A** = không áp dụng.

### 4.1 Điều kiện tiên quyết

| Trạng thái | ID | Ghi chú |
|---|---|---|
| PASS | PRE-01, PRE-03, PRE-08 | SDK/EF đúng; ổ đĩa host đủ dung lượng |
| FAIL | PRE-02 | CLI ban đầu hoạt động nhưng Docker daemon chết trong lúc build và không khởi động lại được |
| BLOCKED | PRE-04, PRE-05, PRE-06, PRE-07 | Integration test đã dùng PostgreSQL 16 thành công, nhưng stack nghiệm thu cố định không còn hoạt động |
| BLOCKED | PRE-09, PRE-10 | Cần secret và domain/origin production do chủ dự án cung cấp |

### 4.2 Build, cấu hình và DI

| Trạng thái | ID | Ghi chú |
|---|---|---|
| FAIL | BLD-01, BLD-02 | Lệnh nguyên bản dùng `HBP.slnx` lỗi trên SDK 8; lệnh thay thế với `HBP.sln` Pass sạch |
| PASS | BLD-03, BLD-04, BLD-05, BLD-06, BLD-07, BLD-08 | Publish/net8/deterministic/templates/tests/dockerignore đạt |
| PASS | CFG-01, CFG-07, CFG-08, CFG-10, CFG-11, CFG-12 | Không có production secret; defaults/seed/design-time/AllowedHosts đúng theo code |
| BLOCKED | CFG-02, CFG-03, CFG-04, CFG-05, CFG-06, CFG-09 | Cần env và endpoint production thật |
| PASS | DI-02, DI-03, DI-04, DI-05, DI-06, DI-07 | Review code xác nhận registration/lifetime/order/options |
| PASS | DI-01 | 18 integration test khởi tạo host và gọi các nhóm controller không có lỗi resolve service |

### 4.3 Migration, schema và database

| Trạng thái | ID | Ghi chú |
|---|---|---|
| PASS | MIG-01, MIG-02, MIG-03, MIG-06, MIG-07, MIG-08, MIG-09, MIG-10, MIG-11, MIG-12 | Script/drift/bundle và SchemaParityTests xác nhận cấu trúc chính |
| PASS | MIG-05 | `SchemaParityTests` xanh; parity tự động giữa EF và schema chuẩn đạt |
| BLOCKED | MIG-04 | Chưa diễn tập Down(0) rồi Up trên DB nghiệm thu cố định |
| PASS | DB-01, DB-03, DB-04, DB-05, DB-06, DB-07, DB-08, DB-09, DB-11, DB-12, DB-16 | Được migration/schema và integration tests bao phủ |
| BLOCKED | DB-02, DB-10, DB-13, DB-14, DB-15, DB-17, DB-18, DB-20 | Cần truy vấn/manual workload trên stack sống |
| FAIL | DB-19 | Chưa diễn tập `pg_dump` → `pg_restore`; đây là lỗi chặn Critical |

### 4.4 Docker và smoke test

| Trạng thái | ID | Ghi chú |
|---|---|---|
| PASS | DOC-02, DOC-05 | Dockerfile multi-stage SDK/runtime 8, port 8080 |
| FAIL | DOC-01, DOC-06 | Build bị Docker Desktop I/O/daemon failure |
| BLOCKED | DOC-03, DOC-04, DOC-07, DOC-08 | Không có image/stack sống để xác minh UID, volume, persistence, size |
| BLOCKED | SMK-01, SMK-02, SMK-03, SMK-04, SMK-06, SMK-07, SMK-08, SMK-09, SMK-10, SMK-11, SMK-12, SMK-13 | Integration tests bao phủ nhiều hành vi, nhưng checklist yêu cầu smoke trên container triển khai |
| PASS | SMK-05 | Code chỉ bật Swagger khi `IsDevelopment()` |

### 4.5 Authentication, CSRF và i18n

| Trạng thái | ID | Ghi chú |
|---|---|---|
| PASS | AUTH-01, AUTH-02, AUTH-03, AUTH-04, AUTH-05, AUTH-06, AUTH-08, AUTH-10, AUTH-11, AUTH-12, AUTH-13, AUTH-14 | Code review + AdminAuthTests; mọi controller trong `Controllers/Admin` có `[Authorize]` |
| BLOCKED | AUTH-07, AUTH-09 | Cần thao tác thời gian/kiểm tra cookie trên deployment thật |
| PASS | CSRF-01, CSRF-02, CSRF-03, CSRF-04, CSRF-05, CSRF-06 | Middleware review và integration tests |
| PASS | I18N-01, I18N-03, I18N-04, I18N-06 | PublicApiTests và middleware/cache review |
| BLOCKED | I18N-02, I18N-05 | Chưa chạy manual HTTP trên stack cố định |

### 4.6 Media, submission và validation

| Trạng thái | ID | Ghi chú |
|---|---|---|
| PASS | MED-01, MED-05, MED-09, MED-10, MED-11, MED-13, MED-14, MED-15 | AdminContentTests + FK/static-files review |
| BLOCKED | MED-02, MED-03, MED-04, MED-06, MED-07, MED-08, MED-12 | Cần file/volume và manual HTTP thật |
| PASS | SUB-01, SUB-02, SUB-03, SUB-04, SUB-05, SUB-06, SUB-07, SUB-09, SUB-10, SUB-11, SUB-13 | BookingSubmissionTests/unit tests + transaction/code review |
| BLOCKED | SUB-08, SUB-12 | Cần manual/proxy deployment thật |
| PASS | VAL-01, VAL-02, VAL-03, VAL-04, VAL-05, VAL-06, VAL-07, VAL-08, VAL-09, VAL-10 | Validator tests/integration tests và transaction review |
| BLOCKED | VAL-11, VAL-12 | Chưa gọi payload hỏng trên container triển khai |

### 4.7 API và quy ước phản hồi

| Trạng thái | ID | Ghi chú |
|---|---|---|
| PASS | API-01, API-02, API-03, API-04, API-05, API-06, API-07, API-08, API-09 | PublicApiTests/BookingSubmissionTests và route review |
| BLOCKED | API-10, API-11, API-12 | Cần container sống |
| PASS | API-20, API-21, API-22, API-23, API-24, API-27, API-28, API-29, API-30, API-32, API-33, API-34, API-35, API-37, API-38, API-39, API-40, API-41, API-42, API-43 | Integration tests và route/service review |
| BLOCKED | API-25, API-26, API-31, API-36 | Checklist yêu cầu gọi thủ công; chưa có stack cố định |
| PASS | CVT-01, CVT-02, CVT-03, CVT-04, CVT-05, CVT-06, CVT-08, CVT-09 | JSON/options/query/cache tests và review |
| BLOCKED | CVT-07, CVT-10, CVT-11 | Cần manual CORS/search trên deployment thật |
| PASS | ERR-01, ERR-02, ERR-03, ERR-04, ERR-05 | Integration tests và GlobalExceptionHandler review |
| BLOCKED | ERR-06, ERR-07 | Cần log/HTTP response của container triển khai |

### 4.8 Background job và email

| Trạng thái | ID | Ghi chú |
|---|---|---|
| PASS | JOB-01, JOB-03, JOB-05, JOB-08, JOB-10, JOB-11, JOB-13, JOB-16 | Worker review: scope riêng, `FOR UPDATE SKIP LOCKED`, backoff, truncate, retention 24h |
| BLOCKED | JOB-02, JOB-04, JOB-06, JOB-07, JOB-09, JOB-12, JOB-14, JOB-15, JOB-17 | Cần worker + DB + SMTP sống và quan sát theo thời gian |
| PASS | EML-04, EML-05, EML-08, EML-12, EML-13 | Code/unit tests xác nhận fallback/normalization/template model |
| BLOCKED | EML-01, EML-02, EML-03, EML-06, EML-07, EML-09, EML-10, EML-11, EML-14, EML-15, EML-16, EML-17, EML-18 | Mailhog/SMTP thật và kiểm tra nội dung/header/log chưa chạy được |

### 4.9 Logging và security

| Trạng thái | ID | Ghi chú |
|---|---|---|
| PASS | LOG-01, LOG-04, LOG-07, LOG-08 | Serilog compact JSON và các nhánh warning/error được cấu hình |
| BLOCKED | LOG-02, LOG-03, LOG-05, LOG-06, LOG-09 | Cần log container thực tế |
| PASS | SEC-01, SEC-02, SEC-03, SEC-04, SEC-05, SEC-06, SEC-07, SEC-08, SEC-09, SEC-10, SEC-16, SEC-17, SEC-18 | Auth/CSRF/parameterized EF/path handling/Swagger/package scan |
| BLOCKED | SEC-11, SEC-12, SEC-13, SEC-14, SEC-15 | Cần tài khoản/domain/proxy/container production; forwarded headers chỉ an toàn khi không expose trực tiếp |

### 4.10 Hiệu năng và regression

| Trạng thái | ID | Ghi chú |
|---|---|---|
| BLOCKED | PRF-01, PRF-02, PRF-03, PRF-04, PRF-05, PRF-06, PRF-07, PRF-08, PRF-09, PRF-10, PRF-11, PRF-12 | Không thể đo tải/thời gian/RAM/pool khi Docker daemon không hoạt động |
| PASS | REG-01, REG-02, REG-03, REG-04, REG-07 | Các phần lõi được 18 integration tests bao phủ |
| BLOCKED | REG-05, REG-06, REG-08, REG-09 | Cần E2E/manual/restart stack |
| FAIL | REG-10 | Lệnh literal `dotnet test HBP.slnx` lỗi trên SDK 8; `dotnet test HBP.sln` Pass 52/52 |

## 5. Đối chiếu Acceptance Criteria

| ID | Kết quả | Nhận định |
|---|---|---|
| AC-01 | FAIL | Còn Critical FAIL/BLOCKED (Docker, backup/restore, production env) |
| AC-02 | FAIL | `.slnx` không chạy với SDK 8; `.sln` thay thế đạt 0/0 |
| AC-03 | PASS có điều kiện | 52/52 test xanh; integration test đã dùng Docker trước khi daemon chết |
| AC-04 | PASS | SchemaParityTests xanh |
| AC-05 | BLOCKED | Chưa chạy trọn E2E Mailhog/admin/media trên Compose deployment |
| AC-06 | FAIL | Chưa diễn tập restore DB + media |
| AC-07 | PASS cho source | Không thấy production secret trong JSON/source; secret runtime chưa được cấp |
| AC-08 | BLOCKED | Chưa xác nhận đổi mật khẩu seed trên production |
| AC-09 | PASS cho code, BLOCKED cho production | Default tắt; compose dev cố ý bật |
| AC-10 | PASS | Swagger chỉ Development |
| AC-11 | BLOCKED | Chưa có domain thật |
| AC-12 | BLOCKED | Cấu hình JSON đúng; chưa xác nhận trên Coolify/log thực |
| AC-13 | FAIL | Nhiều High cần ENV/MAN chưa hoàn tất |
| AC-14 | BLOCKED | Chưa chạy performance suite |
| AC-15 | BLOCKED | Không có bằng chứng monitoring và lịch backup |

## 6. Hạng mục cần theo dõi và cách đóng

1. Khởi động/sửa Docker Desktop, chạy lại `docker build` và `docker compose up -d`; lưu log đầy đủ.
2. Quyết định chuẩn solution:
   - dùng `HBP.sln` cho .NET SDK 8 và sửa mọi lệnh checklist/CI; hoặc
   - giữ `HBP.slnx` nhưng nâng SDK build lên phiên bản hỗ trợ.
3. Chạy đầy đủ smoke/E2E mục 4, đặc biệt health degradation, admin login/CSRF, upload 3 biến thể, static cache và Mailhog.
4. Chạy parity thủ công `pg_dump diff` theo `docs/migration-verification.md` để bổ sung bằng chứng ngoài integration test.
5. Diễn tập migration Down/Up và backup/restore DB + media; đây là điều kiện bắt buộc.
6. Cấp production secrets/domain/SMTP/HTTPS/proxy, đổi mật khẩu admin seed, xác nhận CORS/cookie/Secure/forwarded headers.
7. Chạy performance suite và theo dõi RAM/connection pool ít nhất theo thời lượng checklist.

## 7. Kết luận cuối

Mã nguồn đang ở trạng thái **build/test tốt và các lỗi code phát hiện trong phiên đã được sửa**, nhưng **chưa đạt tiêu chuẩn sẵn sàng triển khai** của checklist. Không được go-live cho tới khi đóng các mục Critical còn FAIL/BLOCKED, đặc biệt Docker deployment, smoke E2E, production configuration và backup/restore rehearsal.

## 8. Cập nhật sau khi phục hồi Docker Desktop

Docker Desktop đã được phục hồi sau khi giải phóng NuGet cache trên ổ C và dừng các backend bị treo. Kết quả dưới đây thay thế các trạng thái Docker/smoke tương ứng ở phần trên:

| ID | Kết quả mới | Bằng chứng |
|---|---|---|
| PRE-02, PRE-04, PRE-05, PRE-06 | PASS | Docker 29.6.1; Compose 5.3.0; PostgreSQL 16 container healthy; Mailhog HTTP 200 |
| DOC-01, DOC-02, DOC-03, DOC-04, DOC-05, DOC-06, DOC-07 | PASS | Image build thành công; UID/GID `app` 1654; media volume mount; port 8080; DB healthy trước API; dữ liệu còn nguyên sau `down/up` |
| DOC-08 | FAIL (Low) | Image `hbp-api:latest` 351 MB, cao hơn ngưỡng tham chiếu ~350 MB khoảng 1 MB |
| SMK-01, SMK-02, SMK-03, SMK-04, SMK-05, SMK-06, SMK-07, SMK-08, SMK-09, SMK-12, SMK-13 | PASS | health 200; ready 200/503 đúng trạng thái DB; JSON logs; Swagger Development 200 và Production 404; public APIs 200; booking 201; login/me/dashboard 200; worker chuyển email sang SENT; Mailhog nhận thư |
| API-10, API-11 | PASS | `/health` 200; `/health/ready` 200 và 503 khi DB dừng |
| CFG-02, CFG-03, CFG-06, CFG-09, CFG-10 | PASS cho development stack | Env binding, CORS dev, SMTP Mailhog, seed đúng 1 admin và không nhân bản sau restart |
| DI-01 | PASS | Public/admin endpoints chạy không có lỗi resolve service |
| DB-01, DB-02, DB-03, DB-05, DB-06, DB-07, DB-10, DB-13, DB-14, DB-15, DB-16 | PASS hoặc đã được schema/integration/smoke xác nhận | 14 bảng gồm history; 2 migration; 2 extension; 2 function; 10 trigger; 5 trgm index; seed/persistence đạt |
| JOB-01, JOB-02, JOB-03, JOB-06, JOB-12, JOB-13, JOB-17 | PASS | Worker chạy cùng API, poll 15 giây, gửi SENT, chịu DB restart, shutdown/restart stack sạch |
| EML-04, EML-06, EML-08, EML-10, EML-14, EML-15 | PASS trên Mailhog | `notification_emails=[]` vẫn gửi email khách; Mailhog nhận 1 thư và DB có `SENT=1` |
| LOG-01, LOG-02, LOG-03, LOG-04, LOG-08, LOG-09 | PASS | Log JSON một dòng, có trace/span/request id và request logging; log truy cập lại được sau restart |
| SEC-13, SEC-17 | PASS | Container non-root; Swagger Production 404 |
| REG-09 | PASS | Booking/admin/email vẫn còn sau `docker compose down` rồi `up` |

### Bằng chứng schema/runtime sau phục hồi

- 14 bảng public = 13 bảng nghiệp vụ + `__EFMigrationsHistory`.
- 2 migration đã áp.
- Extensions: `pgcrypto`, `pg_trgm`.
- Functions: 2; triggers `trg_%`: 10; GIN trigram indexes: 5.
- Seed: 1 admin; restart không tạo thêm.
- Booking smoke: `201`, reference `BK-260725-E4WECV`.
- Email: `SENT=1`, Mailhog total 1.
- Volume persistence: sau `docker compose down`/`up` vẫn còn 1 booking, 1 admin, 1 email.
- Readiness degradation: DB dừng → liveness 200, readiness 503; DB chạy lại → readiness 200.

### Kết luận cập nhật

Các lỗi Docker và phần lớn smoke test Critical đã được đóng. Project vẫn **chưa đủ điều kiện go-live production** vì còn thiếu:

1. DB-19/AC-06: diễn tập backup/restore DB và media.
2. Production secrets, domain/CORS/HTTPS/proxy, SMTP thật và đổi mật khẩu admin seed.
3. E2E upload ảnh thực tế và kiểm tra 3 biến thể/static media trên Compose.
4. Kịch bản email nhiều người nhận admin; seed hiện có `notification_emails=[]`.
5. Performance/load/memory/connection-pool suite.
6. Bất nhất `.slnx` với SDK 8; cần chuẩn hóa CI/checklist sang `HBP.sln` hoặc nâng SDK.

# HBP — Tổng hợp lưu ý tích hợp giữa Back end và Front end

Tài liệu gộp mọi điểm cần biết khi vận hành BE_HBP và FE_HBP cùng nhau: hợp đồng dữ liệu, cấu hình
phải khớp đôi, các lỗi đã gặp thật, và những quyết định còn treo.

| | |
|---|---|
| **Back end** | `BE_HBP` — ASP.NET Core 8, PostgreSQL 16 |
| **Front end** | `ProjectBusiness` — package `fe-hbp`, Next.js 16.2.10 App Router |
| **Tài liệu liên quan** | BE: `docs/deployment.md`, `docs/migration-verification.md`, `Deployment_Verification_Checklist.md` · FE: `Frontend_API_Integration_Report.md`, `Admin_Console_Report.md` |

> Hai dự án nằm ở **hai repo riêng biệt**. Mọi thay đổi DTO ở BE đều là thay đổi phá vỡ (breaking
> change) đối với FE — không có compile-time check nào bắt được, chỉ có tài liệu này và test.

---

## 1. Ba đường gọi API — và vì sao chúng khác nhau

Đây là điểm quan trọng nhất của toàn bộ tích hợp. Hệ thống **không** dùng một cách gọi duy nhất.

| # | Luồng | Ai gọi | Biến môi trường | Vì sao |
|---|---|---|---|---|
| 1 | **Đọc dữ liệu công khai** (`GET /api/rooms`, `/services`, `/gallery`, `/amenities`) | Next server → BE | `API_BASE_URL` | Cho SEO + ISR; URL nội bộ không lộ ra trình duyệt |
| 2 | **Gửi form công khai** (`POST /api/booking-requests`, `/contact-requests`) | **Trình duyệt → BE trực tiếp** | `NEXT_PUBLIC_API_BASE_URL` | BE rate-limit **theo IP khách**. Nếu đẩy qua Next server thì mọi khách dùng chung một hạn mức 5 lần/phút và chặn lẫn nhau |
| 3 | **Toàn bộ khu admin** (`/api/admin/*`) | Next server → BE | `API_BASE_URL` | Cookie `hbp.admin` là `SameSite=Lax` — trình duyệt **không gửi** cookie này trong XHR chéo site. Đi qua Next thì cookie luôn first-party |

**Hệ quả cần nhớ:**

- `Cors__AllowedOrigins` của BE **chỉ ảnh hưởng đến luồng 2**. Nếu để rỗng, website vẫn hiển thị
  bình thường nhưng **hai form gửi luôn báo lỗi** — một kiểu hỏng rất dễ bỏ sót khi nghiệm thu.
- Đừng đảo ngược luồng 3 sang gọi thẳng từ trình duyệt nếu không đồng thời đổi cookie BE sang
  `SameSite=None; Secure`.
- Đừng đảo ngược luồng 2 sang gọi qua server nếu không muốn mất rate-limit theo IP.

---

## 2. Bảng cấu hình phải khớp đôi

Sai một trong các cặp dưới đây là hỏng ngay, nhưng thường không báo lỗi rõ ràng.

| Cấu hình BE | Cấu hình FE | Ràng buộc | Triệu chứng khi lệch |
|---|---|---|---|
| `Cors__AllowedOrigins__0` | origin thật của FE | Phải chứa đúng scheme + host + port của FE | Hai form công khai luôn báo "không gửi được"; DevTools thấy preflight bị chặn |
| `Media__BaseUrl` | `NEXT_PUBLIC_MEDIA_BASE_URL` | Phải là **URL tuyệt đối** và cùng host | Ảnh 404, hoặc `next/image` từ chối vì host không nằm trong `remotePatterns` |
| `Media__StorageRoot` | — | Phải trỏ vào volume đã mount | Upload lỗi `UnauthorizedAccessException` hoặc ảnh biến mất sau khi restart |
| `Auth__CookieSecure=true` | FE chạy HTTPS | Cả hai cùng HTTPS ở production | Đăng nhập admin "thành công" nhưng vòng lại trang login vì trình duyệt loại cookie |
| — | `API_BASE_URL` | URL nội bộ, ví dụ `http://api:8080` trong compose | Trang công khai render error state; log FE có `ECONNREFUSED` |
| Reverse proxy truyền `X-Forwarded-For` | — | Bắt buộc | Mọi khách dùng chung một hạn mức rate-limit |

FE khai báo đủ ba biến trong `.env.example`. BE liệt kê đầy đủ biến trong `docs/deployment.md`.

---

## 3. Hợp đồng dữ liệu — những chỗ FE phải xử lý đặc biệt

### 3.1 Giá phòng bị lược bỏ, không phải null

`RoomTypeListItemResponse` đánh dấu `priceVnd`/`priceUsd` bằng `JsonIgnore(WhenWritingNull)`. Khi
`priceDisplayMode = CONTACT`, hai trường này **biến mất khỏi JSON** chứ không phải trả `null`.

FE khai báo chúng là optional (`priceVnd?: number | null`) và kiểm tra bằng `typeof === "number"`.
Nếu FE nào đó dùng `priceVnd === null` để phân biệt thì sẽ sai.

### 3.2 Trường FE cần nhưng BE không có

| Trường | Tình trạng | FE xử lý |
|---|---|---|
| `tagline` của phòng | BE không có | Đã bỏ khỏi giao diện |
| `view` / hướng nhìn | BE không có | Đã bỏ khỏi thanh thông số (4 cột → 3 cột) |
| `width`/`height` trong `ImageResponse` công khai | BE không trả (chỉ có ở DTO admin) | Lưới masonry dùng nhịp cao/thấp theo chỉ số. **Nếu BE bổ sung hai trường này, FE nên chuyển sang dùng tỉ lệ thật** |
| Thông tin liên hệ (địa chỉ, hotline, giờ lễ tân) | BE lưu ở `system_settings.site_metadata` nhưng **API settings là admin-only** | Vẫn nằm cứng trong file i18n của FE. Cần BE bổ sung endpoint public đọc site metadata |

### 3.3 Trường BE bắt buộc nhưng form FE ban đầu thiếu

| Trường | Ràng buộc BE | Đã sửa |
|---|---|---|
| `contact_requests.phone_number` | `NOT NULL` + regex `^[0-9+\-\s().]{6,30}$` | Thêm ô "Số điện thoại" vào form liên hệ |
| `contact_requests.subject` | `NOT NULL`, max 255 | Chuyển "Chủ đề" thành bắt buộc |

### 3.4 `roomTypeId` là Guid, URL công khai dùng slug

Link "Đặt phòng" từ trang phòng có dạng `?room=<slug>`. Trang booking **phân giải slug sang Guid ở
phía server** trước khi đưa vào form, vì `CreateBookingRequestRequest.RoomTypeId` là `Guid?`.

Lưu ý phụ: nhóm radio chọn phòng dùng `name="roomType"` chứ không phải `name="room"` — trùng tên với
query param sẽ làm mất tiền chọn nếu người dùng submit trước khi React hydrate xong.

### 3.5 Khóa lỗi validation không đồng nhất hoa/thường

- FluentValidation trả **PascalCase**: `FullName`, `PhoneNumber`, `Adults`…
- Service tự ném `ValidationException` trả **camelCase**: `roomTypeId`, `featuredMediaId`, `items`…

FE chuẩn hóa bằng cách hạ chữ cái đầu (`normaliseErrors`) ở cả `src/lib/api/submit.ts` và
`src/lib/admin/form.ts`. **Nếu BE thống nhất về một kiểu, nhớ báo FE** — hiện tại FE chịu được cả hai.

### 3.6 Số bản ghi email không phải luôn là 2

Plan viết "mỗi request tạo 2 bản ghi `email_deliveries`". Thực tế code tạo **một dòng cho mỗi người
nhận admin** trong `notification_emails`, cộng một dòng cho khách. Với 3 địa chỉ admin là 4 dòng.
Đừng viết test hay dashboard giả định con số 2.

### 3.7 BR-BOOK-014 — điểm mâu thuẫn còn treo

BE **cố ý không kiểm tra** `check_out > check_in` (có comment rõ trong `CreateBookingRequestValidator`).
Nhưng form booking của FE **vẫn chặn** trường hợp này (`booking.form.errors.dates`, có sẵn từ trước).

Nghĩa là quy tắc nghiệp vụ hiện bị vô hiệu hóa từ phía giao diện. Cần chốt: hoặc bỏ kiểm tra ở FE,
hoặc xác nhận đây là hành vi mong muốn và ghi vào tài liệu nghiệp vụ.

---

## 4. Xác thực, phiên và CSRF

| Hạng mục | Giá trị | Ghi chú tích hợp |
|---|---|---|
| Cookie phiên | `hbp.admin`, HttpOnly, SameSite=Lax, hết hạn tuyệt đối 8 giờ, **không** trượt | FE admin sao chép cookie của BE sang origin của Next với cùng `maxAge` |
| Xác minh phiên | `GET /api/admin/auth/me` | FE gọi ở **mọi** lần render trang admin — có cookie chưa đủ để tin |
| CSRF | Cookie `hbp.csrf` + header `X-HBP-CSRF`, miễn trừ cho login | Là double-submit, chỉ có ý nghĩa với trình duyệt. FE admin gọi từ server nên sinh cặp ngẫu nhiên khớp nhau mỗi request; bảo vệ thật đến từ same-origin của Server Actions |
| Khóa tài khoản | 5 lần sai trong 15 phút → `locked_until = now+15m`, trả **423** | FE phân biệt 401 ("sai mật khẩu") và 423 ("bị khóa tạm thời") |
| Đổi mật khẩu | **BE chưa có endpoint** | Khoảng trống thật: tiêu chí nghiệm thu AC-08 yêu cầu đổi mật khẩu seed trước go-live, hiện chỉ đổi được trực tiếp trong database |

Khi kiểm thử thủ công mà bị khóa: `UPDATE admin_users SET locked_until=NULL, failed_count=0;`

---

## 5. Cache và đồng bộ nội dung

Có **ba lớp cache** nằm chồng lên nhau. Hiểu sai lớp nào là chẩn đoán nhầm.

| Lớp | Ai quản | Thời hạn | Vô hiệu hóa bằng |
|---|---|---|---|
| `Cache-Control: public,max-age=60` + ETag do BE đặt | `PublicCacheMiddleware` của BE, áp cho 4 endpoint GET công khai | 60 giây | Hết hạn tự nhiên |
| Data cache của Next (response API) | FE, tag `rooms`/`services`/`gallery`/`amenities` | 300 giây | `revalidateTag(tag, "max")` |
| Full route cache (HTML đã prerender) | FE, ISR 300 giây | 300 giây | `revalidatePath(pattern, "page")` |

**Bài học đã trả giá:** ban đầu Server Action của admin chỉ gọi `revalidateTag`. Sửa nội dung xong
mà trang công khai vẫn giữ HTML cũ, và BE **không nhận thêm request nào** — chứng tỏ trang prerender
không hề được tái sinh. Phải gọi **cả hai**:

```ts
revalidateTag(scope, "max");              // bỏ response API đã cache
revalidatePath("/[lang]/rooms", "page");  // bỏ HTML đã prerender
```

**Hành vi vận hành cần truyền đạt cho người dùng admin:** sau khi lưu, trang công khai đổi ở **lần
tải thứ hai**, không phải ngay lập tức (stale-while-revalidate). Nếu cần "sửa xong thấy ngay", Next 16
có `updateTag` với ngữ nghĩa read-your-writes nhưng request đó sẽ chậm hơn.

**Ngôn ngữ và cache:** FE truyền ngôn ngữ bằng query `?lang=vi|ja` chứ không dùng header
`Accept-Language`, để mỗi ngôn ngữ là một khóa cache riêng. BE vẫn đặt `Vary: Accept-Language` —
vô hại nhưng không phải cơ chế FE dựa vào.

---

## 6. Media — chuỗi phụ thuộc dài nhất

```
Upload (admin) → ImageSharp sinh 3 biến thể WebP → ghi /data/media/{yyyy}/{MM}/{id:N}/
   → public_url = Media__BaseUrl + đường dẫn tương đối
      → next/image kiểm tra host với images.remotePatterns
         → trình duyệt tải qua /_next/image
```

Mắt xích hay đứt:

1. **`Media__BaseUrl` để mặc định `/media`** (đường dẫn tương đối) → FE khác domain không tải được ảnh.
2. **Host không nằm trong `remotePatterns`** → `next/image` trả lỗi. FE dựng allow-list tự động từ
   `NEXT_PUBLIC_MEDIA_BASE_URL` trong `next.config.ts`; đổi host của BE thì phải **build lại FE**.
3. **Địa chỉ nội bộ (localhost/10.x/192.168.x)** → Next 16 chặn tối ưu ảnh trừ khi bật
   `dangerouslyAllowLocalIP`; FE bật tự động khi phát hiện host nội bộ.
4. **Volume không mount** → upload lỗi hoặc ảnh biến mất sau restart.

Ràng buộc nghiệp vụ: ảnh tối đa 5 MB, chỉ jpeg/png/webp. **Ảnh đại diện phải tối thiểu 1200×800** —
kiểm tra lúc *gán* làm featured chứ không phải lúc upload, nên ảnh nhỏ vẫn upload được và chỉ báo
lỗi khi chọn làm ảnh đại diện cho phòng/dịch vụ.

Xóa ảnh đang dùng trả **409** kèm mảng `references`; FE admin hiển thị danh sách này ngay trên thẻ ảnh.

**Sao lưu:** database và volume media phải được khôi phục **cùng một mốc thời gian**. Ảnh được
`media_files` tham chiếu mà không có tệp trên đĩa thì không sửa được từ tầng ứng dụng.

---

## 7. Thứ tự khởi động và phụ thuộc lúc build

Điểm này ít được để ý nhưng ảnh hưởng trực tiếp đến chất lượng lần deploy đầu.

**FE gọi BE ngay tại thời điểm `pnpm build`:**

- `generateStaticParams` của `/[lang]/rooms/[id]` gọi `GET /api/rooms` để biết danh sách slug.
- Các trang công khai được prerender kèm dữ liệu.

Nếu BE **chưa chạy** lúc build FE: build vẫn thành công (đã thiết kế để không sập), nhưng HTML sinh
ra chứa trạng thái lỗi/rỗng và không có trang chi tiết phòng nào được prerender. Nội dung đó phục vụ
cho tới lần revalidate đầu tiên (tối đa 5 phút).

**Thứ tự đúng khi deploy:** áp migration → khởi động BE → xác nhận `/health/ready` → mới build/deploy FE.

---

## 8. Bẫy đã gặp thật — đừng lặp lại

### Phía FE

| # | Vấn đề | Kết luận |
|---|---|---|
| 1 | **`loading.tsx` làm trang kẹt ở khung xương** | Thêm loading state cấp route khiến nội dung thật nằm lại trong `<div hidden>` của cơ chế streaming và không bao giờ được hoán vào DOM. Tái hiện ở **cả `next dev` lẫn production standalone**. Đã gỡ toàn bộ. Đừng dùng lại `loading.tsx`/Suspense streaming ở dự án này trước khi điều tra riêng |
| 2 | **`next start` không dùng được với `output: standalone`** | Route động không render. Phải chạy `node .next/standalone/server.js` sau khi copy `.next/static` và `public` vào thư mục standalone — đúng như Dockerfile làm |
| 3 | **Typed routes sinh lúc build** | `pnpm typecheck` báo `Type '"/admin/rooms"' does not satisfy 'AppRoutes'` khi route mới chưa build. Trong CI phải **build trước, typecheck sau** |
| 4 | **`next/font/google` cần mạng lúc build** | Một lần build fail vì lỗi mạng tạm thời. CI phải có internet hoặc chuyển sang font tự host |
| 5 | **`revalidateTag` một mình không đủ** | Xem mục 5 |

### Phía BE

| # | Vấn đề | Kết luận |
|---|---|---|
| 6 | `KnownProxies`/`KnownNetworks` bị xóa trắng | Tin mọi `X-Forwarded-For`. Chấp nhận được **chỉ khi** API không nhận truy cập trực tiếp từ Internet; nếu expose thẳng, kẻ tấn công đổi header là né được rate-limit |
| 7 | Seed chỉ chạy khi bảng rỗng | Đổi biến `HBP_SEED_ADMIN_*` sau lần chạy đầu sẽ không có tác dụng |
| 8 | `Smtp__Host` rỗng → worker đứng yên | Email nằm mãi ở `PENDING`, chỉ có một dòng cảnh báo trong log |
| 9 | `notification_emails` rỗng → chỉ khách nhận thư | Không có lỗi nào được ném ra, chỉ ghi log cảnh báo |
| 10 | `RUN_MIGRATIONS_ON_STARTUP` | Phải tắt ở production; dùng `dotnet ef migrations bundle` chạy pre-deploy |

---

## 9. Smoke test xuyên hai hệ thống

Chuỗi ngắn nhất chứng minh BE và FE thực sự khớp nhau. Chạy sau mỗi lần deploy.

1. `GET /health` và `/health/ready` của BE → 200.
2. Mở `/vi/rooms` → thấy phòng thật, giá đúng định dạng, ảnh tải được.
3. Mở `/ja/rooms` → nội dung tiếng Nhật, phòng thiếu bản Nhật rơi về tiếng Việt.
4. Gửi form đặt phòng → nhận `referenceCode` dạng `BK-yyMMdd-XXXXXX`.
5. Kiểm tra DB: `booking_requests` có 1 dòng, `email_deliveries` có **1 dòng cho mỗi người nhận
   admin + 1 dòng cho khách**.
6. Đợi một chu kỳ worker → trạng thái chuyển `PENDING → SENT`, thư xuất hiện ở Mailhog/SMTP.
7. Đăng nhập `/admin/login` → vào được bảng điều khiển, số liệu khớp bước 5.
8. Mở `/admin/bookings` → thấy lead vừa gửi; mở chi tiết → nhật ký email khớp bước 6.
9. Upload một ảnh ≥1200×800 ở `/admin/media`, gán làm ảnh đại diện cho một phòng, bật hiển thị.
10. Thử xóa chính ảnh đó → **409** kèm danh sách tham chiếu.
11. Tải `/vi/rooms` **hai lần** → lần thứ hai thấy phòng vừa sửa (stale-while-revalidate).
12. Đăng xuất → `/admin/rooms` bị đẩy về trang login.

Bước 5, 10 và 11 là ba bước hay lộ lỗi tích hợp nhất.

---

## 10. Quyết định còn treo

| # | Nội dung | Chặn cái gì | Ai quyết |
|---|---|---|---|
| 1 | Nhà cung cấp SMTP thật (TBD-TECH-005) | Gửi email thật; hiện chỉ kiểm chứng với Mailhog | Nghiệp vụ |
| 2 | Nội dung và thương hiệu email (TBD-TECH-017/018) | Nội dung thư gửi khách; engine đã xong, chỉ cần sửa file `.sbn` | Nghiệp vụ |
| 3 | Domain/origin thật (TBD-TECH-015) | Chốt `Cors__AllowedOrigins` và cookie | Hạ tầng |
| 4 | `booking_request_status` có cần trạng thái ngoài `RECEIVED`? | Admin không đánh dấu được lead đã xử lý; cần migration mở rộng enum + endpoint + màn hình | Nghiệp vụ |
| 5 | FE có nên tiếp tục chặn ngày trả ≤ ngày nhận? | Mâu thuẫn với BR-BOOK-014 — xem mục 3.7 | Nghiệp vụ |
| 6 | Endpoint đổi mật khẩu quản trị | Tiêu chí nghiệm thu AC-08 | Kỹ thuật |
| 7 | Endpoint public đọc `site_metadata` | Thông tin liên hệ vẫn nằm cứng ở FE | Kỹ thuật |
| 8 | Bổ sung `width`/`height` vào `ImageResponse` công khai | Lưới masonry dùng tỉ lệ thật thay vì nhịp giả | Kỹ thuật |
| 9 | Thống nhất hoa/thường khóa lỗi validation | FE hiện phải chuẩn hóa cả hai kiểu | Kỹ thuật |
| 10 | Template email tiếng Nhật cho thông báo admin | Admin luôn nhận thư tiếng Việt; renderer đã có fallback | Nghiệp vụ |

---

## 11. Việc cần làm khi thay đổi mỗi bên

**Khi BE đổi DTO công khai** → cập nhật `src/lib/api/types.ts` ở FE, chạy `pnpm build` (typed routes)
rồi `pnpm typecheck`.

**Khi BE đổi DTO admin** → cập nhật `src/lib/admin/types.ts`, kiểm tra lại các Server Action trong
`src/lib/admin/actions.ts` vì payload được dựng thủ công từ `FormData`.

**Khi BE thêm endpoint công khai mới** → thêm hàm vào `src/lib/api/public.ts` kèm cache tag; nếu nội
dung đó hiển thị trên trang đã prerender, thêm path tương ứng vào `PUBLIC_PATHS` trong
`src/lib/admin/actions.ts` để admin sửa xong là trang công khai được làm mới.

**Khi BE đổi host/domain hoặc `Media__BaseUrl`** → phải **build lại FE**, vì `remotePatterns` được
tính lúc build.

**Khi FE đổi origin** → cập nhật `Cors__AllowedOrigins` của BE và khởi động lại.

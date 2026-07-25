# Báo cáo chuyển dữ liệu hardcode Frontend vào Database

Ngày xác minh: 25/07/2026  
Frontend nguồn: `D:\CauHinh\FE_Bbhome`  
Backend đích: `D:\BE_BBHomes\Project`

## Kết luận

**PASS — dữ liệu hardcode có ý nghĩa nghiệp vụ của Frontend đã được đóng gói và chuyển vào cơ chế seed Database của Backend.**

Seeder có thể chạy trên môi trường mới mà không cần truy cập lại thư mục Frontend. Dữ liệu được upsert theo khóa nghiệp vụ và đã được xác minh chạy lặp lại không tạo bản ghi media, gallery hoặc quan hệ phòng bị trùng.

## Nguồn dữ liệu đã rà soát

| Nguồn | Nội dung | Kết quả |
|---|---|---|
| `src/lib/rooms.ts` | 4 hạng phòng, mã hiển thị, diện tích, sức chứa, giá, thứ tự, đường dẫn ảnh | PASS |
| `src/lib/gallery.ts` | 18 ảnh gallery và 3 nhóm lọc | PASS |
| `src/messages/vi.json` | Tên/mô tả phòng, 32 liên kết tiện ích, 6 dịch vụ, thông tin liên hệ và toàn bộ nội dung UI tiếng Việt | PASS |
| `src/messages/ja.json` | Nội dung tiếng Nhật tương ứng | PASS |
| Các page/component trong `src` | Ảnh hero, ngoại cảnh, phòng và tiện ích được khai báo trực tiếp | PASS |
| `public/bbhomes/**` | 30 ảnh phòng/facade/amenities được phòng và gallery tham chiếu | PASS |
| `public/ImageBbhomes/ImageRoom/**` | 4 ảnh hardcode bổ sung trên trang chủ/contact | PASS |

Các chuỗi điều khiển giao diện như nhãn nút, validation, menu, FAQ và nội dung trang không có bảng nghiệp vụ riêng. Chúng vẫn được lưu nguyên bản trong `system_settings.frontend_content_vi` và `system_settings.frontend_content_ja`, đồng thời giữ trong seed assets để không thất thoát dữ liệu.

## Mapping Database

| Dữ liệu nguồn | Bảng đích | Kết quả |
|---|---|---|
| Premier, Balcony, Deluxe City View, Standard | `room_types` | 4 phòng, đúng giá/diện tích/sức chứa/thứ tự |
| 6 ảnh cho mỗi phòng | `media_files`, `room_type_media`, `room_types.featured_media_id` | 24 liên kết ảnh; cover là featured |
| Danh sách tiện ích song ngữ của từng phòng | `amenities`, `room_type_amenities` | 24 tiện ích duy nhất, 32 liên kết phòng-tiện ích |
| 6 dịch vụ lưu trú | `services` | Upsert theo slug |
| Rooms / Spaces / Details | `gallery_categories` | 3 nhóm nghiệp vụ |
| 18 mục gallery | `gallery_items` | Đúng category và display order |
| 34 ảnh hardcode | `media_files` và kho media vật lý | Đủ kích thước, MIME, URL và 3 biến thể WebP |
| Địa chỉ, hotline, email, giờ nhận/trả phòng | `system_settings.site_metadata` | Có dữ liệu VI/JA |
| Toàn bộ JSON nội dung VI/JA | `system_settings` | `frontend_content_vi`, `frontend_content_ja` |
| Manifest mapping/audit | `system_settings` | `frontend_seed_manifest`, `frontend_seed_source` |

## Thay đổi đã thực hiện

- Tạo `FrontendContentSeeder` để đọc manifest và nội dung song ngữ, upsert dữ liệu theo khóa nghiệp vụ.
- Thay dữ liệu mẫu cũ trong luồng startup bằng seeder từ Frontend.
- Tích hợp `IImageProcessor` và `IMediaStorage` vào quá trình seed.
- Đóng gói toàn bộ ảnh được tham chiếu và tệp dịch VI/JA trong output/publish của Infrastructure.
- Tạo manifest khai báo phòng, gallery và các media bổ sung.
- Ảnh PNG/JPEG nguồn được chuyển thành `original.webp`, `medium.webp`, `thumbnail.webp`; đường dẫn công khai được ghi vào `media_files`.
- Seeder bảo toàn dữ liệu quản trị hiện có: cập nhật bản ghi cùng khóa, bổ sung bản ghi thiếu và không xóa nội dung do người dùng tạo.

## Kết quả xác minh

| Kiểm tra | Kết quả |
|---|---|
| `dotnet build HBP.sln --no-restore` | PASS — 0 warning, 0 error |
| Unit tests | PASS — 34/34 |
| Integration tests | PASS — 19/19 |
| Docker image build và API startup | PASS |
| Migration trước seed | PASS — database up to date |
| API `GET /api/rooms`, VI/JA | PASS — 4 phòng |
| Quan hệ ảnh phòng | PASS — 6 ảnh/phòng |
| Quan hệ tiện ích phòng | PASS — 8 tiện ích/phòng |
| Gallery | PASS — 18 mục từ Frontend |
| Media metadata | PASS — 34/34 có path, URL, width, height |
| Phục vụ ảnh qua HTTP | PASS — HTTP 200, `image/webp` |
| Chạy seeder lần hai | PASS — số lượng media 34, room media 24, gallery 18 không đổi |

## Dữ liệu chưa thể import

Không có ảnh bị thiếu trong các đường dẫn hardcode được phát hiện. Toàn bộ 34 tài sản ảnh liên quan đã được đóng gói và import tự động.

Các SVG mặc định của template Next.js (`file.svg`, `globe.svg`, `next.svg`, `vercel.svg`, `window.svg`) không phải dữ liệu nghiệp vụ BB Homes và không được đưa vào Database.

## Lưu ý triển khai

- Bật `Database:SeedOnStartup=true` cho lần khởi tạo môi trường hoặc gọi cùng luồng `SeedData.InitializeAsync`.
- Thư mục media phải có quyền ghi và cần volume bền vững. Docker Compose hiện dùng volume `media`.
- Lần seed đầu xử lý ảnh độ phân giải cao nên có thể mất vài phút; các lần sau dùng bản ghi đã có và hoàn tất nhanh.
- Không tắt hoặc loại bỏ `Persistence/Seed/Assets` khỏi publish output.
- Database phát triển đã có một số service/category mẫu cũ không trùng khóa nghiệp vụ. Seeder chủ động không xóa chúng để tránh xóa dữ liệu ngoài phạm vi; database sạch sẽ chỉ nhận bộ dữ liệu mới.

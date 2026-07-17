# HBP Frontend — Implementation Plan

## 1. Mục tiêu và phạm vi

Frontend gồm hai khu vực dùng chung design system:

1. **Public Website**: website khách sạn song ngữ Việt/Nhật, tối ưu SEO/ISR, xem phòng–dịch vụ–gallery và gửi booking/contact.
2. **Admin Portal**: đăng nhập, quản trị nội dung/media/lead/dashboard. Dựng shell và auth trước; các màn CRUD hoàn thiện khi BE Phase 7 sẵn sàng.

Frontend nên nằm ở repo riêng `FE_HBP`, cùng cấp với repo `BE_HBP`, vì hai ứng dụng có pipeline build và container deploy riêng.

## 2. Định hướng hình ảnh

Phong cách: **quiet luxury / editorial boutique hotel**, lấy cảm hứng từ ảnh tham chiếu nhưng không sao chép nguyên mẫu.

### 2.1. Visual language

- Hero toàn màn hình với ảnh kiến trúc/phòng giàu texture, overlay than 35–50%.
- Heading serif tương phản cao; body và navigation dùng sans-serif hẹp, tracking rộng.
- Bảng màu:
  - `ink`: `#1E1E1C`
  - `charcoal`: `#292824`
  - `ivory`: `#F6F2EB`
  - `paper`: `#FFFCF7`
  - `champagne`: `#B79263`
  - `muted`: `#817A70`
- Grid rộng, nhiều khoảng thở; section ảnh/text bất đối xứng.
- Card phòng dùng ảnh lớn, gradient tối ở đáy, typography đặt trực tiếp trên ảnh.
- Border mảnh 1px, nút uppercase có letter-spacing; hạn chế bo tròn và shadow.
- Chuyển động 180–500ms: fade/clip reveal, image scale nhẹ, stagger card; tôn trọng `prefers-reduced-motion`.
- Mobile giữ cảm giác cao cấp bằng typography co giãn, horizontal snap cho card và booking CTA dạng sticky bottom.

### 2.2. Typography

- Display serif: ưu tiên font có giấy phép web rõ ràng như Cormorant Garamond hoặc Noto Serif Display.
- Sans-serif: Be Vietnam Pro cho tiếng Việt; Noto Sans JP fallback cho tiếng Nhật.
- Dùng `next/font`; kiểm tra đầy đủ dấu tiếng Việt và Japanese glyph trước khi chốt.

### 2.3. Yêu cầu ảnh

- Hero desktop tối thiểu 2400×1350; mobile crop riêng 1080×1440.
- Room/service: tỷ lệ 4:5 và 3:2; gallery giữ aspect ratio gốc.
- Luôn dùng URL `medium` cho grid, `original` cho hero/detail, `thumbnail` cho preview nhỏ.
- Dùng placeholder blur hoặc dominant-color skeleton; tránh layout shift.

## 3. Stack kỹ thuật

- Next.js App Router bản stable mới nhất, TypeScript strict, React.
- Tailwind CSS v4 cho token/layout; CSS Modules chỉ dùng cho animation/layout đặc thù.
- `next-intl` hoặc dictionary server-side cho `vi`/`ja`; route có prefix `/{lang}`.
- Zod + React Hook Form cho booking/contact/admin forms.
- TanStack Query chỉ dùng cho Admin Portal và mutation cần đồng bộ client state; public pages fetch ở Server Components.
- Motion: Motion for React, chỉ load trong các client island cần animation.
- Icons: Lucide; không dùng icon emoji trong UI production.
- Testing: Vitest + React Testing Library; Playwright cho luồng public booking và admin auth.
- Package manager: pnpm; commit `pnpm-lock.yaml`.

Yêu cầu máy dev: Node.js >= 20.9 và pnpm hiện hành.

## 4. Kiến trúc thư mục mục tiêu

```text
FE_HBP/
  src/
    app/
      [lang]/
        (public)/
          page.tsx
          rooms/page.tsx
          rooms/[slug]/page.tsx
          services/page.tsx
          gallery/page.tsx
          contact/page.tsx
          booking/page.tsx
        admin/
          login/page.tsx
          (protected)/layout.tsx
          (protected)/dashboard/page.tsx
          (protected)/media/page.tsx
          (protected)/rooms/page.tsx
      api/                         # BFF/route handlers khi cần giữ cookie same-origin
      sitemap.ts
      robots.ts
      opengraph-image.tsx
    components/
      ui/
      layout/
      home/
      rooms/
      booking/
      admin/
    features/
      rooms/ services/ gallery/ booking/ contact/ auth/ media/
    lib/
      api/ env/ i18n/ seo/ validation/ utils/
    styles/
      globals.css
      tokens.css
    messages/
      vi.json
      ja.json
  public/
    images/ icons/
  tests/
```

Nguyên tắc:

- Public pages mặc định là Server Components.
- Chỉ booking form, gallery interaction, mobile menu và animation là Client Components.
- DTO TypeScript phản ánh đúng response BE; không để raw `*_vi`/`*_ja` trong FE.
- Một API client server-side và một BFF/client adapter; không gọi `fetch` rải rác trong component.

## 5. Routing và sitemap

### Public

- `/vi`, `/ja`: trang chủ.
- `/vi/rooms`, `/ja/rooms`: danh sách phòng.
- `/vi/rooms/[slug]`: chi tiết phòng.
- `/vi/services`: dịch vụ.
- `/vi/gallery`: hình ảnh.
- `/vi/contact`: liên hệ.
- `/vi/booking`: booking form có thể preselect room bằng query `?room=`.

### Admin

- `/vi/admin/login`.
- `/vi/admin/dashboard`.
- `/vi/admin/media`.
- `/vi/admin/rooms`, amenities, services, gallery, booking requests, contact requests, settings — mở dần theo BE Phase 7.

Admin mặc định dùng tiếng Việt trong MVP; public hỗ trợ đầy đủ VI/JA.

## 6. Tích hợp backend

### API hiện có thể dùng ngay

- `GET /api/rooms`, `GET /api/rooms/{slug}`.
- `GET /api/services`, `GET /api/services/{slug}`.
- `GET /api/gallery`, `GET /api/amenities`.
- `POST /api/booking-requests`, `POST /api/contact-requests`.
- Auth: CSRF/login/logout/me.
- Admin media: upload/list/detail/delete.

### Fetch strategy

- Public GET: server fetch với `Accept-Language` hoặc `?lang=`, `next.revalidate = 60`.
- Tôn trọng ETag của BE; ở FE ưu tiên Next cache/revalidation và dùng ETag cho request trực tiếp/BFF.
- Booking/contact: gọi BFF route handler để chuẩn hóa lỗi ProblemDetails và tránh lộ chi tiết API origin.
- Admin cookie: ưu tiên reverse proxy/BFF cùng origin. Mọi mutation gửi `X-HBP-CSRF`; request có `credentials: include`.
- Env:
  - `HBP_API_INTERNAL_URL`: URL API dùng server/container network.
  - `NEXT_PUBLIC_HBP_API_URL`: chỉ dùng khi browser thực sự cần gọi API trực tiếp.
  - `NEXT_PUBLIC_SITE_URL`: canonical URL.

## 7. Component inventory

### Global

- `SiteHeader`, transparent-over-hero và solid-on-scroll.
- `DesktopNav`, `MobileDrawer`, `LanguageSwitcher`.
- `SectionEyebrow`, `EditorialHeading`, `GoldDivider`.
- `ImageFrame`, `ResponsivePicture`, `Reveal`.
- `BookingBar`, `BookingForm`, `PhoneCTA`.
- `SiteFooter`, address/social/legal.

### Trang chủ

1. Full-bleed hero + headline + CTA.
2. Booking bar desktop/sticky booking CTA mobile.
3. Intro editorial split layout.
4. Featured room masonry/grid.
5. Service section nền charcoal.
6. Gallery strip.
7. Booking banner/form overlay.
8. Footer.

### Rooms

- Editorial room grid, responsive 3/2/1 cột và một featured card ngang.
- Detail: gallery, overview, amenities, capacity/area/bed, pricing/contact mode, booking CTA.
- `CONTACT`: không render vùng giá; CTA đổi thành “Liên hệ để nhận giá”.

### Forms

- Booking: date fields độc lập đúng BR-BOOK-014; room optional; adults >= 1; children >= 0.
- Contact: name/email/phone/subject/message.
- Honeypot `website` tồn tại trong payload nhưng ẩn khỏi người dùng và accessibility tree.
- Thành công hiển thị reference code; lỗi map từ RFC7807 `extensions.errors`.

## 8. Responsive và accessibility

- Breakpoint thiết kế: 375, 768, 1024, 1440, 1920; không thiết kế chỉ theo breakpoint Tailwind mặc định.
- WCAG 2.2 AA: contrast, focus-visible, keyboard navigation, skip link, semantic heading.
- Dialog/menu có focus trap và ESC close.
- Alt lấy từ API; ảnh trang trí dùng alt rỗng.
- Form có label thật, mô tả lỗi liên kết bằng `aria-describedby`, live region cho submit state.
- Không dùng animation gây chóng mặt; hỗ trợ reduced motion.

## 9. SEO và hiệu năng

- `generateMetadata` theo locale và room/service detail.
- Canonical + `hreflang` vi/ja.
- `sitemap.ts`, `robots.ts`, Open Graph image, JSON-LD `Hotel`, `HotelRoom`, `BreadcrumbList`.
- Mục tiêu Lighthouse production: Performance >= 90, Accessibility >= 95, SEO >= 95.
- LCP < 2.5s, CLS < 0.1, INP < 200ms trên mobile trung bình.
- Hero preload có kiểm soát; các ảnh dưới fold lazy load; tránh carousel JS nặng.

## 10. Các phase triển khai

### FE-0 — Đóng checkpoint và bootstrap

- Commit/push toàn bộ thay đổi BE hiện tại trước khi chuyển context.
- Tạo repo `FE_HBP`; scaffold Next.js App Router + TypeScript + ESLint + Tailwind v4.
- Thiết lập env validation, formatting, CI build/typecheck.

**DoD:** `pnpm lint`, `pnpm typecheck`, `pnpm build` xanh.

### FE-1 — Design system và application shell

- Token màu/type/spacing/container.
- Font VI/JA, header/footer/nav/language routing.
- Story/demo page kiểm tra button, field, card, typography, responsive.

**DoD:** shell hoàn chỉnh ở 375/768/1440; keyboard và reduced-motion pass.

### FE-2 — Public homepage

- Hero, intro, room preview, services, gallery teaser, booking band.
- Ban đầu có fixture data; chuyển sang API ngay khi layout ổn.

**DoD:** khớp art direction tham chiếu, không copy logo/nội dung/ảnh; Lighthouse baseline >= 85.

### FE-3 — Rooms/services/gallery

- API client typed, loading/error/empty states.
- Room list/detail; services; gallery filter.
- ISR 60s, metadata, structured data.

**DoD:** VI/JA fallback đúng; room ẩn 404; CONTACT không có giá; ảnh variant đúng ngữ cảnh.

### FE-4 — Booking/contact

- Booking bar → booking page; form validation; submit; reference success.
- Contact page/form; rate-limit UX và RFC7807 mapping.

**DoD:** submit thật vào BE; double-click safe; 400/429/network error có UX rõ ràng.

### FE-5 — Admin shell/auth/media

- Login, CSRF, session check, protected layout, logout.
- Media library/upload/delete/in-use error.
- Sidebar/header/responsive admin layout.

**DoD:** refresh giữ session; 401 quay về login; mutation thiếu CSRF bị chặn; media flow chạy thật.

### FE-6 — Admin business screens

- Dashboard, catalogs, room editor, leads, settings sau khi BE Phase 7 hoàn thành.

**DoD:** toàn bộ catalog endpoint có CRUD UI; optimistic update chỉ dùng nơi rollback an toàn.

### FE-7 — Testing, polish và deploy

- Component/unit tests; Playwright critical journeys.
- Accessibility audit, responsive QA, SEO/performance.
- Dockerfile non-root, Coolify env/domain, error monitoring tùy chọn.

**DoD:** build production xanh; public booking và admin login/media E2E pass; mobile/desktop smoke pass.

## 11. Test matrix ưu tiên

1. Locale redirect và language switch giữ nguyên route.
2. Room CONTACT không render giá.
3. Room detail ẩn trả 404.
4. Booking success hiển thị reference code.
5. Validation field và 429 state.
6. Admin login cookie + CSRF + logout.
7. Media upload trả đủ ba variant và in-use delete hiển thị references.
8. Keyboard mobile menu, form và gallery.
9. Visual regression tại 375/768/1440.

## 12. Phụ thuộc cần chốt

- Tên khách sạn, logo và brand kit chính thức.
- Bộ ảnh có quyền sử dụng; ảnh hero desktop/mobile.
- Nội dung giới thiệu, địa chỉ, hotline, social links bằng VI/JA.
- Domain production/staging.
- Quyết định repo `FE_HBP` riêng (kế hoạch này mặc định là riêng).
- Admin status lifecycle vẫn phụ thuộc quyết định mở rộng enum ở BE.

## 13. Ước lượng

- FE-0 + FE-1: 1.5–2 ngày.
- FE-2: 2–3 ngày.
- FE-3: 2–3 ngày.
- FE-4: 1.5–2 ngày.
- FE-5: 2 ngày.
- FE-6: 3–4 ngày sau BE Phase 7.
- FE-7: 1.5–2 ngày.

Tổng public MVP + auth/media admin: khoảng 9–12 ngày dev tập trung. Toàn bộ admin CRUD và hardening: khoảng 14–18 ngày, chưa gồm thời gian chờ nội dung/ảnh/feedback.

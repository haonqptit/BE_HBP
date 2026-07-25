using System.Text.Json;
using HBP.Application.Abstractions;
using HBP.Domain.Entities;
using HBP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HBP.Infrastructure.Persistence.Seed;

public static class SeedData
{
    public static async Task InitializeAsync(
        HbpDbContext db,
        IPasswordHasher hasher,
        IImageProcessor imageProcessor,
        IMediaStorage mediaStorage,
        CancellationToken cancellationToken = default)
    {
        var username = Environment.GetEnvironmentVariable("HBP_SEED_ADMIN_USERNAME");
        var email = Environment.GetEnvironmentVariable("HBP_SEED_ADMIN_EMAIL");
        var password = Environment.GetEnvironmentVariable("HBP_SEED_ADMIN_PASSWORD");

        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password)
            && !await db.AdminUsers.AnyAsync(cancellationToken))
        {
            db.AdminUsers.Add(new AdminUser
            {
                Username = username.Trim(), Email = email.Trim(), PasswordHash = hasher.Hash(password), IsActive = true
            });
        }

        if (!await db.SystemSettings.AnyAsync(x => x.Key == "notification_emails", cancellationToken))
        {
            db.SystemSettings.Add(new SystemSetting
            {
                Key = "notification_emails",
                Value = JsonSerializer.Serialize(Array.Empty<string>()),
                Description = "Recipients for booking/contact notifications"
            });
        }

        var siteMetadata = await db.SystemSettings.SingleOrDefaultAsync(x => x.Key == "site_metadata", cancellationToken);
        var defaultSiteMetadata = JsonSerializer.Serialize(new
        {
            name = "BB Homes",
            addressVi = "95/12 Đào Tấn, Ba Đình, Hà Nội",
            addressJa = "95/12 ダオタン通り、バーディン区、ハノイ",
            phone = "084 456 5665",
            email = "admin@bbhomesserviced.com",
            checkInVi = "Nhận phòng từ 14:00",
            checkInJa = "チェックイン 14:00から",
            checkOutVi = "Trả phòng trước 12:00",
            checkOutJa = "チェックアウト 12:00まで",
            receptionVi = "Lễ tân 24/7",
            receptionJa = "フロント 24時間対応"
        });
        if (siteMetadata is null)
        {
            db.SystemSettings.Add(new SystemSetting
            {
                Key = "site_metadata", Value = defaultSiteMetadata, Description = "Public site metadata"
            });
        }
        else if (siteMetadata.Value == "{}")
        {
            // Upgrade the original empty development seed without overwriting configured metadata.
            siteMetadata.Value = defaultSiteMetadata;
        }

        await FrontendContentSeeder.SeedAsync(db, imageProcessor, mediaStorage, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Sample content so a fresh database renders something. Each block is skipped once its table
    /// holds any row, so editing or deleting the samples is never undone by a later start-up.
    /// Media references are left null — images come from the admin upload flow.
    /// </summary>
    private static async Task SeedContentAsync(HbpDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.Amenities.AnyAsync(cancellationToken))
        {
            db.Amenities.AddRange(
                new Amenity { NameVi = "Wi-Fi miễn phí", NameJa = "無料Wi-Fi", Icon = "wifi", DisplayOrder = 0, IsVisible = true },
                new Amenity { NameVi = "Điều hòa", NameJa = "エアコン", Icon = "air-conditioning", DisplayOrder = 1, IsVisible = true },
                new Amenity { NameVi = "Bữa sáng", NameJa = "朝食", Icon = "breakfast", DisplayOrder = 2, IsVisible = true },
                new Amenity { NameVi = "Bãi đỗ xe", NameJa = "駐車場", Icon = "parking", DisplayOrder = 3, IsVisible = true });
        }

        if (!await db.RoomTypes.AnyAsync(cancellationToken))
        {
            db.RoomTypes.AddRange(
                new RoomType
                {
                    Code = "STD", Slug = "phong-tieu-chuan", NameVi = "Phòng tiêu chuẩn", NameJa = "スタンダードルーム",
                    ShortDescriptionVi = "Phòng tiêu chuẩn cho hai khách.", ShortDescriptionJa = "2名様向けのスタンダードルーム。",
                    PriceVnd = 900_000m, PriceDisplayMode = PriceDisplayMode.SHOW_PRICE, Capacity = 2,
                    AreaSquareMeters = 24m, BedDescriptionVi = "1 giường đôi", BedDescriptionJa = "ダブルベッド1台",
                    DisplayOrder = 0, IsVisible = true
                },
                new RoomType
                {
                    Code = "DLX", Slug = "phong-deluxe", NameVi = "Phòng Deluxe", NameJa = "デラックスルーム",
                    ShortDescriptionVi = "Phòng rộng có ban công.", ShortDescriptionJa = "バルコニー付きの広いお部屋。",
                    PriceDisplayMode = PriceDisplayMode.CONTACT, Capacity = 3, AreaSquareMeters = 32m,
                    BedDescriptionVi = "1 giường lớn và 1 giường đơn", BedDescriptionJa = "キングベッド1台とシングルベッド1台",
                    DisplayOrder = 1, IsVisible = true
                });
        }

        if (!await db.Services.AnyAsync(cancellationToken))
        {
            db.Services.AddRange(
                new Service
                {
                    Slug = "dua-don-san-bay", NameVi = "Đưa đón sân bay", NameJa = "空港送迎",
                    ShortDescriptionVi = "Xe riêng đón khách tại sân bay.", ShortDescriptionJa = "空港での専用車のお出迎え。",
                    PriceNoteVi = "Liên hệ để báo giá", PriceNoteJa = "料金はお問い合わせください",
                    DisplayOrder = 0, IsVisible = true
                },
                new Service
                {
                    Slug = "giat-la", NameVi = "Giặt là", NameJa = "ランドリー",
                    ShortDescriptionVi = "Nhận trong ngày.", ShortDescriptionJa = "当日仕上げ。",
                    DisplayOrder = 1, IsVisible = true
                });
        }

        if (!await db.GalleryCategories.AnyAsync(cancellationToken))
        {
            db.GalleryCategories.AddRange(
                new GalleryCategory { Slug = "phong-nghi", NameVi = "Phòng nghỉ", NameJa = "客室", DisplayOrder = 0, IsVisible = true },
                new GalleryCategory { Slug = "tien-ich", NameVi = "Tiện ích", NameJa = "施設", DisplayOrder = 1, IsVisible = true });
        }
    }
}

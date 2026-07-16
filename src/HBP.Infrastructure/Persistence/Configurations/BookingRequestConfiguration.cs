using HBP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HBP.Infrastructure.Persistence.Configurations;

public class BookingRequestConfiguration : IEntityTypeConfiguration<BookingRequest>
{
    public void Configure(EntityTypeBuilder<BookingRequest> builder)
    {
        builder.ToTable("booking_requests", t =>
        {
            t.HasCheckConstraint("ck_booking_requests_adults", "adults >= 1");
            t.HasCheckConstraint("ck_booking_requests_children", "children IS NULL OR children >= 0");
            t.HasCheckConstraint("ck_booking_requests_number_of_rooms",
                "number_of_rooms IS NULL OR number_of_rooms >= 1");
        });

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.ReferenceCode)
            .HasColumnName("reference_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.RoomTypeId)
            .HasColumnName("room_type_id");

        builder.Property(e => e.CheckInDate)
            .HasColumnName("check_in_date")
            .HasColumnType("date");

        builder.Property(e => e.CheckOutDate)
            .HasColumnName("check_out_date")
            .HasColumnType("date");

        builder.Property(e => e.Adults)
            .HasColumnName("adults")
            .IsRequired();

        builder.Property(e => e.Children)
            .HasColumnName("children");

        builder.Property(e => e.NumberOfRooms)
            .HasColumnName("number_of_rooms");

        builder.Property(e => e.CustomerMessage)
            .HasColumnName("customer_message")
            .HasColumnType("text");

        builder.Property(e => e.LanguageCode)
            .HasColumnName("language_code")
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasDefaultValue(Domain.Enums.BookingRequestStatus.RECEIVED)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(e => e.ReferenceCode)
            .IsUnique()
            .HasDatabaseName("uq_booking_requests_reference_code");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("idx_booking_requests_created_at")
            .IsDescending();

        builder.HasIndex(e => e.RoomTypeId)
            .HasDatabaseName("idx_booking_requests_room_type_id");

        // NOTE: schema.sql also declares GIN pg_trgm indexes on full_name / email / phone_number
        // (idx_booking_requests_*_trgm). GIN trigram indexes cannot be expressed in the EF model
        // and will be added as raw SQL in the migration step.

        builder.HasOne(e => e.RoomType)
            .WithMany()
            .HasForeignKey(e => e.RoomTypeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

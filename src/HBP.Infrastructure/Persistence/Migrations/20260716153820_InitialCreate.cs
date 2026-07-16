using System;
using HBP.Domain.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HBP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:public.booking_request_status", "RECEIVED")
                .Annotation("Npgsql:Enum:public.email_status", "PENDING,SENT,RETRYING,FAILED")
                .Annotation("Npgsql:Enum:public.language_code_enum", "vi,ja")
                .Annotation("Npgsql:Enum:public.price_display_mode", "SHOW_PRICE,CONTACT")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "admin_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "amenities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name_vi = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name_ja = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    icon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_amenities", x => x.id);
                    table.CheckConstraint("ck_amenities_display_order", "display_order >= 0");
                });

            migrationBuilder.CreateTable(
                name: "contact_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    reference_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    language_code = table.Column<LanguageCode>(type: "language_code_enum", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_deliveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    related_entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    related_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    recipient = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    language_code = table.Column<LanguageCode>(type: "language_code_enum", nullable: false),
                    status = table.Column<EmailStatus>(type: "email_status", nullable: false, defaultValue: EmailStatus.PENDING),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    next_retry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_deliveries", x => x.id);
                    table.CheckConstraint("ck_email_deliveries_attempt_count", "attempt_count >= 0");
                    table.CheckConstraint("ck_email_deliveries_related_entity_type", "related_entity_type IN ('BookingRequest', 'ContactRequest')");
                });

            migrationBuilder.CreateTable(
                name: "gallery_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    slug = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name_vi = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name_ja = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gallery_categories", x => x.id);
                    table.CheckConstraint("ck_gallery_categories_display_order", "display_order >= 0");
                });

            migrationBuilder.CreateTable(
                name: "media_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    stored_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    storage_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    public_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    alt_text_vi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    alt_text_ja = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_files", x => x.id);
                    table.CheckConstraint("ck_media_files_size_bytes", "size_bytes >= 0");
                });

            migrationBuilder.CreateTable(
                name: "system_settings",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_settings", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "gallery_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    media_file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gallery_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    caption_vi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    caption_ja = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gallery_items", x => x.id);
                    table.CheckConstraint("ck_gallery_items_display_order", "display_order >= 0");
                    table.ForeignKey(
                        name: "FK_gallery_items_gallery_categories_gallery_category_id",
                        column: x => x.gallery_category_id,
                        principalTable: "gallery_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gallery_items_media_files_media_file_id",
                        column: x => x.media_file_id,
                        principalTable: "media_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "room_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    slug = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name_vi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name_ja = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    short_description_vi = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    short_description_ja = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description_vi = table.Column<string>(type: "text", nullable: true),
                    description_ja = table.Column<string>(type: "text", nullable: true),
                    price_vnd = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    price_usd = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    price_display_mode = table.Column<PriceDisplayMode>(type: "price_display_mode", nullable: false, defaultValue: PriceDisplayMode.CONTACT),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    area_square_meters = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    bed_description_vi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    bed_description_ja = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    featured_media_id = table.Column<Guid>(type: "uuid", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    seo_title_vi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    seo_title_ja = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    seo_description_vi = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    seo_description_ja = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_types", x => x.id);
                    table.CheckConstraint("ck_room_types_capacity", "capacity >= 1");
                    table.CheckConstraint("ck_room_types_display_order", "display_order >= 0");
                    table.CheckConstraint("ck_room_types_price_usd", "price_usd IS NULL OR price_usd >= 0");
                    table.CheckConstraint("ck_room_types_price_vnd", "price_vnd IS NULL OR price_vnd >= 0");
                    table.ForeignKey(
                        name: "FK_room_types_media_files_featured_media_id",
                        column: x => x.featured_media_id,
                        principalTable: "media_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "services",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    slug = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name_vi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name_ja = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    short_description_vi = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    short_description_ja = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description_vi = table.Column<string>(type: "text", nullable: true),
                    description_ja = table.Column<string>(type: "text", nullable: true),
                    price_note_vi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    price_note_ja = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    featured_media_id = table.Column<Guid>(type: "uuid", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_services", x => x.id);
                    table.CheckConstraint("ck_services_display_order", "display_order >= 0");
                    table.ForeignKey(
                        name: "FK_services_media_files_featured_media_id",
                        column: x => x.featured_media_id,
                        principalTable: "media_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "booking_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    reference_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    room_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    check_in_date = table.Column<DateOnly>(type: "date", nullable: true),
                    check_out_date = table.Column<DateOnly>(type: "date", nullable: true),
                    adults = table.Column<int>(type: "integer", nullable: false),
                    children = table.Column<int>(type: "integer", nullable: true),
                    number_of_rooms = table.Column<int>(type: "integer", nullable: true),
                    customer_message = table.Column<string>(type: "text", nullable: true),
                    language_code = table.Column<LanguageCode>(type: "language_code_enum", nullable: false),
                    status = table.Column<BookingRequestStatus>(type: "booking_request_status", nullable: false, defaultValue: BookingRequestStatus.RECEIVED),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_requests", x => x.id);
                    table.CheckConstraint("ck_booking_requests_adults", "adults >= 1");
                    table.CheckConstraint("ck_booking_requests_children", "children IS NULL OR children >= 0");
                    table.CheckConstraint("ck_booking_requests_number_of_rooms", "number_of_rooms IS NULL OR number_of_rooms >= 1");
                    table.ForeignKey(
                        name: "FK_booking_requests_room_types_room_type_id",
                        column: x => x.room_type_id,
                        principalTable: "room_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "room_type_amenities",
                columns: table => new
                {
                    room_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amenity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_type_amenities", x => new { x.room_type_id, x.amenity_id });
                    table.ForeignKey(
                        name: "FK_room_type_amenities_amenities_amenity_id",
                        column: x => x.amenity_id,
                        principalTable: "amenities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_room_type_amenities_room_types_room_type_id",
                        column: x => x.room_type_id,
                        principalTable: "room_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "room_type_media",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    room_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_type_media", x => x.id);
                    table.CheckConstraint("ck_room_type_media_display_order", "display_order >= 0");
                    table.ForeignKey(
                        name: "FK_room_type_media_media_files_media_file_id",
                        column: x => x.media_file_id,
                        principalTable: "media_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_room_type_media_room_types_room_type_id",
                        column: x => x.room_type_id,
                        principalTable: "room_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "uq_admin_users_email_lower",
                table: "admin_users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_admin_users_username_lower",
                table: "admin_users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_booking_requests_created_at",
                table: "booking_requests",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_booking_requests_room_type_id",
                table: "booking_requests",
                column: "room_type_id");

            migrationBuilder.CreateIndex(
                name: "uq_booking_requests_reference_code",
                table: "booking_requests",
                column: "reference_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_contact_requests_created_at",
                table: "contact_requests",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "uq_contact_requests_reference_code",
                table: "contact_requests",
                column: "reference_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_email_deliveries_next_retry",
                table: "email_deliveries",
                column: "next_retry_at",
                filter: "status = 'RETRYING'");

            migrationBuilder.CreateIndex(
                name: "idx_email_deliveries_related_entity",
                table: "email_deliveries",
                columns: new[] { "related_entity_type", "related_entity_id" });

            migrationBuilder.CreateIndex(
                name: "idx_email_deliveries_status",
                table: "email_deliveries",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "uq_gallery_categories_slug",
                table: "gallery_categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_gallery_items_category_visible_order",
                table: "gallery_items",
                columns: new[] { "gallery_category_id", "is_visible", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_gallery_items_media_file_id",
                table: "gallery_items",
                column: "media_file_id");

            migrationBuilder.CreateIndex(
                name: "IX_room_type_amenities_amenity_id",
                table: "room_type_amenities",
                column: "amenity_id");

            migrationBuilder.CreateIndex(
                name: "IX_room_type_media_media_file_id",
                table: "room_type_media",
                column: "media_file_id");

            migrationBuilder.CreateIndex(
                name: "uq_room_type_media",
                table: "room_type_media",
                columns: new[] { "room_type_id", "media_file_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_room_types_visible_order",
                table: "room_types",
                columns: new[] { "is_visible", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_room_types_featured_media_id",
                table: "room_types",
                column: "featured_media_id");

            migrationBuilder.CreateIndex(
                name: "uq_room_types_code",
                table: "room_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_room_types_slug",
                table: "room_types",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_services_visible_order",
                table: "services",
                columns: new[] { "is_visible", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_services_featured_media_id",
                table: "services",
                column: "featured_media_id");

            migrationBuilder.CreateIndex(
                name: "uq_services_slug",
                table: "services",
                column: "slug",
                unique: true);

            // EF cannot model case-insensitive expression indexes, pg_trgm operator
            // classes, or PostgreSQL trigger functions. Keep these schema.sql details
            // explicit and reversible here.
            migrationBuilder.DropIndex(name: "uq_admin_users_email_lower", table: "admin_users");
            migrationBuilder.DropIndex(name: "uq_admin_users_username_lower", table: "admin_users");

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX uq_admin_users_username_lower ON admin_users (lower(username));
                CREATE UNIQUE INDEX uq_admin_users_email_lower ON admin_users (lower(email));

                CREATE INDEX idx_booking_requests_full_name_trgm
                    ON booking_requests USING gin (full_name gin_trgm_ops);
                CREATE INDEX idx_booking_requests_email_trgm
                    ON booking_requests USING gin (email gin_trgm_ops);
                CREATE INDEX idx_booking_requests_phone_trgm
                    ON booking_requests USING gin (phone_number gin_trgm_ops);
                CREATE INDEX idx_contact_requests_full_name_trgm
                    ON contact_requests USING gin (full_name gin_trgm_ops);
                CREATE INDEX idx_contact_requests_email_trgm
                    ON contact_requests USING gin (email gin_trgm_ops);

                CREATE OR REPLACE FUNCTION set_updated_at()
                RETURNS TRIGGER AS $$
                BEGIN
                    NEW.updated_at = now();
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE OR REPLACE FUNCTION normalize_email()
                RETURNS TRIGGER AS $$
                BEGIN
                    NEW.email = lower(trim(NEW.email));
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_admin_users_updated_at BEFORE UPDATE ON admin_users
                    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
                CREATE TRIGGER trg_admin_users_normalize_email BEFORE INSERT OR UPDATE ON admin_users
                    FOR EACH ROW EXECUTE FUNCTION normalize_email();
                CREATE TRIGGER trg_room_types_updated_at BEFORE UPDATE ON room_types
                    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
                CREATE TRIGGER trg_amenities_updated_at BEFORE UPDATE ON amenities
                    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
                CREATE TRIGGER trg_services_updated_at BEFORE UPDATE ON services
                    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
                CREATE TRIGGER trg_gallery_categories_updated_at BEFORE UPDATE ON gallery_categories
                    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
                CREATE TRIGGER trg_gallery_items_updated_at BEFORE UPDATE ON gallery_items
                    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
                CREATE TRIGGER trg_booking_requests_normalize_email BEFORE INSERT OR UPDATE ON booking_requests
                    FOR EACH ROW EXECUTE FUNCTION normalize_email();
                CREATE TRIGGER trg_contact_requests_normalize_email BEFORE INSERT OR UPDATE ON contact_requests
                    FOR EACH ROW EXECUTE FUNCTION normalize_email();
                CREATE TRIGGER trg_system_settings_updated_at BEFORE UPDATE ON system_settings
                    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP FUNCTION IF EXISTS normalize_email() CASCADE;
                DROP FUNCTION IF EXISTS set_updated_at() CASCADE;
                """);

            migrationBuilder.DropTable(
                name: "admin_users");

            migrationBuilder.DropTable(
                name: "booking_requests");

            migrationBuilder.DropTable(
                name: "contact_requests");

            migrationBuilder.DropTable(
                name: "email_deliveries");

            migrationBuilder.DropTable(
                name: "gallery_items");

            migrationBuilder.DropTable(
                name: "room_type_amenities");

            migrationBuilder.DropTable(
                name: "room_type_media");

            migrationBuilder.DropTable(
                name: "services");

            migrationBuilder.DropTable(
                name: "system_settings");

            migrationBuilder.DropTable(
                name: "gallery_categories");

            migrationBuilder.DropTable(
                name: "amenities");

            migrationBuilder.DropTable(
                name: "room_types");

            migrationBuilder.DropTable(
                name: "media_files");
        }
    }
}

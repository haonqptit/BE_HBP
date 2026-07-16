-- ============================================================
-- Hotel Booking Portal (HBP) — PostgreSQL Schema
-- Phiên bản: 1.0
-- Dựa theo SRS v0.9, Mục 13 (Yêu cầu dữ liệu)
-- ============================================================

-- ============================================
-- EXTENSIONS
-- ============================================
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- ============================================
-- ENUM TYPES
-- ============================================
CREATE TYPE price_display_mode AS ENUM ('SHOW_PRICE', 'CONTACT');
CREATE TYPE booking_request_status AS ENUM ('RECEIVED');
CREATE TYPE email_status AS ENUM ('PENDING', 'SENT', 'RETRYING', 'FAILED');
CREATE TYPE language_code_enum AS ENUM ('vi', 'ja');

-- ============================================
-- HELPER FUNCTIONS
-- ============================================
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

-- ============================================
-- TABLE: admin_users
-- ============================================
CREATE TABLE admin_users (
    id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username       VARCHAR(100) NOT NULL,
    email          VARCHAR(255) NOT NULL,
    password_hash  VARCHAR(255) NOT NULL,
    is_active      BOOLEAN NOT NULL DEFAULT TRUE,
    last_login_at  TIMESTAMPTZ,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at     TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX uq_admin_users_username_lower ON admin_users (lower(username));
CREATE UNIQUE INDEX uq_admin_users_email_lower ON admin_users (lower(email));

CREATE TRIGGER trg_admin_users_updated_at
    BEFORE UPDATE ON admin_users
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_admin_users_normalize_email
    BEFORE INSERT OR UPDATE ON admin_users
    FOR EACH ROW EXECUTE FUNCTION normalize_email();

-- ============================================
-- TABLE: media_files
-- ============================================
CREATE TABLE media_files (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    original_file_name  VARCHAR(255) NOT NULL,
    stored_file_name    VARCHAR(255) NOT NULL,
    storage_path        VARCHAR(500) NOT NULL,
    public_url          VARCHAR(500) NOT NULL,
    mime_type           VARCHAR(100) NOT NULL,
    size_bytes          BIGINT NOT NULL CHECK (size_bytes >= 0),
    width               INTEGER,
    height              INTEGER,
    alt_text_vi         VARCHAR(255),
    alt_text_ja         VARCHAR(255),
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- ============================================
-- TABLE: room_types
-- ============================================
CREATE TABLE room_types (
    id                    UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code                  VARCHAR(50) NOT NULL,
    slug                  VARCHAR(150) NOT NULL,
    name_vi               VARCHAR(255) NOT NULL,
    name_ja               VARCHAR(255),
    short_description_vi  VARCHAR(500),
    short_description_ja  VARCHAR(500),
    description_vi        TEXT,
    description_ja        TEXT,
    price_vnd             NUMERIC(14,2) CHECK (price_vnd IS NULL OR price_vnd >= 0),
    price_usd             NUMERIC(10,2) CHECK (price_usd IS NULL OR price_usd >= 0),
    price_display_mode    price_display_mode NOT NULL DEFAULT 'CONTACT',
    capacity              INTEGER NOT NULL CHECK (capacity >= 1),
    area_square_meters    NUMERIC(6,2),
    bed_description_vi    VARCHAR(255),
    bed_description_ja    VARCHAR(255),
    featured_media_id     UUID REFERENCES media_files(id) ON DELETE RESTRICT,
    display_order         INTEGER NOT NULL DEFAULT 0 CHECK (display_order >= 0),
    is_visible            BOOLEAN NOT NULL DEFAULT TRUE,
    seo_title_vi          VARCHAR(255),
    seo_title_ja          VARCHAR(255),
    seo_description_vi    VARCHAR(500),
    seo_description_ja    VARCHAR(500),
    created_at            TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at            TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_room_types_code UNIQUE (code),
    CONSTRAINT uq_room_types_slug UNIQUE (slug)
);

CREATE TRIGGER trg_room_types_updated_at
    BEFORE UPDATE ON room_types
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE INDEX idx_room_types_visible_order ON room_types (is_visible, display_order);

-- ============================================
-- TABLE: amenities
-- ============================================
CREATE TABLE amenities (
    id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name_vi        VARCHAR(150) NOT NULL,
    name_ja        VARCHAR(150),
    icon           VARCHAR(100),
    display_order  INTEGER NOT NULL DEFAULT 0 CHECK (display_order >= 0),
    is_visible     BOOLEAN NOT NULL DEFAULT TRUE,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at     TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TRIGGER trg_amenities_updated_at
    BEFORE UPDATE ON amenities
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- ============================================
-- TABLE: room_type_amenities (junction)
-- ============================================
CREATE TABLE room_type_amenities (
    room_type_id   UUID NOT NULL REFERENCES room_types(id) ON DELETE CASCADE,
    amenity_id     UUID NOT NULL REFERENCES amenities(id) ON DELETE CASCADE,
    display_order  INTEGER DEFAULT 0,
    PRIMARY KEY (room_type_id, amenity_id)
);

-- ============================================
-- TABLE: services
-- ============================================
CREATE TABLE services (
    id                    UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    slug                  VARCHAR(150) NOT NULL,
    name_vi               VARCHAR(255) NOT NULL,
    name_ja               VARCHAR(255),
    short_description_vi  VARCHAR(500),
    short_description_ja  VARCHAR(500),
    description_vi        TEXT,
    description_ja        TEXT,
    price_note_vi         VARCHAR(255),
    price_note_ja         VARCHAR(255),
    featured_media_id     UUID REFERENCES media_files(id) ON DELETE RESTRICT,
    display_order         INTEGER NOT NULL DEFAULT 0 CHECK (display_order >= 0),
    is_visible            BOOLEAN NOT NULL DEFAULT TRUE,
    created_at            TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at            TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_services_slug UNIQUE (slug)
);

CREATE TRIGGER trg_services_updated_at
    BEFORE UPDATE ON services
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE INDEX idx_services_visible_order ON services (is_visible, display_order);

-- ============================================
-- TABLE: room_type_media (junction — ảnh chi tiết)
-- ============================================
CREATE TABLE room_type_media (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    room_type_id    UUID NOT NULL REFERENCES room_types(id) ON DELETE CASCADE,
    media_file_id   UUID NOT NULL REFERENCES media_files(id) ON DELETE RESTRICT,
    display_order   INTEGER NOT NULL DEFAULT 0 CHECK (display_order >= 0),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_room_type_media UNIQUE (room_type_id, media_file_id)
);

-- ============================================
-- TABLE: gallery_categories
-- ============================================
CREATE TABLE gallery_categories (
    id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    slug           VARCHAR(150) NOT NULL,
    name_vi        VARCHAR(150) NOT NULL,
    name_ja        VARCHAR(150),
    display_order  INTEGER NOT NULL DEFAULT 0 CHECK (display_order >= 0),
    is_visible     BOOLEAN NOT NULL DEFAULT TRUE,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_gallery_categories_slug UNIQUE (slug)
);

CREATE TRIGGER trg_gallery_categories_updated_at
    BEFORE UPDATE ON gallery_categories
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- ============================================
-- TABLE: gallery_items
-- ============================================
CREATE TABLE gallery_items (
    id                    UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    media_file_id         UUID NOT NULL REFERENCES media_files(id) ON DELETE RESTRICT,
    gallery_category_id   UUID NOT NULL REFERENCES gallery_categories(id) ON DELETE CASCADE,
    caption_vi            VARCHAR(255),
    caption_ja            VARCHAR(255),
    display_order         INTEGER NOT NULL DEFAULT 0 CHECK (display_order >= 0),
    is_visible            BOOLEAN NOT NULL DEFAULT TRUE,
    created_at            TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at            TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TRIGGER trg_gallery_items_updated_at
    BEFORE UPDATE ON gallery_items
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE INDEX idx_gallery_items_category_visible_order
    ON gallery_items (gallery_category_id, is_visible, display_order);

-- ============================================
-- TABLE: booking_requests
-- ============================================
CREATE TABLE booking_requests (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    reference_code    VARCHAR(50) NOT NULL,
    full_name         VARCHAR(255) NOT NULL,
    email             VARCHAR(255) NOT NULL,
    phone_number      VARCHAR(30) NOT NULL,
    room_type_id      UUID REFERENCES room_types(id) ON DELETE SET NULL,
    check_in_date     DATE,
    check_out_date    DATE,
    adults            INTEGER NOT NULL CHECK (adults >= 1),
    children          INTEGER CHECK (children IS NULL OR children >= 0),
    number_of_rooms   INTEGER CHECK (number_of_rooms IS NULL OR number_of_rooms >= 1),
    customer_message  TEXT,
    language_code     language_code_enum NOT NULL,
    status            booking_request_status NOT NULL DEFAULT 'RECEIVED',
    created_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_booking_requests_reference_code UNIQUE (reference_code)
);

CREATE TRIGGER trg_booking_requests_normalize_email
    BEFORE INSERT OR UPDATE ON booking_requests
    FOR EACH ROW EXECUTE FUNCTION normalize_email();

CREATE INDEX idx_booking_requests_created_at ON booking_requests (created_at DESC);
CREATE INDEX idx_booking_requests_room_type_id ON booking_requests (room_type_id);
CREATE INDEX idx_booking_requests_full_name_trgm
    ON booking_requests USING gin (full_name gin_trgm_ops);
CREATE INDEX idx_booking_requests_email_trgm
    ON booking_requests USING gin (email gin_trgm_ops);
CREATE INDEX idx_booking_requests_phone_trgm
    ON booking_requests USING gin (phone_number gin_trgm_ops);

-- ============================================
-- TABLE: contact_requests
-- ============================================
CREATE TABLE contact_requests (
    id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    reference_code   VARCHAR(50) NOT NULL,
    full_name        VARCHAR(255) NOT NULL,
    email            VARCHAR(255) NOT NULL,
    phone_number     VARCHAR(30) NOT NULL,
    subject          VARCHAR(255) NOT NULL,
    message          TEXT NOT NULL,
    language_code    language_code_enum NOT NULL,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_contact_requests_reference_code UNIQUE (reference_code)
);

CREATE TRIGGER trg_contact_requests_normalize_email
    BEFORE INSERT OR UPDATE ON contact_requests
    FOR EACH ROW EXECUTE FUNCTION normalize_email();

CREATE INDEX idx_contact_requests_created_at ON contact_requests (created_at DESC);
CREATE INDEX idx_contact_requests_full_name_trgm
    ON contact_requests USING gin (full_name gin_trgm_ops);
CREATE INDEX idx_contact_requests_email_trgm
    ON contact_requests USING gin (email gin_trgm_ops);

-- ============================================
-- TABLE: email_deliveries (polymorphic association)
-- ============================================
CREATE TABLE email_deliveries (
    id                    UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    related_entity_type   VARCHAR(50) NOT NULL
                           CHECK (related_entity_type IN ('BookingRequest', 'ContactRequest')),
    related_entity_id     UUID NOT NULL,
    email_type            VARCHAR(50) NOT NULL,
    recipient             VARCHAR(255) NOT NULL,
    language_code         language_code_enum NOT NULL,
    status                email_status NOT NULL DEFAULT 'PENDING',
    attempt_count         INTEGER NOT NULL DEFAULT 0 CHECK (attempt_count >= 0),
    next_retry_at         TIMESTAMPTZ,
    last_attempt_at       TIMESTAMPTZ,
    sent_at               TIMESTAMPTZ,
    last_error            VARCHAR(1000),
    created_at            TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_email_deliveries_status ON email_deliveries (status);
CREATE INDEX idx_email_deliveries_related_entity
    ON email_deliveries (related_entity_type, related_entity_id);
CREATE INDEX idx_email_deliveries_next_retry
    ON email_deliveries (next_retry_at) WHERE status = 'RETRYING';

-- ============================================
-- TABLE: system_settings
-- ============================================
CREATE TABLE system_settings (
    key           VARCHAR(100) PRIMARY KEY,
    value         TEXT,
    description   VARCHAR(255),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TRIGGER trg_system_settings_updated_at
    BEFORE UPDATE ON system_settings
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- ============================================
-- END OF SCHEMA
-- ============================================

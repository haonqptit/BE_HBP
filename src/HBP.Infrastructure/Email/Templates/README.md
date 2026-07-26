# BB Homes email templates

All transactional emails use `Shared/layout.sbn`. Each email type only owns a
localized `subject.sbn` and `body.sbn` content fragment.

## Brand configuration

Configure the `EmailBrand` section in `appsettings.json` or environment
variables using the standard ASP.NET Core double-underscore notation:

- `EmailBrand__WebsiteUrl`
- `EmailBrand__LogoUrl`
- `EmailBrand__CompanyName`
- `EmailBrand__Address`
- `EmailBrand__Phone`
- `EmailBrand__Email`
- `EmailBrand__FacebookUrl`
- `EmailBrand__InstagramUrl`

Company name, address, phone, and email from the `site_metadata` system setting
override these defaults for each outgoing email. The configured values remain
safe fallbacks when metadata is missing.

## Shared template variables

- `website_url`, `logo_url`
- `company_name`, `company_address`, `company_phone`, `company_email`
- `facebook_url`, `instagram_url`
- `language_path`, `current_year`
- `email_subject`, `preheader`, `email_content` (layout only)

Request-specific values are produced by `EmailDispatchBackgroundService`, such
as `reference_code`, `full_name`, `email`, `phone_number`,
`related_entity_id`, booking dates and occupancy, or contact subject/message.

Always apply `| html.escape` when displaying a value supplied by a website
visitor. URLs and layout markup must come from trusted configuration or code.

## Adding another email type

1. Add `<TYPE>/<language>/subject.sbn`.
2. Add `<TYPE>/<language>/body.sbn` containing only the body fragment.
3. Enqueue an `EmailDelivery` with the matching type.
4. Add its model fields in `EmailDispatchBackgroundService`.
5. Add a render case in `EmailTemplateRendererTests`.

Keep core styling inline. Media queries in the shared layout are progressive
enhancements; the table layout and inline styles must remain readable without
them for Outlook compatibility.

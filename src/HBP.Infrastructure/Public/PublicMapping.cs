using HBP.Application.Common;
using HBP.Application.Public;
using HBP.Domain.Entities;
using HBP.Domain.Enums;

namespace HBP.Infrastructure.Public;

internal static class PublicMapping
{
    public static ImageResponse? Image(MediaFile? media, LanguageCode language) => media is null ? null :
        new ImageResponse(media.PublicUrl, MediaUrl.Variant(media.PublicUrl, "medium"),
            MediaUrl.Variant(media.PublicUrl, "thumbnail"), Localized.Pick(language, media.AltTextVi, media.AltTextJa));
}

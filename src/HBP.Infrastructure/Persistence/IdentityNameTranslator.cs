using Npgsql;
using Npgsql.NameTranslation;

namespace HBP.Infrastructure.Persistence;

/// <summary>
/// A no-op Npgsql name translator that returns CLR names verbatim. Used so C# enum member
/// names map 1:1 to their PostgreSQL enum labels (e.g. <c>SHOW_PRICE</c> stays <c>SHOW_PRICE</c>)
/// instead of being snake-cased by the default translator.
/// </summary>
public sealed class IdentityNameTranslator : INpgsqlNameTranslator
{
    public string TranslateTypeName(string clrName) => clrName;

    public string TranslateMemberName(string clrName) => clrName;
}

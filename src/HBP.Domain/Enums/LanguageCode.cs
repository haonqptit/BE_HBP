namespace HBP.Domain.Enums;

/// <summary>
/// Maps to PostgreSQL enum type <c>language_code_enum</c> (labels: 'vi', 'ja').
/// Enum members are translated to lower-case labels via the snake-case name translator
/// configured in <c>HbpDbContext</c>.
/// </summary>
public enum LanguageCode
{
    Vi,
    Ja
}

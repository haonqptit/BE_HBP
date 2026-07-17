using System.Security.Cryptography;
using HBP.Application.Abstractions;

namespace HBP.Infrastructure.Requests;

public sealed class ReferenceCodeGenerator(IClock clock) : IReferenceCodeGenerator
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    public string GenerateBookingCode() => Generate("BK");
    public string GenerateContactCode() => Generate("CT");
    private string Generate(string prefix)
    {
        Span<char> suffix = stackalloc char[6];
        for (var i = 0; i < suffix.Length; i++) suffix[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        return $"{prefix}-{clock.UtcNow:yyMMdd}-{suffix.ToString()}";
    }
}

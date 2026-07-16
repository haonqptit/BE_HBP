namespace HBP.Application.Abstractions;

public interface IReferenceCodeGenerator
{
    string GenerateBookingCode();
    string GenerateContactCode();
}

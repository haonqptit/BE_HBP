# BE_HBP

Backend API for the Hotel Booking Portal, built with ASP.NET Core 8 and PostgreSQL 16.

## Local development

```powershell
dotnet tool restore
dotnet restore src/HBP.Api/HBP.Api.csproj
dotnet run --project src/HBP.Api/HBP.Api.csproj
```

The development PostgreSQL container uses database/user `hbp` on port `5432`.

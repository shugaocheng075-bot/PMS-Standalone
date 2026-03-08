using PMS.API.Middleware;
using PMS.Application.Contracts.Access;
using PMS.Application.Contracts.Auth;
using PMS.Application.Contracts;
using PMS.Application.Contracts.AnnualReport;
using PMS.Application.Contracts.Contract;
using PMS.Application.Contracts.Handover;
using PMS.Application.Contracts.Hospital;
using PMS.Application.Contracts.Inspection;
using PMS.Application.Contracts.Personnel;
using PMS.Application.Contracts.Product;
using PMS.Application.Contracts.RepairRecord;
using PMS.Application.Contracts.WorkHours;
using PMS.Application.Contracts.MonthlyReport;
using PMS.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (string.IsNullOrWhiteSpace(urls))
{
    builder.WebHost.UseUrls("http://0.0.0.0:5111");
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IProjectQueryService, InMemoryProjectQueryService>();
builder.Services.AddSingleton<IContractAlertService, InMemoryContractAlertService>();
builder.Services.AddSingleton<IHandoverService, InMemoryHandoverService>();
builder.Services.AddSingleton<IInspectionService, InMemoryInspectionService>();
builder.Services.AddSingleton<IAnnualReportService, InMemoryAnnualReportService>();
builder.Services.AddSingleton<IHospitalService, InMemoryHospitalService>();
builder.Services.AddSingleton<IPersonnelService, InMemoryPersonnelService>();
builder.Services.AddSingleton<IProductService, InMemoryProductService>();
builder.Services.AddSingleton<IAccessControlService, InMemoryAccessControlService>();
builder.Services.AddSingleton<IAuthService, InMemoryAuthService>();
builder.Services.AddSingleton<IRepairRecordService, InMemoryRepairRecordService>();
builder.Services.AddSingleton<IWorkHoursService, InMemoryWorkHoursService>();
builder.Services.AddSingleton<IMonthlyReportService, InMemoryMonthlyReportService>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:5174" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithOrigins(allowedOrigins);
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCors("DevCors");
app.UseMiddleware<AuthMiddleware>();
app.UseMiddleware<PermissionMiddleware>();

var enableHttpsRedirect = string.Equals(
    Environment.GetEnvironmentVariable("ENABLE_HTTPS_REDIRECT"),
    "true",
    StringComparison.OrdinalIgnoreCase);

if (enableHttpsRedirect)
{
    app.UseHttpsRedirection();
}

app.MapControllers();

app.Run();

using PMS.Application.Contracts;
using PMS.Application.Contracts.AnnualReport;
using PMS.Application.Contracts.Contract;
using PMS.Application.Contracts.Handover;
using PMS.Application.Contracts.Hospital;
using PMS.Application.Contracts.Inspection;
using PMS.Application.Contracts.Personnel;
using PMS.Application.Contracts.Product;
using PMS.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("DevCors");

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

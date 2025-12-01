using Microsoft.EntityFrameworkCore;
using TenderAI.Core.Interfaces;
using TenderAI.Core.Services;
using TenderAI.Infrastructure.Data;
using TenderAI.Infrastructure.Repositories;
using TenderAI.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=tenderai;Username=postgres;Password=postgres123";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Repository Pattern (Unit of Work)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Business Services
builder.Services.AddScoped<ITenderService, TenderService>();
builder.Services.AddScoped<IRiskCalculationService, RiskCalculationService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDocumentAnalysisService, DocumentAnalysisService>();
builder.Services.AddScoped<IPriceRecommendationService, PriceRecommendationService>();
builder.Services.AddScoped<IBenchmarkService, BenchmarkService>(); // Faz 2

// Faz 2: İhale Sonuç Toplama Servisleri
builder.Services.AddScoped<ITenderResultAnnouncementParser, TenderResultAnnouncementParser>(); // Infrastructure
// DataCollector servisi Web uygulamasında kullanılmıyor - ayrı background servis olarak çalışıyor

// EKAP Service - DataCollector'da kullanılıyor, Web'de gerekli değil
// builder.Services.AddHttpClient<IEkapService, EkapService>();

// HttpClient for AI services (Gemini API)
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();

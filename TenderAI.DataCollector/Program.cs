using Microsoft.EntityFrameworkCore;
using TenderAI.Core.Services;
using TenderAI.DataCollector;
using TenderAI.DataCollector.Services;
using TenderAI.Infrastructure.Data;
using TenderAI.Infrastructure.Repositories;

var builder = Host.CreateApplicationBuilder(args);

// Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=tenderai;Username=postgres;Password=postgres123;Port=5432";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Repository Pattern
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// HTTP Client for EKAP API
builder.Services.AddHttpClient<IEkapService, EkapService>();

// Faz 2: Result Collection Services
builder.Services.AddScoped<TenderAI.Infrastructure.Services.ITenderResultAnnouncementParser, TenderAI.Infrastructure.Services.TenderResultAnnouncementParser>();
builder.Services.AddScoped<ITenderResultCollectorService, TenderResultCollectorService>();

// Background Services
builder.Services.AddHostedService<TenderSyncWorker>(); // İhale çekme (6 saatte bir)
builder.Services.AddHostedService<TenderResultCollectionWorker>(); // Faz 2: Sonuç çekme (günde 1 kez)

var host = builder.Build();
host.Run();

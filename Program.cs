using CoverageReport;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TG.Blazor.IndexedDB;
using CoverageReport.Application.CQRS;
using CoverageReport.Application.CQRS.Commands;
using CoverageReport.Application.CQRS.Queries;
using CoverageReport.Domain.Models;
using CoverageReport.Domain.Repositories;
using CoverageReport.Infrastructure.Repositories;
using CoverageReport.Application.Services;



var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add IndexedDB
builder.Services.AddIndexedDB(dbStore =>
{
    dbStore.DbName = "CoverageReportDb";
    dbStore.Version = 7;
    dbStore.Stores.Add(new StoreSchema
    {
        Name = "Reports",
        PrimaryKey = new IndexSpec { Name = "id", KeyPath = "id", Auto = true },
        Indexes = new List<IndexSpec>
        {
            new IndexSpec { Name = "uploadDate", KeyPath = "uploadDate", Auto = false }
        }
    });
    dbStore.Stores.Add(new StoreSchema
    {
        Name = "Records",
        PrimaryKey = new IndexSpec { Name = "id", KeyPath = "id", Auto = true },
        Indexes = new List<IndexSpec>
        {
            new IndexSpec { Name = "reportHistoryId", KeyPath = "reportHistoryId", Auto = false }
        }
    });
    dbStore.Stores.Add(new StoreSchema
    {
        Name = "ClassDetails",
        PrimaryKey = new IndexSpec { Name = "id", KeyPath = "id", Auto = true },
        Indexes = new List<IndexSpec>
        {
            new IndexSpec { Name = "reportHistoryId", KeyPath = "reportHistoryId", Auto = false }
        }
    });
    dbStore.Stores.Add(new StoreSchema
    {
        Name = "ControllerDetails",
        PrimaryKey = new IndexSpec { Name = "id", KeyPath = "id", Auto = true },
        Indexes = new List<IndexSpec>
        {
            new IndexSpec { Name = "reportHistoryId", KeyPath = "reportHistoryId", Auto = false }
        }
    });
});

// Add Services

// DDD Services
builder.Services.AddScoped<CoverageReport.Infrastructure.Parsers.CoberturaXmlParser>();
builder.Services.AddScoped<CoverageReport.Domain.Repositories.ICoverageRepository, CoverageReport.Infrastructure.Repositories.IndexedDBCoverageRepository>();
builder.Services.AddScoped<CoverageReport.Application.Services.CoverageApplicationService>();

// CQRS Services
builder.Services.AddScoped<CoverageReport.Application.CQRS.Dispatcher>();
builder.Services.AddScoped<CoverageReport.Application.CQRS.ICommandHandler<CoverageReport.Application.CQRS.Commands.UploadCoverageCommand>, CoverageReport.Application.CQRS.Commands.UploadCoverageCommandHandler>();
builder.Services.AddScoped<CoverageReport.Application.CQRS.ICommandHandler<CoverageReport.Application.CQRS.Commands.SaveReportCommand>, CoverageReport.Application.CQRS.Commands.SaveReportCommandHandler>();
builder.Services.AddScoped<CoverageReport.Application.CQRS.ICommandHandler<CoverageReport.Application.CQRS.Commands.DeleteReportCommand>, CoverageReport.Application.CQRS.Commands.DeleteReportCommandHandler>();
builder.Services.AddScoped<CoverageReport.Application.CQRS.IQueryHandler<CoverageReport.Application.CQRS.Queries.GetHistoryQuery, List<CoverageReport.Domain.Models.CoverageReportAggregate>>, CoverageReport.Application.CQRS.Queries.GetHistoryQueryHandler>();
builder.Services.AddScoped<CoverageReport.Application.CQRS.IQueryHandler<CoverageReport.Application.CQRS.Queries.GetReportDetailsQuery, CoverageReport.Domain.Models.CoverageReportAggregate?>, CoverageReport.Application.CQRS.Queries.GetReportDetailsQueryHandler>();

var host = builder.Build();

// Ensure Database Created (IndexedDB handles this on access, no explicit call needed)
// Just run the app
await host.RunAsync();

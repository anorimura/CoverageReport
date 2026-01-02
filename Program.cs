using CoverageReport;

using CoverageReport.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TG.Blazor.IndexedDB;



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
builder.Services.AddScoped<XmlParserService>();
builder.Services.AddScoped<CoverageService>();

var host = builder.Build();

// Ensure Database Created (IndexedDB handles this on access, no explicit call needed)
// Just run the app
await host.RunAsync();

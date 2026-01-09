using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TG.Blazor.IndexedDB;
using CoverageReport.Domain.Models;
using CoverageReport.Domain.Repositories;
using CoverageReport.Models; // Legacy entities for DB mapping if needed, or mapping logic

namespace CoverageReport.Infrastructure.Repositories
{
    public class IndexedDBCoverageRepository : ICoverageRepository
    {
        private readonly IndexedDBManager _dbManager;

        public IndexedDBCoverageRepository(IndexedDBManager dbManager)
        {
            _dbManager = dbManager;
        }

        public async Task SaveAsync(CoverageReportAggregate report, System.IProgress<int>? progress = null)
        {
            // Note: In a real DDD setup, we might map to DB-specific POCOs.
            // For this MVP, we adapt to the existing table structure used by TG.Blazor.IndexedDB.
            
            // 1. Delete existing report for the same day (business rule from CoverageService)
            var allReports = await _dbManager.GetRecords<ReportHistory>("Reports");
            var existing = allReports.FirstOrDefault(h => h.UploadDate.Date == report.UploadDate.Date);
            if (existing != null) await DeleteAsync(existing.Id);

            // 2. Map Aggregate to Legacy Entity for "Reports" store
            var totalMetrics = report.GetTotalMetrics();
            var coreMetrics = report.CoreMetrics;
            var controllerMetrics = report.ControllerMetrics;

            var historyRecord = new ReportHistory
            {
                UploadDate = report.UploadDate,
                LineRate = totalMetrics.Rate,
                BranchRate = 0,
                CoreLineRate = coreMetrics.Rate,
                ControllerLineRate = controllerMetrics.Rate,
                ParseDurationMs = report.ParseDurationMs,
                FileSizeBytes = report.FileSizeBytes,
                LinesTotal = totalMetrics.Total,
                LinesCovered = totalMetrics.Covered,
                CoreLinesTotal = coreMetrics.Total,
                CoreLinesCovered = coreMetrics.Covered,
                ControllerLinesTotal = controllerMetrics.Total,
                ControllerLinesCovered = controllerMetrics.Covered,
                ExclusionPattern = report.ExclusionPattern
            };

            await _dbManager.AddRecord(new StoreRecord<ReportHistory> { Storename = "Reports", Data = historyRecord });

            // 3. Re-fetch to get ID (as done in the original service)
            var saved = (await _dbManager.GetRecords<ReportHistory>("Reports"))
                .FirstOrDefault(h => h.UploadDate == report.UploadDate);

            if (saved != null)
            {
                // Only save classes marked as IsTarget
                var classes = report.AllClasses.Where(c => c.IsTarget).ToList();
                int total = classes.Count;
                int current = 0;
                
                if (total == 0)
                {
                    progress?.Report(100);
                    return;
                }

                // Process in batches of 100 to drastically reduce save time
                const int batchSize = 100;
                for (int i = 0; i < total; i += batchSize)
                {
                    var batch = classes.Skip(i).Take(batchSize).ToList();
                    var tasks = batch.Select(cls => _dbManager.AddRecord(new StoreRecord<CoverageReport.Models.ClassDetail> 
                    { 
                        Storename = "ClassDetails", 
                        Data = new CoverageReport.Models.ClassDetail 
                        {
                            ReportHistoryId = saved.Id,
                            ClassName = cls.FullName,
                            LineRate = cls.LineCoverage.Rate,
                            LinesTotal = cls.LineCoverage.Total,
                            LinesCovered = cls.LineCoverage.Covered,
                            Complexity = cls.Complexity,
                            IsTarget = cls.IsTarget
                        }
                    }));

                    await Task.WhenAll(tasks);

                    current += batch.Count;
                    progress?.Report(current * 100 / total);
                }
            }
        }

        public async Task<CoverageReportAggregate?> GetByIdAsync(int id)
        {
            var history = await _dbManager.GetRecordById<int, ReportHistory>("Reports", id);
            if (history == null) return null;

            var aggregate = new CoverageReportAggregate(history.UploadDate, history.FileSizeBytes, history.ExclusionPattern) { Id = history.Id };
            
            var allDetails = await _dbManager.GetRecords<CoverageReport.Models.ClassDetail>("ClassDetails");
            var details = allDetails.Where(d => d.ReportHistoryId == id);

            // Reconstruct Aggregate (simplified: group by namespace as package)
            var packages = details.GroupBy(d => d.PackageName).Select(g => {
                var pkg = new CoverageReport.Domain.Models.PackageSummary(g.Key);
                foreach(var d in g) {
                    pkg.AddClass(new Domain.Models.ClassDetail(
                        d.ClassName.Split('.').Last(),
                        d.ClassName,
                        new CoverageMetrics(d.LinesCovered, d.LinesTotal),
                        new CoverageMetrics(0, 0), // Branch not stored yet
                        d.Complexity
                    ) { IsTarget = d.IsTarget });
                }
                return pkg;
            });

            foreach(var p in packages) aggregate.AddPackage(p);

            return aggregate;
        }

        public async Task<List<CoverageReportAggregate>> GetAllAsync()
        {
            var records = await _dbManager.GetRecords<ReportHistory>("Reports");
            return records.Select(r => new CoverageReportAggregate(r.UploadDate, r.FileSizeBytes, r.ExclusionPattern) 
            { 
                Id = r.Id,
                ForcedTotalMetrics = new CoverageMetrics(r.LinesCovered, r.LinesTotal),
                ForcedCoreMetrics = new CoverageMetrics(r.CoreLinesCovered, r.CoreLinesTotal),
                ForcedControllerMetrics = new CoverageMetrics(r.ControllerLinesCovered, r.ControllerLinesTotal)
            }).ToList();
        }

        public async Task DeleteAsync(int id)
        {
            await _dbManager.DeleteRecord("Reports", id);
            // Cascading delete simplified for MVP - usually done by Repository
            var details = await _dbManager.GetRecords<CoverageReport.Models.ClassDetail>("ClassDetails");
            foreach(var d in details.Where(d => d.ReportHistoryId == id)) {
                await _dbManager.DeleteRecord("ClassDetails", d.Id);
            }
        }
    }
}

using TG.Blazor.IndexedDB;
using CoverageReport.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CoverageReport.Services
{
    public class CoverageService
    {
        private readonly IndexedDBManager _dbManager;

        public CoverageService(IndexedDBManager dbManager)
        {
            _dbManager = dbManager;
        }

        public async Task<List<ReportHistory>> GetHistoryAsync()
        {
            // IndexedDBManager doesn't support complex ordering/querying natively easily without getting all.
            // For now, get all and order in memory.
            var records = await _dbManager.GetRecords<ReportHistory>("Reports");
            return records.OrderByDescending(h => h.UploadDate).ToList();
        }
        
        public async Task<ReportHistory?> GetReportAsync(int id)
        {
            var report = await _dbManager.GetRecordById<int, ReportHistory>("Reports", id);
            if (report == null) return null;

            // Manual join since IndexedDB is NoSQL-ish here
            var allRecords = await _dbManager.GetRecords<PackageSummary>("Records");
            report.Records = allRecords.Where(r => r.ReportHistoryId == id).ToList();

            var allDetails = await _dbManager.GetRecords<ClassDetail>("ClassDetails");
            report.ClassDetails = allDetails.Where(d => d.ReportHistoryId == id).ToList();

            var allControllers = await _dbManager.GetRecords<ClassDetail>("ControllerDetails");
            report.ControllerDetails = allControllers.Where(d => d.ReportHistoryId == id).ToList();
            
            return report;
        }

        public async Task SaveReportAsync(ReportHistory report)
        {
            // Check for existing report by date
            var allReports = await _dbManager.GetRecords<ReportHistory>("Reports");
            var existing = allReports.FirstOrDefault(h => h.UploadDate.Date == report.UploadDate.Date);

            if (existing != null)
            {
                await DeleteReportAsync(existing.Id);
            }
            
            // Add Report
            // IndexedDB auto-increments ID if mapped.
            // We need to add report first to get ID? 
            // TG.Blazor.IndexedDB AddRecord returns the record with updated ID?
            // Actually it returns void or Task.
            // We might need to handle ID generation or trust the store.
            // Let's rely on the store. However, we need the ID for the records.
            // So we might need to "put" and assume we can query it back or use a GUID if we can't get ID back immediately.
            // Or simpler: Save report, then get it back by trying to match date (since we just deleted unique date).
            
            await _dbManager.AddRecord(new StoreRecord<ReportHistory>
            {
                Storename = "Reports",
                Data = report
            });

            // Re-fetch to get new ID
            var savedReports = await _dbManager.GetRecords<ReportHistory>("Reports");
            var savedReport = savedReports.FirstOrDefault(h => h.UploadDate.Date == report.UploadDate.Date);
            
            if (savedReport != null)
            {
                foreach (var record in report.Records)
                {
                    record.ReportHistoryId = savedReport.Id;
                    await _dbManager.AddRecord(new StoreRecord<PackageSummary>
                    {
                        Storename = "Records",
                        Data = record
                    });
                }

                foreach (var detail in report.ClassDetails)
                {
                    detail.ReportHistoryId = savedReport.Id;
                    await _dbManager.AddRecord(new StoreRecord<ClassDetail>
                    {
                        Storename = "ClassDetails",
                        Data = detail
                    });
                }

                foreach (var controller in report.ControllerDetails)
                {
                    controller.ReportHistoryId = savedReport.Id;
                    await _dbManager.AddRecord(new StoreRecord<ClassDetail>
                    {
                        Storename = "ControllerDetails",
                        Data = controller
                    });
                }
            }
        }

        public async Task DeleteReportAsync(int id)
        {
            // Delete Report
            await _dbManager.DeleteRecord("Reports", id);
            
            // Delete associated Records
            // This is inefficient (get all then delete match), but given data size/IndexedDB capability constraints, it works for MVP.
            // Ideally use an index cursor but library might not expose easily.
            var allRecords = await _dbManager.GetRecords<PackageSummary>("Records");
            var toDelete = allRecords.Where(r => r.ReportHistoryId == id).ToList();
            
            foreach (var r in toDelete)
            {
                await _dbManager.DeleteRecord("Records", r.Id);
            }

            // Delete associated ClassDetails
            var allDetails = await _dbManager.GetRecords<ClassDetail>("ClassDetails");
            var detailsToDelete = allDetails.Where(d => d.ReportHistoryId == id).ToList();
            foreach (var d in detailsToDelete)
            {
                await _dbManager.DeleteRecord("ClassDetails", d.Id);
            }

            // Delete associated ControllerDetails
            var allControllers = await _dbManager.GetRecords<ClassDetail>("ControllerDetails");
            var controllersToDelete = allControllers.Where(d => d.ReportHistoryId == id).ToList();
            foreach (var c in controllersToDelete)
            {
                await _dbManager.DeleteRecord("ControllerDetails", c.Id);
            }
        }
        
        // No explicit EnsureCreated needed for IndexedDB (handled by browser/library on open)
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoverageReport.Domain.Models;

namespace CoverageReport.Domain.Repositories
{
    public interface ICoverageRepository
    {
        Task SaveAsync(CoverageReportAggregate report, IProgress<int>? progress = null);
        Task<CoverageReportAggregate?> GetByIdAsync(int id);
        Task<List<CoverageReportAggregate>> GetAllAsync();
        Task DeleteAsync(int id);
    }
}

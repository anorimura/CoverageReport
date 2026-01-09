using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoverageReport.Domain.Models;
using CoverageReport.Domain.Repositories;
using CoverageReport.Infrastructure.Parsers;

namespace CoverageReport.Application.Services
{
    public class CoverageApplicationService
    {
        public CoverageReportAggregate? CurrentReport { get; set; }
        private readonly ICoverageRepository _repository;
        private readonly CoberturaXmlParser _parser;

        public CoverageApplicationService(ICoverageRepository repository, CoberturaXmlParser parser)
        {
            _repository = repository;
            _parser = parser;
        }

        public async Task<CoverageReportAggregate> UploadReportAsync(string xmlContent, string exclusionPattern = ".Controller")
        {
            var report = await _parser.ParseAsync(xmlContent, exclusionPattern);
            await _repository.SaveAsync(report);
            return report;
        }

        public async Task<List<CoverageReportAggregate>> GetHistoryAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<CoverageReportAggregate?> GetReportDetailsAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task DeleteReportAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}

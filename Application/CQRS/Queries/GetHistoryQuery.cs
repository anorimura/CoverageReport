using System.Collections.Generic;
using System.Threading.Tasks;
using CoverageReport.Domain.Models;
using CoverageReport.Domain.Repositories;

namespace CoverageReport.Application.CQRS.Queries
{
    public record GetHistoryQuery() : IQuery<List<CoverageReportAggregate>>;

    public class GetHistoryQueryHandler : IQueryHandler<GetHistoryQuery, List<CoverageReportAggregate>>
    {
        private readonly ICoverageRepository _repository;

        public GetHistoryQueryHandler(ICoverageRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<CoverageReportAggregate>> HandleAsync(GetHistoryQuery query)
        {
            return await _repository.GetAllAsync();
        }
    }
}

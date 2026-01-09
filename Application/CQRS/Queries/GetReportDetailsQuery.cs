using System.Threading.Tasks;
using CoverageReport.Domain.Models;
using CoverageReport.Domain.Repositories;

namespace CoverageReport.Application.CQRS.Queries
{
    public record GetReportDetailsQuery(int Id) : IQuery<CoverageReportAggregate?>;

    public class GetReportDetailsQueryHandler : IQueryHandler<GetReportDetailsQuery, CoverageReportAggregate?>
    {
        private readonly ICoverageRepository _repository;

        public GetReportDetailsQueryHandler(ICoverageRepository repository)
        {
            _repository = repository;
        }

        public async Task<CoverageReportAggregate?> HandleAsync(GetReportDetailsQuery query)
        {
            return await _repository.GetByIdAsync(query.Id);
        }
    }
}

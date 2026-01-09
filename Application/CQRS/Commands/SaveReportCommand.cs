using System.Threading.Tasks;
using CoverageReport.Domain.Models;
using CoverageReport.Domain.Repositories;
using CoverageReport.Application.CQRS;

namespace CoverageReport.Application.CQRS.Commands
{
    public record SaveReportCommand(CoverageReportAggregate Report, System.IProgress<int>? Progress) : ICommand;

    public class SaveReportCommandHandler : ICommandHandler<SaveReportCommand>
    {
        private readonly ICoverageRepository _repository;

        public SaveReportCommandHandler(ICoverageRepository repository)
        {
            _repository = repository;
        }

        public async Task ExecuteAsync(SaveReportCommand command)
        {
            await _repository.SaveAsync(command.Report, command.Progress);
        }
    }
}

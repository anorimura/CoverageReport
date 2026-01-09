using System.Threading.Tasks;
using CoverageReport.Domain.Repositories;

namespace CoverageReport.Application.CQRS.Commands
{
    public record DeleteReportCommand(int Id) : ICommand;

    public class DeleteReportCommandHandler : ICommandHandler<DeleteReportCommand>
    {
        private readonly ICoverageRepository _repository;

        public DeleteReportCommandHandler(ICoverageRepository repository)
        {
            _repository = repository;
        }

        public async Task ExecuteAsync(DeleteReportCommand command)
        {
            await _repository.DeleteAsync(command.Id);
        }
    }
}

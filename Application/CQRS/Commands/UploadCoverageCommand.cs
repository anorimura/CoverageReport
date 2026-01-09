using System.Threading.Tasks;
using CoverageReport.Domain.Models;
using CoverageReport.Domain.Repositories;
using CoverageReport.Infrastructure.Parsers;

namespace CoverageReport.Application.CQRS.Commands
{
    public record UploadCoverageCommand(string XmlContent, string ExclusionPattern = ".Controller") : ICommand;

    public class UploadCoverageCommandHandler : ICommandHandler<UploadCoverageCommand>
    {
        private readonly ICoverageRepository _repository;
        private readonly CoberturaXmlParser _parser;

        public UploadCoverageCommandHandler(ICoverageRepository repository, CoberturaXmlParser parser)
        {
            _repository = repository;
            _parser = parser;
        }

        public async Task ExecuteAsync(UploadCoverageCommand command)
        {
            var report = await _parser.ParseAsync(command.XmlContent, command.ExclusionPattern);
            await _repository.SaveAsync(report);
        }
    }
}

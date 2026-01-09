using System.Threading.Tasks;

namespace CoverageReport.Application.CQRS
{
    public interface ICommand { }
    
    public interface ICommandHandler<in TCommand> where TCommand : ICommand
    {
        Task ExecuteAsync(TCommand command);
    }
}

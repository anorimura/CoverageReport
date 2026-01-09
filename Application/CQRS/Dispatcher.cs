using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace CoverageReport.Application.CQRS
{
    public class Dispatcher
    {
        private readonly IServiceProvider _serviceProvider;

        public Dispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task DispatchAsync<TCommand>(TCommand command) where TCommand : ICommand
        {
            var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();
            await handler.ExecuteAsync(command);
        }

        public async Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>
        {
            var handler = _serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
            return await handler.HandleAsync(query);
        }
    }
}

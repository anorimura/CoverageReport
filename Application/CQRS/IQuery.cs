using System.Threading.Tasks;

namespace CoverageReport.Application.CQRS
{
    public interface IQuery<out TResult> { }

    public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
    {
        Task<TResult> HandleAsync(TQuery query);
    }
}

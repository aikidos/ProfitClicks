using System.Threading;
using System.Threading.Tasks;

namespace ProfitClicks.Interfaces
{
    public interface IJob
    {
        /// <summary>
        /// Запускает выполнение текущей задачи.
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken = default);
    }
}

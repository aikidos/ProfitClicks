using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProfitClicks.Interfaces
{
    public interface ITaskManager : IDisposable
    {
        /// <summary>
        /// Максимальное количество одновременно выполняемых задач.
        /// </summary>
        int MaxActiveJobs { get; }

        /// <summary>
        /// `True`, если в данный момент запущено выполнение задач.
        /// </summary>
        bool IsWorking { get; }

        /// <summary>
        /// Срабатывает при запуске новой задачи.
        /// </summary>
        event EventHandler<JobStartedEventArgs> JobStarted;

        /// <summary>
        /// Срабатывает при завершении выполнения задачи.
        /// </summary>
        event EventHandler<JobCompletedEventArgs> JobCompleted;

        /// <summary>
        /// Срабатывает при возникновении ошибки при выполнении задачи.
        /// </summary>
        event EventHandler<JobFailedEventArgs> JobFailed;

        /// <summary>
        /// Добавляет задачу <paramref name="job"/> в очередь выполнения с приоритетом <paramref name="priority"/>.
        /// </summary>
        void Add(IJob job, JobPriority priority = JobPriority.Normal);

        /// <summary>
        /// Запускает выполнение задач.
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Ожидает выполнение ранее запущенных задач и останавливает запуск новых.
        /// </summary>
        Task StopAsync(CancellationToken cancellationToken = default);
    }
}

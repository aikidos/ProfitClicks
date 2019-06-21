using System;
using System.ComponentModel;
using ProfitClicks.Interfaces;

namespace ProfitClicks
{
    public sealed class JobFailedEventArgs : EventArgs
    {
        /// <summary>
        /// Исключение, которое возникло в ходе выполнения задачи.
        /// </summary>
        public AggregateException Exception { get; }

        /// <summary>
        /// Приоритет задачи.
        /// </summary>
        public JobPriority Priority { get; }

        /// <summary>
        /// Задача, выполнение которой завершилось ошибкой.
        /// </summary>
        public IJob Job { get; }

        public JobFailedEventArgs(JobPriority priority, IJob job, AggregateException exception)
        {
            if (!Enum.IsDefined(typeof(JobPriority), priority))
                throw new InvalidEnumArgumentException(nameof(priority), (int) priority, typeof(JobPriority));

            Job = job ?? throw new ArgumentNullException(nameof(job));
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            Priority = priority;
        }
    }
}

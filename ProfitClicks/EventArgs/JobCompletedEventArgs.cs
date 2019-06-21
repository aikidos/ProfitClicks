using System;
using System.ComponentModel;
using ProfitClicks.Interfaces;

namespace ProfitClicks
{
    public sealed class JobCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Приоритет задачи.
        /// </summary>
        public JobPriority Priority { get; }

        /// <summary>
        /// Задача, выполнение которой завершилось успешно.
        /// </summary>
        public IJob Job { get; }

        public JobCompletedEventArgs(JobPriority priority, IJob job)
        {
            if (!Enum.IsDefined(typeof(JobPriority), priority))
                throw new InvalidEnumArgumentException(nameof(priority), (int) priority, typeof(JobPriority));

            Job = job ?? throw new ArgumentNullException(nameof(job));
            Priority = priority;
        }
    }
}

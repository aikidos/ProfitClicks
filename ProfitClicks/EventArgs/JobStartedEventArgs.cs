using System;
using System.ComponentModel;
using ProfitClicks.Interfaces;

namespace ProfitClicks
{
    public sealed class JobStartedEventArgs : EventArgs
    {
        /// <summary>
        /// Приоритет задачи.
        /// </summary>
        public JobPriority Priority { get; }

        /// <summary>
        /// Запущенная задача.
        /// </summary>
        public IJob Job { get; }

        public JobStartedEventArgs(JobPriority priority, IJob job)
        {
            if (!Enum.IsDefined(typeof(JobPriority), priority))
                throw new InvalidEnumArgumentException(nameof(priority), (int) priority, typeof(JobPriority));

            Job = job ?? throw new ArgumentNullException(nameof(job));
            Priority = priority;
        }
    }
}

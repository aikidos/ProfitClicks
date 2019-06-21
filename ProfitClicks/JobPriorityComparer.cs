using System.Collections.Generic;

namespace ProfitClicks
{
    internal sealed class JobPriorityComparer : IComparer<JobPriority>
    {
        public int Compare(JobPriority x, JobPriority y) => Comparer<JobPriority>.Default.Compare(y, x);
    }
}

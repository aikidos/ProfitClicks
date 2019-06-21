using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProfitClicks.Interfaces;

namespace ProfitClicks
{
    public sealed class TaskManager : ITaskManager
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _idleSemaphore = new SemaphoreSlim(0);

        private readonly SortedList<JobPriority, Queue<IJob>> _jobs = new SortedList<JobPriority, Queue<IJob>>(new JobPriorityComparer());
        private readonly Dictionary<IJob, Task> _jobTasks = new Dictionary<IJob, Task>();
        private readonly object _jobTasksSyncObject = new object();
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _worker;

        public int MaxActiveJobs { get; }

        public bool IsWorking { get; private set; }

        public event EventHandler<JobStartedEventArgs> JobStarted;
        public event EventHandler<JobCompletedEventArgs> JobCompleted;
        public event EventHandler<JobFailedEventArgs> JobFailed;

        private void OnJobStarted(JobStartedEventArgs eventArgs) => JobStarted?.Invoke(this, eventArgs);
        private void OnJobCompleted(JobCompletedEventArgs eventArgs) => JobCompleted?.Invoke(this, eventArgs);
        private void OnJobFailed(JobFailedEventArgs eventArgs) => JobFailed?.Invoke(this, eventArgs);

        public TaskManager(int maxActiveJobs)
        {
            if (maxActiveJobs <= 0) throw new ArgumentOutOfRangeException(nameof(maxActiveJobs));

            foreach (JobPriority key in Enum.GetValues(typeof(JobPriority)))
                _jobs.Add(key, new Queue<IJob>());

            MaxActiveJobs = maxActiveJobs;
        }

        public void Add(IJob job, JobPriority priority = JobPriority.Normal)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (!Enum.IsDefined(typeof(JobPriority), priority))
                throw new InvalidEnumArgumentException(nameof(priority), (int) priority, typeof(JobPriority));

            _jobs[priority].Enqueue(job);

            _idleSemaphore.Release();
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _semaphore.WaitAsync(cancellationToken);

            if (IsWorking)
                return;

            IsWorking = true;

            try
            {
                _worker = Task.Run(async () =>
                {
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        if (!await TryStartNextJobAsync(_cts.Token))
                            await _idleSemaphore.WaitAsync();
                    }

                    var jobTasks = GetJobTasks();
                    await Task.WhenAll(jobTasks);
                });
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _semaphore.WaitAsync(cancellationToken);

            if (!IsWorking)
                return;

            IsWorking = false;

            try
            {
                _cts.Cancel();
                _idleSemaphore.Release();

                await _worker;

                _cts.Dispose();
                _cts = new CancellationTokenSource();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private bool TryGetNextJob(out JobPriority priority, out IJob job)
        {
            var highPriorityJobs = _jobs
                .Where(pair => pair.Value.Count > 0)
                .Select(pair => new
                {
                    Priority = pair.Key,
                    Queue = pair.Value,
                })
                .FirstOrDefault();

            if (highPriorityJobs == null)
            {
                priority = default;
                job = null;
                return false;
            }

            priority = highPriorityJobs.Priority;
            job = highPriorityJobs.Queue.Dequeue();
            return true;
        }

        private async Task<bool> TryStartNextJobAsync(CancellationToken cancellationToken)
        {
            if (!TryGetNextJob(out var priority, out var job))
                return false;

            var jobTasks = GetJobTasks();

            if (jobTasks.Length == MaxActiveJobs)
                await Task.WhenAny(jobTasks);

            var jobTask = Task
                .Run(async () => await job.StartAsync(cancellationToken))
                .ContinueWith(task =>
                {
                    if (task.IsCompleted)
                        OnJobCompleted(new JobCompletedEventArgs(priority, job));

                    if (task.IsFaulted)
                        OnJobFailed(new JobFailedEventArgs(priority, job, task.Exception));

                    lock (_jobTasksSyncObject)
                        _jobTasks.Remove(job);
                });

            lock (_jobTasksSyncObject)
                _jobTasks.Add(job, jobTask);

            OnJobStarted(new JobStartedEventArgs(priority, job));

            return true;
        }

        private Task[] GetJobTasks()
        {
            lock (_jobTasksSyncObject)
                return _jobTasks.Values.ToArray();
        }

        public async void Dispose()
        {
            await StopAsync();

            _cts.Dispose();
            _semaphore.Dispose();
            _idleSemaphore.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using ProfitClicks.Interfaces;
using Xunit;

namespace ProfitClicks.Tests
{
    public class TaskManagerTests
    {
        [Fact]
        public async Task IsWorking()
        {
            using (var manager = new TaskManager(1))
            {
                Assert.False(manager.IsWorking);

                await manager.StartAsync();

                Assert.True(manager.IsWorking);

                await manager.StopAsync();

                Assert.False(manager.IsWorking);
            }
        }

        [Fact]
        public async Task Raise_JobStarted()
        {
            using (var manager = new TaskManager(1))
            {
                JobStartedEventArgs jobStartedEventArgs = null;
                manager.JobStarted += (sender, e) => jobStartedEventArgs = e;

                var job = Mock.Of<IJob>();

                manager.Add(job, JobPriority.High);

                await manager.StartAsync();
                await Task.Delay(100);
                await manager.StopAsync();

                Assert.NotNull(jobStartedEventArgs);
                Assert.Equal(JobPriority.High, jobStartedEventArgs.Priority);
                Assert.Equal(job, jobStartedEventArgs.Job);
            }
        }

        [Fact]
        public async Task Raise_JobCompleted()
        {
            using (var manager = new TaskManager(1))
            {
                JobCompletedEventArgs jobCompletedEventArgs = null;
                manager.JobCompleted += (sender, e) => jobCompletedEventArgs = e;

                var job = Mock.Of<IJob>();

                manager.Add(job, JobPriority.High);

                await manager.StartAsync();
                await Task.Delay(100);
                await manager.StopAsync();

                Assert.NotNull(jobCompletedEventArgs);
                Assert.Equal(JobPriority.High, jobCompletedEventArgs.Priority);
                Assert.Equal(job, jobCompletedEventArgs.Job);
            }
        }

        [Fact]
        public async Task Raise_JobFailed()
        {
            using (var manager = new TaskManager(1))
            {
                JobFailedEventArgs jobFailedEventArgs = null;
                manager.JobFailed += (sender, e) => jobFailedEventArgs = e;

                var exception = new Exception("error");

                var job = new Mock<IJob>();
                job.Setup(jb => jb.StartAsync(It.IsAny<CancellationToken>())).Callback(() => throw exception);

                manager.Add(job.Object, JobPriority.High);

                await manager.StartAsync();
                await Task.Delay(100);
                await manager.StopAsync();

                Assert.NotNull(jobFailedEventArgs);
                Assert.Equal(JobPriority.High, jobFailedEventArgs.Priority);
                Assert.Equal(job.Object, jobFailedEventArgs.Job);
                Assert.Equal(exception, jobFailedEventArgs.Exception.InnerExceptions.Single());
            }
        }

        [Fact]
        public async Task Idle()
        {
            using (var manager = new TaskManager(1))
            {
                var job = new Mock<IJob>();

                await manager.StartAsync();
                manager.Add(job.Object);

                job.Verify(jb => jb.StartAsync(It.IsAny<CancellationToken>()), Times.Once);

                await manager.StopAsync();

                manager.Add(job.Object);

                job.Reset();
                job.Verify(jb => jb.StartAsync(It.IsAny<CancellationToken>()), Times.Never);
            }
        }

        [Fact]
        public async Task MaxJobs()
        {
            using (var manager = new TaskManager(1))
            using (var cts = new CancellationTokenSource())
            {
                var job1 = new Mock<IJob>();
                var job2 = new Mock<IJob>();

                job1.Setup(jb => jb.StartAsync(It.IsAny<CancellationToken>())).Returns(() =>
                {
                    return Task.Delay(TimeSpan.FromMinutes(1), cts.Token);
                });

                await manager.StartAsync();
                
                manager.Add(job1.Object);
                manager.Add(job2.Object);

                job2.Verify(jb => jb.StartAsync(It.IsAny<CancellationToken>()), Times.Never);
                job2.Reset();

                cts.Cancel();
                await Task.Delay(100);

                job2.Verify(jb => jb.StartAsync(It.IsAny<CancellationToken>()), Times.Once);

                await manager.StopAsync();
            }

            using (var manager = new TaskManager(2))
            {
                var job1 = new Mock<IJob>();
                var job2 = new Mock<IJob>();

                job1.Setup(jb => jb.StartAsync(It.IsAny<CancellationToken>())).Returns((CancellationToken ct) =>
                {
                    return Task.Delay(TimeSpan.FromMinutes(1), ct);
                });

                job2.Setup(jb => jb.StartAsync(It.IsAny<CancellationToken>())).Returns((CancellationToken ct) =>
                {
                    return Task.Delay(TimeSpan.FromMinutes(1), ct);
                });

                manager.Add(job1.Object);
                manager.Add(job2.Object);

                await manager.StartAsync();
                await Task.Delay(100);
                
                job1.Verify(jb => jb.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
                job2.Verify(jb => jb.StartAsync(It.IsAny<CancellationToken>()), Times.Once);

                await manager.StopAsync();
            }
        }

        [Fact]
        public async Task Priority()
        {
            using (var manager = new TaskManager(2))
            {
                var job1 = Mock.Of<IJob>();
                var job2 = Mock.Of<IJob>();
                var job3 = Mock.Of<IJob>();

                var completedJobs = new List<IJob>();
                manager.JobCompleted += (sender, e) => completedJobs.Add(e.Job);

                manager.Add(job1, JobPriority.Low);
                manager.Add(job2, JobPriority.High);
                manager.Add(job3, JobPriority.VeryLow);

                await manager.StartAsync();
                await Task.Delay(100);
                await manager.StopAsync();

                Assert.Equal(new [] { job2, job1, job3 }, completedJobs);
            }
        }
    }
}

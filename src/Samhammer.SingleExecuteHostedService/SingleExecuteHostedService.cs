using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Samhammer.SingleExecuteHostedService
{
    public abstract class SingleExecuteHostedService<T> : BackgroundService
    {
        protected ILogger<SingleExecuteHostedService<T>> Logger { get; }

        protected IServiceScopeFactory Services { get; }

        protected IHostApplicationLifetime ApplicationLifetime { get; }

        protected virtual TimeSpan ShutdownDelay => TimeSpan.FromSeconds(1);

        private bool IsFinished { get; set; }

        protected SingleExecuteHostedService(ILogger<SingleExecuteHostedService<T>> logger, IServiceScopeFactory services, IHostApplicationLifetime applicationLifetime)
        {
            Logger = logger;
            Services = services;
            ApplicationLifetime = applicationLifetime;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("SingleExecuteHostedService {HostedService} is starting", typeof(T));
            return base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("SingleExecuteHostedService {HostedService} is stopping", typeof(T));
            await base.StopAsync(cancellationToken);
            ThrowIfNotFinished();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                Logger.LogInformation("SingleExecuteHostedService {HostedService} is executed", typeof(T));
                await Run(stoppingToken);
                SetFinished();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SingleExecuteHostedService {HostedService} failed with an error", typeof(T));
            }
            finally
            {
                // when run is finished too fast, the app could still be starting
                await Task.Delay(ShutdownDelay, CancellationToken.None);
                ApplicationLifetime.StopApplication();
            }
        }

        private async Task Run(CancellationToken stoppingToken)
        {
            using var scope = Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<T>();
            await RunScoped(service, stoppingToken);
        }

        protected abstract Task RunScoped(T service, CancellationToken stoppingToken);

        private void SetFinished()
        {
            Logger.LogInformation("SingleExecuteHostedService {HostedService} is finished", typeof(T));
            IsFinished = true;
        }

        private void ThrowIfNotFinished()
        {
            if (!IsFinished)
            {
                Logger.LogWarning("SingleExecuteHostedService {HostedService} was not finished", typeof(T));
                throw new SingleExecuteNotFinishedException();
            }
        }
    }
}

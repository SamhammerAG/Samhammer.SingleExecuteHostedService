using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Samhammer.SingleExecuteHostedService.Test;

public class SingleExecuteHostedServiceTest
{
    private ISampleService SampleService { get; }

    private IServiceCollection Services { get; }

    private ServiceProvider ServiceProvider => Services.BuildServiceProvider();

    public SingleExecuteHostedServiceTest()
    {
        var logger = Substitute.For<ILogger<SingleExecuteHostedService<ISampleService>>>();
        var hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();

        SampleService = Substitute.For<ISampleService>();

        Services = new ServiceCollection();
        Services.AddSingleton(logger);
        Services.AddSingleton(hostApplicationLifetime);
        Services.AddHostedService<SampleHostedService>();
        Services.AddSingleton(SampleService);
    }

    [Fact]
    public async Task HostedServiceExecutes()
    {
        var cancellationSource = new CancellationTokenSource();
        SampleService.When(s => s.DoWork()).Do(_ => cancellationSource.Cancel());

        var hostedService = ServiceProvider.GetService<IHostedService>();

        await hostedService!.StartAsync(CancellationToken.None);
        await Delay(1000, cancellationSource);
        await hostedService.StopAsync(CancellationToken.None);

        await SampleService.Received().DoWork();
    }

    private static async Task Delay(int milliseconds, CancellationTokenSource cancelSource)
    {
        try
        {
            await Task.Delay(milliseconds, cancelSource.Token);
        }
        catch (TaskCanceledException)
        {
        }
    }

    public interface ISampleService
    {
        Task DoWork();
    }

    public class SampleHostedService : SingleExecuteHostedService<ISampleService>
    {
        public SampleHostedService(ILogger<SingleExecuteHostedService<ISampleService>> logger, IServiceScopeFactory services, IHostApplicationLifetime applicationLifetime)
            : base(logger, services, applicationLifetime)
        {
        }

        protected override async Task RunScoped(ISampleService service, CancellationToken stoppingToken)
        {
            await service.DoWork();
        }
    }
}

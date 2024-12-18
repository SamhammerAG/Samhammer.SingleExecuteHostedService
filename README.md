## Samhammer.Samhammer.SingleExecuteHostedService

This package provides a single execute hosted service.

#### How to add this to your project:
- Reference this nuget package: https://www.nuget.org/packages/Samhammer.SingleExecuteHostedService/

#### How to use:

Implement a class that inherits from SingleExecuteHostedService.
```csharp
public class MyHostedService : SingleExecuteHostedService<IMyService>
{
    protected override TimeSpan ShutdownDelay => TimeSpan.FromSeconds(2);

	public MyHostedService(ILogger<MyHostedService> logger, IServiceScopeFactory services, IHostApplicationLifetime applicationLifetime) 
		: base(logger, services, applicationLifetime)
	{
	}
	
	protected override Task RunScoped(IMyService myService, CancellationToken stoppingToken)
	{
		myService.DoSomething();
		return Task.CompletedTask;
	}
}
```

Register the hosted service in startup:
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddHostedService<MyHostedService>()
}
```

Possible configurations:
- ShutdownDelay: Delay after executing a task in case a run is finished too fast since app could still be starting (default: 1)

Possible hook points:
- RunScoped: Execute your logic here (mandatory)

Note:
- "RunScoped" is running in it's ioc scope

## Contribute

#### How to publish a nuget package
- Create a tag and let the github action do the publishing for you
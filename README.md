## Synopsis

Provides full-flavour implementation of reliable background service with Serilog logging and environment dependent configuration.

## Code Example

### Hosting SafeBackgroundService (and other IHostServices)

```csharp
class SafeProgram : SafeProgramBase<HostedServiceLikeSafeBackgroundService>
{
    protected override void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
    {
        services
            .AddSingleton<ISomeService, SomeServiceImpl>();

        // do not add HostedService to services, it has already been added
    }

    public static void Main(string[] args)
    {
        new SafeProgram().run();
    }
}
```

### Implementing SafeBackgroundService

To use this class you should override at least one of following methods: 

```csharp
    protected abstract Task ConnectAsync(CancellationToken cancellationToken);

    protected abstract Task DisconnectAsync();

    protected abstract Task ExecuteIterationAsync(CancellationToken cancellationToken);
```

You also need to provide ISafeBackgroundServicePanicHandler to the constructor if you don't use
SafeProgramBase. If you do use SafeProgramBase, it will be automatically provided and will stop 
all services when one of them raised panic event.

## Motivation

When creating long-running background services you have to deal with failures caused by environment like
network failure, SQL timeout or other. Most of those conditions are temporary and should not cause service
failure.

This implementation separates small failures (wait and try again) and big failures (stop, wait, initialize and
try again).

## Installation

Use NuGet.

## API Reference

Look at code example should be enough.

## Tests

Not included in this release (but be patient, I'll migrate it later)

## Contributors

Every contributor is welcome here. But keep it simple.

## License

I like MIT licence for my work, so this one will be used.

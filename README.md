## Synopsis

Provides full-flavour implementation of error-prune background service with Serilog logging and environment
dependend configuration.

## Code Example

### Hosting SafeBackgroundService (and other IHostServices)

```csharp
class Program : ProgramBase<HostedServiceLikeSafeBackgroundService>
{
    protected override void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
    {
        services
            .AddSingleton<ISomeService, SomeServiceImpl>();

        // do not add HostedService to services, it has already been added
    }

    public static void Main(string[] args)
    {
        new Program().run();
    }
}
```

### Implementing SafeBackgroundService

To use this class you should override at least one of first three methods below. 

```csharp
    protected abstract Task ConnectAsync(CancellationToken cancellationToken);

    protected abstract Task DisconnectAsync();

    protected abstract Task ExecuteIterationAsync(CancellationToken cancellationToken);

    protected virtual void Panic(Exception reasonException) { }
```

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

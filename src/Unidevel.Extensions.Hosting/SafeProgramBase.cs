using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Threading;

namespace Unidevel.Extensions.Hosting
{
    public class SafeProgramBase<P> : SafeProgramBase
       where P : class, IHostedService
    {
        protected override void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            base.ConfigureServices(hostContext, services);

            services.AddSingleton<IHostedService, P>();
        }
    }

    public class SafeProgramBase<P, Q> : SafeProgramBase
        where P : class, IHostedService
        where Q : class, IHostedService
    {
        protected override void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            base.ConfigureServices(hostContext, services);

            services.AddSingleton<IHostedService, P>();
            services.AddSingleton<IHostedService, Q>();
        }
    }

    public class SafeProgramBase<P, Q, R> : SafeProgramBase
        where P : class, IHostedService
        where Q : class, IHostedService
        where R : class, IHostedService
    {
        protected override void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            base.ConfigureServices(hostContext, services);

            services.AddSingleton<IHostedService, P>();
            services.AddSingleton<IHostedService, Q>();
            services.AddSingleton<IHostedService, R>();
        }
    }

    public class SafeProgramBase : ISafeBackgroundServicePanicHandler
    {
        protected virtual void ConfigureHostConfiguration(IConfigurationBuilder configHost)
        {
            configHost.SetBasePath(Directory.GetCurrentDirectory());
            configHost.AddEnvironmentVariables("NETCORE_");
            configHost.AddCommandLine(System.Environment.GetCommandLineArgs());
        }

        protected virtual void ConfigureAppConfiguration(HostBuilderContext hostContext, IConfigurationBuilder config)
        {
            config
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(path: "appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName.ToLower()}.json", true)
                .AddEnvironmentVariables()
                .AddCommandLine(System.Environment.GetCommandLineArgs());
        }

        protected virtual void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            services.AddSingleton<ISafeBackgroundServicePanicHandler>(this);
        }

        protected virtual void ConfigureLogging(HostBuilderContext hostContext, ILoggingBuilder logging)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(hostContext.Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            logging.AddSerilog(dispose: true);
        }

        protected void run()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureHostConfiguration(configHost => ConfigureHostConfiguration(configHost))
                .ConfigureAppConfiguration((hostContext, config) => ConfigureAppConfiguration(hostContext, config))
                .ConfigureServices((hostContext, services) => ConfigureServices(hostContext, services))
                .ConfigureLogging((hostContext, logging) => ConfigureLogging(hostContext, logging));

            host = hostBuilder.Build();
            host.Run();
        }

        void ISafeBackgroundServicePanicHandler.HandlePanic(Exception reasonException)
        {
            // Make it in thread-safe manner

            IHost currentHost;

            lock (hostPanicLock)
            {
                currentHost = host;
                host = null;
            }

            if (currentHost != null)
            {
                Log.Logger.Fatal(reasonException, "Panic received, attempting to shutdown program.");

                using (var gracefulShutdownToken = new CancellationTokenSource())
                {
                    Log.Logger.Warning("Panic shutdown procedure started.");

                    gracefulShutdownToken.CancelAfter(TimeSpan.FromMinutes(5));
                    gracefulShutdownToken.Token.Register(() => Log.Logger.Error("Non-graceful shutdown forced after graceful timeout."));
                    currentHost.StopAsync(gracefulShutdownToken.Token);

                    Log.Logger.Warning("Panic shutdown procedure completed.");
                }
            }
            else
            {
                Log.Logger.Warning("Panic received and ignored because shutdown seems to be already performed.");
            }
        }

        private IHost host;
        private readonly object hostPanicLock = new object();
    }
}
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IO;

namespace Unidevel.Extensions.Hosting
{
    public class ProgramBase<T>
        where T : class, IHostedService
    {
        protected virtual void ConfigureHostConfiguration(IConfigurationBuilder configHost)
        {
        }

        protected virtual void ConfigureAppConfiguration(HostBuilderContext hostContext, IConfigurationBuilder config)
        {
        }

        protected virtual void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
        }

        protected virtual void ConfigureLogging(HostBuilderContext hostContext, ILoggingBuilder logging)
        {
        }

        private void configureHostConfiguration(IConfigurationBuilder configHost)
        {
            configHost.SetBasePath(Directory.GetCurrentDirectory());
            configHost.AddEnvironmentVariables("NETCORE_");
            configHost.AddCommandLine(System.Environment.GetCommandLineArgs());

            ConfigureHostConfiguration(configHost);
        }

        private void configureAppConfiguration(HostBuilderContext hostContext, IConfigurationBuilder config)
        {
            config
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(path: "appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName.ToLower()}.json", true)
                .AddEnvironmentVariables()
                .AddCommandLine(System.Environment.GetCommandLineArgs());

            ConfigureAppConfiguration(hostContext, config);
        }

        private void configureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            services.AddSingleton<IHostedService, T>();

            ConfigureServices(hostContext, services);
        }

        private void configureLogging(HostBuilderContext hostContext, ILoggingBuilder logging)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(hostContext.Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            logging.AddSerilog(dispose: true);

            ConfigureLogging(hostContext, logging);
        }

        protected void run()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureHostConfiguration(configHost => configureHostConfiguration(configHost))
                .ConfigureAppConfiguration((hostContext, config) => configureAppConfiguration(hostContext, config))
                .ConfigureServices((hostContext, services) => configureServices(hostContext, services))
                .ConfigureLogging((hostContext, logging) => configureLogging(hostContext, logging));

            hostBuilder.Build().Run();
        }
    }
}
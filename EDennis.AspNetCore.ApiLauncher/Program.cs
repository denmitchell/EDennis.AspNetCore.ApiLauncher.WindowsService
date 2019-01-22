using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace EDennis.AspNetCore.ApiLauncher {

    /// <summary>
    /// To run as a service ...
    /// https://www.stevejgordon.co.uk/running-net-core-generic-host-applications-as-a-windows-service
    /// </summary>
    class Program {
        public static async Task Main(string[] args) {

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.RollingFile("c:\\Temp\\apiLauncherLog-{Date}.txt")
                .WriteTo.Console()
                .CreateLogger();

            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            if (isService) {
                var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
                var pathToContentRoot = Path.GetDirectoryName(pathToExe);
                Directory.SetCurrentDirectory(pathToContentRoot);
            }

            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) => {
                    config.AddJsonFile("appsettings.json");
                    config.AddEnvironmentVariables();
                    if (args != null) {
                        config.AddCommandLine(args);
                    }
                })
                .ConfigureServices((hostContext, services) => {
                    services.AddOptions();
                    services.Configure<MqttConfig>(hostContext.Configuration.GetSection("MQTT"));
                    services.AddSingleton<IHostedService, MqttService>();
                    services.AddSingleton<DotNetProcessTerminator>();
                    services.AddSingleton<Launcher>();
                })
                .ConfigureLogging((hostingContext, logging) => {
                    //logging.AddConsole();
                    logging.AddSerilog();
                });
            
            builder.UseSerilog();

            if (isService) {
                await builder.RunAsServiceAsync();
            } else {
                await builder.RunConsoleAsync();
            }

        }
    }
}

using Serilog;
using Serilog.Templates.Themes;
using SerilogTracing;
using SerilogTracing.Expressions;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

// ReSharper disable RedundantSuppressNullableWarningExpression

namespace Example.WeatherService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithProperty("Application", typeof(Program).Assembly.GetName().Name)
                .MinimumLevel.Debug()
                .WriteTo.Console(Formatters.CreateConsoleTextFormatter(TemplateTheme.Code))
                .CreateBootstrapLogger();

            try
            {
                Log.Information("Starting web host");
                using (new ActivityListenerConfiguration().Instrument.HttpClientRequests().TraceToSharedLogger())
                {
                    CreateHostBuilder(args).Build().Run();
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}

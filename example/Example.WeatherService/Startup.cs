using Serilog;
using Serilog.Templates.Themes;
using SerilogTracing;
using SerilogTracing.Expressions;
using ILogger = Serilog.ILogger;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Example.WeatherService
{
    public class Startup
    {
        private IDisposable? _activityConfiguration;
        private Dictionary<string, string> _forecastByPostcode = null!;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSerilog(
                configuration =>
                    configuration
                        .Enrich.WithProperty("Application", typeof(Program).Assembly.GetName().Name)
                        .WriteTo.Console(Formatters.CreateConsoleTextFormatter(TemplateTheme.Code))
                        .WriteTo.SeqTracing("http://localhost:5341")
                        .WriteTo.Console()
                );

            services.AddLogging(
                builder =>
                {
                    builder.AddSerilog();

                    _activityConfiguration = new ActivityListenerConfiguration().Instrument.HttpClientRequests().TraceToSharedLogger();
                });

            _forecastByPostcode = Directory.GetFiles("./data")
                .ToDictionary(f => Path.GetFileNameWithoutExtension(f)!, f => File.ReadAllText(f).Trim());
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseEndpoints(
                builder =>
                {
                    builder.MapGet(
                        "/{postcode}",
                        (string postcode) =>
                        {
                            var logger = app.ApplicationServices.GetRequiredService<ILogger>();
                            logger.Information("Begin getting forecast for {Postcode}", postcode);

                            using var activity = logger.StartActivity("Look up forecast for postcode {Postcode}", postcode);
                            var forecast = _forecastByPostcode[postcode];
                            activity.AddProperty("Forecast", forecast);


                            logger.Information("End getting forecast for {Postcode}, got {Forecast}", postcode, forecast);
                            return forecast;
                        });
                });
        }
    }
}

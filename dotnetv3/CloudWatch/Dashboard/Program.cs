using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using CloudWatchActions;
using CloudWatchScenario;

namespace Program;
public class Program
{

    private static ILogger logger = null!;
    private static CloudWatchWrapper _cloudWatchWrapper = null!;
    private static IConfiguration _configuration = null!;
    private static readonly List<string> _statTypes = new List<string> { "SampleCount", "Average", "Sum", "Minimum", "Maximum" };
    private static SingleMetricAnomalyDetector? anomalyDetector = null!;

    static async Task Main(string[] args)
    {
        // Set up dependency injection for the Amazon service.
        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
                logging.AddFilter("System", LogLevel.Debug)
                    .AddFilter<DebugLoggerProvider>("Microsoft", LogLevel.Information)
                    .AddFilter<ConsoleLoggerProvider>("Microsoft", LogLevel.Trace))
            .ConfigureServices((_, services) =>
            services.AddAWSService<IAmazonCloudWatch>()
            .AddTransient<CloudWatchWrapper>()
        )
        .Build();

        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("settings.json") // Load settings from .json file.
            .AddJsonFile("settings.local.json",
                true) // Optionally, load local settings.
            .Build();

        logger = LoggerFactory.Create(builder => { builder.AddConsole(); })
                              .CreateLogger<Program>();

        _cloudWatchWrapper = host.Services.GetRequiredService<CloudWatchWrapper>();

        Console.WriteLine(new string('-', 80));
        Console.WriteLine("Amazon CloudWatch Metrics Retriever.");
        Console.WriteLine(new string('-', 80));

        // Run metric functions here
        try
        {   // set the namespace - there is no default and without namespace the metrics can be
            // allocated to the wrong area on accident
            var selectedNamespace = await SelectNamespace();
            

        }
        catch (Exception ex) { logger.LogError(ex, "There was a problem executing the scenario."); }
    }

    private static async Task CreateDashboard()
    {
        Console.WriteLine("Creating Dashboard...");
        var dashboardName = _configuration["dashboardName"];
        var newDashboard = new DashboardModel();
        //TODO - update new dashboard info here
        // Add a new metric to the dashboard.
        newDashboard.Widgets.Add(new Widget
        {
            Height = 8,
            Width = 8,
            Y = 8,
            X = 0,
            Type = "metric",
            Properties = new Properties
            {
                Metrics = new List<List<object>>
                    { //todo
                      },
                View = "timeSeries",
                Region = _configuration["region"],
                Stat = "Sum",
                Period = 86400,
                YAxis = new YAxis { Left = new Left { Min = 0, Max = 100 } },
                Title = "Custom Metric Widget",
                LiveData = true,
                Sparkline = true,
                Trend = true,
                Stacked = false,
                SetPeriodToTimeRange = false
            }
        });

        var newDashboardString = JsonSerializer.Serialize(newDashboard,
            new JsonSerializerOptions
            { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        var validationMessages =
            await _cloudWatchWrapper.PutDashboard(dashboardName, newDashboardString);
    }


    private static async Task<string> SelectNamespace()
    {
        Console.WriteLine(new string('-', 80));
        Console.WriteLine($"CloudWatch Namespace");
        var metrics = await _cloudWatchWrapper.ListMetrics();
        // namespace = targeted dashboard??
        var selectedNamespace = _configuration["dashboardName"];

        Console.WriteLine(new string('-', 80));

        return selectedNamespace;
    }


    
}

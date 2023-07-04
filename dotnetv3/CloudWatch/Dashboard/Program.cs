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
using Amazon.EC2;
using Newtonsoft;
using Newtonsoft.Json;
using Dashboard;

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
           
            var selectedNamespace = "AWS/EC2";
            UpdateJson(region:"ap-southeast-2",dashboard:"Metrics-DEV", accountid:"427510574968", instancetype:"t3a.medium");
            await CreateDashboard();
            //await CleanupResources();
            

        }
        catch (Exception ex) { logger.LogError(ex, "There was a problem executing the scenario."); }
    }
    // Creates a dashboard in the accounts AWS Cloudwatch account
    private static async Task CreateDashboard()
    {   // Dashboard name from settings.json and model
        Console.WriteLine("Creating Dashboard...");
        var dashboardName = _configuration["dashboardName"];
        var newDashboard = new DashboardModel();
        // Cloudwatch statistics
        var client = new AmazonCloudWatchClient();

        var dimension = new Dimension
        {
            Name = dashboardName,
            Value = "Virtual Machine Metric",
            Namespace
        };

        
        //,new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }
        var newDashboardString = System.Text.Json.JsonSerializer.Serialize(newDashboard);
        await _cloudWatchWrapper.PutDashboard(dashboardName, newDashboardString);
    }

    // Optionally cleans up the dashboard by deleting it
    private static async Task CleanupResources()
    {
        Console.WriteLine(new string('-', 80));
        Console.WriteLine($"18. Clean up resources.");

        var dashboardName = _configuration["dashboardName"];
        if (GetYesNoResponse($"\tDelete dashboard {dashboardName}? (y/n)"))
        {
            Console.WriteLine($"\tDeleting dashboard.");
            var dashboardList = new List<string> { dashboardName };
            await _cloudWatchWrapper.DeleteDashboards(dashboardList);
        }

        var alarmName = _configuration["exampleAlarmName"];
        if (GetYesNoResponse($"\tDelete alarm {alarmName}? (y/n)"))
        {
            Console.WriteLine($"\tCleaning up alarms.");
            var alarms = new List<string> { alarmName };
            await _cloudWatchWrapper.DeleteAlarms(alarms);
        }

        if (GetYesNoResponse($"\tDelete anomaly detector? (y/n)") && anomalyDetector != null)
        {
            Console.WriteLine($"\tCleaning up anomaly detector.");

            await _cloudWatchWrapper.DeleteAnomalyDetector(
                anomalyDetector);
        }

        Console.WriteLine(new string('-', 80));
    }
    private static bool GetYesNoResponse(string question)
    {
        Console.WriteLine(question);
        var ynResponse = Console.ReadLine();
        var response = ynResponse != null &&
                       ynResponse.Equals("y",
                           StringComparison.InvariantCultureIgnoreCase);
        return response;
    }

    private static async Task ProvideMetrics()
    {
        var client = new AmazonEC2Client();

    }
    // Convert settings.json to jsonobject to update the values with the needed information.
    private static void UpdateJson(string region, string dashboard, string accountid, string instancetype)
    {
        
        string json = File.ReadAllText("settings.json");
        var settings = JsonConvert.DeserializeObject<Settings>(json);

        settings.InstanceTypes[0].InstanceType = instancetype;
        settings.dashboardName = dashboard;
        settings.accountId = accountid;
        settings.region = region;
        string output = JsonConvert.SerializeObject(settings, Formatting.Indented);

        File.WriteAllText("settings.json", output);
    }
}

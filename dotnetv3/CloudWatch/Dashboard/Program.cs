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
using System.Data;

namespace Program;
public class Program
{
    private readonly IAmazonCloudWatch _amazonCloudWatch;
    private static ILogger logger = null!;
    private static CloudWatchWrapper _cloudWatchWrapper = null!;
    private static IConfiguration _configuration = null!;
    private static readonly List<string> _statTypes = new List<string> { "SampleCount", "Average", "Sum", "Minimum", "Maximum" };
    private static SingleMetricAnomalyDetector? anomalyDetector = null!;
    public List<Datapoint> dp;

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
            UpdateJson(region:"ap-southeast-2",dashboard:"Metrics-DEV", accountid:"", instancetype:"t3a.medium");
            Console.WriteLine("Listing Metrics..");


            var namespaceMetrics = await _cloudWatchWrapper.ListMetrics(selectedNamespace);

            for (int i = 0; i < namespaceMetrics.Count && i < 15; i++)
            {
                var dimensionsWithValues = namespaceMetrics[i].Dimensions
                    .Where(d => !string.Equals("None", d.Value));
                Console.WriteLine($"\t{i + 1}. {namespaceMetrics[i].MetricName} " +
                                  $"{string.Join(", :", dimensionsWithValues.Select(d => d.Value))}");
            }

            // Dimension is key/value pair
            //Dimension dim1 = new Dimension() { Name = "InstanceId", Value = "i-050ed4aeec8409673" };
            //Dimension dim2 = new Dimension() { Name = "Namespace", Value = "AWS/EC2" };
            //Dimension dim3 = new Dimension() { Name = "MetricName", Value = "CPUUtilization" };
            // Dimension dim4 = new Dimension() { Name = "Region", Value = "ap-southeast-2" };
            Dimension dim5 = new Dimension() { Name="Timestamp", Value= "2023-07-10T21:00Z" };
            List < Dimension > dimensions = new List<Dimension>() { //dim1 
                //dim2, 
                //dim3,
                dim5
            };
            /*
             * Manual CLI command - aws cloudwatch get-metric-statistics --namespace AWS/EC2 --metric-name CPUUtilization --dimensions Name=InstanceId,Value=i-050ed4aeec8409673 --statistics Average --start-time 2023-07-09T11:53:04Z --end-time 2023-07-10T11:53:04Z --period 60
             */
            Console.WriteLine("CPUUtilization");
            await GetMetricStats(selectedNamespace, new Metric 
            { 
                Namespace = selectedNamespace,
                MetricName = "CPUUtilization",
                Dimensions = dimensions
            });
            Console.WriteLine("DiskReadBytes");
            await GetMetricStats(selectedNamespace, new Metric
            {
                Namespace = selectedNamespace,
                MetricName = "DiskReadBytes",
                Dimensions = dimensions
            });
            //await CreateDashboard();

            await CleanupResources();
            

        }
        catch (Exception ex) { logger.LogError(ex, "There was a problem executing the scenario."); }
    }
  
    public static async Task GetMetricStats(string metricNamespace, Metric metric)
    {

        Console.WriteLine(new string('-', 80));
        Console.WriteLine($"Getting CloudWatch metric statistics.");
       
        /*
        for (int i = 0; i < _statTypes.Count; i++)
        {
            Console.WriteLine($"\t{i + 1}. {_statTypes[i]}");
        }

        var statisticChoiceNumber = 0;
        while (statisticChoiceNumber < 1 || statisticChoiceNumber > _statTypes.Count)
        {
            Console.WriteLine(
                "Select a metric statistic by entering a number from the preceding list:");
            var choice = Console.ReadLine();
            Int32.TryParse(choice, out statisticChoiceNumber);
        }

    */
        //var selectedStatistic = _statTypes[statisticChoiceNumber - 1];
        var statisticsList = new List<string> { };
        for (int i=0;i< _statTypes.Count-1;i++)
        {
            statisticsList.Add(_statTypes[i]);
        }

        var metricStatistics = await _cloudWatchWrapper.GetMetricStatistics(metricNamespace, metric.MetricName, statisticsList, metric.Dimensions, 1, 60);
        Console.WriteLine("Size of list:", metricStatistics.Count);
        foreach (var point in metricStatistics)
        {
            Console.WriteLine("Point Min:", point.Minimum.ToString());
            Console.WriteLine("Point Max:", point.Maximum.ToString());
        }
       /* metricStatistics = metricStatistics.OrderBy(s => s.Timestamp).ToList();
        for (int i = 0; i < metricStatistics.Count && i < 10; i++)
        {
            var metricStat = metricStatistics[i];
            var statValue = metricStat.GetType().GetProperty(selectedStatistic)!.GetValue(metricStat, null);
            Console.WriteLine($"\t{i + 1}. Timestamp {metricStatistics[i].Timestamp:G} {selectedStatistic}: {statValue}");
        }*/

        Console.WriteLine(new string('-', 80));
    }
  
    // Creates a dashboard in the accounts AWS Cloudwatch account
    private static async Task CreateDashboard()
    {   // Dashboard name from settings.json and model
        Console.WriteLine("Creating Dashboard...");
        var mr = new Metric_Requests();
        var dashboardName = _configuration["dashboardName"];
        var dashboardModel = new DashboardModel();
        _configuration.GetSection("dashboardBody").Bind(dashboardModel);
        var newDashboardString = System.Text.Json.JsonSerializer.Serialize(
            dashboardModel,
            new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        var validationMessages =
            await _cloudWatchWrapper.PutDashboard(dashboardName, newDashboardString);

        Console.WriteLine(validationMessages.Any() ? $"\tValidation messages:" : null);
        for (int i = 0; i < validationMessages.Count; i++)
        {
            Console.WriteLine($"\t{i + 1}. {validationMessages[i].Message}");
        }
        Console.WriteLine($"\tDashboard {dashboardName} was created.");
        Console.WriteLine(new string('-', 80));
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

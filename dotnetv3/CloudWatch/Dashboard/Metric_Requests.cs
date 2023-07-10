using Amazon.CloudWatch.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudWatchScenario
{
    internal class Metric_Requests
    {
        private static IConfiguration _configuration = null!;
        public List<MetricDataQuery> Data_Query(string MetricName)
        {
            var query = new List<MetricDataQuery>{new MetricDataQuery
                {
                    AccountId = _configuration["accountId"],
                    Id = "m1",
                    Label = MetricName,
                    MetricStat = new MetricStat
                    {
                        Metric = new Metric
                        {
                            MetricName = MetricName,
                            Namespace = "AWS/EC2",
                        },
                        Period = 30,
                        Stat = "Maximum"
                    }
                }
            };
            return query;
        }
    }
}


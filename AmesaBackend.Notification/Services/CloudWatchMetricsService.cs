using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Notification.Services
{
    public interface ICloudWatchMetricsService
    {
        Task PutMetricAsync(string metricName, double value, string unit = "Count");
        Task PutMetricWithDimensionsAsync(string metricName, Dictionary<string, string> dimensions, double value, string unit = "Count");
    }

    public class CloudWatchMetricsService : ICloudWatchMetricsService
    {
        private readonly IAmazonCloudWatch _cloudWatch;
        private readonly ILogger<CloudWatchMetricsService> _logger;
        private readonly string _namespace = "Amesa/Notification";
        
        public CloudWatchMetricsService(
            IAmazonCloudWatch cloudWatch,
            ILogger<CloudWatchMetricsService> logger)
        {
            _cloudWatch = cloudWatch;
            _logger = logger;
        }
        
        /// <summary>
        /// Puts a metric to CloudWatch with default dimensions
        /// </summary>
        public async Task PutMetricAsync(string metricName, double value, string unit = "Count")
        {
            try
            {
                var request = new PutMetricDataRequest
                {
                    Namespace = _namespace,
                    MetricData = new List<MetricDatum>
                    {
                        new MetricDatum
                        {
                            MetricName = metricName,
                            Value = value,
                            Unit = unit,
                            TimestampUtc = DateTime.UtcNow,
                            Dimensions = new List<Dimension>
                            {
                                new Dimension { Name = "Service", Value = "Notification" }
                            }
                        }
                    }
                };
                
                await _cloudWatch.PutMetricDataAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to put CloudWatch metric {MetricName}", metricName);
                // Don't throw - metrics are non-critical
            }
        }
        
        /// <summary>
        /// Puts a metric to CloudWatch with custom dimensions
        /// </summary>
        public async Task PutMetricWithDimensionsAsync(string metricName, Dictionary<string, string> dimensions, double value, string unit = "Count")
        {
            try
            {
                var dimensionList = new List<Dimension>
                {
                    new Dimension { Name = "Service", Value = "Notification" }
                };
                
                foreach (var dim in dimensions)
                {
                    dimensionList.Add(new Dimension { Name = dim.Key, Value = dim.Value });
                }
                
                var request = new PutMetricDataRequest
                {
                    Namespace = _namespace,
                    MetricData = new List<MetricDatum>
                    {
                        new MetricDatum
                        {
                            MetricName = metricName,
                            Value = value,
                            Unit = unit,
                            TimestampUtc = DateTime.UtcNow,
                            Dimensions = dimensionList
                        }
                    }
                };
                
                await _cloudWatch.PutMetricDataAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to put CloudWatch metric {MetricName} with dimensions", metricName);
                // Don't throw - metrics are non-critical
            }
        }
    }
}


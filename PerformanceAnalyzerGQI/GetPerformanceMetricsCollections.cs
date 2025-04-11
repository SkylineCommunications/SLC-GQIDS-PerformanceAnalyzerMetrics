namespace Skyline.DataMiner.Utils.PerformanceAnalyzerGQI
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json;
    using Skyline.DataMiner.Analytics.GenericInterface;
    using Skyline.DataMiner.Utils.PerformanceAnalyzerGQI.Models;

    [GQIMetaData(Name = "Get Performance Metrics Collections")]
    public class GetPerformanceMetricsCollections : IGQIDataSource, IGQIInputArguments
    {
        private readonly GQIStringArgument fileLocation = new GQIStringArgument("File Location") { IsRequired = false };
        private readonly GQIStringArgument fileName = new GQIStringArgument("File Name") { IsRequired = false };

        internal static List<PerformanceLog> PerformanceMetrics { get; set; }

        public GQIArgument[] GetInputArguments()
        {
            return new GQIArgument[] { fileLocation, fileName };
        }

        public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
        {
            try
            {
                var location = args.GetArgumentValue(fileLocation);
                var names = args.GetArgumentValue(fileName).Split(',');

                PerformanceMetrics=new List<PerformanceLog>();

                foreach (var name in names)
                {
                    var rawJson = File.ReadAllText(Path.Combine(location, name));
                    PerformanceMetrics.AddRange(JsonConvert.DeserializeObject<List<PerformanceLog>>(rawJson));
                }

                return default;
            }
            catch (Exception ex)
            {
                throw new Exception("Please select at least one file, so valid data can be shown.");
            }
        }

        public GQIColumn[] GetColumns()
        {
            return new GQIColumn[]
            {
                new GQIStringColumn("Name"),
                new GQIDateTimeColumn("Start Time"),
                new GQIDateTimeColumn("End Time"),
                new GQIDoubleColumn("Execution Time"),
                new GQIStringColumn("Metadata"),
                new GQIStringColumn("ID"),
            };
        }

        public GQIPage GetNextPage(GetNextPageInputArgs args)
        {
            var rows = new List<GQIRow>();

            foreach (var metric in PerformanceMetrics)
            {
                DateTime endTime = metric.Data.Max(d => d.StartTime + d.ExecutionTime);
                TimeSpan executionTime = endTime - metric.StartTime.ToUniversalTime();

                rows.Add(new GQIRow(
                        new[]
                        {
                            new GQICell
                            {
                                Value = metric.Name,
                            },
                            new GQICell
                            {
                                Value = metric.StartTime.ToUniversalTime(),
                            },
                            new GQICell
                            {
                                Value = endTime,
                            },
                            new GQICell
                            {
                                Value = executionTime.TotalMilliseconds,
                                DisplayValue = GetExecutionTimeDisplayValue(executionTime),
                            },
                            new GQICell
                            {
                                Value = JsonConvert.SerializeObject(metric.Metadata),
                            },
                            new GQICell
                            {
                                Value = metric.Id.ToString(),
                            },
                        }));
            }

            return new GQIPage(rows.ToArray());
        }

        private static string GetExecutionTimeDisplayValue(TimeSpan executionTime)
        {
            string executionTimeDisplayValue = Math.Round(executionTime.TotalSeconds, 3) + " s";
            if (executionTime.TotalSeconds < 1.0)
            {
                executionTimeDisplayValue = Math.Round(executionTime.TotalSeconds * 1000) + " ms";
            }

            return executionTimeDisplayValue;
        }
    }
}
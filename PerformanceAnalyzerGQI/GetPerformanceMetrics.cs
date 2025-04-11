namespace Skyline.DataMiner.Utils.PerformanceAnalyzerGQI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Skyline.DataMiner.Analytics.GenericInterface;
    using Skyline.DataMiner.Utils.PerformanceAnalyzerGQI.Models;

    [GQIMetaData(Name = "Get Performance Metrics")]
    public class GetPerformanceMetrics : IGQIDataSource, IGQIInputArguments
    {
        private readonly GQIStringArgument idArg = new GQIStringArgument("ID") { IsRequired = true };

        private List<PerformanceLog> selectedPerformanceLog;

        internal static List<PerformanceLog> PerformanceMetrics => GetPerformanceMetricsCollections.PerformanceMetrics;

        public GQIArgument[] GetInputArguments()
        {
            return new GQIArgument[] { idArg };
        }

        public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
        {
            try
            {
                var ids = args.GetArgumentValue(idArg).Split(',');

                selectedPerformanceLog = PerformanceMetrics.Where(x => ids.Contains(x.Id.ToString())).ToList() ?? PerformanceMetrics;

                selectedPerformanceLog = selectedPerformanceLog.Any() ? selectedPerformanceLog : PerformanceMetrics;

                return default;
            }
            catch (Exception)
            {
                throw new Exception("Please, select one row so valid data could be shown.");
            }
        }

        public GQIColumn[] GetColumns()
        {
            return new GQIColumn[]
            {
                new GQIStringColumn("Class"),
                new GQIStringColumn("Method"),
                new GQIDateTimeColumn("Start Time"),
                new GQIDateTimeColumn("End Time"),
                new GQIDoubleColumn("Execution Time"),
                new GQIIntColumn("Method Level"),
                new GQIStringColumn("Metadata"),
            };
        }

        public GQIPage GetNextPage(GetNextPageInputArgs args)
        {
            var rows = new List<GQIRow>();

            foreach (var performanceMetric in selectedPerformanceLog)
            {
                foreach (var performanceData in performanceMetric.Data)
                {
                    ProcessSubMethods(performanceData, rows, 0);
                }
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

        private void ProcessSubMethods(PerformanceData data, List<GQIRow> rows, int level)
        {
            if (data == null)
            {
                return;
            }

            CreateRow(data, rows, level);

            if (data.SubMethods != null && data.SubMethods.Any())
            {
                foreach (var subMethod in data.SubMethods)
                {
                    ProcessSubMethods(subMethod, rows, level + 1);
                }
            }
        }

        private void CreateRow(PerformanceData performanceData, List<GQIRow> rows, int level)
        {
            rows.Add(new GQIRow(
                new[]
                {
                    new GQICell
                    {
                        Value = performanceData.ClassName,
                    },
                    new GQICell
                    {
                        Value = performanceData.MethodName,
                    },
                    new GQICell
                    {
                        Value = performanceData.StartTime,
                    },
                    new GQICell
                    {
                        Value = performanceData.StartTime + performanceData.ExecutionTime,
                    },
                    new GQICell
                    {
                        Value = performanceData.ExecutionTime.TotalMilliseconds,
                        DisplayValue = GetExecutionTimeDisplayValue(performanceData.ExecutionTime),
                    },
                    new GQICell
                    {
                        Value = level,
                    },
                    new GQICell
                    {
                        Value = performanceData.ShouldSerializeMetadata() ? JsonConvert.SerializeObject(performanceData.Metadata) : string.Empty,
                    },
                }));
        }
    }
}
namespace Skyline.DataMiner.Utils.PerformanceAnalyzerGQI
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Newtonsoft.Json;

    using Skyline.DataMiner.Analytics.GenericInterface;
    using Skyline.DataMiner.Utils.PerformanceAnalyzerGQI.Models;

    [GQIMetaData(Name = "Get Performance Metrics")]
    public class GetPerformanceMetrics : IGQIDataSource, IGQIInputArguments
    {
        private readonly GQIStringArgument nameArg = new GQIStringArgument("Name") { IsRequired = true };
        private readonly GQIStringArgument startTimeArg = new GQIStringArgument("Start Time") { IsRequired = true };

        private List<PerformanceLog> selectedPerformaceLog;

        internal static List<PerformanceLog> PerformanceMetrics => GetPerformanceMetricsCollections.PerformanceMetrics;

        public GQIArgument[] GetInputArguments()
        {
            return new GQIArgument[] { nameArg, startTimeArg };
        }

        public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
        {
            var name = args.GetArgumentValue(nameArg);
            var startTime = args.GetArgumentValue(startTimeArg);

            selectedPerformaceLog = PerformanceMetrics
                .Where(x => x.Name.Equals(name) && Convert.ToString(x.StartTime.ToOADate()).Equals(startTime))
                .ToList() ?? PerformanceMetrics;

            selectedPerformaceLog = selectedPerformaceLog.Any() ? selectedPerformaceLog : PerformanceMetrics;

            return default;
        }

        public GQIColumn[] GetColumns()
        {
            return new GQIColumn[]
            {
                new GQIStringColumn("Class"),
                new GQIStringColumn("Method"),
                new GQIDateTimeColumn("Start Time"),
                new GQIDateTimeColumn("End Time"),
                new GQIIntColumn("Execution Time"),
                new GQIIntColumn("Method Level"),
                new GQIStringColumn("Metadata"),
            };
        }

        public GQIPage GetNextPage(GetNextPageInputArgs args)
        {
            var rows = new List<GQIRow>();

            foreach (var performanceMetric in selectedPerformaceLog)
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
                        DisplayValue = performanceData.StartTime.ToString("dd/MM/yyyy HH:mm:ss.fff"),
                    },
                    new GQICell
                    {
                        Value = performanceData.StartTime + performanceData.ExecutionTime,
                        DisplayValue = (performanceData.StartTime + performanceData.ExecutionTime).ToString("dd/MM/yyyy HH:mm:ss.fff"),
                    },
                    new GQICell
                    {
                        Value = (int)performanceData.ExecutionTime.TotalMilliseconds,
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
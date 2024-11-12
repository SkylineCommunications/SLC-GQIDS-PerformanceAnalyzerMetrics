namespace Skyline.DataMiner.Utils.PerformanceAnalyzerGQI
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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
            var location = args.GetArgumentValue(fileLocation);
            var name = args.GetArgumentValue(fileName);

            var rawJson = File.ReadAllText(Path.Combine(location, name));
            PerformanceMetrics = JsonConvert.DeserializeObject<List<PerformanceLog>>(rawJson);

            return default;
        }

        public GQIColumn[] GetColumns()
        {
            return new GQIColumn[]
            {
                new GQIStringColumn("Name"),
                new GQIStringColumn("Start Time"),
                new GQIStringColumn("Metadata"),
            };
        }

        public GQIPage GetNextPage(GetNextPageInputArgs args)
        {
            var rows = new List<GQIRow>();

            foreach (var metric in PerformanceMetrics)
            {
                rows.Add(new GQIRow(
                        new[]
                        {
                            new GQICell
                            {
                                Value = metric.Name,
                            },
                            new GQICell
                            {
                                Value = Convert.ToString(metric.StartTime.ToOADate()),
                                DisplayValue = metric.StartTime.ToString("MM/dd/yyyy hh:mm:ss tt"),
                            },
                            new GQICell
                            {
                                Value = JsonConvert.SerializeObject(metric.Metadata),
                            },
                        }));
            }

            return new GQIPage(rows.ToArray());
        }
    }
}
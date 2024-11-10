namespace Skyline.DataMiner.Utilities.PerformanceAnalyzerGQI
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Utilities.PerformanceAnalyzerGQI.Models;

	[GQIMetaData(Name = "Get Performance Metrics")]
	public class GetPerformanceMetrics : IGQIDataSource, IGQIInputArguments
	{
		private readonly GQIStringArgument folderPathArgument = new GQIStringArgument("Folder Path") { IsRequired = false };
		private readonly GQIStringArgument fileNameArgument = new GQIStringArgument("File Name") { IsRequired = true };

		internal static List<PerformanceLog> PerformanceMetrics { get; set; }

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { folderPathArgument, fileNameArgument };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			var folderPath = String.IsNullOrWhiteSpace(args.GetArgumentValue(folderPathArgument)) ? @"C:\Skyline_Data\PerformanceLogger" : args.GetArgumentValue(folderPathArgument);
			var fileName = args.GetArgumentValue(fileNameArgument);

			var rawJson = File.ReadAllText(Path.Combine(folderPath, fileName));

			PerformanceMetrics = JsonConvert.DeserializeObject<List<PerformanceLog>>(rawJson);

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
				new GQIStringColumn("ID"),
			};
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			var rows = new List<GQIRow>();

			foreach (var performanceMetric in PerformanceMetrics)
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

			data.Id = Guid.NewGuid();
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
					new GQICell
					{
						Value = performanceData.Id.ToString(),
					},
				}));
		}
	}
}
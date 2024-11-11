namespace Skyline.DataMiner.Utils.PerformanceAnalyzerGQI
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Utils.PerformanceAnalyzerGQI.Models;

	[GQIMetaData(Name = "Get Performance Metadata Metrics")]
	public class GetPerformanceMetadataMetrics : IGQIDataSource, IGQIInputArguments
	{
		private readonly GQIStringArgument metadataIdArgument = new GQIStringArgument("Metadata ID") { IsRequired = true };
		private List<PerformanceLog> performanceMetrics;

		private string id;

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { metadataIdArgument };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			id = args.GetArgumentValue(metadataIdArgument);

			performanceMetrics = GetPerformanceMetrics.PerformanceMetrics;

			return default;
		}

		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[]
			{
				new GQIStringColumn("Key"),
				new GQIStringColumn("Value"),
			};
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			Dictionary<string, string> metadata = new Dictionary<string, string>();

			foreach (var performanceMetric in performanceMetrics)
			{
				foreach (var performanceData in performanceMetric.Data)
				{
					metadata = GetMetadataNeeded(performanceData);
					if (metadata.Count > 0)
						break;
				}

				if (metadata.Count > 0)
					break;
			}

			if (metadata.Count > 0)
			{
				var row = GenerateRow(metadata);

				return new GQIPage(row.ToArray());
			}
			else
			{
				return new GQIPage(new GQIRow[0]);
			}
		}

		private IEnumerable<GQIRow> GenerateRow(Dictionary<string, string> metadata)
		{
			var rows = new List<GQIRow>();

			foreach (var info in metadata)
			{
				var row = new GQIRow(new[]
					{
						new GQICell { Value = info.Key },
						new GQICell { Value = info.Value },
					});

				rows.Add(row);
			}

			return rows;
		}

		private Dictionary<string, string> GetMetadataNeeded(PerformanceData data)
		{
			if (data == null)
			{
				return new Dictionary<string, string>();
			}

			CheckForCorrectMetadata(data, out var metadata);
			if (metadata.Count > 0)
				return metadata;

			if (data.SubMethods != null && data.SubMethods.Any())
			{
				foreach (var subMethod in data.SubMethods)
				{
					var subMetadata = GetMetadataNeeded(subMethod);
					if (subMetadata.Count > 0)
						return subMetadata;
				}
			}

			return new Dictionary<string, string>();
		}

		private void CheckForCorrectMetadata(PerformanceData data, out Dictionary<string, string> metadata)
		{
			var isCorrectMetadata = Guid.TryParse(id, out var parsedID) && parsedID == data.Id;
			metadata = isCorrectMetadata ? data.Metadata : new Dictionary<string, string>();
		}
	}
}
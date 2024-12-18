namespace Skyline.DataMiner.Utils.PerformanceAnalyzerGQI
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Threading.Tasks;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Utils.PerformanceAnalyzerGQI.Models;

	[GQIMetaData(Name = "Get Performance Metrics Multi File")]
	public class GetPerformanceMetricsMultiFile : IGQIDataSource, IGQIOnInit, IGQIInputArguments
	{
		private static readonly GQIStringColumn _fileNameColumn = new GQIStringColumn("File Name");
		private static readonly GQIStringColumn _classColumn = new GQIStringColumn("Class");
		private static readonly GQIStringColumn _methodColumn = new GQIStringColumn("Method");
		private static readonly GQIDateTimeColumn _startTimeColumn = new GQIDateTimeColumn("Start Time");
		private static readonly GQIDateTimeColumn _endTimeColumn = new GQIDateTimeColumn("End Time");
		private static readonly GQIIntColumn _executionTimeColumn = new GQIIntColumn("Execution Time");
		private static readonly GQIIntColumn _methodLevelColumn = new GQIIntColumn("Method Level");
		private static readonly GQIStringColumn _metadataColumn = new GQIStringColumn("Metadata");

		private readonly GQIStringArgument pathArgument = new GQIStringArgument("Path") { IsRequired = true, DefaultValue = @"C:\Skyline_Data\MetricLogging" };
		private readonly GQIStringArgument searchPatternArgument = new GQIStringArgument("Search Pattern") { IsRequired = false, DefaultValue = "*.*" };
		private readonly GQIBooleanArgument searchOptionArgument = new GQIBooleanArgument("Recursive") { DefaultValue = false };
		private readonly GQIStringArgument classArgument = new GQIStringArgument("Class") { IsRequired = true };
		private readonly GQIStringArgument methodArgument = new GQIStringArgument("Method") { IsRequired = true };
		private readonly GQIDateTimeArgument startTimeArgument = new GQIDateTimeArgument("Start Time") { IsRequired = true };
		private readonly GQIDateTimeArgument endTimeArgument = new GQIDateTimeArgument("End Time") { IsRequired = true };

		private string fileLocation;
		private string searchPattern;
		private SearchOption searchOption;
		private string classFilter;
		private string methodFilter;
		private DateTime startTimeFilter;
		private DateTime endTimeFilter;

		private IGQILogger _logger;

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			_logger = args.Logger;
			_logger.MinimumLogLevel = GQILogLevel.Debug;
			return new OnInitOutputArgs();
		}

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { pathArgument, searchPatternArgument, searchOptionArgument, classArgument, methodArgument, startTimeArgument, endTimeArgument };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			fileLocation = args.GetArgumentValue(pathArgument);
			searchPattern = args.GetArgumentValue(searchPatternArgument);
			searchOption = args.GetArgumentValue(searchOptionArgument) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
			classFilter = args.GetArgumentValue(classArgument);
			methodFilter = args.GetArgumentValue(methodArgument);
			startTimeFilter = args.GetArgumentValue(startTimeArgument);
			endTimeFilter = args.GetArgumentValue(endTimeArgument);

			if (!Directory.Exists(fileLocation))
			{
				throw new GenIfException($"Path does not exist: {fileLocation}");
			}

			return default;
		}

		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[]
			{
				_fileNameColumn,
				_classColumn,
				_methodColumn,
				_startTimeColumn,
				_endTimeColumn,
				_executionTimeColumn,
				_methodLevelColumn,
				_metadataColumn,
			};
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			try
			{
				ConcurrentDictionary<string, List<PerformanceLog>> performanceMetricsByFile = new ConcurrentDictionary<string, List<PerformanceLog>>();
				List<FileMetricRow> rows = new List<FileMetricRow>();

				_logger.Information($"Start deserializing");
				var sw = Stopwatch.StartNew();
				LoadFiles(performanceMetricsByFile);
				_logger.Information($"Deserializing done in - {sw.ElapsedMilliseconds}ms.");

				_logger.Information($"Start processing");
				sw = Stopwatch.StartNew();
				CreateFileMetricRows(performanceMetricsByFile, rows);
				_logger.Information($"Processing done in - {sw.ElapsedMilliseconds}ms.");

				_logger.Information($"Start filtering rows");
				sw = Stopwatch.StartNew();
				var filteredRows = rows.Where(x => x.ClassName.Equals(classFilter) && x.MethodName.Equals(methodFilter)).OrderBy(x => x.StartTime);
				_logger.Information($"Filtering rows done in - {sw.ElapsedMilliseconds}ms.");

				_logger.Information($"Start creating GQI rows");
				sw = Stopwatch.StartNew();
				var gqiRows = new GQIPage(filteredRows.Select(x => CreateGqiRow(x)).ToArray()) { HasNextPage = false };
				_logger.Information($"Creating GQI rows done in - {sw.ElapsedMilliseconds}ms.");

				return gqiRows;
			}
			catch (Exception ex)
			{
				_logger.Error(ex.ToString());
			}

			return new GQIPage(new GQIRow[0]);
		}

		private void LoadFiles(ConcurrentDictionary<string, List<PerformanceLog>> performanceMetricsByFile)
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(fileLocation);
			FileInfo[] fileInfos = directoryInfo.GetFiles(searchPattern, searchOption);

			Parallel.ForEach(fileInfos, fileInfo =>
			{
				if (fileInfo.LastWriteTime > startTimeFilter && fileInfo.LastWriteTime < endTimeFilter)
				{
					using (StreamReader reader = new StreamReader(fileInfo.FullName))
					using (JsonTextReader jsonReader = new JsonTextReader(reader))
					{
						JsonSerializer serializer = new JsonSerializer();
						performanceMetricsByFile.TryAdd(fileInfo.Name, serializer.Deserialize<List<PerformanceLog>>(jsonReader));
					}
				}
			});
		}

		private void CreateFileMetricRows(ConcurrentDictionary<string, List<PerformanceLog>> performanceMetricsByFile, List<FileMetricRow> rows)
		{
			foreach (var fileMetrics in performanceMetricsByFile)
			{
				foreach (PerformanceLog performanceMetric in fileMetrics.Value.Where(x => x.StartTime > startTimeFilter && x.StartTime < endTimeFilter))
				{
					foreach (PerformanceData performanceData in performanceMetric.Data)
					{
						ProcessSubMethods(performanceData, rows, 0, fileMetrics.Key);
					}
				}
			}
		}

		private void ProcessSubMethods(PerformanceData data, List<FileMetricRow> rows, int level, string fileName)
		{
			if (data == null)
			{
				return;
			}

			rows.Add(CreateFileMetricRow(data, level, fileName));

			if (data.SubMethods != null && data.SubMethods.Any())
			{
				foreach (var subMethod in data.SubMethods)
				{
					ProcessSubMethods(subMethod, rows, level + 1, fileName);
				}
			}
		}

		private FileMetricRow CreateFileMetricRow(PerformanceData performanceData, int level, string fileName)
		{
			return new FileMetricRow
			{
				FileName = fileName,
				ClassName = performanceData.ClassName,
				MethodName = performanceData.MethodName,
				StartTime = performanceData.StartTime,
				EndTime = performanceData.StartTime + performanceData.ExecutionTime,
				ExecutionTime = performanceData.ExecutionTime,
				MethodLevel = level,
				Metadata = performanceData.ShouldSerializeMetadata() ? JsonConvert.SerializeObject(performanceData.Metadata) : string.Empty,
			};
		}

		private GQIRow CreateGqiRow(FileMetricRow fileMetricRow)
		{
			return new GQIRow(new[]
			{
				new GQICell
				{
					Value = fileMetricRow.FileName,
				},
				new GQICell
				{
					Value = fileMetricRow.ClassName,
				},
				new GQICell
				{
					Value = fileMetricRow.MethodName,
				},
				new GQICell
				{
					Value = fileMetricRow.StartTime,
				},
				new GQICell
				{
					Value = fileMetricRow.EndTime,
				},
				new GQICell
				{
					Value = (int)fileMetricRow.ExecutionTime.TotalMilliseconds,
					DisplayValue = GetExecutionTimeDisplayValue(fileMetricRow.ExecutionTime),
				},
				new GQICell
				{
					Value = fileMetricRow.MethodLevel,
				},
				new GQICell
				{
					Value = fileMetricRow.Metadata,
				},
			});
		}

		private static string GetExecutionTimeDisplayValue(TimeSpan executionTime)
		{
			string executionTimeDisplayValue = executionTime.TotalSeconds < 1.0 ? Math.Round(executionTime.TotalSeconds * 1000) + " ms" : Math.Round(executionTime.TotalSeconds, 3) + " s";

			return executionTimeDisplayValue;
		}
	}

	public class FileMetricRow
	{
		public string FileName { get; set; }

		public string ClassName { get; set; }

		public string MethodName { get; set; }

		public DateTime StartTime { get; set; }

		public DateTime EndTime { get; set; }

		public TimeSpan ExecutionTime { get; set; }

		public int MethodLevel { get; set; }

		public string Metadata { get; set; }
	}
}
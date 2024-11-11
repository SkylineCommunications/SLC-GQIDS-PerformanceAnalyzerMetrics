//------------------------------------------------------------------------------
// This code was copied from ScriptPerformanceLogger/Models/PerformanceData.
// Changes to this will result in errors.
//------------------------------------------------------------------------------
namespace Skyline.DataMiner.Utils.PerformanceAnalyzerGQI.Models
{
	using System;
	using System.Collections.Generic;

	using Newtonsoft.Json;

	/// <summary>
	/// <see cref="PerformanceData"/> is a model for method performance metrics.
	/// </summary>
	[Serializable]
	public class PerformanceData
	{
		[JsonConstructor]
		public PerformanceData()
		{
		}

		public PerformanceData(string className, string methodName)
		{
			ClassName = className;
			MethodName = methodName;
		}

		[JsonIgnore]
		public bool IsStarted { get; internal set; }

		[JsonIgnore]
		public bool IsStopped { get; internal set; }

		[JsonIgnore]
		public PerformanceData Parent { get; set; }

		[JsonIgnore]
		public Guid Id { get; set; }

		[JsonProperty(Order = 0)]
		public string ClassName { get; set; }

		[JsonProperty(Order = 1)]
		public string MethodName { get; set; }

		[JsonProperty(Order = 2)]
		public DateTime StartTime { get; set; }

		[JsonProperty(Order = 3)]
		public TimeSpan ExecutionTime { get; set; }

		[JsonProperty(Order = 4)]
		public List<PerformanceData> SubMethods { get; } = new List<PerformanceData>();

		[JsonProperty(Order = 5)]
		public Dictionary<string, string> Metadata { get; private set; } = new Dictionary<string, string>();

		public PerformanceData AddMetadata(string key, string value)
		{
			Metadata[key] = value;
			return this;
		}

		public bool ShouldSerializeSubMethods()
		{
			return SubMethods.Count > 0;
		}

		public bool ShouldSerializeMetadata()
		{
			return Metadata.Count > 0;
		}
	}
}
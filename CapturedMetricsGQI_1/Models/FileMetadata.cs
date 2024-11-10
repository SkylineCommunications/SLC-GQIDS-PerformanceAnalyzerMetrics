namespace Skyline.DataMiner.Utilities.PerformanceAnalyzerGQI.Models
{
	using System;

	public struct FileMetadata
	{
		public FileMetadata(string name, DateTime created, DateTime lastModified, long size)
		{
			Name = name;
			Created = created;
			LastModified = lastModified;
			Size = size;
		}

		public string Name { get; set; }

		public DateTime Created { get; set; }

		public DateTime LastModified { get; set; }

		public long Size { get; set; }
	}
}
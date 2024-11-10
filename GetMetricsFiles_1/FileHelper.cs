using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetMetricsFiles_1
{
	public class FileDetails
	{
		public string FileName { get; set; }


		public string Path { get; set; }

		public DateTime Created { get; set; }

		public DateTime LastModified { get; set; }

		public long Size { get; set; }

		public string Type { get; set; }

		public bool ReadOnly { get; set; }
	}

	public enum FilePropertyEnum
	{
		FileName,
		Path,
		Created,
		LastModified,
		Size,
		Type,
		ReadOnly,
	}

	public static class FilePropertyMappings
	{
		public static readonly Dictionary<string, FilePropertyEnum> PropertyNameToEnum = new Dictionary<string, FilePropertyEnum>
	{
		{ nameof(FileDetails.FileName), FilePropertyEnum.FileName },
		{ nameof(FileDetails.Path), FilePropertyEnum.Path },
		{ nameof(FileDetails.Created), FilePropertyEnum.Created },
		{ nameof(FileDetails.LastModified), FilePropertyEnum.LastModified },
		{ nameof(FileDetails.Size), FilePropertyEnum.Size },
		{ nameof(FileDetails.Type), FilePropertyEnum.Type },
		{ nameof(FileDetails.ReadOnly), FilePropertyEnum.ReadOnly },
	};
	}
}

namespace GetMetricsFiles_1
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Analytics.GenericInterface;

	public class GQIHelper
	{
		private List<GQIColumn> _gqiColumns;
		private List<GQIRow> _gqiRows;

		public void Initialize(IEnumerable<FileDetails> fileDetails)
		{
			var propertyNames = typeof(FileDetails).GetProperties().Select(p => p.Name);

			InitializeFileColumns(propertyNames);
			InitializeRows(fileDetails);
		}

		public GQIColumn[] GetColumns()
		{
			return _gqiColumns?.ToArray();
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			return new GQIPage(_gqiRows.ToArray()) { HasNextPage = false };
		}

		private void InitializeFileColumns(IEnumerable<string> propertyNames)
		{
			_gqiColumns = new List<GQIColumn>();

			foreach (var propertyName in propertyNames)
			{
				GQIColumn gqiColumn;
				if (FilePropertyMappings.PropertyNameToEnum.TryGetValue(propertyName, out var propertyEnum))
				{
					switch (propertyEnum)
					{
						case FilePropertyEnum.FileName:
						case FilePropertyEnum.Path:
						case FilePropertyEnum.Type:
							gqiColumn = new GQIStringColumn(propertyName);
							break;

						case FilePropertyEnum.Created:
						case FilePropertyEnum.LastModified:
							gqiColumn = new GQIDateTimeColumn(propertyName);
							break;

						case FilePropertyEnum.Size:
							gqiColumn = new GQIIntColumn(propertyName);
							break;

						case FilePropertyEnum.ReadOnly:
							gqiColumn = new GQIBooleanColumn(propertyName);
							break;

						default:
							gqiColumn = new GQIStringColumn(propertyName);
							break;
					}

					_gqiColumns.Add(gqiColumn);
				}
				else
				{
					gqiColumn = new GQIStringColumn(propertyName);
					_gqiColumns.Add(gqiColumn);
				}
			}
		}

		private void InitializeRows(IEnumerable<FileDetails> fileDetailsList)
		{
			_gqiRows = new List<GQIRow>();

			foreach (var file in fileDetailsList)
			{
				var gqiCells = new List<GQICell>
				{
					CreateGQICellForFile(nameof(FileDetails.FileName), file.FileName),
					CreateGQICellForFile(nameof(FileDetails.Path), file.Path),
					CreateGQICellForFile(nameof(FileDetails.Created), file.Created),
					CreateGQICellForFile(nameof(FileDetails.LastModified), file.LastModified),
					CreateGQICellForFile(nameof(FileDetails.Size), file.Size),
					CreateGQICellForFile(nameof(FileDetails.Type), file.Type),
					CreateGQICellForFile(nameof(FileDetails.ReadOnly), file.ReadOnly),
				};

				// Assuming we can use file.FileName as a unique index for simplicity, or another identifier if needed
				_gqiRows.Add(new GQIRow(file.FileName, gqiCells.ToArray()));
			}
		}

		private GQICell CreateGQICellForFile(string propertyName, object value)
		{
			string stringValue = value?.ToString() ?? string.Empty;

			object cellValue;
			try
			{
				cellValue = ConvertValueForFileProperty(propertyName, stringValue);
			}
			catch (Exception ex)
			{
				throw new Exception($"Unexpected error converting value '{stringValue}' for property '{propertyName}': {ex.Message}.");
			}

			string displayValue = cellValue is DateTime dt
				? dt.ToString("dd/MM/yyyy HH:mm:ss") // Example format
				: cellValue?.ToString() ?? string.Empty;

			return new GQICell { Value = cellValue, DisplayValue = displayValue };
		}

		private object ConvertValueForFileProperty(string propertyName, string value)
		{
			if (Enum.TryParse(propertyName, out FilePropertyEnum propertyEnum))
			{
				switch (propertyEnum)
				{
					case FilePropertyEnum.FileName:
					case FilePropertyEnum.Path:
					case FilePropertyEnum.Type:
						return value;

					case FilePropertyEnum.Created:
					case FilePropertyEnum.LastModified:
						// If the format worked previously with "dd/MM/yyyy", and "MM/dd/yyyy" is just an example, handle both formats
						DateTime dateTimeValue;
						if (DateTime.TryParseExact(value, "dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dateTimeValue) ||
							DateTime.TryParseExact(value, "MM/dd/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dateTimeValue))
						{
							return DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Utc);
						}
						throw new FormatException($"Unable to parse '{value}' as a valid date and time for property '{propertyName}'.");

					case FilePropertyEnum.Size:
						return Convert.ToInt32(value);

					case FilePropertyEnum.ReadOnly:
						return bool.Parse(value);

					default:
						return value;
				}
			}

			return value;
		}
	}
}
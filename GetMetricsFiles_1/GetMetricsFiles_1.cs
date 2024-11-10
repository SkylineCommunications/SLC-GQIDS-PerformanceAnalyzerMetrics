/*
****************************************************************************
*  Copyright (c) 2024,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENTS

dd/mm/2024	1.0.0.1		XXX, Skyline	Initial version
****************************************************************************
*/

namespace GetMetricsFiles_1
{
	using System;
	using System.Collections.Generic;
	using System.IO;

	using Skyline.DataMiner.Analytics.GenericInterface;

	[GQIMetaData(Name = "Get Metrics Files")]
	public class GetMetricsFiles : IGQIDataSource, IGQIInputArguments, IGQIOnInit
	{
		private GQIStringArgument _folderPathArgument = new GQIStringArgument("Folder Path") { IsRequired = true };
		private GQIHelper _dataHelper;
		private string folderPath;

		public GQIColumn[] GetColumns()
		{
			InitializeDataHelper();
			return _dataHelper.GetColumns();
		}

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { _folderPathArgument };
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			return _dataHelper.GetNextPage(args);
		}
		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			folderPath = args.GetArgumentValue(_folderPathArgument);
			return new OnArgumentsProcessedOutputArgs();
		}
		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			return new OnInitOutputArgs();
		}
		private void InitializeDataHelper()
		{
			try
			{
				if (_dataHelper == null)
				{
					_dataHelper = new GQIHelper();
					var fileDetails = FetchFileDetailsFromFolder(folderPath);

					_dataHelper.Initialize(fileDetails);
				}
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("An error occurred while initializing the data helper: " + ex.Message, ex);
			}
		}

		private IEnumerable<FileDetails> FetchFileDetailsFromFolder(string folderPath)
		{
			var files = Directory.GetFiles(folderPath);
			var fileDetailsList = new List<FileDetails>();

			foreach (var file in files)
			{
				var fileInfo = new FileInfo(file);
				fileDetailsList.Add(new FileDetails
				{
					FileName = fileInfo.Name,
					Path = fileInfo.FullName,
					Created = fileInfo.CreationTimeUtc,
					LastModified = fileInfo.LastWriteTimeUtc,
					Size = fileInfo.Length,
					Type = fileInfo.Extension,
					ReadOnly = fileInfo.IsReadOnly,
				});
			}

			return fileDetailsList;
		}
	}
}
﻿/*
 * Blob storage model.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace SensateIoT.API.Common.Data.Models
{
	public enum StorageType
	{
		FileSystem,
		DigitalOceanSpaces,
		FTP
	}

	public class Blob
	{
		[Required, JsonIgnore]
		public long Id { get; set; }
		[Required, StringLength(24, MinimumLength = 24)]
		public string SensorId { get; set; }
		[Required]
		public string FileName { get; set; }
		[Required, JsonIgnore]
		public string Path { get; set; }
		[Required, JsonIgnore]
		public StorageType StorageType { get; set; }
		[Required]
		public long FileSize { get; set; }
		[Required]
		public DateTime Timestamp { get; set; }
	}
}

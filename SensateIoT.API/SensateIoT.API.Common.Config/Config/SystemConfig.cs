﻿/*
 * Configuration file for the system config.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

namespace SensateIoT.API.Common.Config.Config
{
	public class SystemConfig
	{
		public int ProxyLevel { get; set; }
		public string MeasurementAuthProxyUrl { get; set; }
		public string MessageAuthProxyUrl { get; set; }
	}
}
/*
 * Statistics type definition.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

namespace SensateIoT.API.Common.Data.Enums
{
	public enum StatisticsType
	{
		HttpGet,
		HttpPost,
		Email,
		SMS,
		LiveData,
		MQTT,
		ControlMessage,
		MeasurementStorage,
		MessageStorage,
		MessageRouted
	}
}

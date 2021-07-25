/*
 * Object ID schema filter to improve Swagger documentation.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using JetBrains.Annotations;
using MongoDB.Bson;

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace SensateIoT.API.Common.ApiCore.Swagger
{
	[UsedImplicitly]
	public class ObjectIdSchemaFilter : ISchemaFilter
	{
		public void Apply(OpenApiSchema schema, SchemaFilterContext context)
		{
			if(context.Type != typeof(ObjectId)) {
				return;
			}

			schema.Type = "string";
			schema.Format = "24-digit hex string";
			schema.Example = new OpenApiString(ObjectId.Empty.ToString());
		}
	}
}

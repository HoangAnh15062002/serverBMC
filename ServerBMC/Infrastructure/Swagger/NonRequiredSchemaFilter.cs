using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ServerBMC.Infrastructure.Swagger;

public class NonRequiredSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties == null) return;

        var type = context.Type;
        
        // Với PagedRequest và các class có default values, bỏ required
        if (type.IsClass && !type.IsPrimitive && type != typeof(string))
        {
            foreach (var prop in type.GetProperties())
            {
                if (schema.Properties.TryGetValue(prop.Name, out var schemaProp))
                {
                    schemaProp.Required = new HashSet<string>();
                }
            }
        }
    }
}

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ServerBMC.Infrastructure.Swagger;

public class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var endpointMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var authorizeAttributes = endpointMetadata
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .ToList();

        if (authorizeAttributes.Count == 0)
            return;

        operation.Security ??= new List<OpenApiSecurityRequirement>();

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }
            ] = Array.Empty<string>()
        });
    }
}

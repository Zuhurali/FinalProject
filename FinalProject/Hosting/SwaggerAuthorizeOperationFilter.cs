using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FinalProject.Hosting;

/// <summary>
/// Adds bearer security only to actions protected by <see cref="AuthorizeAttribute"/>.
/// Anonymous actions stay unlocked. Role lists are described and do not change runtime checks.
/// </summary>
internal sealed class SwaggerAuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ApplyDeclaredResponses(operation, context);

        var method = context.MethodInfo;
        if (method.IsDefined(typeof(AllowAnonymousAttribute), inherit: true))
        {
            return;
        }

        var authorize = method.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Concat(method.DeclaringType?.GetCustomAttributes<AuthorizeAttribute>(inherit: true) ?? [])
            .ToArray();
        if (authorize.Length == 0)
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }] = []
        });

        var roles = authorize
            .Select(attribute => attribute.Roles)
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (roles.Length == 0)
        {
            return;
        }

        var roleText = "Required roles: " + string.Join("; ", roles);
        operation.Description = string.IsNullOrWhiteSpace(operation.Description)
            ? roleText
            : operation.Description.TrimEnd() + Environment.NewLine + roleText;
    }

    /// <summary>
    /// Aligns response schemas with the attributes on the action.
    /// A status declared as <see cref="void"/> has no body.
    /// Two declared types for one status are published as oneOf.
    /// An inferred 200 is removed when the action declares 204 and not 200.
    /// </summary>
    private static void ApplyDeclaredResponses(OpenApiOperation operation, OperationFilterContext context)
    {
        var attributes = context.MethodInfo
            .GetCustomAttributes<ProducesResponseTypeAttribute>(inherit: true)
            .ToArray();
        if (attributes.Length == 0)
        {
            return;
        }

        var declaredCodes = new HashSet<string>(
            attributes.Select(attribute => attribute.StatusCode.ToString(CultureInfo.InvariantCulture)));
        if (declaredCodes.Contains("204") && !declaredCodes.Contains("200"))
        {
            operation.Responses.Remove("200");
        }

        foreach (var group in attributes.GroupBy(attribute => attribute.StatusCode))
        {
            var key = group.Key.ToString(CultureInfo.InvariantCulture);
            if (!operation.Responses.TryGetValue(key, out var response))
            {
                continue;
            }

            var types = group
                .Select(attribute => attribute.Type)
                .Where(type => type != null && type != typeof(void))
                .Distinct()
                .ToArray();
            if (types.Length == 0)
            {
                response.Content?.Clear();
                continue;
            }

            if (types.Length < 2)
            {
                continue;
            }

            response.Content = new Dictionary<string, OpenApiMediaType>(StringComparer.Ordinal)
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        OneOf = types
                            .Select(type => context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository))
                            .ToList()
                    }
                }
            };
        }
    }
}

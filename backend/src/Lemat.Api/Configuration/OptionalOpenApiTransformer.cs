// @Role: Transformator schematu OpenAPI: Opcjonalne<T> staje się schematem T dopuszczającym null (ADR-072 p. 2), dzięki czemu klient TS dostaje pole?: T | null | @DependsOn: Opcjonalne<T>, Microsoft.AspNetCore.OpenApi, Microsoft.OpenApi | @UsedBy: ApiHost (AddOpenApi) | @Task: TASK-124
using Lemat.Api.Contracts.Common;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Lemat.Api.Configuration;

internal sealed class OpcjonalneSchematOpenApi : IOpenApiSchemaTransformer
{
    public async Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);
        var typ = context.JsonTypeInfo.Type;
        if (!typ.IsGenericType || typ.GetGenericTypeDefinition() != typeof(Opcjonalne<>))
        {
            return;
        }

        var argument = typ.GetGenericArguments()[0];
        var bazowy = Nullable.GetUnderlyingType(argument) ?? argument;
        var wewnetrzny = await context.GetOrCreateSchemaAsync(bazowy, null, cancellationToken);
        if (wewnetrzny is OpenApiSchema { Type: not null } pelny)
        {
            schema.Type = (pelny.Type ?? default) | JsonSchemaType.Null;
            schema.Format = pelny.Format;
            schema.Enum = pelny.Enum;
        }
        else
        {
            schema.OneOf = [new OpenApiSchema { Type = JsonSchemaType.Null }, wewnetrzny];
        }
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;

using LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;

namespace LastHour.BuildingBlocks.Infrastructure.StronglyTypedIds;

/// <summary>
/// Resolves a <see cref="StronglyTypedIdJsonConverter{TValue, TSelf}"/> for any strongly
/// typed identifier. Register one instance on <see cref="JsonSerializerOptions.Converters"/>
/// to serialize every strongly typed id in the application.
/// </summary>
public sealed class StronglyTypedIdJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) => GetTypeArguments(typeToConvert) is not null;

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        (Type valueType, Type idType) = GetTypeArguments(typeToConvert).GetValueOrDefault();
        Type converterType = typeof(StronglyTypedIdJsonConverter<,>).MakeGenericType(valueType, idType);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }

    private static (Type ValueType, Type IdType)? GetTypeArguments(Type type)
    {
        Type? contract = type.GetInterfaces().FirstOrDefault(interfaceType =>
            interfaceType.IsGenericType
            && interfaceType.GetGenericTypeDefinition() == typeof(IStronglyTypedId<,>)
            && interfaceType.GetGenericArguments()[1] == type);

        if (contract is null)
        {
            return null;
        }

        Type[] arguments = contract.GetGenericArguments();
        return (arguments[0], arguments[1]);
    }
}

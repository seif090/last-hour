using System.Text.Json;
using System.Text.Json.Serialization;

using LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;

namespace LastHour.BuildingBlocks.Infrastructure.StronglyTypedIds;

/// <summary>
/// Serializes a strongly typed identifier as its primitive value in string form.
/// </summary>
/// <typeparam name="TValue">The wrapped primitive value type.</typeparam>
/// <typeparam name="TSelf">The concrete strongly typed identifier type.</typeparam>
public sealed class StronglyTypedIdJsonConverter<TValue, TSelf> : JsonConverter<TSelf>
    where TValue : struct, IEquatable<TValue>, IParsable<TValue>
    where TSelf : StronglyTypedId<TValue, TSelf>, IStronglyTypedId<TValue, TSelf>
{
    /// <inheritdoc/>
    public override TSelf? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (StronglyTypedId<TValue, TSelf>.TryParse(value, out TSelf? result))
        {
            return result;
        }

        throw new JsonException($"The value '{value}' is not a valid {typeof(TSelf).Name}.");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, TSelf value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value.ToString());
}

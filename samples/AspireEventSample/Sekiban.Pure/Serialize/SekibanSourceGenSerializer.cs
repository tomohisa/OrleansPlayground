using System.Text.Json;
namespace Sekiban.Pure.Serialize;

public class SekibanSourceGenSerializer : ISekibanSerializer
{
    private readonly JsonSerializerOptions _serializerOptions;

    public SekibanSourceGenSerializer(JsonSerializerOptions? serializerOptions) =>
        _serializerOptions = serializerOptions ??
            new JsonSerializerOptions
                { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public string Serialize<T>(T json) => JsonSerializer.Serialize(json, _serializerOptions);

    public T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _serializerOptions);
}
public class SekibanReflectionSerializer : ISekibanSerializer
{
    private readonly JsonSerializerOptions _serializerOptions = new()
        { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public string Serialize<T>(T json) => JsonSerializer.Serialize(json, _serializerOptions);

    public T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _serializerOptions);
}
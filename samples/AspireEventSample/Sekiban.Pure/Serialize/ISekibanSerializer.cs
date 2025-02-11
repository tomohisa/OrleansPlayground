namespace Sekiban.Pure.Serialize;

public interface ISekibanSerializer
{
    string Serialize<T>(T json);
    T Deserialize<T>(string json);
}
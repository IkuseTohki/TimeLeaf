using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TimeLeaf.Repositories.FileSystem;

/// <summary>
/// JSON形式でプロジェクトデータをシリアライズ・デシリアライズするクラス。
/// </summary>
public class JsonProjectFileSystemSerializer : IProjectFileSystemSerializer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 日本語をエスケープせずに保存
        Converters = { new JsonStringEnumConverter() } // Enum を文字列で保存
    };

    public string Serialize<T>(T dto) where T : class
    {
        return JsonSerializer.Serialize(dto, _options);
    }

    public T? Deserialize<T>(string data) where T : class
    {
        return JsonSerializer.Deserialize<T>(data, _options);
    }
}

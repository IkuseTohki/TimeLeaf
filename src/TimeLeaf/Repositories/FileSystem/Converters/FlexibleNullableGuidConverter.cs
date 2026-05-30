using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TimeLeaf.Repositories.FileSystem.Converters;

/// <summary>
/// GUID 形式でない文字列が含まれていても例外を投げずに null を返す、寛容な Guid? コンバーター。
/// 過去のデータ（名前が入っている場合など）との互換性を維持するために使用します。
/// </summary>
public class FlexibleNullableGuidConverter : JsonConverter<Guid?>
{
    public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (string.IsNullOrEmpty(stringValue))
            {
                return null;
            }

            if (Guid.TryParse(stringValue, out var guid))
            {
                return guid;
            }

            // GUID としてパースできない場合は null を返す（例外を投げない）
            return null;
        }

        // それ以外のトークン（数値など）の場合は標準の挙動に任せるか null
        return null;
    }

    public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString());
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

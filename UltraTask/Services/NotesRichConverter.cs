using System.Text.Json;
using System.Text.Json.Serialization;
using UltraTask.Models;

namespace UltraTask.Services;

// Deserializa notes_rich em dois formatos:
//   - string pura (formato legado): "notes_rich": "<b>texto</b>"
//   - objeto normalizado:           "notes_rich": { "html": "<b>texto</b>" }
// Sempre serializa como objeto { "html": "..." }.
public class NotesRichConverter : JsonConverter<NotesRich?>
{
    public override NotesRich? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        // Formato legado: string pura
        if (reader.TokenType == JsonTokenType.String)
            return new NotesRich { Html = reader.GetString() ?? string.Empty };

        // Formato normalizado: objeto { "html": "..." }
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            string html = string.Empty;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName &&
                    reader.GetString()?.Equals("html", StringComparison.OrdinalIgnoreCase) == true)
                {
                    reader.Read();
                    html = reader.GetString() ?? string.Empty;
                }
                else
                {
                    reader.Skip();
                }
            }
            return new NotesRich { Html = html };
        }

        reader.Skip();
        return null;
    }

    public override void Write(Utf8JsonWriter writer, NotesRich? value, JsonSerializerOptions options)
    {
        if (value is null) { writer.WriteNullValue(); return; }
        writer.WriteStartObject();
        writer.WriteString("html", value.Html);
        writer.WriteEndObject();
    }
}

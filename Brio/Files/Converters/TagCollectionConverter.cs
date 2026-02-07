using Brio.Library.Tags;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Brio.Files.Converters;

public class TagCollectionConverter : JsonConverter<TagCollection>
{
    public override TagCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if(reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array for TagCollection");

        TagCollection tags = [];

        while(reader.Read())
        {
            if(reader.TokenType == JsonTokenType.EndArray)
                return tags;

            if(reader.TokenType == JsonTokenType.String)
            {
                string? name = reader.GetString();
                if(name != null)
                    tags.Add(name);
            }
        }

        throw new JsonException("Unexpected end of JSON while reading TagCollection");
    }

    public override void Write(Utf8JsonWriter writer, TagCollection value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach(Tag tag in value)
        {
            writer.WriteStringValue(tag.Name);
        }

        writer.WriteEndArray();
    }
}

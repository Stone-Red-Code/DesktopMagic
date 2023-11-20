using System;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DesktopMagic.Helpers;

public class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ColorTranslator.FromHtml(reader.GetString() ?? "#000000");
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue("#" + value.A.ToString("X2").ToLower() + value.R.ToString("X2") + value.G.ToString("X2") + value.B.ToString("X2").ToLower());
    }
}
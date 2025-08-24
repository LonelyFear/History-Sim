using System;
using Godot;
using MessagePack;
using MessagePack.Formatters;
public class ColorFormatter : IMessagePackFormatter<Color>
{
    public void Serialize(ref MessagePackWriter writer, Color value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(3); // store as [r, g, b]
        writer.WriteString(value.ToString().ToUtf8Buffer());
    }

    public Color Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        options.Security.DepthStep(ref reader);
        var count = reader.ReadArrayHeader();
        if (count != 2) throw new InvalidOperationException("Invalid Vector2I format");

        string color = reader.ReadString();
        reader.Depth--;
        return Color.FromString(color, new Color(0, 0, 0));
    }
}
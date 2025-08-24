using System;
using Godot;
using MessagePack;
using MessagePack.Formatters;
public class Vector2IFormatter : IMessagePackFormatter<Vector2I>
{
    public void Serialize(ref MessagePackWriter writer, Vector2I value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(2); // store as [x, y]
        writer.WriteInt32(value.X);
        writer.WriteInt32(value.Y);
    }

    public Vector2I Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        options.Security.DepthStep(ref reader);
        var count = reader.ReadArrayHeader();
        if (count != 2) throw new InvalidOperationException("Invalid Vector2I format");

        var x = reader.ReadInt32();
        var y = reader.ReadInt32();
        reader.Depth--;
        return new Vector2I(x, y);
    }
}
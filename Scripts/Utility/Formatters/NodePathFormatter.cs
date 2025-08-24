using System;
using Godot;
using MessagePack;
using MessagePack.Formatters;
public class NodePathFormatter : IMessagePackFormatter<NodePath>
{
    public void Serialize(ref MessagePackWriter writer, NodePath value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(3); // store as [r, g, b]
        writer.WriteString(value.ToString().ToUtf8Buffer());
    }

    public NodePath Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        options.Security.DepthStep(ref reader);
        var count = reader.ReadArrayHeader();
        if (count != 2) throw new InvalidOperationException("Invalid Vector2I format");

        string path= reader.ReadString();
        reader.Depth--;
        return new NodePath(path);
    }
}
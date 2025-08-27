using System;
using System.Linq;
using System.Xml.Schema;
using Godot;
using MessagePack;
using MessagePack.Formatters;
public class ColorFormatter : IMessagePackFormatter<Color>
{
    public void Serialize(ref MessagePackWriter writer, Color value, MessagePackSerializerOptions options)
    {
        options.Resolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.ToHtml(true), options);
    }

    public Color Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
            return new Color(0, 0, 0);
        
        string color = options.Resolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
        //GD.Print(color);
        return Color.FromHtml(color);
    }
}
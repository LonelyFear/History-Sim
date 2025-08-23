using System;

using System.Text.Json;
using System.Text.Json.Serialization;

public class TwoDimensionalArrayConverter<T> : JsonConverter<T[,]>
{
    public override T[,] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Expecting a jagged array in JSON
        var jagged = JsonSerializer.Deserialize<T[][]>(ref reader, options);
        int rows = jagged.Length;
        int cols = jagged[0].Length;
        var result = new T[rows, cols];

        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                result[i, j] = jagged[i][j];

        return result;
    }

    public override void Write(Utf8JsonWriter writer, T[,] value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        for (int i = 0; i < value.GetLength(0); i++)
        {
            writer.WriteStartArray();
            for (int j = 0; j < value.GetLength(1); j++)
                JsonSerializer.Serialize(writer, value[i, j], options);
            writer.WriteEndArray();
        }
        writer.WriteEndArray();
    }
}
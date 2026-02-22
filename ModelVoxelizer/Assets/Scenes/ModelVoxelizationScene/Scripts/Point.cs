using UnityEngine;

public class Point
{
    public Vector3 Position { get; }
    public Color Color { get; }

    public Point(Vector3 position, Color color)
    {
        Position = position;
        Color = color;
    }

    public string ToXyzRecord() =>
        string.Join(",",
            Position.x,
            Position.y,
            Position.z,
            (byte)(Color.r * 255),
            (byte)(Color.g * 255),
            (byte)(Color.b * 255)
        );
}

using Godot;
using Vector2 = System.Numerics.Vector2;
public partial class StreamlineRenderer : Node2D
{
	public WorldGenerator world;
    public override void _Draw()
    {
		if (world == null) return;
		Scale = new Godot.Vector2(1, 1) * 80f / world.WorldSize.X;
        for (int x = 0; x < world.WorldSize.X; x++)
        {
            for (int y = 0; y < world.WorldSize.Y; y++)
			{
				int sampleDist = 4;
				if (Mathf.PosMod(x, sampleDist) != 0 || Mathf.PosMod(y, sampleDist) != 0) continue;
				Vector2 wind = Vector2.Zero;
				for (int ax = 0; ax < sampleDist; ax++)
				{
					for (int ay = 0; ay < sampleDist; ay++)
					{
						wind += world.SummerWindVelMap[(x/sampleDist * sampleDist) + ax, (y/sampleDist * sampleDist) + ay];
					}
				}
				wind /= sampleDist * sampleDist;
				wind *= 5f;
				float scale = 16f;
				Vector2 pos = new((x + (sampleDist/2f)) * scale, (y + (sampleDist/2f)) * scale);
				//Vector2 wind = world.WindVelMap[x,y] * 3f;
				//DrawLine(new Godot.Vector2(pos.X, pos.Y), new(pos.X + wind.X, pos.Y + wind.Y), new Color(1,1,1));
			}
		}
    }
}

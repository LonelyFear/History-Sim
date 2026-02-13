using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class BorderRenderer : Node2D
{
	public SimManager simManager;
	[Export] public MapManager mapManager;
	public ObjectManager objectManager;
	MultiMeshInstance2D borderMultiMesh;
	Vector2I worldSize;
    public override void _Ready()
    {
		borderMultiMesh = GetNode<MultiMeshInstance2D>("BorderMultiMesh");
		GetNode<SimNodeManager>("/root/Game/Simulation").simStartEvent += Init;
	}
    void Init() {
        simManager = GetNode<SimNodeManager>("/root/Game/Simulation").simManager;
        objectManager = simManager.objectManager;
		InitMultiMesh();
    }
	void InitMultiMesh()
    {
        borderMultiMesh.Multimesh = new MultiMesh()
        {
            Mesh = CreateLineMesh(2),
			TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
			UseColors = true,
			InstanceCount = 0,
        };
    }
	QuadMesh CreateLineMesh(int thickness)
    {
		float mult = simManager.worldGenerator.WorldMult;
        QuadMesh lineMesh = new QuadMesh()
        {
			// thick 1: -0.8
			// thick 2: 0.15
            Size = new Vector2((SimManager.regionGlobalWidth - 0.8f + (0.95f * (thickness - 1)))/mult , thickness/mult),
        };
		StandardMaterial3D material = new StandardMaterial3D(){
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			VertexColorUseAsAlbedo = true,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };
		lineMesh.SurfaceSetMaterial(0, material);
		return lineMesh;
    }

	public void RedrawBorders()
    {
		/*
		List<(Region, Region)> drawnBorders = [];
		List<int> borderSegments = new List<int>();

        foreach (Region region in simManager.habitableRegions)
        {
			if (region.pops.Count < 1) continue;
            int x = region.pos.X;
			int y = region.pos.Y;
			foreach (var pair in region.borderingRegionIds)
            {
                Direction direction = pair.Key;
				Region border = objectManager.GetRegion(pair.Value);

				if (drawnBorders.Contains((region,border)) || drawnBorders.Contains((border,region))){
					continue;
				} 

				switch (mapManager.mapMode)
                {
                    case MapModes.REALM:
						if ((region.owner == null && border.pops.Count < 1) || region.owner != null && (border.owner == null || border.owner.vassalManager.GetOverlord(true) != region.owner.vassalManager.GetOverlord(true)))
                        {
							borderSegments.Add(x);
							borderSegments.Add(y);							
                            borderSegments.Add((int)direction);
							drawnBorders.Add((region, border));
                        }
						break;
                    case MapModes.POLITIY:
						if ((region.owner == null && border.pops.Count < 1) || region.owner != null && (border.owner == null || border.owner != region.owner))
                        {
							borderSegments.Add(x);
							borderSegments.Add(y);							
                            borderSegments.Add((int)direction);
							drawnBorders.Add((region, border));
                        }
						break;
                }
            }
        }
		BuildBorderMesh(borderMultiMesh.Multimesh, borderSegments, new Color(0,0,0,1));
		*/
    }
	void BuildBorderMesh(MultiMesh multiMesh, List<int> borderSegments, Color color)
    {
        int segmentCount = borderSegments.Count / 3;
		multiMesh.InstanceCount = segmentCount;
		Parallel.For(0, segmentCount, (i) =>
        {
            int idx = i * 3;
			int xPos = borderSegments[idx];
			int yPos = borderSegments[idx + 1];
			Direction direction = (Direction)borderSegments[idx + 2];
			float rotationRad = 0;
			Vector2 offset = new Vector2();
			switch (direction)
            {
                case Direction.UP:
					rotationRad = 0;
					offset = new Vector2(0.5f, 0);
					break;

                case Direction.DOWN:
					rotationRad = 0;
					offset = new Vector2(0.5f, 1);
					break;
				
                case Direction.RIGHT:
					rotationRad = Mathf.Pi/2;
					offset = new Vector2(1, 0.5f);
					break;
                case Direction.LEFT:
					rotationRad = Mathf.Pi/2;
					offset = new Vector2(0, 0.5f);
					break;
            }
			//Transform2D borderTransform = new(rotationRad, simManager.RegionToGlobalPos(new Vector2(xPos + offset.X, yPos + offset.Y)));
			//multiMesh.CallDeferred("set_instance_transform_2d", [i, borderTransform]);
			multiMesh.CallDeferred("set_instance_color", [i, color]);            
        });
		for (int i = 0; i < segmentCount; i++)
        {
			//GD.Print("Segment!");

        }
    }

}
public enum Direction{
    UP = 0,
	RIGHT = 1,
	DOWN = 2,
	LEFT = 3
}

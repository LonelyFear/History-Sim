using System.Collections.Generic;
using System.Linq;
using Godot;

public class OceanGenerator
{
    public SimManager simManager;

    public OceanGenerator(SimManager sim)
    {
        simManager = sim;
    }

    public void GenerateOceans()
    {
        HashSet<Region> unassignedRegions = [..simManager.regionIds.Values.Where(r => r.isWater)];

        while (unassignedRegions.Count > 0)
        {
            Ocean ocean = new();

            Region seed = unassignedRegions.FirstOrDefault();

            unassignedRegions.Remove(seed);
            Queue<Region> frontier = new();
            frontier.Enqueue(seed);
            List<Region> oceanRegions = [seed];

            while (frontier.Count > 0)
            {
                Region region = frontier.Dequeue();
                foreach (Region border in region.borderingRegions)
                {
                    if (border.isWater && border.ocean == null)
                    {
                        border.ocean = ocean;
                        oceanRegions.Add(border);
                        frontier.Enqueue(border);
                        unassignedRegions.Remove(border);
                    }
                }
            }
            ObjectManager.CreateOcean([..oceanRegions]);
        }
    }
}
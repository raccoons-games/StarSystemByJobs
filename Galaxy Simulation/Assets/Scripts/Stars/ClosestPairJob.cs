using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
public struct ClosestPairJob : IJob
{
    [ReadOnly]
    public NativeArray<float3> positions;
    
    [WriteOnly]
    public NativeArray<int> star1;

    [WriteOnly]
    public NativeArray<int> star2;

    public NativeArray<float> distance;

    public void Execute()
    {
        star1[0] = -1;
        star2[0] = -1;
        distance[0] = float.MaxValue;
        for (int i = 1; i<positions.Length; i++)
        {
            for (int j = 0; j<i; j++)
            {
                float currentDistance = math.distance(positions[i], positions[j]);
                if (currentDistance < distance[0])
                {
                    distance[0] = currentDistance;
                    star1[0] = i;
                    star2[0] = j;
                }
            }
        }
    }
}
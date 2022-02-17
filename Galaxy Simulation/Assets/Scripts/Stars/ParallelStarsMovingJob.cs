using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine.Jobs;

[BurstCompile]
public struct ParallelStarsMovingJob : IJobParallelForTransform
{
    [NativeDisableParallelForRestriction]
    [ReadOnly] 
    public NativeArray<float> mass;

    public NativeArray<float3> velocity;

    [NativeDisableParallelForRestriction]
    public NativeArray<float3> position;

    [NativeDisableParallelForRestriction] 
    [ReadOnly] 
    public NativeArray<float> deltaTime;

    public void CalculateVelocity(int index)
    {
        for (int i = 0; i < index; i++)
        {
            ApplyVelocityForStarFrom(index, i);
        }
        for (int i = index+1; i < mass.Length; i++)
        {
            ApplyVelocityForStarFrom(index, i);
        }
    }

    public void Execute(int index, TransformAccess transform)
    {
        CalculateVelocity(index);
        float3 moving = velocity[index] * deltaTime[0];
        position[index] = position[index] + moving;
        transform.position = position[index];
    }

    private void ApplyVelocityForStarFrom(int starIndex, int oppositeStarId)
    {
        float3 starDir = position[oppositeStarId] - position[starIndex];
        float distance = math.length(starDir);
        float acceleration = mass[oppositeStarId] / (distance*distance) * deltaTime[0];
        velocity[starIndex] = velocity[starIndex] + acceleration * math.normalize(starDir);
    }
}

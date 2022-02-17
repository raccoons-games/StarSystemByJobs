using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using System;
using System.Threading.Tasks;

namespace Stars
{
    public class StarSystem:IDisposable
    {
        private NativeArray<float> mass;
        private NativeArray<float3> position;
        private NativeArray<float3> velocity;
        private NativeArray<float> deltaTime;
        private TransformAccessArray transforms;

        private ParallelStarsMovingJob parallelStarsMovingJob;
        private JobHandle currentJobHandle;

        public StarSystem(Star[] stars)
        {
            int size = stars.Length;
            mass = new NativeArray<float>(size, Allocator.Persistent);
            position = new NativeArray<float3>(size, Allocator.Persistent);
            velocity = new NativeArray<float3>(size, Allocator.Persistent);
            deltaTime = new NativeArray<float>(1, Allocator.Persistent);
            Transform[] starTransforms = new Transform[size];
            for (int starId = 0; starId<size; starId++)
            {
                var star = stars[starId];
                mass[starId] = star.Mass;
                position[starId] = star.transform.position;
                velocity[starId] = star.Velocity;
                starTransforms[starId] = star.transform;
            }
            transforms = new TransformAccessArray(starTransforms);
            parallelStarsMovingJob = new ParallelStarsMovingJob();
            parallelStarsMovingJob.deltaTime = deltaTime;
            parallelStarsMovingJob.mass = mass;
            parallelStarsMovingJob.position = position;
            parallelStarsMovingJob.velocity = velocity;
        }

        public void Dispose()
        {
            mass.Dispose();
            position.Dispose();
            velocity.Dispose();
            deltaTime.Dispose();
            transforms.Dispose();
        }

        public void Update(float deltaTime)
        {
            this.deltaTime[0] = deltaTime;
            currentJobHandle = parallelStarsMovingJob.Schedule(transforms, currentJobHandle);
            currentJobHandle.Complete();
        }

        public async Task<(int star1, int star2, float distance)> GetClosestPair()
        {
            ClosestPairJob closestPairJob = new ClosestPairJob();
            closestPairJob.positions = position;
            closestPairJob.star1 = new NativeArray<int>(1, Allocator.TempJob);
            closestPairJob.star2 = new NativeArray<int>(1, Allocator.TempJob);
            closestPairJob.distance = new NativeArray<float>(1, Allocator.TempJob);
            currentJobHandle = closestPairJob.Schedule(currentJobHandle);
            while (this!=null && currentJobHandle.IsCompleted==false)
            {
                await Task.Yield();
            }
            var result = (closestPairJob.star1[0], closestPairJob.star2[0], closestPairJob.distance[0]);
            closestPairJob.star1.Dispose();
            closestPairJob.star2.Dispose();
            closestPairJob.distance.Dispose();
            return result;
        }
    }
}

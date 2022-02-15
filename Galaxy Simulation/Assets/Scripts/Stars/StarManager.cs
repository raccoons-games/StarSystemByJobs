using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;
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

    public class StarManager : MonoBehaviour
    {
        [SerializeField]
        private int starCount = 100;

        [SerializeField]
        private Star starOriginal;

        [SerializeField]
        private float spawnRadius = 100;

        [SerializeField]
        private float spawnHeight = 5;

        [SerializeField]
        private float simulationSpeed = 1;

        [SerializeField]
        private Vector2 massRange = new Vector2(0.1f, 1);

        [SerializeField]
        private float maxStartVelocity = 2;

        [SerializeField]
        private float blackHoleMass = 10;

        [SerializeField]
        private AnimationCurve curve;

        [SerializeField]
        private bool useJobs = true;

        private Star[] stars;

        private StarSystem starSystem;

        [SerializeField]
        private int selectedStar;
        private Vector3 previousSelectedStarPosition;

        private void Awake()
        {
            stars = new Star[starCount];
            CreateStar(0, blackHoleMass,new Vector3(5f,5f,5f), Color.white, true);
            for (int i=1; i<starCount; i++)
            {
                float mass = Random.Range(massRange.x, massRange.y);
                Vector3 colorVector = Random.onUnitSphere;
                colorVector = Vector3.Lerp(colorVector, Vector3.one, 0.9f);
                Vector3 position = Random.onUnitSphere * curve.Evaluate(Random.value);
                position.x *= spawnRadius;
                position.z *= spawnRadius;
                position.y *= spawnHeight;
                CreateStar(i, mass,position, new Color(Mathf.Abs(colorVector.x),Mathf.Abs(colorVector.y),Mathf.Abs(colorVector.z)),true);
            }
            starSystem = new StarSystem(stars);
        }

        private void Start()
        {
            //CheckClosestStars();    
        }

        public async void CheckClosestStars()
        {
            while (true)
            {
                var closestPair = await starSystem.GetClosestPair();
                if (closestPair.star1 >= 0 && closestPair.star2 >= 0)
                {
                    Debug.DrawLine(stars[closestPair.star1].transform.position, stars[closestPair.star2].transform.position, Color.red, 1);
                }
                await Task.Delay(1000);
            }
        }

        private void CreateStar(int i, float mass, Vector3 position, Color color, bool applyStartVelocity=true)
        {
            stars[i] = Instantiate(starOriginal);
            
            stars[i].transform.position = position;
            stars[i].Mass = mass;
            stars[i].transform.localScale = stars[i].Mass * stars[i].transform.localScale;
            if (applyStartVelocity)
            {
                Vector3 velocity = Random.insideUnitSphere * maxStartVelocity;
                velocity.y = 0;
                stars[i].Velocity = velocity;
            }
            stars[i].Color = color;
        }

        private void Update()
        {
            var simulationDeltaTime = Time.deltaTime * simulationSpeed;

            if (useJobs)
            {
                starSystem.Update(simulationDeltaTime);
            }
            else
            {
                for (int i = 0; i < starCount; i++)
                {
                    for (int j = 0; j < starCount; j++)
                    {
                        if (i == j) continue;
                        Vector3 starDir = stars[j].transform.position - stars[i].transform.position;
                        float distance = starDir.magnitude;
                        float acceleration = stars[j].Mass / Mathf.Pow(distance, 2) * Time.deltaTime * simulationSpeed;
                        stars[i].Velocity += acceleration * starDir.normalized;
                    }
                }
                for (int i = 0; i < starCount; i++)
                {
                    stars[i].transform.position += stars[i].Velocity * simulationDeltaTime;
                }

            }
            //Debug.DrawLine(stars[selectedStar].transform.position, previousSelectedStarPosition, Color.green, 3);
            //previousSelectedStarPosition = stars[selectedStar].transform.position;
        }

        private void OnDestroy()
        {
            starSystem.Dispose();
        }
    }
}

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
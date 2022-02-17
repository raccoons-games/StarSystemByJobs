using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Threading.Tasks;

namespace Stars
{

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
        }

        private void OnDestroy()
        {
            starSystem.Dispose();
        }
    }
}

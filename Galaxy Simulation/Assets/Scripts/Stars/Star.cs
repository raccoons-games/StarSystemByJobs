using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Stars
{
    public class Star : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer spriteRenderer;

        public Color Color
        {
            get
            {
                return spriteRenderer.color;
            }
            set
            {
                spriteRenderer.color = value;
            }
        }

        public Vector3 Velocity { get; set; }
        public float Mass { get; set; } = 1;

    }
}
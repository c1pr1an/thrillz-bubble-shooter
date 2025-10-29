using UnityEngine;

namespace Brain.Gameplay
{
    /// <summary>
    /// Component that marks a ball as a Bomb bonus ball.
    /// Bomb balls destroy all balls within 2 grid positions on impact.
    /// </summary>
    [RequireComponent(typeof(Ball))]
    public class BombBall : MonoBehaviour
    {
        private Ball _ball;

        [Header("Bomb Settings")]
        [SerializeField] private int _explosionRadius = 2; // Grid positions


        private void Awake()
        {
            _ball = GetComponent<Ball>();
        }

        public bool IsBomb()
        {
            return enabled && _ball != null;
        }

        public int GetExplosionRadius()
        {
            return _explosionRadius;
        }
    }
}
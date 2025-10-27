using UnityEngine;

namespace Brain.Gameplay
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class PhantomBall : MonoBehaviour
    {
        private Ball _linkedBall;
        private Vector2Int _gridPosition;
        private CircleCollider2D _collider;

        public Ball LinkedBall => _linkedBall;
        public Vector2Int GridPosition => _gridPosition;
        public CircleCollider2D Collider => _collider;

        public void Initialize(Ball linkedBall, Vector2Int gridPos, CircleCollider2D collider)
        {
            _linkedBall = linkedBall;
            _gridPosition = gridPos;
            _collider = collider;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
            Gizmos.DrawSphere(transform.position, 0.35f);

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.35f);

            if (_linkedBall != null)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
                Gizmos.DrawLine(transform.position, _linkedBall.transform.position);
            }
        }
#endif
    }
}

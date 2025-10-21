using System.Collections.Generic;
using Brain.Gameplay;
using Brain.Util;
using UnityEngine;

namespace Brain.Managers
{
    public class PhantomBallManager : UnitySingleton<PhantomBallManager>
    {
        private Dictionary<Ball, PhantomBall> _ballToPhantom = new Dictionary<Ball, PhantomBall>();
        private static readonly Dictionary<Collider2D, PhantomBall> s_colliderToPhantom = new();

        private GridManager GridManager => GridManager.Instance;

        public static bool IsPhantomCollider(Collider2D collider)
        {
            return collider != null && s_colliderToPhantom.ContainsKey(collider);
        }

        public void InitializePhantoms()
        {
            ClearAllPhantoms();

            for (int row = 0; row < GridManager.MaxRows; row++)
            {
                int maxCols = GridUtils.GetMaxColumns(row);

                Ball leftBall = GridManager.GetBall(0, row);
                if (leftBall != null && leftBall.HasFlag(BallFlags.Pinned))
                {
                    SpawnPhantom(leftBall, isLeftEdge: true);
                }

                Ball rightBall = GridManager.GetBall(maxCols - 1, row);
                if (rightBall != null && rightBall.HasFlag(BallFlags.Pinned))
                {
                    SpawnPhantom(rightBall, isLeftEdge: false);
                }
            }

            Debug.Log($"PhantomBallManager: Initialized {_ballToPhantom.Count} phantoms");
        }

        public void OnBallAddedToGrid(Ball ball)
        {
            if (ball == null || !ball.HasFlag(BallFlags.Pinned))
                return;

            Vector2Int pos = ball.Position;
            int maxCols = GridUtils.GetMaxColumns(pos.y);

            if (pos.x == 0)
            {
                SpawnPhantom(ball, isLeftEdge: true);
            }
            else if (pos.x == maxCols - 1)
            {
                SpawnPhantom(ball, isLeftEdge: false);
            }
        }

        public void OnBallRemovedFromGrid(Ball ball)
        {
            if (ball == null)
                return;

            DestroyPhantom(ball);
        }

        private void SpawnPhantom(Ball edgeBall, bool isLeftEdge)
        {
            if (edgeBall == null) return;
            if (_ballToPhantom.ContainsKey(edgeBall)) return;

            Vector2Int edgePos = edgeBall.Position;
            int maxCols = GridUtils.GetMaxColumns(edgePos.y);

            Vector2Int phantomGridPos = isLeftEdge
                ? new Vector2Int(-1, edgePos.y)
                : new Vector2Int(maxCols, edgePos.y);

            float offset = isLeftEdge ? -GridManager.BallWidth : GridManager.BallWidth;
            Vector3 phantomWorldPos = edgeBall.transform.position + new Vector3(offset, 0, 0);

            GameObject phantomObj = new GameObject($"Phantom_{edgePos.y}_{(isLeftEdge ? "L" : "R")}");
            phantomObj.transform.position = phantomWorldPos;
            phantomObj.transform.SetParent(transform);
            phantomObj.layer = LayerMask.NameToLayer("Default");

            CircleCollider2D collider = phantomObj.AddComponent<CircleCollider2D>();
            collider.radius = 0.35f;

            PhantomBall phantom = phantomObj.AddComponent<PhantomBall>();
            phantom.Initialize(edgeBall, phantomGridPos, collider);

            _ballToPhantom[edgeBall] = phantom;
            s_colliderToPhantom[collider] = phantom;

            edgeBall.OnDestroyed += OnEdgeBallDestroyed;
        }

        private void DestroyPhantom(Ball edgeBall)
        {
            if (edgeBall == null) return;

            if (_ballToPhantom.TryGetValue(edgeBall, out PhantomBall phantom))
            {
                if (phantom != null)
                {
                    if (phantom.Collider != null)
                    {
                        s_colliderToPhantom.Remove(phantom.Collider);
                    }

                    Destroy(phantom.gameObject);
                }

                _ballToPhantom.Remove(edgeBall);
                edgeBall.OnDestroyed -= OnEdgeBallDestroyed;
            }
        }

        private void OnEdgeBallDestroyed(Ball ball)
        {
            DestroyPhantom(ball);
        }

        public void ClearAllPhantoms()
        {
            foreach (var kvp in _ballToPhantom)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.OnDestroyed -= OnEdgeBallDestroyed;
                }
            }

            foreach (var kvp in _ballToPhantom)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }

            _ballToPhantom.Clear();
            s_colliderToPhantom.Clear();
        }

        private void OnDisable()
        {
            ClearAllPhantoms();
        }
    }
}

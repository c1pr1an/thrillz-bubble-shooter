using UnityEngine;
using Brain.Util;
using Brain.Gameplay;

namespace Brain.Managers
{
    public class BonusBallPreview : UnitySingleton<BonusBallPreview>
    {
        [SerializeField] private GameObject _model;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private SpriteRenderer _highlightSpriteRenderer;
        private bool _isShowingPreview = false;

        private void OnEnable()
        {
            // Subscribe to trajectory events
            if (TrajectoryPredictor.Instance != null)
            {
                TrajectoryPredictor.Instance.OnTrajectoryUpdated += OnTrajectoryUpdated;
                TrajectoryPredictor.Instance.OnTrajectoryHidden += OnTrajectoryHidden;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from trajectory events
            if (TrajectoryPredictor.Instance != null)
            {
                TrajectoryPredictor.Instance.OnTrajectoryUpdated -= OnTrajectoryUpdated;
                TrajectoryPredictor.Instance.OnTrajectoryHidden -= OnTrajectoryHidden;
            }
        }

        private void OnTrajectoryUpdated(Vector2 impactPosition, Vector2 direction, Ball ball)
        {
            ShowPreview(ball, impactPosition, direction);
        }

        private void OnTrajectoryHidden()
        {
            HidePreview();
        }

        public void ShowPreview(Ball ball, Vector2 impactPosition, Vector2 trajectoryDirection)
        {
            if (ball == null || !ball.IsBonusBall)
            {
                HidePreview();
                return;
            }

            _spriteRenderer.sprite = ball.SpriteRenderer.sprite;
            _highlightSpriteRenderer.sprite = ball.HighlightSprite.sprite;

            Vector3 previewPosition;
            if (ball.IsRocket())
            {
                previewPosition = impactPosition;
                float angle = Mathf.Atan2(trajectoryDirection.y, trajectoryDirection.x) * Mathf.Rad2Deg - 90f;
                _model.transform.rotation = Quaternion.Euler(0, 0, angle + 44f); // 44f is an additional offset to align the sprite correctly
            }
            else
            {
                previewPosition = GridManager.Instance.GetGridSnapPosition(impactPosition);
                _model.transform.rotation = Quaternion.identity;
            }

            transform.position = previewPosition;

            if (_isShowingPreview == false)
            {
                _model.SetActive(true);
                _isShowingPreview = true;
            }
        }

        public void HidePreview()
        {
            if (_isShowingPreview == false)
                return;

            _model.SetActive(false);
            _isShowingPreview = false;
        }

        public bool IsShowingPreview => _isShowingPreview;
    }
}
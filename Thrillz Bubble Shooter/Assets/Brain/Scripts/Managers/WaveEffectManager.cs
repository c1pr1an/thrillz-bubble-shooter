using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Brain.Util;
using Brain.Gameplay;

namespace Brain.Managers
{
    public class WaveEffectManager : UnitySingleton<WaveEffectManager>
    {
        // Private Fields
        [Header("Wave Effect Settings")]
        [SerializeField] private float _waveAmplitude = 0.2f;
        [SerializeField] private float _waveDuration = 0.15f;
        [SerializeField] private float _delayBetweenLayers = 0.02f;
        [SerializeField] private int _maxWaveLayers = 3;
        [SerializeField] private float _amplitudeAttenuation = 0.05f;
        [SerializeField] private Ease _waveEase = Ease.OutElastic;

        public void TriggerWaveEffect(Ball centerBall)
        {
            if (centerBall == null) return;
            StartCoroutine(PropagateWave(centerBall));
        }

        private IEnumerator PropagateWave(Ball centerBall)
        {
            Vector3 waveOrigin = centerBall.transform.position;
            HashSet<Ball> processedBalls = new HashSet<Ball>();
            List<Ball> currentLayer = new List<Ball>();
            List<Ball> nextLayer = new List<Ball>();

            // Add center ball to processed set
            processedBalls.Add(centerBall);

            // Start with immediate neighbors
            for (int i = 0; i < 6; i++)
            {
                Ball neighbor = centerBall.Neighbors[i];
                if (neighbor != null && !processedBalls.Contains(neighbor))
                {
                    currentLayer.Add(neighbor);
                    processedBalls.Add(neighbor);
                }
            }

            float currentAmplitude = _waveAmplitude;

            // Process each layer
            for (int layer = 0; layer < _maxWaveLayers; layer++)
            {
                if (currentLayer.Count == 0 || currentAmplitude <= 0) break;

                // Animate all balls in current layer
                foreach (Ball ball in currentLayer)
                {
                    if (ball != null && !ball.HasFlag(BallFlags.AnimatingWave))
                    {
                        Vector3 direction = (ball.transform.position - waveOrigin).normalized;
                        ball.PlayWaveAnimation(direction, currentAmplitude, _waveDuration, _waveEase);

                        // Find neighbors for next layer
                        for (int i = 0; i < 6; i++)
                        {
                            Ball neighbor = ball.Neighbors[i];
                            if (neighbor != null && !processedBalls.Contains(neighbor))
                            {
                                nextLayer.Add(neighbor);
                                processedBalls.Add(neighbor);
                            }
                        }
                    }
                }

                // Wait before starting next layer
                yield return new WaitForSeconds(_delayBetweenLayers);

                // Prepare for next layer
                currentLayer.Clear();
                currentLayer.AddRange(nextLayer);
                nextLayer.Clear();

                // Reduce amplitude for next layer
                currentAmplitude -= _amplitudeAttenuation;
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Brain
{
    public class BallExplosionVFX : MonoBehaviour
    {
        private List<ParticleSystem> _particleSystems;
        void Init()
        {
            _particleSystems = new List<ParticleSystem>();
            foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>())
            {
                _particleSystems.Add(ps);
            }
        }

        public void SetColor(Color color)
        {
            if (_particleSystems == null || _particleSystems.Count == 0) Init();

            foreach (ParticleSystem ps in _particleSystems)
            {
                var main = ps.main;
                main.startColor = new ParticleSystem.MinMaxGradient(color);
            }
        }
    }
}

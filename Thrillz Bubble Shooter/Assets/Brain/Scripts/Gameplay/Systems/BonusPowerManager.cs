using System;
using UnityEngine;
using Brain.Util;

namespace Brain.Gameplay
{
    /// <summary>
    /// Manages bonus ball power accumulation and activation
    /// </summary>
    public class BonusPowerManager : UnitySingleton<BonusPowerManager>
    {
        [Header("Power Settings")]
        [SerializeField] private float _powerPerBall = 0.05f; // 20 balls to fill
        [SerializeField] private float _maxPower = 1.0f;

        [Header("Current State")]
        [SerializeField] private float _currentPower = 0f;
        [SerializeField] private bool _isBonusReady = false;
        [SerializeField] private bool _isBonusActive = false;

        // Events
        public static event Action<float> OnPowerChanged;
        public static event Action OnBonusReady;
        public static event Action OnBonusUsed;

        /// <summary>
        /// Current power level (0-1)
        /// </summary>
        public float CurrentPower => _currentPower;

        /// <summary>
        /// Is bonus ball ready to use
        /// </summary>
        public bool IsBonusReady => _isBonusReady;

        /// <summary>
        /// Is bonus ball currently active
        /// </summary>
        public bool IsBonusActive => _isBonusActive;

        /// <summary>
        /// Add power from destroyed balls
        /// </summary>
        public void AddPower(int ballsDestroyed)
        {
            if (_isBonusReady || _isBonusActive)
                return;

            _currentPower = Mathf.Min(_currentPower + (ballsDestroyed * _powerPerBall), _maxPower);
            OnPowerChanged?.Invoke(_currentPower / _maxPower);

            if (_currentPower >= _maxPower && !_isBonusReady)
            {
                _isBonusReady = true;
                OnBonusReady?.Invoke();
                Debug.Log("[BonusPowerManager] Bonus ball ready!");
            }
        }

        public void UsedBonus()
        {
            _currentPower = 0f;
            _isBonusReady = false;
            _isBonusActive = false;

            OnPowerChanged?.Invoke(0f);
            OnBonusUsed?.Invoke();
            Debug.Log("[BonusPowerManager] Bonus ball used, power reset.");
        }

        public BonusBallType GetRandomBonusType()
        {
            // For testing: return Lightning type
            return BonusBallType.Lightning;

            // Future implementation:
            // var types = new[] { BonusBallType.Rainbow, BonusBallType.Bomb, BonusBallType.Lightning };
            // return types[UnityEngine.Random.Range(0, types.Length)];
        }

        /// <summary>
        /// Reset power system
        /// </summary>
        public void ResetPower()
        {
            _currentPower = 0f;
            _isBonusReady = false;
            _isBonusActive = false;
            OnPowerChanged?.Invoke(0f);
        }

        protected override void Awake()
        {
            base.Awake();
            ResetPower();
        }

        private void OnDestroy()
        {
            OnPowerChanged = null;
            OnBonusReady = null;
            OnBonusUsed = null;
        }
    }
}
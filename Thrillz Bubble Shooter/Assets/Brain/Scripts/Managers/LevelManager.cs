using Brain.Gameplay;
using Brain.Util;
using UnityEngine.Events;

namespace Brain.Managers
{
    public class LevelManager : UnitySingleton<LevelManager>
    {
        public event UnityAction<bool> OnLevelEnded;

        public int CurrentLevelIndex;
        public Level LevelInstance;
        public bool LevelCompleted = false;

        public bool IsTutorial
        {
            get { return CurrentLevelIndex == 0; }
        }

        private int _actualLevelIndex;

        public void Init()
        {
            LoadCurrentLevel();
            LevelInstance.OnLevelCompleted += HandleLevelEnded;
        }

        public void SetCurrentLevelIndex(int levelIndex)
        {
            CurrentLevelIndex = levelIndex;
        }

        public void LoadCurrentLevel()
        {
            _actualLevelIndex = CurrentLevelIndex;

            LevelInstance.Init();
        }

        private void HandleLevelEnded(bool levelResult)
        {
            if (OnLevelEnded != null)
            {
                OnLevelEnded(levelResult);
            }
        }
    }
}
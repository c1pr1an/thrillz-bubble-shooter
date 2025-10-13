using System;
using UnityEngine;

namespace Brain.Core
{
    [CreateAssetMenu(fileName = "New LevelData", menuName = "Level Data", order = 51)]
    public class LevelData : ScriptableObject
    {
        public int seedMultiplier = 1;
    }
}
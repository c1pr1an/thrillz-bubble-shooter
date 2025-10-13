using System.Collections;
using System.Collections.Generic;
using Brain.Util;
using UnityEngine;

namespace Brain.Audio
{
    public class SoundResources : UnitySingleton<SoundResources>
    {

        [HideInInspector]
        public List<SoundData> Sounds = new List<SoundData>();
        protected override void Awake()
        {
            base.Awake();

            SoundResourceGroup[] soundResourceGroup = GetComponentsInChildren<SoundResourceGroup>();
            for (int i = 0; i < soundResourceGroup.Length; i++)
            {
                Sounds.AddRange(soundResourceGroup[i].Sounds);
            }
        }

        public AudioClip GetClip(SoundType soundType)
        {
            SoundData result = Sounds.Find(sound => sound.SoundType == soundType);

            if (result != null)
                return result.Clip;

            Debug.LogWarningFormat("SoundResources : No Sound Resource For [{1}]", soundType.ToString());
            return null;
        }
    }
}

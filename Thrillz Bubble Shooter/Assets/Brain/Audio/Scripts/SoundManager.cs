using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Brain.Util;
using UnityEngine;

namespace Brain.Audio
{
    public class SoundManager : UnitySingleton<SoundManager>
    {
        private bool _sfxOn;
        public bool SfxOn
        {
            get { return _sfxOn; }
            set
            {
                _sfxOn = value;
            }
        }

        private bool _musicOn;
        public bool MusicOn
        {
            get { return _musicOn; }
            set
            {
                _musicOn = value;
                ToggleMusic(_musicOn);
            }
        }

        [SerializeField] private AudioSource[] _sfxOneShotSources;
        [SerializeField] private AudioSource _sfxLoopMusicSource;

        public void PlaySfxOneShot(SoundType soundType)
        {
            PlaySfxOneShot(soundType, 1f);
        }

        public void PlaySfxOneShot(SoundType soundType, float pitch)
        {
            AudioClip clip = SoundResources.Instance.GetClip(soundType);

            if (clip == null)
                return;

            if (_sfxOneShotSources.Length > 0)
            {
                AudioSource availableSource = _sfxOneShotSources[0];
                if (pitch != 1f)
                {
                    availableSource = GetAvailableAudioSource(pitch);
                    availableSource.pitch = pitch;
                }
                availableSource.PlayOneShot(clip);
            }
        }

        private AudioSource GetAvailableAudioSource(float targetPitch)
        {
            //Find a source with the exact pitch
            for (int i = 1; i < _sfxOneShotSources.Length; i++)
            {
                if (Mathf.Approximately(_sfxOneShotSources[i].pitch, targetPitch))
                    return _sfxOneShotSources[i];
            }

            // Find a source that's not playing
            for (int i = 1; i < _sfxOneShotSources.Length; i++)
            {
                if (!_sfxOneShotSources[i].isPlaying)
                    return _sfxOneShotSources[i];
            }

            // If all are busy, use the first one (it will interrupt)
            return _sfxOneShotSources[1];
        }

        public void PlaySfxLoopMusic(SoundType soundType)
        {
            AudioClip clip = SoundResources.Instance.GetClip(soundType);

            if (clip == null || _sfxLoopMusicSource.clip == clip)
                return;

            _sfxLoopMusicSource.clip = clip;

            if (MusicOn)
                _sfxLoopMusicSource.Play();
        }

        public void ToggleMusic(bool toggled)
        {
            if (toggled)
                _sfxLoopMusicSource.Play();
            else if (_sfxLoopMusicSource.isPlaying)
                _sfxLoopMusicSource.Stop();
        }
    }
}

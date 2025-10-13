using Brain.Util;
using Cinemachine;
using System.Collections;
using UnityEngine;

namespace Brain.Gameplay
{
    public class Cameras : UnitySingleton<Cameras>
    {
        public Camera MainCam;
        public CinemachineBrain CinemachineBrain;
        public CinemachineVirtualCamera MainVcam;

        public void ShakeActiveCamera(CameraShakeProfile cameraShakeProfile)
        {
            CinemachineBasicMultiChannelPerlin vCamNoise =
                CinemachineBrain.ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            StartCoroutine(ShakeCameraCoroutine(vCamNoise, cameraShakeProfile));
        }

        public void ShakeCamera(CinemachineBasicMultiChannelPerlin vCamNoise, CameraShakeProfile cameraShakeProfile)
        {
            StartCoroutine(ShakeCameraCoroutine(vCamNoise, cameraShakeProfile));
        }

        private IEnumerator ShakeCameraCoroutine(CinemachineBasicMultiChannelPerlin vCamNoise, CameraShakeProfile cameraShakeProfile)
        {
            vCamNoise.m_AmplitudeGain = cameraShakeProfile.ShakeAmplitude;
            vCamNoise.m_FrequencyGain = cameraShakeProfile.ShakeFrequency;

            yield return new WaitForSeconds(cameraShakeProfile.ShakeDuration);

            vCamNoise.m_AmplitudeGain = 0f;
            vCamNoise.m_FrequencyGain = 0f;
        }

        public void DisableDefaultBlendForAFrame()
        {
            StartCoroutine(DisableDefaultBlendCoroutine());
        }

        private IEnumerator DisableDefaultBlendCoroutine()
        {
            float defaultBlendTime = CinemachineBrain.m_DefaultBlend.m_Time;
            CinemachineBrain.m_DefaultBlend.m_Time = 0f;
            yield return new WaitForEndOfFrame();
            CinemachineBrain.m_DefaultBlend.m_Time = defaultBlendTime;
        }

        public void TriggerWhiteFlash()
        {
            //whiteFlashFeedback.PlayFeedbacks();
        }
    }

    public class CameraShakeProfile
    {
        public float ShakeAmplitude;
        public float ShakeFrequency;
        public float ShakeDuration;
    }
}
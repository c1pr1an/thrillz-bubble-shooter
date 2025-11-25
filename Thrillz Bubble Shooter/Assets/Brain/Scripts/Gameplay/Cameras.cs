using Brain.Managers;
using Brain.Util;
using Cinemachine;
using System.Collections;
using UnityEngine;

namespace Brain.Gameplay
{
    public class Cameras : UnitySingleton<Cameras>
    {
        // Public Fields
        public Camera MainCam;
        public CinemachineBrain CinemachineBrain;
        public CinemachineVirtualCamera MainVcam;


        public void SetCameraByAspectRatio()
        {
            float aspect = (float)Screen.width / (float)Screen.height;

            bool isNotchedPhone = aspect < 0.5f;
            if (isNotchedPhone) return; //Camera position for notched phones by default

            MainVcam.m_Lens.OrthographicSize = 9.9f;
            GridGenerator.Instance.GridPosition = new Vector3(0, 1.9f, 0);

            Vector3 pos = MainVcam.transform.position;
            MainVcam.transform.position = new Vector3(pos.x, pos.y, pos.z);
        }

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
using Brain.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Thrillz.Tools.Haptic;
using MoreMountains.NiceVibrations;

namespace Brain.Managers
{
    public enum HapticType
    {
        Selection,
        LightImpact,
        Failure,
    }

    public class HapticManager : UnitySingleton<HapticManager>
    {
        // Public Methods
        public void TriggerHaptic(HapticType hapticType)
        {
            // if (!_0_CoreProject.Scripts.SettingsToggleParameters.HapticOn)
            //     return;

            switch (hapticType)
            {
                case HapticType.Selection:
                    MMVibrationManager.Haptic(HapticTypes.Selection);
                    //ThrillzHaptic.Selection().Play();
                    break;
                case HapticType.Failure:
                    MMVibrationManager.Haptic(HapticTypes.Failure);
                    //ThrillzHaptic.Failure().Play();
                    break;
                case HapticType.LightImpact:
                    MMVibrationManager.Haptic(HapticTypes.LightImpact);
                    //ThrillzHaptic.LightImpact().Play();
                    break;
            }
        }
    }
}
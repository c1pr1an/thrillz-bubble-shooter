using Brain.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Thrillz.Tools.Haptic;

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
        public void TriggerHaptic(HapticType hapticType)
        {
            // if (!_0_CoreProject.Scripts.SettingsToggleParameters.HapticOn)
            //     return;

            // switch (hapticType)
            // {
            //     case HapticTypes.Selection:
            //         ThrillzHaptic.Selection().Play();
            //         break;
            //     case HapticTypes.Failure:
            //         ThrillzHaptic.Failure().Play();
            //         break;
            //     case HapticTypes.LightImpact:
            //         ThrillzHaptic.LightImpact().Play();
            //         break;
            // }
        }
    }
}
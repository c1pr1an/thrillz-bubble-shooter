using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Brain.Audio
{
    public enum SoundType
    {
        Generic = 0,

        UI_TapButton = 1,
        Screen_Transition_Open,
        Screen_Transition_Close,
        UI_OutOfTime,
        UI_OutOfLives,
        UI_GameFinished,
        UI_ButtonUndo,
        UI_ConfettiPop,
        UI_WarningTime,

        //Gameplay
        Game_ScoreAdd = 500,
        Game_ScoreAppear,
        Game_CartoonPop,
    }
}
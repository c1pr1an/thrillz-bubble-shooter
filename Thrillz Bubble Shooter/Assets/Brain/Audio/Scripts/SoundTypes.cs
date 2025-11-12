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
        Game_MatchPop,
        Game_BoosterBomb,
        Game_BoosterLightning,
        Game_BoosterRainbow,
        Game_BallShoot,
        Game_BallGridImpact,
        Game_BallSideBounce,
        Game_BonusBallReady,
        Game_BallSwap,
        Game_BoosterWick_Loop,
        Game_Electricity_Loop,
        Game_Magic_Loop,
        Game_Rocket_Launch,
        Game_Electricity_Launch,
        Game_Streak,
    }
}
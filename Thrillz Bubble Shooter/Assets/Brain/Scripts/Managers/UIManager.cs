using System.Collections;
using Brain.Audio;
using Brain.UI;
using Brain.Util;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Brain.Managers
{
    public class UIManager : UnitySingleton<UIManager>
    {
        // Serialized Fields
        [SerializeField] private Image _whiteOverlayImage;
        [SerializeField] private TextMeshProUGUI _gameSeedIdTxt;
        [SerializeField] private UIObjectPooler _uiObjectPooler;

        // Public Fields
        public Canvas Canvas;
        public GameplayUI GameplayUI;
        public OutOfTimePanel OutOfTimePanel;
        public LimitHitPanel LimitHitPanel;
        public GameFinishedPanel GameFinishedPanel;
        public EntryUI EntryUI;
        //public GlobalPauseMode GlobalPauseMode;

        // Properties
        public UIObjectPooler UIObjectPooler => _uiObjectPooler;

        // Public Methods
        public void Init()
        {
            DisplayGameSeedId();
        }

        public void ToggleWhiteFlash(bool toggled)
        {
            _whiteOverlayImage.gameObject.SetActive(toggled);
        }

        public void DisplayGameSeedId()
        {
            if (Debug.isDebugBuild)
            {
                _gameSeedIdTxt.gameObject.SetActive(true);
                _gameSeedIdTxt.text = "Game Seed: " + GameController.Instance.MatchSeed;
                Debug.Log("Game Seed: " + GameController.Instance.MatchSeed);
            }
            else
            {
                _gameSeedIdTxt.gameObject.SetActive(false);
            }
        }
    }
}

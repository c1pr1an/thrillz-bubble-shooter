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
        [SerializeField] private Image _blackOverlayImage;
        [SerializeField] private Image _whiteOverlayImage;
        [SerializeField] private TextMeshProUGUI _gameSeedIdTxt;

        public Canvas Canvas;
        public GameplayUI GameplayUI;
        public OutOfTimePanel OutOfTimePanel;
        public OutOfLivesPanel OutOfLivesPanel;
        public GameFinishedPanel GameFinishedPanel;
        //public GlobalPauseMode GlobalPauseMode;
        public void Init()
        {
            _blackOverlayImage.color = new Color(0f, 0f, 0f, 1f);
            _blackOverlayImage.DOFade(0f, 0.15f).SetEase(Ease.OutSine).OnComplete(() =>
            {
                _blackOverlayImage.gameObject.SetActive(false);
            });

            //DeckManager.Instance.UndoStateManager.OnGameStateChanged += OnGameStateChanged;
            //_undoButton.interactable = DeckManager.Instance.CanUndo();
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

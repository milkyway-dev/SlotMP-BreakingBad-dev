using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

public class UIManager : MonoBehaviour
{
    [Header("Popus UI")]
    [SerializeField] private GameObject MainPopup_Object;

    [Header("Win Popup")]
    [SerializeField] private Image Win_Image;
    [SerializeField] private GameObject WinPopup_Object;
    [SerializeField] private TMP_Text Win_Text;
    [SerializeField] private RectTransform WinBgAnimation;
    [SerializeField] private Sprite BigWin_Sprite, HugeWin_Sprite, MegaWin_Sprite, Jackpot_Sprite;
    [SerializeField] private ImageAnimation JackpotImageAnimation;
    private Tween ImageRotationTween;

    [Header("Disconnection Popup")]
    [SerializeField] private Button CloseDisconnect_Button;
    [SerializeField] private GameObject DisconnectPopup_Object;

    [Header("AnotherDevice Popup")]
    [SerializeField] private GameObject ADPopup_Object;

    [Header("LowBalance Popup")]
    [SerializeField] private Button LBExit_Button;
    [SerializeField] private GameObject LBPopup_Object;

    [Header("Audio Objects")]
    [SerializeField] private GameObject Settings_Object;
    [SerializeField] private Button SettingsQuit_Button;
    [SerializeField] private Slider Sound_Slider;
    [SerializeField] private Slider Music_Slider;

    [Header("Paytable Objects")]
    [SerializeField] private GameObject PaytableMenuObject;
    [SerializeField] private Button Paytable_Button;
    [SerializeField] private Button PaytableClose_Button;
    [SerializeField] private Button PaytableLeft_Button;
    [SerializeField] private Button PaytableRight_Button;
    [SerializeField] private TMP_Text FreeSpin_Text;
    [SerializeField] private TMP_Text Jackpot_Text;
    [SerializeField] private TMP_Text Wild_Text;
    [SerializeField] private List<GameObject> GameRulesPages = new();
    private int PageIndex;

    [Header("Game Quit Objects")]
    [SerializeField] private Button Quit_Button;
    [SerializeField] private Button QuitYes_Button;
    [SerializeField] private Button QuitNo_Button;
    [SerializeField] private GameObject QuitMenuObject;

    [Header("Menu Objects")]
    [SerializeField] private Button Menu_Button;
    [SerializeField] private Button Info_Button;
    [SerializeField] private Button Settings_Button;
    [SerializeField] private RectTransform Info_BttnTransform;
    [SerializeField] private RectTransform Settings_BttnTransform;

    [Header("Paytable Slot Text")]
    [SerializeField] private List<TMP_Text> SymbolsText = new();

    [SerializeField] private AudioController audioController;

    [SerializeField] private SlotBehaviour slotManager;

    [SerializeField] private SocketIOManager socketManager;

    private bool isMusic = true;
    private bool isSound = true;
    private bool isExit = false;
    private bool isMenu = false;


    private void Start()
    {

        if (LBExit_Button) LBExit_Button.onClick.RemoveAllListeners();
        if (LBExit_Button) LBExit_Button.onClick.AddListener(delegate { ClosePopup(LBPopup_Object); });

        if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.RemoveAllListeners();
        if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.AddListener(CallOnExitFunction);

        if (Sound_Slider)
        {
            Sound_Slider.onValueChanged.RemoveAllListeners();
            Sound_Slider.onValueChanged.AddListener((val) => { OnSoundChanged(val); });
        }

        if (Music_Slider)
        {
            Music_Slider.onValueChanged.RemoveAllListeners();
            Music_Slider.onValueChanged.AddListener((val) => { OnMusicChanged(val); });
        }

        if (Quit_Button) Quit_Button.onClick.RemoveAllListeners();
        if (Quit_Button) Quit_Button.onClick.AddListener(OpenQuitPanel);

        if (QuitNo_Button) QuitNo_Button.onClick.RemoveAllListeners();
        if (QuitNo_Button) QuitNo_Button.onClick.AddListener(delegate { ClosePopup(QuitMenuObject); });

        if (QuitYes_Button) QuitYes_Button.onClick.RemoveAllListeners();
        if (QuitYes_Button) QuitYes_Button.onClick.AddListener(CallOnExitFunction);

        if (Paytable_Button) Paytable_Button.onClick.RemoveAllListeners();
        if (Paytable_Button) Paytable_Button.onClick.AddListener(OpenPaytablePanel);

        if (PaytableClose_Button) PaytableClose_Button.onClick.RemoveAllListeners();
        if (PaytableClose_Button) PaytableClose_Button.onClick.AddListener(delegate { ClosePopup(PaytableMenuObject); });

        if (Menu_Button) Menu_Button.onClick.RemoveAllListeners();
        if (Menu_Button) Menu_Button.onClick.AddListener(delegate
        {
            if (!isMenu)
            {
                OpenCloseMenu(true);
            }
            else
            {
                OpenCloseMenu(false);
            }
        });

        if (Settings_Button) Settings_Button.onClick.RemoveAllListeners();
        if (Settings_Button) Settings_Button.onClick.AddListener(OpenSettingsPanel);

        if (SettingsQuit_Button) SettingsQuit_Button.onClick.RemoveAllListeners();
        if (SettingsQuit_Button) SettingsQuit_Button.onClick.AddListener(delegate { ClosePopup(Settings_Object); });

        if (PaytableLeft_Button) PaytableLeft_Button.onClick.RemoveAllListeners();
        if (PaytableLeft_Button) PaytableLeft_Button.onClick.AddListener(()=> ChangePage(false));

        if (PaytableRight_Button) PaytableRight_Button.onClick.RemoveAllListeners();
        if (PaytableRight_Button) PaytableRight_Button.onClick.AddListener(()=> ChangePage(true));
    }

    private void ChangePage(bool IncDec)
    {
        if (audioController) audioController.PlayButtonAudio();

        if (IncDec)
        {
            if(PageIndex < GameRulesPages.Count - 1)
            {
                PageIndex++;
            }
            if(PageIndex == GameRulesPages.Count - 1)
            {
                if (PaytableRight_Button) PaytableRight_Button.interactable = false;
            }
            if(PageIndex > 0)
            {
                if(PaytableLeft_Button) PaytableLeft_Button.interactable = true;
            }
        }
        else
        {
            if(PageIndex > 0)
            {
                PageIndex--;
            }
            if(PageIndex == 0)
            {
                if (PaytableLeft_Button) PaytableLeft_Button.interactable = false;
            }
            if(PageIndex < GameRulesPages.Count - 1)
            {
                if (PaytableRight_Button) PaytableRight_Button.interactable = true;
            }
        }
        foreach(GameObject g in GameRulesPages)
        {
            g.SetActive(false);
        }
        if (GameRulesPages[PageIndex]) GameRulesPages[PageIndex].SetActive(true);
    }

    private void OpenCloseMenu(bool toggle)
    {
        if(audioController) audioController.PlayButtonAudio();
        if (toggle)
        {
            isMenu = true;
            if (Info_Button) Info_Button.gameObject.SetActive(true);
            if (Settings_Button) Settings_Button.gameObject.SetActive(true);

            DOTween.To(() => Info_BttnTransform.anchoredPosition, (val) => Info_BttnTransform.anchoredPosition = val, new Vector2(Info_BttnTransform.anchoredPosition.x + 150, Info_BttnTransform.anchoredPosition.y), 0.1f).OnUpdate(() =>
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(Info_BttnTransform);
            });

            DOTween.To(() => Settings_BttnTransform.anchoredPosition, (val) => Settings_BttnTransform.anchoredPosition = val, new Vector2(Settings_BttnTransform.anchoredPosition.x + 300, Settings_BttnTransform.anchoredPosition.y), 0.1f).OnUpdate(() =>
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(Settings_BttnTransform);
            });
        }
        else
        {
            isMenu = false;
            DOTween.To(() => Info_BttnTransform.anchoredPosition, (val) => Info_BttnTransform.anchoredPosition = val, new Vector2(Info_BttnTransform.anchoredPosition.x - 150, Info_BttnTransform.anchoredPosition.y), 0.1f).OnUpdate(() =>
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(Info_BttnTransform);
            });

            DOTween.To(() => Settings_BttnTransform.anchoredPosition, (val) => Settings_BttnTransform.anchoredPosition = val, new Vector2(Settings_BttnTransform.anchoredPosition.x - 300, Settings_BttnTransform.anchoredPosition.y), 0.1f).OnUpdate(() =>
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(Settings_BttnTransform);
            });

            DOVirtual.DelayedCall(0.1f, () =>
            {
                if (Info_Button) Info_Button.gameObject.SetActive(false);
                if (Settings_Button) Settings_Button.gameObject.SetActive(false);
            });
        }
    }

    private void OnSoundChanged(float value)
    {
        audioController.OnVolumeChanged(value, "sound");
    }

    private void OnMusicChanged(float value)
    {
        audioController.OnVolumeChanged(value, "music");
    }

    private void SoundOnOFF(bool state)
    {
        //if (state)
        //{
        //    isSound = true;
        //    audioController.ToggleMute(!state, "sound");
        //    RectTransform soundTransform = Sound_Button.GetComponent<RectTransform>();
        //    DOTween.To(() => soundTransform.anchoredPosition, (val) => soundTransform.anchoredPosition = val, new Vector2(soundTransform.anchoredPosition.x + 95, soundTransform.anchoredPosition.y), 0.1f).OnUpdate(() =>
        //    {
        //        LayoutRebuilder.ForceRebuildLayoutImmediate(Info_BttnTransform);
        //    });
        //}
        //else
        //{
        //    isSound = false;
        //    audioController.ToggleMute(!state, "sound");
        //    RectTransform soundTransform = Sound_Button.GetComponent<RectTransform>();
        //    DOTween.To(() => soundTransform.anchoredPosition, (val) => soundTransform.anchoredPosition = val, new Vector2(soundTransform.anchoredPosition.x - 95, soundTransform.anchoredPosition.y), 0.1f).OnUpdate(() =>
        //    {
        //        LayoutRebuilder.ForceRebuildLayoutImmediate(Info_BttnTransform);
        //    });
        //}
    }

    private void MusicONOFF(bool state)
    {
        //if (state)
        //{
        //    isMusic = true;
        //    audioController.ToggleMute(!state, "music");
        //    RectTransform musicTransform = Music_Button.GetComponent<RectTransform>();
        //    DOTween.To(() => musicTransform.anchoredPosition, (val) => musicTransform.anchoredPosition = val, new Vector2(musicTransform.anchoredPosition.x + 95, musicTransform.anchoredPosition.y), 0.1f).OnUpdate(() =>
        //    {
        //        LayoutRebuilder.ForceRebuildLayoutImmediate(Info_BttnTransform);
        //    });
        //}
        //else
        //{
        //    isMusic = false;
        //    audioController.ToggleMute(!state, "music");
        //    RectTransform musicTransform = Music_Button.GetComponent<RectTransform>();
        //    DOTween.To(() => musicTransform.anchoredPosition, (val) => musicTransform.anchoredPosition = val, new Vector2(musicTransform.anchoredPosition.x - 95, musicTransform.anchoredPosition.y), 0.1f).OnUpdate(() =>
        //    {
        //        LayoutRebuilder.ForceRebuildLayoutImmediate(Info_BttnTransform);
        //    });
        //}
    }

    private void OpenSettingsPanel()
    {
        if (audioController) audioController.PlayButtonAudio();

        if (MainPopup_Object) MainPopup_Object.SetActive(true);
        if (Settings_Object) Settings_Object.SetActive(true);
        OpenCloseMenu(false);
    }

    private void OpenQuitPanel()
    {
        if (audioController) audioController.PlayButtonAudio();


        if (MainPopup_Object) MainPopup_Object.SetActive(true);
        if (QuitMenuObject) QuitMenuObject.SetActive(true);
    }

    private void OpenPaytablePanel()
    {
        if (audioController) audioController.PlayButtonAudio();

        if (MainPopup_Object) MainPopup_Object.SetActive(true);

        PageIndex = 0;

        foreach(GameObject g in GameRulesPages)
        {
            g.SetActive(false);
        }

        GameRulesPages[0].SetActive(true);
        if(PaytableLeft_Button) PaytableLeft_Button.interactable = false;
        if(PaytableRight_Button) PaytableRight_Button.interactable = true;

        if (PaytableMenuObject) PaytableMenuObject.SetActive(true);

        OpenCloseMenu(false);
    }

    internal void LowBalPopup()
    {
        OpenPopup(LBPopup_Object);
    }

    internal void DisconnectionPopup(bool isReconnection)
    {
        if (!isExit)
        {
            OpenPopup(DisconnectPopup_Object);
        }
    }

    internal void PopulateWin(int value)
    {
        switch (value)
        {
            case 1:
                if (Win_Image) Win_Image.sprite = BigWin_Sprite;
                break;
            case 2:
                if (Win_Image) Win_Image.sprite = HugeWin_Sprite;
                break;
            case 3:
                if (Win_Image) Win_Image.sprite = MegaWin_Sprite;
                break;
            case 4:
                if (Win_Image) Win_Image.sprite = Jackpot_Sprite;
                JackpotImageAnimation.StartAnimation();
                break;
        }

        StartPopupAnim();
    }

    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Space))
    //    {
    //        PopulateWin(4);
    //        Debug.Log("Called");
    //    }
    //}

    private void StartPopupAnim()
    {
        if (WinPopup_Object) WinPopup_Object.SetActive(true);
        if (MainPopup_Object) MainPopup_Object.SetActive(true);

        audioController.PlayWLAudio("bigwin");

        Win_Image.rectTransform.DOScale(new Vector3(1, 1, 1), .5f).SetEase(Ease.OutCirc);

        ImageRotationTween = WinBgAnimation.DORotate(new Vector3(0, 0, 360), 2f, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear) // Make the rotation constant
            .SetLoops(-1, LoopType.Incremental); // Rotate infinitely in an incremental way

        WinBgAnimation.DOScale(Vector3.one, .6f).SetEase(Ease.OutCirc);

        DOVirtual.DelayedCall(3f, () =>
        {
            Win_Image.rectTransform.DOScale(Vector3.zero, .5f).SetEase(Ease.InBack).OnComplete(() => ClosePopup(WinPopup_Object));

            WinBgAnimation.DOScale(Vector3.zero, .5f).SetEase(Ease.InBack).OnComplete(()=> ImageRotationTween.Kill());

            slotManager.CheckPopups = false;
        });
    }

    internal void ADfunction()
    {
        OpenPopup(ADPopup_Object); 
    }

    internal void InitialiseUIData(string SupportUrl, string AbtImgUrl, string TermsUrl, string PrivacyUrl, Paylines symbolsText)
    {
        StartCoroutine(DownloadImage(AbtImgUrl));
        PopulateSymbolsPayout(symbolsText);
    }

    internal IEnumerator MidGameImageAnimation(ImageAnimation imageAnimation)
    {
        imageAnimation.transform.parent.GetChild(0).gameObject.SetActive(true);
        imageAnimation.gameObject.SetActive(true);
        imageAnimation.StartAnimation();
        yield return new WaitUntil(() => imageAnimation.rendererDelegate.sprite == imageAnimation.textureArray[imageAnimation.textureArray.Count - 1]);
        imageAnimation.transform.parent.GetChild(0).gameObject.SetActive(false);
        imageAnimation.StopAnimation();
        imageAnimation.gameObject.SetActive(false);
    }

    private void PopulateSymbolsPayout(Paylines paylines)
    {
        for (int i = 0; i < SymbolsText.Count; i++)
        {
            string text = null;
            if (paylines.symbols[i].Multiplier[0][0] != 0)
            {
                text += "5x - " + paylines.symbols[i].Multiplier[0][0];
            }
            if (paylines.symbols[i].Multiplier[1][0] != 0)
            {
                text += "\n4x - " + paylines.symbols[i].Multiplier[1][0];
            }
            if (paylines.symbols[i].Multiplier[2][0] != 0)
            {
                text += "\n3x - " + paylines.symbols[i].Multiplier[2][0];
            }
            if (SymbolsText[i]) SymbolsText[i].text = text;
        }

        for (int i = 0; i < paylines.symbols.Count; i++)
        {
            if (paylines.symbols[i].Name.ToUpper() == "FREESPIN")
            {
                if (FreeSpin_Text) FreeSpin_Text.text = paylines.symbols[i].description.ToString();
            }            
            if (paylines.symbols[i].Name.ToUpper() == "JACKPOT")
            {
                if (Jackpot_Text) Jackpot_Text.text = paylines.symbols[i].description.ToString();
            }
            if (paylines.symbols[i].Name.ToUpper() == "WILD")
            {
                if (Wild_Text) Wild_Text.text = paylines.symbols[i].description.ToString();
            }
        }
    }

    private void CallOnExitFunction()
    {
        isExit = true;
        audioController.PlayButtonAudio();
        slotManager.CallCloseSocket();
    }

    private void OpenPopup(GameObject Popup)
    {
        if (audioController) audioController.PlayButtonAudio();

        if (Popup) Popup.SetActive(true);
        if (MainPopup_Object) MainPopup_Object.SetActive(true);
    }

    private void ClosePopup(GameObject Popup)
    {
        if (audioController) audioController.PlayButtonAudio();
        if (Popup) Popup.SetActive(false);
        if (!DisconnectPopup_Object.activeSelf) 
        {
            if (MainPopup_Object) MainPopup_Object.SetActive(false);
        }
    }

    private void UrlButtons(string url)
    {
        Application.OpenURL(url);
    }

    private IEnumerator DownloadImage(string url)
    {
        // Create a UnityWebRequest object to download the image
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);

        // Wait for the download to complete
        yield return request.SendWebRequest();

        // Check for errors
        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);

            // Apply the sprite to the target image
            //AboutLogo_Image.sprite = sprite;
        }
        else
        {
            Debug.LogError("Error downloading image: " + request.error);
        }
    }
}

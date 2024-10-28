using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System;

public class SlotBehaviour : MonoBehaviour
{
    [Header("Script References")]
    [SerializeField] private SocketIOManager SocketManager;
    [SerializeField] private StaticSymbolController staticSymbolController;
    [SerializeField] private AudioController audioController;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private BonusController _bonusManager;

    [Header("Sprites References")]
    [SerializeField] private Sprite[] myImages;  //images taken initially
    [SerializeField] private Sprite[] losPollosSprites;

    [Header("Slot References")]
    [SerializeField] private List<SlotImage> images;     //class to store total images
    [SerializeField] internal List<SlotImage> Tempimages;     //class to store the result matrix
    [SerializeField] internal List<SlotImage> ResultMatrix;

    [Header("Slots Transform Reference")]
    [SerializeField] private Transform[] Slot_Transform;

    [Header("UI Objects References")]
    [SerializeField] private Button SlotStart_Button;
    [SerializeField] private Button AutoSpin_Button;
    [SerializeField] private Button AutoSpinStop_Button;
    [SerializeField] private Button TotalBetPlus_Button;
    [SerializeField] private Button TotalBetMinus_Button;
    [SerializeField] private Button LineBetPlus_Button;
    [SerializeField] private Button LineBetMinus_Button;

    [SerializeField] private TMP_Text Balance_text;
    [SerializeField] private TMP_Text TotalBet_text;
    [SerializeField] private TMP_Text LineBet_text;
    [SerializeField] private TMP_Text TotalWin_text;
    [SerializeField] private TMP_Text BigWin_Text;
    [SerializeField] private TMP_Text BonusWin_Text;
    [SerializeField] private TMP_Text FSnum_text;

    [SerializeField] private ImageAnimation BonusImageAnimation;
    [SerializeField] private ImageAnimation FreeGamesImageAnimation;

    [SerializeField] private GameObject FreeSpinsUI_Panel;
    [SerializeField] private GameObject LinesUI;
    [SerializeField] private GameObject TotalBetUI;
    [SerializeField] private GameObject LineBetUI;

    [Header("Animation Sprites References")]
    [SerializeField] private Sprite[] B_Sprites;
    [SerializeField] private Sprite[] C_Sprites;
    [SerializeField] private Sprite[] N_Sprites;
    [SerializeField] private Sprite[] O_Sprites;
    [SerializeField] private Sprite[] Link_Sprites;
    [SerializeField] private Sprite[] MegaLink_Sprites;
    [SerializeField] private Sprite[] Barrel_Sprites;
    [SerializeField] private Sprite[] Bus_Sprites;
    [SerializeField] private Sprite[] Blue_Sprites;
    [SerializeField] private Sprite[] Orange_Sprites;
    [SerializeField] private Sprite[] Purple_Sprites;
    [SerializeField] private Sprite[] Yellow_Sprites;
    [SerializeField] private Sprite[] Diamond_Sprites;
    [SerializeField] private Sprite[] CC_Sprites;
    [SerializeField] private Sprite[] LP2_Sprites;
    [SerializeField] private Sprite[] LP3_Sprites;
    [SerializeField] private Sprite[] LP4_Sprites;
    [SerializeField] private Sprite[] LP5_Sprites;
    [SerializeField] private Sprite[] LP7_Sprites;
    [SerializeField] private Sprite[] GoldCoin_Sprites;

    private List<Tweener> alltweens = new List<Tweener>();
    private List<(Transform slotTransform, int originalSiblingIndex)> changedSlots = new();  //hold the reordered result matrix slots to show the fire animation

    private bool IsAutoSpin = false;
    private bool IsFreeSpin = false;
    private bool WinAnimationFin = true;
    private bool IsSpinning = false;
    private bool CheckSpinAudio = false;
    internal bool CheckPopups = false;

    private Coroutine AutoSpinRoutine = null;
    private Coroutine FreeSpinRoutine = null;
    private Coroutine tweenroutine;
    private Coroutine LineAnimRoutine = null;

    int tweenHeight = 0;  //calculate the height at which tweening is done
    private int BetCounter = 0;
    protected int Lines = 20;
    private int numberOfSlots = 5;          //number of columns
    private int freeSpinsCount;
    [SerializeField] private int IconSizeFactor = 100;       //set this parameter according to the size of the icon and spacing

    private double currentBalance = 0;
    private double currentTotalBet = 0;

    private void Start()
    {
        IsAutoSpin = false;

        if (SlotStart_Button) SlotStart_Button.onClick.RemoveAllListeners();
        if (SlotStart_Button) SlotStart_Button.onClick.AddListener(delegate { StartSlots(); });

        if (TotalBetPlus_Button) TotalBetPlus_Button.onClick.RemoveAllListeners();
        if (TotalBetPlus_Button) TotalBetPlus_Button.onClick.AddListener(delegate { ChangeBet(true); });

        if (TotalBetMinus_Button) TotalBetMinus_Button.onClick.RemoveAllListeners();
        if (TotalBetMinus_Button) TotalBetMinus_Button.onClick.AddListener(delegate { ChangeBet(false); });

        if (LineBetPlus_Button) LineBetPlus_Button.onClick.RemoveAllListeners();
        if (LineBetPlus_Button) LineBetPlus_Button.onClick.AddListener(delegate { ChangeBet(true); });

        if (LineBetMinus_Button) LineBetMinus_Button.onClick.RemoveAllListeners();
        if (LineBetMinus_Button) LineBetMinus_Button.onClick.AddListener(delegate { ChangeBet(false); });

        if (AutoSpin_Button) AutoSpin_Button.onClick.RemoveAllListeners();
        if (AutoSpin_Button) AutoSpin_Button.onClick.AddListener(AutoSpin);

        if (AutoSpinStop_Button) AutoSpinStop_Button.onClick.RemoveAllListeners();
        if (AutoSpinStop_Button) AutoSpinStop_Button.onClick.AddListener(StopAutoSpin);

        tweenHeight = (13 * IconSizeFactor) - 280;
    }

    #region Autospin
    private void AutoSpin()
    {
        if (!IsAutoSpin)
        {
            IsAutoSpin = true;
            if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(true);
            if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(false);

            if (AutoSpinRoutine != null)
            {
                StopCoroutine(AutoSpinRoutine);
                AutoSpinRoutine = null;
            }
            AutoSpinRoutine = StartCoroutine(AutoSpinCoroutine());
        }
    }

    private void StopAutoSpin()
    {
        if (IsAutoSpin)
        { 
            if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(false);
            if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(true);
            StartCoroutine(StopAutoSpinCoroutine());
        }
    }

    private IEnumerator AutoSpinCoroutine()
    {
        while (IsAutoSpin)
        {
            if(LineAnimRoutine!=null && !WinAnimationFin)
            {
                yield return new WaitUntil(() => WinAnimationFin);
                StopGameAnimation();
            }

            StartSlots(IsAutoSpin);
            yield return tweenroutine;
        }
    }

    private IEnumerator StopAutoSpinCoroutine()
    {
        yield return new WaitUntil(() => !IsSpinning);
        ToggleButtonGrp(true);
        if (AutoSpinRoutine != null || tweenroutine != null)
        {
            StopCoroutine(AutoSpinRoutine);
            StopCoroutine(tweenroutine);
            tweenroutine = null;
            AutoSpinRoutine = null;
            IsAutoSpin = false;
            StopCoroutine(StopAutoSpinCoroutine());
        }
    }
    #endregion

    private void CompareBalance()
    {
        if (currentBalance < currentTotalBet)
        {
            uiManager.LowBalPopup();
            if (AutoSpin_Button) AutoSpin_Button.interactable = false;
            if (SlotStart_Button) SlotStart_Button.interactable = false;
        }
        else
        {
            if (AutoSpin_Button) AutoSpin_Button.interactable = true;
            if (SlotStart_Button) SlotStart_Button.interactable = true;
        }
    }

    private void ChangeBet(bool IncDec)
    {
        if (audioController) audioController.PlayButtonAudio();
        if (IncDec)
        {
            BetCounter++;
            if (BetCounter >= SocketManager.initialData.Bets.Count)
            {
                BetCounter = 0; // Loop back to the first bet
            }
        }
        else
        {
            BetCounter--;
            if (BetCounter < 0)
            {
                BetCounter = SocketManager.initialData.Bets.Count - 1; // Loop to the last bet
            }
        }

        if (LineBet_text) LineBet_text.text = SocketManager.initialData.Bets[BetCounter].ToString();
        if (TotalBet_text) TotalBet_text.text = (SocketManager.initialData.Bets[BetCounter] * Lines).ToString();

        currentTotalBet = SocketManager.initialData.Bets[BetCounter] * Lines;
        CompareBalance();
    }

    #region InitialFunctions
    internal void shuffleInitialMatrix()
    {
        for(int i = 0; i < images.Count; i++)
        {
            for(int j = 0; j < myImages.Length; j++)
            {
                int randomIndex = UnityEngine.Random.Range(0, myImages.Length-8);
                images[i].slotImages[j].sprite = myImages[randomIndex];
            }
        }
    }

    internal void SetInitialUI()
    {
        BetCounter = 0;
        if (LineBet_text) LineBet_text.text = SocketManager.initialData.Bets[BetCounter].ToString();
        if (TotalBet_text) TotalBet_text.text = (SocketManager.initialData.Bets[BetCounter] * Lines).ToString();
        if (TotalWin_text) TotalWin_text.text = "0.00";
        if (Balance_text) Balance_text.text = SocketManager.playerdata.Balance.ToString("f2");
        currentBalance = SocketManager.playerdata.Balance;
        currentTotalBet = SocketManager.initialData.Bets[BetCounter] * Lines;
        CompareBalance();
        uiManager.InitialiseUIData(SocketManager.initUIData.AbtLogo.link, SocketManager.initUIData.AbtLogo.logoSprite, SocketManager.initUIData.ToULink, SocketManager.initUIData.PopLink, SocketManager.initUIData.paylines);
    }
    #endregion

    private void ReorderImages()
    {
        for (int i = 0; i < Tempimages.Count; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (Tempimages[i].slotImages[j].sprite == myImages[13])
                {
                    // Store the original sibling index before changing it
                    Transform slotTransform = Tempimages[i].slotImages[j].transform;
                    int originalSiblingIndex = slotTransform.GetSiblingIndex();

                    // Add the slot transform and its original sibling index to the list
                    changedSlots.Add((slotTransform, originalSiblingIndex));

                    // Now apply the changes
                    SetUpAccordingToCC(slotTransform);
                }
            }
        }
    }

    private void SetUpAccordingToCC(Transform slotTransform)
    {
        slotTransform.SetSiblingIndex(24);

        for (int i = 0; i < 2; i++)
        {
            var animation = slotTransform.GetChild(i).GetComponent<ImageAnimation>();
            if (animation != null)
            {
                animation.AnimationSpeed = 15;  // Change animation speed
                animation.StartAnimation();     // Start animation
                slotTransform.GetChild(i).gameObject.SetActive(true);  // Activate the animation object
            }
        }
    }

    // Function to reset all changed slots
    private void ResetImages()
    {
        foreach (var (slotTransform, originalSiblingIndex) in changedSlots)
        {
            // Reset the sibling index to the original value
            slotTransform.SetSiblingIndex(originalSiblingIndex);

            // Stop the animation and reset the state
            for (int i = 0; i < 2; i++)
            {
                var animation = slotTransform.GetChild(i).GetComponent<ImageAnimation>();
                if (animation != null)
                {
                    animation.StopAnimation();  // Assuming you have a StopAnimation method
                    slotTransform.GetChild(i).gameObject.SetActive(false);  // Deactivate the animation object
                }
            }
        }

        // Clear the list after resetting everything
        changedSlots.Clear();
    }

    private void OnApplicationFocus(bool focus)
    {
        audioController.CheckFocusFunction(focus, CheckSpinAudio);
    }

    //function to populate animation sprites accordingly
    private void PopulateAnimationSprites(ImageAnimation animScript, int val, int LP = 0, string coin = null)
    {
        animScript.textureArray.Clear();
        animScript.textureArray.TrimExcess();
        switch (val)
        {
            case 0:
                for(int i = 0; i < C_Sprites.Length; i++)
                {
                    animScript.textureArray.Add(C_Sprites[i]);
                }
                animScript.AnimationSpeed = 15f;
                break;
            case 1:
                for (int i = 0; i < O_Sprites.Length; i++)
                {
                    animScript.textureArray.Add(O_Sprites[i]);
                }
                animScript.AnimationSpeed = 15f;
                break;
            case 2:
                foreach(Sprite sprite in N_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 15f;
                break;
            case 3:
                foreach (Sprite sprite in B_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 15f;
                break;
            case 4:
                foreach (Sprite sprite in Barrel_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 12f;
                break;
            case 5:
                foreach (Sprite sprite in Bus_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 12f;
                break;
            case 6:
                foreach (Sprite sprite in Orange_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 12f;
                break;
            case 7:
                foreach (Sprite sprite in Purple_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 12f;
                break;
            case 8:
                foreach (Sprite sprite in Blue_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 12f;
                break;
            case 10:
                foreach (Sprite sprite in Yellow_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 12f;
                break;
            case 11:
                foreach (Sprite sprite in Link_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 12f;
                break;
            case 12:
                foreach (Sprite sprite in MegaLink_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 10f;
                break;
            case 13:
                foreach (Sprite sprite in CC_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 10f;
                break;
            case 14:
                foreach (Sprite sprite in GoldCoin_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 12f;
                animScript.transform.GetChild(3).GetComponent<TMP_Text>().text = coin;
                animScript.transform.GetChild(3).gameObject.SetActive(true);
                break;
            case 15:
                foreach (Sprite sprite in Diamond_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 12f;
                break;
            case 16:
                if(LP == 2)
                {
                    foreach(Sprite sprite in LP2_Sprites)
                    {
                        animScript.textureArray.Add(sprite);
                    }
                    animScript.AnimationSpeed = 10f;
                }
                else if(LP == 3)
                {
                    foreach (Sprite sprite in LP3_Sprites)
                    {
                        animScript.textureArray.Add(sprite);
                    }
                    animScript.AnimationSpeed = 10f;
                }
                else if(LP == 4)
                {
                    foreach (Sprite sprite in LP4_Sprites)
                    {
                        animScript.textureArray.Add(sprite);
                    }
                    animScript.AnimationSpeed = 10f;
                }
                else if(LP == 5)
                {
                    foreach (Sprite sprite in LP5_Sprites)
                    {
                        animScript.textureArray.Add(sprite);
                    }
                    animScript.AnimationSpeed = 10f;
                }
                else if(LP == 7)
                {
                    foreach (Sprite sprite in LP7_Sprites)
                    {
                        animScript.textureArray.Add(sprite);
                    }
                    animScript.AnimationSpeed = 10f;
                }
                else{
                    Debug.LogError("LP index value sent was wrong");
                }
                break;
        }
    }

    #region SlotSpin
    //starts the spin process
    private void StartSlots(bool autoSpin = false)
    {
        if (audioController) audioController.PlaySpinButtonAudio();

        if (IsFreeSpin)
        {
            FSnum_text.text = (freeSpinsCount - 1).ToString();
        }

        if (!autoSpin)
        {
            if (AutoSpinRoutine != null)
            {
                StopCoroutine(AutoSpinRoutine);
                StopCoroutine(tweenroutine);
                tweenroutine = null;
                AutoSpinRoutine = null;
            }
        }
        if (SlotStart_Button) SlotStart_Button.interactable = false;
        if (TotalWin_text) TotalWin_text.text = "0.00";

        StopGameAnimation(); //commented this line for testing

        tweenroutine = StartCoroutine(TweenRoutine());
    }

    //manage the Routine for spinning of the slots
    private IEnumerator TweenRoutine()
    {
        if (currentBalance < currentTotalBet && !IsFreeSpin) // Check if balance is sufficient to place the bet
        {
            CompareBalance();
            StopAutoSpin();
            yield return new WaitForSeconds(1);
            yield break;
        }

        CheckSpinAudio = true;
        IsSpinning = true;
        ToggleButtonGrp(false);

        for (int i = 0; i < numberOfSlots; i++) // Initialize tweening for slot animations
        {
            InitializeTweening(Slot_Transform[i]);
        }

        if (!IsFreeSpin) // Deduct balance if not a bonus
        {
            BalanceDeduction();
        }

        SocketManager.AccumulateResult(BetCounter);
        yield return new WaitUntil(() => SocketManager.isResultdone);

        int CCcount = 0;
        for (int j = 0; j < SocketManager.resultData.ResultReel.Count; j++) // Update slot images based on the results
        {
            List<int> resultnum = SocketManager.resultData.FinalResultReel[j]?.Split(',')?.Select(Int32.Parse)?.ToList();
            for (int i = 0; i < 5; i++)
            {
                if (images[i].slotImages[images[i].slotImages.Count - 5 + j])
                {
                    if(resultnum[i] == 16)
                    {
                        if (SocketManager.resultData.winData.losPollos.Count > 0)
                        {
                            foreach (var losPollos in SocketManager.resultData.winData.losPollos)
                            {
                                if (losPollos.index[0] == j && losPollos.index[1] == i)
                                {
                                    images[i].slotImages[images[i].slotImages.Count - 5 + j].sprite = losPollosSprites[losPollos.value];
                                    PopulateAnimationSprites(images[i].slotImages[images[i].slotImages.Count - 5 + j].gameObject.GetComponent<ImageAnimation>(), resultnum[i], losPollos.value);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            int[] tempIndex = { 2, 3, 4, 5, 7 };
                            int randomIndex = tempIndex[UnityEngine.Random.Range(0, tempIndex.Length)];
                            images[i].slotImages[images[i].slotImages.Count - 5 + j].sprite = losPollosSprites[randomIndex];
                        }
                    }
                    else if (resultnum[i] == 14)
                    {
                        foreach(var coins in SocketManager.resultData.winData.coinValues)
                        {
                            if(coins.index[0] == j && coins.index[1] == i)
                            {
                                images[i].slotImages[images[i].slotImages.Count - 5 + j].sprite = myImages[resultnum[i]];
                                PopulateAnimationSprites(images[i].slotImages[images[i].slotImages.Count - 5 + j].gameObject.GetComponent<ImageAnimation>(), resultnum[i], 0, coins.value.ToString("f2"));
                                break;
                            }
                        }
                    }
                    else
                    {
                        if(resultnum[i] == 13)
                        {
                            CCcount++;
                        }
                        images[i].slotImages[images[i].slotImages.Count - 5 + j].sprite = myImages[resultnum[i]];
                        PopulateAnimationSprites(images[i].slotImages[images[i].slotImages.Count - 5 + j].gameObject.GetComponent<ImageAnimation>(), resultnum[i]);
                    }
                } 
            }
        }
        if (CCcount != 0)
        {
            ReorderImages();
        }

        yield return new WaitForSeconds(.5f);

        for (int i = 0; i < numberOfSlots; i++) // Stop tweening for each slot
        {
            yield return StopTweening(5, Slot_Transform[i], i);
        }

        if (SocketManager.playerdata.currentWining > 0) WinningsTextAnimation(); // Trigger winnings animation if applicable


        CheckPayoutLineBackend(SocketManager.resultData.linesToEmit, SocketManager.resultData.FinalsymbolsToEmit, SocketManager.resultData.jackpot);
        yield return new WaitForSeconds(1f);
        KillAllTweens();

        //currentBalance = SocketManager.playerdata.Balance;

        CheckPopups = true;

        CheckWinPopups();

        yield return new WaitUntil(()=> !CheckPopups);
        yield return new WaitUntil(() => WinAnimationFin);

        if (SocketManager.resultData.bonus.isBonus)
        {
            StopGameAnimation();

            IsSpinning = false;
            if (IsAutoSpin)
            {
                StopAutoSpin();
            }
            else if (IsFreeSpin)
            {
                FreeSpinsUI_Panel.SetActive(false);
            }

            yield return StartCoroutine(uiManager.MidGameImageAnimation(BonusImageAnimation));
            staticSymbolController.TurnOnIndices(SocketManager.resultData.bonus.freezeIndices);             //code to turn on static slot objects to turn on
            _bonusManager.StartBonus(SocketManager.resultData.bonus.spinCount);
        }

        if (SocketManager.resultData.freeSpins.isNewAdded)
        {
            if (IsFreeSpin)
            {
                freeSpinsCount = SocketManager.resultData.freeSpins.count;
                FSnum_text.text = freeSpinsCount.ToString();
            }
            else
            {
                freeSpinsCount = SocketManager.resultData.freeSpins.count;
                IsFreeSpin = true;
                IsSpinning = false;
                if (IsAutoSpin)
                {
                    StopAutoSpin();
                }
                StopGameAnimation();
                yield return StartCoroutine(uiManager.MidGameImageAnimation(FreeGamesImageAnimation));
                OpenFreeSpinsUI(freeSpinsCount);
            }
        }

        //if(SocketManager.playerdata.currentWining <= 0 && SocketManager.resultData.jackpot <= 0 && !SocketManager.resultData.freeSpins.isNewAdded)
        //{
        //    audioController.PlayWLAudio("lose");
        //}

        if (IsFreeSpin && SocketManager.resultData.freeSpins.count <= 0)
        {
            IsFreeSpin = false;
            IsSpinning = false;
            CloseFreeSpinsUI();
            ToggleButtonGrp(true);
        }
        else if(IsAutoSpin)
        {
            IsSpinning = false;
            yield return new WaitForSeconds(2f);
        }
        else
        {
            ToggleButtonGrp(true);
            IsSpinning = false;
        }
    }
    #endregion

    internal void CheckWinPopups()
    {
        if (SocketManager.resultData.WinAmout >= currentTotalBet * 5 && SocketManager.resultData.WinAmout < currentTotalBet * 10)
        {
            uiManager.PopulateWin(1);
        }
        else if (SocketManager.resultData.WinAmout >= currentTotalBet * 10 && SocketManager.resultData.WinAmout < currentTotalBet * 15)
        {
            uiManager.PopulateWin(2);
        }
        //else if (SocketManager.resultData.WinAmout >= currentTotalBet * 15)
        //{
        //    uiManager.PopulateWin(3);
        //}
        else
        {
            CheckPopups = false;
        }
    }

    private void OpenFreeSpinsUI(int spinsLeft)
    {
        FSnum_text.text = spinsLeft.ToString();
        FreeSpinsUI_Panel.SetActive(true);
        LinesUI.SetActive(false);
        TotalBetUI.SetActive(false);
        LineBetUI.SetActive(false);
    }

    private void CloseFreeSpinsUI()
    {
        FreeSpinsUI_Panel.SetActive(false);
        LinesUI.SetActive(true);
        TotalBetUI.SetActive(true);
        LineBetUI.SetActive(true);
    }

    private void WinningsTextAnimation()
    {
        double winAmt = 0;
        double currentWin = 0;

        double currentBal = 0;
        double Balance = 0;

        //double BonusWinAmt = 0;
        //double currentBonusWinnings = 0;

        //if (bonus)
        //{
        //    try
        //    {
        //        BonusWinAmt = double.Parse(SocketManager.playerdata.currentWining.ToString("f2"));
        //        currentBonusWinnings = double.Parse(BonusWin_Text.text);
        //    }
        //    catch (Exception e)
        //    {
        //        Debug.Log("Error while conversion " + e.Message);
        //    }
        //}
        try
        {
            winAmt = double.Parse(SocketManager.playerdata.currentWining.ToString("f2"));
        }
        catch (Exception e)
        {
            Debug.Log("Error while conversion " + e.Message);
        }

        try
        {
            currentBal = double.Parse(Balance_text.text);
        }
        catch (Exception e)
        {
            Debug.Log("Error while conversion " + e.Message);
        }

        try
        {
            Balance = double.Parse(SocketManager.playerdata.Balance.ToString("f2"));
        }
        catch (Exception e)
        {
            Debug.Log("Error while conversion " + e.Message);
        }

        try
        {
            currentWin = double.Parse(TotalWin_text.text);
        }
        catch (Exception e)
        {
            Debug.Log("Error while conversion " + e.Message);
        }

        //if (bonus)
        //{
        //    double CurrTotal = BonusWinAmt + currentBonusWinnings;
        //    DOTween.To(() => currentBonusWinnings, (val) => currentBonusWinnings = val, CurrTotal, 0.8f).OnUpdate(() =>
        //    {
        //        if (BonusWin_Text) BonusWin_Text.text = currentBonusWinnings.ToString("f2");
        //    });

        //    double start = 0;
        //    DOTween.To(() => start, (val) => start = val, BonusWinAmt, 0.8f).OnUpdate(() =>
        //    {
        //        if (BigWin_Text) BigWin_Text.text = start.ToString("f2");
        //    });
        //}
        //else
        //{
        //}
            DOTween.To(() => currentWin, (val) => currentWin = val, winAmt, 0.8f).OnUpdate(() =>
            {
                if (TotalWin_text) TotalWin_text.text = currentWin.ToString("f2");
                if (BigWin_Text) BigWin_Text.text = currentWin.ToString("f2");
            });

            DOTween.To(() => currentBal, (val) => currentBal = val, Balance, 0.8f).OnUpdate(() =>
            {
                if (Balance_text) Balance_text.text = currentBal.ToString("f2");
            });
    }

    private void BalanceDeduction()
    {
        double bet = 0;
        double balance = 0;
        try
        {
            bet = double.Parse(TotalBet_text.text);
        }
        catch (Exception e)
        {
            Debug.Log("Error while conversion " + e.Message);
        }

        try
        {
            balance = double.Parse(Balance_text.text);
        }
        catch (Exception e)
        {
            Debug.Log("Error while conversion " + e.Message);
        }
        double initAmount = balance;

        balance = balance - bet;

        DOTween.To(() => initAmount, (val) => initAmount = val, balance, 0.8f).OnUpdate(() =>
        {
            if (Balance_text) Balance_text.text = initAmount.ToString("f2");
        });
    }

    //generate the payout lines generated 
    private void CheckPayoutLineBackend(List<int> LineId, List<string> points_AnimString, double jackpot = 0)
    {
        List<int> points_anim = null;
        if (LineId.Count > 0 || points_AnimString.Count > 0)
        {
            TurnOnBlackBoxes();

            if (jackpot > 0)
            {
                if (audioController) audioController.PlayWLAudio("megaWin");
                for (int i = 0; i < Tempimages.Count; i++)
                {
                    for (int k = 0; k < Tempimages[i].slotImages.Count; k++)
                    {
                        StartGameAnimation(Tempimages[i].slotImages[k].gameObject);
                    }
                }
            }
            else
            {
                if (audioController) audioController.PlayWLAudio("win");
                for (int i = 0; i < points_AnimString.Count; i++)
                {
                    points_anim = points_AnimString[i]?.Split(',')?.Select(Int32.Parse)?.ToList();

                    for (int k = 0; k < points_anim.Count; k++)
                    {
                        if (points_anim[k] >= 10)
                        {
                            StartGameAnimation(Tempimages[(points_anim[k] / 10) % 10].slotImages[points_anim[k] % 10].gameObject);
                        }
                        else
                        {
                            StartGameAnimation(Tempimages[0].slotImages[points_anim[k]].gameObject);
                        }
                    }
                }
            }

            //if (!SocketManager.resultData.freeSpins.isNewAdded)
            //{
            //    if (SkipWinAnimation_Button) SkipWinAnimation_Button.gameObject.SetActive(true);
            //}

            //if (IsFreeSpin && !SocketManager.resultData.freeSpins.isNewAdded)
            //{
            //    if (BonusSkipWinAnimation_Button) BonusSkipWinAnimation_Button.gameObject.SetActive(true);
            //}
        }
        else
        {
            if (audioController) audioController.StopWLAaudio();
        }

        if (LineId.Count > 0)
        {
            LineAnimRoutine = StartCoroutine(LineAnimationRoutine(LineId));
        }

        CheckSpinAudio = false;
    }

    private IEnumerator LineAnimationRoutine(List<int> LineIDs)
    {
        WinAnimationFin = false;

        yield return new WaitForSeconds(2f);

        TurnOnBlackBoxes();

        yield return new WaitForSeconds(2f);

        while (true)
        {
            for (int i = 0; i < LineIDs.Count; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (Tempimages[j].slotImages[SocketManager.LineData[LineIDs[i]][j]].GetComponent<ImageAnimation>().isAnim)
                    {
                        Tempimages[j].slotImages[SocketManager.LineData[LineIDs[i]][j]].transform.GetChild(2).gameObject.SetActive(false);
                        Tempimages[j].slotImages[SocketManager.LineData[LineIDs[i]][j]].GetComponent<ImageAnimation>().StartAnimation();
                    }
                }

                yield return new WaitForSeconds(1.5f);

                TurnOnBlackBoxes();

                if (LineIDs.Count < 2)
                {
                    yield return new WaitForSeconds(1.5f);
                    if (!WinAnimationFin) WinAnimationFin = true;
                }

                if(!WinAnimationFin) WinAnimationFin = true;
                yield return new WaitForSeconds(1.5f);
            }
        }
    }

    private void TurnOnBlackBoxes()
    {
        for (int i = 0; i < Tempimages.Count; i++)
        {
            for (int j = 0; j < Tempimages[i].slotImages.Count; j++)
            {
                Tempimages[i].slotImages[j].transform.GetChild(2).gameObject.SetActive(true);
                Tempimages[i].slotImages[j].GetComponent<ImageAnimation>().StopAnimation();
            }
        }
    }

    internal void CallCloseSocket()
    {
        SocketManager.CloseSocket();
    }

    void ToggleButtonGrp(bool toggle)
    {
        if (SlotStart_Button) SlotStart_Button.interactable = toggle;
        if (AutoSpin_Button && !IsAutoSpin) AutoSpin_Button.interactable = toggle;
        if (LineBetPlus_Button) LineBetPlus_Button.interactable = toggle;
        if (LineBetMinus_Button) LineBetMinus_Button.interactable = toggle;
        if (TotalBetPlus_Button) TotalBetPlus_Button.interactable = toggle;
        if (TotalBetMinus_Button) TotalBetMinus_Button.interactable = toggle;
    }

    //Start the icons animation
    private void StartGameAnimation(GameObject animObjects)
    {
        ImageAnimation temp = animObjects.GetComponent<ImageAnimation>();
        if (temp.textureArray.Count > 0)
        {
            temp.StartAnimation();
            temp.isAnim = true;
            animObjects.transform.GetChild(2).gameObject.SetActive(false);
        }
    }

    //Stop the icons animation
    internal void StopGameAnimation()
    {
        if (changedSlots.Count > 0)
        {
            ResetImages();
        }

        if (LineAnimRoutine != null)
        {
            StopCoroutine(LineAnimRoutine);
            LineAnimRoutine = null;
            WinAnimationFin = true;
        }

        for (int i = 0; i < Tempimages.Count; i++)
        {
            for (int j = 0; j < Tempimages[i].slotImages.Count; j++)
            {
                Tempimages[i].slotImages[j].transform.GetChild(2).gameObject.SetActive(false);
                Tempimages[i].slotImages[j].GetComponent<ImageAnimation>().StopAnimation();
                Tempimages[i].slotImages[j].GetComponent<ImageAnimation>().isAnim = false;
            }
        }
    }

    #region TweeningCode
    private void InitializeTweening(Transform slotTransform, bool bonus = false)
    {
        Tweener tweener = null;
        
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, slotTransform.localPosition.y + 440);
        tweener = slotTransform.DOLocalMoveY(-1436, .3f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetDelay(0);
        
        tweener.Play();
        alltweens.Add(tweener);
    }

    private IEnumerator StopTweening(int reqpos, Transform slotTransform, int index, bool magnet = false)
    {
        bool IsRegister = false;
        yield return alltweens[index].OnStepComplete(delegate { IsRegister = true; });
        yield return new WaitUntil(() => IsRegister);

        alltweens[index].Pause();

        int tweenpos = (reqpos * IconSizeFactor) - IconSizeFactor;
        if (!magnet)
        {
            alltweens[index] = slotTransform.DOLocalMoveY(tweenpos + 441.255f, 0.5f).SetEase(Ease.OutQuad); //1789
        }
        else
        {
            //write magnet code here
        }

        if (audioController) audioController.PlayWLAudio("spinStop");
        yield return alltweens[index].WaitForCompletion();
        alltweens[index].Kill();
    }

    private void KillAllTweens()
    {
        if(alltweens.Count > 0)
        {
            for (int i = 0; i < alltweens.Count; i++)
            {
                alltweens[i].Kill();
            }
            alltweens.Clear();
        }
    }
    #endregion

}

[Serializable]
public class SlotImage
{
    public List<Image> slotImages = new List<Image>(10);
}


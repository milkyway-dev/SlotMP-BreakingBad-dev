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
    [Header("Sprites")]
    [SerializeField] private Sprite[] myImages;  //images taken initially

    [Header("Slot Images")]
    [SerializeField] private List<SlotImage> images;     //class to store total images
    [SerializeField] private List<SlotImage> Tempimages;     //class to store the result matrix
    [SerializeField] private List<BoxScript> TempBoxScripts;
    [SerializeField] private List<Sprite> Box_Sprites;

    [Header("Slots Transforms")]
    [SerializeField] private Transform[] Slot_Transform;

    private Dictionary<int, string> y_string = new Dictionary<int, string>();

    [Header("Buttons")]
    [SerializeField] private Button SlotStart_Button;
    [SerializeField] private Button AutoSpin_Button;
    [SerializeField] private Button AutoSpinStop_Button;
    [SerializeField] private Button TotalBetPlus_Button;
    [SerializeField] private Button TotalBetMinus_Button;
    [SerializeField] private Button LineBetPlus_Button;
    [SerializeField] private Button LineBetMinus_Button;
    [SerializeField] private Button SkipWinAnimation_Button;
    [SerializeField] private Button BonusSkipWinAnimation_Button;

    [Header("Animated Sprites")]
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

    [Header("Miscellaneous UI")]
    [SerializeField] private TMP_Text Balance_text;
    [SerializeField] private TMP_Text TotalBet_text;
    [SerializeField] private TMP_Text LineBet_text;
    [SerializeField] private TMP_Text TotalWin_text;
    [SerializeField] private TMP_Text BigWin_Text;
    [SerializeField] private TMP_Text BonusWin_Text;


    [Header("Audio Management")]
    [SerializeField] private AudioController audioController;

    [SerializeField] private UIManager uiManager;

    [Header("BonusGame Popup")]
    [SerializeField] private BonusController _bonusManager;

    [Header("Free Spins Board")]
    [SerializeField] private GameObject FSBoard_Object;
    [SerializeField] private TMP_Text FSnum_text;

    int tweenHeight = 0;  //calculate the height at which tweening is done

    [SerializeField] private PayoutCalculation PayCalculator;

    private List<Tweener> alltweens = new List<Tweener>();

    private Tweener WinTween = null;

    [SerializeField] private List<ImageAnimation> TempList;  //stores the sprites whose animation is running at present 

    [SerializeField] private SocketIOManager SocketManager;

    private Coroutine AutoSpinRoutine = null;
    private Coroutine FreeSpinRoutine = null;
    private Coroutine tweenroutine;
    private Coroutine BoxAnimRoutine = null;

    private bool IsAutoSpin = false;
    private bool IsFreeSpin = false;
    private bool WinAnimationFin = true;
    private bool IsSpinning = false;
    private bool CheckSpinAudio = false;
    internal bool CheckPopups = false;
    private int BetCounter = 0;
    private double currentBalance = 0;
    private double currentTotalBet = 0;
    protected int Lines = 20;

    [SerializeField] private int IconSizeFactor = 100;       //set this parameter according to the size of the icon and spacing
    private int numberOfSlots = 5;          //number of columns

    private List<(Transform slotTransform, int originalSiblingIndex)> changedSlots = new();


    private void Start()
    {
        shuffleInitialMatrix(); //Done for testing. Remove this later

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

        if (SkipWinAnimation_Button) SkipWinAnimation_Button.onClick.RemoveAllListeners();
        if (SkipWinAnimation_Button) SkipWinAnimation_Button.onClick.AddListener(StopGameAnimation);

        if (BonusSkipWinAnimation_Button) BonusSkipWinAnimation_Button.onClick.RemoveAllListeners();
        if (BonusSkipWinAnimation_Button) BonusSkipWinAnimation_Button.onClick.AddListener(StopGameAnimation);

        if (FSBoard_Object) FSBoard_Object.SetActive(false);

        tweenHeight = (13 * IconSizeFactor) - 280;
        //Debug.Log("Tween Height: " + tweenHeight);
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
            if(BoxAnimRoutine!=null && !WinAnimationFin)
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

    #region FreeSpin
    internal void FreeSpin(int spins)
    {
        if (!IsFreeSpin)
        {
            if (FSnum_text) FSnum_text.text = spins.ToString();
            IsFreeSpin = true;
            ToggleButtonGrp(false);

            if (FreeSpinRoutine != null)
            {
                StopCoroutine(FreeSpinRoutine);
                FreeSpinRoutine = null;
            }
            FreeSpinRoutine = StartCoroutine(FreeSpinCoroutine(spins));
        }
    }

    private IEnumerator FreeSpinCoroutine(int spinchances)
    {
        int i = 0;
        int j = spinchances;
        while (i < spinchances)
        {
            j -= 1;
            if (FSnum_text) FSnum_text.text = j.ToString();

            StartSlots(false, true);

            yield return tweenroutine;
            yield return new WaitForSeconds(1f);
            i++;
        }
        ToggleButtonGrp(true);
        IsFreeSpin = false;
        StartCoroutine(_bonusManager.BonusGameEndRoutine());
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

    #region LinesCalculation

    //Destroy Static Lines from button hovers
    internal void DestroyStaticLine()
    {
        PayCalculator.ResetStaticLine();
    }
    #endregion

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
        //for (int i = 0; i < Tempimages.Count; i++)
        //{
        //    for (int j = 0; j < 3; j++)
        //    {
        //        int randomIndex = UnityEngine.Random.Range(0, 13);
        //        Tempimages[i].slotImages[j].sprite = myImages[randomIndex];
        //    }
        //}

        for(int i = 0; i < images.Count; i++)
        {
            for(int j = 0; j < myImages.Length; j++)
            {
                int randomIndex = UnityEngine.Random.Range(0, myImages.Length-8);
                images[i].slotImages[j].sprite = myImages[randomIndex];
            }
        }

        //ReorderImages(); commented out because its not set where its gon be used yet
    }

    private void ReorderImages()
    {
        for (int i = 0; i < Tempimages.Count; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (Tempimages[i].slotImages[j].sprite == myImages[3])
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

    private void OnApplicationFocus(bool focus)
    {
        audioController.CheckFocusFunction(focus, CheckSpinAudio);
    }

    //function to populate animation sprites accordingly
    private void PopulateAnimationSprites(ImageAnimation animScript, int val)
    {
        animScript.textureArray.Clear();
        animScript.textureArray.TrimExcess();
        switch (val)
        {
            //case 11:
            //    for(int i=0; i < Bonus_Sprite.Length; i++)
            //    {
            //        animScript.textureArray.Add(Bonus_Sprite[i]);
            //    }
            //    animScript.AnimationSpeed = 12f;
            //    break;

            //case 12:
            //    for(int i=0;i<Cleopatra_Sprite.Length; i++)
            //    {
            //        animScript.textureArray.Add(Cleopatra_Sprite[i]);
            //    }
            //    animScript.AnimationSpeed = 15f;
            //    break;
        }
    }

    #region SlotSpin
    //starts the spin process
    private void StartSlots(bool autoSpin = false, bool bonus = false)
    {
        if (audioController) audioController.PlaySpinButtonAudio();

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

        //StopGameAnimation(); //commented this line for testing
        
        tweenroutine = StartCoroutine(TweenRoutine(bonus));
    }

    //manage the Routine for spinning of the slots
    private IEnumerator TweenRoutine(bool bonus = false)
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
        //ToggleButtonGrp(false);

        for (int i = 0; i < numberOfSlots; i++) // Initialize tweening for slot animations
        {
            InitializeTweening(Slot_Transform[i]);
        }

        //if (!bonus) // Deduct balance if not a bonus
        //{
        //    BalanceDeduction();
        //}

        //SocketManager.AccumulateResult(BetCounter);
        //yield return new WaitUntil(() => SocketManager.isResultdone);

        //for (int j = 0; j < SocketManager.resultData.ResultReel.Count; j++) // Update slot images based on the results
        //{
        //    List<int> resultnum = SocketManager.resultData.FinalResultReel[j]?.Split(',')?.Select(Int32.Parse)?.ToList();
        //    for (int i = 0; i < 5; i++)
        //    {
        //        if (images[i].slotImages[images[i].slotImages.Count - 5 + j]) images[i].slotImages[images[i].slotImages.Count - 5 + j].sprite = myImages[resultnum[i]];
        //        PopulateAnimationSprites(images[i].slotImages[images[i].slotImages.Count - 5 + j].gameObject.GetComponent<ImageAnimation>(), resultnum[i]);
        //    }
        //}

        yield return new WaitForSeconds(2f);

        for (int i = 0; i < numberOfSlots; i++) // Stop tweening for each slot
        {
            yield return StopTweening(5, Slot_Transform[i], i);
        }

        

        yield return new WaitForSeconds(0.3f);

        //CheckPayoutLineBackend(SocketManager.resultData.linesToEmit, SocketManager.resultData.FinalsymbolsToEmit, SocketManager.resultData.jackpot);

        KillAllTweens();

        yield return new WaitForSeconds(2f);

        //_bonusManager.StartBonus(); this is for testing purposes, remove 

        SlotStart_Button.interactable = true; //this is for testing, remove this line 

        //if (SocketManager.playerdata.currentWining>0) WinningsTextAnimation(bonus); // Trigger winnings animation if applicable

        //CheckPopups = true;

        //if (SocketManager.resultData.jackpot > 0) // Check for jackpot or winnings popups
        //{
        //    uiManager.PopulateWin(4); 
        //}
        //else
        //{
        //    CheckWinPopups();
        //}

        //if(SocketManager.playerdata.currentWining <= 0 && SocketManager.resultData.jackpot <= 0 && !SocketManager.resultData.freeSpins.isNewAdded)
        //{
        //    audioController.PlayWLAudio("lose");
        //}

        //currentBalance = SocketManager.playerdata.Balance;
        //yield return new WaitUntil(() => !CheckPopups);

        //if (IsFreeSpin && BoxAnimRoutine != null && !WinAnimationFin) // Waits for winning payline animation to finish when triggered bonus
        //{
        //    yield return new WaitUntil(() => WinAnimationFin);
        //    //yield return new WaitForSeconds(0.5f);
        //    StopGameAnimation();
        //}

        //if (SocketManager.resultData.freeSpins.isNewAdded)
        //{
        //    Debug.Log(IsFreeSpin ? "Bonus In Bonus" : "First Time Bonus");

        //    yield return new WaitForSeconds(1.5f);

        //    if (BoxAnimRoutine != null && !WinAnimationFin)
        //    {
        //        yield return new WaitUntil(() => WinAnimationFin);
        //        StopGameAnimation();
        //    }

        //    yield return new WaitForSeconds(1f);

        //    if (!IsFreeSpin)
        //    {
        //        _bonusManager.StartBonus(SocketManager.resultData.freeSpins.count);
        //    }
        //    else
        //    {
        //        IsFreeSpin = false;
        //        yield return StartCoroutine(_bonusManager.BonusInBonus());
        //    }

        //    if (IsAutoSpin)
        //    {
        //        IsSpinning = false;
        //        StopAutoSpin();
        //    }

        //}

        //if (!IsAutoSpin && !IsFreeSpin) // Reset spinning state and toggle buttons
        //{
        //    ToggleButtonGrp(true);
        //    IsSpinning = false;
        //}
        //else
        //{
        //    IsSpinning = false;
        //    yield return new WaitForSeconds(2f);
        //}
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
        else if (SocketManager.resultData.WinAmout >= currentTotalBet * 15)
        {
            uiManager.PopulateWin(3);
        }
        else
        {
            CheckPopups = false;
        }
    }

    private void WinningsTextAnimation(bool bonus = false)
    {
        double winAmt = 0;
        double currentWin = 0;

        double currentBal = 0;
        double Balance = 0;

        double BonusWinAmt = 0;
        double currentBonusWinnings = 0;

        if (bonus)
        {
            try
            {
                BonusWinAmt = double.Parse(SocketManager.playerdata.currentWining.ToString("f2"));
                currentBonusWinnings = double.Parse(BonusWin_Text.text);
            }
            catch (Exception e)
            {
                Debug.Log("Error while conversion " + e.Message);
            }
        }
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

        if (bonus)
        {
            double CurrTotal = BonusWinAmt + currentBonusWinnings;
            DOTween.To(() => currentBonusWinnings, (val) => currentBonusWinnings = val, CurrTotal, 0.8f).OnUpdate(() =>
            {
                if (BonusWin_Text) BonusWin_Text.text = currentBonusWinnings.ToString("f2");
            });

            double start = 0;
            DOTween.To(() => start, (val) => start = val, BonusWinAmt, 0.8f).OnUpdate(() =>
            {
                if (BigWin_Text) BigWin_Text.text = start.ToString("f2");
            });
        }
        else
        {
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
            if (jackpot > 0)
            {
                if (audioController) audioController.PlayWLAudio("megaWin");
                for (int i = 0; i < Tempimages.Count; i++)
                {
                    for (int k = 0; k < Tempimages[i].slotImages.Count; k++)
                    {
                        StartGameAnimation(Tempimages[i].slotImages[k].gameObject, TempBoxScripts[i].boxScripts[k]);
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
                            StartGameAnimation(Tempimages[(points_anim[k] / 10) % 10].slotImages[points_anim[k] % 10].gameObject, TempBoxScripts[(points_anim[k] / 10) % 10].boxScripts[points_anim[k] % 10]);
                        }
                        else
                        {
                            StartGameAnimation(Tempimages[0].slotImages[points_anim[k]].gameObject, TempBoxScripts[0].boxScripts[points_anim[k]]);
                        }
                    }
                }
            }

            if (!SocketManager.resultData.freeSpins.isNewAdded)
            {
                if (SkipWinAnimation_Button) SkipWinAnimation_Button.gameObject.SetActive(true);
            }

            if (IsFreeSpin && !SocketManager.resultData.freeSpins.isNewAdded)
            {
                if (BonusSkipWinAnimation_Button) BonusSkipWinAnimation_Button.gameObject.SetActive(true);
            }
        }
        else
        {
            if (audioController) audioController.StopWLAaudio();
        }

        if (LineId.Count > 0)
        {
            BoxAnimRoutine = StartCoroutine(BoxRoutine(LineId));
        }

        CheckSpinAudio = false;
    }

    private IEnumerator BoxRoutine(List<int> LineIDs)
    {
        WinAnimationFin = false;
        while (true)
        {
            for (int i = 0; i < LineIDs.Count; i++)
            {
                PayCalculator.GeneratePayoutLinesBackend(LineIDs[i]);
                PayCalculator.DontDestroyLines.Add(LineIDs[i]);
                for (int s = 0; s < 5; s++)
                {
                    if (TempBoxScripts[s].boxScripts[SocketManager.LineData[LineIDs[i]][s]].isAnim)
                    {
                        TempBoxScripts[s].boxScripts[SocketManager.LineData[LineIDs[i]][s]].SetBG(Box_Sprites[LineIDs[i]]);
                    }
                }
                if (LineIDs.Count < 2)
                {
                    WinAnimationFin = true;
                    yield return new WaitForSeconds(2f);
                    yield break;
                }
                yield return new WaitForSeconds(2f);
                for (int s = 0; s < 5; s++)
                {
                    if (TempBoxScripts[s].boxScripts[SocketManager.LineData[LineIDs[i]][s]].isAnim)
                    {
                        TempBoxScripts[s].boxScripts[SocketManager.LineData[LineIDs[i]][s]].ResetBG();
                    }
                }
                PayCalculator.DontDestroyLines.Clear();
                PayCalculator.DontDestroyLines.TrimExcess();
                PayCalculator.ResetStaticLine();
            }
            for (int i = 0; i < LineIDs.Count; i++)
            {
                PayCalculator.GeneratePayoutLinesBackend(LineIDs[i]);
                PayCalculator.DontDestroyLines.Add(LineIDs[i]);
            }
            yield return new WaitForSeconds(3f);
            PayCalculator.DontDestroyLines.Clear();
            PayCalculator.DontDestroyLines.TrimExcess();
            PayCalculator.ResetStaticLine();
            WinAnimationFin = true;
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
        if (BetCounter != 0)
        {
            if (LineBetMinus_Button) LineBetMinus_Button.interactable = toggle;
            if (TotalBetMinus_Button) TotalBetMinus_Button.interactable = toggle;
        }
        if(BetCounter < SocketManager.initialData.Bets.Count - 1)
        {
            if (LineBetPlus_Button) LineBetPlus_Button.interactable = toggle;
            if (TotalBetPlus_Button) TotalBetPlus_Button.interactable = toggle;
        }
    }

    //Start the icons animation
    private void StartGameAnimation(GameObject animObjects, BoxScripting boxscript)
    {
        ImageAnimation temp = animObjects.GetComponent<ImageAnimation>();
        if (temp.textureArray.Count > 0)
        {
            temp.StartAnimation();
            TempList.Add(temp);
        }
        boxscript.isAnim = true;
    }

    //Stop the icons animation
    internal void StopGameAnimation()
    {
        if (BoxAnimRoutine != null)
        {
            StopCoroutine(BoxAnimRoutine);
            BoxAnimRoutine = null;
            WinAnimationFin = true;
        }

        if (TempBoxScripts.Count > 0)
        {
            for (int i = 0; i < TempBoxScripts.Count; i++)
            {
                foreach (BoxScripting b in TempBoxScripts[i].boxScripts)
                {
                    b.isAnim = false;
                    b.ResetBG();
                }
            }
        }
        
        if (SkipWinAnimation_Button) SkipWinAnimation_Button.gameObject.SetActive(false);
        if (BonusSkipWinAnimation_Button) BonusSkipWinAnimation_Button.gameObject.SetActive(false);

        if (TempList.Count > 0)
        {
            for (int i = 0; i < TempList.Count; i++)
            {
                TempList[i].StopAnimation();
            }
            TempList.Clear();
            TempList.TrimExcess();
        }

        PayCalculator.DontDestroyLines.Clear();
        PayCalculator.DontDestroyLines.TrimExcess();
        PayCalculator.ResetStaticLine();
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

[Serializable]
public class BoxScript
{
    public List<BoxScripting> boxScripts = new List<BoxScripting>(10);
}
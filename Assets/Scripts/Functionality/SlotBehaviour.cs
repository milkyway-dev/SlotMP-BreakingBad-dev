using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System;
using System.Net.Sockets;
using System.Linq.Expressions;
using Best.SocketIO;
using Unity.VisualScripting;
using System.Runtime.InteropServices;

public class SlotBehaviour : MonoBehaviour
{
    [Header("Script References")]
    [SerializeField] private SocketIOManager SocketManager;
    [SerializeField] private StaticSymbolController staticSymbolController;
    [SerializeField] private AudioController audioController;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private BonusController _bonusManager;

    [Header("Sprites References")]
    [SerializeField] private Sprite[] SlotSymbols;  //images taken initially
    [SerializeField] private Sprite[] losPollosSprites;
    [SerializeField] private Sprite[] losPollosNumberSprites;
    [SerializeField] private Sprite losPollosNoNumberSprite;
    [SerializeField] private Sprite[] JackpotSlotSymbols;


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
    [SerializeField] private TMP_Text CoinWinning_Text;
    [SerializeField] private TMP_Text FSnum_text;

    [SerializeField] private ImageAnimation BonusImageAnimation;
    [SerializeField] private ImageAnimation FreeGamesImageAnimation;
    [SerializeField] private ImageAnimation LeftMagnetImageAnimation;
    [SerializeField] private ImageAnimation RightMagnetImageAnimation;

    [SerializeField] private CanvasGroup FreeSpinsUI_Panel;
    [SerializeField] private CanvasGroup WinningsUI_Panel;
    [SerializeField] private CanvasGroup TopPayoutUI_CG;
    [SerializeField] private CanvasGroup LinesUI;
    [SerializeField] private CanvasGroup TotalBetUI;
    [SerializeField] private CanvasGroup LineBetUI;
    [SerializeField] private RectTransform FreeSpinCountUIPositon;
    [SerializeField] private Transform AnimationParent;

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
    [SerializeField] private Sprite[] MagnetInSprites;
    [SerializeField] private Sprite[] MagnetLightening_Sprites;

    private List<Tweener> alltweens = new List<Tweener>();
    private List<(Transform slotTransform, int originalSiblingIndex)> changedSlots = new();  //hold the reordered result matrix slots to show the fire animation

    private bool IsAutoSpin = false;
    private bool IsFreeSpin = false;
    internal bool IsBonus = false;
    private bool WinAnimationFin = true;
    private bool IsSpinning = false;
    private bool CheckSpinAudio = false;
    internal bool CheckPopups = false;

    private Coroutine AutoSpinRoutine = null;
    private Coroutine FreeSpinRoutine = null; //CAN BE REMOVED
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
    internal double currentLineBet = 0;

    private void Start()
    {
        IsAutoSpin = false;

        if (SlotStart_Button) SlotStart_Button.onClick.RemoveAllListeners();
        if (SlotStart_Button) SlotStart_Button.onClick.AddListener(delegate { StartSlots(); uiManager.CanCloseMenu();});

        if (TotalBetPlus_Button) TotalBetPlus_Button.onClick.RemoveAllListeners();
        if (TotalBetPlus_Button) TotalBetPlus_Button.onClick.AddListener(delegate { ChangeBet(true); uiManager.CanCloseMenu();});

        if (TotalBetMinus_Button) TotalBetMinus_Button.onClick.RemoveAllListeners();
        if (TotalBetMinus_Button) TotalBetMinus_Button.onClick.AddListener(delegate { ChangeBet(false); uiManager.CanCloseMenu();});

        if (LineBetPlus_Button) LineBetPlus_Button.onClick.RemoveAllListeners();
        if (LineBetPlus_Button) LineBetPlus_Button.onClick.AddListener(delegate { ChangeBet(true); uiManager.CanCloseMenu();});

        if (LineBetMinus_Button) LineBetMinus_Button.onClick.RemoveAllListeners();
        if (LineBetMinus_Button) LineBetMinus_Button.onClick.AddListener(delegate { ChangeBet(false); uiManager.CanCloseMenu();});

        if (AutoSpin_Button) AutoSpin_Button.onClick.RemoveAllListeners();
        if (AutoSpin_Button) AutoSpin_Button.onClick.AddListener(delegate {AutoSpin(); uiManager.CanCloseMenu();});

        if (AutoSpinStop_Button) AutoSpinStop_Button.onClick.RemoveAllListeners();
        if (AutoSpinStop_Button) AutoSpinStop_Button.onClick.AddListener(delegate {StopAutoSpin(); uiManager.CanCloseMenu();});

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
            IsAutoSpin = false;
            if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(false);
            if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(false);
            StartCoroutine(StopAutoSpinCoroutine());
        }
    }

    private IEnumerator AutoSpinCoroutine()
    {
        while (IsAutoSpin)
        {
            StartSlots(IsAutoSpin);
            yield return tweenroutine;
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator StopAutoSpinCoroutine()
    {
        yield return new WaitUntil(() => !IsSpinning);
        if (AutoSpinRoutine != null || tweenroutine != null)
        {
            StopCoroutine(AutoSpinRoutine);
            StopCoroutine(tweenroutine);
            tweenroutine = null;
            AutoSpinRoutine = null;
            if(!IsBonus) ToggleButtonGrp(true);
            StopCoroutine(StopAutoSpinCoroutine());
        }
    }
    #endregion

    private void CompareBalance()
    {
        if (currentBalance < currentTotalBet)
        {
            uiManager.LowBalPopup();
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
        currentLineBet = SocketManager.initialData.Bets[BetCounter];
        uiManager.PopulateTopSymbolsPayout();
        // CompareBalance();
    }

    #region InitialFunctions
    internal void shuffleInitialMatrix()
    {
        for(int i=0;i<images.Count;i++){
            for(int j=0;j<images[i].slotImages.Count;j++){
                int randomIndex=UnityEngine.Random.Range(0, SlotSymbols.Length-8);
                images[i].slotImages[j].sprite=SlotSymbols[randomIndex];
            }
        }
    }

    internal void SetInitialUI()
    {
        BetCounter = 0;
        if (LineBet_text) LineBet_text.text = SocketManager.initialData.Bets[BetCounter].ToString();
        if (TotalBet_text) TotalBet_text.text = (SocketManager.initialData.Bets[BetCounter] * Lines).ToString();
        if (TotalWin_text) TotalWin_text.text = "0.000";
        if (Balance_text) Balance_text.text = SocketManager.playerdata.Balance.ToString("f2");
        currentBalance = SocketManager.playerdata.Balance;
        currentTotalBet = SocketManager.initialData.Bets[BetCounter] * Lines;
        currentLineBet = SocketManager.initialData.Bets[BetCounter];
        shuffleInitialMatrix();
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
                if (Tempimages[i].slotImages[j].sprite == SlotSymbols[13]) //if the symbol is cash collect
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
        Debug.Log("Here");
        slotTransform.SetSiblingIndex(17);

        slotTransform.GetComponent<Mask>().enabled =false;
        for (int i = 0; i < 2; i++)
        {
            var animation = slotTransform.GetChild(i).GetComponent<ImageAnimation>();
            if (animation != null)
            {
                animation.AnimationSpeed = 15;  // Change animation speed
                Image image = slotTransform.GetChild(i).GetComponent<Image>();
                image.DOFade(0,0);
                image.gameObject.SetActive(true);  // Activate the animation object
                image.DOFade(1,0.5f);
                animation.StartAnimation();     // Start animation
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

            slotTransform.GetComponent<Mask>().enabled=true;
            // Stop the animation and reset the state
            for (int i = 0; i < 2; i++)
            {
                var animation = slotTransform.GetChild(i).GetComponent<ImageAnimation>();
                if (animation != null)
                {
                    animation.StopAnimation();  // Assuming you have a StopAnimation method
                    animation.rendererDelegate.DOFade(1, 0.5f).OnComplete(()=> {
                        animation.gameObject.SetActive(false);
                    });
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
        // animScript.doLoopAnimation=true;
        switch (val)
        {
            case 0:
                for(int i = 0; i < C_Sprites.Length; i++)
                {
                    animScript.textureArray.Add(C_Sprites[i]);
                }
                animScript.AnimationSpeed = 19f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 1:
                for (int i = 0; i < O_Sprites.Length; i++)
                {
                    animScript.textureArray.Add(O_Sprites[i]);
                }
                animScript.AnimationSpeed = 19f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 2:
                foreach(Sprite sprite in N_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 19f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 3:
                foreach (Sprite sprite in B_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 19f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 4:
                foreach (Sprite sprite in Barrel_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 16f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 5:
                foreach (Sprite sprite in Bus_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 16f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 6:
                foreach (Sprite sprite in Orange_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 16f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 7:
                foreach (Sprite sprite in Purple_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 16f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 8:
                foreach (Sprite sprite in Blue_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 16f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 10:
                foreach (Sprite sprite in Yellow_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 16f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 11:
                foreach (Sprite sprite in Link_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 19f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 12:
                foreach (Sprite sprite in MegaLink_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 12f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 13:
                foreach (Sprite sprite in CC_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 12f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 14:
                foreach (Sprite sprite in GoldCoin_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 19f;
                animScript.transform.GetChild(3).GetComponent<TMP_Text>().text = coin;
                animScript.transform.GetChild(3).gameObject.SetActive(true);
                break;
            case 15:
                foreach (Sprite sprite in Diamond_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 16f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 16:
                if(LP == 2)
                {
                    foreach(Sprite sprite in LP2_Sprites)
                    {
                        animScript.textureArray.Add(sprite);
                    }
                }
                else if(LP == 3)
                {
                    foreach (Sprite sprite in LP3_Sprites)
                    {
                        animScript.textureArray.Add(sprite);
                    }
                }
                else if(LP == 4)
                {
                    foreach (Sprite sprite in LP4_Sprites)
                    {
                        animScript.textureArray.Add(sprite);
                    }
                }
                else if(LP == 5)
                {
                    foreach (Sprite sprite in LP5_Sprites)
                    {
                        animScript.textureArray.Add(sprite);
                    }
                }
                else if(LP == 7)
                {
                    foreach (Sprite sprite in LP7_Sprites)
                    {
                        animScript.textureArray.Add(sprite);
                    }
                }
                else{
                    Debug.LogError("LP index value sent was wrong");
                }
                animScript.AnimationSpeed = 9f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
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
            freeSpinsCount-=1;
            FSnum_text.text = freeSpinsCount.ToString();
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
        if (LineBetPlus_Button && LineBetPlus_Button.interactable!=false) LineBetPlus_Button.interactable = false;
        if (LineBetMinus_Button && LineBetMinus_Button.interactable!=false) LineBetMinus_Button.interactable = false;
        if (TotalBetPlus_Button && TotalBetPlus_Button.interactable!=false) TotalBetPlus_Button.interactable = false;
        if (TotalBetMinus_Button && TotalBetMinus_Button.interactable!=false) TotalBetMinus_Button.interactable = false;
        if (TotalWin_text) TotalWin_text.text = "0.000";

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

        if (!IsFreeSpin)
        {
            BalanceDeduction();
        }

        for (int i = 0; i < numberOfSlots; i++) // Initialize tweening for slot animations
        {
            InitializeTweening(Slot_Transform[i]);
        }
        
        SocketManager.AccumulateResult(BetCounter);
        yield return new WaitUntil(() => SocketManager.isResultdone);
        currentBalance = SocketManager.playerdata.Balance;

        yield return PopulateResultMatrix();

        bool magnetAnim = false;
        int slotIndex = 0;
        int ccIndex = 0;
        if(SocketManager.resultData.isCoinCollect && UnityEngine.Random.Range(0f ,1f)>=0.85f){
            for(int i = 0 ; i < 3 ; i++){
                if(Tempimages[0].slotImages[i].sprite == SlotSymbols[13] && (i == 0||i==1)){
                    magnetAnim = true;
                    slotIndex = 0;
                    ccIndex = i;
                    break;
                }
            }
            if(!magnetAnim){
                for(int i =0 ;i<3;i++){
                    if(Tempimages[4].slotImages[i].sprite == SlotSymbols[13] && (i == 0||i==1)){
                        magnetAnim = true;
                        slotIndex = 4;
                        ccIndex = i;
                        break;
                    }
                }
            }
        }

        for (int i = 0; i < numberOfSlots; i++) // Stop tweening for each slot
        {
            if(!magnetAnim){
                yield return StopTweening(5, Slot_Transform[i], i);
            }
            else {
                if(i == slotIndex)
                    yield return StopTweening(5, Slot_Transform[i], i, magnetAnim, ccIndex);
                else
                    yield return StopTweening(5, Slot_Transform[i], i);
            }
        }

        KillAllTweens();

        yield return StartSpecialSymbolAnimations();

        CheckPayoutLineBackend(SocketManager.resultData.linesToEmit, SocketManager.resultData.FinalsymbolsToEmit, SocketManager.resultData.jackpot.payout);

        if(IsAutoSpin || SocketManager.resultData.isCoinCollect || SocketManager.resultData.bonus.isBonus || SocketManager.resultData.jackpot.isTriggered || SocketManager.resultData.freeSpins.isNewAdded){
            yield return new WaitUntil(() => WinAnimationFin);
            StopGameAnimation();
            yield return new WaitForSeconds(.5f);
        }

        if(SocketManager.resultData.jackpot.isTriggered){
            yield return new WaitForSeconds(.5f);
            bool Jackpottriggered = false;
            for(int i = 0 ; i < ResultMatrix.Count && !Jackpottriggered; i++){
                for(int j = 0 ; j < ResultMatrix[i].slotImages.Count && !Jackpottriggered; j++){
                    if(SocketManager.resultData.ResultReel[i][j] == "15"){
                        Jackpottriggered = true;
                        yield return PlayJackpotAnimation(ResultMatrix[i].slotImages[j].rectTransform);
                        break;
                    }
                }
            }
            yield return new WaitForSeconds(.5f);
        }

        if(SocketManager.resultData.isCoinCollect){
            yield return new WaitForSeconds(.5f);
            WinningsUI_Panel.DOFade(1, 0.3f);
            int ccCount = 0;
            for(int k=0;k<Tempimages[0].slotImages.Count;k++){
                if(Tempimages[0].slotImages[k].sprite == SlotSymbols[13]){
                    ccCount++;
                }
            }
            for(int k=0;k<Tempimages[^1].slotImages.Count;k++){
                if(Tempimages[^1].slotImages[k].sprite == SlotSymbols[13]){
                    ccCount++;
                }
            }
            if(ccCount==0){
                Debug.Log("CC count 0");
                ccCount=1;
            }
            switchTopUI(true);
            for(int i = 0;i< ResultMatrix.Count;i++){
                for(int j = 0; j<ResultMatrix[i].slotImages.Count; j++){
                    if(ResultMatrix[i].slotImages[j].sprite == SlotSymbols[14]){
                        yield return uiManager.TrailRendererAnimation(ResultMatrix[i].slotImages[j].transform.GetChild(5).gameObject, 3, ccCount);
                    }
                }
            }
            yield return new WaitForSeconds(.5f);
            switchTopUI(false);
            WinningsUI_Panel.DOFade(0, 0.3f).OnComplete(()=> { CoinWinning_Text.text = "0"; });

            yield return new WaitForSeconds(.5f);
        }

        if (SocketManager.resultData.freeSpins.isNewAdded)
        {
            yield return new WaitForSeconds(.5f);
            yield return ResetUI();

            OpenFreeSpinsUI();
            IsFreeSpin = true;

            int extraFreeSpin = 0;
            yield return new WaitForSeconds(.5f);
            if(SocketManager.resultData.freeSpins.count>freeSpinsCount){
                yield return FreeSpinsSymbolAnimation();
                extraFreeSpin = SocketManager.resultData.freeSpins.count-freeSpinsCount;
            }
            
            freeSpinsCount = SocketManager.resultData.freeSpins.count;
            yield return new WaitForSeconds(1f); // Optional delay for UI stability
            
            if(extraFreeSpin!=0){
                yield return uiManager.MidGameImageAnimation(FreeGamesImageAnimation, extraFreeSpin);
            }
            else{
                yield return uiManager.MidGameImageAnimation(FreeGamesImageAnimation, freeSpinsCount);
            }
            yield return new WaitForSeconds(.5f);
        }

        if (SocketManager.resultData.bonus.isBonus)
        {
            yield return new WaitForSeconds(.5f);
            if(SocketManager.playerdata.currentWining>0){
                CheckPopups = true;
                WinningsTextAnimation();
                CheckWinPopups();

                yield return new WaitUntil(()=> !CheckPopups);
                yield return new WaitForSeconds(2f);
            }
            IsBonus=true;
            yield return ResetUI();

            yield return new WaitForSeconds(.5f);
            // Only Bonus is awarded without Free Spins, directly trigger bonus round
            yield return uiManager.MidGameImageAnimation(BonusImageAnimation);

            staticSymbolController.TurnOnIndices(GenerateFreezedLocations());
            yield return new WaitForSeconds(.5f);
            _bonusManager.StartBonus(SocketManager.resultData.bonus.spinCount);
            IsSpinning= false;
            yield break;
        }


        if(SocketManager.playerdata.currentWining>0){
            CheckPopups = true;
            WinningsTextAnimation();
            CheckWinPopups();

            yield return new WaitUntil(()=> !CheckPopups);
            yield return new WaitForSeconds(.5f);
        }

        if(SocketManager.playerdata.currentWining <= 0)
        {
           audioController.PlayWLAudio("lose");
        }

        // Post-bonus and free spins cleanup
        ToggleButtonGrp(true);

        if(freeSpinsCount<=0 && SocketManager.resultData.freeSpins.count<=0 && !SocketManager.resultData.freeSpins.isNewAdded){
            CloseFreeSpinsUI();
        }
        IsSpinning = false;
    }
    #endregion

    private void switchTopUI(bool trigger){
        if(trigger){
            TopPayoutUI_CG.DOFade(0, 0.5f);
            WinningsUI_Panel.DOFade(1, 0.5f);
        }
        else{
            TopPayoutUI_CG.DOFade(1, 0.5f);
            WinningsUI_Panel.DOFade(0, 0.5f);
        }
    }

    private IEnumerator ResetUI(){
        if (TotalWin_text) {
            yield return new WaitForSeconds(.5f);
            TotalWin_text.text = "0.000";
        }
        if (IsAutoSpin)
        {
            StopAutoSpin();
        }
        StopGameAnimation();
    }

    private IEnumerator StartSpecialSymbolAnimations(){
        List<ImageAnimation> imageAnimations = new();
        for(int i = 0;i < ResultMatrix.Count;i++){
            for(int j=0;j<ResultMatrix[i].slotImages.Count; j++){
                if(SocketManager.resultData.ResultReel[i][j] == "11" ||
                SocketManager.resultData.ResultReel[i][j] == "12" ||
                SocketManager.resultData.ResultReel[i][j] == "13" ||
                SocketManager.resultData.ResultReel[i][j] == "14" ||
                SocketManager.resultData.ResultReel[i][j] == "15" ||
                SocketManager.resultData.ResultReel[i][j] == "16" ){
                    imageAnimations.Add(ResultMatrix[i].slotImages[j].GetComponent<ImageAnimation>());
                    ResultMatrix[i].slotImages[j].GetComponent<ImageAnimation>().StartAnimation();
                }
            }
        }
        if(imageAnimations.Count>0){
            yield return new WaitUntil(()=> imageAnimations[^1].textureArray[^1] == imageAnimations[^1].rendererDelegate.sprite);
            foreach(ImageAnimation animation in imageAnimations){
                animation.StopAnimation();
            }
            yield return new WaitForSeconds(1f);
        } 
        
    } 

    private List<List<int>> GenerateFreezedLocations(){
        List<List<int>> loc = new();
        for(int i=0;i<ResultMatrix.Count;i++){
            for(int j=0;j<ResultMatrix[i].slotImages.Count;j++){
                if(ResultMatrix[i].slotImages[j].sprite == SlotSymbols[11] || 
                    ResultMatrix[i].slotImages[j].sprite == SlotSymbols[12] || 
                    ResultMatrix[i].slotImages[j].sprite == SlotSymbols[13]){
                    List<int> rXc = new() {i, j};
                    loc.Add(rXc);
                }
            }
        }
        return loc;
    }

    private IEnumerator PlayJackpotAnimation(RectTransform RectTransform){
        Transform tempParent = RectTransform.parent;
        Vector3 tempPosition = RectTransform.localPosition;
        int TempSiblingIndex = RectTransform.GetSiblingIndex();
        Vector2 tempAnchorMin = RectTransform.anchorMin;
        Vector2 tempAnchorMax = RectTransform.anchorMax;

        RectTransform.SetParent(AnimationParent);
        Vector3 tempWorldPosi = RectTransform.position;
        RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        RectTransform.position = tempWorldPosi;
        RectTransform.DOLocalMove(Vector3.zero, 1.5f);
        yield return RectTransform.DOScale(2.5f, 1.5f).WaitForCompletion();

        yield return new WaitForSeconds(1f);
        Transform jackpotSlotTransform = RectTransform.GetChild(6);
        Image ResultImage = jackpotSlotTransform.GetChild(7).GetComponent<Image>();
        jackpotSlotTransform.GetComponent<CanvasGroup>().DOFade(1, 0.8f);

        jackpotSlotTransform.localPosition = new Vector2(jackpotSlotTransform.localPosition.x, 151f);
        audioController.PlaySpinButtonAudio();
        Tween JackpotTween = jackpotSlotTransform.DOLocalMoveY(-151f, .3f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear);
        JackpotTween.Play();
        yield return new WaitForSeconds(2f);
        bool found = false;
        for(int i = 0 ; i < SocketManager.initialData.Jackpot.Count ; i ++){
            if(SocketManager.initialData.Jackpot[i]*currentLineBet == SocketManager.resultData.jackpot.payout && !found){
                found = true;
                ResultImage.sprite = JackpotSlotSymbols[i];
            }
        }
        if(!found){
            Debug.Log("Error while finding payout");
        }
        bool IsFin = false;
        JackpotTween.OnStepComplete(()=> { IsFin =true;});
        yield return new WaitUntil(()=> IsFin);

        JackpotTween.Kill();
        yield return jackpotSlotTransform.DOLocalMoveY(65.86f, .3f).SetEase(Ease.OutQuad).WaitForCompletion();
        audioController.PlayWLAudio("megaWin");
        yield return new WaitForSeconds(1f);        
        
        JackpotWinnings();      
        // yield return uiManager.MidGameImageAnimation(YouWinImageAnimation, SocketManager.resultData.jackpot.payout);

        yield return new WaitForSeconds(1f);

        yield return jackpotSlotTransform.GetComponent<CanvasGroup>().DOFade(0, 0.5f).WaitForCompletion();
        yield return new WaitForSeconds(1f);
        yield return RectTransform.GetComponent<Image>().DOFade(0, 1f).WaitForCompletion();
        // RectTransform.GetComponent<Mask>().showMaskGraphic = true;
        RectTransform.DOScale(1f, 0f);
        RectTransform.SetParent(tempParent);
        RectTransform.DOLocalMove(tempPosition, 0f);
        RectTransform.SetSiblingIndex(TempSiblingIndex);
        RectTransform.anchorMin = tempAnchorMin;
        RectTransform.anchorMax = tempAnchorMax;
        RectTransform.position = tempWorldPosi;
        RectTransform.GetComponent<Image>().DOFade(1, 1f);
    }

    private void JackpotWinnings(){
        double start = 0;
        DOTween.To(()=> start, (val)=> start = val, SocketManager.resultData.jackpot.payout, 0.5f).OnUpdate(()=>{
            TotalWin_text.text = start.ToString("F3");
        });
    }

    private IEnumerator PopulateResultMatrix(){
        int CCcount = 0;
        for (int j = 0; j < SocketManager.resultData.ResultReel.Count; j++) // Update slot images based on the results
        {
            List<int> resultnum = SocketManager.resultData.FinalResultReel[j]?.Split(',')?.Select(Int32.Parse)?.ToList();
            for (int i = 0; i < 5; i++) //Loop through each column
            {
                if (ResultMatrix[j].slotImages[i]) //Checking if the required object is not null
                {
                    Image SlotImage=ResultMatrix[j].slotImages[i];
                    if(resultnum[i] == 16) //LP coin (Free spin)
                    {
                        if (SocketManager.resultData.winData.losPollos.Count > 0)
                        {
                            foreach (var losPollos in SocketManager.resultData.winData.losPollos)
                            {
                                if (losPollos.index[0] == j && losPollos.index[1] == i)
                                {
                                    SlotImage.sprite = losPollosSprites[losPollos.value];
                                    PopulateAnimationSprites(SlotImage.gameObject.GetComponent<ImageAnimation>(), resultnum[i], losPollos.value);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            int[] tempIndex = { 2, 3, 4, 5, 7 };
                            int randomIndex = tempIndex[UnityEngine.Random.Range(0, tempIndex.Length)];
                            SlotImage.sprite = losPollosSprites[randomIndex];
                            PopulateAnimationSprites(SlotImage.gameObject.GetComponent<ImageAnimation>(), resultnum[i], randomIndex);
                        }
                    }
                    else if (resultnum[i] == 14) //gold coin
                    {
                        foreach(var coins in SocketManager.resultData.winData.coinValues)
                        {
                            if(coins.index[0] == j && coins.index[1] == i)
                            {
                                SlotImage.sprite = SlotSymbols[resultnum[i]];
                                PopulateAnimationSprites(SlotImage.gameObject.GetComponent<ImageAnimation>(), resultnum[i], 0, coins.value.ToString("f2"));
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
                        SlotImage.sprite = SlotSymbols[resultnum[i]];
                        PopulateAnimationSprites(SlotImage.gameObject.GetComponent<ImageAnimation>(), resultnum[i]);
                    }
                }
            }
        }

        yield return new WaitForSeconds(.5f);
        if (CCcount != 0)
        {
            ReorderImages();
        }
    }

    internal void CheckWinPopups()
    {
        if (SocketManager.playerdata.currentWining >= currentTotalBet * 5 && SocketManager.playerdata.currentWining < currentTotalBet * 10)
        {
            uiManager.PopulateWin(1);
        }
        else if (SocketManager.playerdata.currentWining >= currentTotalBet * 10)
        {
            uiManager.PopulateWin(2);
        }
        else
        {
            CheckPopups = false;
        }
    }

    private IEnumerator FreeSpinsSymbolAnimation(){
        yield return new WaitForSeconds(1.5f);
        for(int i = 0;i<SocketManager.resultData.winData.losPollos.Count;i++){
            Debug.Log("Looping throght lp");
            LosPollos lp = SocketManager.resultData.winData.losPollos[i];
            if(lp!=null && ResultMatrix[lp.index[0]].slotImages[lp.index[1]]!=null){
                Debug.Log("Found lp on result matrix");
                Image LosPollosImage = ResultMatrix[lp.index[0]].slotImages[lp.index[1]];
                RectTransform freeSpinNumberTransform = LosPollosImage.transform.GetChild(4).GetComponent<RectTransform>();
                freeSpinNumberTransform.GetComponent<Image>().sprite=losPollosNumberSprites[lp.value];
                freeSpinNumberTransform.gameObject.SetActive(true);
                LosPollosImage.sprite = losPollosNoNumberSprite;

                Vector3 tempPosi=freeSpinNumberTransform.localPosition;
                Transform tempParent = freeSpinNumberTransform.parent;
                int TempSiblingIndex=freeSpinNumberTransform.GetSiblingIndex();

                freeSpinNumberTransform.SetParent(AnimationParent);       

                bool scale = false;
                yield return freeSpinNumberTransform.DOLocalMove(FreeSpinCountUIPositon.localPosition, .4f).SetEase(Ease.Linear).OnUpdate(()=>{
                    if (Vector3.Distance(freeSpinNumberTransform.localPosition, FreeSpinCountUIPositon.localPosition) < 100f && !scale)
                    {
                        scale = true;
                        freeSpinNumberTransform.DOScale(0, 0.2f);
                    }
                }).WaitForCompletion();
                freeSpinNumberTransform.gameObject.SetActive(false);

                if(int.TryParse(FSnum_text.text, out int currFScount)){
                    currFScount += lp.value;
                    FSnum_text.text = currFScount.ToString();
                }
                else{
                    Debug.Log("Error while FS int conversion");
                }

                freeSpinNumberTransform.localPosition = tempPosi;
                freeSpinNumberTransform.SetParent(tempParent);
                freeSpinNumberTransform.SetSiblingIndex(TempSiblingIndex);
                freeSpinNumberTransform.DOScale(1, 0f);
                yield return new WaitForSeconds(1f);
            }
        }
    }

    internal void OpenFreeSpinsUI()
    {
        FSnum_text.text = freeSpinsCount.ToString();
        FreeSpinsUI_Panel.DOFade(1, 0.3f);
        if(LinesUI.alpha!=0) LinesUI.DOFade(0, 0.3f).OnComplete(()=> {LinesUI.interactable=false; LinesUI.blocksRaycasts=false;});
        if(TotalBetUI.alpha!=0) TotalBetUI.DOFade(0, 0.3f).OnComplete(()=> {TotalBetUI.interactable=false; TotalBetUI.blocksRaycasts=false;});
        if(LineBetUI.alpha!=0) LineBetUI.DOFade(0, 0.3f).OnComplete(()=> {LineBetUI.interactable=false; LineBetUI.blocksRaycasts=false;});
    }

    internal void CloseFreeSpinsUI()
    {
        IsFreeSpin=false;
        FreeSpinsUI_Panel.DOFade(0, 0.3f);
        FSnum_text.text = "0";
        LinesUI.DOFade(1, 0.3f).OnComplete(()=> {LinesUI.interactable=true; LinesUI.blocksRaycasts=true;});
        TotalBetUI.DOFade(1, 0.3f).OnComplete(()=> {TotalBetUI.interactable=true; TotalBetUI.blocksRaycasts=true;});
        LineBetUI.DOFade(1, 0.3f).OnComplete(()=> {LineBetUI.interactable=true; LineBetUI.blocksRaycasts=true;});
    }

    internal void WinningsTextAnimation()
    {
        if(!double.TryParse(SocketManager.playerdata.currentWining.ToString("f2"),out double winAmt)){
            Debug.LogError("Error while conversion current winnings: " + SocketManager.playerdata.currentWining.ToString("f2"));
        }
        if(!double.TryParse(Balance_text.text, out double currentBal)){
            Debug.LogError("Error while converting string to double in current balance: " + Balance_text.text);
        }
        if(!double.TryParse(SocketManager.playerdata.Balance.ToString("f2"), out double Balance)){
            Debug.LogError("Error while converting string to double in new balance: " + SocketManager.playerdata.Balance.ToString("f2"));
        }
        if(!double.TryParse(TotalWin_text.text, out double currentWin)){
            Debug.LogError("Error while converting string to double in Totalwin:" + TotalWin_text.text);
        }
        DOTween.To(() => currentWin, (val) => currentWin = val, winAmt, 0.8f).OnUpdate(() =>
        {
            if (TotalWin_text) TotalWin_text.text = currentWin.ToString("f3");
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
            // }
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

        yield return new WaitForSeconds(2.2f);

        TurnOnBlackBoxes();

        yield return new WaitForSeconds(1f);

        while (true)
        {
            for (int i = 0; i < LineIDs.Count; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (Tempimages[j].slotImages[SocketManager.LineData[LineIDs[i]][j]].GetComponent<ImageAnimation>().isAnim)
                    {
                        Tempimages[j].slotImages[SocketManager.LineData[LineIDs[i]][j]].transform.GetChild(2).GetComponent<Image>().DOFade(0f, 0.2f);
                        Tempimages[j].slotImages[SocketManager.LineData[LineIDs[i]][j]].GetComponent<ImageAnimation>().StartAnimation();
                    }
                }

                yield return new WaitForSeconds(2.2f);

                TurnOnBlackBoxes();
                yield return new WaitForSeconds(1f);
                
            }
            if(!WinAnimationFin) WinAnimationFin = true;
        }
    }

    private void TurnOnBlackBoxes()
    {
        for (int i = 0; i < Tempimages.Count; i++)
        {
            for (int j = 0; j < Tempimages[i].slotImages.Count; j++)
            {
                Tempimages[i].slotImages[j].transform.GetChild(2).GetComponent<Image>().DOFade(0.85f, 0.2f);
                Tempimages[i].slotImages[j].GetComponent<ImageAnimation>().StopAnimation();
            }
        }
    }

    internal void CallCloseSocket()
    {
        SocketManager.CloseSocket();
    }

    internal void ToggleButtonGrp(bool toggle)
    {
        if (SlotStart_Button && !IsAutoSpin && !SlotStart_Button.gameObject.activeInHierarchy) SlotStart_Button.gameObject.SetActive(toggle);
        if (SlotStart_Button && !IsAutoSpin) SlotStart_Button.interactable = toggle;
        if (AutoSpin_Button && !IsAutoSpin) AutoSpin_Button.gameObject.SetActive(toggle);
        if (AutoSpin_Button && !IsAutoSpin) AutoSpin_Button.interactable = toggle;
        if (LineBetPlus_Button && !IsAutoSpin) LineBetPlus_Button.interactable = toggle;
        if (LineBetMinus_Button && !IsAutoSpin) LineBetMinus_Button.interactable = toggle;
        if (TotalBetPlus_Button && !IsAutoSpin) TotalBetPlus_Button.interactable = toggle;
        if (TotalBetMinus_Button && !IsAutoSpin) TotalBetMinus_Button.interactable = toggle;
    }

    //Start the icons animation
    private void StartGameAnimation(GameObject animObjects)
    {
        ImageAnimation temp = animObjects.GetComponent<ImageAnimation>();
        if (temp.textureArray.Count > 0)
        {
            temp.StartAnimation();
            temp.isAnim = true;
            animObjects.transform.GetChild(2).gameObject.GetComponent<Image>().DOFade(0f, 0.2f);
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
                Tempimages[i].slotImages[j].transform.GetChild(2).gameObject.GetComponent<Image>().DOFade(0f, 0.2f);
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

    private IEnumerator StopTweening(int reqpos, Transform slotTransform, int index, bool magnet = false, int CCloc = 0)
    {
        bool IsRegister = false;
        yield return alltweens[index].OnStepComplete(delegate { IsRegister = true; });
        yield return new WaitUntil(() => IsRegister);

        alltweens[index].Pause();

        int tweenpos = (reqpos * IconSizeFactor) - IconSizeFactor;
        if (!magnet)
        {
            alltweens[index] = slotTransform.DOLocalMoveY(tweenpos + 441.255f, 0.5f).SetEase(Ease.OutQuad); //1789
            if (audioController) audioController.PlayWLAudio("spinStop");
            yield return alltweens[index].WaitForCompletion();
            alltweens[index].Kill();
        }
        else
        {
            ImageAnimation anim = null;
            if(slotTransform.name == "Slot"){
                anim = LeftMagnetImageAnimation;
            }
            else if(slotTransform.name =="Slot (4)"){
                anim = RightMagnetImageAnimation;
            }
            anim.rendererDelegate.sprite=MagnetInSprites[0];
            anim.gameObject.SetActive(true);

            if(CCloc == 0){
                alltweens[index] = slotTransform.DOLocalMoveY(1547.255f, 2f).SetEase(Ease.OutQuad);
            }
            else if(CCloc == 1){
                alltweens[index] = slotTransform.DOLocalMoveY(1768.255f, 2f).SetEase(Ease.OutQuad);
            }
            else{
                Debug.Log(("wrong cc loc1`"));
            }
            if (audioController) audioController.PlayWLAudio("spinStop");
            yield return alltweens[index].WaitForCompletion();
            alltweens[index].Kill();
            
            ClearAnimtionArray(anim);
            foreach(Sprite sprite in MagnetInSprites){
                anim.textureArray.Add(sprite);
            }
            anim.doLoopAnimation=false;
            anim.AnimationSpeed = 8;
            anim.StartAnimation();
            yield return new WaitUntil(()=> anim.textureArray[^1]==anim.rendererDelegate.sprite);
            yield return new WaitForSeconds(1f);
            ClearAnimtionArray(anim);
            foreach(Sprite sprite in MagnetLightening_Sprites){
                anim.textureArray.Add(sprite);
            }
            anim.AnimationSpeed = 17;
            anim.StopAnimation();
            audioController.PlayWLAudio("Electric");
            anim.StartAnimation();
            yield return new WaitUntil(()=> anim.textureArray[^5]==anim.rendererDelegate.sprite);
            if (audioController) audioController.PlayWLAudio("spinStop");
            slotTransform.DOLocalMoveY(tweenpos + 441.255f, 0.5f).SetEase(Ease.OutCubic);
            StartCoroutine(CloseMagnet(anim));
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator CloseMagnet(ImageAnimation anim){
        yield return new WaitUntil(()=> anim.textureArray[^1]==anim.rendererDelegate.sprite);
        ClearAnimtionArray(anim);
        for(int i = MagnetInSprites.Length-1;i>=0;i--){
            anim.textureArray.Add(MagnetInSprites[i]);
        }
        anim.AnimationSpeed = 8;
        anim.StopAnimation();
        anim.StartAnimation();
        yield return new WaitUntil(()=> anim.textureArray[^1]==anim.rendererDelegate.sprite);
        anim.gameObject.SetActive(false);
        anim.StopAnimation();
        anim.rendererDelegate.sprite=MagnetInSprites[0];
    }

    private void ClearAnimtionArray(ImageAnimation imageAnimation){
        imageAnimation.textureArray.Clear();
        imageAnimation.textureArray.TrimExcess();
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


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

    [SerializeField] private CanvasGroup FreeSpinsUI_Panel;
    [SerializeField] private CanvasGroup WinningsUI_Panel;
    [SerializeField] private CanvasGroup LinesUI;
    [SerializeField] private CanvasGroup TotalBetUI;
    [SerializeField] private CanvasGroup LineBetUI;
    [SerializeField] private RectTransform FreeSpinCountUIPositon;

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
        if (TotalWin_text) TotalWin_text.text = "0.00";
        if (Balance_text) Balance_text.text = SocketManager.playerdata.Balance.ToString("f2");
        currentBalance = SocketManager.playerdata.Balance;
        currentTotalBet = SocketManager.initialData.Bets[BetCounter] * Lines;
        currentLineBet = SocketManager.initialData.Bets[BetCounter];
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
                if (Tempimages[i].slotImages[j].sprite == SlotSymbols[12]) //if the symbol is cash collect
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
        // animScript.doLoopAnimation=true;
        switch (val)
        {
            case 0:
                for(int i = 0; i < C_Sprites.Length; i++)
                {
                    animScript.textureArray.Add(C_Sprites[i]);
                }
                animScript.AnimationSpeed = 15f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 1:
                for (int i = 0; i < O_Sprites.Length; i++)
                {
                    animScript.textureArray.Add(O_Sprites[i]);
                }
                animScript.AnimationSpeed = 15f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 2:
                foreach(Sprite sprite in N_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 15f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 3:
                foreach (Sprite sprite in B_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 15f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 4:
                foreach (Sprite sprite in Barrel_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 12f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 5:
                foreach (Sprite sprite in Bus_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 12f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 6:
                foreach (Sprite sprite in Orange_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 12f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 7:
                foreach (Sprite sprite in Purple_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 12f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 8:
                foreach (Sprite sprite in Blue_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 12f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 10:
                foreach (Sprite sprite in Yellow_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 12f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 11:
                foreach (Sprite sprite in Link_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 12f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 12:
                foreach (Sprite sprite in MegaLink_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 10f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
                break;
            case 13:
                foreach (Sprite sprite in CC_Sprites)
                {
                    animScript.textureArray.Add(sprite);
                }
                animScript.AnimationSpeed = 10f;
                animScript.transform.GetChild(3).gameObject.SetActive(false);
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
                animScript.transform.GetChild(3).gameObject.SetActive(false);
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

        yield return PopulateResultMatrix();

        for (int i = 0; i < numberOfSlots; i++) // Stop tweening for each slot
        {
            yield return StopTweening(5, Slot_Transform[i], i);
        }

        KillAllTweens();
        if (SocketManager.playerdata.currentWining > 0 && !SocketManager.resultData.isCoinCollect ) WinningsTextAnimation(); // Trigger winnings animation if applicable

        CheckPayoutLineBackend(SocketManager.resultData.linesToEmit, SocketManager.resultData.FinalsymbolsToEmit, SocketManager.resultData.jackpot.payout);

        yield return new WaitForSeconds(1f);
        if(IsAutoSpin || SocketManager.resultData.isCoinCollect || SocketManager.resultData.bonus.isBonus){
            yield return new WaitUntil(() => WinAnimationFin);
            StopGameAnimation();
        }
        //currentBalance = SocketManager.playerdata.Balance;

        if(SocketManager.resultData.isCoinCollect){
            WinningsUI_Panel.DOFade(1, 0.3f);
            yield return new WaitForSeconds(1f);
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
            for(int i = 0;i< ResultMatrix.Count;i++){
                for(int j = 0; j<ResultMatrix[i].slotImages.Count; j++){
                    if(ResultMatrix[i].slotImages[j].sprite == SlotSymbols[14]){
                        
                        yield return uiManager.TrailRendererAnimation(ResultMatrix[i].slotImages[j].transform.GetChild(5).gameObject, 3, ccCount);
                    }
                }
            }
            yield return new WaitForSeconds(1f);
            WinningsUI_Panel.DOFade(0, 0.3f).OnComplete(()=> { BonusWin_Text.text = "0"; });
            yield return new WaitForSeconds(1f);
            WinningsTextAnimation();
        }

        if (SocketManager.resultData.freeSpins.isNewAdded)
        {
            // Free Spins have been awarded, trigger free spins animation and UI.
            if (IsAutoSpin)
            {
                StopAutoSpin();
            }
            OpenFreeSpinsUI();
            IsFreeSpin = true;
            IsSpinning = false;

            StopGameAnimation();
            int extraFreeSpin = 0;
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
        }

        if (SocketManager.resultData.bonus.isBonus)
        {
            if (TotalWin_text && SocketManager.resultData.isCoinCollect) {
                yield return new WaitForSeconds(1.5f);
                TotalWin_text.text = "0.00";
            }
            if(IsAutoSpin){
                IsSpinning = false;
                StopAutoSpin();
            }

            StopGameAnimation();

            // Only Bonus is awarded without Free Spins, directly trigger bonus round
            yield return uiManager.MidGameImageAnimation(BonusImageAnimation);

            staticSymbolController.TurnOnIndices(GenerateFreezedLocations());
            yield return new WaitForSeconds(1f);
            _bonusManager.StartBonus(SocketManager.resultData.bonus.spinCount);
            yield break;
        }


        CheckPopups = true;
        CheckWinPopups();

        yield return new WaitUntil(()=> !CheckPopups);

        if(SocketManager.playerdata.currentWining <= 0)
        {
           audioController.PlayWLAudio("lose");
        }

        // Post-bonus and free spins cleanup
        ToggleButtonGrp(true);
        IsSpinning = false;

        if(freeSpinsCount<=0 && SocketManager.resultData.freeSpins.count<=0 && !SocketManager.resultData.freeSpins.isNewAdded){
            IsFreeSpin=false;
            CloseFreeSpinsUI();
        }

        if(IsAutoSpin){
            yield return new WaitForSeconds(2f);
        }
    }
    #endregion

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
                    if(resultnum[i] == 16)
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
                        }
                    }
                    else if (resultnum[i] == 14)
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
        if (SocketManager.resultData.WinAmout >= currentTotalBet * 5 && SocketManager.resultData.WinAmout < currentTotalBet * 10)
        {
            uiManager.PopulateWin(1);
        }
        else if (SocketManager.resultData.WinAmout >= currentTotalBet * 10 && SocketManager.resultData.WinAmout < currentTotalBet * 15)
        {
            uiManager.PopulateWin(2);
        }
        else
        {
            CheckPopups = false;
        }
    }

    private IEnumerator FreeSpinsSymbolAnimation(){
        for(int i=0;i<ResultMatrix.Count;i++){
            for(int j=0;j<ResultMatrix[i].slotImages.Count;j++){
                if(SocketManager.resultData.ResultReel[i][j] == "16"){
                    for(int k=0;k<losPollosSprites.Length;k++){
                        if(losPollosSprites[k]!=null && losPollosSprites[k]==ResultMatrix[i].slotImages[j].sprite){
                            Transform freeSpinNumberTransform = ResultMatrix[i].slotImages[j].transform.GetChild(4);
                            freeSpinNumberTransform.GetComponent<Image>().sprite=losPollosNumberSprites[k];
                            freeSpinNumberTransform.gameObject.SetActive(true);
                            ResultMatrix[i].slotImages[j].sprite = losPollosNoNumberSprite;

                            Vector3 tempPosi=freeSpinNumberTransform.position;
                            yield return freeSpinNumberTransform.DOLocalMove(FreeSpinCountUIPositon.position, 5f).WaitForCompletion();
                            freeSpinNumberTransform.gameObject.SetActive(false);

                            if(int.TryParse(FSnum_text.text, out int currFScount)){
                                currFScount += j;
                                FSnum_text.text = currFScount.ToString();
                            }
                            else{
                                Debug.Log("Error while FS int conversion");
                            }

                            freeSpinNumberTransform.position = tempPosi;
                            yield return new WaitForSeconds(1f);
                        }
                    }
                }
            }
        }
    }

    private void OpenFreeSpinsUI()
    {
        FSnum_text.text = freeSpinsCount.ToString();
        FreeSpinsUI_Panel.DOFade(1, 0.3f);
        if(LinesUI.alpha!=0) LinesUI.DOFade(0, 0.3f).OnComplete(()=> {LinesUI.interactable=false; LinesUI.blocksRaycasts=false;});
        if(TotalBetUI.alpha!=0) TotalBetUI.DOFade(0, 0.3f).OnComplete(()=> {TotalBetUI.interactable=false; TotalBetUI.blocksRaycasts=false;});
        if(LineBetUI.alpha!=0) LineBetUI.DOFade(0, 0.3f).OnComplete(()=> {LineBetUI.interactable=false; LineBetUI.blocksRaycasts=false;});
    }

    private void CloseFreeSpinsUI()
    {
        FreeSpinsUI_Panel.DOFade(0, 0.3f);
        FSnum_text.text = "0";
        LinesUI.DOFade(1, 0.3f).OnComplete(()=> {LinesUI.interactable=true; LinesUI.blocksRaycasts=true;});
        TotalBetUI.DOFade(1, 0.3f).OnComplete(()=> {TotalBetUI.interactable=true; TotalBetUI.blocksRaycasts=true;});
        LineBetUI.DOFade(1, 0.3f).OnComplete(()=> {LineBetUI.interactable=true; LineBetUI.blocksRaycasts=true;});
    }

    private void WinningsTextAnimation()
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
            if (TotalWin_text) TotalWin_text.text = currentWin.ToString("f2");
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

            // if (jackpot > 0)
            // {
                //Play Jackpot Animation
            // }
            // else
            // {
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

        yield return new WaitForSeconds(2.5f);

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

                yield return new WaitForSeconds(2.5f);

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
                Tempimages[i].slotImages[j].transform.GetChild(2).GetComponent<Image>().DOFade(0.67f, 0.2f);
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
        
        // DOVirtual.DelayedCall(1f, ()=> {
        //     for(int i = 0;i<slotTransform.childCount;i++){
        //         slotTransform.GetChild(i).GetChild(3).gameObject.SetActive(false);
        //     }
        // });

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


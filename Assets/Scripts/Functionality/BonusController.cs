using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using System.Linq;

public class BonusController : MonoBehaviour
{
    [SerializeField] private SlotBehaviour slotManager;
    [SerializeField] private SocketIOManager SocketManager;
    [SerializeField] private AudioController audioController;
    [SerializeField] private ImageAnimation BonusOpen_ImageAnimation;
    [SerializeField] private ImageAnimation BonusClose_ImageAnimation;
    [SerializeField] private ImageAnimation BonusInBonus_ImageAnimation;
    [SerializeField] private GameObject BonusGame_Panel;
    [SerializeField] private GameObject BonusOpeningUI;
    [SerializeField] private GameObject BonusClosingUI;
    [SerializeField] private GameObject BonusInBonusUI;
    [SerializeField] private TMP_Text FSnum_Text;
    [SerializeField] private TMP_Text BonusOpeningText;
    [SerializeField] private TMP_Text BonusClosingText;
    [SerializeField] private TMP_Text BonusInBonusText;
    [SerializeField] private TMP_Text BonusWinningsText;

    [SerializeField] private CanvasGroup NormalSlot_CG;
    [SerializeField] private CanvasGroup BonusSlot_CG;

    [SerializeField] private Button BonusSlotStart_Button;
    [SerializeField] private Button NormalSlotStart_Button;
    [SerializeField] private Button AutoSpin_Button;

    [SerializeField] private TMP_Text BonusSpinCounter_Text;

    [SerializeField] private Sprite[] miniSlotImages;
    [SerializeField] private Sprite[] index9Sprites;
    [SerializeField] private Sprite[] losPollos;
    [SerializeField] private Sprite coinFrame;

    [SerializeField] private List<SlotImage> TotalMiniSlotImages;     //class to store total images
    [SerializeField] private List<SlotImage> TempMiniSlotImages;     //class to store the result matrix
    [SerializeField] private Transform[] MiniSlot_Transform;

    [SerializeField] public List<SlotTransform> Slot;

    [SerializeField] private StaticSymbolController staticSymbol;

    [Header("Animation Sprites")]
    [SerializeField] private Sprite[] Be_Sprites, Co_Sprites, Ga_Sprites, Ho_Sprites, Os_Sprites, Rb_Sprites, Rn_Sprites, Diamond_Sprites, CC_Sprites, coin2_Sprites, coin3_Sprites, coin4_Sprites, coin5_Sprites, coin7_Sprites;

    private List<KeyValuePair<Transform, Tweener>> singleSlotTweens = new List<KeyValuePair<Transform, Tweener>>();


    private int IconSizeFactor = 202;
    private bool IsSpinning;


    private void Start()
    {
        if (BonusSlotStart_Button)
        {
            BonusSlotStart_Button.onClick.RemoveAllListeners();
            BonusSlotStart_Button.onClick.AddListener(StartBonusSlot);
        }

        for(int i = 0; i < TotalMiniSlotImages.Count; i++)
        {
            for (int j = 0; j < TotalMiniSlotImages[i].slotImages.Count; j++)
            {
                int randomIndex = Random.Range(0, index9Sprites.Length);
                TotalMiniSlotImages[i].slotImages[j].sprite = index9Sprites[randomIndex];
            }
        }

    }

    internal void StartBonus(int count)
    {
        if(NormalSlotStart_Button && BonusSlotStart_Button && AutoSpin_Button) //Manage Button CLick here maybe set interactable = false, and turn off autospin button  
        {
            NormalSlotStart_Button.gameObject.SetActive(false);
            AutoSpin_Button.gameObject.SetActive(false);
            BonusSpinCounter_Text.text = count.ToString();
        }

        DOTween.To(() => NormalSlot_CG.alpha, (val) => NormalSlot_CG.alpha = val, 0, .5f).OnComplete(()=>
        {
            NormalSlot_CG.interactable = false;
            NormalSlot_CG.blocksRaycasts = false;
        });

        DOTween.To(() => BonusSlot_CG.alpha, (val) => BonusSlot_CG.alpha = val, 1, .5f).OnComplete(() =>
        {
            BonusSlot_CG.interactable = true;
            BonusSlot_CG.blocksRaycasts = true;
            BonusSlotStart_Button.gameObject.SetActive(true);
        });
    }




    private void StartBonusSlot()
    {
        if (audioController) audioController.PlaySpinButtonAudio();

        if (BonusSlotStart_Button) BonusSlotStart_Button.interactable = false;

        if(!int.TryParse(BonusSpinCounter_Text.text, out int spinCount))
        {
            Debug.LogError("Conversion error");
        }
        else
        {
            spinCount -= 1;
            BonusSpinCounter_Text.text = spinCount.ToString();
        }

        StartCoroutine(BonusTweenRoutine());
    }

    private IEnumerator BonusTweenRoutine()
    {
        IsSpinning = true;

        // Initialize tweening for non-frozen slot animations
        for (int row = 0; row < Slot.Count; row++)
        {
            for (int col = 0; col < Slot[row].slotTransforms.Count; col++)
            {
                if (staticSymbol.freezedLocations[row].index[col] == 0) // Only initialize non-frozen slots
                {
                    InitializeSingleSlotTweening(Slot[row].slotTransforms[col]);
                }
            }
        }

        //yield return new WaitForSeconds(2f);
        SocketManager.AccumulateResult(0);
        yield return new WaitUntil(() => SocketManager.isResultdone);

        for (int j = 0; j < SocketManager.resultData.bonus.BonusResult.Count; j++)
        {
            for(int i = 0; i < 5; i++)
            {
                if(SocketManager.resultData.bonus.BonusResult[j][i] == 9)
                {
                    Slot[j].slotTransforms[i].GetChild(3).GetComponent<Image>().sprite = index9Sprites[Random.Range(0, index9Sprites.Count())];
                }
                else if(SocketManager.resultData.bonus.BonusResult[j][i] == 14)
                {
                    //run a loop to find the value of the coin and set the coin and its text
                    Slot[j].slotTransforms[i].GetChild(3).GetComponent<Image>().sprite = coinFrame;
                }
                else if(SocketManager.resultData.bonus.BonusResult[j][i] == 13)
                {
                    Debug.Log("Frozen index CC");
                }
            }
        } 

        // Create a list of all slot indices for randomization
        List<(int row, int col)> indices = new List<(int, int)>();
        for (int row = 0; row < Slot.Count; row++)
        {
            for (int col = 0; col < Slot[row].slotTransforms.Count; col++)
            {
                indices.Add((row, col));
            }
        }

        // Shuffle the list to get random indices
        System.Random random = new System.Random();
        indices = indices.OrderBy(x => random.Next()).ToList();

        foreach (var (row, col) in indices)
        {
            if (staticSymbol.freezedLocations[row].index[col] == 0) // Stop only non-frozen slots
            {
                int flattenedIndex = row * Slot[row].slotTransforms.Count + col;
                yield return StopSingleSlotTweening(3, Slot[row].slotTransforms[col], flattenedIndex);
            }
        }

        KillAllTweens();

        yield return new WaitForSeconds(2f);

        //if(no more spins left start end bonus)
        //EndBonus();

        IsSpinning = false;
        BonusSlotStart_Button.interactable = true;
    }


    private void EndBonus()
    {
        if (NormalSlotStart_Button && BonusSlotStart_Button) //Manage Button CLick here maybe set interactable = false, and turn off autospin button  
        {
            NormalSlotStart_Button.gameObject.SetActive(true);
            BonusSlotStart_Button.gameObject.SetActive(false);
            BonusSlotStart_Button.interactable = true;
        }

        DOTween.To(() => BonusSlot_CG.alpha, (val) => BonusSlot_CG.alpha = val, 0, .5f).OnComplete(() =>
        {
            BonusSlot_CG.interactable = false;
            BonusSlot_CG.blocksRaycasts = false;
        });

        DOTween.To(() => NormalSlot_CG.alpha, (val) => NormalSlot_CG.alpha = val, 1, .5f).OnComplete(() =>
        {
            NormalSlot_CG.interactable = true;
            NormalSlot_CG.blocksRaycasts = true;
        });
    }


    private void InitializeSingleSlotTweening(Transform slotTransform, bool bonus = false)
    {
        Tweener tweener = null;

        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, slotTransform.localPosition.y + 442);
        tweener = slotTransform.DOLocalMoveY(-670, .3f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetDelay(0);

        tweener.Play();
        //singleSlotTweens.Add(slotTransform, tweener);
        singleSlotTweens.Add(new KeyValuePair<Transform, Tweener>(slotTransform, tweener));
    }

    private IEnumerator StopSingleSlotTweening(int reqpos, Transform slotTransform, int index, bool bonus = false)
    {
        // Find the corresponding KeyValuePair entry in singleSlotTweens for the given slotTransform
        var tweenPair = singleSlotTweens.Find(pair => pair.Key == slotTransform);

        // Check if the tween was found
        if (tweenPair.Value == null)
        {
            Debug.LogWarning("Tween not found for the specified slotTransform.");
            yield break;
        }

        bool IsRegister = false;

        // Register to complete the current loop
        yield return tweenPair.Value.OnStepComplete(() => IsRegister = true);
        yield return new WaitUntil(() => IsRegister);

        // Pause the tween
        tweenPair.Value.Pause();

        // Calculate the position and stop tweening at the required position
        int tweenpos = (reqpos * IconSizeFactor) - IconSizeFactor;
        Tweener stopTween = slotTransform.DOLocalMoveY(tweenpos - 290.5f, 0.5f);

        if (audioController) audioController.PlayWLAudio("spinStop");

        yield return stopTween.WaitForCompletion();

        // Kill the original tween after it has completed
        tweenPair.Value.Kill();
    }

    private void KillAllTweens()
    {
        if (singleSlotTweens.Count > 0)
        {
            for (int i = 0; i < singleSlotTweens.Count; i++)
            {
                singleSlotTweens[i].Value.Kill();
            }
            singleSlotTweens.Clear();
        }
    }

    private IEnumerator BonusGameStartRoutine(int spins)
    {
        audioController.SwitchBGSound(true);
        if (BonusOpen_ImageAnimation) BonusOpen_ImageAnimation.StartAnimation();

        slotManager.StopGameAnimation();

        yield return new WaitUntil(() => BonusOpen_ImageAnimation.rendererDelegate.sprite == BonusOpen_ImageAnimation.textureArray[16]);

        BonusOpen_ImageAnimation.PauseAnimation();
        BonusOpeningUI.SetActive(true);
        yield return new WaitForSeconds(2f);
        BonusOpeningUI.SetActive(false);
        BonusOpen_ImageAnimation.ResumeAnimation();

        yield return new WaitUntil(() => BonusOpen_ImageAnimation.rendererDelegate.sprite == BonusOpen_ImageAnimation.textureArray[BonusOpen_ImageAnimation.textureArray.Count-1]);
        BonusOpen_ImageAnimation.StopAnimation();

        yield return new WaitForSeconds(1f);

        slotManager.FreeSpin(spins);
    }

    internal IEnumerator BonusInBonus()
    {
        BonusInBonus_ImageAnimation.StartAnimation();

        yield return new WaitUntil(() => BonusInBonus_ImageAnimation.rendererDelegate.sprite == BonusInBonus_ImageAnimation.textureArray[5]);

        BonusInBonus_ImageAnimation.PauseAnimation();

        int currFS = 0;
        int.TryParse(FSnum_Text.text, out currFS);
        Debug.Log("Current Spins: " + currFS.ToString());
        FSnum_Text.text = SocketManager.resultData.freeSpins.count.ToString();
        Debug.Log("Total Spins now: " + FSnum_Text.text);
        print("Free Spins Added: " + (SocketManager.resultData.freeSpins.count - currFS).ToString());
        BonusInBonusText.text = (SocketManager.resultData.freeSpins.count-currFS).ToString() + " FREE SPINS";

        BonusInBonusUI.SetActive(true);
        yield return new WaitForSeconds(2f);
        BonusInBonusUI.SetActive(false);
        BonusInBonus_ImageAnimation.ResumeAnimation();

        yield return new WaitUntil(() => BonusInBonus_ImageAnimation.rendererDelegate.sprite == BonusInBonus_ImageAnimation.textureArray[BonusInBonus_ImageAnimation.textureArray.Count-1]);
        BonusInBonus_ImageAnimation.StopAnimation();

        yield return new WaitForSeconds(1f);

        slotManager.FreeSpin(SocketManager.resultData.freeSpins.count);
    }

    internal IEnumerator BonusGameEndRoutine()
    {
        if (BonusClosingText) BonusClosingText.text = BonusWinningsText.text;
        BonusClose_ImageAnimation.StartAnimation();

        double.TryParse(BonusClosingText.text, out double totalWin);
        if (totalWin > 0)
        {
            yield return new WaitUntil(() => BonusClose_ImageAnimation.rendererDelegate.sprite == BonusClose_ImageAnimation.textureArray[6]);

            BonusClose_ImageAnimation.PauseAnimation();
            BonusClosingUI.SetActive(true);
            yield return new WaitForSeconds(3f);
            BonusClosingUI.SetActive(false);
            BonusClose_ImageAnimation.ResumeAnimation();
        }
        slotManager.StopGameAnimation();
        yield return new WaitUntil(()=> BonusClose_ImageAnimation.rendererDelegate.sprite == BonusClose_ImageAnimation.textureArray[BonusClose_ImageAnimation.textureArray.Count-1]);
        BonusClose_ImageAnimation.StopAnimation();
        audioController.SwitchBGSound(false);

        if (BonusGame_Panel) BonusGame_Panel.SetActive(false);
        BonusWinningsText.text = "0";
    }
}

[System.Serializable]
public class SlotTransform
{
    public List<Transform> slotTransforms = new List<Transform>();
}
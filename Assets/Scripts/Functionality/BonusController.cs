using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using System.Linq;

public class BonusController : MonoBehaviour
{
    [Header("Scripts References")]
    [SerializeField] private SlotBehaviour slotManager;
    [SerializeField] private SocketIOManager SocketManager;
    [SerializeField] private AudioController audioController;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private StaticSymbolController staticSymbol;

    [Header("Sprites References")]
    [SerializeField] private Sprite[] index9Sprites;
    //[SerializeField] private Sprite[] losPollos;
    [SerializeField] private Sprite coinFrame;
    [SerializeField] private Sprite CC_Sprite;

    [Header("UI Objects References")]
    [SerializeField] private CanvasGroup NormalSlot_CG;
    [SerializeField] private CanvasGroup BonusSlot_CG;

    [SerializeField] private Button BonusSlotStart_Button;
    [SerializeField] private Button NormalSlotStart_Button;
    [SerializeField] private Button AutoSpin_Button;

    [SerializeField] private TMP_Text BonusSpinCounter_Text;
    [SerializeField] private TMP_Text BonusWinnings_Text;

    [SerializeField] private Transform WinningsPosition;
    [SerializeField] private GameObject BonusWinningsUI_Panel;
    [SerializeField] private GameObject FreeSpinsCounterUI_Panel;
    [SerializeField] private GameObject lines, lineBet, totalBet;

    [Header("Slot References")]
    [SerializeField] private List<SlotImage> TotalMiniSlotImages;     //class to store total images
    [SerializeField] public List<SlotTransform> Slot;

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
        lineBet.SetActive(false);
        lines.SetActive(false);
        totalBet.SetActive(false);
        NormalSlotStart_Button.gameObject.SetActive(false);
        AutoSpin_Button.gameObject.SetActive(false);
        BonusSlotStart_Button.interactable = false;
        BonusSlotStart_Button.gameObject.SetActive(true);
        BonusSpinCounter_Text.text = count.ToString();
        BonusWinningsUI_Panel.SetActive(true);

        DOTween.To(() => NormalSlot_CG.alpha, (val) => NormalSlot_CG.alpha = val, 0, .5f);
      
        DOTween.To(() => BonusSlot_CG.alpha, (val) => BonusSlot_CG.alpha = val, 1, .5f).OnComplete(() =>
        {
            StartCoroutine(staticSymbol.ChangeLinkToGoldCoin(BonusSlotStart_Button));
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
                    foreach (var coins in SocketManager.resultData.winData.coinValues)
                    {
                        if (coins.index[0] == j && coins.index[1] == i)
                        {
                            Slot[j].slotTransforms[i].GetChild(3).GetComponent<Image>().sprite = coinFrame;
                            Slot[j].slotTransforms[i].GetChild(3).GetChild(0).gameObject.SetActive(true);
                            Slot[j].slotTransforms[i].GetChild(3).GetChild(0).GetComponent<TMP_Text>().text = coins.value.ToString("f2");
                            break;
                        }
                    }
                }
                else if(SocketManager.resultData.bonus.BonusResult[j][i] == 13)
                {
                    Slot[j].slotTransforms[i].GetChild(3).GetComponent<Image>().sprite = CC_Sprite;
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

        staticSymbol.GenerateFreezeMatrix(SocketManager.resultData.bonus.freezeIndices);

        if(SocketManager.resultData.bonus.isWalterStash || SocketManager.resultData.jackpot>0)
        {
            yield return new WaitForSeconds(2f);

            for(int i = 0; i < Slot.Count; i++)
            {
                for(int j = 0; j < Slot[i].slotTransforms.Count; j++)
                {
                    if(Slot[i].slotTransforms[j].GetChild(3).GetComponent<Image>().sprite == coinFrame)
                    {
                        TrailRenderer trail = Slot[i].slotTransforms[j].GetChild(3).GetChild(1).GetComponent<TrailRenderer>();
                        Slot[i].slotTransforms[j].GetChild(3).GetChild(1).gameObject.SetActive(true);
                        Vector3 tempPosi = trail.transform.position;
                        yield return trail.transform.DOMove(WinningsPosition.position, .5f).OnComplete(()=>
                        {
                            Debug.Log("Here");
                            trail.gameObject.SetActive(false);
                            trail.transform.position = tempPosi;

                            int currWin = 0;
                            int coin = 0;
                            try
                            {
                                currWin = int.Parse(BonusWinnings_Text.text);
                                coin = int.Parse(Slot[i].slotTransforms[j].GetChild(3).GetChild(0).GetComponent<TMP_Text>().text.Split('.')[0]);
                            }
                            catch
                            {
                                Debug.Log("Caught conversion error");
                            }

                            currWin += coin;
                            BonusWinnings_Text.text = currWin.ToString();
                        });
                        yield return new WaitForSeconds(1f);
                    }
                }
            }

            uiManager.PopulateWin(3);

            yield return new WaitForSeconds(5f);

            IsSpinning = false;
            EndBonus();
        }

        if (SocketManager.resultData.bonus.freeSpinAdded)
        {
            BonusSpinCounter_Text.text = "3";
        }

        yield return new WaitForSeconds(2f);

        BonusSlotStart_Button.interactable = true;
        IsSpinning = false;
    }

    private void EndBonus()
    {
        BonusWinningsUI_Panel.SetActive(false);
        staticSymbol.Reset();

        DOTween.To(() => BonusSlot_CG.alpha, (val) => BonusSlot_CG.alpha = val, 0, .5f);
        
        DOTween.To(() => NormalSlot_CG.alpha, (val) => NormalSlot_CG.alpha = val, 1, .5f).OnComplete(() =>
        {
            BonusSlotStart_Button.gameObject.SetActive(false);
            BonusSlotStart_Button.interactable = false;
            if (SocketManager.resultData.freeSpins.count <= 0)
            {
                lineBet.SetActive(true);
                lines.SetActive(true);
                totalBet.SetActive(true);
            }
            else
            {
                FreeSpinsCounterUI_Panel.SetActive(true);
            }
            AutoSpin_Button.gameObject.SetActive(true);
            NormalSlotStart_Button.gameObject.SetActive(true);
            NormalSlotStart_Button.interactable = true;
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
}

[System.Serializable]
public class SlotTransform
{
    public List<Transform> slotTransforms = new List<Transform>();
}
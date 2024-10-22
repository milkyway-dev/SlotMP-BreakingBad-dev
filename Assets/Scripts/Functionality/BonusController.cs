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

    [SerializeField] private Sprite[] miniSlotImages;

    [SerializeField] private List<SlotImage> TotalMiniSlotImages;     //class to store total images
    [SerializeField] private List<SlotImage> TempMiniSlotImages;     //class to store the result matrix
    [SerializeField] private Transform[] MiniSlot_Transform;
    private List<Tweener> singleSlotTweens = new List<Tweener>();
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
                int randomIndex = Random.Range(14, miniSlotImages.Length);
                TotalMiniSlotImages[i].slotImages[j].sprite = miniSlotImages[randomIndex];
            }
        }

    }

    internal void StartBonus()
    {
        if(NormalSlotStart_Button && BonusSlotStart_Button) //Manage Button CLick here maybe set interactable = false, and turn off autospin button  
        {
            NormalSlotStart_Button.gameObject.SetActive(false);
            BonusSlotStart_Button.gameObject.SetActive(true);
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
        });
    }




    private void StartBonusSlot()
    {
        if (audioController) audioController.PlaySpinButtonAudio();

        if (BonusSlotStart_Button) BonusSlotStart_Button.interactable = false;

        StartCoroutine(BonusTweenRoutine());
    }

    private IEnumerator BonusTweenRoutine()
    {
        IsSpinning = true;

        for (int i = 0; i < MiniSlot_Transform.Length; i++) // Initialize tweening for slot animations
        {
            InitializeSingleSlotTweening(MiniSlot_Transform[i]);
        }

        yield return new WaitForSeconds(2f);

        // Create a list of indices from 0 to MiniSlot_Transform.Length - 1
        List<int> indices = Enumerable.Range(0, MiniSlot_Transform.Length).ToList();

        // Shuffle the list to get random indices
        System.Random random = new System.Random();
        indices = indices.OrderBy(x => random.Next()).ToList();

        // Iterate over the shuffled indices
        for (int i = 0; i < indices.Count; i++)
        {
            int randomIndex = indices[i];
            yield return StopSingleSlotTweening(3, MiniSlot_Transform[randomIndex], randomIndex);
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
        singleSlotTweens.Add(tweener);
    }

    private IEnumerator StopSingleSlotTweening(int reqpos, Transform slotTransform, int index, bool bonus = false)
    {
        bool IsRegister = false;
        yield return singleSlotTweens[index].OnStepComplete(delegate { IsRegister = true; });
        yield return new WaitUntil(() => IsRegister);

        singleSlotTweens[index].Pause();

        int tweenpos = (reqpos * IconSizeFactor) - IconSizeFactor;
        singleSlotTweens[index] = slotTransform.DOLocalMoveY(tweenpos - 290.5f, 0.5f);

        if (audioController) audioController.PlayWLAudio("spinStop");
        yield return singleSlotTweens[index].WaitForCompletion();
        singleSlotTweens[index].Kill();
    }

    private void KillAllTweens()
    {
        if (singleSlotTweens.Count > 0)
        {
            for (int i = 0; i < singleSlotTweens.Count; i++)
            {
                singleSlotTweens[i].Kill();
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

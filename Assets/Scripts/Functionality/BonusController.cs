using System.Collections;
using UnityEngine;
using TMPro;

public class BonusController : MonoBehaviour
{
    [SerializeField] private SlotBehaviour slotManager;
    [SerializeField] private SocketIOManager SocketManager;
    [SerializeField] private AudioController _audioManager;
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

    internal void StartBonus(int freespins)
    {
        if (FSnum_Text) FSnum_Text.text = freespins.ToString();
        if (BonusWinningsText) BonusWinningsText.text = "0.00";
        if (BonusOpeningText) BonusOpeningText.text = freespins.ToString() + " FREE SPINS";
        if (BonusGame_Panel) BonusGame_Panel.SetActive(true);
        StartCoroutine(BonusGameStartRoutine(freespins));
    }

    private IEnumerator BonusGameStartRoutine(int spins)
    {
        _audioManager.SwitchBGSound(true);
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
        _audioManager.SwitchBGSound(false);

        if (BonusGame_Panel) BonusGame_Panel.SetActive(false);
        BonusWinningsText.text = "0";
    }
}

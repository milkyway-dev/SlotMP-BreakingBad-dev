using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class StaticSymbolController : MonoBehaviour
{
    [Header("Scripts References")]
    [SerializeField] private SocketIOManager socketManager;
    [SerializeField] private SlotBehaviour slotManager;

    [Header("Slots Reference")]
    [SerializeField] public List<SlotImage> Slot;

    [Header("Sprites References")]
    [SerializeField] private Sprite[] images;

    [Header("Animation Sprites References")]
    [SerializeField] private Sprite[] LinkToGoldCoin_Animation;
    [SerializeField] private Sprite[] MegaLinkToGoldCoin_Animation;

    [SerializeField]internal List<Column> freezedLocations = new();
    [SerializeField] private RectTransform middlePosition;
    [SerializeField] internal List<List<int>> Locations = new();
    
    internal List<List<int>> GenerateFreezeMatrix(List<List<int>> loc, bool dontReturn=false)
    {
        for(int i=0;i<loc.Count;i++){
            if(!Locations.Contains(loc[i])){
                Locations.Add(loc[i]);
            }
        }
        // Initialize matrix with 0s
        List<List<int>> freezeMatrix = new List<List<int>>();

        for (int i = 0; i < Slot.Count; i++)
        {
            List<int> row = new List<int>(new int[Slot[i].slotImages.Count]);
            freezeMatrix.Add(row);
        }

        // Set 1s for frozen slots based on loc
        foreach (List<int> indexPair in Locations)
        {
            if (indexPair.Count == 2)
            {
                int row = indexPair[0];
                int column = indexPair[1];

                // Check bounds
                if (row >= 0 && row < freezeMatrix.Count &&
                    column >= 0 && column < freezeMatrix[row].Count)
                {
                    freezeMatrix[row][column] = 1;
                }
            }
        }

        // Update freezedLocations with data from freezeMatrix
        freezedLocations.Clear();
        foreach (var row in freezeMatrix)
        {
            Column column = new() { index = new List<int>(row) };
            freezedLocations.Add(column);
        }

        if (!dontReturn)
        {
            return freezeMatrix;
        }
        else
        {
            return null;
        }
    }

    // Method to apply the freeze effect to SlotImages based on the freeze matrix
    internal void TurnOnIndices(List<List<int>> loc)
    {
        // Generate the freeze matrix
        List<List<int>> freezeMatrix = GenerateFreezeMatrix(loc);

        // Apply the freeze effect based on the freeze matrix
        for (int i = 0; i < Slot.Count; i++)
        {
            for (int j = 0; j < Slot[i].slotImages.Count; j++)
            {
                if (freezeMatrix[i][j] == 1)
                {
                    // Apply effect to frozen slots
                    if(slotManager.ResultMatrix[i].slotImages[j].sprite == images[11]) //Checking if the frozen slot is a link and turning it to a coin
                    {
                        Slot[i].slotImages[j].GetComponent<ImageAnimation>().isAnim = true;
                        foreach(Sprite s in LinkToGoldCoin_Animation)
                        {
                            Slot[i].slotImages[j].GetComponent<ImageAnimation>().textureArray.Add(s);
                        }
                        //Add Value of the coins text component
                        
                        foreach(CoinValues coin in socketManager.resultData.bonus.coins)
                        {
                            if(coin.index[0] == i && coin.index[1] == j)
                            {
                                Slot[i].slotImages[j].transform.GetChild(0).GetComponent<TMP_Text>().text = coin.value.ToString("F2");
                                break;
                            }
                        }
                    }
                    else if(slotManager.ResultMatrix[i].slotImages[j].sprite == images[12]){ //checking if its megalink
                        ImageAnimation imageAnimation = Slot[i].slotImages[j].GetComponent<ImageAnimation>();
                        imageAnimation.isAnim = true;
                        foreach(Sprite s in MegaLinkToGoldCoin_Animation){
                            imageAnimation.textureArray.Add(s);
                        }

                        foreach(CoinValues coin in socketManager.resultData.bonus.coins){
                            if(coin.index[0] == i && coin.index[1] == j){
                                imageAnimation.transform.GetChild(0).GetComponent<TMP_Text>().text = coin.value.ToString("F2");
                                break;
                            }
                        }
                    }
                    // else if(socketManager.resultData.ResultReel[i][j] == "14"){ coin values gets payed out if they appear on the matrix so removed from here
                    //     foreach(CoinValues coin in socketManager.resultData.winData.coinValues){
                    //         if(i == coin.index[0] && j == coin.index[1]){
                    //             Slot[i].slotImages[j].transform.GetChild(0).GetComponent<TMPro.TMP_Text>().text = coin.value.ToString("F2");
                    //             Slot[i].slotImages[j].transform.GetChild(0).gameObject.SetActive(true);
                    //             break;
                    //         }
                    //     }
                    // }
                    Slot[i].slotImages[j].sprite = slotManager.ResultMatrix[i].slotImages[j].sprite;
                    Slot[i].slotImages[j].gameObject.SetActive(true);
                }
                else
                {
                    Slot[i].slotImages[j].gameObject.SetActive(false);
                }
            }
        }
    }

    internal IEnumerator ChangeLinksToGoldCoin(Button button)
    {
        for(int i = 0; i < Slot.Count; i++)
        {
            for(int j = 0; j < Slot[i].slotImages.Count; j++)
            {
                ImageAnimation anim = Slot[i].slotImages[j].GetComponent<ImageAnimation>();
                if (anim.isAnim)
                {
                    anim.AnimationSpeed = 15;
                    anim.StartAnimation();
                    yield return new WaitUntil(() => anim.rendererDelegate.sprite == anim.textureArray[7]);
                    anim.transform.GetChild(0).gameObject.SetActive(true);
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }

        yield return new WaitForSeconds(1f);

        button.interactable = true;
    }

    internal void Reset()
    {
        freezedLocations = new();

        for(int i = 0; i<Slot.Count; i++)
        {
            foreach (var j in Slot[i].slotImages)
            {
                j.gameObject.SetActive(false);
                j.sprite = null;
                j.transform.GetChild(0).GetComponent<TMPro.TMP_Text>().text = "";
                j.transform.GetChild(0).gameObject.SetActive(false);
                j.GetComponent<ImageAnimation>().isAnim=false;
            }
        }
    }
}

[Serializable]
public class Column
{
    public List<int> index = new();
}
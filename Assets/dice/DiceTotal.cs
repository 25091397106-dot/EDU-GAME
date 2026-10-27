
using System.Collections;
using UnityEngine;

public class DiceTotal : MonoBehaviour
{
    [Header("Dadu")]
    public DiceResult dice1;
    public DiceResult dice2;
    public DiceMixAnimation diceMix;

    [Header("Giliran")]
    public TurnManager turnManager;

    private bool sedangRoll = false;

    public bool SedangRoll => sedangRoll;
    public bool DaduKembar { get; private set; }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Space))
            return;

        // MODE MULTIPLAYER
        if (MainMenuManager.modeMultiplayer)
        {
            NetworkGameManager networkGame =
                FindAnyObjectByType<NetworkGameManager>();

            if (networkGame != null)
            {
                networkGame.MintaLemparDadu();
            }

            return;
        }

        // MODE LAWAN BOT
        if (turnManager != null)
        {
            turnManager.CobaLemparManusia();
        }
    }

    public bool LemparDadu()
    {
        // Jangan jalankan lemparan lokal
        // ketika sedang Multiplayer.
        if (MainMenuManager.modeMultiplayer)
            return false;

        if (sedangRoll ||
            diceMix == null ||
            diceMix.SedangMix ||
            dice1 == null ||
            dice2 == null)
        {
            return false;
        }

        sedangRoll = true;
        StartCoroutine(RollDanHitung());

        return true;
    }

    IEnumerator RollDanHitung()
    {
        yield return StartCoroutine(
            diceMix.KocokDadu()
        );

        int hasil1 = dice1.GetResult();
        int hasil2 = dice2.GetResult();
        int total = hasil1 + hasil2;

        DaduKembar = hasil1 == hasil2;

        Debug.Log("Dadu 1: " + hasil1);
        Debug.Log("Dadu 2: " + hasil2);
        Debug.Log("Total Dadu: " + total);

        if (DaduKembar)
        {
            Debug.Log("DADU KEMBAR!");
        }

        if (turnManager != null)
        {
            turnManager.HasilLemparan(
                total,
                DaduKembar
            );
        }
        else
        {
            Debug.LogError(
                "TurnManager belum dihubungkan!"
            );
        }

        sedangRoll = false;
    }
}

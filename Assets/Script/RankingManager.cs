using UnityEngine;
using TMPro;
using System.Linq;

public class RankingManager : MonoBehaviour
{
    public PlayerMoneyManager moneyManager;

    public GameObject[] panelPeringkat;
    public TMP_Text[] textNama;
    public TMP_Text[] textSaldo;

    public void TampilkanRanking()
    {
        if (moneyManager == null || moneyManager.saldoPemain == null)
        {
            Debug.LogError("Ranking: PlayerMoneyManager belum siap!");
            return;
        }

        int[] urutan = Enumerable.Range(
            0, moneyManager.saldoPemain.Length
        )
        .OrderByDescending(i => moneyManager.saldoPemain[i])
        .ToArray();

        for (int i = 0; i < panelPeringkat.Length; i++)
        {
            bool tampil = i < urutan.Length;
            panelPeringkat[i].SetActive(tampil);

            if (!tampil) continue;

            int id = urutan[i];

            textNama[i].text = MainMenuManager.modeMultiplayer
                ? "Player " + (id + 1)
                : id == 0 ? "Player 1" : "Bot " + (id + 1);

            textSaldo[i].text =
                "Rp" + moneyManager.saldoPemain[id].ToString("N0");
        }
    }
}

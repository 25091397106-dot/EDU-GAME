using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public GameObject panelMenang;
    public GameObject panelKalah;
    [Header("Final Ranking")]
    public GameObject panelRanking;
    public RankingManager rankingManager;

    void Start()
    {
        // Pastikan waktu berjalan normal setiap permainan baru dimulai
        Time.timeScale = 1f;

        // Sembunyikan kedua panel saat permainan dimulai
        panelMenang.SetActive(false);
        panelKalah.SetActive(false);
        panelRanking.SetActive(false);
    }

    public void TampilkanMenang()
    {
        panelKalah.SetActive(false);
        panelMenang.SetActive(true);

        if (!MainMenuManager.modeMultiplayer)
            Time.timeScale = 0f;
        StartCoroutine(TampilkanRankingSetelahDelay());
    }

    public void TampilkanKalah()
    {
        panelMenang.SetActive(false);
        panelKalah.SetActive(true);

        if (!MainMenuManager.modeMultiplayer)
            Time.timeScale = 0f;
        StartCoroutine(TampilkanRankingSetelahDelay());
    }
    private IEnumerator TampilkanRankingSetelahDelay()
    {
        yield return new WaitForSecondsRealtime(3f);

        panelMenang.SetActive(false);
        panelKalah.SetActive(false);

        rankingManager.TampilkanRanking();
        panelRanking.SetActive(true);
    }

    [ContextMenu("Tes Menang")]
    void TesMenang() { TampilkanMenang(); }

    [ContextMenu("Tes Kalah")]
    void TesKalah() { TampilkanKalah(); }
    public void MainLagi()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void KembaliKeMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
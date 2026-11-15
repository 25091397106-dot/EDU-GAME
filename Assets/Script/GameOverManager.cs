using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    public GameObject panelMenang;
    public GameObject panelKalah;

    void Start()
    {
        // Pastikan waktu berjalan normal setiap permainan baru dimulai
        Time.timeScale = 1f;

        // Sembunyikan kedua panel saat permainan dimulai
        panelMenang.SetActive(false);
        panelKalah.SetActive(false);
    }

    public void TampilkanMenang()
    {
        panelKalah.SetActive(false);
        panelMenang.SetActive(true);

        Time.timeScale = 0f;
    }

    public void TampilkanKalah()
    {
        panelMenang.SetActive(false);
        panelKalah.SetActive(true);

        Time.timeScale = 0f;
    }

    [ContextMenu("Tes Menang")]
    void TesMenang() { TampilkanMenang(); }

    [ContextMenu("Tes Kalah")]
    void TesKalah() { TampilkanKalah(); }
}
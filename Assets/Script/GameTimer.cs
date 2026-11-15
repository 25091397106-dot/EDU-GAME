
using UnityEngine;
using TMPro;

public class TimerGame : MonoBehaviour
{
    public float waktuPermainan = 20 * 60f;
    public TMP_Text teksTimer;
    public GameOverManager gameOverManager;

    private bool permainanSelesai = false;

    void Update()
    {
        if (permainanSelesai) return;

        waktuPermainan -= Time.deltaTime;

        if (waktuPermainan <= 0)
        {
            waktuPermainan = 0;
            permainanSelesai = true;
            AkhiriPermainan();
        }

        int menit = Mathf.FloorToInt(waktuPermainan / 60);
        int detik = Mathf.FloorToInt(waktuPermainan % 60);

        teksTimer.text = string.Format("{0:00}:{1:00}", menit, detik);
    }

    void AkhiriPermainan()
    {
        Debug.Log("Waktu habis! Permainan selesai.");

        if (gameOverManager != null)
        {
            gameOverManager.TampilkanKalah();
        }

        Time.timeScale = 0;
    }
}

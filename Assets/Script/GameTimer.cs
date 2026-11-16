using UnityEngine;
using TMPro;

public class TimerGame : MonoBehaviour
{
    public float waktuPermainan = 0f;
    public TMP_Text teksTimer;
    public GameOverManager gameOverManager;

    void Start()
    {
        waktuPermainan = 0f;
        TampilkanWaktu();
    }

    void Update()
    {
        waktuPermainan += Time.deltaTime;
        TampilkanWaktu();
    }

    void TampilkanWaktu()
    {
        if (teksTimer == null) return;

        int jam = Mathf.FloorToInt(waktuPermainan / 3600);
        int menit = Mathf.FloorToInt((waktuPermainan % 3600) / 60);
        int detik = Mathf.FloorToInt(waktuPermainan % 60);

        teksTimer.text = string.Format(
            "{0:00}:{1:00}:{2:00}",
            jam, menit, detik
        );
    }
}

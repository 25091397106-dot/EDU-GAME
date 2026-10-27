using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KartuSoal : MonoBehaviour
{
    [Header("Identitas Kartu")]
    public string namaPetak = "Seni Rupa";

    [Header("Pengelola Soal")]
    public SoalManager soalManager;

    [Header("Tampilan Pertanyaan")]
    public TMP_Text textPertanyaan;

    [Header("Tombol Jawaban")]
    public Button jawabanA;
    public Button jawabanB;
    public Button jawabanC;
    public Button jawabanD;
    [Header("Pengaturan Jarak Jawaban")]
    public JawabanLayout jawabanLayout;
    [ContextMenu("Tes Tampilkan Soal")]
    public void TampilkanSoal()
    {
        if (soalManager == null || textPertanyaan == null ||
            jawabanA == null || jawabanB == null ||
            jawabanC == null || jawabanD == null)
        {
            Debug.LogError(
                "Pengaturan KartuSoal belum lengkap pada " + gameObject.name
            );
            return;
        }

        Button[] tombol =
        {
            jawabanA,
            jawabanB,
            jawabanC,
            jawabanD
        };

        soalManager.BukaSoal(
            namaPetak,
            textPertanyaan,
            tombol
        );
        if (jawabanLayout != null)
        {
            StartCoroutine(RapikanSetelahSoalMuncul());
        }
    }
    private IEnumerator RapikanSetelahSoalMuncul()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();
        jawabanLayout.RapikanJawaban();
    }
}

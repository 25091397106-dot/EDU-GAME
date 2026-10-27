
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class JawabanLayout : MonoBehaviour
{
    public Button[] tombolJawaban = new Button[4];

    public float jarakAntarJawaban = 0.5f;
    public float posisiAtas = 11f;

    public void RapikanJawaban()
    {
        float posisiAtas = 11f;

        foreach (Button tombol in tombolJawaban)
        {
            if (tombol == null) continue;

            RectTransform rect = tombol.GetComponent<RectTransform>();
            TMP_Text teks = tombol.GetComponentInChildren<TMP_Text>();

            if (teks == null) continue;

            // Hitung tinggi teks berdasarkan lebar yang tersedia.
            float lebar = teks.rectTransform.rect.width;
            float tinggiTeks = teks.GetPreferredValues(
                teks.text, lebar, Mathf.Infinity
            ).y;

            float tinggi = Mathf.Max(6f, tinggiTeks + 1f);

            // Susun jawaban dari atas ke bawah.
            rect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical, tinggi
);
            rect.anchoredPosition = new Vector2(
                rect.anchoredPosition.x,
                posisiAtas - tinggi / 2f
            );

            posisiAtas -= tinggi + jarakAntarJawaban;
        }
    }
}

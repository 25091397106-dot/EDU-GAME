using System;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Menampilkan kartu EDUCARD dan menjalankan efeknya untuk Single Player vs Bot.</summary>
public class KartuEdupolyEffectManager : MonoBehaviour
{
    [Header("Referensi")]
    public KartuEdupolyManager dekKartu;
    public PlayerMoneyManager pengelolaUang;
    [Header("Pengaturan")]
    public float durasiKartuAksi = 3f;
    public float batasWaktuWawasan = 30f;
    public float jedaJawabanBot = 1.2f;
    [Range(0f, 1f)] public float peluangBotBenar = 0.75f;

    public bool mintaLemparUlang { get; private set; }
    public bool kehilanganGiliran { get; private set; }

    private Canvas uiCanvas;
    private GameObject panel;
    private TMP_Text judulText, isiText, timerText, hasilText;
    private Button[] pilihan = new Button[4];
    private bool sudahMemilih;
    private bool jawabanBenar;
    private int indeksBenar;
    private int indeksBenarTampil;
    private float sisaWaktu;

    public IEnumerator ProsesKartu(int idPemain, PionMovement pion)
    {
        mintaLemparUlang = false;
        kehilanganGiliran = false;
        if (dekKartu == null || pion == null)
        {
            Debug.LogError("EDUCARD: Dek Kartu atau Pion belum dihubungkan.");
            yield break;
        }

        DataKartuAksiEdupoly kartu = dekKartu.AmbilKartu();
        if (kartu == null) yield break;

        BuatUIJikaBelumAda();
        if (kartu.kategori == "Wawasan Umum")
        {
            if (!dekKartu.CobaAmbilSoalWawasanAktif(out DataKartuWawasanEdupoly soal) || soal == null)
            {
                Debug.LogError("EDUCARD: data soal Wawasan Umum tidak ditemukan.");
                yield break;
            }
            yield return TampilkanDanJalankanKuis(idPemain, soal);
            yield break;
        }

        // Dana Umum dan Kesempatan: tampilkan teks kartu terlebih dahulu.
        TampilkanPanel("EDUCARD - " + kartu.kategori, kartu.Kartu, kartu.Aksi, false);
        yield return new WaitForSeconds(durasiKartuAksi);
        SembunyikanPanel();
        yield return JalankanEfekAksi(idPemain, pion, kartu.Aksi ?? "");
    }

    private IEnumerator TampilkanDanJalankanKuis(int idPemain, DataKartuWawasanEdupoly soal)
    {
        string key = AmbilHurufKunci(soal.Jawaban);
        indeksBenar = "ABCD".IndexOf(key);
        if (indeksBenar < 0)
        {
            Debug.LogError("EDUCARD: kunci jawaban tidak valid: '" + soal.Jawaban + "'. Format yang diterima: C atau C. Gurita.");
            yield break;
        }

        string[] jawaban = { soal.A, soal.B, soal.C, soal.D };
        for (int i = 0; i < 4; i++)
        {
            if (string.IsNullOrWhiteSpace(jawaban[i]))
            {
                Debug.LogError("EDUCARD: pilihan " + (char)('A' + i) + " kosong pada soal nomor " + soal.No);
                yield break;
            }
        }

        TampilkanPanel("WAWASAN UMUM - " + soal.Level, soal.Pertanyaan, "Pilih satu jawaban yang benar.", true);
        int[] urutan = { 0, 1, 2, 3 };
        for (int i = 3; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (urutan[i], urutan[j]) = (urutan[j], urutan[i]);
        }
        for (int i = 0; i < 4; i++)
        {
            int original = urutan[i];
            if (original == indeksBenar) indeksBenarTampil = i;
            TMP_Text label = pilihan[i].GetComponentInChildren<TMP_Text>();
            label.text = ((char)('A' + i)) + ". " + jawaban[original];
            pilihan[i].onClick.RemoveAllListeners();
            pilihan[i].interactable = true;
            pilihan[i].onClick.AddListener(() => PilihJawaban(original == indeksBenar));
        }

        sudahMemilih = false;
        jawabanBenar = false;
        sisaWaktu = batasWaktuWawasan;
        float jedaBot = 0f;
        bool bot = idPemain != 0;
        while (!sudahMemilih && sisaWaktu > 0f)
        {
            sisaWaktu -= Time.deltaTime;
            timerText.text = "Waktu: " + Mathf.CeilToInt(Mathf.Max(0, sisaWaktu)) + " detik";
            if (bot)
            {
                jedaBot += Time.deltaTime;
                if (jedaBot >= jedaJawabanBot)
                {
                    bool botBenar = UnityEngine.Random.value <= peluangBotBenar;
                    int pilih = botBenar ? indeksBenarTampil : UnityEngine.Random.Range(0, 4);
                    pilihan[pilih].onClick.Invoke();
                }
            }
            yield return null;
        }
        if (!sudahMemilih) { sudahMemilih = true; jawabanBenar = false; }
        foreach (Button b in pilihan) b.interactable = false;
        hasilText.gameObject.SetActive(true);
        hasilText.text = jawabanBenar ? "✓ JAWABAN BENAR!" : "✕ JAWABAN SALAH / WAKTU HABIS";
        hasilText.color = jawabanBenar ? new Color(0.1f, 0.75f, 0.3f) : new Color(1f, 0.35f, 0.3f);
        Debug.Log("EDUCARD Wawasan Umum selesai. Jawaban " + (jawabanBenar ? "BENAR" : "SALAH/TIMEOUT"));
        yield return new WaitForSeconds(2f);
        SembunyikanPanel();
    }

    private void PilihJawaban(bool benar)
    {
        if (sudahMemilih) return;
        sudahMemilih = true;
        jawabanBenar = benar;
    }

    private IEnumerator JalankanEfekAksi(int pemain, PionMovement pion, string aksiAsli)
    {
        string aksi = aksiAsli.ToLowerInvariant();
        int nominal = AmbilAngka(aksiAsli);
        if (aksi.Contains("dapatkan rp") && nominal > 0)
        {
            if (pengelolaUang != null) pengelolaUang.TambahSaldo(pemain, nominal);
            else Debug.LogWarning("EDUCARD: PlayerMoneyManager belum dihubungkan.");
            yield break;
        }
        if (aksi.Contains("bayar rp") && nominal > 0)
        {
            if (pengelolaUang != null)
            {
                if (!pengelolaUang.KurangiSaldo(pemain, nominal)) Debug.LogWarning("EDUCARD: saldo tidak cukup untuk membayar.");
            }
            else Debug.LogWarning("EDUCARD: PlayerMoneyManager belum dihubungkan.");
            yield break;
        }
        if (aksi.Contains("maju") || aksi.Contains("mundur") || aksi.Contains("petak sebelumnya"))
        {
            int langkah = aksi.Contains("petak sebelumnya") ? -1 : nominal;
            if (aksi.Contains("mundur")) langkah = -Mathf.Abs(langkah);
            else langkah = Mathf.Abs(langkah);
            if (langkah != 0) { pion.Bergerak(langkah); yield return new WaitUntil(() => !pion.SedangBergerak); }
            yield break;
        }
        if (aksi.Contains("lempar dadu sekali lagi")) { mintaLemparUlang = true; yield break; }
        if (aksi.Contains("kehilangan 1 giliran")) { kehilanganGiliran = true; yield break; }
        if (aksi.Contains("lempar dadu: 1-3"))
        {
            int dadu = UnityEngine.Random.Range(1, 7);
            bool bayar = dadu <= 3;
            if (pengelolaUang != null)
            {
                if (bayar) pengelolaUang.KurangiSaldo(pemain, 20000);
                else pengelolaUang.TambahSaldo(pemain, 20000);
            }
            Debug.Log("EDUCARD: dadu bersyarat menghasilkan " + dadu + (bayar ? ", bayar Rp20.000" : ", mendapat Rp20.000"));
            yield break;
        }
        Debug.LogWarning("EDUCARD: efek belum otomatis didukung: " + aksiAsli);
    }

    private string AmbilHurufKunci(string teks)
    {
        if (string.IsNullOrWhiteSpace(teks)) return "";
        Match m = Regex.Match(teks.Trim().ToUpperInvariant(), @"^[\s]*([ABCD])(?:\s*\.|\s|$)");
        return m.Success ? m.Groups[1].Value : "";
    }

    private int AmbilAngka(string teks)
    {
        Match m = Regex.Match(teks ?? "", @"(\d[\d\.]*)");
        if (!m.Success) return 0;
        return int.TryParse(m.Value.Replace(".", ""), out int hasil) ? hasil : 0;
    }

    private void BuatUIJikaBelumAda()
    {
        if (panel != null) return;
        GameObject canvasGO = new GameObject("EDUCARD_RuntimeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        uiCanvas = canvasGO.GetComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 500;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        panel = BuatUI("PanelEDUCARD", canvasGO.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900, 650), new Color(0.035f, 0.08f, 0.13f, 0.96f), true);
        judulText = BuatTeks("Judul", panel.transform, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.98f), 34, TextAlignmentOptions.Center);
        isiText = BuatTeks("Isi", panel.transform, new Vector2(0.08f, 0.59f), new Vector2(0.92f, 0.84f), 28, TextAlignmentOptions.Center);
        timerText = BuatTeks("Timer", panel.transform, new Vector2(0.1f, 0.51f), new Vector2(0.9f, 0.58f), 22, TextAlignmentOptions.Center);
        hasilText = BuatTeks("Hasil", panel.transform, new Vector2(0.1f, 0.42f), new Vector2(0.9f, 0.5f), 28, TextAlignmentOptions.Center);
        string[] nama = { "A", "B", "C", "D" };
        for (int i = 0; i < 4; i++)
        {
            int row = i;
            float yTop = 0.39f - row * 0.105f;
            GameObject btnGO = BuatUI("Pilihan" + nama[i], panel.transform, new Vector2(0.08f, yTop - 0.085f), new Vector2(0.92f, yTop), new Vector2(0, 0), new Color(0.12f, 0.28f, 0.42f, 1), false);
            Button btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnGO.GetComponent<Image>();
            TMP_Text label = BuatTeks("Label", btnGO.transform, Vector2.zero, Vector2.one, 22, TextAlignmentOptions.Left);
            label.margin = new Vector4(24, 8, 18, 8);
            pilihan[i] = btn;
        }
        panel.SetActive(false);
    }

    private GameObject BuatUI(string nama, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Color warna, bool tengah)
    {
        GameObject go = new GameObject(nama, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        if (tengah) { rt.sizeDelta = size; rt.anchoredPosition = Vector2.zero; }
        go.GetComponent<Image>().color = warna;
        return go;
    }

    private TMP_Text BuatTeks(string nama, Transform parent, Vector2 anchorMin, Vector2 anchorMax, int ukuran, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(nama, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        TMP_Text t = go.GetComponent<TMP_Text>();
        t.fontSize = ukuran; t.color = Color.white; t.alignment = alignment; t.textWrappingMode = TextWrappingModes.Normal;
        t.text = "";
        return t;
    }

    private void TampilkanPanel(string judul, string isi, string sub, bool kuis)
    {
        judulText.text = judul;
        isiText.text = isi + (string.IsNullOrEmpty(sub) ? "" : "\n\n" + sub);
        timerText.gameObject.SetActive(kuis);
        hasilText.gameObject.SetActive(false);
        foreach (Button b in pilihan) b.gameObject.SetActive(kuis);
        panel.SetActive(true);
    }

    private void SembunyikanPanel()
    {
        if (panel != null) panel.SetActive(false);
    }
}

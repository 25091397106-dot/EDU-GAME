using System;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Menampilkan kartu EDUCARD dan menjalankan efeknya untuk Single Player vs Bot.</summary>
public class KartuEdupolyEffectManager : MonoBehaviour
{
    [Header("Tampilan Kartu Edupoly")]
    public Sprite gambarDepanKartu;
    public Sprite gambarBelakangKartu;
    [Header("UI Kartu Edupoly 3D")]
    public GameObject kartuEdupolyUI;
    public KartuSoal kartuSoalEdupoly;
    public CardFlip cardFlipEdupoly;
    public KartuEdupolyDisplay displayEdupoly;
    [Header("Popup Hasil Jawaban")]
    public SoalManager soalManagerEdupoly;
    private void TampilkanHasilEdupoly(bool benar)
    {
        if (soalManagerEdupoly == null)
        {
            Debug.LogError("SoalManager Edupoly belum dihubungkan!");
            return;
        }

        if (soalManagerEdupoly.panelPopUpHasil == null ||
            soalManagerEdupoly.teksHasilJawaban == null)
        {
            Debug.LogError("Popup hasil di SoalManager belum lengkap!");
            return;
        }

        soalManagerEdupoly.teksHasilJawaban.text =
            benar ? "JAWABAN BENAR!" : "JAWABAN SALAH!";

        soalManagerEdupoly.panelPopUpHasil.SetActive(true);
    }
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
    private void Start()
    {
        if (kartuEdupolyUI != null)
        {
            kartuEdupolyUI.SetActive(false);
        }
    }
    private Vector3 posisiAsliEdupoly;
    private Quaternion rotasiAsliEdupoly;
    private Vector3 skalaAsliEdupoly;

    private bool posisiEdupolyTersimpan;
    private void TampilkanKartuEdupoly3D()
    {
        if (displayEdupoly == null)
        {
            Debug.LogError("KartuEdupolyDisplay belum dihubungkan!");
            return;
        }

        displayEdupoly.Tampilkan();
    }
    private void SembunyikanKartuEdupoly3D()
    {
        if (displayEdupoly != null)
            displayEdupoly.Sembunyikan();
    }
    private TMP_Text teksSoalEdupoly;
    private Button[] tombolSoalEdupoly;

    private void SiapkanUIKartuEdupoly()
    {
        if (kartuSoalEdupoly == null)
        {
            Debug.LogError("KartuSoal Edupoly belum terhubung!");
            return;
        }

        teksSoalEdupoly = kartuSoalEdupoly.textPertanyaan;
        Transform canvasSoal = kartuEdupolyUI.transform.Find("DEPAN/CanvasSoal");

        if (canvasSoal != null)
        {
            canvasSoal.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("CanvasSoal Edupoly tidak ditemukan!");
        }

        tombolSoalEdupoly = new Button[]
        {
        kartuSoalEdupoly.jawabanA,
        kartuSoalEdupoly.jawabanB,
        kartuSoalEdupoly.jawabanC,
        kartuSoalEdupoly.jawabanD
        };
        foreach (Button tombol in tombolSoalEdupoly)
        {
            if (tombol != null)
                tombol.gameObject.SetActive(true);
        }
    }
    private Canvas uiCanvas;
    private GameObject panel;
    private TMP_Text judulText, isiText, timerText, hasilText;
    private Button[] pilihan = new Button[4];
    private bool sudahMemilih;
    private bool jawabanBenar;
    private int indeksBenar;
    private int indeksBenarTampil;
    private float sisaWaktu;
    private Coroutine notifikasiCoroutine;
    
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

    // Pemain 0 adalah manusia. Pemain selain 0 adalah bot.
    bool pemainManusia = idPemain == 0;

    if (kartu.kategori == "Wawasan Umum")
    {
        if (!dekKartu.CobaAmbilSoalWawasanAktif(
            out DataKartuWawasanEdupoly soal) || soal == null)
        {
            Debug.LogError("EDUCARD: data soal Wawasan Umum tidak ditemukan.");
            yield break;
        }

        if (pemainManusia)
        {   
            SiapkanUIKartuEdupoly();
            TampilkanKartuEdupoly3D();
            yield return TampilkanDanJalankanKuis(idPemain, soal);
            SembunyikanKartuEdupoly3D();
        }
        else
        {
            // Bot menjawab tanpa menampilkan panel EDUCARD.
            yield return ProsesKuisBot(idPemain, soal);
        }

        yield break;
    }

        // Kartu aksi hanya ditampilkan kepada pemain manusia.
        if (pemainManusia)
        {
            SiapkanUIKartuEdupoly();
            TampilkanKartuEdupoly3D();

            teksSoalEdupoly.text = kartu.Kartu;

            foreach (Button tombol in tombolSoalEdupoly)
            {
                tombol.gameObject.SetActive(false);
            }

            yield return new WaitForSeconds(durasiKartuAksi);

            SembunyikanKartuEdupoly3D();
        }

        yield return JalankanEfekAksi(
        idPemain,
        pion,
        kartu.Aksi ?? ""
    );
}

public IEnumerator JalankanKuisMultiplayer(
    DataKartuWawasanEdupoly soal, System.Action<bool> callbackHasil)
{
    if (soal == null)
    {
        Debug.LogError("EDUCARD MULTIPLAYER: Soal kosong.");
        callbackHasil?.Invoke(false);
        yield break;
    }

    string key = AmbilHurufKunci(soal.Jawaban);
    int indeksBenar = "ABCD".IndexOf(key);

    if (indeksBenar < 0)
    {
        Debug.LogError("EDUCARD MULTIPLAYER: Kunci jawaban tidak valid.");
        callbackHasil?.Invoke(false);
        yield break;
    }

    string[] jawaban = { soal.A, soal.B, soal.C, soal.D };

    for (int i = 0; i < 4; i++)
    {
        if (string.IsNullOrWhiteSpace(jawaban[i]))
        {
            Debug.LogError("EDUCARD MULTIPLAYER: Pilihan jawaban kosong.");
            callbackHasil?.Invoke(false);
            yield break;
        }
    }

    if (teksSoalEdupoly == null || tombolSoalEdupoly == null ||
         tombolSoalEdupoly.Length < 4)
        {
            Debug.LogError("EDUCARD: UI kartu soal belum terhubung!");
            callbackHasil?.Invoke(false);
            yield break;
        }

        teksSoalEdupoly.text = soal.Pertanyaan;
        SiapkanUIKartuEdupoly();
        TampilkanKartuEdupoly3D();
        pilihan = tombolSoalEdupoly;

        int[] urutan = { 0, 1, 2, 3 };

    for (int i = 3; i > 0; i--)
    {
        int j = UnityEngine.Random.Range(0, i + 1);
        (urutan[i], urutan[j]) = (urutan[j], urutan[i]);
    }

    sudahMemilih = false;
    jawabanBenar = false;

    for (int i = 0; i < 4; i++)
    {
        int original = urutan[i];

        TMP_Text label =
            pilihan[i].GetComponentInChildren<TMP_Text>();

        label.text = ((char)('A' + i)) + ". " + jawaban[original];

        pilihan[i].onClick.RemoveAllListeners();
        pilihan[i].interactable = true;

        pilihan[i].onClick.AddListener(
            () => PilihJawaban(original == indeksBenar)
        );
    }

    sisaWaktu = batasWaktuWawasan;

    while (!sudahMemilih && sisaWaktu > 0f)
    {
        sisaWaktu -= Time.deltaTime;

        timerText.text =
            "Waktu: " +
            Mathf.CeilToInt(Mathf.Max(0, sisaWaktu)) +
            " detik";

        yield return null;
    }

    if (!sudahMemilih)
    {
        sudahMemilih = true;
        jawabanBenar = false;
    }

    foreach (Button b in pilihan)
        b.interactable = false;

    hasilText.gameObject.SetActive(true);

    Debug.Log(jawabanBenar
        ? "EDUCARD: JAWABAN BENAR!"
        : "EDUCARD: JAWABAN SALAH / WAKTU HABIS");
   
    hasilText.color = jawabanBenar
        ? new Color(0.1f, 0.75f, 0.3f)
        : new Color(1f, 0.35f, 0.3f);

    callbackHasil?.Invoke(jawabanBenar);

    yield return new WaitForSeconds(2f);
    SembunyikanKartuEdupoly3D();
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

        teksSoalEdupoly.text = soal.Pertanyaan;
        pilihan = tombolSoalEdupoly;
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
        
        Debug.Log("EDUCARD Wawasan Umum selesai. Jawaban " + (jawabanBenar ? "BENAR" : "SALAH/TIMEOUT"));
        // Hadiah atau denda setelah menjawab Wawasan Umum.
if (pengelolaUang != null)
{
    if (jawabanBenar)
    {
        pengelolaUang.TambahSaldo(idPemain, 200000);
        Debug.Log("EDUCARD: Jawaban benar! Mendapatkan Rp200.000.");
    }
    else
    {
        pengelolaUang.KurangiSaldo(idPemain, 100000);
        Debug.Log("EDUCARD: Jawaban salah atau waktu habis. Membayar Rp100.000.");
    }
}
else
{
    Debug.LogWarning("EDUCARD: PlayerMoneyManager belum dihubungkan.");
}
        TampilkanHasilEdupoly(jawabanBenar);

        yield return new WaitForSeconds(2f);

        if (soalManagerEdupoly != null &&
            soalManagerEdupoly.panelPopUpHasil != null)
        {
            soalManagerEdupoly.panelPopUpHasil.SetActive(false);
        }
    }

private IEnumerator ProsesKuisBot(
    int idPemain,
    DataKartuWawasanEdupoly soal)
{
    string key = AmbilHurufKunci(soal.Jawaban);
    int jawabanBenar = "ABCD".IndexOf(key);

    if (jawabanBenar < 0)
    {
        Debug.LogError(
            "EDUCARD BOT: Kunci jawaban tidak valid: " +
            soal.Jawaban
        );
        yield break;
    }

    string[] jawaban = {
        soal.A, soal.B, soal.C, soal.D
    };

    for (int i = 0; i < 4; i++)
    {
        if (string.IsNullOrWhiteSpace(jawaban[i]))
        {
            Debug.LogError(
                "EDUCARD BOT: Pilihan jawaban kosong."
            );
            yield break;
        }
    }

    // Bot berpikir sebentar sebelum menjawab.
    yield return new WaitForSeconds(jedaJawabanBot);

    bool benar =
        UnityEngine.Random.value <= peluangBotBenar;

    Debug.Log(
        "EDUCARD BOT menjawab soal nomor " +
        soal.No + ": " +
        (benar ? "BENAR" : "SALAH")
    );

if (pengelolaUang != null)
{
    if (benar)
    {
        pengelolaUang.TambahSaldo(idPemain, 200000);
        Debug.Log("EDUCARD BOT mendapat Rp200.000.");
    }
    else
    {
        pengelolaUang.KurangiSaldo(idPemain, 100000);
        Debug.Log("EDUCARD BOT membayar Rp100.000.");
    }
}
else
{
    Debug.LogWarning(
        "EDUCARD: PlayerMoneyManager belum dihubungkan."
    );
    }
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

    [ContextMenu("TEST TAMPILAN KARTU EDUPOLY")]
    public void TestTampilanKartu()
    {
        BuatUIJikaBelumAda();

        TampilkanPanel(
            "WAWASAN UMUM",
            "Apa nama planet terbesar di tata surya?",
            "",
            true
        );

        string[] jawaban =
        {
        "A. Bumi",
        "B. Mars",
        "C. Jupiter",
        "D. Venus"
    };

        for (int i = 0; i < 4; i++)
        {
            TMP_Text teks = pilihan[i]
                .GetComponentInChildren<TMP_Text>();

            teks.text = jawaban[i];
        }
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

        panel = BuatUI("PanelEDUCARD", canvasGO.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(520, 740), new Color(0.035f, 0.08f, 0.13f, 0.96f), true);
        judulText = BuatTeks("Judul", panel.transform,
            new Vector2(0.16f, 0.7f), new Vector2(0.84f, 0.715f),
            20, TextAlignmentOptions.Center);
        judulText.gameObject.SetActive(false);

        isiText = BuatTeks("Isi", panel.transform,
            new Vector2(0.16f, 0.4f), new Vector2(0.84f, 0.68f),
            30, TextAlignmentOptions.Center);

        timerText = BuatTeks("Timer", panel.transform,
            new Vector2(0.16f, 0.48f), new Vector2(0.84f, 0.52f),
            18, TextAlignmentOptions.Center);

        hasilText = BuatTeks("Hasil", panel.transform,
            new Vector2(0.16f, 0.43f), new Vector2(0.84f, 0.48f),
            23, TextAlignmentOptions.Center);
        string[] nama = { "A", "B", "C", "D" };

        Image gambarPanel = panel.GetComponent<Image>();

        if (gambarPanel != null && gambarDepanKartu != null)
        {
            gambarPanel.sprite = gambarDepanKartu;
            gambarPanel.type = Image.Type.Simple;
            gambarPanel.color = Color.white;
            gambarPanel.preserveAspect = true;
        }

        for (int i = 0; i < 4; i++)
        {
            int row = i;
            float yTop = 0.44f - row * 0.075f;
            GameObject btnGO = BuatUI("Pilihan" + nama[i], panel.transform, new Vector2(0.16f, yTop - 0.06f), new Vector2(0.84f, yTop), new Vector2(0, 0), new Color(0.12f, 0.28f, 0.42f, 1), false);
            Button btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnGO.GetComponent<Image>();
            TMP_Text label = BuatTeks("Label", btnGO.transform, Vector2.zero, Vector2.one, 22, TextAlignmentOptions.Left);
            label.margin = new Vector4(12, 4, 10, 4);
            pilihan[i] = btn;
        }
        Color warnaTeks = new Color(0.25f, 0.16f, 0.11f);

        judulText.color = warnaTeks;
        isiText.color = warnaTeks;
        timerText.color = warnaTeks;
        hasilText.color = warnaTeks;
        judulText.color = new Color(0.30f, 0.16f, 0.08f);
        judulText.fontStyle = FontStyles.Bold;

        isiText.color = new Color(0.20f, 0.12f, 0.08f);
        isiText.fontStyle = FontStyles.Bold;
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

// ==========================================
// NOTIFIKASI UMUM EDUCARD
// Untuk Multiplayer dan pengumuman kemenangan
// ==========================================
public void TampilkanNotifikasiSistem(
    string judul,
    string isi,
    float durasi = 5f)
{
    BuatUIJikaBelumAda();

    if (notifikasiCoroutine != null)
        StopCoroutine(notifikasiCoroutine);

    TampilkanPanel(
        judul,
        isi,
        "",
        false
    );

    notifikasiCoroutine = StartCoroutine(
        TutupNotifikasiSistem(durasi)
    );
}

private IEnumerator TutupNotifikasiSistem(float durasi)
{
    yield return new WaitForSeconds(
        Mathf.Max(0.1f, durasi)
    );

    SembunyikanPanel();
    notifikasiCoroutine = null;
}
private void TampilkanPanel(string judul, string isi, string sub, bool kuis)
{
    judulText.text = judul;
    isiText.text = isi + (string.IsNullOrEmpty(sub) ? "" : "\n\n" + sub);

    // Panel kuis tetap tinggi agar empat pilihan jawaban muat.
    // Panel kartu aksi dibuat lebih pendek agar tidak banyak ruang kosong.
    RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(600, 850);

        timerText.gameObject.SetActive(kuis);
    hasilText.gameObject.SetActive(false);

    foreach (Button b in pilihan)
        b.gameObject.SetActive(kuis);

    panel.SetActive(true);
}

    private void SembunyikanPanel()
    {
        if (panel != null) panel.SetActive(false);
    }
}

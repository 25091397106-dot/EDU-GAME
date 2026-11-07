
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class DataSoal
{
    public string komplek;
    public string petak;
    public int nomor;

    // 1 = Pembelian
    // 2 = Landmark / Pengambilalihan
    public int level = 1;

    public string pertanyaan;
    public string A;
    public string B;
    public string C;
    public string D;
    public string jawaban;
}

[Serializable]
public class KumpulanSoal
{
    public DataSoal[] soal;
}

public class SoalManager : MonoBehaviour
{
    [Header("Pop Up Hasil Jawaban")]
    public GameObject panelPopUpHasil;
    public TMP_Text teksHasilJawaban;
    public CardDisplayManager cardDisplayManager;

    [Header("Database Soal")]
    public TextAsset fileSoal;

    [Header("Timer Soal")]
    public float batasWaktuSoal = 30f;

    private KumpulanSoal database;

    private Dictionary<string, List<int>> urutanAcak =
        new Dictionary<string, List<int>>();

    private Dictionary<string, int> indeksSoal =
        new Dictionary<string, int>();

    private TMP_Text textPertanyaan;
    private Button[] tombolJawaban;
    private DataSoal soalAktif;
    private bool sudahMenjawab;

    private Coroutine timerSoal;
    private Coroutine popUpAktif;

    public event Action<bool> OnJawabanSelesai;

    void Awake()
    {
        if (fileSoal == null)
        {
            fileSoal =
                Resources.Load<TextAsset>("SoalEdupoly");
        }

        if (fileSoal == null)
        {
            Debug.LogError(
                "File SoalEdupoly.json tidak ditemukan!"
            );
            return;
        }

        database =
            JsonUtility.FromJson<KumpulanSoal>(
                fileSoal.text
            );

        if (database == null ||
            database.soal == null)
        {
            Debug.LogError(
                "Format database soal tidak sesuai!"
            );
            return;
        }

        Debug.Log(
            "EDU-POLY: Database berisi " +
            database.soal.Length + " soal."
        );
    }

    // Pemanggilan lama otomatis menggunakan Level 1.
    // Jadi script pembelian lama tetap kompatibel.
    public void BukaSoal(
        string namaPetak,
        TMP_Text teks,
        Button[] tombol)
    {
        BukaSoal(namaPetak, teks, tombol, 1);
    }

    // Pemanggilan baru dengan level tertentu.
    public void BukaSoal(
        string namaPetak,
        TMP_Text teks,
        Button[] tombol,
        int level)
    {
        if (database == null ||
            database.soal == null)
        {
            Debug.LogError(
                "Database soal belum tersedia!"
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(namaPetak) ||
            teks == null ||
            tombol == null ||
            tombol.Length != 4)
        {
            Debug.LogError(
                "Nama petak atau UI soal belum lengkap!"
            );
            return;
        }

        if (level != 1 && level != 2)
        {
            Debug.LogError(
                "Level soal tidak valid: " + level
            );
            return;
        }

        List<DataSoal> daftar =
            new List<DataSoal>();

        foreach (DataSoal soal in database.soal)
        {
            if (soal == null)
                continue;

            bool petakSesuai = string.Equals(
                soal.petak?.Trim(),
                namaPetak.Trim(),
                StringComparison.OrdinalIgnoreCase
            );

            if (petakSesuai &&
                soal.level == level)
            {
                daftar.Add(soal);
            }
        }

        if (daftar.Count == 0)
        {
            Debug.LogWarning(
                "Tidak ada soal Level " + level +
                " untuk petak " + namaPetak
            );
            return;
        }

        daftar.Sort(
            (a, b) => a.nomor.CompareTo(b.nomor)
        );

        // Pisahkan urutan acak setiap petak dan level.
        string kunciPetak =
            namaPetak.Trim().ToLowerInvariant() +
            "_level_" + level;

        if (!urutanAcak.ContainsKey(kunciPetak) ||
            !indeksSoal.ContainsKey(kunciPetak) ||
            indeksSoal[kunciPetak] >= daftar.Count)
        {
            List<int> urutanBaru =
                new List<int>();

            for (int i = 0; i < daftar.Count; i++)
            {
                urutanBaru.Add(i);
            }

            for (int i = urutanBaru.Count - 1;
                 i > 0;
                 i--)
            {
                int j = UnityEngine.Random.Range(
                    0, i + 1
                );

                int sementara = urutanBaru[i];
                urutanBaru[i] = urutanBaru[j];
                urutanBaru[j] = sementara;
            }

            urutanAcak[kunciPetak] = urutanBaru;
            indeksSoal[kunciPetak] = 0;
        }

        int indeks =
            urutanAcak[kunciPetak][
                indeksSoal[kunciPetak]
            ];

        DataSoal soalDipilih = daftar[indeks];

        string teksKunci = (soalDipilih.jawaban ?? "").Trim().ToUpperInvariant();
        Match matchKunci = Regex.Match(teksKunci, @"^([ABCD])(?:\s*\.|\s|$)");
        int kunci = matchKunci.Success ? "ABCD".IndexOf(matchKunci.Groups[1].Value) : -1;

        if (kunci < 0 ||
            kunci > 3)
        {
            Debug.LogError(
                "Kunci jawaban tidak valid pada " +
                namaPetak
            );
            return;
        }

        foreach (Button tombolAktif in tombol)
        {
            if (tombolAktif == null ||
                tombolAktif.GetComponentInChildren<TMP_Text>()
                    == null)
            {
                Debug.LogError(
                    "Ada tombol jawaban yang belum lengkap!"
                );
                return;
            }
        }

        // Soal valid, baru mulai sesi baru.
        BatalkanSoal();

        if (popUpAktif != null)
        {
            StopCoroutine(popUpAktif);
            popUpAktif = null;
        }

        if (panelPopUpHasil != null)
            panelPopUpHasil.SetActive(false);

        soalAktif = soalDipilih;
        indeksSoal[kunciPetak]++;

        textPertanyaan = teks;
        tombolJawaban = tombol;
        sudahMenjawab = false;

        textPertanyaan.text =
            soalAktif.pertanyaan;

        string[] isiJawaban =
        {
            soalAktif.A,
            soalAktif.B,
            soalAktif.C,
            soalAktif.D
        };

        // Acak posisi pilihan jawaban.
        List<int> urutan =
            new List<int> { 0, 1, 2, 3 };

        for (int i = urutan.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(
                0, i + 1
            );

            int sementara = urutan[i];
            urutan[i] = urutan[j];
            urutan[j] = sementara;
        }

        for (int i = 0; i < 4; i++)
        {
            int indeksAsli = urutan[i];

            Button tombolAktif =
                tombolJawaban[i];

            TMP_Text teksTombol =
                tombolAktif
                    .GetComponentInChildren<TMP_Text>();

            teksTombol.richText = true;

            teksTombol.text =
                ((char)('A' + i)) + ". " +
                "<indent=12%>" +
                isiJawaban[indeksAsli] +
                "</indent>";

            tombolAktif.onClick.RemoveAllListeners();

            bool benar =
                indeksAsli == kunci;

            tombolAktif.onClick.AddListener(
                () => PeriksaJawaban(benar)
            );

            tombolAktif.interactable = true;
        }

        timerSoal =
            StartCoroutine(HitungWaktuSoal());

        Debug.Log(
            "EDU-POLY: Soal Level " +
            level + " dibuka untuk " +
            namaPetak
        );
    }

    // Jalur khusus EDUCARD: memakai soal langsung dari database kartu,
    // tanpa mencari nama petak pada database soal properti.
    public void BukaSoalWawasan(
        DataKartuWawasanEdupoly data,
        TMP_Text teks,
        Button[] tombol)
    {
        if (data == null || teks == null || tombol == null || tombol.Length != 4)
        {
            Debug.LogError("EDUCARD: data soal atau referensi UI belum lengkap.");
            return;
        }

        string kunciTeks = (data.Jawaban ?? "").Trim().ToUpperInvariant();
        Match match = Regex.Match(kunciTeks, @"^([ABCD])(?:\s*\.|\s|$)");
        int kunci = match.Success ? "ABCD".IndexOf(match.Groups[1].Value) : -1;
        if (kunci < 0)
        {
            Debug.LogError("EDUCARD: format kunci jawaban tidak valid: " + data.Jawaban);
            return;
        }

        string[] isi = { data.A, data.B, data.C, data.D };
        for (int i = 0; i < 4; i++)
        {
            if (tombol[i] == null || tombol[i].GetComponentInChildren<TMP_Text>() == null ||
                string.IsNullOrWhiteSpace(isi[i]))
            {
                Debug.LogError("EDUCARD: tombol/pilihan " + (char)('A' + i) + " belum lengkap.");
                return;
            }
        }

        BatalkanSoal();
        if (popUpAktif != null)
        {
            StopCoroutine(popUpAktif);
            popUpAktif = null;
        }
        if (panelPopUpHasil != null) panelPopUpHasil.SetActive(false);

        textPertanyaan = teks;
        tombolJawaban = tombol;
        sudahMenjawab = false;
        textPertanyaan.text = data.Pertanyaan;

        List<int> urutan = new List<int> { 0, 1, 2, 3 };
        for (int i = urutan.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            int tmp = urutan[i]; urutan[i] = urutan[j]; urutan[j] = tmp;
        }

        for (int i = 0; i < 4; i++)
        {
            int indeksAsli = urutan[i];
            TMP_Text label = tombol[i].GetComponentInChildren<TMP_Text>();
            label.text = ((char)('A' + i)) + ". " + isi[indeksAsli];
            tombol[i].onClick.RemoveAllListeners();
            bool benar = indeksAsli == kunci;
            tombol[i].onClick.AddListener(() => PeriksaJawaban(benar));
            tombol[i].interactable = true;
        }

        timerSoal = StartCoroutine(HitungWaktuSoal());
        Debug.Log("EDUCARD: soal Wawasan Umum nomor " + data.No + " ditampilkan.");
    }

    private IEnumerator HitungWaktuSoal()
    {
        yield return new WaitForSeconds(
            batasWaktuSoal
        );

        timerSoal = null;

        if (!sudahMenjawab)
        {
            Debug.Log(
                "Waktu soal habis! Jawaban salah."
            );

            PeriksaJawaban(false);
        }
    }

    public void BatalkanSoal()
    {
        if (timerSoal != null)
        {
            StopCoroutine(timerSoal);
            timerSoal = null;
        }

        sudahMenjawab = true;
    }

    private IEnumerator TampilkanPopUpHasil(
        bool benar)
    {
        if (panelPopUpHasil == null ||
            teksHasilJawaban == null)
        {
            popUpAktif = null;
            yield break;
        }

        teksHasilJawaban.text =
            benar
            ? "✓ Jawaban Benar!"
            : "✕ Jawaban Salah!";

        teksHasilJawaban.color =
            benar
            ? new Color(0.09f, 0.5f, 0.24f)
            : Color.red;

        panelPopUpHasil.SetActive(true);

        yield return new WaitForSeconds(2f);

        panelPopUpHasil.SetActive(false);

        if (cardDisplayManager != null)
        {
            cardDisplayManager.SembunyikanKartu();
        }

        popUpAktif = null;
    }

    private void PeriksaJawaban(bool benar)
    {
        if (sudahMenjawab)
            return;

        sudahMenjawab = true;

        if (timerSoal != null)
        {
            StopCoroutine(timerSoal);
            timerSoal = null;
        }

        if (tombolJawaban != null)
        {
            foreach (Button tombol in tombolJawaban)
            {
                if (tombol != null)
                    tombol.interactable = false;
            }
        }

        Debug.Log(
            benar
            ? "Jawaban BENAR!"
            : "Jawaban SALAH!"
        );

        popUpAktif =
            StartCoroutine(
                TampilkanPopUpHasil(benar)
            );

        OnJawabanSelesai?.Invoke(benar);
    }
}

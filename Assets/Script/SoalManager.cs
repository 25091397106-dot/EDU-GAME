
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class DataSoal
{
    public string komplek;
    public string petak;
    public int nomor;
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

    private KumpulanSoal database;

    // Menyimpan urutan soal acak untuk setiap petak
    private Dictionary<string, List<int>> urutanAcak =
        new Dictionary<string, List<int>>();

    private Dictionary<string, int> indeksSoal =
        new Dictionary<string, int>();

    private TMP_Text textPertanyaan;
    private Button[] tombolJawaban;
    private DataSoal soalAktif;
    private bool sudahMenjawab;
    [Header("Timer Soal")]
    public float batasWaktuSoal = 30f;

    private Coroutine timerSoal;

    private System.Collections.IEnumerator HitungWaktuSoal()
    {
        yield return new WaitForSeconds(batasWaktuSoal);

        timerSoal = null;

        if (!sudahMenjawab)
        {
            Debug.Log("Waktu menjawab soal habis! Jawaban dianggap salah.");
            PeriksaJawaban(false);
        }
    }

    // Nantinya dipakai sistem pembelian properti
    public event Action<bool> OnJawabanSelesai;
    public void BatalkanSoal()
    {
        if (timerSoal != null)
        {
            StopCoroutine(timerSoal);
            timerSoal = null;
        }

        sudahMenjawab = true;
    }

    void Awake()
    {
        if (fileSoal == null)
        {
            fileSoal = Resources.Load<TextAsset>("SoalEdupoly");
        }

        if (fileSoal == null)
        {
            Debug.LogError("File SoalEdupoly.json tidak ditemukan!");
            return;
        }

        database = JsonUtility.FromJson<KumpulanSoal>(
            fileSoal.text
        );

        if (database == null || database.soal == null)
        {
            Debug.LogError("Format database soal tidak sesuai!");
        }
    }

    public void BukaSoal(
        string namaPetak,
        TMP_Text teks,
        Button[] tombol)
    {
        if (database == null || database.soal == null)
            return;

        if (teks == null || tombol == null ||
            tombol.Length != 4)
        {
            Debug.LogError("UI soal belum lengkap!");
            return;
        }

        List<DataSoal> daftar = new List<DataSoal>();

        foreach (DataSoal soal in database.soal)
        {
            if (string.Equals(
                soal.petak,
                namaPetak,
                StringComparison.OrdinalIgnoreCase))
            {
                daftar.Add(soal);
            }
        }

        if (daftar.Count == 0)
        {
            Debug.LogWarning(
                "Soal tidak ditemukan untuk petak: " + namaPetak
            );
            return;
        }

        daftar.Sort((a, b) => a.nomor.CompareTo(b.nomor));

        string kunciPetak = namaPetak.Trim().ToLowerInvariant();

        // Buat urutan acak baru jika belum ada atau semua soal sudah keluar
        if (!urutanAcak.ContainsKey(kunciPetak) ||
            indeksSoal[kunciPetak] >= daftar.Count)
        {
            List<int> urutanBaru = new List<int>();

            for (int i = 0; i < daftar.Count; i++)
            {
                urutanBaru.Add(i);
            }

            // Acak urutan soal
            for (int i = urutanBaru.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);

                int sementara = urutanBaru[i];
                urutanBaru[i] = urutanBaru[j];
                urutanBaru[j] = sementara;
            }

            urutanAcak[kunciPetak] = urutanBaru;
            indeksSoal[kunciPetak] = 0;
        }

        // Ambil soal berikutnya dari urutan acak
        int indeks = urutanAcak[kunciPetak][indeksSoal[kunciPetak]];
        soalAktif = daftar[indeks];

        indeksSoal[kunciPetak]++;
        textPertanyaan = teks;
        tombolJawaban = tombol;
        sudahMenjawab = false;

        textPertanyaan.text = soalAktif.pertanyaan;

        string[] isiJawaban =
        {
            soalAktif.A,
            soalAktif.B,
            soalAktif.C,
            soalAktif.D
        };

        int kunci = "ABCD".IndexOf(
            soalAktif.jawaban.Trim().ToUpper()
        );

        if (kunci < 0)
        {
            Debug.LogError("Kunci jawaban tidak valid!");
            return;
        }

        // Acak posisi pilihan jawaban
        List<int> urutan = new List<int> { 0, 1, 2, 3 };

        for (int i = urutan.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);

            int sementara = urutan[i];
            urutan[i] = urutan[j];
            urutan[j] = sementara;
        }

        for (int i = 0; i < 4; i++)
        {
            int indeksAsli = urutan[i];

            Button tombolAktif = tombolJawaban[i];

            TMP_Text teksTombol =
                tombolAktif.GetComponentInChildren<TMP_Text>();

            if (teksTombol == null)
            {
                Debug.LogError("Teks tombol tidak ditemukan!");
                return;
            }

            teksTombol.richText = true;

            teksTombol.text =
                ((char)('A' + i)) + ". " +
                "<indent=12%>" +
                isiJawaban[indeksAsli] +
                "</indent>";

            tombolAktif.onClick.RemoveAllListeners();

            bool benar = indeksAsli == kunci;

            tombolAktif.onClick.AddListener(
                () => PeriksaJawaban(benar)
            );

            tombolAktif.interactable = true;
        }
        if (timerSoal != null)
        {
            StopCoroutine(timerSoal);
        }

        timerSoal = StartCoroutine(HitungWaktuSoal());
    }
    private System.Collections.IEnumerator TampilkanPopUpHasil(bool benar)
    {
        if (panelPopUpHasil == null || teksHasilJawaban == null)
            yield break;

        teksHasilJawaban.text = benar
            ? "✓ Jawaban Benar!"
            : "✕ Jawaban Salah!";

        teksHasilJawaban.color = benar
            ? new Color(0.09f, 0.5f, 0.24f)
            : Color.red;

        panelPopUpHasil.SetActive(true);

        yield return new WaitForSeconds(2f);

        panelPopUpHasil.SetActive(false);

        if (cardDisplayManager != null)
        {
            cardDisplayManager.SembunyikanKartu();
        }
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
        StartCoroutine(TampilkanPopUpHasil(benar));

        foreach (Button tombol in tombolJawaban)
        {
            tombol.interactable = false;
        }

        if (benar)
        {
            Debug.Log(
                "BENAR! Pemain boleh membeli properti."
            );
        }
        else
        {
            Debug.Log(
                "SALAH! Pemain tidak boleh membeli properti."
            );
        }

        OnJawabanSelesai?.Invoke(benar);
    }
}

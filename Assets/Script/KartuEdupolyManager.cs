using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DataKartuAksiEdupoly
{
    public int No;
    public string Kartu;
    public string Aksi;
    public string kategori;
}

[Serializable]
public class DataKartuWawasanEdupoly
{
    public int No;
    public string Level;
    public string Pertanyaan;
    public string A;
    public string B;
    public string C;
    public string D;
    public string Jawaban;
}

[Serializable]
public class WrapperKartuAksiEdupoly
{
    public DataKartuAksiEdupoly[] items;
}

[Serializable]
public class WrapperKartuWawasanEdupoly
{
    public DataKartuWawasanEdupoly[] items;
}

/// <summary>
/// Pengelola dek gabungan 9 kartu EDUCARD.
/// Tidak membutuhkan panel, TMP Text, atau tombol UI kartu.
/// </summary>
public class KartuEdupolyManager : MonoBehaviour
{
    [Header("Database Kartu")]
    [Tooltip("Masukkan Edupoly_Kartu_Permainan.json sebagai TextAsset.")]
    public TextAsset databaseKartu;

    [Header("Waypoint Petak EDUCARD")]
    public int[] waypointKartu = { 3, 15, 24, 30 };

    private readonly List<DataKartuAksiEdupoly> semuaKartu = new List<DataKartuAksiEdupoly>();
    private readonly List<DataKartuAksiEdupoly> dekAcak = new List<DataKartuAksiEdupoly>();
    private readonly Dictionary<DataKartuAksiEdupoly, DataKartuWawasanEdupoly> indeksSoalWawasan =
        new Dictionary<DataKartuAksiEdupoly, DataKartuWawasanEdupoly>();

    private DataKartuAksiEdupoly kartuAktif;
    private bool databaseSiap;

    /// <summary>Dikirim setelah kartu diambil. Sistem lain dapat menampilkan teks/menjalankan efek.</summary>
    public event Action<DataKartuAksiEdupoly> OnKartuDiambil;

    public DataKartuAksiEdupoly KartuAktif => kartuAktif;
    public bool DatabaseSiap => databaseSiap;
    public int SisaKartuDiDek => dekAcak.Count;

    private void Awake()
    {
        MuatDatabase();
    }

    private void MuatDatabase()
    {
        semuaKartu.Clear();
        dekAcak.Clear();
        indeksSoalWawasan.Clear();
        databaseSiap = false;

        if (databaseKartu == null)
        {
            Debug.LogError("KartuEdupolyManager: Database kartu belum diisi di Inspector.");
            return;
        }

        string json = databaseKartu.text;
        string jsonDana = AmbilArrayJSON(json, "Dana Umum");
        string jsonKesempatan = AmbilArrayJSON(json, "Kesempatan");
        string jsonWawasan = AmbilArrayJSON(json, "Wawasan Umum");

        if (string.IsNullOrEmpty(jsonDana) || string.IsNullOrEmpty(jsonKesempatan) || string.IsNullOrEmpty(jsonWawasan))
        {
            Debug.LogError("KartuEdupolyManager: JSON harus memiliki Dana Umum, Kesempatan, dan Wawasan Umum.");
            return;
        }

        WrapperKartuAksiEdupoly dana = JsonUtility.FromJson<WrapperKartuAksiEdupoly>("{\"items\":" + jsonDana + "}");
        WrapperKartuAksiEdupoly kesempatan = JsonUtility.FromJson<WrapperKartuAksiEdupoly>("{\"items\":" + jsonKesempatan + "}");
        WrapperKartuWawasanEdupoly wawasan = JsonUtility.FromJson<WrapperKartuWawasanEdupoly>("{\"items\":" + jsonWawasan + "}");

        if (dana?.items == null || kesempatan?.items == null || wawasan?.items == null)
        {
            Debug.LogError("KartuEdupolyManager: Gagal membaca data JSON.");
            return;
        }

        foreach (DataKartuAksiEdupoly data in dana.items)
        {
            data.kategori = "Dana Umum";
            semuaKartu.Add(data);
        }

        foreach (DataKartuAksiEdupoly data in kesempatan.items)
        {
            data.kategori = "Kesempatan";
            semuaKartu.Add(data);
        }

        foreach (DataKartuWawasanEdupoly soal in wawasan.items)
        {
            DataKartuAksiEdupoly kartu = new DataKartuAksiEdupoly
            {
                No = soal.No,
                Kartu = "WAWASAN UMUM - " + soal.Level,
                Aksi = soal.Pertanyaan,
                kategori = "Wawasan Umum"
            };
            indeksSoalWawasan.Add(kartu, soal);
            semuaKartu.Add(kartu);
        }

        
int jumlahDana = dana.items.Length;
int jumlahKesempatan = kesempatan.items.Length;
int jumlahWawasan = wawasan.items.Length;

if (jumlahDana != 3 ||
    jumlahKesempatan != 3 ||
    jumlahWawasan != 3 ||
    semuaKartu.Count != 9)
{
    Debug.LogError(
        "EDUCARD harus berisi tepat 3 Dana Umum, " +
        "3 Kesempatan, dan 3 Wawasan Umum. " +
        "Jumlah terbaca: Dana=" + jumlahDana +
        ", Kesempatan=" + jumlahKesempatan +
        ", Wawasan=" + jumlahWawasan
    );

    semuaKartu.Clear();
    indeksSoalWawasan.Clear();
    dekAcak.Clear();
    databaseSiap = false;
    return;
}


        databaseSiap = true;
        IsiUlangDanAcakDek();
        Debug.Log("KartuEdupolyManager: 9 kartu berhasil dimuat dalam satu dek gabungan.");
    }

    private string AmbilArrayJSON(string json, string namaKunci)
    {
        string kunci = "\"" + namaKunci + "\"";
        int posisiKunci = json.IndexOf(kunci, StringComparison.Ordinal);
        if (posisiKunci < 0) return null;

        int awal = json.IndexOf('[', posisiKunci + kunci.Length);
        if (awal < 0) return null;

        bool diDalamString = false;
        bool escape = false;
        int kedalaman = 0;
        for (int i = awal; i < json.Length; i++)
        {
            char c = json[i];
            if (diDalamString)
            {
                if (escape) escape = false;
                else if (c == '\\') escape = true;
                else if (c == '"') diDalamString = false;
                continue;
            }

            if (c == '"') diDalamString = true;
            else if (c == '[') kedalaman++;
            else if (c == ']')
            {
                kedalaman--;
                if (kedalaman == 0) return json.Substring(awal, i - awal + 1);
            }
        }
        return null;
    }

    private void IsiUlangDanAcakDek()
    {
        dekAcak.Clear();
        dekAcak.AddRange(semuaKartu);
        for (int i = dekAcak.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            DataKartuAksiEdupoly sementara = dekAcak[i];
            dekAcak[i] = dekAcak[j];
            dekAcak[j] = sementara;
        }
    }

    public bool ApakahPetakKartu(int waypoint)
    {
        if (waypointKartu == null) return false;
        for (int i = 0; i < waypointKartu.Length; i++)
            if (waypointKartu[i] == waypoint) return true;
        return false;
    }

    /// <summary>Mengambil kartu berikutnya dari dek gabungan dan mengirim datanya melalui event.</summary>
    public DataKartuAksiEdupoly AmbilKartu()
    {
        if (!databaseSiap)
        {
            Debug.LogError("KartuEdupolyManager: Database belum siap.");
            return null;
        }

        if (dekAcak.Count == 0)
            IsiUlangDanAcakDek();

        kartuAktif = dekAcak[0];
        dekAcak.RemoveAt(0);

        Debug.Log("EDUCARD terambil: " + kartuAktif.kategori + " | " + kartuAktif.Kartu + " | " + kartuAktif.Aksi);
        OnKartuDiambil?.Invoke(kartuAktif);
        return kartuAktif;
    }

    // Alias untuk memudahkan pemanggilan dari kode lama; tidak membuka UI.
    public void AmbilDanTampilkanKartu()
    {
        AmbilKartu();
    }

    public bool CobaAmbilSoalWawasanAktif(out DataKartuWawasanEdupoly soal)
    {
        soal = null;
        if (kartuAktif == null || kartuAktif.kategori != "Wawasan Umum") return false;
        return indeksSoalWawasan.TryGetValue(kartuAktif, out soal);
    }
}

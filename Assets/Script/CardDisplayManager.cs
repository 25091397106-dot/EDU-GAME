
using System.Collections.Generic;
using UnityEngine;

public class CardDisplayManager : MonoBehaviour
{
    [Header("Referensi")]
    public Camera kameraGame;
    public GameObject kartu3D;

    [Header("Daftar Kartu Berdasarkan Petak")]
    public List<GameObject> daftarKartu =
        new List<GameObject>();

    [Header("Tampilan Kartu")]
    public float jarakKamera = 4f;
    public float ukuranKartu = 2f;

    [Header("Offset Tampilan")]
    public Vector3 offsetTampilan =
        new Vector3(-0.8f, -0.15f, 0f);

    private Vector3 posisiAsli;
    private Quaternion rotasiAsli;
    private Vector3 skalaAsli;

    private bool transformTersimpan;

    void Start()
    {
        if (kameraGame == null)
            kameraGame = Camera.main;

        if (kartu3D == null)
            return;

        SimpanTransformAsli(kartu3D);
        kartu3D.SetActive(false);
    }

    // Pemanggilan lama:
    // TampilkanKartuSesuaiPetak("Seni Rupa");
    // Otomatis memakai Level 1.
    public bool TampilkanKartuSesuaiPetak(
        string namaPetak)
    {
        return TampilkanKartuSesuaiPetak(
            namaPetak, 1
        );
    }

    // Pemanggilan baru:
    // Level 1 = Pembelian
    // Level 2 = Landmark / Pengambilalihan
    public bool TampilkanKartuSesuaiPetak(
        string namaPetak,
        int levelSoal)
    {
        if (string.IsNullOrWhiteSpace(namaPetak))
            return false;

        if (levelSoal != 1 && levelSoal != 2)
        {
            Debug.LogError(
                "Level soal tidak valid: " +
                levelSoal
            );

            return false;
        }

        foreach (GameObject kartu in daftarKartu)
        {
            if (kartu == null)
                continue;

            KartuSoal dataKartu =
                kartu.GetComponent<KartuSoal>();

            if (dataKartu == null)
                continue;

            if (string.Equals(
                dataKartu.namaPetak.Trim(),
                namaPetak.Trim(),
                System.StringComparison.OrdinalIgnoreCase))
            {
                // Sembunyikan kartu sebelumnya.
                if (kartu3D != null)
                {
                    SembunyikanKartu();
                }

                // Pilih kartu sesuai petak.
                kartu3D = kartu;

                SimpanTransformAsli(kartu3D);

                // Tetapkan level sebelum soal dibuka.
                dataKartu.levelSoal = levelSoal;

                TampilkanKartu();

                dataKartu.TampilkanSoal();

                Debug.Log(
                    "Kartu muncul: " +
                    namaPetak +
                    " | Level " + levelSoal
                );

                return true;
            }
        }

        Debug.LogWarning(
            "Kartu tidak ditemukan untuk petak: " +
            namaPetak
        );

        return false;
    }

    private void SimpanTransformAsli(GameObject kartu)
    {
        if (kartu == null)
            return;

        posisiAsli = kartu.transform.position;
        rotasiAsli = kartu.transform.rotation;
        skalaAsli = kartu.transform.localScale;

        transformTersimpan = true;
    }

    public void TampilkanKartu()
    {
        if (kameraGame == null)
            kameraGame = Camera.main;

        if (kameraGame == null || kartu3D == null)
        {
            Debug.LogWarning(
                "Kamera atau kartu 3D belum tersedia!"
            );

            return;
        }

        Transform cam = kameraGame.transform;

        kartu3D.transform.position =
            cam.position +
            cam.forward * jarakKamera;

        kartu3D.transform.position +=
            cam.right * offsetTampilan.x +
            cam.up * offsetTampilan.y +
            cam.forward * offsetTampilan.z;

        kartu3D.transform.rotation =
            Quaternion.LookRotation(
                -cam.forward,
                cam.up
            );

        kartu3D.transform.Rotate(
            0f, 180f, 0f, Space.Self
        );

        kartu3D.transform.localScale =
            new Vector3(
                ukuranKartu * 1.4f,
                ukuranKartu,
                ukuranKartu
            );

        kartu3D.SetActive(true);
    }

    public void SembunyikanKartu()
    {
        if (kartu3D == null)
            return;
       
        kartu3D.SetActive(false);

        if (transformTersimpan)
        {
            kartu3D.transform.position =
                posisiAsli;

            kartu3D.transform.rotation =
                rotasiAsli;

            kartu3D.transform.localScale =
                skalaAsli;
        }
    }

    [ContextMenu("Tes Tampilkan Kartu")]
    public void TesTampilkanKartu()
    {
        TampilkanKartu();
    }

    [ContextMenu("Tes Sembunyikan Kartu")]
    public void TesSembunyikanKartu()
    {
        SembunyikanKartu();
    }
}

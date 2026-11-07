
using System;
using System.Collections;
using UnityEngine;

public static class PropertyUpgradeFlow
{
    public static IEnumerator Proses(
        PionMovement pion,
        int idPemain,
        bool modeMultiplayer,
        CardDisplayManager cardDisplayManager)
    {
        // Multiplayer akan dihubungkan terpisah.
        if (modeMultiplayer || pion == null)
            yield break;

        PropertyDataManager dataManager =
            UnityEngine.Object.FindAnyObjectByType
                <PropertyDataManager>();

        PlayerMoneyManager uang =
            UnityEngine.Object.FindAnyObjectByType
                <PlayerMoneyManager>();

        if (dataManager == null || uang == null)
            yield break;

        DataLahan lahan =
            dataManager.CariLahan(pion.posisiSekarang);

        // Hanya lahan milik sendiri
        // yang mempunyai 3 bangunan.
        if (lahan == null ||
            lahan.pemilik != idPemain ||
            lahan.tahapProperti != 1)
        {
            yield break;
        }

        int biaya =
            dataManager.HitungBiayaUpgrade(lahan);

        if (uang.saldoPemain == null ||
            idPemain < 0 ||
            idPemain >= uang.saldoPemain.Length)
        {
            yield break;
        }

        if (uang.saldoPemain[idPemain] < biaya)
        {
            Debug.Log(
                "Saldo Player " + (idPemain + 1) +
                " tidak cukup untuk upgrade " +
                lahan.namaLahan
            );

            yield break;
        }

        // =====================================
        // UPGRADE OTOMATIS UNTUK BOT
        // =====================================
        if (idPemain != 0)
        {
            yield return new WaitForSeconds(1f);

            // Simulasi jawaban soal Level 2 bot.
            bool jawabanBenar =
                UnityEngine.Random.value < 0.75f;

            if (!jawabanBenar)
            {
                Debug.Log(
                    "Bot gagal menjawab soal " +
                    "Level 2 di " + lahan.namaLahan
                );

                yield break;
            }

            bool berhasil =
                dataManager.UpgradeLandmark(
                    lahan,
                    idPemain,
                    uang
                );

            Debug.Log(
                berhasil
                ? "Bot berhasil upgrade landmark " +
                  lahan.namaLahan
                : "Bot gagal upgrade landmark."
            );

            yield break;
        }

        // =====================================
        // UPGRADE PEMAIN MANUSIA
        // =====================================
        PropertyPurchaseUI purchaseUI =
            UnityEngine.Object.FindAnyObjectByType
                <PropertyPurchaseUI>();

        SoalManager soalManager =
            UnityEngine.Object.FindAnyObjectByType
                <SoalManager>();

        if (purchaseUI == null ||
            soalManager == null ||
            cardDisplayManager == null)
        {
            Debug.LogWarning(
                "UI upgrade atau sistem soal " +
                "belum lengkap."
            );

            yield break;
        }

        bool sudahMemilih = false;
        bool sedangMenjawab = false;
        bool soalSelesai = false;

        Action aksiUpgrade = () =>
        {
            if (sudahMemilih)
                return;

            bool kartuMuncul =
                cardDisplayManager
                    .TampilkanKartuSesuaiPetak(
                        lahan.namaLahan,
                        2
                    );

            if (!kartuMuncul)
            {
                Debug.LogWarning(
                    "Kartu Level 2 tidak ditemukan."
                );
                return;
            }

            sedangMenjawab = true;
            sudahMemilih = true;

            Debug.Log(
                "Soal Level 2 dimulai: " +
                lahan.namaLahan
            );
        };

        Action aksiLewati = () =>
        {
            if (sudahMemilih)
                return;

            sudahMemilih = true;

            Debug.Log(
                "Upgrade dilewati: " +
                lahan.namaLahan
            );
        };

        Action<bool> hasilJawaban = benar =>
        {
            if (!sedangMenjawab || soalSelesai)
                return;

            soalSelesai = true;

            if (!benar)
            {
                Debug.Log(
                    "Jawaban Level 2 salah. " +
                    "Upgrade dibatalkan."
                );

                return;
            }

            bool berhasil =
                dataManager.UpgradeLandmark(
                    lahan,
                    idPemain,
                    uang
                );

            Debug.Log(
                berhasil
                ? "Upgrade landmark berhasil: " +
                  lahan.namaLahan
                : "Upgrade gagal: saldo atau " +
                  "status lahan tidak sesuai."
            );
        };

        // Daftarkan event selama proses upgrade.
        purchaseUI.saatKlikBeli += aksiUpgrade;
        purchaseUI.saatKlikLewati += aksiLewati;
        soalManager.OnJawabanSelesai += hasilJawaban;

        // Sementara memakai panel pembelian,
        // dengan harga yang diganti biaya upgrade.
        purchaseUI.TampilkanPembelian(
            "UPGRADE " + lahan.namaLahan,
            biaya,
            lahan.warnaKomplek
        );

        Debug.Log(
            "Upgrade tersedia: " +
            lahan.namaLahan +
            " | Biaya Rp" +
            biaya.ToString("N0")
        );

        yield return new WaitUntil(
            () => sudahMemilih
        );

        if (sedangMenjawab)
        {
            yield return new WaitUntil(
                () => soalSelesai
            );

            yield return new WaitForSeconds(2f);
        }

        // Hapus event agar tidak terpicu
        // pada pembelian atau upgrade berikutnya.
        purchaseUI.saatKlikBeli -= aksiUpgrade;
        purchaseUI.saatKlikLewati -= aksiLewati;
        soalManager.OnJawabanSelesai -= hasilJawaban;
    }
}

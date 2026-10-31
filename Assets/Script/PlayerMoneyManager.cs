using UnityEngine;
using TMPro;

public class PlayerMoneyManager : MonoBehaviour
{
    public TurnManager turnManager;
    public GameObject[] panelInfoPemain;
    public int saldoAwal = 1500000;

    public int[] saldoPemain;
    [Header("Tampilan Nama dan Saldo Pemain")]
    public TMP_Text[] textNamaPemain;
    public TMP_Text[] textSaldoPemain;

    void Start()
    {
        // Mengambil jumlah pemain dari Main Menu
        int jumlahPemain = MainMenuManager.jumlahPemain;

        // Menyiapkan saldo untuk setiap pemain
        saldoPemain = new int[jumlahPemain];

        for (int i = 0; i < jumlahPemain; i++)
        {
            saldoPemain[i] = saldoAwal;

            Debug.Log("Pemain " + (i + 1) +
                " memiliki saldo Rp" + saldoPemain[i]);
        }
        // kode lama kamu tetap

        AturTampilanPanelPemain();
        PerbaruiUIPemain();
    }
    void AturTampilanPanelPemain()
    {
        int jumlahTampil = 2;

        if (turnManager != null)
        {
            jumlahTampil = Mathf.Clamp(turnManager.jumlahPemain, 2, 4);
        }

        for (int i = 0; i < panelInfoPemain.Length; i++)
        {
            if (panelInfoPemain[i] != null)
            {
                panelInfoPemain[i].SetActive(i < jumlahTampil);
            }
        }
    }

    void LateUpdate()
    {
        TampilkanPanelMilikSendiri();
    }

    public bool KurangiSaldo(int idPemain, int jumlah)
    {
        if (!PemainValid(idPemain) || jumlah < 0)
            return false;

        if (saldoPemain[idPemain] < jumlah)
        {
            Debug.Log("Saldo Pemain " + (idPemain + 1) +
                " tidak cukup!");
            return false;
        }

        saldoPemain[idPemain] -= jumlah;
        TampilkanSaldo(idPemain);
        return true;
    }

    public void TambahSaldo(int idPemain, int jumlah)
    {
        if (!PemainValid(idPemain) || jumlah < 0)
            return;

        saldoPemain[idPemain] += jumlah;
        TampilkanSaldo(idPemain);
    }

    public bool TransferSaldo(int dariPemain, int kePemain, int jumlah)
    {
        if (!PemainValid(kePemain))
            return false;

        if (!KurangiSaldo(dariPemain, jumlah))
            return false;

        TambahSaldo(kePemain, jumlah);
        return true;
    }

    bool PemainValid(int idPemain)
    {
        return saldoPemain != null &&
               idPemain >= 0 &&
               idPemain < saldoPemain.Length;
    }

    public void TampilkanSaldo(int idPemain)
    {
        if (!PemainValid(idPemain))
            return;

        Debug.Log("Saldo Pemain " + (idPemain + 1) +
            ": Rp" + saldoPemain[idPemain].ToString("N0"));
        PerbaruiUIPemain();
    }

    public void PerbaruiUIPemain()
    {
        if (saldoPemain == null)
            return;

        for (int i = 0; i < saldoPemain.Length; i++)
        {
            // Perbarui nama pemain
            if (textNamaPemain != null &&
                i < textNamaPemain.Length &&
                textNamaPemain[i] != null)
            {
                if (MainMenuManager.modeMultiplayer)
                {
                    textNamaPemain[i].text =
                        "Player " + (i + 1);
                }
                else
                {
                    textNamaPemain[i].text =
                        i == 0
                        ? "Player 1"
                        : "Bot " + (i + 1);
                }
            }

            // Perbarui saldo pada panel
            if (textSaldoPemain != null &&
                i < textSaldoPemain.Length &&
                textSaldoPemain[i] != null)
            {
                textSaldoPemain[i].text =
                    saldoPemain[i].ToString("N0");
            }
        }
    }

    public void TampilkanPanelMilikSendiri()
    {
        int indexSaya = 0;

        if (MainMenuManager.modeMultiplayer)
        {
            NetworkGameManager networkGame =
                FindAnyObjectByType<NetworkGameManager>();

            if (networkGame == null ||
                networkGame.playerIndexLokal < 0)
            {
                // Identitas belum siap, sembunyikan dulu
                foreach (GameObject panel in panelInfoPemain)
                {
                    if (panel != null)
                        panel.SetActive(false);
                }

                return;
            }

            indexSaya = networkGame.playerIndexLokal;
        }

        // Hanya tampilkan panel milik pemain ini
        for (int i = 0; i < panelInfoPemain.Length; i++)
        {
            if (panelInfoPemain[i] != null)
            {
                panelInfoPemain[i].SetActive(i == indexSaya);
            }
        }
    }
    public bool BeliLahan(DataLahan lahan, int idPemain,
                      PlayerMoneyManager uang)
    {
        if (lahan == null || uang == null || lahan.pemilik != -1)
            return false;

        if (!uang.KurangiSaldo(idPemain, lahan.harga))
            return false;

        lahan.pemilik = idPemain;

        Debug.Log("Pemain " + (idPemain + 1) +
                  " berhasil membeli " + lahan.namaLahan);

        return true;
    }
}

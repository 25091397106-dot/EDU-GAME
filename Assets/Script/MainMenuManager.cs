
using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class MainMenuManager : MonoBehaviour
{
    public GameObject panelPilihMode;
    public GameObject panelJumlahPemain;
    public GameObject panelMultiplayer;
    public GameObject panelBuatRoom;
    public GameObject panelGabungRoom;

    public GameObject panelLobby;
    public TMP_Text textKodeRoom;

    public TMP_Text textJumlahPemainLobby;
    public GameObject[] slotPemainLobby;
    public TMP_Text[] statusPemainLobby;
    public GameObject panelPengaturan;

    void Start()
    {
        panelPilihMode.SetActive(false);
        panelJumlahPemain.SetActive(false);
        panelMultiplayer.SetActive(false);
        kapasitasRoom = 2;
        UpdateWarnaTombol();
        panelBuatRoom.SetActive(false);
        panelGabungRoom.SetActive(false);

        panelLobby.SetActive(false);
        panelPengaturan.SetActive(false);

    }

    public void BukaPilihanMode()
    {
        panelPilihMode.SetActive(true);
        panelJumlahPemain.SetActive(false);
    }

    public void TutupPilihanMode()
    {
        panelPilihMode.SetActive(false);
    }

    public void BukaJumlahPemain()
    {
        panelPilihMode.SetActive(false);
        panelJumlahPemain.SetActive(true);
    }

    public void KembaliKePilihanMode()
    {
        panelJumlahPemain.SetActive(false);
        panelPilihMode.SetActive(true);
    }

    public static int jumlahPemain = 2;
    public static bool modeMultiplayer = false;

    public void PilihJumlahPemain(int jumlah)
    {
        // Mode melawan Bot
        modeMultiplayer = false;

        jumlahPemain = Mathf.Clamp(jumlah, 2, 4);

        Debug.Log("MODE: LAWAN BOT");
        Debug.Log("Jumlah Pemain: " + jumlahPemain);
        Debug.Log("Jumlah Bot: " + (jumlahPemain - 1));

        SceneManager.LoadScene("game");
    }

    public void BukaMultiplayer()
    {
        // Mode bermain online bersama teman
        modeMultiplayer = true;

        panelPilihMode.SetActive(false);
        panelMultiplayer.SetActive(true);

        Debug.Log("MODE: MULTIPLAYER");
    }

    public void KembaliDariMultiplayer()
    {
        panelMultiplayer.SetActive(false);
        panelPilihMode.SetActive(true);
    }

    public int kapasitasRoom = 2;
    public Button tombolRoom2;
    public Button tombolRoom3;
    public Button tombolRoom4;


    public void PilihKapasitasRoom(int jumlah)
    {
        kapasitasRoom = jumlah;

        Debug.Log("Kapasitas Room: " + kapasitasRoom);

        UpdateWarnaTombol();
    }

    void UpdateWarnaTombol()
    {
        Color warnaAktif = new Color32(112, 69, 47, 255);
        Color warnaTidakAktif = new Color32(191, 166, 141, 255);

        tombolRoom2.image.color =
            kapasitasRoom == 2 ? warnaAktif : warnaTidakAktif;

        tombolRoom3.image.color =
            kapasitasRoom == 3 ? warnaAktif : warnaTidakAktif;

        tombolRoom4.image.color =
            kapasitasRoom == 4 ? warnaAktif : warnaTidakAktif;
    }

    public void BukaPanelBuatRoom()
    {
        panelMultiplayer.SetActive(false);
        panelBuatRoom.SetActive(true);
    }

    public void KembaliDariBuatRoom()
    {
        panelBuatRoom.SetActive(false);
        panelMultiplayer.SetActive(true);
    }

    public MultiplayerManager multiplayerManager;
    public TMP_InputField inputKodeRoom;

    public void KonfirmasiBuatRoom()
    {
        multiplayerManager.BuatRoom(kapasitasRoom);
    }

    public void BukaPanelGabungRoom()
    {
        panelMultiplayer.SetActive(false);
        panelGabungRoom.SetActive(true);
    }

    public void KembaliDariGabungRoom()
    {
        panelGabungRoom.SetActive(false);
        panelMultiplayer.SetActive(true);
    }

    public void KonfirmasiGabungRoom()
    {
        string kode = inputKodeRoom.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(kode))
        {
            Debug.LogWarning("Masukkan kode room terlebih dahulu!");
            return;
        }

        multiplayerManager.GabungRoom(kode);
    }

    public void BukaLobby()
    {
        panelBuatRoom.SetActive(false);
        panelGabungRoom.SetActive(false);
        panelMultiplayer.SetActive(false);

        panelLobby.SetActive(true);
    }
    public void TampilkanKodeRoom(string kode)
    {
        textKodeRoom.text = "Kode Room: " + kode;
    }

    public void TampilkanJumlahPemain(int jumlah, int kapasitas)
    {
        textJumlahPemainLobby.text =
            "Pemain: " + jumlah + "/" + kapasitas;
    }
    public void UpdateSlotPemain(int jumlah, int kapasitas)
    {
        for (int i = 0; i < slotPemainLobby.Length; i++)
        {
            bool slotAktif = i < kapasitas;
            slotPemainLobby[i].SetActive(slotAktif);

            if (!slotAktif)
                continue;

            if (i == 0)
            {
                statusPemainLobby[i].text = "HOST";
            }
            else if (i < jumlah)
            {
                statusPemainLobby[i].text = "Terhubung";
            }
            else
            {
                statusPemainLobby[i].text = "Menunggu...";
            }
        }
        TampilkanJumlahPemain(jumlah, kapasitas);
    }

    public void KembaliDariLobby()
    {
        panelLobby.SetActive(false);
        panelBuatRoom.SetActive(false);
        panelGabungRoom.SetActive(false);

        panelMultiplayer.SetActive(true);
    }
    public void BukaPengaturan()
    {
        panelPengaturan.SetActive(true);
    }

    public void TutupPengaturan()
    {
        panelPengaturan.SetActive(false);
    }

    public void KeluarGame()
    {
        Debug.Log("Keluar dari Edupoly...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}
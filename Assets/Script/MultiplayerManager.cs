
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MultiplayerManager : MonoBehaviour
{
    public MainMenuManager mainMenuManager;
    private ISession sesiAktif;
    async void Start()
    {
        await SiapkanLayananOnline();
    }

    async Task SiapkanLayananOnline()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Debug.Log("Edupoly berhasil terhubung ke layanan Unity!");
        }
        catch (System.Exception error)
        {
            Debug.LogError("Koneksi Unity gagal: " + error.Message);
        }
    }


    public async void BuatRoom(int jumlahPemain)
    {
        try
        {
            var options = new SessionOptions
            {
                MaxPlayers = jumlahPemain
            }.WithRelayNetwork();

            var session = await MultiplayerService.Instance.CreateSessionAsync(options);

            sesiAktif = session;
            MainMenuManager.modeMultiplayer = true;
            MainMenuManager.jumlahPemain = session.MaxPlayers;
            PasangEventPemain();

            Debug.Log("Room berhasil dibuat!");

            // Tampilkan kode room asli
            mainMenuManager.TampilkanKodeRoom(session.Code);

            // Buka lobby terlebih dahulu
            mainMenuManager.BukaLobby();

            // Perbarui slot pemain
            mainMenuManager.UpdateSlotPemain(
                sesiAktif.PlayerCount,
                sesiAktif.MaxPlayers
            );

            Debug.Log("Lobby berhasil diperbarui!");
            Debug.Log("Kode Room: " + session.Code);
        }
        catch (System.Exception error)
        {
            Debug.LogError("Gagal membuat room: " + error);
        }
    }


    public async void GabungRoom(string kodeRoom)
    {
        try
        {
            kodeRoom = kodeRoom.Trim().ToUpper();

            if (string.IsNullOrEmpty(kodeRoom))
            {
                Debug.LogWarning("Masukkan kode room terlebih dahulu!");
                return;
            }

            var session = await MultiplayerService.Instance.JoinSessionByCodeAsync(kodeRoom);

            sesiAktif = session;
            MainMenuManager.modeMultiplayer = true;
            MainMenuManager.jumlahPemain = session.MaxPlayers;
            PasangEventPemain();

            // Tampilkan kode room
            mainMenuManager.TampilkanKodeRoom(session.Code);

            // Buka lobby terlebih dahulu
            mainMenuManager.BukaLobby();

            // Perbarui slot pemain
            mainMenuManager.UpdateSlotPemain(
                sesiAktif.PlayerCount,
                sesiAktif.MaxPlayers
            );

            Debug.Log("Berhasil bergabung ke room!");
        }
        catch (System.Exception error)
        {
            Debug.LogError("Gagal bergabung ke room: " + error);
        }
    }

    void PasangEventPemain()
    {
        if (sesiAktif == null)
            return;

        sesiAktif.PlayerJoined += SaatPemainMasuk;
        sesiAktif.PlayerHasLeft += SaatPemainKeluar;
    }

    void SaatPemainMasuk(string playerId)
    {
        Debug.Log("Pemain bergabung: " + playerId);
        PerbaruiLobby();
    }

    void SaatPemainKeluar(string playerId)
    {
        Debug.Log("Pemain keluar: " + playerId);
        PerbaruiLobby();
    }

    void PerbaruiLobby()
    {
        if (sesiAktif == null)
            return;
        mainMenuManager.UpdateSlotPemain(
            sesiAktif.PlayerCount,
            sesiAktif.MaxPlayers
        );

        Debug.Log("Jumlah pemain sekarang: " +
                  sesiAktif.PlayerCount);
    }

    void OnDestroy()
    {
        if (sesiAktif == null)
            return;

        sesiAktif.PlayerJoined -= SaatPemainMasuk;
        sesiAktif.PlayerHasLeft -= SaatPemainKeluar;
    }

    public void MulaiPermainan()
    {
        if (sesiAktif == null)
        {
            Debug.LogWarning("Belum masuk room!");
            return;
        }

        if (sesiAktif.PlayerCount < sesiAktif.MaxPlayers)
        {
            Debug.LogWarning("Menunggu pemain lain!");
            return;
        }

        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning(
                "Netcode belum berjalan. Periksa koneksi Relay!"
            );
            return;
        }

        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("Hanya Host yang bisa memulai!");
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(
            "game",
            LoadSceneMode.Single
        );
    }

    public async void KeluarRoom()
    {
        if (sesiAktif == null)
        {
            Debug.LogWarning("Tidak ada room aktif!");
            return;
        }

        try
        {
            // Simpan sesi yang akan ditinggalkan
            var session = sesiAktif;

            // Host menutup room, pemain biasa keluar
            if (session.IsHost)
            {
                await session.AsHost().DeleteAsync();
                Debug.Log("Host menutup room.");
            }
            else
            {
                await session.LeaveAsync();
                Debug.Log("Pemain keluar dari room.");
            }

            // Hapus referensi sesi aktif
            sesiAktif = null;

            // Kembali ke menu Multiplayer
            mainMenuManager.KembaliDariLobby();
        }
        catch (System.Exception error)
        {
            Debug.LogError("Gagal keluar room: " + error);
        }
    }
}

using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MultiplayerDisconnectHandler : MonoBehaviour
{
    private NetworkManager network;
    private bool sedangKeluar = false;

    private void Start()
    {
        network = NetworkManager.Singleton;

        if (network != null)
        {
            network.OnClientDisconnectCallback += SaatTerputus;
        }
    }

    private void OnDestroy()
    {
        if (network != null)
        {
            network.OnClientDisconnectCallback -= SaatTerputus;
        }
    }

    private async void SaatTerputus(ulong clientId)
    {
        if (sedangKeluar || network == null)
            return;

        // Host mengatur penutupan room sendiri.
        if (network.IsServer)
            return;

        // Hanya tangani disconnect milik Client lokal.
        if (clientId != network.LocalClientId)
            return;

        sedangKeluar = true;

        Debug.LogWarning(
            "EDU-POLY: Koneksi Host terputus. Kembali ke MainMenu."
        );

        try
        {
            var sesi = MultiplayerManager.sesiAktif;

            if (sesi != null)
            {
                await sesi.LeaveAsync();
            }
        }
        catch (System.Exception error)
        {
            Debug.LogWarning(
                "EDU-POLY: Sesi sudah ditutup atau gagal ditinggalkan: " +
                error.Message
            );
        }
        finally
        {
            MultiplayerManager.sesiAktif = null;

            if (network != null && network.IsListening)
            {
                network.Shutdown();
            }

            MainMenuManager.modeMultiplayer = false;

            SceneManager.LoadScene("MainMenu");
        }
    }
}

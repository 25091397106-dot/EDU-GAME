
using UnityEngine;

public class BotManager : MonoBehaviour
{
    public int jumlahPemain;
    public int jumlahBot;

    void Start()
    {
        // Jumlah pemain dari pilihan Main Menu
        jumlahPemain = MainMenuManager.jumlahPemain;

        // Player 1 manusia, sisanya bot
        jumlahBot = jumlahPemain - 1;

        Debug.Log("MODE LAWAN BOT");
        Debug.Log("Total pemain: " + jumlahPemain);
        Debug.Log("Total bot: " + jumlahBot);
    }
}
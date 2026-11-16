using UnityEngine;

public class EducardPopup : MonoBehaviour
{
    public KartuEdupolyManager kartuManager;

    private void OnEnable()
    {
        if (kartuManager != null)
            kartuManager.OnKartuDiambil += TampilkanKartu;
    }

    private void OnDisable()
    {
        if (kartuManager != null)
            kartuManager.OnKartuDiambil -= TampilkanKartu;
    }

    private void TampilkanKartu(DataKartuAksiEdupoly kartu)
    {
        if (kartu == null) return;

        if (EducardPopupUI.Instance == null)
        {
            Debug.LogError("EducardPopupUI belum terpasang!");
            return;
        }

        string isi = kartu.Kartu + "\n\n" + kartu.Aksi;
        EducardPopupUI.Instance.TampilkanKartu(isi, 5f);
    }
}

using UnityEngine;

public class KartuEdupolyDisplay : MonoBehaviour
{
    [Header("Referensi")]
    public Camera kameraGame;
    public GameObject kartuEdupoly;

    [Header("Tampilan Kartu Edupoly")]
    public float jarakKamera = 4f;
    public float ukuranKartu = 1.3f;
    public Vector3 offsetTampilan = Vector3.zero;

    private bool sedangTampil = false;

    private Vector3 posisiAsli;
    private Quaternion rotasiAsli;
    private Vector3 skalaAsli;
    private bool sudahDisimpan = false;

    public void Tampilkan()
    {
        if (kartuEdupoly == null)
        {
            Debug.LogError("Kartu Edupoly belum dihubungkan!");
            return;
        }

        if (kameraGame == null)
            kameraGame = Camera.main;

        if (kameraGame == null)
        {
            Debug.LogError("Kamera Game belum dihubungkan!");
            return;
        }

        if (!sudahDisimpan)
        {
            posisiAsli = kartuEdupoly.transform.position;
            rotasiAsli = kartuEdupoly.transform.rotation;
            skalaAsli = kartuEdupoly.transform.localScale;
            sudahDisimpan = true;
        }

        sedangTampil = true;
        kartuEdupoly.SetActive(true);
        AturPosisi();
    }

    void LateUpdate()
    {
        if (sedangTampil)
            AturPosisi();
    }

    private void AturPosisi()
    {
        if (kameraGame == null || kartuEdupoly == null)
            return;

        Transform cam = kameraGame.transform;
        Transform kartu = kartuEdupoly.transform;

        kartu.position =
            cam.position +
            cam.forward * jarakKamera +
            cam.right * offsetTampilan.x +
            cam.up * offsetTampilan.y +
            cam.forward * offsetTampilan.z;

        kartu.rotation =
            Quaternion.LookRotation(-cam.forward, cam.up);

        kartu.Rotate(0f, 180f, 0f, Space.Self);

        kartu.localScale = new Vector3(
            ukuranKartu * 1.4f,
            ukuranKartu,
            ukuranKartu
        );
    }

    public void Sembunyikan()
    {
        sedangTampil = false;

        if (kartuEdupoly == null)
            return;

        kartuEdupoly.SetActive(false);

        if (sudahDisimpan)
        {
            kartuEdupoly.transform.position = posisiAsli;
            kartuEdupoly.transform.rotation = rotasiAsli;
            kartuEdupoly.transform.localScale = skalaAsli;
        }
    }
    [ContextMenu("TES TAMPILKAN KARTU EDUPOLY")]
    public void TesTampilkan()
    {
        Tampilkan();
    }
}
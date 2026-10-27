
using System.Collections;
using UnityEngine;

public class CardFlip : MonoBehaviour
{
    public float durasiPutar = 0.6f;

    private bool sedangBerputar = false;
    private bool sisiBelakang = false;

    void Update()
    {
        // Tekan F untuk mencoba membalik kartu
        if (Input.GetKeyDown(KeyCode.F))
        {
            BalikKartu();
        }
    }

    public void BalikKartu()
    {
        if (!sedangBerputar)
        {
            StartCoroutine(AnimasiBalik());
        }
    }

    IEnumerator AnimasiBalik()
    {
        sedangBerputar = true;

        Quaternion rotasiAwal = transform.localRotation;

        Quaternion rotasiTujuan = rotasiAwal *
            Quaternion.Euler(0f, 180f, 0f);

        float waktu = 0f;

        while (waktu < durasiPutar)
        {
            waktu += Time.deltaTime;

            float t = Mathf.Clamp01(waktu / durasiPutar);

            t = Mathf.SmoothStep(0f, 1f, t);

            transform.localRotation = Quaternion.Slerp(
                rotasiAwal,
                rotasiTujuan,
                t
            );

            yield return null;
        }

        transform.localRotation = rotasiTujuan;

        sisiBelakang = !sisiBelakang;
        sedangBerputar = false;
    }
}
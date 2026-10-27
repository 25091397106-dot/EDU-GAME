
using System.Collections;
using UnityEngine;

public class DiceMixAnimation : MonoBehaviour
{
    public Transform dadu1;
    public Transform dadu2;

    public float durasi = 2f;
    public float jarakCampur = 0.35f;
    public float kecepatan = 900f;

    private bool sedangMix = false;

    public bool SedangMix => sedangMix;

    public IEnumerator KocokDadu()
    {
        if (sedangMix)
            yield break;

        sedangMix = true;

        Vector3 posisi1 = dadu1.position;
        Vector3 posisi2 = dadu2.position;

        Vector3 tengah = (posisi1 + posisi2) / 2f;

        float waktu = 0f;

        while (waktu < durasi)
        {
            waktu += Time.deltaTime;

            float sudut = waktu * 12f;

            Vector3 offset = new Vector3(
                Mathf.Cos(sudut) * jarakCampur,
                0f,
                Mathf.Sin(sudut) * jarakCampur
            );

            dadu1.position = tengah + offset;
            dadu2.position = tengah - offset;

            dadu1.Rotate(
                Vector3.one,
                kecepatan * Time.deltaTime,
                Space.World
            );

            dadu2.Rotate(
                new Vector3(1, -1, 1),
                kecepatan * Time.deltaTime,
                Space.World
            );

            yield return null;
        }

        // Kembalikan posisi setelah dikocok
        dadu1.position = posisi1;
        dadu2.position = posisi2;

        // Tentukan orientasi akhir
        dadu1.rotation = Quaternion.Euler(
            Random.Range(0, 4) * 90,
            Random.Range(0, 4) * 90,
            Random.Range(0, 4) * 90
        );

        dadu2.rotation = Quaternion.Euler(
            Random.Range(0, 4) * 90,
            Random.Range(0, 4) * 90,
            Random.Range(0, 4) * 90
        );

        sedangMix = false;
    }
}

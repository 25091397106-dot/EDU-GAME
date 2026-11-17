
using System.Collections;
using UnityEngine;

public class AnimasiBangunan : MonoBehaviour
{
    public float durasi = 0.65f;
    public float tinggiMuncul = 0.25f;
    public float sudutPutar = 25f;

    private void Start()
    {
        StartCoroutine(Muncul());
    }

    private IEnumerator Muncul()
    {
        Vector3 skalaAkhir = transform.localScale;
        Vector3 posisiAkhir = transform.localPosition;
        Quaternion rotasiAkhir = transform.localRotation;

        Vector3 posisiAwal = posisiAkhir - Vector3.up * tinggiMuncul;

        transform.localScale = skalaAkhir * 0.01f;
        transform.localPosition = posisiAwal;
        transform.localRotation =
            rotasiAkhir * Quaternion.Euler(0f, -sudutPutar, 0f);

        float waktu = 0f;

        while (waktu < durasi)
        {
            waktu += Time.deltaTime;

            float t = Mathf.Clamp01(waktu / durasi);
            float halus = 1f - Mathf.Pow(1f - t, 3f);

            float pantulan = 1f +
                Mathf.Sin(t * Mathf.PI) * 0.12f;

            transform.localScale =
                skalaAkhir * halus * pantulan;

            transform.localPosition =
                Vector3.Lerp(posisiAwal, posisiAkhir, halus);

            transform.localRotation =
                Quaternion.Slerp(
                    rotasiAkhir * Quaternion.Euler(0f, -sudutPutar, 0f),
                    rotasiAkhir,
                    halus
                );

            yield return null;
        }

        transform.localScale = skalaAkhir;
        transform.localPosition = posisiAkhir;
        transform.localRotation = rotasiAkhir;
    }
}

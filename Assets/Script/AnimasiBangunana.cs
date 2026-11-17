
using System.Collections;
using UnityEngine;

public class AnimasiBangunan : MonoBehaviour
{
    public float durasi = 1.5f;
    public float tinggiMuncul = 1.2f;

    private void Start()
    {
        StartCoroutine(Muncul());
    }

    private IEnumerator Muncul()
    {
        Vector3 posisiAkhir = transform.localPosition;
        Vector3 skalaAkhir = transform.localScale;

        Vector3 posisiAwal =
            posisiAkhir - Vector3.up * tinggiMuncul;

        transform.localPosition = posisiAwal;
        transform.localScale = Vector3.one * 0.01f;

        float waktu = 0f;

        while (waktu < durasi)
        {
            waktu += Time.deltaTime;

            float t = Mathf.Clamp01(waktu / durasi);
            float halus = t * t * (3f - 2f * t);

            transform.localPosition =
                Vector3.Lerp(posisiAwal, posisiAkhir, halus);

            transform.localScale =
                Vector3.Lerp(
                    Vector3.one * 0.01f,
                    skalaAkhir,
                    halus
                );

            yield return null;
        }

        transform.localPosition = posisiAkhir;
        transform.localScale = skalaAkhir;
    }
}

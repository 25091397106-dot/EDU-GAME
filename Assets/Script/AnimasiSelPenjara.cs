
using System.Collections;
using UnityEngine;

public class AnimasiSelPenjara : MonoBehaviour
{
    public GameObject selPenjara;

    public IEnumerator TampilkanSel(Transform pion)
    {
        if (selPenjara == null || pion == null)
            yield break;

        // Letakkan sel mengelilingi pion
        selPenjara.transform.position = pion.position;
        selPenjara.transform.rotation = Quaternion.identity;

        // Munculkan sel dari atas
        selPenjara.SetActive(true);

        Vector3 posisiAkhir = selPenjara.transform.position;
        Vector3 posisiMulai = posisiAkhir + Vector3.up * 3f;

        float durasi = 0.7f;
        float waktu = 0f;

        while (waktu < durasi)
        {
            waktu += Time.deltaTime;

            float t = Mathf.SmoothStep(
                0f, 1f,
                Mathf.Clamp01(waktu / durasi)
            );

            selPenjara.transform.position =
                Vector3.Lerp(posisiMulai, posisiAkhir, t);

            yield return null;
        }

        selPenjara.transform.position = posisiAkhir;

        // Tahan sel agar pion terlihat di balik jeruji
        yield return new WaitForSeconds(2f);

        // Sembunyikan sel
        selPenjara.SetActive(false);
    }
}
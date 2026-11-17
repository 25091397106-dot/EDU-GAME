using System.Collections;
using UnityEngine;

public class AnimasiPeringatanPenjara : MonoBehaviour
{
    public GameObject panelPeringatan;

    public IEnumerator TampilkanPeringatan()
    {
        if (panelPeringatan == null)
            yield break;

        panelPeringatan.SetActive(true);

        Transform panel = panelPeringatan.transform;
        panel.localScale = Vector3.zero;

        float durasi = 0.5f;
        float waktu = 0f;

        while (waktu < durasi)
        {
            waktu += Time.deltaTime;

            float t = Mathf.SmoothStep(
                0f, 1f,
                Mathf.Clamp01(waktu / durasi)
            );

            panel.localScale = Vector3.one * t;
            yield return null;
        }

        panel.localScale = Vector3.one;

        yield return new WaitForSeconds(2f);

        panelPeringatan.SetActive(false);
    }
}


using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform targetPion;

    public Vector3 offset = new Vector3(0, 7, -6);
    public float kecepatanKamera = 4f;
    public bool sedangMengikuti = false;
    public Vector3 pusatPapan = Vector3.zero;
    public float jarakKamera = 14f;
    public float tinggiKamera = 12f;
    private Vector3 posisiAwal;
    private Quaternion rotasiAwal;
    private bool sedangSinematik = false;
    private Coroutine prosesSinematik;

    private void Start()
    {
        posisiAwal = transform.position;
        rotasiAwal = transform.rotation;
    }

    private void LateUpdate()
    {
        if (sedangSinematik)
            return;
        Vector3 posisiTujuan;

        if (sedangMengikuti && targetPion != null)
        {
            Vector3 arah = targetPion.position - pusatPapan;
            arah.y = 0f;
            arah.Normalize();

            posisiTujuan = pusatPapan
                + arah * jarakKamera
                + Vector3.up * tinggiKamera;
        }
        else
        {
            posisiTujuan = posisiAwal;
        }

        transform.position = Vector3.Lerp(
            transform.position,
            posisiTujuan,
            Mathf.Clamp01(kecepatanKamera * Time.deltaTime)
        );

        Quaternion rotasiTujuan = rotasiAwal;

        if (sedangMengikuti && targetPion != null)
        {
            rotasiTujuan = Quaternion.LookRotation(
                pusatPapan - transform.position,
                Vector3.up
            );
        }

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            rotasiTujuan,
            Mathf.Clamp01(kecepatanKamera * Time.deltaTime)
        );
    }

    public void IkutiPion(Transform pion)
    {
        targetPion = pion;
        sedangMengikuti = pion != null;
    }

    public void BerhentiMengikuti()
    {
        sedangMengikuti = false;
        targetPion = null;
    }

    public void FokusBangunan(Transform bangunan)
    {
        if (bangunan == null)
            return;

        Debug.Log("KAMERA ZOOM DIPANGGIL: " + bangunan.name);

        if (prosesSinematik != null)
            StopCoroutine(prosesSinematik);

        prosesSinematik = StartCoroutine(
            AnimasiFokusBangunan(bangunan)
        );
    }

    private System.Collections.IEnumerator AnimasiFokusBangunan(
        Transform bangunan)
    {
        sedangSinematik = true;

        Vector3 posisiMulai = transform.position;
        Quaternion rotasiMulai = transform.rotation;

        Vector3 arah = posisiMulai - bangunan.position;
        arah.y = 0f;

        if (arah.sqrMagnitude < 0.001f)
            arah = Vector3.back;

        arah.Normalize();

        Vector3 posisiZoom =
            bangunan.position + arah * 2.5f + Vector3.up * 2f;

        Quaternion rotasiZoom = Quaternion.LookRotation(
            bangunan.position + Vector3.up * 0.4f - posisiZoom
        );

        float waktu = 0f;
        float durasiZoom = 0.8f;

        while (waktu < durasiZoom)
        {
            waktu += Time.deltaTime;
            float t = Mathf.SmoothStep(
                0f, 1f, Mathf.Clamp01(waktu / durasiZoom)
            );

            transform.position =
                Vector3.Lerp(posisiMulai, posisiZoom, t);

            transform.rotation =
                Quaternion.Slerp(rotasiMulai, rotasiZoom, t);

            yield return null;
        }

        yield return new WaitForSeconds(1.8f);

        posisiMulai = transform.position;
        rotasiMulai = transform.rotation;

        // Kembali ke posisi kamera sebelum zoom.
        waktu = 0f;

        while (waktu < durasiZoom)
        {
            waktu += Time.deltaTime;
            float t = Mathf.SmoothStep(
                0f, 1f, Mathf.Clamp01(waktu / durasiZoom)
            );

            transform.position =
                Vector3.Lerp(posisiMulai, posisiAwal, t);

            transform.rotation =
                Quaternion.Slerp(rotasiMulai, rotasiAwal, t);

            yield return null;
        }

        sedangSinematik = false;
        prosesSinematik = null;
    }

}

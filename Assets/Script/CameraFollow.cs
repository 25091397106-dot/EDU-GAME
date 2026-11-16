
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

    private void Start()
    {
        posisiAwal = transform.position;
        rotasiAwal = transform.rotation;
    }

    private void LateUpdate()
    {
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
}


using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform targetPion;

    public Vector3 offset = new Vector3(0, 7, -6);
    public float kecepatanKamera = 4f;
    public bool sedangMengikuti = false;

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
            posisiTujuan = targetPion.position + offset;
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

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            rotasiAwal,
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


using System.Collections;
using UnityEngine;

public class DiceRoller : MonoBehaviour
{
    private Rigidbody rb;
    private Vector3 posisiAwal;
    private bool sedangBerputar = false;

    public float durasiPutaran = 2f;
    public float kecepatanPutaran = 900f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        posisiAwal = transform.position;

        // Dadu tidak dipengaruhi gravitasi atau benturan
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    public void RollDice()
    {
        if (!sedangBerputar)
        {
            StartCoroutine(PutarDadu());
        }
    }

    IEnumerator PutarDadu()
    {
        sedangBerputar = true;

        float waktu = 0f;

        // Acak orientasi akhir: kelipatan 90 derajat
        Quaternion rotasiAkhir = Quaternion.Euler(
            Random.Range(0, 4) * 90f,
            Random.Range(0, 4) * 90f,
            Random.Range(0, 4) * 90f
        );

        while (waktu < durasiPutaran)
        {
            waktu += Time.deltaTime;

            transform.Rotate(
                Vector3.right,
                kecepatanPutaran * Time.deltaTime,
                Space.World
            );

            transform.Rotate(
                Vector3.up,
                kecepatanPutaran * Time.deltaTime,
                Space.World
            );

            // Posisi dadu selalu sama
            transform.position = posisiAwal;

            yield return null;
        }

        transform.rotation = rotasiAkhir;
        transform.position = posisiAwal;

        sedangBerputar = false;
    }

    public bool IsStopped()
    {
        return !sedangBerputar;
    }
}


using UnityEngine;

public class DiceResult : MonoBehaviour
{
    [Header("Nama Dadu")]
    public string diceName;

    [Header("Sisi Dadu")]
    public Transform face1;
    public Transform face2;
    public Transform face3;
    public Transform face4;
    public Transform face5;
    public Transform face6;

    // Membaca angka pada sisi atas dadu
    public int GetResult()
    {
        float highest = -999f;
        int result = 0;

        Transform[] faces =
        {
            face1, face2, face3,
            face4, face5, face6
        };

        for (int i = 0; i < faces.Length; i++)
        {
            if (faces[i] == null)
                continue;

            Vector3 direction =
                (faces[i].position - transform.position).normalized;

            float dot = Vector3.Dot(
                direction,
                Vector3.up
            );

            if (dot > highest)
            {
                highest = dot;
                result = i + 1;
            }
        }

        return result;
    }

    // Mengatur agar angka tertentu menghadap ke atas
    public void SetResult(int angka)
    {
        if (angka < 1 || angka > 6)
        {
            Debug.LogError("Angka dadu harus 1 sampai 6!");
            return;
        }

        Transform[] faces =
        {
            face1, face2, face3,
            face4, face5, face6
        };

        Transform targetFace = faces[angka - 1];

        if (targetFace == null)
        {
            Debug.LogError(
                "Face " + angka + " belum dihubungkan!"
            );
            return;
        }

        // Cari arah sisi angka yang diinginkan
        Vector3 arahFace =
            (targetFace.position - transform.position).normalized;

        // Putar dadu agar sisi tersebut berada di atas
        Quaternion rotasiPenyesuaian =
            Quaternion.FromToRotation(
                arahFace,
                Vector3.up
            );

        transform.rotation =
            rotasiPenyesuaian * transform.rotation;

        Debug.Log(
            diceName + " diatur menjadi " + angka
        );
    }

    void Update()
    {
        // Tombol R untuk memeriksa hasil dadu
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log(
                diceName + ": " + GetResult()
            );
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual properti otomatis untuk papan Edupoly.
/// Tahap 1: tiga rumah berjajar mengikuti sisi petak.
/// Tahap 2: satu landmark menggantikan tiga rumah.
/// Arah depan rumah/landmark selalu menghadap ke tengah papan.
/// </summary>
public class PropertyBuildingVisual : MonoBehaviour
{
    [Header("Referensi")]
    public PropertyDataManager dataManager;
    public Transform wadahWaypoints;

    [Header("Ukuran Visual")]
    [Min(0.05f)] public float ukuranBangunan = 0.40f;
    [Min(0.05f)] public float ukuranLandmark = 0.45f;
    [Min(0f)] public float jarakAntarBangunan = 0.23f;
    [Min(0f)] public float tinggiDariPetak = 0.05f;
    [Min(0f)] public float jarakDariTepi = 0.28f;

    private readonly Dictionary<int, GameObject> visualAktif = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, int> tahapTerakhir = new Dictionary<int, int>();
    private readonly Dictionary<int, int> pemilikTerakhir = new Dictionary<int, int>();

    private IEnumerator Start()
    {
        for (int i = 0; i < 10; i++)
        {
            if (dataManager == null)
                dataManager = FindAnyObjectByType<PropertyDataManager>();

            if (wadahWaypoints == null)
            {
                PionMovement pion = FindAnyObjectByType<PionMovement>();
                if (pion != null)
                    wadahWaypoints = pion.wadahWaypoints;
            }

            if (dataManager != null && wadahWaypoints != null)
                break;

            yield return null;
        }

        if (dataManager == null || wadahWaypoints == null)
        {
            Debug.LogError("PropertyBuildingVisual: DataManager atau WadahWaypoints belum ditemukan.");
            yield break;
        }

        PerbaruiSemuaBangunan();
    }

    private void Update()
    {
        if (dataManager == null || wadahWaypoints == null || dataManager.daftarLahan == null)
            return;

        foreach (DataLahan lahan in dataManager.daftarLahan)
        {
            if (lahan == null || lahan.waypoint < 0 || lahan.waypoint >= wadahWaypoints.childCount)
                continue;

            bool belumAdaCache = !tahapTerakhir.ContainsKey(lahan.waypoint);
            bool tahapBerubah = belumAdaCache || tahapTerakhir[lahan.waypoint] != lahan.tahapProperti;
            bool pemilikBerubah = belumAdaCache || pemilikTerakhir[lahan.waypoint] != lahan.pemilik;

            if (tahapBerubah || pemilikBerubah)
                TampilkanBangunan(lahan);
        }
    }

    public void PerbaruiSemuaBangunan()
    {
        if (dataManager == null || wadahWaypoints == null || dataManager.daftarLahan == null)
            return;

        foreach (DataLahan lahan in dataManager.daftarLahan)
        {
            if (lahan != null)
                TampilkanBangunan(lahan);
        }
    }

    private void TampilkanBangunan(DataLahan lahan)
    {
        int wp = lahan.waypoint;
        if (wp < 0 || wp >= wadahWaypoints.childCount)
            return;

        if (visualAktif.TryGetValue(wp, out GameObject lama) && lama != null)
            Destroy(lama);
        visualAktif.Remove(wp);

        tahapTerakhir[wp] = lahan.tahapProperti;
        pemilikTerakhir[wp] = lahan.pemilik;

        if (lahan.pemilik < 0 || lahan.tahapProperti <= 0)
            return;

        Transform petak = wadahWaypoints.GetChild(wp);
        Vector3 arahKeTengah = HitungArahKeTengah(petak);
        Vector3 posisi = petak.position + arahKeTengah * jarakDariTepi + Vector3.up * tinggiDariPetak;

        GameObject root = new GameObject("Properti_WP_" + wp + "_" + lahan.namaLahan);
        root.transform.SetParent(transform, true);
        root.transform.position = posisi;
        // Sumbu depan lokal (+Z) menghadap ke tengah papan. Dengan demikian
        // sisi atas/bawah/kiri/kanan otomatis mendapat rotasi berbeda.
        root.transform.rotation = Quaternion.LookRotation(arahKeTengah, Vector3.up);
        root.transform.localScale = Vector3.one;

        Color warna = WarnaPemilik(lahan.pemilik);
        if (lahan.tahapProperti == 1)
            BuatTigaBangunan(root.transform, warna);
        else
            BuatLandmark(root.transform, warna);

        visualAktif[wp] = root;
        Debug.Log("PropertyBuildingVisual: visual tahap " + lahan.tahapProperti + " dibuat di " + lahan.namaLahan + " (WP " + wp + "), menghadap tengah papan.");
    }

private Vector3 HitungArahKeTengah(Transform petak)
{
    if (wadahWaypoints == null ||
        wadahWaypoints.childCount == 0)
    {
        return Vector3.forward;
    }

    float minX = float.MaxValue;
    float maxX = float.MinValue;
    float minZ = float.MaxValue;
    float maxZ = float.MinValue;

    // Cari batas papan berdasarkan posisi waypoint lokal.
    for (int i = 0; i < wadahWaypoints.childCount; i++)
    {
        Vector3 pos = wadahWaypoints.InverseTransformPoint(
            wadahWaypoints.GetChild(i).position
        );

        minX = Mathf.Min(minX, pos.x);
        maxX = Mathf.Max(maxX, pos.x);
        minZ = Mathf.Min(minZ, pos.z);
        maxZ = Mathf.Max(maxZ, pos.z);
    }

    Vector3 posisiPetak =
        wadahWaypoints.InverseTransformPoint(petak.position);

    // Jarak petak terhadap keempat sisi papan.
    float jarakKiri = Mathf.Abs(posisiPetak.x - minX);
    float jarakKanan = Mathf.Abs(maxX - posisiPetak.x);
    float jarakBawah = Mathf.Abs(posisiPetak.z - minZ);
    float jarakAtas = Mathf.Abs(maxZ - posisiPetak.z);

    Vector3 arahLokal;

    // Tentukan arah tegak lurus menuju bagian dalam papan.
    float jarakTerdekat = Mathf.Min(
        jarakKiri,
        jarakKanan,
        jarakBawah,
        jarakAtas
    );

    if (jarakTerdekat == jarakKiri)
        arahLokal = Vector3.right;
    else if (jarakTerdekat == jarakKanan)
        arahLokal = Vector3.left;
    else if (jarakTerdekat == jarakBawah)
        arahLokal = Vector3.forward;
    else
        arahLokal = Vector3.back;

    Vector3 arahDunia =
        wadahWaypoints.TransformDirection(arahLokal);

    arahDunia.y = 0f;

    return arahDunia.normalized;
}

    private void BuatTigaBangunan(Transform parent, Color warna)
    {
        for (int i = 0; i < 3; i++)
        {
            GameObject rumah = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rumah.name = "Bangunan_" + (i + 1);
            rumah.transform.SetParent(parent, false);
            rumah.transform.localPosition = new Vector3((i - 1) * jarakAntarBangunan, ukuranBangunan * 0.45f, 0f);
            rumah.transform.localScale = new Vector3(ukuranBangunan * 0.72f, ukuranBangunan * 0.9f, ukuranBangunan * 0.72f);
            AturWarnaDanCollider(rumah, warna);

            GameObject atap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            atap.name = "Atap_" + (i + 1);
            atap.transform.SetParent(rumah.transform, false);
            atap.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            atap.transform.localScale = new Vector3(1.12f, 0.22f, 1.12f);
            AturWarnaDanCollider(atap, Color.Lerp(warna, Color.white, 0.28f));
        }
    }

    private void BuatLandmark(Transform parent, Color warna)
    {
        // Landmark dibuat bertingkat agar jelas berbeda dari tiga rumah.
        GameObject dasar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dasar.name = "Landmark_Dasar";
        dasar.transform.SetParent(parent, false);
        dasar.transform.localPosition = new Vector3(0f, ukuranLandmark * 0.18f, 0f);
        dasar.transform.localScale = new Vector3(ukuranLandmark, ukuranLandmark * 0.36f, ukuranLandmark);
        AturWarnaDanCollider(dasar, warna);

        GameObject menara = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        menara.name = "Landmark_Menara";
        menara.transform.SetParent(parent, false);
        menara.transform.localPosition = new Vector3(0f, ukuranLandmark * 0.72f, 0f);
        menara.transform.localScale = new Vector3(ukuranLandmark * 0.42f, ukuranLandmark * 0.42f, ukuranLandmark * 0.42f);
        AturWarnaDanCollider(menara, Color.Lerp(warna, Color.white, 0.18f));

        GameObject puncak = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        puncak.name = "Landmark_Puncak";
        puncak.transform.SetParent(parent, false);
        puncak.transform.localPosition = new Vector3(0f, ukuranLandmark * 1.25f, 0f);
        puncak.transform.localScale = Vector3.one * ukuranLandmark * 0.28f;
        AturWarnaDanCollider(puncak, Color.yellow);
    }

    private void AturWarnaDanCollider(GameObject objek, Color warna)
    {
        Renderer rendererObjek = objek.GetComponent<Renderer>();
        if (rendererObjek != null)
            rendererObjek.material.color = warna;

        Collider colliderObjek = objek.GetComponent<Collider>();
        if (colliderObjek != null)
            Destroy(colliderObjek);
    }

    private Color WarnaPemilik(int pemilik)
    {
        switch (pemilik)
        {
            case 0: return new Color32(23, 59, 140, 255);
            case 1: return new Color32(103, 201, 245, 255);
            case 2: return new Color32(255, 105, 180, 255);
            case 3: return new Color32(229, 57, 53, 255);
            default: return Color.white;
        }
    }
}

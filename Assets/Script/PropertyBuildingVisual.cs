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
        root.transform.rotation = Quaternion.LookRotation(-arahKeTengah, Vector3.up);
        root.transform.localScale = Vector3.one;

        Color warna = WarnaPemilik(lahan.pemilik);
        if (lahan.tahapProperti == 1)
            BuatRumahMini(root.transform, warna);
        else if (lahan.tahapProperti == 2)
            BuatRumahBesar(root.transform, warna);
        else
            BuatLandmark(root.transform, warna);

        root.AddComponent<AnimasiBangunan>();

        CameraFollow kamera =
            FindAnyObjectByType<CameraFollow>();

        if (kamera != null && Application.isPlaying)
        {
            kamera.FokusBangunan(root.transform);
        }

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


    private void BuatBagian(Transform induk, string nama, PrimitiveType bentuk,
        Vector3 posisi, Vector3 skala, Color warna, Vector3 rotasi = default)
    {
        GameObject obj = GameObject.CreatePrimitive(bentuk);
        obj.name = nama;
        obj.transform.SetParent(induk, false);
        obj.transform.localPosition = posisi;
        obj.transform.localScale = skala;
        obj.transform.localEulerAngles = rotasi;
        AturWarnaDanCollider(obj, warna);
    }

    private void BuatAtapSegitiga(Transform induk, float y, float lebar, float panjang)
    {
        Color atap = new Color32(95, 51, 35, 255);

        BuatBagian(induk, "Atap_Kiri", PrimitiveType.Cube,
            new Vector3(-lebar * 0.23f, y, 0),
            new Vector3(lebar * 0.65f, 0.055f, panjang),
            atap, new Vector3(0, 0, 35));

        BuatBagian(induk, "Atap_Kanan", PrimitiveType.Cube,
            new Vector3(lebar * 0.23f, y, 0),
            new Vector3(lebar * 0.65f, 0.055f, panjang),
            atap, new Vector3(0, 0, -35));
    }

    private void BuatRumahMini(Transform parent, Color warna)
    {
        float s = ukuranBangunan;
        Color coklat = new Color32(177, 125, 91, 255);
        Color krem = new Color32(245, 222, 185, 255);
        Color pintu = new Color32(89, 53, 38, 255);

        BuatBagian(parent, "Pondasi", PrimitiveType.Cube,
            new Vector3(0, 0.025f * s, 0),
            new Vector3(1.15f * s, 0.05f * s, 1.05f * s), krem);

        BuatBagian(parent, "Dinding_Rumah", PrimitiveType.Cube,
            new Vector3(0, 0.43f * s, 0),
            new Vector3(0.85f * s, 0.8f * s, 0.75f * s), coklat);

        BuatAtapSegitiga(parent, 0.91f * s, 1.05f * s, 0.95f * s);

        BuatBagian(parent, "Pintu", PrimitiveType.Cube,
            new Vector3(0, 0.27f * s, 0.385f * s),
            new Vector3(0.22f * s, 0.48f * s, 0.025f * s), pintu);

        BuatBagian(parent, "Gagang_Pintu", PrimitiveType.Sphere,
            new Vector3(0.065f * s, 0.29f * s, 0.405f * s),
            Vector3.one * 0.035f * s, Color.yellow);

        for (int i = -1; i <= 1; i += 2)
        {
            BuatBagian(parent, "Jendela_" + i, PrimitiveType.Cube,
                new Vector3(i * 0.27f * s, 0.53f * s, 0.39f * s),
                new Vector3(0.17f * s, 0.19f * s, 0.025f * s), krem);

            BuatBagian(parent, "Kaca_" + i, PrimitiveType.Cube,
                new Vector3(i * 0.27f * s, 0.53f * s, 0.408f * s),
                new Vector3(0.13f * s, 0.15f * s, 0.012f * s),
                new Color32(125, 205, 225, 255));
        }

        BuatBagian(parent, "Cerobong", PrimitiveType.Cube,
            new Vector3(0.23f * s, 1.10f * s, -0.12f * s),
            new Vector3(0.13f * s, 0.32f * s, 0.13f * s), pintu);

        BuatBagian(parent, "Tanda_Pemilik", PrimitiveType.Cube,
            new Vector3(0, 0.08f * s, 0.54f * s),
            new Vector3(0.6f * s, 0.08f * s, 0.12f * s), warna);
    }

    private void BuatRumahBesar(Transform parent, Color warna)
    {
        float s = ukuranBangunan;
        Color coklat = new Color32(185, 140, 104, 255);
        Color krem = new Color32(248, 225, 190, 255);
        Color kayu = new Color32(105, 62, 42, 255);
        Color kaca = new Color32(125, 205, 225, 255);

        BuatBagian(parent, "Pondasi_Besar", PrimitiveType.Cube,
            new Vector3(0, 0.035f * s, 0),
            new Vector3(1.35f * s, 0.07f * s, 1.1f * s), krem);

        BuatBagian(parent, "Lantai_1", PrimitiveType.Cube,
            new Vector3(0, 0.37f * s, 0),
            new Vector3(1.05f * s, 0.65f * s, 0.85f * s), coklat);

        BuatBagian(parent, "Pembatas_Lantai", PrimitiveType.Cube,
            new Vector3(0, 0.73f * s, 0),
            new Vector3(1.13f * s, 0.08f * s, 0.93f * s), krem);

        BuatBagian(parent, "Lantai_2", PrimitiveType.Cube,
            new Vector3(0, 1.05f * s, 0),
            new Vector3(1.05f * s, 0.58f * s, 0.85f * s), coklat);

        BuatAtapSegitiga(parent, 1.43f * s, 1.25f * s, 1.02f * s);

        BuatBagian(parent, "Pintu_Utama", PrimitiveType.Cube,
            new Vector3(0, 0.29f * s, 0.44f * s),
            new Vector3(0.23f * s, 0.48f * s, 0.025f * s), kayu);

        for (int lantai = 0; lantai < 2; lantai++)
        {
            for (int i = -1; i <= 1; i += 2)
            {
                BuatBagian(parent, "Bingkai_Jendela", PrimitiveType.Cube,
                    new Vector3(i * 0.34f * s, (0.44f + lantai * 0.65f) * s, 0.44f * s),
                    new Vector3(0.23f * s, 0.25f * s, 0.025f * s), krem);

                BuatBagian(parent, "Kaca_Jendela", PrimitiveType.Cube,
                    new Vector3(i * 0.34f * s, (0.44f + lantai * 0.65f) * s, 0.46f * s),
                    new Vector3(0.18f * s, 0.20f * s, 0.012f * s), kaca);
            }
        }

        BuatBagian(parent, "Balkon", PrimitiveType.Cube,
            new Vector3(0, 0.83f * s, 0.53f * s),
            new Vector3(0.52f * s, 0.055f * s, 0.28f * s), kayu);

        for (int i = -1; i <= 1; i++)
        {
            BuatBagian(parent, "Pagar_Balkon", PrimitiveType.Cube,
                new Vector3(i * 0.22f * s, 0.96f * s, 0.65f * s),
                new Vector3(0.035f * s, 0.25f * s, 0.035f * s), krem);
        }

        BuatBagian(parent, "Warna_Pemilik", PrimitiveType.Cube,
            new Vector3(0, 0.77f * s, 0.47f * s),
            new Vector3(0.85f * s, 0.07f * s, 0.04f * s), warna);
    }

    private void BuatLandmark(Transform parent, Color warna)
    {
        float s = ukuranLandmark;

        Color krem = new Color32(230, 205, 165, 255);
        Color coklat = new Color32(137, 91, 65, 255);
        Color emas = new Color32(245, 185, 55, 255);
        Color kayu = new Color32(83, 49, 37, 255);
        Color kaca = new Color32(115, 185, 210, 255);

        // Pondasi kastil
        BuatBagian(parent, "Pondasi_Kastil", PrimitiveType.Cube,
            new Vector3(0, 0.05f * s, 0),
            new Vector3(1.7f * s, 0.1f * s, 1.15f * s), coklat);

        // Bangunan utama
        BuatBagian(parent, "Istana_Utama", PrimitiveType.Cube,
            new Vector3(0, 0.65f * s, 0),
            new Vector3(1.05f * s, 1.15f * s, 0.85f * s), krem);

        // Atap utama
        BuatAtapSegitiga(parent, 1.36f * s, 1.25f * s, 1.05f * s);

        // Dua menara kastil
        for (int i = -1; i <= 1; i += 2)
        {
            float x = i * 0.68f * s;

            BuatBagian(parent, "Menara_" + i, PrimitiveType.Cylinder,
                new Vector3(x, 0.75f * s, 0),
                new Vector3(0.28f * s, 0.75f * s, 0.28f * s), coklat);

            BuatBagian(parent, "Mahkota_Menara_" + i, PrimitiveType.Cylinder,
                new Vector3(x, 1.52f * s, 0),
                new Vector3(0.36f * s, 0.12f * s, 0.36f * s), emas);

            BuatBagian(parent, "Puncak_Emas_" + i, PrimitiveType.Sphere,
                new Vector3(x, 1.72f * s, 0),
                Vector3.one * 0.22f * s, emas);
        }

        // Gerbang kastil
        BuatBagian(parent, "Gerbang", PrimitiveType.Cube,
            new Vector3(0, 0.37f * s, 0.44f * s),
            new Vector3(0.34f * s, 0.65f * s, 0.04f * s), kayu);

        // Hiasan di atas gerbang
        BuatBagian(parent, "Hiasan_Gerbang", PrimitiveType.Cube,
            new Vector3(0, 0.76f * s, 0.46f * s),
            new Vector3(0.48f * s, 0.1f * s, 0.06f * s), emas);

        // Jendela depan
        for (int i = -1; i <= 1; i += 2)
        {
            BuatBagian(parent, "Jendela_Kastil_" + i, PrimitiveType.Cube,
                new Vector3(i * 0.33f * s, 0.93f * s, 0.44f * s),
                new Vector3(0.18f * s, 0.25f * s, 0.04f * s), kaca);
        }

        // Penanda warna pemilik
        BuatBagian(parent, "Bendera_Pemilik", PrimitiveType.Cube,
            new Vector3(0, 1.77f * s, 0),
            new Vector3(0.35f * s, 0.16f * s, 0.04f * s), warna);

        BuatBagian(parent, "Tiang_Bendera", PrimitiveType.Cylinder,
            new Vector3(-0.15f * s, 1.64f * s, 0),
            new Vector3(0.025f * s, 0.22f * s, 0.025f * s), kayu);
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

#if UNITY_EDITOR
    [ContextMenu("PREVIEW - Tampilkan Semua Bangunan")]
    private void PreviewSemuaBangunan()
    {
        HapusPreview();

        GameObject root = new GameObject("PREVIEW_BANGUNAN_3D");
        root.transform.position = new Vector3(0f, 0.5f, 0f);

        float jarak = 2f;

        GameObject rumah1 = new GameObject("01_Rumah_Mini");
        rumah1.transform.SetParent(root.transform, false);
        rumah1.transform.localPosition = new Vector3(-jarak, 0, 0);
        BuatRumahMini(rumah1.transform, Color.blue);

        GameObject rumah2 = new GameObject("02_Rumah_Besar");
        rumah2.transform.SetParent(root.transform, false);
        rumah2.transform.localPosition = Vector3.zero;
        BuatRumahBesar(rumah2.transform, Color.blue);

        GameObject kastil = new GameObject("03_Landmark_Kastil");
        kastil.transform.SetParent(root.transform, false);
        kastil.transform.localPosition = new Vector3(jarak, 0, 0);
        BuatLandmark(kastil.transform, Color.blue);

        UnityEditor.Selection.activeGameObject = root;
        UnityEditor.SceneView.FrameLastActiveSceneView();
    }

    [ContextMenu("PREVIEW - Hapus Semua Bangunan")]
    private void HapusPreview()
    {
        GameObject lama = GameObject.Find("PREVIEW_BANGUNAN_3D");

        if (lama != null)
            DestroyImmediate(lama);
    }
#endif

}

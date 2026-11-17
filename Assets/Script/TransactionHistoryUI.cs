
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class TransactionHistoryUI : MonoBehaviour
{
    [Header("Panel Riwayat")]
    public RectTransform panelRiwayat;
    public Button tombolBuka;
    public Button tombolTutup;

    [Header("Konten Riwayat")]
    public Transform content;
    public GameObject itemPrefab;

    [Header("Animasi")]
    public float durasiAnimasi = 0.3f;
    public float jarakGeser = 500f;

    [Header("Batas Riwayat")]
    public int maksimalRiwayat = 30;

    private Vector2 posisiTerbuka;
    private Vector2 posisiTertutup;
    private bool sedangTerbuka;
    private Coroutine animasi;

    private readonly List<GameObject> daftarItem =
        new List<GameObject>();

    private void Start()
    {
        if (panelRiwayat == null)
        {
            Debug.LogError("Panel Riwayat belum dipasang!");
            return;
        }

        posisiTerbuka = panelRiwayat.anchoredPosition;
        posisiTertutup = posisiTerbuka +
                         Vector2.right * jarakGeser;

        panelRiwayat.anchoredPosition = posisiTertutup;
        panelRiwayat.gameObject.SetActive(false);

        if (tombolBuka != null)
            tombolBuka.onClick.AddListener(BukaPanel);

        if (tombolTutup != null)
            tombolTutup.onClick.AddListener(TutupPanel);

        PlayerMoneyManager uang =
            FindAnyObjectByType<PlayerMoneyManager>();

        if (uang != null)
            uang.OnTransaksi += TambahkanRiwayat;
    }

    private void OnDestroy()
    {
        PlayerMoneyManager uang =
            FindAnyObjectByType<PlayerMoneyManager>();

        if (uang != null)
            uang.OnTransaksi -= TambahkanRiwayat;
    }

    public void BukaPanel()
    {
        panelRiwayat.gameObject.SetActive(true);
        JalankanAnimasi(posisiTerbuka);
        sedangTerbuka = true;
    }

    public void TutupPanel()
    {
        JalankanAnimasi(posisiTertutup);
        sedangTerbuka = false;
        StartCoroutine(NonaktifkanSetelahAnimasi());
    }

    private void JalankanAnimasi(Vector2 tujuan)
    {
        if (animasi != null)
            StopCoroutine(animasi);

        animasi = StartCoroutine(GeserPanel(tujuan));
    }

    private IEnumerator GeserPanel(Vector2 tujuan)
    {
        Vector2 awal = panelRiwayat.anchoredPosition;
        float waktu = 0f;

        while (waktu < durasiAnimasi)
        {
            waktu += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(waktu / durasiAnimasi);
            t = Mathf.SmoothStep(0f, 1f, t);

            panelRiwayat.anchoredPosition =
                Vector2.Lerp(awal, tujuan, t);

            yield return null;
        }

        panelRiwayat.anchoredPosition = tujuan;
    }

    private IEnumerator NonaktifkanSetelahAnimasi()
    {
        yield return new WaitForSecondsRealtime(durasiAnimasi);

        if (!sedangTerbuka)
            panelRiwayat.gameObject.SetActive(false);
    }

    public void TambahkanRiwayat(
        int idPemain,
        string jenis,
        string keterangan,
        int nominal,
        bool pemasukan)
    {
        if (content == null || itemPrefab == null)
        {
            Debug.LogWarning(
                "Content atau Item Prefab riwayat belum dipasang."
            );
            return;
        }

        GameObject item = Instantiate(itemPrefab, content);

        TMP_Text[] teks =
            item.GetComponentsInChildren<TMP_Text>(true);

        if (teks.Length > 0)
            teks[0].text = "Player " + (idPemain + 1);

        if (teks.Length > 1)
            teks[1].text = jenis;

        if (teks.Length > 2)
            teks[2].text = keterangan;

        if (teks.Length > 3)
        {
            string tanda = pemasukan ? "+ " : "- ";
            teks[3].text =
                tanda + "Rp" + nominal.ToString("N0");
        }

        daftarItem.Add(item);

        while (daftarItem.Count > maksimalRiwayat)
        {
            Destroy(daftarItem[0]);
            daftarItem.RemoveAt(0);
        }

        item.transform.SetAsFirstSibling();
    }
}

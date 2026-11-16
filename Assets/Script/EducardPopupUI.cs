using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EducardPopupUI : MonoBehaviour
{
    public static EducardPopupUI Instance;

    private GameObject panel;
    private TextMeshProUGUI teksKartu;
    private Coroutine coroutineTutup;

    private void Awake()
    {
        Instance = this;
        BuatTampilan();
    }

    private void BuatTampilan()
    {
        GameObject canvasObj = new GameObject("EducardCanvas");
        canvasObj.transform.SetParent(transform);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        panel = new GameObject("PanelKartu");
        panel.transform.SetParent(canvasObj.transform, false);

        Image background = panel.AddComponent<Image>();
        background.color = Color.clear;

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.15f, 0.3f);
        rect.anchorMax = new Vector2(0.85f, 0.7f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        GameObject textObj = new GameObject("TeksKartu");
        textObj.transform.SetParent(panel.transform, false);

        teksKartu = textObj.AddComponent<TextMeshProUGUI>();
        teksKartu.alignment = TextAlignmentOptions.Center;
        teksKartu.fontSize = 28;
        teksKartu.color = Color.clear;
        teksKartu.textWrappingMode = TextWrappingModes.Normal;

        RectTransform textRect = teksKartu.rectTransform;
        textRect.anchorMin = new Vector2(0.05f, 0.05f);
        textRect.anchorMax = new Vector2(0.95f, 0.95f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        panel.SetActive(false);
    }

    public void TampilkanKartu(string isiKartu, float durasi = 5f)
    {
        if (coroutineTutup != null)
            StopCoroutine(coroutineTutup);

        teksKartu.text = isiKartu;
        panel.SetActive(true);

        coroutineTutup = StartCoroutine(TutupSetelah(durasi));
    }

    private IEnumerator TutupSetelah(float durasi)
    {
        yield return new WaitForSeconds(durasi);
        panel.SetActive(false);
        coroutineTutup = null;
    }
}

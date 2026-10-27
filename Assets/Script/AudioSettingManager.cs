
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AudioSettingsManager : MonoBehaviour
{
    public Slider sliderMusik;
    public Slider sliderEfekSuara;

    public TMP_Text persenMusik;
    public TMP_Text persenEfekSuara;

    void Start()
    {
        float musik = PlayerPrefs.GetFloat("VolumeMusik", 0.8f);
        float efek = PlayerPrefs.GetFloat("VolumeEfek", 1f);

        sliderMusik.SetValueWithoutNotify(musik);
        sliderEfekSuara.SetValueWithoutNotify(efek);

        UbahMusik(musik);
        UbahEfekSuara(efek);

        sliderMusik.onValueChanged.AddListener(UbahMusik);
        sliderEfekSuara.onValueChanged.AddListener(UbahEfekSuara);
    }

    public void UbahMusik(float volume)
    {
        PlayerPrefs.SetFloat("VolumeMusik", volume);

        if (persenMusik != null)
            persenMusik.text =
                Mathf.RoundToInt(volume * 100) + "%";
    }

    public void UbahEfekSuara(float volume)
    {
        PlayerPrefs.SetFloat("VolumeEfek", volume);

        if (persenEfekSuara != null)
            persenEfekSuara.text =
                Mathf.RoundToInt(volume * 100) + "%";
    }

    void OnDestroy()
    {
        if (sliderMusik != null)
            sliderMusik.onValueChanged.RemoveListener(UbahMusik);

        if (sliderEfekSuara != null)
            sliderEfekSuara.onValueChanged.RemoveListener(UbahEfekSuara);
    }
}

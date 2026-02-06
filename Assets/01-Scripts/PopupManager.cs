using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//옵션, 종료 팝업창 담당
public class PopupManager : MonoBehaviour
{
    [SerializeField] private Slider _volumeSlider;
    [SerializeField] private Slider _offsetSlider;

    [SerializeField] private SongSelectController _controller;
    [SerializeField] private TextMeshProUGUI _offsetText;

    //Button-On Click()
    public void OpenOptions()
    {
        // 1. 저장되어 있던 값 불러와서 슬라이더 위치 맞추기
        float savedVol = PlayerPrefs.GetFloat("Volume", 100f);
        int savedOffset = PlayerPrefs.GetInt("Offset", 0);
        //슬라이더 위치 설정
        _volumeSlider.value = savedVol;
        _offsetSlider.value = savedOffset;
        OnOffsetChanged(_offsetSlider.value); //오프셋 텍스트 호출

        _controller.SetInputLock(true);
    }

    //Slider-OnValueChanged()
    public void OnVolumeChanged(float value)
    {
        AudioListener.volume = value / 100f;
    }
    public void OnOffsetChanged(float value)
    {
        int offsetValue = Mathf.RoundToInt(value);
        _offsetText.text = offsetValue > 0 ? $"+{offsetValue}ms" : $"{offsetValue}ms";
    }
    public void SaveOptions()
    {
        //데이터 저장
        PlayerPrefs.SetFloat("Volume", _volumeSlider.value);
        PlayerPrefs.SetInt("Offset", (int)_offsetSlider.value);

        _controller.SetInputLock(false);
        PlayerPrefs.Save();
    }
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}

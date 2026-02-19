using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//시간 슬라이더와 마우스 휠에 따른 시간 조절 담당
public class EditorTimeline : MonoBehaviour
{
    [SerializeField] private Slider _timeSlider;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private float _wheelSensitivity = 0.1f;

    private float _currentTime = 0f;
    private float _noteSpeed = 8f;
    void Start()
    {
        if(_audioSource.clip != null)
        {
            _timeSlider.maxValue = _audioSource.clip.length;
        }
    }

    void Update()
    {
        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(wheel) > 0.01f)
        {
            SetTime(_currentTime + wheel * _wheelSensitivity * 10f);
        }

        _gridManager.UpdateGridVisual(_currentTime, _noteSpeed);
    }

    //↓Slider - OnValueChanged에 연결
    public void OnSliderChanged(float value)
    {
        //유저가 직접 드래그 중일 때만 반응(무한루프 방지)
        SetTime(value);
    }

    void SetTime(float targetTime)
    {
        _currentTime = Mathf.Clamp(targetTime, 0, _timeSlider.maxValue);
        _timeSlider.value = _currentTime;
        _audioSource.time = _currentTime;
    }
}

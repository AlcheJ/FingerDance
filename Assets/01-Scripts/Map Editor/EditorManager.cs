using System.Collections.Generic;
using UnityEngine;

//채보 에디터의 기능 총괄
public class EditorManager : MonoBehaviour
{
    [Header("에디터 기본 설정")]
    [SerializeField] private float _scrollSpeed = 0.5f; //휠 1칸당 이동할 시간
    [SerializeField] private float _noteSpeed = 8f; //인게임과 동일한 배속
    [SerializeField] private float _judgmentY = -2.7f;

    [Header("References")]
    [SerializeField] private AudioSource _audioSource;

    // --- 내부 데이터 ---
    private float _currentTime = 0f;
    private bool _isPlaying = false;

    private List<NoteObject> _visibleNotes = new List<NoteObject>(); // 현재 화면에 보이는 노트들

    void Update()
    {
        if (_isPlaying) { _currentTime = _audioSource.time; }
        else
        {
            float wheelInput = Input.GetAxis("Mouse ScrollWheel");
            if(Mathf.Abs(wheelInput) > 0.01f) { ScrollTime(wheelInput); }
        }

        UpdateEditVisuals(); //스크롤에 따른 시각 요소 동기화
        if (Input.GetKeyDown(KeyCode.Space)) TogglePlayback();
    }

    //마우스 휠에 따른 시간 조작
    void ScrollTime(float delta)
    {
        _currentTime += delta * _scrollSpeed;
        //스크롤 범위 = 0 ~ 곡 최대 길이
        _currentTime = Mathf.Clamp(_currentTime, 0f, _audioSource.clip.length);
        _audioSource.time = _currentTime; //음악 미재생 시에도 시간 변경
    }

    //스페이스 바로 곡 재생 및 일시정지
    void TogglePlayback()
    {
        _isPlaying = !_isPlaying;
        if (_isPlaying) _audioSource.Play();
        else _audioSource.Pause();
    }

    void UpdateEditVisuals()
    {
        // [수석 개발자의 조언]
        // 인게임의 NoteSpawner 로직을 역으로 이용합니다.
        // 현재 화면에 배치된 모든 노트와 마디선에게 UpdateNotes(_currentTime, _noteSpeed)를 강제로 호출합니다.
    }
}

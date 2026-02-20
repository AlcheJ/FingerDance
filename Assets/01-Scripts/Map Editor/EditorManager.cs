using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//채보 에디터의 기능 총괄
public class EditorManager : MonoBehaviour
{
    [Header("에디터 기본 설정")]
    [SerializeField] private float _scrollSpeed = 0.5f; //휠 1칸당 이동할 시간
    [SerializeField] private float _noteSpeed = 8f; //인게임과 동일한 배속
    [SerializeField] private float _judgmentY = -2.7f;
    [SerializeField] private float _spawnY = 10f;
    [SerializeField] private float[] _laneXPositions;

    [Header("오브젝트 참조")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private TextMeshProUGUI _timeDisplayText;
    [SerializeField] private Slider _timelineSlider;

    private float _currentTime = 0f;
    private bool _isPlaying = false;
    private SongMetaData _meta;

    //[추가] 에디터 매니저로부터 받은 설정값들을 기억해둘 변수
    private float _cachedJudgmentY;
    private float _cachedSpawnY;

    private List<NoteObject> _editingNoteObjects = new List<NoteObject>(); //에디터에 올라온 노트들

    void Start()
    {
        if (GlobalDataManager.Instance.SelectedSong == null)
        {
            Debug.LogError("[Editor] 선택된 곡 정보가 없습니다! 선곡 씬부터 시작하세요.");
            return;
        }

        _meta = GlobalDataManager.Instance.SelectedSong;
        AudioClip music = Resources.Load<AudioClip>($"Sounds/{_meta.AudioFileName}");
        if (music != null)
        {
            _audioSource.clip = music;
            Debug.Log($"[Editor] 음원 로드 완료: {music.name}");
        }
        else
        {
            Debug.LogError($"[Editor] 음원을 찾을 수 없습니다: Sounds/{_meta.AudioFileName}");
            return;
        }

        //슬라이더의 최대값 = 음악 길이
        if (_audioSource.clip != null)
        {
            _timelineSlider.maxValue = _audioSource.clip.length;
        }

        //에디터 입장 시 그리드 생성
        _gridManager.RefreshGrid(_judgmentY, _spawnY);

        //기존 채보 데이터 확인(선곡 씬에서 들어가므로)
        var chart = GlobalDataManager.Instance.CurrentChart;
        if (chart != null && chart.Notes != null)
        {
            foreach (var data in chart.Notes)
            {
                CreateNoteObjectInEditor(data);
            }
        }

        //UpdateAllNotePositions();
        UpdateEditVisuals();
    }

    void Update()
    {
        if (_isPlaying)
        {
            _currentTime = _audioSource.time;
            _timelineSlider.value = _currentTime;
        }
        else
        {
            float wheelInput = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(wheelInput) > 0.01f) { ScrollTime(wheelInput); }
        }

        UpdateEditVisuals(); //스크롤에 따른 시각 요소 동기화
        if (Input.GetKeyDown(KeyCode.Space)) TogglePlayback();
    }

    //마우스 휠에 따른 시간 조작
    void ScrollTime(float delta)
    {
        if (_audioSource == null || _audioSource.clip == null) return;

        _currentTime += delta * _scrollSpeed;
        //스크롤 범위 = 0 ~ 곡 최대 길이
        _currentTime = Mathf.Clamp(_currentTime, 0f, _audioSource.clip.length);
        _audioSource.time = _currentTime; //음악 미재생 시에도 시간 변경

        if (_timeDisplayText != null)
        {
            _timeDisplayText.text = _currentTime.ToString("F3");
        }
    }

    //스페이스 바로 곡 재생 및 일시정지
    void TogglePlayback()
    {
        _isPlaying = !_isPlaying;
        if (_isPlaying) _audioSource.Play();
        else _audioSource.Pause();
    }

    //에디터 입장 시 채보에 맞는 노트 생성
    void CreateNoteObjectInEditor(NoteData data)
    {
        NoteObject noteObj = (data.Type == NoteType.Short)
        ? NotePoolManager.Instance.GetShortNote()
        : NotePoolManager.Instance.GetLongNote();

        if (noteObj != null)
        {
            //데이터 주입, 초기 위치 설정, 
            noteObj.InitializeNotes(data, _judgmentY);
            // [해결] 인덱스 오류 방지를 위해 배열 크기 체크 권장
            if (data.Lane >= 0 && data.Lane < _laneXPositions.Length)
            {
                float x = _laneXPositions[data.Lane];
                noteObj.transform.localPosition = new Vector3(x, 0, 0);
            }
            _editingNoteObjects.Add(noteObj);
        }
    }
    //화면 상의 노트들 위치 갱신
    void UpdateAllNotePositions()
    {
        foreach (var note in _editingNoteObjects)
        {
            note.UpdateNotes(_currentTime, _noteSpeed);
        }
    }
    //스크롤에 따른 화면 갱신
    void UpdateEditVisuals()
    {
        foreach (var note in _editingNoteObjects)
        {
            //노트 비활성화 없는 전용 함수 호출
            note.UpdateNotesForEditor(_currentTime, _noteSpeed);
        }

        if (_gridManager != null)
        {
            _gridManager.UpdateGridVisual(_currentTime, _noteSpeed);
        }

        if (_timeDisplayText != null)
        {
            _timeDisplayText.text = _currentTime.ToString("F3");
        }
    }

    //인스펙터 - OnValueChanged
    public void OnTimelineSliderChanged(float value)
    {
        if (!_isPlaying)
        {
            _currentTime = value;
            _audioSource.time = _currentTime;
            UpdateEditVisuals();
        }
    }
}

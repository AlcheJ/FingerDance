using System.IO; //JSON 저장용
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    //프로퍼티
    public float NoteSpeed => _noteSpeed;
    public float JudgmentY => _judgmentY;
    public float CurrentTime => _currentTime;
    public float[] LaneXPositions => _laneXPositions;
    public SongMetaData Meta => _meta;

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
    NoteObject CreateNoteObjectInEditor(NoteData data)
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
        return noteObj;
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

    //[에디터]시간과 틱을 서로 변환
    public long TimeToTick(float time)
    {
        if (_meta == null) return 0;
        //(시간 * BPM * 해상도) / 60
        return (long)((time * _meta.Bpm * _meta.Resolution) / 60f);
    }
    public float TickToTime(long tick)
    {
        if (_meta == null) return 0;
        //(틱 / 해상도) * (60 / BPM)
        float secondsPerBeat = 60f / _meta.Bpm;
        return (tick / (float)_meta.Resolution) * secondsPerBeat;
    }

    //마우스 클릭 시 그 데이터를 생성하고 리스트에 추가
    public void AddNewNoteFromEditor(NoteType type, int lane, long tick, int duration)
    {
        //틱 기반으로 마디 번호와 현재 틱 계산
        int ticksPerMeasure = _meta.Numerator * _meta.Resolution;
        int bar = (int)(tick / ticksPerMeasure);
        int innerTick = (int)tick % ticksPerMeasure;

        if (IsOverlapped(lane, bar, innerTick)) return; //설치 전 체크
        //새 NoteData 객체 생성
        NoteData newData = new NoteData(type, lane, bar, innerTick, duration);

        float secondsPerBeat = 60f / _meta.Bpm;
        float secondsPerTick = secondsPerBeat / _meta.Resolution;
        newData.TargetTime = (float)(tick * secondsPerTick);

        if (type == NoteType.Long) newData.DurationTime = duration * secondsPerTick;
        //데이터 추가
        var chart = GlobalDataManager.Instance.CurrentChart;
        chart.Notes.Add(newData);

        NoteObject newObj = CreateNoteObjectInEditor(newData); //시각적 오브젝트
        if (newObj != null)
        {
            //노트를 에디터 타임라인에 맞게 강제이동
            newObj.UpdateNotesForEditor(_currentTime, _noteSpeed);
        }
        UpdateEditVisuals();

        //노트가 인게임 순서대로 나오게 정렬
        chart.Notes.Sort((a, b) => a.TargetTime.CompareTo(b.TargetTime));
    }

    //그리드 위치에 이미 노트나 기둥이 있는가?
    public bool IsOverlapped(int lane, int bar, long tick)
    {
        var notes = GlobalDataManager.Instance.CurrentChart.Notes;
        foreach (var note in notes)
        {
            if (note.Lane != lane) continue;

            if (note.Type == NoteType.Short)
            {
                if(note.Bar == bar && note.Tick == tick) return true; //여기에 일반 노트 있음
            }
            else
            {
                //롱노트 범위 체크 (정수 기반)
                int ticksPerMeasure = _meta.Numerator * _meta.Resolution;
                long startTotalTick = (long)note.Bar * ticksPerMeasure + note.Tick;
                long endTotalTick = startTotalTick + note.DurationTick;
                long currentTotalTick = (long)bar * ticksPerMeasure + tick;

                if (currentTotalTick >= startTotalTick && currentTotalTick <= endTotalTick) return true;
            }
        }
        return false;
    }

    public void RemoveNote(int lane, int bar, long tick)
    {
        var chart = GlobalDataManager.Instance.CurrentChart;
        NoteData targetData = chart.Notes.Find(n => n.Lane == lane && n.Bar == bar && n.Tick == (int)tick);

        if (targetData != null)
        {
            NoteObject obj = _editingNoteObjects.Find(o => o.TargetTime == targetData.TargetTime && o.Lane == lane);
            if (obj != null)
            {
                _editingNoteObjects.Remove(obj);
                NotePoolManager.Instance.ReturnNote(obj);
            }

            chart.Notes.Remove(targetData);
            Debug.Log($"[Editor] 노트 삭제: {lane}번 레인, {bar}마디, {tick}틱");
        }
    }

    //저장 버튼 누르면 JSON에 물리 저장
    public void SaveChart()
    {
        var chart = GlobalDataManager.Instance.CurrentChart;
        if (chart == null) return;

        var meta = GlobalDataManager.Instance.SelectedSong;
        int diffIndex = GlobalDataManager.Instance.SelectedDifficultyIndex;

        string fileName = meta.DifficultyList[diffIndex].ChartFileName;

        //데이터 시간순 정렬, JSON으로 변환, 경로 설정
        //저장 직전 Bar, Tick 순으로 정렬
        chart.Notes.Sort((a, b) => {
            if (a.Bar != b.Bar) return a.Bar.CompareTo(b.Bar);
            return a.Tick.CompareTo(b.Tick);
        });
        string json = JsonUtility.ToJson(chart, true);
        // 주의: 빌드된 게임에서는 dataPath가 아닌 persistentDataPath를 써야 하지만, 
        // 에디터 도구이므로 dataPath를 사용하여 개발 폴더에 직접 씁니다.
        string path = Path.Combine(Application.dataPath, "Resources/Charts", fileName + ".json");

        try
        {
            File.WriteAllText(path, json);
            Debug.Log($"<color=cyan>[Editor] 저장 성공!</color> 위치: {path}");

            // [에디터 전용] 유니티가 파일 변경을 즉시 인지하도록 새로고침
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Editor] 저장 실패: {e.Message}");
        }
    }

    //선곡 씬으로 회귀(버튼의 On Click()에 연결)
    public void BackToSelectScene()
    {
        if (_audioSource != null) _audioSource.Stop();

        //에디터용 임시 데이터 해제
        GlobalDataManager.Instance.SetCurrentChart(null);
        GlobalDataManager.Instance.FadeOut(1f, () => {
            SceneManager.LoadScene("1-SongSelect");
        });
    }
}

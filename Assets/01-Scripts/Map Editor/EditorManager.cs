using System.Collections;
using System.Collections.Generic;
using System.IO; //JSON 저장용
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//채보 에디터의 기능 총괄
public class EditorManager : MonoBehaviour
{
    struct TempoNode
    {
        public long absTick;
        public double startTime;
        public float bpm;
    }
    private List<TempoNode> _tempoMap = new List<TempoNode>();

    [Header("에디터 기본 설정")]
    [SerializeField] private float _wheelSensitivity = 0.15f; //휠 1칸당 이동할 시간
    [SerializeField] private float _noteSpeed = 8f; //인게임과 동일한 배속
    [SerializeField] private float _judgmentY = -2.7f;
    [SerializeField] private float _spawnY = 10f;
    [SerializeField] private float[] _laneXPositions;

    [Header("오브젝트 참조")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private TextMeshProUGUI _timeDisplayText;
    [SerializeField] private Slider _timelineSlider;

    [Header("빌드 이후 전용 텍스트")]
    [SerializeField] private TextMeshProUGUI _saveStatusText;

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
        BuildTempoMap();
        Debug.Log($"[Editor] 템포 맵 빌드 완료. 노드 개수: {_tempoMap.Count}");

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
            _timelineSlider.maxValue = _audioSource.clip.length - 0.001f;
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

            UpdateEditVisuals(); //에디터 내 재생 중에도 실시간 갱신
        }
        else
        {
            float wheelInput = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(wheelInput) > 0.01f) { ScrollTime(wheelInput); }
        }

        if (Input.GetKeyDown(KeyCode.Space)) TogglePlayback();
    }

    public void BuildTempoMap()
    {
        _tempoMap.Clear();
        _tempoMap.Add(new TempoNode { absTick = 0, startTime = 0, bpm = _meta.Bpm });

        if (_meta.BpmEvent != null && _meta.BpmEvent.Count > 0)
        {
            // 틱 순서대로 정렬
            _meta.BpmEvent.Sort((a, b) => a.absTick.CompareTo(b.absTick));

            foreach (var ev in _meta.BpmEvent)
            {
                TempoNode last = _tempoMap[_tempoMap.Count - 1];
                long tickDelta = ev.absTick - last.absTick;
                // 이전 노드의 BPM으로 흐른 시간 계산
                double duration = (double)tickDelta / _meta.Resolution * (60.0 / last.bpm);

                _tempoMap.Add(new TempoNode
                {
                    absTick = ev.absTick,
                    startTime = last.startTime + duration,
                    bpm = ev.bpm
                });
            }
        }
    }

    //마우스 휠에 따른 시간 조작
    void ScrollTime(float delta)
    {
        if (_audioSource == null || _audioSource.clip == null) return;

        float nextTime = _currentTime + (delta * _wheelSensitivity * 10f);
        SetTime(nextTime);
    }

    public void SetTime(float targetTime)
    {
        if (_audioSource == null || _audioSource.clip == null) return;

        //슬라이더 값이 최대치가 될 때 게임이 뻗는 현상을 막기 위해 값을 뺌
        float maxSafeTime = Mathf.Max(0f, _audioSource.clip.length - 0.001f);
        //스크롤 범위 = 0 ~ 안전범위
        _currentTime = Mathf.Clamp(targetTime, 0f, maxSafeTime);
        if (_audioSource.clip != null)
        {
            try //음악 미재생 시에도 시간 변경됨
            {
                _audioSource.time = _currentTime;
            }
            catch (System.Exception e)
            {
                //만약의 상황을 대비한 예외 처리(로그만 찍고 넘어감)
                Debug.LogWarning($"[Editor] Audio Seek Error 방지: {_currentTime} / {e.Message}");
            }
        }

        //SetValueWithoutNotify: 값 변경 이벤트 발생 없이 값만 설정
        if (_timelineSlider != null) _timelineSlider.SetValueWithoutNotify(_currentTime);
        if (_timeDisplayText != null) _timeDisplayText.text = _currentTime.ToString("F3");

        UpdateEditVisuals();
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
        if (!_isPlaying && Mathf.Abs(_currentTime - value) > 0.001f)
        {
            SetTime(value);
        }
    }

    //시간과 틱을 서로 변환
    public long TimeToTick(float time)
    {
        if (_meta == null || _tempoMap.Count == 0) return 0;

        TempoNode node = _tempoMap[0];
        for (int i = _tempoMap.Count - 1; i >= 0; i--)
        {
            if (time >= _tempoMap[i].startTime)
            {
                node = _tempoMap[i];
                break;
            }
        }
        double elapsedTime = (double)time - node.startTime;
        long additionalTicks = (long)((elapsedTime * node.bpm * _meta.Resolution) / 60.0);
        return node.absTick + additionalTicks;
    }
    public float TickToTime(long tick)
    {
        if (_meta == null || _tempoMap.Count == 0) return 0;

        TempoNode node = _tempoMap[0];
        for (int i = _tempoMap.Count - 1; i >= 0; i--)
        {
            if (tick >= _tempoMap[i].absTick)
            {
                node = _tempoMap[i];
                break;
            }
        }
        long elapsedTicks = tick - node.absTick;
        double additionalTime = (double)elapsedTicks / _meta.Resolution * (60.0 / node.bpm);
        return (float)(node.startTime + additionalTime);
    }

    //누적 틱에 따라 변박을 고려한 마디 번호와 마디 내 틱을 계산
    public void GetBarAndInnerTick(long totalTick, out int bar, out int innerTick)
    {
        bar = 0;
        innerTick = (int)totalTick;

        var meta = _meta;
        long currentCumulativeTick = 0;
        int currentNumerator = meta.Numerator;

        //totaltick이 어느 마디에 속하는지 검색
        for (int i = 0; i < 5000; i++) //5000마디까지 검사
        {
            var sigEvent = meta.TimeSignatures.FindLast(s => s.Bar <= i);
            if (sigEvent != null) currentNumerator = sigEvent.Numerator;

            long measureLength = (long)currentNumerator * meta.Resolution;

            if (totalTick < currentCumulativeTick + measureLength)
            {
                bar = i;
                innerTick = (int)(totalTick - currentCumulativeTick);
                return; //마디를 찾고 종료
            }

            currentCumulativeTick += measureLength;
        }
    }

    //마우스 클릭 시 그 데이터를 생성하고 리스트에 추가
    public void AddNewNoteFromEditor(NoteType type, int lane, long totalTick, int duration)
    {
        int bar, innerTick;
        GetBarAndInnerTick(totalTick, out bar, out innerTick);

        if (IsOverlapped(lane, bar, innerTick)) return; //설치 전 체크
        //새 NoteData 객체 생성
        NoteData newData = new NoteData(type, lane, bar, innerTick, duration);

        newData.AbsoluteTick = totalTick;
        newData.TargetTime = TickToTime(totalTick);

        if (type == NoteType.Long) newData.DurationTime = TickToTime(totalTick + duration) - newData.TargetTime;
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
                if (note.Bar == bar && note.Tick == tick) return true; //여기에 일반 노트 있음
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
#if UNITY_EDITOR
        var chart = GlobalDataManager.Instance.CurrentChart;
        if (chart == null) return;

        var meta = GlobalDataManager.Instance.SelectedSong;
        int diffIndex = GlobalDataManager.Instance.SelectedDifficultyIndex;

        string fileName = meta.DifficultyList[diffIndex].ChartFileName;

        //데이터 시간순 정렬, JSON으로 변환, 경로 설정
        //저장 직전 Bar, Tick 순으로 정렬
        chart.Notes.Sort((a, b) =>
        {
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

            //[에디터 전용] 유니티가 파일 변경을 즉시 인지하도록 새로고침
            UnityEditor.AssetDatabase.Refresh();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Editor] 저장 실패: {e.Message}");
        }
#else
    ShowSaveMessage("빌드 버전에서는\n저장이\n불가능합니다!", Color.yellow);
#endif
    }
    void ShowSaveMessage(string msg, Color color)
    {
        if (_saveStatusText != null)
        {
            _saveStatusText.text = msg;
            _saveStatusText.color = color;
            StopAllCoroutines();
            StartCoroutine(ClearMessageAfterDelay(1.5f));
        }
    }
    IEnumerator ClearMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_saveStatusText != null) _saveStatusText.text = "";
    }

    //선곡 씬으로 회귀(버튼의 On Click()에 연결)
    public void BackToSelectScene()
    {
        if (_audioSource != null) _audioSource.Stop();

        //에디터용 임시 데이터 해제
        GlobalDataManager.Instance.SetCurrentChart(null);
        GlobalDataManager.Instance.FadeOut(0.5f, () =>
        {
            SceneManager.LoadScene("1-SongSelect");
        });
    }
}

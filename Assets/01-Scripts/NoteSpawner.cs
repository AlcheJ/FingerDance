using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [SerializeField] private float _noteSpeed; //노트 속도
    [SerializeField] private float _spawnY; //노트가 소환될 Y좌표
    [SerializeField] private float[] _laneXPositions; //각 레인의 X좌표
    [SerializeField] private float _judgmentY = -2.7f;
    [SerializeField] private AudioSource _audioSource;

    private SongChartData _currentChart;
    private int _noteIndex = 0; //지금 몇 번째 노트임?
    private double _startTime;
    private bool _isGameStarted = false;
    private List<NoteObject> _activeNotes = new List<NoteObject>(); //지금 움직이는 노트

    private int _barIndex; //마디선 개수
    private float _secPerMeasure = 0f; //1마디 길이
    private List<BarLineObject> _activeBarLines = new List<BarLineObject>();

    public float NoteSpeed => _noteSpeed;
    public float SpawnY => _spawnY;
    public double StartTime => _startTime;
    public List<NoteObject> ActiveNotes => _activeNotes;
    public float[] LaneXPositions => _laneXPositions;

    // --- 기즈모 구현 (Scene 뷰 시각화) ---
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_laneXPositions == null || _laneXPositions.Length == 0) return;

        // 1. 판정선 시각화 (초록색)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(-10, _judgmentY, 0), new Vector3(10, _judgmentY, 0));

        // 2. 각 레인 판정 포인트 및 소환 포인트
        for (int i = 0; i < _laneXPositions.Length; i++)
        {
            // 판정 지점 (하늘색 원)
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(new Vector3(_laneXPositions[i], _judgmentY, 0), 0.3f);

            // 소환 지점 (빨간색 선)
            Gizmos.color = Color.red;
            Vector3 sPos = new Vector3(_laneXPositions[i], _spawnY, 0);
            Gizmos.DrawLine(sPos + Vector3.left * 0.5f, sPos + Vector3.right * 0.5f);
        }
    }
#endif

    public void StartGame(SongChartData chart, AudioClip music)
    {
        if (chart == null)
        {
            Debug.LogError("[NoteSpawner] 채보 데이터가 null입니다!");
            return;
        }
        if (music == null)
        {
            Debug.LogError("[NoteSpawner] 전달받은 AudioClip이 null입니다!");
            return;
        }
        if (_audioSource == null)
        {
            Debug.LogError("[NoteSpawner] AudioSource가 인스펙터에서 연결되지 않았습니다!");
            return;
        }
        if (chart == null || chart.Notes == null || chart.Notes.Count == 0)
        {
            Debug.LogError("[NoteSpawner] 비상! 로드된 채보에 노트가 단 하나도 없습니다!");
            return;
        }

        _currentChart = chart;
        _noteIndex = 0;
        _activeNotes.Clear();

        _audioSource.clip = music; //음원 할당
        float startDelay = 2.0f; //2초 후 예약 재생
        _audioSource.PlayDelayed(startDelay);

        //게임 시작 전의 여유 시간
        _startTime = AudioSettings.dspTime + (double)startDelay;
        Debug.Log($"시작 시간 기록 완료: {_startTime}");
        _isGameStarted = true;
        Debug.Log($"[NoteSpawner] 게임 시작! 총 {chart.Notes.Count}개의 노트를 소환할 준비가 되었습니다.");

        //1마디가 몇 초인지 계산(GlobalDataManager에서 데이터 가져옴)
        var meta = GlobalDataManager.Instance.SelectedSong;
        _secPerMeasure = (60f / meta.Bpm) * meta.Numerator;
        _barIndex = 0;
        _activeBarLines.Clear();
        Debug.Log($"한 마디당 시간: {_secPerMeasure}");
    }

    private void Update()
    {
        if (!_isGameStarted) return;

        //현재 게임 시간(음악 시작점은 0)
        float currentTime = (float)(AudioSettings.dspTime - _startTime);
        CheckSpawn(currentTime); //노트 소환
        UpdateActiveNotes(currentTime); //노트 이동
        CheckBarLineSpawn(currentTime); //마디선 소환
        UpdateActiveBarLines(currentTime); //마디선 이동
        _activeNotes.RemoveAll(note => !note.gameObject.activeSelf); //비활성화된 노트 제거
        _activeBarLines.RemoveAll(bar => !bar.gameObject.activeSelf); //마디선 제거
        Debug.Log($"현재 시간: {currentTime}, 다음 소환 인덱스: {_noteIndex}");
    }

    void CheckSpawn(float currentTime)
    {
        //소환 시간 계산: (소환 지점 / 속도)만큼 미리 소환
        // [수정] 소환 예비 시간 계산 시 판정선 높이(_judgmentY)를 고려해야 합니다.
        float spawnDistance = _spawnY - _judgmentY;
        float spawnLookAhead = spawnDistance / _noteSpeed;

        //while: 동시에 여러 노트 소환할 가능성 고려
        while (_noteIndex < _currentChart.Notes.Count)
        {
            NoteData data = _currentChart.Notes[_noteIndex];
            Debug.Log($"[Spawn] {_noteIndex}번 노트 소환! Target:{data.TargetTime}s, Lane:{data.Lane}");

            if (data.TargetTime - spawnLookAhead <= currentTime)
            {
                SpawnNote(data);
                _noteIndex++;
            }
            else break; //SongDataLoader가 이미 노트를 시간 순으로 정렬함
        }
    }

    void CheckBarLineSpawn(float currentTime)
    {
        if (_currentChart.BarLineTimes == null || _barIndex >= _currentChart.BarLineTimes.Count) return;
        //노트와 동일한 공식으로 소환 시간 계산
        float spawnDistance = _spawnY - _judgmentY;
        float spawnLookAhead = spawnDistance / _noteSpeed;

        // float nextBarTime = _barIndex * _secPerMeasure;
        // [중요] _secPerMeasure를 계산해서 쓰는 대신, 리스트에 담긴 '진짜 시간'을 가져옵니다.
        float nextBarTime = _currentChart.BarLineTimes[_barIndex];
        //소환 기준: (다음 마디선 시간 - 예비 2초) < 현재 시간
        if (nextBarTime - spawnLookAhead <= currentTime)
        {
            SpawnBarLine(nextBarTime);
            _barIndex++;
        }
    }    

    void SpawnNote(NoteData data)
    {
        NoteObject note = null;

        if (data.Type == NoteType.Short) note = NotePoolManager.Instance.GetShortNote();
        else if (data.Type == NoteType.Long) note = NotePoolManager.Instance.GetLongNote();

        if (note != null)
        {
            //데이터 초기화 + 위치 설정
            note.InitializeNotes(data, _judgmentY);
            //(x,y) = (레인배열, 소환지점)
            float x = _laneXPositions[data.Lane];
            note.transform.localPosition = new Vector3(x, _spawnY, 0);

            _activeNotes.Add(note);
        }
    }

    void SpawnBarLine(float targetTime)
    {
        Debug.Log($"{_barIndex}번 마디선 소환 시도!");
        BarLineObject bar = NotePoolManager.Instance.GetBarLine();

        if(bar != null)
        {
            bar.InitializeBarLine(targetTime, _judgmentY, _spawnY);
            _activeBarLines.Add(bar);
        }
    }

    void UpdateActiveNotes(float currentTime)
    {
        //화면 상의 모든 노트를 이동
        foreach (var note in _activeNotes)
        {
            note.UpdateNotes(currentTime, _noteSpeed);
        }
    }

    void UpdateActiveBarLines(float currentTime)
    {
        foreach(var bar in _activeBarLines)
        {
            bar.UpdateBarLine(currentTime, _noteSpeed);
        }
    }

    //게임 진행 시간을 반환
    public double GetCurrentTime()
    {
        // 게임이 시작되지 않았다면 0을 반환하거나 예외 처리
        if (!_isGameStarted) return 0;

        // 절대 dspTime에서 게임 시작 시점의 dspTime을 빼서 경과 시간을 구합니다.
        return AudioSettings.dspTime - _startTime;
    }
}

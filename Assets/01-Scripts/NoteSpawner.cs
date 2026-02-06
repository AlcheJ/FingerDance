using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    /// [summary] 
    /// 1. 시간 동기화
    /// AudioSettings.dspTime을 기반으로 음악의 절대 시작 시간 기록,
    /// 매 프레임마다 (현재 시간 - 시작 시간) 계산
    /// 2. 노트 활성화
    /// _currentNoteIndex: 0번 노트부터 순서대로 소환하고 인덱스 1 증가
    /// 노트 소환 시점 = TargetTime - 미리보기 시간(배속 영향 있음)
    /// 3. 레인 배치
    /// NoteData의 Lane 정보를 보고 x 좌표값을 노트에 부여
    /// [/summary]
    
    [SerializeField] private float _noteSpeed; //노트 속도
    [SerializeField] private float _spawnY; //노트가 소환될 Y좌표
    [SerializeField] private float[] _laneXPositions; //각 레인의 X좌표

    private SongChartData _currentChart;
    private int _noteIndex = 0; //지금 몇 번째 노트임?
    private double _startTime;
    private bool _isGameStarted = false;
    public List<NoteObject> _activeNotes; //지금 움직이는 노트

    public double StartTime => _startTime;
    public List<NoteObject> ActiveNotes => _activeNotes;

    public void StartGame(SongChartData chart)
    {
        _currentChart = chart;
        _noteIndex = 0;
        _activeNotes.Clear();

        //게임 시작 전의 여유 시간
        _startTime = AudioSettings.dspTime + 3.0f;
        _isGameStarted = true;
        Debug.Log("[Spawner] 게임 시작: 3초 후 음악 재생");
    }

    private void Update()
    {
        if (!_isGameStarted) return;

        //현재 게임 시간(음악 시작점은 0)
        float currentTime = (float)(AudioSettings.dspTime - _startTime);
        CheckSpawn(currentTime); //노트 소환
        UpdateActiveNotes(currentTime); //노트 이동
        _activeNotes.RemoveAll(note => !note.gameObject.activeSelf); //비활성화된 노트 제거
    }

    void CheckSpawn(float currentTime)
    {
        //소환 시간 계산: (소환 지점 / 속도)만큼 미리 소환
        float spawnTime = _spawnY / _noteSpeed;
        //while: 동시에 여러 노트 소환할 가능성 고려
        while (_noteIndex < _currentChart.Notes.Count)
        {
            NoteData data = _currentChart.Notes[_noteIndex];

            if (data.TargetTime - spawnTime <= currentTime)
            {
                SpawnNote(data);
                _noteIndex++;
            }
            else break; //SongDataLoader가 이미 노트를 시간 순으로 정렬함
        }
    }

    void SpawnNote(NoteData data)
    {
        NoteObject note = null;

        if (data.Type == NoteType.Short) note = NotePoolManager.Instance.GetShortNote();
        else if (data.Type == NoteType.Long) note = NotePoolManager.Instance.GetLongNote();

        if(note != null)
        {
            //데이터 초기화 + 위치 설정
            note.InitializeNotes(data);
            //(x,y) = (레인배열, 소환지점)
            float x = _laneXPositions[data.Lane];
            note.transform.localPosition = new Vector3(x, _spawnY, 0);

            _activeNotes.Add(note);
        }
    }

    void UpdateActiveNotes(float currentTime)
    {
        //화면 상의 모든 노트를 이동
        foreach(var note in _activeNotes)
        {
            note.UpdateNotes(currentTime, _noteSpeed);
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

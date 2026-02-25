using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    private const float BaseSpeed = 8.0f;

    [SerializeField] private float _noteSpeed; //노트 속도
    [SerializeField] private float _spawnY; //노트가 소환될 Y좌표
    [SerializeField] private float[] _laneXPositions; //각 레인의 X좌표
    [SerializeField] private float _judgmentY = -2.7f;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private TextMeshProUGUI _speedText;

    private SongChartData _currentChart;
    private int _noteIndex = 0; //지금 몇 번째 노트임?
    private double _startTime;
    private bool _isGameStarted = false;
    private bool _isEnding = false;
    private bool _isAudioStarted = false; //곡 시작과 음악 시작은 별개
    private List<NoteObject> _activeNotes = new List<NoteObject>(); //지금 움직이는 노트

    private int _barIndex; //마디선 개수
    private float _secPerMeasure = 0f; //1마디 길이
    private List<BarLineObject> _activeBarLines = new List<BarLineObject>();

    //일시정지 기능
    private double _pauseStartTime;
    private bool _isPaused = false;

    public float NoteSpeed => _noteSpeed;
    public float SpawnY => _spawnY;
    public double StartTime => _startTime;
    public List<NoteObject> ActiveNotes => _activeNotes;
    public float[] LaneXPositions => _laneXPositions;
    public bool IsPaused => _isPaused;

    // --- 기즈모 구현 (Scene 뷰 시각화) ---
#if UNITY_EDITOR
    void OnDrawGizmos()
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

    void Start()
    {
        float multiplier = PlayerPrefs.GetFloat("UserNoteSpeed", 1.0f);
        _noteSpeed = multiplier;
        UpdateSpeedUI();
    }

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
        _startTime = AudioSettings.dspTime + (double)startDelay; //게임 시작 전의 여유 시간
        //_audioSource.PlayDelayed(startDelay); => 로딩하자마자 일시정지하면 곡이 멋대로 빨리 재생되더라

        _audioSource.PlayScheduled(_startTime); //프레임과 무관하게 정확한 타이밍에 음악 재생

        //_totalPausedTime = 0;
        _isPaused = false;
        //_isAudioStarted = false;
        _isGameStarted = true;
        Debug.Log($"[NoteSpawner] 게임 시작! 총 {chart.Notes.Count}개의 노트를 소환할 준비가 되었습니다.");

        //1마디가 몇 초인지 계산(GlobalDataManager에서 데이터 가져옴)
        var meta = GlobalDataManager.Instance.SelectedSong;
        _secPerMeasure = (60f / meta.Bpm) * meta.Numerator;
        _barIndex = 0;
        _activeBarLines.Clear();
        Debug.Log($"한 마디당 시간: {_secPerMeasure}");
    }

    void Update()
    {
        if (!_isGameStarted || _isPaused || _isEnding) return;

        //현재 게임 시간(일시정지 고려)- /_totalPausedTime
        float currentTime = (float)(AudioSettings.dspTime - _startTime);
        
        if(!_isAudioStarted && currentTime >= 0) //게임 상 시간이 0이 될 때
        {
            _audioSource.Play();
            _isAudioStarted = true;
            Debug.Log("[Spawner] 음악 재생 시작! (0.0s sync 완료)");
        }

        CheckSpawn(currentTime); //노트 소환
        UpdateActiveNotes(currentTime); //노트 이동
        CheckBarLineSpawn(currentTime); //마디선 소환
        UpdateActiveBarLines(currentTime); //마디선 이동
        _activeNotes.RemoveAll(note => !note.gameObject.activeSelf); //비활성화된 노트 제거
        _activeBarLines.RemoveAll(bar => !bar.gameObject.activeSelf); //마디선 제거

        HandleSpeedInput();

        //게임 종료 감지
        if(_noteIndex >= _currentChart.Notes.Count && _activeNotes.Count == 0 && !_audioSource.isPlaying)
        {
            StartCoroutine(FinishGameCo());
        }
    }

    void CheckSpawn(float currentTime)
    {
        //(소환 지점 / 속도)만큼 미리 소환. 판정선 높이(_judgmentY) 고려.
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

        if(bar != null) //여기 수정 중
        {
            bar.InitializeBarLine(targetTime, _judgmentY, _spawnY, true, _barIndex);
            _activeBarLines.Add(bar);
        }
    }

    void UpdateActiveNotes(float currentTime)
    {
        float finalSpeed = BaseSpeed * _noteSpeed;
        //화면 상의 모든 노트를 이동
        foreach (var note in _activeNotes)
        {
            note.UpdateNotes(currentTime, finalSpeed);
        }
    }

    void UpdateActiveBarLines(float currentTime)
    {
        float finalSpeed = BaseSpeed * _noteSpeed;
        foreach (var bar in _activeBarLines)
        {
            bar.UpdateBarLine(currentTime, finalSpeed);
        }
    }

    //게임 진행 시간을 반환
    public double GetCurrentTime()
    {
        if (!_isGameStarted) return 0;

        return AudioSettings.dspTime - _startTime;
    }

    //[배속]UI 반영
    void UpdateSpeedUI()
    {
        if(_speedText != null)
        {
            _speedText.text = $"x {_noteSpeed:F1}"; //소수점 첫째까지
        }
    }
    //[배속]키 입력에 따른 배속 조절 수행
    void HandleSpeedInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) AdjustSpeed(-0.1f);
        if (Input.GetKeyDown(KeyCode.Alpha2)) AdjustSpeed(0.1f);
    }
    void AdjustSpeed(float delta)
    {
        _noteSpeed = Mathf.Clamp(_noteSpeed + delta, 0.5f, 3.0f);
        PlayerPrefs.SetFloat("UserNoteSpeed", _noteSpeed);
        UpdateSpeedUI();
    }

    //[일시정지] PauseManager가 호출하는 함수들
    public void PauseGame()
    {
        if (_isPaused) return;

        _isPaused = true;
        _pauseStartTime = AudioSettings.dspTime; //멈춘 시점
        _audioSource.Stop();
        Debug.Log("[Spawner] 일시정지 시작");

        //if (_isAudioStarted) _audioSource.Pause();
    }
    public void ResumeGame()
    {
        if (!_isPaused) return;

        //dspTime은 일시정지 중에도 누적되므로, 멈춘 시간만큼 빼야 함
        double pauseDuration = AudioSettings.dspTime - _pauseStartTime;
        _startTime += pauseDuration;

        float currentTime = (float)(AudioSettings.dspTime - _startTime);

        if(currentTime >= 0) //이미 재생된 것은 현 시점부터 재생
        {
            _audioSource.Play();
            _audioSource.time = currentTime;
        }
        else //아직 재생 안 됐으면 재생 예약
        {
            _audioSource.PlayScheduled(_startTime);
        }

        _isPaused = false;
        Debug.Log($"[Spawner] 재개 완료. 정지 시간: {pauseDuration:F2}s");
    }
    //재시작 or 선곡 씬으로 돌아갈 때 사용(오디오 완전 정지)
    public void StopGame()
    {
        if (_audioSource != null)
        {
            _audioSource.Stop();
            _audioSource.clip = null; //메모리 누수 방지
        }
        _isGameStarted = false;
        _isPaused = false;
    }

    //한 곡 완료 시 작동
    IEnumerator FinishGameCo()
    {
        _isEnding = true;
        Debug.Log("[Game] 모든 연주 종료. 결과 집계 중...");
        yield return null; //기다릴 필요... 있나? 음.

        //ScoreManager로부터 최종 데이터를 가져와...
        if (ScoreManager.Instance != null)
        {
            PlayResult finalResult = ScoreManager.Instance.GetFinalResult();

            Debug.Log($"[Spawner] 데이터 포장 완료: {finalResult.SongID}, 점수: {finalResult.Score}");
            //GlobalDataManager(싱글톤)에 정보 주입
            GlobalDataManager.Instance.UpdateResult(finalResult);
        }
        else
        {
            Debug.LogError("[Spawner] ScoreManager를 찾을 수 없어 데이터를 보낼 수 없습니다!");
        }

        GlobalDataManager.Instance.FadeOut(1.5f, () => {
            UnityEngine.SceneManagement.SceneManager.LoadScene("3-SongResult");
        });
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//채보 에디터에 사용되는 그리드 마디선을 담당함
public class GridManager : MonoBehaviour
{
    [Header("비트 슬라이더")]
    [SerializeField] private Slider _divisionSlider;

    [Header("비트 그리드 설정")]
    // 슬라이더 인덱스에 매칭될 실제 분할 수
    private readonly int[] _divisions = { 8, 12, 16, 24, 32 };
    private int _currentDivision = 8;

    [SerializeField] private EditorManager _editorManager;

    //EditorManager에서 받은 값을 기억
    private float _cachedJudgmentY;
    private float _cachedSpawnY;

    //현재 표시 중인 그리드들(마디선 재활용)
    private List<BarLineObject> _activeGridLines = new List<BarLineObject>();

    void Awake()
    {
        if (_editorManager == null)
        {
            _editorManager = FindObjectOfType<EditorManager>();

            if (_editorManager == null)
            {
                Debug.LogError("[GridManager] EditorManager를 씬에서 찾을 수 없습니다!");
            }
        }
    }
    //슬라이더 직접 드래그 시 호출(Inspector의 Dynamic float에 연결)
    public void OnDivisionChanged(float value)
    {
        int index = Mathf.RoundToInt(value); //정수 단위 슬라이더
        _currentDivision = _divisions[index];
        
        RefreshGrid(_cachedJudgmentY, _cachedSpawnY);
    }

    //슬라이더로 설정한 분할에 맞게 그리드를 생성
    public void RefreshGrid(float judgmentY, float spawnY)
    {
        //슬라이드 바가 사용할 값 캐싱
        _cachedJudgmentY = judgmentY;
        _cachedSpawnY = spawnY;

        foreach (var line in _activeGridLines)
        {
            NotePoolManager.Instance.ReturnBarLine(line);
        }
        _activeGridLines.Clear(); //직전 그리드들 비활성화

        //데이터 유효성 검사
        if (GlobalDataManager.Instance.SelectedSong == null || GlobalDataManager.Instance.CurrentChart == null) return;

        //곡 정보
        var meta = GlobalDataManager.Instance.SelectedSong;
        var chart = GlobalDataManager.Instance.CurrentChart;
        var barTimes = GlobalDataManager.Instance.CurrentChart.BarLineTimes;
        
        Dictionary<int, long> barStartTickMap = new Dictionary<int, long>();
        long cumulativeTick = 0;
        int currentNumerator = meta.Numerator;

        //마디선 시간 리스트의 개수만큼 루프를 돌며 각 마디의 시작 틱을 기록합니다.
        for (int i = 0; i < chart.BarLineTimes.Count; i++)
        {
            //변박 확인 - SongDataLoader와 동일한 로직이어야 함
            var sigEvent = meta.TimeSignatures?.FindLast(s => s.Bar <= i);
            if (sigEvent != null) currentNumerator = sigEvent.Numerator;

            barStartTickMap[i] = cumulativeTick;
            cumulativeTick += (long)currentNumerator * meta.Resolution;
        }

        for (int i = 0; i < barStartTickMap.Count - 1; i++)
        {
            long currentBarStartTick = barStartTickMap[i];
            long nextBarStartTick = barStartTickMap[i + 1];

            float barStartTime = _editorManager.TickToTime(currentBarStartTick);
            float barEndTime = _editorManager.TickToTime(nextBarStartTick);
            float measureDuration = barEndTime - barStartTime;

            int numerator = meta.Numerator;
            var sigEvent = meta.TimeSignatures?.FindLast(s => s.Bar <= i);
            if (sigEvent != null) numerator = sigEvent.Numerator;

            //분할할 선의 개수
            int linesInThisBar = Mathf.RoundToInt(_currentDivision * (numerator / 4f));

            for (int j = 0; j < linesInThisBar; j++)
            {
                //정밀한 틱 위치 계산(정수 오차 방지)
                long gridTickOffset = (long)Mathf.Round((j * (float)numerator * meta.Resolution) / linesInThisBar);
                long targetTick = currentBarStartTick + gridTickOffset;

                //최종 위치 시간 변환
                float t = _editorManager.TickToTime(targetTick);
                BarLineObject line = NotePoolManager.Instance.GetBarLine();

                bool isMajor = (j == 0); //마디의 시작이라는 뜻
                line.InitializeBarLine(t, judgmentY, spawnY, isMajor, i); //박자가 변해도 대응 가능
                _activeGridLines.Add(line);
            }
        }
        Debug.Log($"[Grid] 총 {_activeGridLines.Count}개의 그리드 생성 완료");
    }

    //곡 시간이 바뀌면 그리드 위치 갱신
    public void UpdateGridVisual(float currentTime, float noteSpeed)
    {
        foreach(var line in _activeGridLines)
        {
            if (line == null) continue;

            if (!line.gameObject.activeSelf) line.gameObject.SetActive(true);
            line.UpdateBarLine(currentTime, noteSpeed); //(수정)그리드 활성 유무 무관하게 업데이트
            //float dist = Mathf.Abs(line._targetTime - currentTime); //전후 2초만 그리드 표시
        }
    }

    //[에디터]그리드에 맞는 틱 간격 반환
    public int GetCurrentGridTick()
    {
        //공식: (resolution * 4분음표) / division
        var meta = GlobalDataManager.Instance.SelectedSong;
        return (meta.Resolution * 4) / _currentDivision;
    }
}

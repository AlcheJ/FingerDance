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

    //EditorManager에서 받은 값을 기억
    private float _cachedJudgmentY;
    private float _cachedSpawnY;

    //현재 표시 중인 그리드들(마디선 재활용)
    private List<BarLineObject> _activeGridLines = new List<BarLineObject>();

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
        var barTimes = GlobalDataManager.Instance.CurrentChart.BarLineTimes;

        for (int i = 0; i < barTimes.Count - 1; i++)
        {
            float barStartTime = barTimes[i];
            float barEndTime = barTimes[i + 1];
            float measureDuration = barEndTime - barStartTime;

            int numerator = meta.Numerator;
            var sigEvent = meta.TimeSignatures.FindLast(s => s.Bar <= i);
            if (sigEvent != null) numerator = sigEvent.Numerator;

            int linesInThisBar = Mathf.RoundToInt(_currentDivision * (numerator / 4f));

            for (int j = 0; j < linesInThisBar; j++)
            {
                float t = barStartTime + (measureDuration * j / linesInThisBar);
                BarLineObject line = NotePoolManager.Instance.GetBarLine();
                line.InitializeBarLine(t, judgmentY, spawnY); //박자가 변해도 대응 가능
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
            if (line != null && line.gameObject.activeSelf)
            {
                line.UpdateBarLine(currentTime, noteSpeed);
            }
        }
    }
}

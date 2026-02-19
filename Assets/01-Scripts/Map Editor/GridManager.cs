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
    private int _currentDivision;

    //현재 표시 중인 그리드들(마디선 재활용)
    private List<BarLineObject> _activeGridLines = new List<BarLineObject>();

    public void OnDivisionChanged(float value)
    {
        int index = Mathf.RoundToInt(value); //정수 단위 슬라이더
        _currentDivision = _divisions[index];
        Debug.Log($"[Grid] 입력 기준: {_currentDivision}분음표");
        RefreshGrid();
    }

    //슬라이더로 설정한 분할에 맞게 그리드를 생성
    public void RefreshGrid()
    {
        foreach(var line in _activeGridLines)
        {
            NotePoolManager.Instance.ReturnBarLine(line);
        }
        _activeGridLines.Clear(); //직전 그리드들 비활성화

        //곡 정보
        var meta = GlobalDataManager.Instance.SelectedSong;
        float secondsPerBeat = 60f / meta.Bpm;

        var barTimes = GlobalDataManager.Instance.CurrentChart.BarLineTimes;

        for (int i = 0; i < barTimes.Count - 1; i++)
        {
            float barStartTime = barTimes[i];
            float barEndTime = barTimes[i + 1];
            float measureDuration = barEndTime - barStartTime;

            //한 마디를 현재 설정된 분할 수로 쪼갬
            //4/4박자에서 16분할이면 16개, 2/4박자에서 16분할이면 8개
            int linesInThisBar = Mathf.RoundToInt(_currentDivision * (meta.Numerator / 4f));

            for (int j = 0; j < linesInThisBar; j++)
            {
                float t = barStartTime + (measureDuration * j / linesInThisBar);
                //여기서 마디선 생성 및 초기화 (j == 0 일 때 마디선 강조)
            }
        }

        //곡 전체 길이만큼 그리드 생성(최적화 전)
        float totalDuration = 300f; //임시값
        for (float t = 0; t <= totalDuration; t += intervalTime)
        {
            BarLineObject line = NotePoolManager.Instance.GetBarLine();
            line.InitializeBarLine(t, -2.7f, 10f); //judgmentY, spawnY 전달
            _activeGridLines.Add(line);
        }
    }

    //곡 시간이 바뀌면 그리드 위치 갱신
    public void UpdateGridVisual(float currentTime, float noteSpeed)
    {
        foreach(var line in _activeGridLines)
        {
            line.UpdateBarLine(currentTime, noteSpeed);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SongDataLoader : MonoBehaviour
{
    struct TempoNode
    {
        public long absTick;
        public double startTime;
        public float bpm;
    }

    //Resources 폴더의 JSON 파일을 읽고 SongMetaData.cs로 반환
    public SongChartData LoadChartData(string chartFileName)
    {
        //Resources 폴더에서 TextAsset 형태로 파일을 로드
        //채보 데이터 역직렬화(로딩씬)
        TextAsset asset = Resources.Load<TextAsset>($"Charts/{chartFileName}");
        if (asset == null)
        {
            Debug.LogError($"[SongDataLoader] '{chartFileName}'의 채보 파일을 찾을 수 없습니다");
            return null;
        }
        return JsonUtility.FromJson<SongChartData>(asset.text);
    }

    public SongMetaData ParseMetadata(string jsonText)
    {
        // 곡 정보 역직렬화(선곡씬): 텍스트 -> 객체
        SongMetaData metadata = JsonUtility.FromJson<SongMetaData>(jsonText);
        if (metadata == null) return null;
        return metadata;
    }

    // 시간 계산(메타데이터 BPM + 채보 노트 정보)
    public void InitializeChartTimes(SongMetaData meta, SongChartData chart)
    {
        if (meta == null || chart == null || chart.Notes == null) return;
        if (meta.Resolution <= 0 || meta.Bpm <= 0) return;

        List<TempoNode> tempoMap = new List<TempoNode>();
        tempoMap.Add(new TempoNode { absTick = 0, startTime = 0, bpm = meta.Bpm });

        if (meta.BpmEvent != null && meta.BpmEvent.Count > 0)
        {
            // 틱 순서대로 정렬
            meta.BpmEvent.Sort((a, b) => a.absTick.CompareTo(b.absTick));
            foreach (var ev in meta.BpmEvent)
            {
                TempoNode lastNode = tempoMap[tempoMap.Count - 1];
                long tickDelta = ev.absTick - lastNode.absTick;

                // (델타 틱 / 해상도) * (60 / 이전 BPM) = 흐른 시간
                double duration = (double)tickDelta / meta.Resolution * (60.0 / lastNode.bpm);

                tempoMap.Add(new TempoNode
                {
                    absTick = ev.absTick,
                    startTime = lastNode.startTime + duration,
                    bpm = ev.bpm
                });
            }
        }

        //각 마디가 시작되는 틱을 담을 딕셔너리
        Dictionary<int, long> barStartTickMap = new Dictionary<int, long>();

        chart.BarLineTimes ??= new List<float>();
        chart.BarLineTimes.Clear();

        long currentCumulativeTick = 0; //Cumulative: 누적되는
        int currentNumerator = meta.Numerator;
        int maxCalculationBar = 700; //마지막 노트 이후에도 그리드가 보여야 함

        for (int i = 0; i <= maxCalculationBar; i++)
        {
            //현재 마디에서 변박이 있는지 확인
            if (meta.TimeSignatures != null)
            {
                var sigEvent = meta.TimeSignatures.FindLast(s => s.Bar <= i);
                if (sigEvent != null)
                {
                    currentNumerator = sigEvent.Numerator;
                }
            }

            //i번째 마디의 시작 지점: 누적된 currentCumulativeTick
            //틱 정보를 리스트화
            barStartTickMap[i] = currentCumulativeTick;
            chart.BarLineTimes.Add((float)GetTimeFromTick(currentCumulativeTick, tempoMap, meta.Resolution));

            //틱 누적
            currentCumulativeTick += (long)currentNumerator * meta.Resolution;
        }

        int totalUnits = 0; //총 판정 단위

        foreach (NoteData note in chart.Notes)
        {
            if (barStartTickMap.TryGetValue(note.Bar, out long barStartTick))
            {
                //해당 마디까지 흐른 총 틱수(딕셔너리에서 시작 틱 지점 확인)
                note.AbsoluteTick = barStartTick + note.Tick;
                note.TargetTime = (float)GetTimeFromTick(note.AbsoluteTick, tempoMap, meta.Resolution);
                //롱노트 지속시간 계산
                if (note.Type == NoteType.Short) totalUnits++;
                else if (note.Type == NoteType.Long)
                {
                    //롱노트 끝나는 시간 계산(변속 반영)
                    long endTick = note.AbsoluteTick + note.DurationTick;
                    float endTime = (float)GetTimeFromTick(endTick, tempoMap, meta.Resolution);
                    note.DurationTime = endTime - note.TargetTime;

                    //롱노트 판정 단위 계산(60틱마다 1판정)
                    int ticks = note.DurationTick / 60;
                    totalUnits += (1 + ticks);
                }
            }
        }

        if(ScoreManager.Instance != null)
        {
            ScoreManager.Instance.InitializeScore(totalUnits);
        }
        //노트 데이터를 시간순 정렬
        chart.Notes.Sort((a, b) => a.TargetTime.CompareTo(b.TargetTime));
    }

    double GetTimeFromTick(long targetTick, List<TempoNode> tempoMap, int resolution)
    {
        // 해당 틱을 포함하는 가장 최근의 변속 노드 탐색
        TempoNode node = tempoMap[0];
        for (int i = tempoMap.Count - 1; i >= 0; i--)
        {
            if (targetTick >= tempoMap[i].absTick)
            {
                node = tempoMap[i];
                break;
            }
        }

        //기준 노드 시간 + (추가 틱 / 해상도 * 60 / BPM)
        long elapsedTicks = targetTick - node.absTick;
        double additionalTime = (double)elapsedTicks / resolution * (60.0 / node.bpm);

        return node.startTime + additionalTime;
    }
}

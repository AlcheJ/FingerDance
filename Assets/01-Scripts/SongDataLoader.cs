using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;

public class SongDataLoader : MonoBehaviour
{
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
        if (chart == null || chart.Notes == null) return;
        if (meta.Resolution <= 0) return;

        float secondsPerBeat = 60f / meta.Bpm;
        float secondsPerTick = secondsPerBeat / meta.Resolution;

        //각 마디가 시작되는 틱을 담을 딕셔너리
        Dictionary<int, long> barStartTickMap = new Dictionary<int, long>();

        //채보의 마지막 마디를 찾음(여유 8개)
        int maxBar = 0;
        foreach (var note in chart.Notes) if (note.Bar > maxBar) maxBar = note.Bar;
        maxBar += 8;

        long currentCumulativeTick = 0; //Cumulative: 누적되는
        int currentNumerator = meta.Numerator;

        for (int i = 0; i <= maxBar; i++)
        {
            //현재 마디(i)에서 변박 여부 확인
            var sigEvent = meta.TimeSignatures.Find(s => s.Bar == i);
            if (sigEvent != null) //변박 있으면 변경된 박자 적용
            {
                currentNumerator = sigEvent.Numerator;
            }

            //i번째 마디의 시작 지점: 누적된 currentCumulativeTick
            //틱 정보를 리스트화
            barStartTickMap[i] = currentCumulativeTick;
            chart.BarLineTimes.Add(currentCumulativeTick * secondsPerTick);

            //틱 누적
            currentCumulativeTick += (long)currentNumerator * meta.Resolution;
        }

        foreach (NoteData note in chart.Notes)
        {
            if (barStartTickMap.TryGetValue(note.Bar, out long barStartTick))
            {
                //해당 마디까지 흐른 총 틱수(딕셔너리에서 시작 틱 지점 확인)
                long totalTicks = barStartTick + note.Tick;
                note.TargetTime = totalTicks * secondsPerTick;
                //롱노트 지속시간 계산
                if (note.Type == NoteType.Long)
                {
                    note.DurationTime = note.DurationTick * secondsPerTick;
                    Debug.Log($"[LongNote Data] {meta.SongTitle} - Duration: {note.DurationTime}s");
                }
            }
        }
        //노트 데이터를 시간순 정렬
        chart.Notes.Sort((a, b) => a.TargetTime.CompareTo(b.TargetTime));
    }
}

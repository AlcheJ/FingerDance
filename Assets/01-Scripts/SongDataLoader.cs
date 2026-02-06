using System.Collections;
using System.Collections.Generic;
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
        if(metadata == null) return null;
        return metadata;
    }

    // 시간 계산(메타데이터 BPM + 채보 노트 정보)
    public void InitializeChartTimes(SongMetaData meta, SongChartData chart)
    {
        if (chart == null || chart.Notes == null) return;

        float secondsPerBeat = 60f / meta.Bpm;
        float secondsPerTick = secondsPerBeat / meta.Resolution;
        int ticksPerMeasure = meta.Numerator * meta.Resolution;

        foreach(NoteData note in chart.Notes)
        {
            //해당 마디까지 흐른 총 틱수
            //(총 틱수)*(틱당 시간) = 절대 시간(targetTime = 판정선에 도달하는 시간)
            long totalTicks = (long)note.Bar * ticksPerMeasure + note.Tick;
            note.TargetTime = totalTicks * secondsPerTick;
            //롱노트 지속시간 계산
            if(note.Type == NoteType.Long)
            {
                note.DurationTime = note.DurationTick * secondsPerTick;
            }
        }
        //노트 데이터를 시간순 정렬
        chart.Notes.Sort((a,b) => a.TargetTime.CompareTo(b.TargetTime));
    }
}

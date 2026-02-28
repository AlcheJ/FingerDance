using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//선곡 씬 좌측의 모든 정보 출력을 담당
public class SongInfoView : MonoBehaviour
{
    [SerializeField] private Image _jacketImage;
    [SerializeField] private TextMeshProUGUI _nmLevel;
    [SerializeField] private TextMeshProUGUI _hdLevel;
    [SerializeField] private TextMeshProUGUI _bpm;
    [SerializeField] private TextMeshProUGUI _bestScore;
    [SerializeField] private TextMeshProUGUI _bestCombo;

    [SerializeField] private RectTransform _nmArea;
    [SerializeField] private RectTransform _hdArea;

    private SongMetaData _meta;

    string FormatBpm(float bpm)
    {
        return bpm.ToString("0.###");
    }
    public void ShowInfo(SongMetaData meta, SavingData record)
    {
        _meta = meta;

        _jacketImage.sprite = Resources.Load<Sprite>($"Jackets/{meta.JacketImage}");
        float minBpm = meta.Bpm;
        float maxBpm = meta.Bpm;
        //변속이 있다면 전체를 훑어서 최저/최고값을 갱신
        if (meta.BpmEvent != null && meta.BpmEvent.Count > 0)
        {
            foreach (var ev in meta.BpmEvent)
            {
                if (ev.bpm < minBpm) minBpm = ev.bpm;
                if (ev.bpm > maxBpm) maxBpm = ev.bpm;
            }
        }

        //최저와 최고가 같다면 하나만 표시, 다르다면 범위 표시
        if (Mathf.Approximately(minBpm, maxBpm))
        {
            _bpm.text = FormatBpm(minBpm);
        }
        else
        {
            _bpm.text = $"{FormatBpm(minBpm)}\n~\n{FormatBpm(maxBpm)}";
        }

        _nmLevel.text = meta.DifficultyList[0].Level.ToString();
        _hdLevel.text = meta.DifficultyList[1].Level.ToString();

        if(record != null)
        {
            _bestScore.text = record.bestScore.ToString("N0");
            _bestCombo.text = record.maxCombo.ToString("N0");
        }
        else
        {
            _bestScore.text = "0";
            _bestCombo.text = "0";
        }
    }

    public void HighlightDifficulty(int diffIndex)
    {
        //각각 NM, HD 강조
        Debug.Log($"[SongInfoView] 입력된 난이도 인덱스: {diffIndex}");
        _nmArea.localScale = (diffIndex == 0) ? Vector3.one * 1.05f : Vector3.one;
        _hdArea.localScale = (diffIndex == 1) ? Vector3.one * 1.05f : Vector3.one;
    }
}

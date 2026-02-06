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

    public void ShowInfo(SongMetaData meta, SavingData record)
    {
        _meta = meta;

        _jacketImage.sprite = Resources.Load<Sprite>($"Jackets/{meta.JacketImage}");
        _bpm.text = meta.Bpm.ToString();
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

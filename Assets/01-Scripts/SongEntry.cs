using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
//using UnityEngine.TextCore.Text;

//선곡 씬에서 각 곡의 정보를 담은 선곡 목록 프리팹을 담당
public class SongEntry : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _entryFrameImage;
    [SerializeField] private GameObject _crownFullCombo;
    [SerializeField] private GameObject _crownPerfectPlay;

    private SongMetaData _data;

    string Abbreviate(string fullName)
    {
        return fullName switch
        {
            "Normal" => "NM",
            "Hard" => "HD",
            _ => fullName // 혹시 모를 예외는 그대로 반환
        };
    }
    public void SetData(SongMetaData data, SavingData record)
    {
        _data = data;
        _titleText.text = data.SongTitle; //곡명 호출

        //레벨 호출
        string levelInfo = "";
        var diffList = data.DifficultyList;

        for (int i = 0; i < diffList.Count; i++)
        {
            string shortName = Abbreviate(diffList[i].DifficultyType);
            levelInfo += $"{shortName} {diffList[i].Level}"; //정보 합치기 (예: "NM 1")
            if (i < diffList.Count - 1) // 마지막 항목이 아닐 때만 슬래시 추가
            {
                levelInfo += " / ";
            }
        }
        _levelText.text = levelInfo; // 최종: "NM 1 / HD 4"

        //왕관 아이콘 비활성화로 초기화
        if (_crownFullCombo != null) _crownFullCombo.SetActive(false);
        if (_crownPerfectPlay != null) _crownPerfectPlay.SetActive(false);
        if (record == null) return; //기록 없으면 과정 중지
        if (record.isPerfectPlay)
        {
            if (_crownPerfectPlay != null) _crownPerfectPlay.SetActive(true);
        }
        else if (record.isFullCombo)
        {
            if (_crownFullCombo != null) _crownFullCombo.SetActive(true);
        }
        Debug.Log($"제목: {data.SongTitle}");
    }

    public void SetHighlight(bool isSelected)
    {
        transform.localScale = isSelected ? Vector3.one * 1.05f : Vector3.one;
    }
}

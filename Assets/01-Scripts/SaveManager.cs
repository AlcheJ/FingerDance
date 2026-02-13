using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SavingData
{
    public string songID;
    public int bestScore;
    public float bestAccuracy;
    public int maxCombo;
    public bool isFullCombo;
    public bool isPerfectPlay;
}

public class ListWrapper //json은 리스트를 바로 인식 못함
{
    public List<SavingData> saveList = new List<SavingData>();
}
public class SaveManager : MonoBehaviour
{
    private static SaveManager _instance;
    public static SaveManager Instance => _instance;

    private string _saveFileName = "UserData.json";
    private string _fullPath;
    //↓List와 달리 곡 수와 상관없이 한번에 찾을 수 있음
    private Dictionary<string, SavingData> _cachedRecords = new Dictionary<string, SavingData>();

    SavingData _savingData = new SavingData();
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            //persistentDataPath: 런타임 도중 플레이데이터 저장 가능
            _fullPath = Path.Combine(Application.persistentDataPath, _saveFileName);
            LoadAllRecords(); //기존 기록 부르고 시작
        }
        else { Destroy(gameObject); }
    }

    public void SaveRecord(PlayResult result)
    {
        bool isUpdated = false;
        //곡ID 및 난이도를 합친 고유 키
        string saveKey = $"{result.SongID}_{result.DifficultyLevel}";

        //기존 기록의 존재 여부 확인(예외 발생 방지)
        if (_cachedRecords.TryGetValue(saveKey, out SavingData existingData))
        {
            //정확도 기준으로 최고 기록 여부 판단
            if(result.Accuracy > existingData.bestAccuracy)
            {
                existingData.bestScore = result.Score;
                existingData.bestAccuracy = result.Accuracy;
                existingData.maxCombo = result.MaxCombo;
                existingData.isFullCombo = result.IsFullCombo;
                existingData.isPerfectPlay = result.IsPerfectPlay;
                isUpdated = true;
            }  
        }
        else //플레이 기록 없으면 새로 생성
        {
            SavingData newData = new SavingData
            {
                songID = saveKey,
                bestScore = result.Score,
                bestAccuracy = result.Accuracy,
                maxCombo = result.MaxCombo,
                isFullCombo = result.IsFullCombo,
                isPerfectPlay = result.IsPerfectPlay
            };
            _cachedRecords.Add(saveKey, newData);
            isUpdated = true;
        }
        if (isUpdated) //위의 두 경우를 기록
        {
            WriteToFile();
        }
    }

    void WriteToFile()
    {
        //딕셔너리의 값들만 리스트화
        ListWrapper wrapper = new ListWrapper();
        wrapper.saveList = new List<SavingData>(_cachedRecords.Values);
        //2번째 인자가 true라면 줄바꿈
        string json = JsonUtility.ToJson(wrapper, true);
        //실제로 파일 생성 및 텍스트 기록
        File.WriteAllText(_fullPath, json);
        Debug.Log($"[SaveManager] 데이터 저장 완료: {_fullPath}");
    }
    public void LoadAllRecords() //게임 시작 시 기록들을 딕셔너리에 보관
    {
        if (!File.Exists(_fullPath)) return;

        string json = File.ReadAllText(_fullPath);
        //읽어온 텍스트를 wrapper 형태로 복원
        ListWrapper wrapper = JsonUtility.FromJson<ListWrapper>(json);
        //복원된 리스트를 딕셔너리에 저장
        _cachedRecords.Clear();
        foreach(var data in wrapper.saveList)
        {
            _cachedRecords.Add(data.songID, data);
        }
        Debug.Log("[SaveManager] 기존 기록 로드 완료.");
    }
    //선곡 씬에서 해당 곡의 기록 유무 확인(왕관 있냐?)
    public SavingData GetRecord(string songId, int level)
    {
        string key = $"{songId}_{level}";

        if (_cachedRecords.TryGetValue(key, out SavingData data))
        {
            return data;
        }
        return null;
    }
}

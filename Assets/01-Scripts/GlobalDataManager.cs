using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//선곡한 곡의 정보, 난이도, 이전 씬의 플레이 데이터 등을 저장
//씬이 바뀌어도 유지되어야 하는 싱글톤 개체
public class GlobalDataManager : MonoBehaviour
{
    private List<SongMetaData> _allSongs = new List<SongMetaData>();
    private SongMetaData _selectedSong;
    private SongDataLoader _dataLoader; // 다른 매니저 참조용 변수
    private SongChartData _currentChart;
    private int _selectedDifficultyIndex; // 0: Easy, 1: Hard 등
    private int _currentSelectIndex;      // 선곡 씬 리스트 위치

    public List<SongMetaData> AllSongs => _allSongs;
    public SongMetaData SelectedSong => _selectedSong;
    public SongChartData CurrentChart => _currentChart;
    public int SelectedDifficultyIndex => _selectedDifficultyIndex;

    // 플레이 결과 데이터 (클래스/구조체 만들어야 함)
    private PlayResult _lastPlayResult;

    private static GlobalDataManager _instance = null;
    public static GlobalDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GlobalDataManager>();
            }
            return _instance;
        }
    }
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            _dataLoader = GetComponent<SongDataLoader>();
            Debug.Log($"[GlobalDataManager] 내가 살아남았습니다! 오브젝트 이름: {gameObject.name}");
            Initialize(); // 데이터 초기화 실행
        }
        else
        {
            Debug.LogWarning($"[GlobalDataManager] 중복된 매니저 발견되어 파괴됨: {gameObject.name}");
            Destroy(gameObject);
        }
    }

    // 게임 시작 시 모든 곡 데이터를 로드
    private void Initialize()
    {

        // 'Resources/Charts' 폴더 내의 모든 JSON을 불러옴
        TextAsset[] chartAssets = Resources.LoadAll<TextAsset>("Charts");
        Debug.Log($"찾은 파일 개수: {chartAssets.Length}");
        if (chartAssets.Length == 0)
        {
            Debug.LogWarning("[GlobalDataManager] Charts 폴더가 비어 있거나 찾을 수 없습니다.");
            return;
        }
        _allSongs.Clear();
        //불러온 파일을 순회하며 가공함
        foreach(TextAsset asset in chartAssets)
        {
            try
            {
                // SongDataLoader에게 JSON 텍스트를 번역시킴
                SongMetaData chart = _dataLoader.ParseMetadata(asset.text);
                if (chart != null)
                {
                    _allSongs.Add(chart);
                    Debug.Log($"[GlobalDataManager] 곡 로드 성공: {chart.SongTitle}");
                }
            }
            catch (Exception e)
            {
                // 한 곡이 깨졌다고 전체 로딩을 멈추지 않게 하는 예외 처리
                Debug.LogError($"[GlobalDataManager] {asset.name} 로드 중 에러 발생: {e.Message}");
            }
        }
        //로드된 곡들을 곡 ID 순으로 정렬
        _allSongs.RemoveAll(song => song == null || string.IsNullOrEmpty(song.SongID));
        _allSongs.Sort((a, b) => a.SongID.CompareTo(b.SongID));
        Debug.Log($"[GlobalDataManager] 총 {_allSongs.Count}곡의 로딩이 완료되었습니다.");
    }

    // 플레이할 곡과 난이도 설정
    public void PrepareGamePlay(SongMetaData song, int difficulty)
    {
        _selectedSong = song;
        _selectedDifficultyIndex = difficulty;
        // 이후 SceneManager.LoadScene("2-GamePlay") 호출
    }

    // 게임 종료 후 결과를 기록
    public void UpdateResult(PlayResult result)
    {
        _lastPlayResult = result;
        // TODO: 최고 기록 갱신 로직 및 SaveManager 연동
    }

    //로딩 완료된 채보 데이터 저장
    public void SetCurrentChart(SongChartData chart)
    {
        _currentChart = chart;
    }
}

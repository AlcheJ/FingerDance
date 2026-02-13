using System;
using UnityEngine;
using TMPro;

//게임플레이 씬에서 점수와 콤보, 정확도를 담당
public class ScoreManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _accuracyText;
    [SerializeField] private TextMeshProUGUI _comboText;

    //판정별 점수 비율
    private const double WeightPerfect = 1.0;
    private const double WeightGood = 0.9;
    private const double WeightOK = 0.5;
    private const double MaxScore = 1000000.0;

    private int _totalScoringUnits; //곡의 총 판정 단위(일반노트 + 롱노트 틱)
    private double _currentScore; //누적 점수
    private int _currentCombo;
    private int _maxCombo;
    private double _totalWeightEarned; //지금까지 획득한 가중치 합계
    private int _processedUnits; //지금까지 처리된 판정 단위 개수

    private int _perfectCount;
    private int _goodCount;
    private int _okCount;
    private int _missCount;

    public int CurrentScore => (int)Math.Round(_currentScore);
    public int CurrentCombo => _currentCombo;
    public float CurrentAccuracy => _processedUnits == 0 ? 100f : (float)((_totalWeightEarned / _processedUnits) * 100.0);

    //싱글톤
    private static ScoreManager _instance;
    public static ScoreManager Instance => _instance;

    void Awake()
    {
        if (_instance == null) _instance = this;
        else Destroy(gameObject);
    }

    public void InitializeScore(int totalUnits) //로딩 중 호출
    {
        _totalScoringUnits = totalUnits;
        _currentScore = 0;
        _currentCombo = 0;
        _maxCombo = 0;
        _totalWeightEarned = 0;
        _processedUnits = 0;

        _perfectCount = 0;
        _goodCount = 0;
        _okCount = 0;
        _missCount = 0;

        UpdateScoreUI();
    }

    //판정 가중치
    double GetWeight(JudgType type)
    {
        return type switch
        {
            JudgType.Perfect => WeightPerfect,
            JudgType.Good => WeightGood,
            JudgType.OK => WeightOK,
            _ => 0.0
        };
    }

    //노트 머리 판정 시 호출
    public void AddScore(JudgType type)
    {
        if (type == JudgType.None) return;

        _processedUnits++;

        if (type == JudgType.Perfect) _perfectCount++;
        else if (type == JudgType.Good) _goodCount++;
        else if (type == JudgType.OK) _okCount++;
        else if (type == JudgType.Miss) { _missCount++; ResetCombo(); }

        if (type != JudgType.Miss) AddCombo();

        _totalWeightEarned += GetWeight(type);
        CalculateCurrentScore();
        UpdateScoreUI();
    }

    //롱노트 키다운 중 틱 판정 시 호출(LongNoteObject)
    //머리 판정에 따라 키다운 판정이 결정됨
    public void AddTickScore(JudgType initialType)
    {
        _processedUnits++;
        _totalWeightEarned += GetWeight(initialType);

        if (initialType == JudgType.Perfect) _perfectCount++;
        else if (initialType == JudgType.Good) _goodCount++;
        else if (initialType == JudgType.OK) _okCount++;

        AddCombo();
        CalculateCurrentScore();
        UpdateScoreUI();
    }

    void CalculateCurrentScore()
    {
        _currentScore = (_totalWeightEarned / _totalScoringUnits) * MaxScore;
    }
    void AddCombo()
    {
        _currentCombo++;
        if (_currentCombo > _maxCombo) _maxCombo = _currentCombo;
    }

    void ResetCombo()
    {
        _currentCombo = 0;
    }

    void UpdateScoreUI()
    {
        if (_scoreText != null) _scoreText.text = CurrentScore.ToString("N0"); //3자리 단위 콤마
        if (_accuracyText != null) _accuracyText.text = $"{CurrentAccuracy:F2}%"; //소수점 2자리까지 표기
        if (_comboText != null) _comboText.text = _currentCombo > 0 ? _currentCombo.ToString() : "";
    }

    //곡 종료 후의 플레이 데이터를 PlayResult 형태로 반환
    public PlayResult GetFinalResult()
    {
        //현재 곡의 메타 데이터
        var meta = GlobalDataManager.Instance.SelectedSong;
        int diffIndex = GlobalDataManager.Instance.SelectedDifficultyIndex;
        int level = meta.DifficultyList[diffIndex].Level;

        //이 곡의 플레이 데이터
        PlayResult result = new PlayResult(
        meta.SongID,
        meta.SongTitle,
        level,
        _perfectCount,
        _goodCount,
        _okCount,
        _missCount,
        _maxCombo
        );

        //참고: 점수와 정확도는 PlayResult.CalculateResult()가 처리 중
        return result;
    }
}

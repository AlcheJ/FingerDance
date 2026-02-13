using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 게임 씬에서 얻은 정보를 수집/전송, 점수와 랭크 계산
public enum Rank { S, A, B, C, F }

[Serializable]
public class PlayResult
{
    //private const float PerfectWeight = 1f;
    //private const float GoodWeight = 0.8f;
    //private const float OKWeight = 0.5f;

    [SerializeField] private string _songId;
    [SerializeField] private string _songTitle;
    [SerializeField] private int _difficultyLevel;
    [SerializeField] private int _score;
    [SerializeField] private int _maxCombo;
    [SerializeField] private float _accuracy;
    [SerializeField] private int _perfectCount;
    [SerializeField] private int _goodCount;
    [SerializeField] private int _okCount;
    [SerializeField] private int _missCount;
    [SerializeField] private bool _isFullCombo;
    [SerializeField] private bool _isPerfectPlay;

    public string SongID => _songId;
    public string SongTitle => _songTitle;
    public int DifficultyLevel => _difficultyLevel;
    public int Score => _score;
    public int MaxCombo => _maxCombo;
    public float Accuracy => _accuracy;
    public int PerfectCount => _perfectCount;
    public int GoodCount => _goodCount;
    public int OKCount => _okCount;
    public int MissCount => _missCount;
    public bool IsFullCombo => _isFullCombo;
    public bool IsPerfectPlay => _isPerfectPlay;

    public PlayResult(string id, string title, int level, int perfect, int good, int ok, int miss, int maxCombo, int finalScore, float finalAccuracy)
    {
        _songId = id;
        _songTitle = title;
        _difficultyLevel = level;
        _maxCombo = maxCombo;
        _perfectCount = perfect;
        _goodCount = good;
        _okCount = ok;
        _missCount = miss;

        _score = finalScore; //계산된 점수를 직접 받기 위함
        _accuracy = finalAccuracy;

        SetAchievementFlags(); //점수계산을 제외하도록 변경해야 함
    }

    void SetAchievementFlags() //풀콤 퍼펙트만 저장
    {
        _isFullCombo = (_missCount == 0);
        _isPerfectPlay = (_isFullCombo && _goodCount == 0 && _okCount == 0);
    }

    public Rank GetRank()
    {
        if (_accuracy >= 97f) return Rank.S;
        else if (_accuracy >= 90f) return Rank.A;
        else if (_accuracy >= 80f) return Rank.B;
        else if (_accuracy >= 70f) return Rank.C;
        else return Rank.F;
    }
}

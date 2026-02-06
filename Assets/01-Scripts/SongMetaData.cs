using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//곡마다 담겨야 할 정보들을 json에 맞게 설정
[System.Serializable]
public class DifficultyInfo //난이도별 json 파일을 받아옴
{
    [SerializeField] private string difficultyType;
    [SerializeField] private int level;
    [SerializeField] private string chartFileName;

    public string DifficultyType => difficultyType;
    public int Level => level;
    public string ChartFileName => chartFileName;
}

[Serializable]
public class SongMetaData
{
    [SerializeField] private string songID;
    [SerializeField] private string songTitle;
    [SerializeField] private float bpm;
    [SerializeField] private int numerator; //몇 박자
    [SerializeField] private int denominator; //몇 분의...
    [SerializeField] private int resolution; //틱(480)
    [SerializeField] private float previewStartTime;
    [SerializeField] private string audioFileName;
    [SerializeField] private string jacketImage;
    [SerializeField] private List<DifficultyInfo> difficulty;

    //이하의 프로퍼티는 외부에서 접근해야 하므로 필요
    public string SongID => songID;
    public string SongTitle => songTitle;
    public float Bpm => bpm;
    public int Numerator => numerator;
    public int Denominator => denominator;
    public int Resolution => resolution;
    public float PreviewStartTime => previewStartTime;
    public string AudioFileName => audioFileName;
    public string JacketImage => jacketImage;
    public List<DifficultyInfo> DifficultyList => difficulty;
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//노트의 종류, 마디와 박자, 레인과 타이밍 등을 정의
public enum NoteType
{
    Short,
    Long
}

[System.Serializable] //json 인식해야 하므로
public class NoteData
{
    [SerializeField] private int bar;
    [SerializeField] private int tick;
    [SerializeField] private int lane;
    [SerializeField] private string noteType;
    [SerializeField] private int durationTick;

    public float TargetTime { get; set; } //판정선에 닿기 시작하는 시간
    public float DurationTime { get; set; } //롱노트 유지 시간

    //이하의 프로퍼티는 외부에서 접근해야 하므로 필요
    public int Bar => bar;
    public int Tick => tick;
    public int Lane => lane;
    public int DurationTick => durationTick;

    public NoteType Type
    {
        get
        {
            if (noteType == "Long") return NoteType.Long;
            return NoteType.Short;
        }
    }
}

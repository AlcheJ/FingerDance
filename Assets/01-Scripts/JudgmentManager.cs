using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//입력에 따른 판정을 내림
public enum JudgType
{
    None,
    Perfect,
    Good,
    OK,
    Miss
}
public class JudgmentManager : MonoBehaviour
{
    //판정 기준
    private const float WindowPerfect = 0.025f;
    private const float WindowGood = 0.060f;
    private const float WindowOK = 0.100f;
    private const float WindowMiss = 0.150f;

    [SerializeField] private NoteSpawner _noteSpawner; //활성 노트 리스트 가져오는 용도
    //판정이 날 때마다 판정 종류와 레인 번호를 전송
    public event Action<JudgType, int> OnJudged;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) ProcessInput(0);
        if (Input.GetKeyDown(KeyCode.R)) ProcessInput(1);
        if (Input.GetKeyDown(KeyCode.O)) ProcessInput(2);
        if (Input.GetKeyDown(KeyCode.P)) ProcessInput(3);
    }

    //레인에 들어온 입력에 판정을 내림
    void ProcessInput(int laneIndex)
    {
        //이 레인에서 가장 오래된(최하단) 노트 탐색
        NoteObject targetNote = Find1stNoteInLane(laneIndex);

        if (targetNote == null) return;

        //곡 시간과 노트 목표 시간 사이의 오차를 절댓값으로 계산
        float currentTime = (float)_noteSpawner.GetCurrentTime();
        float delta = currentTime - targetNote.TargetTime;
        float absDelta = Mathf.Abs(delta);

        //판정 범위에 따른 분기
        if (delta < -WindowMiss) return; //150ms보다 빨리 누른 경우 노트 보호

        JudgType result = JudgType.None;

        if (absDelta <= WindowPerfect) result = JudgType.Perfect;
        else if (absDelta <= WindowGood) result = JudgType.Good;
        else if (absDelta <= WindowOK) result = JudgType.OK;
        else if (absDelta <= WindowMiss) result = JudgType.Miss;

        if (result != JudgType.None) ExecuteJudgment(result, targetNote, laneIndex);
    }

    NoteObject Find1stNoteInLane(int laneIndex)
    {
        if (_noteSpawner.ActiveNotes == null) return null;

        foreach (var note in _noteSpawner.ActiveNotes)
        {
            //해당 레인의 아직 처리 안 된 1번째 노트 반환
            if (!note.IsHit && note.Lane == laneIndex) return note;
        }
        return null;
    }

    void ExecuteJudgment(JudgType type, NoteObject note, int lane)
    {
        if (type == JudgType.Miss)
        {
            note.HandleMiss();
            //여기에 콤보 초기화 등의 호출을 넣을 것
        }
        else note.OnHit(); //노트 처리되어 사라짐

        OnJudged?.Invoke(type, lane);
        Debug.Log($"[Judge] Lane {lane}: {type} (오차: {(AudioSettings.dspTime - _noteSpawner.StartTime - note.TargetTime) * 1000:F2}ms)");
    }
}

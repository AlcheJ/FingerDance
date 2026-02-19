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
    [SerializeField] private InputFeedbackManager _feedbackManager;
    //판정이 날 때마다 판정 종류와 레인 번호를 전송
    public event Action<JudgType, int> OnJudged;

    //각 레인이 지금 누르는 롱노트를 저장하는 곳
    private LongNoteObject[] _activeHoldNotes = new LongNoteObject[4];

    void Start()
    {
        // 인스펙터 연결 대신, 싱글톤 인스턴스의 이벤트를 찾아가서 내 함수를 등록합니다.
        if (ScoreManager.Instance != null)
        {
            //판정이 발생하면 ScoreManager의 AddScore를 실행
            this.OnJudged += (type, lane) => ScoreManager.Instance.AddScore(type);
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) { StartLaneInput(0); }
        if (Input.GetKeyDown(KeyCode.R)) { StartLaneInput(1); }
        if (Input.GetKeyDown(KeyCode.O)) { StartLaneInput(2); }
        if (Input.GetKeyDown(KeyCode.P)) { StartLaneInput(3); }

        if (Input.GetKeyUp(KeyCode.E)) HandleKeyUp(0);
        if (Input.GetKeyUp(KeyCode.R)) HandleKeyUp(1);
        if (Input.GetKeyUp(KeyCode.O)) HandleKeyUp(2);
        if (Input.GetKeyUp(KeyCode.P)) HandleKeyUp(3);
    }

    //키를 뗐을 때 실행
    void HandleKeyUp(int laneIndex)
    {
        _feedbackManager.StopFeedback(laneIndex);
        //롱노트 입력 중단 로직
        if (_activeHoldNotes[laneIndex] != null)
        {
            _activeHoldNotes[laneIndex].OnRelease(); //롱노트 입력 멈춤을 알림
            _activeHoldNotes[laneIndex] = null; //입력 중인 롱노트 정보 제거
        }
    }

    void StartLaneInput(int laneIndex)
    {
        _feedbackManager.StartFeedback(laneIndex); //빛 효과, 키음
        ProcessInput(laneIndex);
    }

    //레인에 들어온 입력에 판정을 내림
    void ProcessInput(int laneIndex)
    {
        //이 레인에서 가장 오래된(최하단) 노트 탐색, 효과 활성화
        NoteObject targetNote = Find1stNoteInLane(laneIndex);
        _feedbackManager.StartFeedback(laneIndex);

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
        else
        {
            if (note is LongNoteObject longNote)
            {
                _activeHoldNotes[lane] = longNote; //롱노트는 인덱스로
                longNote.OnHit(type);
            }
            else note.OnHit(type);
        }

        OnJudged?.Invoke(type, lane);
    }

    public void NotifyMiss(int laneIndex)
    {
        OnJudged?.Invoke(JudgType.Miss, laneIndex);
    }

    //각 레인별로 누르고 있던 롱노트 정보를 해제
    public void ClearHoldNote(int laneIndex)
    {
        if (laneIndex >= 0 && laneIndex < _activeHoldNotes.Length)
        {
            _activeHoldNotes[laneIndex] = null;
        }
    }
}

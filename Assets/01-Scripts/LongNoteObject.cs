using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LongNoteObject : NoteObject
{
    [SerializeField] private SpriteRenderer _pillarRenderer;
    [SerializeField] private Transform _tailTransform;

    private float _duration;
    private float _endTime;
    private bool _isHolding = false;
    public bool IsHolding => _isHolding;
    public override void InitializeNotes(NoteData data, float judgmentY) // 1. 인자 추가
    {
        base.InitializeNotes(data, judgmentY); //부모 초기화

        _duration = data.DurationTime; //계산된 롱노트 유지 시간 대입
        _endTime = data.TargetTime + _duration;
        _isHolding = false;
    }

    public override void UpdateNotes(float currentTime, float noteSpeed)
    {
        if (IsHit) return;

        //누르기 전
        if(!IsHolding)
        {
            base.UpdateNotes(currentTime, noteSpeed); //단노트처럼 내려옴
            //기둥 길이 설정
            UpdatePillarVisual(noteSpeed);
        }
        //누르는 상태
        else
        {
            //머리는 판정선에 고정
            transform.localPosition = new Vector3(transform.localPosition.x, 0, 0f);
            //꼬리가 판정선에 도달할 때까지 기둥 길이 감소
            float remainingDistance = (_endTime - currentTime) * noteSpeed;

            if (remainingDistance > 0)
            {
                //기둥의 Y 스케일이나 SpriteRenderer의 Size를 조절
                _pillarRenderer.size = new Vector2(_pillarRenderer.size.x, remainingDistance);
                _tailTransform.localPosition = new Vector3(0, remainingDistance, 0);
            }
            else OnLongNoteComplete(); //끝까지 키다운 성공
        }
    }

    void UpdatePillarVisual(float noteSpeed)
    {
        float pillarLength = _duration * noteSpeed;
        _pillarRenderer.size = new Vector2(_pillarRenderer.size.x, pillarLength);
        _tailTransform.localPosition = new Vector3(0, pillarLength, 0);
    }

    //InputManager에서 KeyDown 시 호출
    public override void OnHit()
    {
        _isHolding = true;
        Debug.Log("롱노트 입력 시작");
    }
    // InputManager에서 KeyUp 시 호출
    public void OnRelease(float currentTime)
    {
        if(_isHolding)
        {
            HandleMiss(); //너무 일찍 떼면 미스
        }
    }
    private void OnLongNoteComplete()
    {
        IsHit = true;
        _isHolding = false;
        Debug.Log("롱노트 입력 성공");
        gameObject.SetActive(false);
    }
}

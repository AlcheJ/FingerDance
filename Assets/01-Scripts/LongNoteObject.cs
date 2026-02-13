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
    private JudgType _initialJudg;
    private float _tickIntervalTime;
    private float _nextTickTime;
    private int _totalTicksToGive; //이 롱노트 키다운으로 점수를 얻는 횟수
    private int _ticksGivenCount; //실제 키다운 점수 횟수

    public bool IsHolding => _isHolding;
    public override void InitializeNotes(NoteData data, float judgmentY)
    {
        base.InitializeNotes(data, judgmentY); //부모 초기화

        _duration = data.DurationTime; //계산된 롱노트 유지 시간 대입
        _endTime = data.TargetTime + _duration;
        //점수를 얻는 횟수를 미리 계산
        _totalTicksToGive = data.DurationTick / 60;
        _ticksGivenCount = 0;

        _isHolding = false;
        if (data.DurationTick > 0) //60틱 단위로 판정
        {
            _tickIntervalTime = (data.DurationTime / data.DurationTick) * 60f;
            //위의 인터벌타임이 너무 작으면 강제 보정
            if (_tickIntervalTime < 0.01f) _tickIntervalTime = 0.05f;
        }
        
        //현재 배속에 맞춰 롱노트의 기둥 길이 설정
        float currentSpeed = FindObjectOfType<NoteSpawner>().NoteSpeed;
        UpdatePillarVisual(currentSpeed);
    }

    public override void UpdateNotes(float currentTime, float noteSpeed)
    {
        if (IsHit && !_isHolding) return;

        //누르기 전
        if (!IsHolding)
        {
            base.UpdateNotes(currentTime, noteSpeed); //단노트처럼 내려옴
            //기둥 길이 설정
            UpdatePillarVisual(noteSpeed);
        }
        //누르는 상태
        else
        {
            //머리는 판정선에 고정
            transform.localPosition = new Vector3(transform.localPosition.x, _currentJudgmentY, 0f);
            //꼬리가 판정선에 도달할 때까지 기둥 길이 감소
            float remainingDistance = (_endTime - currentTime) * noteSpeed;

            if (remainingDistance > 0)
            {
                //기둥의 Y 스케일이나 SpriteRenderer의 Size를 조절
                _pillarRenderer.size = new Vector2(_pillarRenderer.size.x, remainingDistance);
                _tailTransform.localPosition = new Vector3(0, remainingDistance, 0);

                //키다운 틱 판정 체크(렉으로 밀린 틱이 있으면 한번에 처리, 오차 허용)
                while (currentTime >= _nextTickTime && _ticksGivenCount < _totalTicksToGive)
                {
                    //ScoreManager에 현재 판정 타입으로 점수 요청
                    ScoreManager.Instance.AddTickScore(_initialJudg);
                    _nextTickTime += _tickIntervalTime; //다음 틱 목표 시간 갱신
                    _ticksGivenCount++;
                    // 안전장치: _tickIntervalTime이 0이면 무한루프에 빠지므로 체크
                    if (_tickIntervalTime <= 0) break;
                }
                if (currentTime >= _endTime)
                {
                    OnLongNoteComplete();
                }
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
    public override void OnHit(JudgType type)
    {
        _isHolding = true;
        _initialJudg = type;

        //현재 note 시간 기준으로 첫 틱 시간 설정
        _nextTickTime = TargetTime + _tickIntervalTime;

        Debug.Log($"롱노트 홀딩 시작: {type}");
    }
    //InputManager에서 KeyUp 시 호출
    public void OnRelease()
    {
        if(_isHolding)
        {
            HandleMiss(); //너무 일찍 떼면 미스
        }
    }
    void OnLongNoteComplete()
    {
        //만약 프레임 오차로 못 준 틱이 남아있다면 여기서 해결
        while (_ticksGivenCount < _totalTicksToGive)
        {
            ScoreManager.Instance.AddTickScore(_initialJudg);
            _ticksGivenCount++;
        }
        IsHit = true;
        _isHolding = false;
        gameObject.SetActive(false);
    }
}

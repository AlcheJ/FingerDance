using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//단일 노트와 롱 노트 구현
public class NoteObject : MonoBehaviour
{
    [SerializeField] protected SpriteRenderer _sr;

    private float _targetTime; //판정선에 도달해야 할 절대 시간
    protected float _currentJudgmentY; // 스포너로부터 전달받을 판정선 높이
    private int _lane;
    private bool _isHit = false; //이미 처리된 노트인지 확인(중복 판정 방지)

    public float TargetTime => _targetTime;
    public int Lane => _lane;
    public bool IsHit
    {
        get => _isHit;
        protected set => _isHit = value;
    }

    public virtual void InitializeNotes(NoteData data, float judgmentY)
    {
        _targetTime = data.TargetTime;
        _lane = data.Lane;
        _currentJudgmentY = judgmentY;
        _isHit = false;
        if (_sr != null) _sr.enabled = true; //시각적 초기화
        gameObject.SetActive(true);
    }

    //노트 위치 갱신(NoteManager가 매 프레임 호출)
    public virtual void UpdateNotes(float currentTime, float noteSpeed)
    {
        if (_isHit) return;

        float distance = (_targetTime - currentTime) * noteSpeed;
        //Y축 위치 강제지정
        transform.localPosition = new Vector3(transform.localPosition.x, distance + _currentJudgmentY, 0f);
        if (currentTime > _targetTime + 0.2f)
        {
            HandleMiss();
        }
    }
    //에디터에서 HandleMiss를 호출하지 않기 위함
    public void UpdateNotesForEditor(float currentTime, float noteSpeed)
    {
        float distance = (TargetTime - currentTime) * noteSpeed;
        transform.localPosition = new Vector3(transform.localPosition.x, distance + _currentJudgmentY, 0f);
        gameObject.SetActive(true); //먼 거리의 노트를 잠시 끌까?
    }

    public void HandleMiss()
    {
        _isHit = true;

        var judgeManager = FindObjectOfType<JudgmentManager>();
        if (judgeManager != null)
        {
            judgeManager.NotifyMiss(_lane);
            judgeManager.ClearHoldNote(_lane);
        }

        Debug.Log($"Miss: Lane {_lane}");
        DeactivateNote();
    }

    public virtual void OnHit(JudgType type)
    {
        _isHit = true;
        DeactivateNote();
    }
    void DeactivateNote()
    {
        gameObject.SetActive(false);
    }
}

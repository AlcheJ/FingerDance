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
    protected NoteData _data;
    protected EditorManager _editorManager;

    public float TargetTime => _targetTime;
    public int Lane => _lane;
    public bool IsHit
    {
        get => _isHit;
        protected set => _isHit = value;
    }

    void Awake()
    {
        //에디터 매니저를 찾아서 할당(성능을 위해 Awake에서 미리 찾아둡니다)
        _editorManager = FindObjectOfType<EditorManager>();
    }

    public virtual void InitializeNotes(NoteData data, float judgmentY)
    {
        _data = data;
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
    public virtual void UpdateNotesForEditor(float currentTime, float noteSpeed)
    {
        if (_data == null || _editorManager == null) return;

        float accurateTime = _editorManager.TickToTime(_data.AbsoluteTick);

        float distance = (accurateTime - currentTime) * noteSpeed;
        transform.localPosition = new Vector3(transform.localPosition.x, distance + _currentJudgmentY, 0f);
        gameObject.SetActive(true);
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

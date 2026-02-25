using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GhostNote : MonoBehaviour
{
    [Header("오브젝트")]
    [SerializeField] private SpriteRenderer _sr;
    [SerializeField] private EditorManager _editorManager; //속도, 시간 참조용
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private TextMeshProUGUI _modeText; //노트 타입 표시용

    [Header("롱노트 미리보기")]
    [SerializeField] private SpriteRenderer _previewPillar;

    private NoteType _currentEditMode = NoteType.Short;
    private bool _isPlacingLongNote = false; //롱노트의 꼬리 찍는 상태
    private int _startLane;
    private long _startTick; //롱노트의 시작지점들
    public NoteType CurrentEditMode => _currentEditMode;

    //마우스 커서에 담기는 정보들
    public int SnappedLane { get; private set; }
    public long SnappedTick { get; private set; }
    public int SnappedBar { get; private set; }
    public int SnappedInnerTick { get; private set; }

    void Start()
    {
        UpdateModeUI();
        if (_previewPillar != null) _previewPillar.gameObject.SetActive(false);
    }
    void Update()
    {
        //UI 오브젝트 위에서는 노트 숨김
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            _sr.enabled = false;
            if (_previewPillar != null) _previewPillar.gameObject.SetActive(false);
            return;
        }
        _sr.enabled = true;

        HandleModeSwitch();
        UpdateGhostPosition();

        if (Input.GetMouseButtonDown(0)) //마우스 왼쪽 클릭 시
        {
            if (!_isPlacingLongNote && _editorManager.IsOverlapped(SnappedLane, SnappedBar, SnappedInnerTick))
            {
                Debug.LogWarning("이미 자리잡은 노트가 있습니다!");
                return;
            }
            HandlePlacement();
        }
        if (Input.GetMouseButtonDown(1))
        {
            if(_isPlacingLongNote)
            {
                _isPlacingLongNote = false;
                _previewPillar.gameObject.SetActive(false);
            }
            else
            {
                //누적 SnappedTick을 마디 내 틱으로 변환
                int ticksPerMeasure = _editorManager.Meta.Numerator * _editorManager.Meta.Resolution;
                int innerTick = (int)(SnappedTick % ticksPerMeasure); //마디 내 틱 계산

                _editorManager.RemoveNote(SnappedLane, SnappedBar, SnappedTick);
            }
        }

        //롱노트 설치 중이면 기둥 미리보기
        if (_isPlacingLongNote) UpdatePillarPreview();
    }

    void HandleModeSwitch()
    {
        if (_isPlacingLongNote) return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _currentEditMode = NoteType.Short;
            UpdateModeUI();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _currentEditMode = NoteType.Long;
            UpdateModeUI();
        }
    }

    //설정된 노트 설치
    void HandlePlacement()
    {
        Debug.Log($"[Placement] 시도 - Lane: {SnappedLane}, Tick: {SnappedTick}, Occupied: {_editorManager.IsOverlapped(SnappedLane, SnappedBar, SnappedTick)}");

        if (_currentEditMode == NoteType.Short)
        {
            PlaceNote(NoteType.Short, SnappedTick, 0);
        }
        else if (_currentEditMode == NoteType.Long)
        {
            if (!_isPlacingLongNote) //머리
            {
                _isPlacingLongNote = true;
                _startLane = SnappedLane;
                _startTick = SnappedTick;
                _previewPillar.gameObject.SetActive(true);
            }
            else //꼬리
            {
                if (SnappedTick > _startTick && SnappedLane == _startLane)
                {
                    long duration = SnappedTick - _startTick;
                    PlaceNote(NoteType.Long, _startTick, (int)duration);

                    _isPlacingLongNote = false;
                    _previewPillar.gameObject.SetActive(false);
                    Debug.Log($"[Editor] 롱노트 설치 완료: {duration}틱 길이");
                }
            }
        }
    }

    //찍은 노트를 데이터에 저장
    void PlaceNote(NoteType type, long tick, int duration)
    {
        //인자의 정보로 노트 제작을 요청(실제 제작은 에디터 매니저가 함)
        _editorManager.AddNewNoteFromEditor(type, SnappedLane, tick, duration);
    }

    void UpdateModeUI()
    {
        if (_modeText != null)
        {
            _modeText.text = $"Note Type: {_currentEditMode}";
        }
        _sr.color = (_currentEditMode == NoteType.Short)
            ? new Color(0, 1, 1, 0.5f)
            : new Color(1, 0.9f, 0, 0.5f);
    }

    void UpdateGhostPosition()
    {
        //마우스 커서에 해당하는 월드 좌표 확보
        Vector3 mouseInput = Input.mousePosition;
        mouseInput.z = 10f; //카메라 거리 보정
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(mouseInput);

        //(x축)현재 레인, 시간 및 틱 순서로 스냅 계산
        float closestX = GetClosestLaneX(mousePos.x, out int laneIndex);
        SnappedLane = laneIndex;
        //(y축)시간 역산: TargetTime = (Y - JudgmentY) / Speed + CurrentTime
        float mouseTime = (mousePos.y - _editorManager.JudgmentY) / _editorManager.NoteSpeed + _editorManager.CurrentTime;
        SnappedTick = SnapTimeToGrid(mouseTime);

        //변박에 대응하는 계산용 함수 호출
        int bar;
        int innerTick;
        _editorManager.GetBarAndInnerTick(SnappedTick, out bar, out innerTick);

        SnappedBar = bar;
        SnappedInnerTick = innerTick;
        
        //고스트 노트의 틱을 시간으로 바꿈
        float finalY = (_editorManager.TickToTime(SnappedTick) - _editorManager.CurrentTime) * _editorManager.NoteSpeed + _editorManager.JudgmentY;
        transform.localPosition = new Vector3(closestX, finalY, 0);

        if(Input.GetMouseButtonDown(1) && !_isPlacingLongNote)
        {
            _editorManager.RemoveNote(SnappedLane, SnappedBar, SnappedInnerTick);
        }
    }

    //마우스의 X 좌표와 가장 가까운 레인의 X 위치 탐색
    float GetClosestLaneX(float mouseX, out int index)
    {
        float[] lanePositions = _editorManager.LaneXPositions;
        float closestX = lanePositions[0];
        index = 0;
        float minDistance = Mathf.Abs(mouseX - lanePositions[0]);

        //레인에서 마우스와 가장 가까운 곳을 탐색하는 로직
        for (int i = 1; i < lanePositions.Length; i++)
        {
            float distance = Mathf.Abs(mouseX - lanePositions[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestX = lanePositions[i];
                index = i;
            }
        }
        return closestX;
    }

    //그리드 설정에 맞는 시간을 스냅
    long SnapTimeToGrid(float time)
    {
        long rawTicks = _editorManager.TimeToTick(time);
        int gridInterval = _gridManager.GetCurrentGridTick();
        if (gridInterval <= 0) return rawTicks;

        //(현재 틱 / 간격)을 반올림, 다시 간격을 곱함
        long snappedTicks = (long)Mathf.Round((float)rawTicks / gridInterval) * gridInterval;

        //음수값 방지
        return System.Math.Max(0, snappedTicks);
    }

    //롱노트 기둥 미리보기
    void UpdatePillarPreview()
    {
        float currentFinalSpeed = _editorManager.NoteSpeed;
        
        long diffTicks = SnappedTick - _startTick; //(현재 마우스 위치) - (머리 위치)
        float durationSeconds = _editorManager.TickToTime(diffTicks);

        //(시간 * 속도) = 롱노트 기둥 길이
        float height = durationSeconds * currentFinalSpeed;

        if (height > 0)
        {
            // 만약 기둥이 머리 중앙에서 시작하게 하고 싶다면 Y를 약간만 보정하세요.
            _previewPillar.transform.localPosition = Vector3.zero;
            _previewPillar.size = new Vector2(_previewPillar.size.x, height);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

//오브젝트 풀링 방식으로 노트를 (비)활성화
public class NotePoolManager : MonoBehaviour
{
    //싱글톤
    private static NotePoolManager _instance;
    public static NotePoolManager Instance => _instance;

    //프리팹
    [SerializeField] private GameObject _shortNotePrefab;
    [SerializeField] private GameObject _longNotePrefab;
    [SerializeField] private GameObject _barLinePrefab;
    //부모 오브젝트
    [SerializeField] private Transform _shortPoolParent;
    [SerializeField] private Transform _longPoolParent;
    [SerializeField] private Transform _barLinePoolParent;
    //노트 저장용 큐(선입선출, 정해진 순서로 나옴)
    private Queue<NoteObject> _shortPool = new Queue<NoteObject>();
    private Queue<LongNoteObject> _longPool = new Queue<LongNoteObject>();
    private Queue<BarLineObject> _barLinePool = new Queue<BarLineObject>();
    private void Awake()
    {
        // 싱글톤 초기화
        if (_instance == null) _instance = this;
        else Destroy(gameObject);
    }

    //노트 생성 단계. 로딩 바에 진행도를 반영하기 위해 Action 매개변수 추가.
    public void PreparePool(SongChartData chart, Action<float> progressCallback)
    {
        if (chart == null || chart.Notes == null) return;

        int shortCount = 0;
        int longCount = 0;
        int maxBar = 0;
        //foreach문으로 chart.Notes 순회, note.Type에 따라 카운트 증가
        foreach (var note in chart.Notes)
        {
            if (note.Type == NoteType.Short) shortCount++;
            else if (note.Type == NoteType.Long) longCount++;

            //마지막 노트가 속한 마디 번호 탐색
            if (note.Bar > maxBar) maxBar = note.Bar;
        }

        int totalBarsToCreate = maxBar + 8;
        int totalToCreate = (shortCount + 5) + (longCount + 5) + totalBarsToCreate;
        int currentCreated = 0;
        
        for (int i = 0; i < shortCount + 5; i++) //5개는 여유분
        {
            CreateNotes(NoteType.Short);
            currentCreated++;
            //진행도 계산 후 콜백 호출
            progressCallback?.Invoke((float)currentCreated / totalToCreate);
        }
        for (int i = 0; i < longCount + 5; i++) //5개는 여유분
        {
            CreateNotes(NoteType.Long);
            currentCreated++;
            //진행도 계산 후 콜백 호출
            progressCallback?.Invoke((float)currentCreated / totalToCreate);
        }
        for(int i = 0; i < totalBarsToCreate; i++)
        {
            CreateBarLine();
            currentCreated++;
            progressCallback?.Invoke((float)currentCreated / totalToCreate);
        }
        Debug.Log($"[Pool] 노트: {shortCount + longCount}개, 마디선: {totalBarsToCreate}개");
    }

    //노트 종류별로 생성해 보관
    private void CreateNotes(NoteType type)
    {
        if (type == NoteType.Short)
        {
            //Instantiate(원본, 부모): 부모 아래에 생성해 Hierarchy 정돈
            GameObject go = Instantiate(_shortNotePrefab, _shortPoolParent);
            NoteObject note = go.GetComponent<NoteObject>();
            go.SetActive(false);
            _shortPool.Enqueue(note);
        }
        else if (type == NoteType.Long)
        {
            GameObject go = Instantiate(_longNotePrefab, _longPoolParent);
            LongNoteObject note = go.GetComponent<LongNoteObject>();
            go.SetActive(false);
            _longPool.Enqueue(note);
        }
    }

    //NoteManager가 단/롱노트 1개를 대여. 풀이 비었으면 만드는 로직 포함.
    public NoteObject GetShortNote()
    {
        if (_shortPool.Count == 0) CreateNotes(NoteType.Short);

        NoteObject note = _shortPool.Dequeue();
        return note;
    }
    public NoteObject GetLongNote()
    {
        if (_longPool.Count == 0) CreateNotes(NoteType.Long);

        NoteObject note = _longPool.Dequeue();
        return note;
    }
    //노트 반환
    public void ReturnNote(NoteObject note)
    {
        note.gameObject.SetActive(false);
        if (note is LongNoteObject longNote)
        {
            _longPool.Enqueue(longNote);
        }
        else _shortPool.Enqueue(note);
    }

    void CreateBarLine()
    {
        //폴더 내에 마디선 정돈
        GameObject go = Instantiate(_barLinePrefab, _barLinePoolParent);
        BarLineObject bar = go.GetComponent<BarLineObject>();
        go.SetActive(false);
        _barLinePool.Enqueue(bar);
    }
    public BarLineObject GetBarLine()
    {
        if (_barLinePool.Count == 0) CreateBarLine();

        return _barLinePool.Dequeue();
    }
    public void ReturnBarLine(BarLineObject barLine)
    {
        barLine.gameObject.SetActive(false);
        _barLinePool.Enqueue(barLine);
    }
}

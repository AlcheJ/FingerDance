using System.Collections;
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
    //부모 오브젝트
    [SerializeField] private Transform _shortPoolParent;
    [SerializeField] private Transform _longPoolParent;
    //노트 저장용 큐(선입선출, 정해진 순서로 나옴)
    private Queue<NoteObject> _shortPool = new Queue<NoteObject>();
    private Queue<LongNoteObject> _longPool = new Queue<LongNoteObject>();
    private void Awake()
    {
        // 싱글톤 초기화
        if (_instance == null) _instance = this;
        else Destroy(gameObject);
    }
    public void PreparePool(SongChartData chart)
    {
        if (chart == null || chart.Notes == null) return;

        int shortCount = 0;
        int longCount = 0;
        //foreach문으로 chart.Notes 순회, note.Type에 따라 카운트 증가
        foreach (var note in chart.Notes)
        {
            if (note.Type == NoteType.Short) shortCount++;
            else if (note.Type == NoteType.Long) longCount++;
        }
        CreateNotes(NoteType.Short, shortCount + 5); //5개는 여유분
        CreateNotes(NoteType.Long, longCount + 5);
        Debug.Log($"[Pool] 단노트: {shortCount}개, 롱노트: {longCount}개");
    }

    //노트 종류별로 생성해 보관
    private void CreateNotes(NoteType type, int count)
    {
        for(int i = 0; i < count; i++)
        {
            if(type == NoteType.Short)
            {
                //Instantiate(원본, 부모): 부모 아래에 생성해 Hierarchy 정돈
                GameObject go = Instantiate(_shortNotePrefab, _shortPoolParent);
                NoteObject note = go.GetComponent<NoteObject>();
                go.SetActive(false);
                _shortPool.Enqueue(note);
            }
            else if(type == NoteType.Long)
            {
                GameObject go = Instantiate(_longNotePrefab, _longPoolParent);
                LongNoteObject note = go.GetComponent<LongNoteObject>();
                go.SetActive(false);
                _longPool.Enqueue(note);
            }
        }
    }

    //NoteManager가 단/롱노트 1개를 대여. 풀이 비었으면 만드는 로직 포함.
    public NoteObject GetShortNote()
    {
        if (_shortPool.Count == 0) CreateNotes(NoteType.Short, 1);

        NoteObject note = _shortPool.Dequeue();
        return note;
    }
    public NoteObject GetLongNote()
    {
        if (_longPool.Count == 0) CreateNotes(NoteType.Long, 1);

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
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//SongMetaData와 별개로, 노트 정보만 보관하는 데이터 뭉치
public class SongChartData
{
    [SerializeField] private List<NoteData> notes;
    public List<NoteData> Notes => notes;
}

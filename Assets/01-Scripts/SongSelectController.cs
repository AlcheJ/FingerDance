using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class SongSelectController : MonoBehaviour
{
    [SerializeField] private GameObject _entryPrefab;
    [SerializeField] private RectTransform _contentTransform; // ScrollRect의 Content 객체
    [SerializeField] private SongInfoView _infoView;
    [SerializeField] private AudioSource _audioSource;

    // 생성된 UI 스크립트들을 담아둘 리스트(인덱스 접근용)
    private List<SongEntry> _entryList = new List<SongEntry>();
    private List<SongMetaData> _allSongs;
    private int _currentSongIndex = 0;
    private int _currentDifficultyIndex = 0;

    // 프리뷰 변수들
    private Coroutine _previewCoroutine;
    private float _baseVolume;
    private const float PreviewDelay = 0.5f;
    private const float LoopDuration = 15.0f;
    private const float FadeDuration = 1.0f;

    // 팝업창
    private bool _isInputLocked = false;
    public void SetInputLock(bool isLocked) => _isInputLocked = isLocked;

    private void Awake()
    {
        if (_audioSource != null)
        {
            _baseVolume = _audioSource.volume; //초기 설정된 볼륨 저장
        }
    }
    private void Start()
    {
        RefreshSongList();
        PlayPreview();
        GlobalDataManager.Instance.FadeIn(1.5f);
    }

    private void Update()
    {
        if (_entryList.Count == 0) return;

        HandleNavigation();
    }

    //GlobalDataManager가 만든 데이터로 UI 리스트 생성
    private void RefreshSongList()
    {
        foreach (Transform child in _contentTransform)
        {
            Destroy(child.gameObject);
        }
        _entryList.Clear(); //기존 UI 제거

        _allSongs = GlobalDataManager.Instance.AllSongs;

        if (_allSongs == null || _allSongs.Count == 0) return;

        for (int i = 0; i < _allSongs.Count; i++)
        {
            //프리팹 생성, 부모 설정
            //생성된 객체에서 SongEntry 컴포넌트 가져옴
            GameObject go = Instantiate(_entryPrefab, _contentTransform);

            SongEntry entry = go.GetComponent<SongEntry>();
            if (entry != null)
            {
                //데이터 주입: 곡 정보와 플레이 기록(풀콤 유무 등)
                var record = SaveManager.Instance.GetRecord(_allSongs[i].SongID);
                entry.SetData(_allSongs[i], record);
                _entryList.Add(entry); //이 순서가 인덱스 번호임
            }
        }
        if (_entryList.Count > 0)
        {
            UpdateSelection();
        }
    }

    void UpdateSelection() //현재 인덱스(0번)에 시각적 강조 추가
    {
        for (int i = 0; i < _entryList.Count; i++)
        {
            //현재 인덱스와 일치하면 true 전송
            _entryList[i].SetHighlight(i == _currentSongIndex);
        }
        //여기에 선택된 곡을 스크롤 뷰 중앙에 오게 하는 로직 넣어야 함

        SongMetaData currentSong = _allSongs[_currentSongIndex];
        SavingData currentRecord = SaveManager.Instance.GetRecord(currentSong.SongID);
        _infoView.ShowInfo(currentSong, currentRecord);
        _infoView.HighlightDifficulty(_currentDifficultyIndex); //기본값 0(NM)
    }

    //키보드 입력에 따라 인덱스 변경
    void HandleNavigation()
    {
        if (_isInputLocked) return; //팝업창 있으면 선곡 불가

        int prevIndex = _currentSongIndex;
        int prevDiff = _currentDifficultyIndex;

        // 위아래 키 선곡 로직
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            // 위로 이동: 0보다 작아지면 끝으로 보냄
            _currentSongIndex = (_currentSongIndex - 1 + _entryList.Count) % _entryList.Count;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            // 아래로 이동: 끝을 넘어가면 0으로 보냄
            _currentSongIndex = (_currentSongIndex + 1) % _entryList.Count;
        }

        if (prevIndex != _currentSongIndex)
        {
            UpdateSelection();
            PlayPreview();
            Debug.Log($"현재 선택된 곡: {_allSongs[_currentSongIndex].SongTitle}");
        }

        // 좌우 키 난이도 로직
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            _currentDifficultyIndex = (_currentDifficultyIndex - 1 + 2) % 2;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            _currentDifficultyIndex = (_currentDifficultyIndex + 1) % 2;
        }
        if (prevDiff != _currentDifficultyIndex)
        {
            _infoView.HighlightDifficulty(_currentDifficultyIndex);
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            StartGame();
        }
    }

    IEnumerator PlayPreviewCo(SongMetaData targetSong)
    {
        yield return new WaitForSeconds(PreviewDelay);

        AudioClip clip = Resources.Load<AudioClip>($"Sounds/{targetSong.AudioFileName}");
        if (clip == null)
        {
            Debug.LogWarning($"[Preview] 음원을 찾을 수 없습니다: {targetSong.AudioFileName}");
            yield break;
        }
        _audioSource.clip = clip;

        while (true)
        {
            _audioSource.Play();
            _audioSource.time = targetSong.PreviewStartTime;
            _audioSource.volume = _baseVolume;

            double startTime = AudioSettings.dspTime;

            //15초 반복 루프
            while (AudioSettings.dspTime < startTime + LoopDuration)
            {
                double elapsedTime = AudioSettings.dspTime - startTime;
                if (elapsedTime >= LoopDuration - FadeDuration)
                {
                    float t = (float)(elapsedTime - (LoopDuration - FadeDuration)) / FadeDuration;
                    _audioSource.volume = Mathf.Lerp(_baseVolume, 0f, t);
                }
                yield return null; //매 프레임 체크
                //15초 경과 후 while문의 처음으로 회귀
            }
        }
    }

    public void PlayPreview()
    {
        if (_previewCoroutine != null) StopCoroutine(_previewCoroutine);
        _audioSource.Stop();
        _audioSource.clip = null; //이전 곡을 해제

        SongMetaData selectedSong = _allSongs[_currentSongIndex];
        _previewCoroutine = StartCoroutine(PlayPreviewCo(selectedSong));
    }

    void StartGame()
    {
        SetInputLock(true); //중복실행 방지

        //곡명과 난이도 데이터 전달
        SongMetaData selectedSong = _allSongs[_currentSongIndex];
        GlobalDataManager.Instance.PrepareGamePlay(selectedSong, _currentDifficultyIndex);

        //오디오 및 코루틴 정리
        if (_previewCoroutine != null) StopCoroutine(_previewCoroutine);
        _audioSource.Stop();
        _audioSource.clip = null;

        SceneManager.LoadScene("2-GamePlay");
    }
}

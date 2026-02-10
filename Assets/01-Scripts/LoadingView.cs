using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//로딩 씬을 따로 두지 않고 게임 씬 위에서 로딩을 처리
public class LoadingView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image _jacketImage;
    [SerializeField] private TextMeshProUGUI _songTitle;
    [SerializeField] private TextMeshProUGUI _selectedDiff;
    [SerializeField] private Slider _loadingBar;
    [SerializeField] private CanvasGroup _canvasGroup;
    
    [Header("매니저들")]
    [SerializeField] private NoteSpawner _noteSpawner;
    [SerializeField] private SongDataLoader _dataLoader;

    private float _currentProgress = 0f;

    private void Start()
    {
        InitializeLoadingUI();
        StartCoroutine(StartLoadingCo());
    }

    //게임 씬 진입 시 GlobalDataManager에서 정보를 가져와 초기화
    void InitializeLoadingUI()
    {
        // [보완] 인스펙터 연결 대신, 씬에 살아남은 진짜 인스턴스를 찾습니다.
        if (GlobalDataManager.Instance == null)
        {
            Debug.LogError("비상! 씬 전환 후 GlobalDataManager가 증발했습니다!");
            return;
        }
        // [보완] DataLoader도 살아남은 매니저로부터 가져옵니다.
        _dataLoader = GlobalDataManager.Instance.GetComponent<SongDataLoader>();

        var meta = GlobalDataManager.Instance.SelectedSong;
        if (meta == null) return;

        _songTitle.text = meta.SongTitle;
        _jacketImage.sprite = Resources.Load<Sprite>($"Jackets/{meta.JacketImage}");
        //나중에 난이도 텍스트를 표기하는 로직 추가할 것
        _loadingBar.value = 0f;
        _canvasGroup.alpha = 1f;
    }

    //NotePoolManager에서 호출
    public void SetPoolProgress(float ratio)
    {
        //오브젝트 풀링에 80%, 나머지에 20%를 로딩 바에 할당
        _loadingBar.value = 0.2f + (ratio * 0.8f);
    }

    IEnumerator StartLoadingCo()
    {
        var meta = GlobalDataManager.Instance.SelectedSong;
        int diffIndex = GlobalDataManager.Instance.SelectedDifficultyIndex;

        //JSON 파일 로드(10%)
        string chartFileName = meta.DifficultyList[diffIndex].ChartFileName;
        SongChartData chart = _dataLoader.LoadChartData(chartFileName);
        _loadingBar.value = 0.1f;
        yield return null;

        //노트 시간 계산(10%)
        _dataLoader.InitializeChartTimes(meta, chart);
        GlobalDataManager.Instance.SetCurrentChart(chart);
        _loadingBar.value = 0.2f;
        yield return null;

        //음원 로드(10%)
        AudioClip musicClip = Resources.Load<AudioClip>($"Sounds/{meta.AudioFileName}");
        if (musicClip == null)
        {
            Debug.LogError($"[Loading] 음원을 찾을 수 없습니다: Sounds/{meta.AudioFileName}");
            yield break;
        }
        _loadingBar.value = 0.3f;
        yield return null;

        //오브젝트 풀링(70%)
        /// [summary] 
        /// NotePoolManager.cs: Action<float> progressCallback이 변수에 있음
        /// Action<>: 내장 델리게이트(메서드에 대한 참조를 저장)
        /// 메서드 A 실행 시, 콜백 함수인 메서드 B를 델리게이트로 전달해
        /// 메서드 A->B 순으로 실행해줌.
        /// 클래스 간 결합도를 낮추고 코드 재사용성을 높이며,
        /// Action<>은 이를 간편화한 것.
        /// [/summary]
        NotePoolManager.Instance.PreparePool(chart, (ratio) =>
        {
            _loadingBar.value = 0.2f + (ratio * 0.8f);
        });
       
        _loadingBar.value = 1f; //이 시점에서 로딩 완료
        yield return new WaitForSeconds(0.1f);
        StartCoroutine(FadeOutCo(chart, musicClip));
    }

    //로딩 완료 시 실행할 페이드아웃 코루틴
    public IEnumerator FadeOutCo(SongChartData chart, AudioClip clip)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null; //다음 프레임까지 대기
        }
        _canvasGroup.alpha = 0f;
        gameObject.SetActive(false);

        _noteSpawner.StartGame(chart, clip);
    }
}

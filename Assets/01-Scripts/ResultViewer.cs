using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

//결과창 씬에서 정보들을 보여줌
public class ResultViewer : MonoBehaviour
{
    [Header("곡명, 난이도")]
    [SerializeField] private TextMeshProUGUI _songTitleText;
    [SerializeField] private TextMeshProUGUI _difficultyText;

    [Header("점수")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _accuracyText;
    [SerializeField] private TextMeshProUGUI _perfectCountText;
    [SerializeField] private TextMeshProUGUI _goodCountText;
    [SerializeField] private TextMeshProUGUI _okCountText;
    [SerializeField] private TextMeshProUGUI _missCountText;
    [SerializeField] private TextMeshProUGUI _maxComboText;

    [Header("랭크, 왕관")]
    [SerializeField] private TextMeshProUGUI _rankText;
    [SerializeField] private GameObject _crownFullCombo;
    [SerializeField] private GameObject _crownPerfectPlay;

    [Header("브금")]
    [SerializeField] private AudioSource _resultBGM;

    private PlayResult _data;
    private bool _canEnter = false;

    void Start()
    {
        _data = GlobalDataManager.Instance.LastPlayResult;
        if (_data == null)
        {
            Debug.LogWarning("[Result] 전달된 데이터가 없어 테스트용 데이터를 사용합니다.");
            _data = new PlayResult("TEST_ID", "Test Song", 5, 500, 10, 5, 0, 515);
        }

        if (GlobalDataManager.Instance != null)
        {
            GlobalDataManager.Instance.FadeIn(1.5f);
        }
        
        InitializeUI();
        StartCoroutine(ResultSequenceCo());

        if (_resultBGM != null)
        {
            _resultBGM.loop = true;
            _resultBGM.volume = 0f;
            _resultBGM.Play();
            StartCoroutine(FadeInBGM(1.5f));
        }
    }

    void Update()
    {
        if (_canEnter && Input.GetKeyDown(KeyCode.Return))
        {
            ExitToSelectScene();
            SceneManager.LoadScene("1-SongSelect");
        }
    }

    void InitializeUI()
    {
        _songTitleText.text = _data.SongTitle;
        //난이도 인덱스를 문자화(0은 NM, 1은 HD)
        string diffName = GlobalDataManager.Instance.SelectedDifficultyIndex == 0 ? "NM" : "HD";
        _difficultyText.text = $"{diffName} - {_data.DifficultyLevel}";

        _scoreText.text = "0";
        _accuracyText.text = "(0.00%)";
        _perfectCountText.text = "0";
        _goodCountText.text = "0";
        _okCountText.text = "0";
        _missCountText.text = "0";
        _maxComboText.text = "0";
        _rankText.text = "";

        if (_crownFullCombo != null && _crownPerfectPlay != null)
        {
            _crownFullCombo.SetActive(false);
            _crownPerfectPlay.SetActive(false);
        }
    }

    IEnumerator ResultSequenceCo()
    {
        //1. '점수와 정확도' 출력
        yield return StartCoroutine(CountUpInt(_scoreText, 0, _data.Score, "N0", 1.0f));
        yield return StartCoroutine(CountUpFloat(_accuracyText, 0f, _data.Accuracy, 0.5f));

        //2. 각 판정 출력
        StartCoroutine(CountUpInt(_perfectCountText, 0, _data.PerfectCount, "N0", 0.5f));
        StartCoroutine(CountUpInt(_goodCountText, 0, _data.GoodCount, "N0", 0.5f));
        StartCoroutine(CountUpInt(_okCountText, 0, _data.OKCount, "N0", 0.5f));
        yield return StartCoroutine(CountUpInt(_missCountText, 0, _data.MissCount, "N0", 0.5f));

        //3. 최대 콤보 출력
        yield return StartCoroutine(CountUpInt(_maxComboText, 0, _data.MaxCombo, "N0", 0.5f));

        //4. 랭크 출력 및 데이터 저장
        ApplyRankVisual(_data.GetRank());
        SaveManager.Instance.SaveRecord(_data);

        //5. 왕관 씌우기
        if (_data.IsPerfectPlay)
        {
            if (_crownPerfectPlay != null) _crownPerfectPlay.SetActive(true);
        }
        else if (_data.IsFullCombo)
        {
            if (_crownFullCombo != null) _crownFullCombo.SetActive(true);
        }

        _canEnter = true;
    }

    void ApplyRankVisual(Rank rank)
    {
        _rankText.text = rank.ToString();

        //선택: 랭크별로 텍스트에 색상 부여
        /// [summary] 
        /// _rankText.color = rank switch
        /// {
        /// Rank.S => new Color32(255, 215, 0, 255),
        /// Rank.A => new Color32(30, 144, 255, 255), ...
        /// _ => Color.white
        /// };
        /// [/summary]

        //랭크 확대 후 원래대로 돌아오는 연출
        _rankText.transform.localScale = Vector3.one * 1.5f;
        StartCoroutine(PulseRankText());
    }
    private IEnumerator PulseRankText()
    {
        float elapsed = 0f;
        Vector3 targetScale = Vector3.one;
        Vector3 startScale = _rankText.transform.localScale;

        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            _rankText.transform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / 0.2f);
            yield return null;
        }
    }

    //결과창의 정수값을 빠르게 올리는 코루틴
    private IEnumerator CountUpInt(TextMeshProUGUI text, int start, int end, string format, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            int current = (int)Mathf.Lerp(start, end, elapsed / duration);
            text.text = current.ToString(format);
            yield return null;
        }
        text.text = end.ToString(format);
    }
    //위 함수의 실수 버전
    private IEnumerator CountUpFloat(TextMeshProUGUI text, float start, float end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float current = Mathf.Lerp(start, end, elapsed / duration);
            text.text = $"({current:F2}%)";
            yield return null;
        }
        text.text = $"({end:F2}%)";
    }

    public void OnExitButtonClicked()
    {
        if (_canEnter) ExitToSelectScene();
    }

    void ExitToSelectScene()
    {
        _canEnter = false;

        StartCoroutine(FadeOutBGM(1.5f));
        GlobalDataManager.Instance.FadeOut(1.5f, () => {
            SceneManager.LoadScene("1-SongSelect");
        });
    }
    IEnumerator FadeInBGM(float duration)
    {
        float targetVolume = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _resultBGM.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
            yield return null;
        }
        _resultBGM.volume = targetVolume;
    }
    IEnumerator FadeOutBGM(float duration)
    {
        float startVolume = _resultBGM.volume;
        float elapsed = 0f;

        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _resultBGM.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }
        _resultBGM.Stop();
        _resultBGM.loop = false;
    }
}

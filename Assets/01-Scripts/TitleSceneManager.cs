using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TitleSceneManager : MonoBehaviour
{
    [Header("브금")]
    [SerializeField] private AudioSource _titleBGM;

    private bool _isStarting = false;
    private bool _canInput = false;

    void Start()
    {
        if (GlobalDataManager.Instance != null)
        {
            GlobalDataManager.Instance.FadeIn(1.5f, () => {
                _canInput = true;
                Debug.Log("[Title] 이제 아무 키나 누르세요!");
            });
        }
        if (_titleBGM != null)
        {
            _titleBGM.loop = true;
            _titleBGM.volume = 0f;
            _titleBGM.Play();
            StartCoroutine(FadeInTitleBGM(1.5f));
        }
    }
    void Update()
    {
        if (!_canInput || _isStarting) return;

        if (Input.anyKeyDown) GoToSongSelect();
    }

    void GoToSongSelect()
    {
        _isStarting = true;
        _canInput = false;

        if (GlobalDataManager.Instance != null)
        {
            StartCoroutine(FadeOutTitleBGM(1.5f));
            GlobalDataManager.Instance.FadeOut(1.5f, () => {
                SceneManager.LoadScene("1-SongSelect");
            });
        }
        else
        {
            // 만약 GlobalDataManager가 없는 상황을 대비한 예외 처리
            SceneManager.LoadScene("1-SongSelect");
        }
    }

    IEnumerator FadeInTitleBGM(float duration)
    {
        float targetVolume = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _titleBGM.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
            yield return null;
        }
        _titleBGM.volume = targetVolume;
    }
    IEnumerator FadeOutTitleBGM(float duration)
    {
        float startVolume = _titleBGM.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _titleBGM.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }
        _titleBGM.Stop();
        _titleBGM.loop = false;
    }
}

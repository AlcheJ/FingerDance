using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject _pausePopup;
    [SerializeField] private TextMeshProUGUI _readyText;
    [SerializeField] private NoteSpawner _spawner;


    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if (!_spawner.IsPaused) RequestPause();
        }
    }

    public void RequestPause() //Esc or 버튼 눌렀을 때
    {
        _pausePopup.SetActive(true);
        _spawner.PauseGame();
    }
    public void OnResumeClicked() //Resume 버튼 눌렀을 때
    {
        StartCoroutine(ResumeSequenceCo());
    }
    IEnumerator ResumeSequenceCo()
    {
        _pausePopup.SetActive(false);

        if (_readyText != null)
        {
            _readyText.gameObject.SetActive(true);
            _readyText.text = "READY...";
            _readyText.transform.localScale = Vector3.one;

            float duration = 2.0f;
            float elapsed = 0f;
            Vector3 startScale = Vector3.one;
            Vector3 targetScale = Vector3.one * 0.8f;

            while(elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float ratio = elapsed / duration;

                _readyText.transform.localScale = Vector3.Lerp(startScale, targetScale, ratio);
                yield return null;
            }
            _readyText.gameObject.SetActive(false);
        }
        _spawner.ResumeGame(); //음악 및 시간 보정
    }

    public void OnSelectSongClicked()
    {
        GlobalDataManager.Instance.FadeOut(1.0f, () => {
            SceneManager.LoadScene("1-SongSelect");
        });
    }

    public void OnRestartButtonClicked()
    {
        _pausePopup.SetActive(false);
        _spawner.StopGame();

        GlobalDataManager.Instance.IsRestarting = true;

        GlobalDataManager.Instance.FadeOut(1.0f, () => {
            string currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentSceneName);
        });
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}

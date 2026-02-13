using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//키 입력에 따른 레인 효과 담당
public class InputFeedbackManager : MonoBehaviour
{
    [SerializeField] private GameObject[] _laneFlares;
    [SerializeField] private AudioSource _sfxSource;
    [SerializeField] private AudioClip _hitSound;

    private Coroutine[] _fadeoutCo = new Coroutine[4];

    //키를 누르면 호출
    public void StartFeedback(int laneIndex)
    {
        if (_fadeoutCo[laneIndex] != null)
        {
            StopCoroutine(_fadeoutCo[laneIndex]);
            _fadeoutCo[laneIndex] = null;
        }
        if (laneIndex < 0 || laneIndex >= _laneFlares.Length) return;
        if (_laneFlares[laneIndex] == null)
        {
            Debug.LogWarning($"[InputFeedback] {laneIndex}번 레인의 Flare 오브젝트가 할당되지 않았습니다.");
            return;
        }

        _laneFlares[laneIndex].SetActive(true);
        _sfxSource.PlayOneShot(_hitSound);
    }
    //키를 떼면 호출
    public void StopFeedback(int laneIndex)
    {
        if (_fadeoutCo[laneIndex] == null)
        {
            _fadeoutCo[laneIndex] = StartCoroutine(FadeOutFlareCo(laneIndex));
        }
    }
    //빛 효과의 빠른 페이드아웃
    IEnumerator FadeOutFlareCo(int laneIndex)
    {
        yield return new WaitForSeconds(0.05f);
        _laneFlares[laneIndex].SetActive(false);
        _fadeoutCo[laneIndex] = null;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

//마디를 구분하는 박자선을 담당함
public class BarLineObject : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _sr; //색상 변경용
    [SerializeField] private TextMeshPro _barNumberText; //n번째 마디

    public float _targetTime;
    public float _currentJudgmentY;
    public bool _isActivated;

    //노트와 같은 원리로 호출 및 초기화
    public void InitializeBarLine(float targetTime, float judgmentY, float spawnY, bool isMajor, int barNumber = -1)
    {
        _targetTime = targetTime;
        _currentJudgmentY = judgmentY;
        _isActivated = true;

        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        else if (_sr != null)
        {
            _sr.color = isMajor ? Color.white : new Color32(149, 149, 149, 255);
            transform.localScale = isMajor ? new Vector3(6f, 0.07f, 1f) : new Vector3(6f, 0.06f, 1f);
        }

        transform.localPosition = new Vector3(0, spawnY, 0);
        gameObject.SetActive(true);

        if (_barNumberText != null)
        {
            if (isMajor && barNumber >= 0)
            {
                _barNumberText.text = barNumber.ToString();
                _barNumberText.gameObject.SetActive(true);
            }
            else
            {
                _barNumberText.gameObject.SetActive(false);
            }
        }
    }

    public void UpdateBarLine(float currentTime, float noteSpeed)
    {
        if (!_isActivated) return;

        float distance = (_targetTime - currentTime) * noteSpeed;
        transform.localPosition = new Vector3(0f, distance + _currentJudgmentY, 0f);

        //마디선을 풀에 반납하는 기능이 에디터에서 독이 됨
        //if (currentTime > _targetTime + 0.5f) DeactivateBar();
    }

    private void DeactivateBar()
    {
        _isActivated = false;
        gameObject.SetActive(false);
    }
}

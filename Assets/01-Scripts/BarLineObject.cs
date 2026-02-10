using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//마디를 구분하는 박자선을 담당함
public class BarLineObject : MonoBehaviour
{
    public float _targetTime;
    public float _currentJudgmentY;
    public bool _isActivated;

    //노트와 같은 원리로 호출 및 초기화
    public void InitializeBarLine(float targetTime, float judgmentY, float spawnY)
    {
        _targetTime = targetTime;
        _currentJudgmentY = judgmentY;
        _isActivated = true;
        transform.localPosition = new Vector3(0, spawnY, 0);
        gameObject.SetActive(true);
    }

    public void UpdateBarLine(float currentTime, float noteSpeed)
    {
        if (!_isActivated) return;

        float distance = (_targetTime - currentTime) * noteSpeed;
        transform.localPosition = new Vector3(0f, distance + _currentJudgmentY, 0f);

        //마디선을 풀에 반납
        if (currentTime > _targetTime + 0.5f) DeactivateBar();
    }

    private void DeactivateBar()
    {
        _isActivated = false;
        gameObject.SetActive(false);
    }
}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class JudgmentEffectManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _judgmentText;
    [SerializeField] private TextMeshProUGUI _comboText;
    [SerializeField] private JudgmentManager _judgManager;

    private int _currentCombo = 0;
    private Coroutine _fadeCoroutine;

    void Start()
    {
        //판정이 날 때마다 함수 실행
        _judgManager.OnJudged += ShowEffect;
        _judgmentText.text = "";
        _comboText.text = "";
    }

    public void ShowEffect(JudgType type, int lane)
    {
        if(type == JudgType.Miss)
        {
            _currentCombo = 0;
            _comboText.text = "";
        }
        else
        {
            _currentCombo++;
            _comboText.text = _currentCombo.ToString();
        }

        _judgmentText.gameObject.SetActive(true);
        _judgmentText.text = type.ToString();
        _judgmentText.color = GetColorByJudg(type);

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
        _fadeCoroutine = StartCoroutine(FadeJudgCo());
    }

    Color GetColorByJudg(JudgType type)
    {
        return type switch
        {
            //Color: RGB 0~1 값으로 입력,
            //Color32: RGB 0~255 값으로 입력
            JudgType.Perfect => new Color32(253, 255, 146, 255),
            JudgType.Good => new Color32(212, 251, 255, 255),
            JudgType.OK => new Color32(143, 208, 215, 255),
            JudgType.Miss => new Color32(150, 150, 150, 255),
            _ => Color.white
        };
    }

    private IEnumerator FadeJudgCo()
    {
        yield return new WaitForSeconds(0.5f);
        _judgmentText.gameObject.SetActive(false);
        _fadeCoroutine = null;
    }
}

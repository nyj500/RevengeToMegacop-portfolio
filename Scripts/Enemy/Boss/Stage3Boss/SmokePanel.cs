using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SmokePanel : MonoBehaviour
{
    [SerializeField] private Image _smokePanel;
    [SerializeField] private float _fadeSpeed = 2f;
    [SerializeField] private float _smokeOpacity = 1f;

    private Coroutine _currentCoroutine;
    private CoroutineType _currentType;

    private enum CoroutineType
    {
        None,
        Show,
        Hide
    }

    public void Show()
    {
        StartSafeCoroutine(SmokePanelActive(), CoroutineType.Show);
    }

    public void Hide()
    {
        StartSafeCoroutine(SmokePanelDisActive(), CoroutineType.Hide);
    }

    void StartSafeCoroutine(IEnumerator coroutine, CoroutineType type)
    {
        //  같은 코루틴이면 무시
        if (_currentCoroutine != null && _currentType == type)
            return;

        //  다른 코루틴이면 중지
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
        }

        _currentType = type;
        _currentCoroutine = StartCoroutine(coroutine);
    }

    IEnumerator SmokePanelActive()
    {
        Color color = _smokePanel.color;

        while (color.a < _smokeOpacity)
        {
            color.a += Time.deltaTime * _fadeSpeed;
            _smokePanel.color = color;

            yield return null;
        }

        color.a = _smokeOpacity;
        _smokePanel.color = color;

        _currentCoroutine = null;
        _currentType = CoroutineType.None;
    }

    IEnumerator SmokePanelDisActive()
    {
        Color color = _smokePanel.color;

        while (color.a > 0f)
        {
            color.a -= Time.deltaTime * _fadeSpeed;
            _smokePanel.color = color;

            yield return null;
        }

        color.a = 0f;
        _smokePanel.color = color;

        _currentCoroutine = null;
        _currentType = CoroutineType.None;
    }
}
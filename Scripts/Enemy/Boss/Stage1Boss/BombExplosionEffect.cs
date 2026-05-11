using System.Collections;
using UnityEngine;

public class BombExplosionEffect : MonoBehaviour
{
    [SerializeField] private float duration = 0.5f;

    public void Play(float radius)
    {
        gameObject.SetActive(true);
        StartCoroutine(Animate(radius));
    }

    private IEnumerator Animate(float radius)
    {
        float elapsed = 0f;
        float diameter = radius * 2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.one * Mathf.Lerp(0f, diameter, t);
            yield return null;
        }
        Destroy(gameObject);
    }
}

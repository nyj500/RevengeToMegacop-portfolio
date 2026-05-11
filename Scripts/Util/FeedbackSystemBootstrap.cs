using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Feel 피드백 시스템이 요구하는 씬 인프라를 자동으로 생성하는 부트스트랩.
/// 시간 관련 피드백에 필요한 MMTimeManager와 후처리 피드백에 필요한 Shaker들을 보장한다.
/// 씬에 빈 GameObject에 부착하거나, 기존 매니저 오브젝트에 추가한다.
/// </summary>
public class FeedbackSystemBootstrap : MonoBehaviour
{
    void Awake()
    {
        EnsureMMTimeManager();
        EnsureVolumeShakers();
    }

    private void EnsureMMTimeManager()
    {
        MMTimeManager existing = FindAnyObjectByType<MMTimeManager>();
        if (existing != null) return;

        GameObject timeManagerObject = new GameObject("MMTimeManager");
        timeManagerObject.AddComponent<MMTimeManager>();
    }

    private void EnsureVolumeShakers()
    {
        Volume volume = FindAnyObjectByType<Volume>();
        if (volume == null) return;

        // ChromaticAberration override가 없으면 추가 (초기값 0)
        if (!volume.profile.TryGet(out ChromaticAberration chromaticAberration))
        {
            chromaticAberration = volume.profile.Add<ChromaticAberration>();
            chromaticAberration.intensity.Override(0f);
        }

        // Chromatic Aberration Shaker
        if (volume.gameObject.GetComponent<MMChromaticAberrationShaker_URP>() == null)
        {
            volume.gameObject.AddComponent<MMChromaticAberrationShaker_URP>();
        }
    }
}

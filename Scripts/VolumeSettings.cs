using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// PlayerPrefs와 AudioMixer 사이의 볼륨 저장/불러오기/적용을 담당하는 정적 유틸.
/// 선형(0~1) ↔ 데시벨 변환을 처리하며, 첫 실행 시 기본값 1.0을 반환한다.
/// </summary>
public static class VolumeSettings
{
    private const float SilenceDecibels = -80f;

    /// <summary>
    /// 선형 볼륨 값을 데시벨로 변환한 뒤 AudioMixer에 설정하고 PlayerPrefs에 저장한다.
    /// </summary>
    /// <param name="mixer">설정할 AudioMixer</param>
    /// <param name="parameterName">AudioMixer에 노출된 파라미터 이름 (예: "MasterVolume")</param>
    /// <param name="linearValue">선형 볼륨 값 [0, 1]</param>
    public static void SetVolume(AudioMixer mixer, string parameterName, float linearValue)
    {
        float decibels = LinearToDecibels(linearValue);
        mixer.SetFloat(parameterName, decibels);
        PlayerPrefs.SetFloat(parameterName, linearValue);
    }

    /// <summary>
    /// PlayerPrefs에서 저장된 선형 볼륨 값을 불러온다. 저장된 값이 없으면 1.0을 반환한다.
    /// </summary>
    /// <param name="parameterName">AudioMixer에 노출된 파라미터 이름</param>
    /// <returns>선형 볼륨 값 [0, 1]</returns>
    public static float GetVolume(string parameterName)
    {
        return PlayerPrefs.GetFloat(parameterName, 1.0f);
    }

    /// <summary>
    /// 저장된 모든 볼륨 설정을 AudioMixer에 일괄 적용한다. 씬 로드 시 호출한다.
    /// </summary>
    /// <param name="mixer">적용할 AudioMixer</param>
    public static void LoadAndApply(AudioMixer mixer)
    {
        mixer.SetFloat("MasterVolume", LinearToDecibels(GetVolume("MasterVolume")));
        mixer.SetFloat("BGMVolume", LinearToDecibels(GetVolume("BGMVolume")));
        mixer.SetFloat("SFXVolume", LinearToDecibels(GetVolume("SFXVolume")));
    }

    private static float LinearToDecibels(float linearValue)
    {
        if (linearValue <= 0f)
            return SilenceDecibels;

        float decibels = Mathf.Log10(Mathf.Max(linearValue, 0.0001f)) * 20f;
        return Mathf.Max(decibels, SilenceDecibels);
    }
}

using UnityEngine;

/// <summary>
/// 스테이지 선택 허브에서 관리하는 스테이지 하나의 메타데이터와 런타임 캐시.
/// StageSelectController 의 stages 배열 원소로 Inspector 에서 설정한다.
/// </summary>
[System.Serializable]
public class StageEntry
{
    /// <summary>스테이지 선택 화면에 표시할 이름.</summary>
    public string displayName;

    /// <summary>스테이지 선택 화면에 표시할 설명.</summary>
    [TextArea] public string description;

    /// <summary>Additive 로드할 씬 이름. Build Settings 에 등록되어 있어야 한다.</summary>
    public string sceneName;

    /// <summary>[런타임] 로드된 씬의 StageRoot GameObject 캐시.</summary>
    [System.NonSerialized] public GameObject stageRoot;

    /// <summary>[런타임] StageRoot 내 PlayerSpawn Transform 캐시.</summary>
    [System.NonSerialized] public Transform playerSpawn;
}

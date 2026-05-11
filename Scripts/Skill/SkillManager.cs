using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;

/// <summary>
/// 스킬 해금 상태를 런타임에 관리하는 싱글톤 매니저.
/// 씬에 별도 GameObject로 배치한다.
/// </summary>
public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    [SerializeField] private SkillDatabase database;

    /// <summary>
    /// 게임 시작 시 기본으로 해금할 스킬 목록.
    /// 개발 중에는 모든 스킬을 넣어두고, 스킬트리 UI 완성 후 비운다.
    /// </summary>
    [SerializeField] private SkillId[] initiallyUnlockedSkills;

    private readonly HashSet<SkillId> unlockedSkills = new();

    /// <summary>스킬이 해금될 때 발행된다. 스킬트리 UI 등에서 구독한다.</summary>
    public event Action<SkillId> OnSkillUnlocked;

    private string SaveFilePath => Path.Combine(Application.persistentDataPath, "skills.json");

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 세이브 파일이 있으면 디스크에서 복원, 없으면 초기 해금 목록 적용
        if (File.Exists(SaveFilePath))
        {
            LoadFromDisk();
        }
        else
        {
            foreach (SkillId id in initiallyUnlockedSkills)
            {
                if (id != SkillId.None)
                    unlockedSkills.Add(id);
            }
        }
    }

    /// <summary>
    /// 해당 스킬이 해금되어 있는지 확인한다.
    /// </summary>
    public bool IsUnlocked(SkillId id) => unlockedSkills.Contains(id);

    /// <summary>
    /// 스킬 해금을 시도한다. 선행 조건이 모두 충족되면 해금하고 true를 반환한다.
    /// 이미 해금됐거나 선행 조건 미충족 시 false를 반환한다.
    /// </summary>
    public bool TryUnlock(SkillId id)
    {
        if (id == SkillId.None) return false;
        if (unlockedSkills.Contains(id)) return false;

        if (database != null)
        {
            SkillData data = database.GetSkill(id);
            if (data != null)
            {
                foreach (var prerequisite in data.Prerequisites)
                {
                    if (!unlockedSkills.Contains(prerequisite.SkillId))
                        return false;
                }
            }
        }

        unlockedSkills.Add(id);
        OnSkillUnlocked?.Invoke(id);
        SaveToDisk();
        return true;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnApplicationQuit()
    {
        SaveToDisk();
    }

    private void SaveToDisk()
    {
        string json = JsonUtility.ToJson(ToSaveData());
        File.WriteAllText(SaveFilePath, json);
    }

    private void LoadFromDisk()
    {
        try
        {
            string json = File.ReadAllText(SaveFilePath);
            LoadSaveData(JsonUtility.FromJson<SkillSaveData>(json));
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"SkillManager: 세이브 파일 로드 실패, 초기 해금 목록으로 대체합니다. ({exception.Message})");
            foreach (SkillId id in initiallyUnlockedSkills)
            {
                if (id != SkillId.None)
                    unlockedSkills.Add(id);
            }
        }
    }

    /// <summary>현재 해금 상태를 세이브 데이터로 변환한다.</summary>
    public SkillSaveData ToSaveData() => new() { unlockedIds = unlockedSkills.ToArray() };

    /// <summary>세이브 데이터를 불러와 해금 상태를 복원한다.</summary>
    public void LoadSaveData(SkillSaveData data)
    {
        unlockedSkills.Clear();
        if (data.unlockedIds == null) return;
        foreach (var id in data.unlockedIds)
        {
            if (id != SkillId.None)
                unlockedSkills.Add(id);
        }
    }
}


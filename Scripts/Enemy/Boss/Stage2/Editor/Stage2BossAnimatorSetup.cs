#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Stage2Boss 전용 Animator Controller를 생성하는 에디터 유틸리티.
/// Tools > Stage2Boss > Create Animator Controller 로 실행한다.
/// 생성 위치: Assets/_Project/Scripts/Enemy/Boss/Stage2/Stage2BossAnimCtrl.controller
/// </summary>
public static class Stage2BossAnimatorSetup
{
    private const string ControllerPath =
        "Assets/_Project/Scripts/Enemy/Boss/Stage2/Stage2BossAnimCtrl.controller";
    private const string AnimPath =
        "Assets/_Project/Resource/Archer_LowPoly/Animations/";

    [MenuItem("Tools/Stage2Boss/Create Animator Controller")]
    public static void CreateController()
    {
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;

        var idle   = AddState(sm, "Idle",   "Idle.anim");
        var attack = AddState(sm, "Attack", "Attack1.anim");
        var hit    = AddState(sm, "Hit",    "Damage1.anim");
        var die    = AddState(sm, "Die",    "Die1.anim");

        sm.defaultState = idle;

        AddAnyTransition(sm, attack, "Attack");
        AddAnyTransition(sm, hit,    "Hit");
        AddAnyTransition(sm, die,    "Die");

        AddExitTransition(attack, idle);
        AddExitTransition(hit,    idle);
        // Die는 복귀 없음

        AssetDatabase.SaveAssets();
        Selection.activeObject = controller;
        Debug.Log("[Stage2Boss] Animator Controller 생성 완료: " + ControllerPath);
    }

    private static AnimatorState AddState(AnimatorStateMachine sm, string stateName, string clipFile)
    {
        var state = sm.AddState(stateName);
        state.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimPath + clipFile);
        return state;
    }

    private static void AddAnyTransition(AnimatorStateMachine sm, AnimatorState target, string trigger)
    {
        var t = sm.AddAnyStateTransition(target);
        t.AddCondition(AnimatorConditionMode.If, 0, trigger);
        t.hasExitTime = false;
        t.duration = 0.1f;
        t.canTransitionToSelf = false;
    }

    private static void AddExitTransition(AnimatorState from, AnimatorState to)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = true;
        t.exitTime = 1f;
        t.duration = 0.15f;
        t.hasFixedDuration = false;
    }
}
#endif

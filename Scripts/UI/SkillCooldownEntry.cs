using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SkillToggle 프리팹 인스턴스 1개를 스킬 쿨타임 슬롯으로 변환한다.
/// SkillCooldownActionBar가 런타임에 Instantiate 후 AddComponent.
/// </summary>
public class SkillCooldownEntry : MonoBehaviour
{
    private const float IconSize = 80f;
    private const float OverlayAlpha = 0.55f;
    private const float FillEpsilon = 0.001f;

    private static readonly Vector2 IconSizeDelta = new(IconSize, IconSize);
    private static readonly Color OverlayColor = new(0f, 0f, 0f, OverlayAlpha);

    private IPlayerCooldownSource source;
    private Image cooldownOverlay;
    private float lastFill = -1f;

    /// <summary>
    /// 1회만 호출. SkillToggle 인스턴스의 자식 구조를 변형하고 데이터/소스를 바인딩한다.
    /// </summary>
    public void Bind(SkillData data, IPlayerCooldownSource cooldownSource, ToggleGroup group)
    {
        source = cooldownSource;

        // 1. Toggle은 디스플레이 전용 — 클릭 무반응, 색 변화 없음
        if (TryGetComponent<Toggle>(out var toggle))
        {
            toggle.transition = Selectable.Transition.None;
            toggle.interactable = false;
            toggle.group = group;
            toggle.isOn = false;
        }

        // 2. 자식 구조 변형: Background만 살리고 나머지(Locked 자물쇠 포함)는 비활성화.
        //    쿨타임 HUD는 해금 여부와 무관하게 진행도만 표시하는 역할이므로
        //    SkillToggle 프리팹의 Locked 오버레이는 아이콘과 겹쳐 노이즈가 된다.
        Transform backgroundTransform = null;
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (child.name == "Background")
            {
                backgroundTransform = child;
            }
            else
            {
                // Connector, Locked, Tier/Usage 텍스트 등 — 슬롯 표시에 무관
                child.gameObject.SetActive(false);
            }
        }

        if (backgroundTransform == null)
        {
            Debug.LogError("SkillCooldownEntry: Background child not found in SkillToggle prefab.", this);
            return;
        }

        // 3. 아이콘 추가 — Background 자식
        if (data != null && data.Icon != null)
        {
            CreateChildImage(backgroundTransform, "Icon", data.Icon, IconSizeDelta, Color.white,
                stretch: false, filled: false);
        }

        // 4. 쿨타임 오버레이 추가 — Background 자식, stretch all, Radial360
        cooldownOverlay = CreateChildImage(backgroundTransform, "CooldownOverlay",
            sprite: GetBackgroundSprite(backgroundTransform),
            sizeDelta: Vector2.zero,
            color: OverlayColor,
            stretch: true,
            filled: true);

        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
            // Radial360 origin: Bottom=0, Right=1, Top=2, Left=3
            cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
            cooldownOverlay.fillClockwise = false;
            cooldownOverlay.fillAmount = 0f;
        }

        // 5. 쿨타임 시작 이벤트 구독 (UI는 LateUpdate 폴링으로 갱신)
        if (source != null) source.OnCooldownStarted += OnCooldownStarted;
    }

    void OnDestroy()
    {
        if (source != null) source.OnCooldownStarted -= OnCooldownStarted;
    }

    void LateUpdate()
    {
        if (source == null || cooldownOverlay == null) return;

        if (!source.IsOnCooldown)
        {
            // idle 시 zero work — 단, 마지막에 0으로 한 번 정리
            if (lastFill > FillEpsilon)
            {
                lastFill = 0f;
                cooldownOverlay.fillAmount = 0f;
            }
            return;
        }

        float fill = source.CooldownProgress01;
        if (Mathf.Abs(fill - lastFill) > FillEpsilon)
        {
            lastFill = fill;
            cooldownOverlay.fillAmount = fill;
        }
    }

    private void OnCooldownStarted()
    {
        // LateUpdate가 즉시 fill을 갱신하므로 여기서는 트리거만
        lastFill = -1f;
    }

    private static Sprite GetBackgroundSprite(Transform backgroundTransform)
    {
        var backgroundImage = backgroundTransform.GetComponent<Image>();
        return backgroundImage != null ? backgroundImage.sprite : null;
    }

    private static Image CreateChildImage(
        Transform parent,
        string name,
        Sprite sprite,
        Vector2 sizeDelta,
        Color color,
        bool stretch,
        bool filled)
    {
        var childObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        childObject.layer = parent.gameObject.layer;
        var rectTransform = (RectTransform)childObject.transform;
        rectTransform.SetParent(parent, false);

        if (stretch)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
        else
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = sizeDelta;
        }

        var image = childObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false; // [PF-01]
        if (filled) image.type = Image.Type.Filled;
        return image;
    }
}

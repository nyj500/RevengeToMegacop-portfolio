using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 처형 시 적 오브젝트를 평면 기준으로 두 조각으로 분할하고,
/// Rigidbody를 부여하여 물리적으로 날려보내는 연출 효과.
/// MeshFilter(플레이스홀더)와 SkinnedMeshRenderer(캐릭터 모델) 모두 지원한다.
/// </summary>
public class ExecutionSliceEffect : MonoBehaviour
{
    [Header("슬라이스 설정")]
    [SerializeField] private float separationForce = 8f;
    [SerializeField] private float upwardForce = 3f;
    [SerializeField] private float torqueForce = 5f;

    [Header("단면 머티리얼")]
    [SerializeField] private Material crossSectionMaterial;

    [Header("정리")]
    [SerializeField] private float destroyDelay = 3f;
    [SerializeField] private float fadeStartTime = 1.5f;

    [Header("URP 셰이더 (crossSectionMaterial 미지정 시 사용)")]
    [SerializeField] private Shader unlitShader;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private Material fallbackCrossSectionMaterial;

    /// <summary>
    /// 대상 오브젝트를 슬라이스한다. sliceNormal은 월드 좌표 기준 절단 방향.
    /// 호출 후 원본 오브젝트는 비활성화된다 (파괴는 호출자가 관리).
    /// </summary>
    public void Slice(GameObject target, Vector3 slicePosition, Vector3 sliceNormal)
    {
        if (target == null) return;

        // 자식 포함 모든 렌더러에서 슬라이스 가능한 메시 수집
        List<SliceSource> sources = CollectSliceSources(target);
        if (sources.Count == 0) return;

        // 원본 비활성화
        target.SetActive(false);

        foreach (SliceSource source in sources)
        {
            // 월드 평면을 메시의 로컬 좌표로 변환
            Vector3 localPosition = source.worldToLocal.MultiplyPoint3x4(slicePosition);
            Vector3 localNormal = source.worldToLocal.MultiplyVector(sliceNormal).normalized;
            Plane localPlane = new Plane(localNormal, localPosition);

            MeshSlicer.SlicedMesh slicedMesh = MeshSlicer.Slice(source.mesh, localPlane);
            if (source.isBakedMesh) Destroy(source.mesh);

            if (slicedMesh == null) continue;

            Material[] originalMaterials = source.materials;
            Material capMaterial = GetCrossSectionMaterial();

            // upper (평면 법선 방향)
            GameObject upperHalf = CreateSliceHalf(
                "SliceUpper", slicedMesh.upperMesh,
                source.worldPosition, source.worldRotation, source.worldScale,
                originalMaterials, capMaterial,
                sliceNormal * separationForce + Vector3.up * upwardForce
            );

            // lower (평면 법선 반대 방향)
            GameObject lowerHalf = CreateSliceHalf(
                "SliceLower", slicedMesh.lowerMesh,
                source.worldPosition, source.worldRotation, source.worldScale,
                originalMaterials, capMaterial,
                -sliceNormal * separationForce + Vector3.up * upwardForce
            );

            StartCoroutine(FadeAndDestroy(upperHalf));
            StartCoroutine(FadeAndDestroy(lowerHalf));
        }
    }

    private struct SliceSource
    {
        public Mesh mesh;
        public bool isBakedMesh;
        public Material[] materials;
        public Matrix4x4 worldToLocal;
        public Vector3 worldPosition;
        public Quaternion worldRotation;
        public Vector3 worldScale;
    }

    private List<SliceSource> CollectSliceSources(GameObject target)
    {
        List<SliceSource> sources = new List<SliceSource>(4);

        // SkinnedMeshRenderer 우선 탐색
        SkinnedMeshRenderer[] skinnedRenderers = target.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer skinnedRenderer in skinnedRenderers)
        {
            Mesh bakedMesh = new Mesh();
            skinnedRenderer.BakeMesh(bakedMesh);

            // BakeMesh(mesh)는 바인드 포즈 매트릭스가 lossyScale을 흡수하므로
            // 이미 월드 스케일로 베이크된 메시를 돌려준다 (SMR의 rotation만 미적용).
            // 따라서 plane 변환은 rotation-only 역변환을 사용하고,
            // 최종 half의 transform scale은 1(이미 베이크됨)로 둔다.
            Transform smrTransform = skinnedRenderer.transform;
            Matrix4x4 worldToBaked = Matrix4x4.TRS(smrTransform.position, smrTransform.rotation, Vector3.one).inverse;

            sources.Add(new SliceSource
            {
                mesh = bakedMesh,
                isBakedMesh = true,
                materials = skinnedRenderer.sharedMaterials,
                worldToLocal = worldToBaked,
                worldPosition = smrTransform.position,
                worldRotation = smrTransform.rotation,
                worldScale = Vector3.one
            });
        }

        // MeshFilter 탐색 (SkinnedMesh가 없는 파트)
        MeshFilter[] meshFilters = target.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh == null) continue;

            MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
            if (meshRenderer == null) continue;

            sources.Add(new SliceSource
            {
                mesh = meshFilter.sharedMesh,
                materials = meshRenderer.sharedMaterials,
                worldToLocal = meshFilter.transform.worldToLocalMatrix,
                worldPosition = meshFilter.transform.position,
                worldRotation = meshFilter.transform.rotation,
                worldScale = meshFilter.transform.lossyScale
            });
        }

        return sources;
    }

    private GameObject CreateSliceHalf(
        string halfName, Mesh mesh,
        Vector3 position, Quaternion rotation, Vector3 scale,
        Material[] originalMaterials, Material capMaterial,
        Vector3 force)
    {
        // SMR의 경우 source.worldScale = Vector3.one(이미 베이크됨), rotation = SMR 월드 회전.
        // MeshFilter의 경우 source.worldScale = lossyScale, rotation = transform.rotation.
        // 어느 경우든 half.transform에 위치/회전/스케일을 위임한다.
        GameObject half = new GameObject(halfName);
        half.layer = LayerMask.NameToLayer("SlicedMesh");
        half.transform.SetPositionAndRotation(position, rotation);
        half.transform.localScale = scale;

        MeshFilter meshFilter = half.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = half.AddComponent<MeshRenderer>();

        // submesh 0 = 원본 머티리얼, submesh 1 = 단면 캡
        Material[] sliceMaterials = new Material[2];
        sliceMaterials[0] = (originalMaterials != null && originalMaterials.Length > 0)
            ? originalMaterials[0]
            : capMaterial;
        sliceMaterials[1] = capMaterial;
        meshRenderer.materials = sliceMaterials;

        // BoxCollider center/size는 메시 로컬 좌표 기준. Transform 스케일이 자동 반영된다.
        BoxCollider boxCollider = half.AddComponent<BoxCollider>();
        boxCollider.center = mesh.bounds.center;
        boxCollider.size = mesh.bounds.size;

        // 콜라이더 하단이 지면 아래에서 시작하면 PhysX 이탈 방향이 불안정해져
        // Rigidbody가 지면을 뚫고 내려가는 문제가 발생한다.
        // Renderer.bounds는 Transform(회전/스케일) 적용 후의 월드 AABB라서 안전하다.
        const float groundClearance = 0.05f;
        float groundY = 0f;
        int groundLayerMask = 1 << LayerMask.NameToLayer("Ground");
        if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out RaycastHit groundHit, 20f, groundLayerMask))
            groundY = groundHit.point.y;

        float colliderBottomY = meshRenderer.bounds.min.y;
        float minBottom = groundY + groundClearance;
        if (colliderBottomY < minBottom)
        {
            Vector3 correctedPosition = half.transform.position;
            correctedPosition.y += minBottom - colliderBottomY;
            half.transform.position = correctedPosition;
        }

        Rigidbody rigidbody = half.AddComponent<Rigidbody>();
        rigidbody.mass = 1f;
        rigidbody.AddForce(force, ForceMode.Impulse);
        rigidbody.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.Impulse);

        return half;
    }

    private IEnumerator FadeAndDestroy(GameObject half)
    {
        if (half == null) yield break;

        MeshRenderer meshRenderer = half.GetComponent<MeshRenderer>();

        // fadeStartTime까지 대기
        yield return new WaitForSeconds(fadeStartTime);

        if (half == null) yield break;

        // 머티리얼 인스턴스 생성 (fade를 위해)
        Material[] instanceMaterials = meshRenderer.materials;
        EnableTransparency(instanceMaterials);

        float fadeDuration = destroyDelay - fadeStartTime;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            if (half == null) yield break;
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            foreach (Material material in instanceMaterials)
            {
                Color color = material.GetColor(BaseColorId);
                color.a = alpha;
                material.SetColor(BaseColorId, color);
            }

            yield return null;
        }

        if (half != null)
        {
            // 인스턴스 머티리얼 정리
            foreach (Material material in instanceMaterials)
            {
                Destroy(material);
            }
            Destroy(half);
        }
    }

    private void EnableTransparency(Material[] materials)
    {
        foreach (Material material in materials)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }

    private Material GetCrossSectionMaterial()
    {
        if (crossSectionMaterial != null) return crossSectionMaterial;

        if (fallbackCrossSectionMaterial != null) return fallbackCrossSectionMaterial;

        if (unlitShader == null)
        {
            Debug.LogWarning("ExecutionSliceEffect: unlitShader가 지정되지 않았습니다. Inspector에서 URP Unlit 셰이더를 할당해 주세요.");
            return null;
        }

        // 기본 단면 머티리얼 생성 (어두운 빨간색) — 최초 1회만 생성
        fallbackCrossSectionMaterial = new Material(unlitShader);
        fallbackCrossSectionMaterial.SetColor(BaseColorId, new Color(0.4f, 0.05f, 0.05f, 1f));
        return fallbackCrossSectionMaterial;
    }

    void OnDestroy()
    {
        if (fallbackCrossSectionMaterial != null) Destroy(fallbackCrossSectionMaterial);
    }
}

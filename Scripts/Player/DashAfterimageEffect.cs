using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMovementController))]
public class DashAfterimageEffect : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("Execution Dash")]
    [SerializeField] private float executionSpawnInterval = 0.02f;
    [SerializeField] private Color executionAfterimageColor = new Color(1f, 0.2f, 0.1f, 0.7f);

    [SerializeField] private Shader unlitShader;

    private PlayerMovementController movementController;

    private struct MeshEntry
    {
        public SkinnedMeshRenderer skinned;
        public Transform transform;
    }

    private struct PoolItem
    {
        public GameObject gameObject;
        public MeshFilter filter;
        public Mesh bakedMesh;
        public Material material;
    }

    private struct ActiveItem
    {
        public int poolIndex;
        public float elapsed;
        public Color startColor;
    }

    // Shader.PropertyToID로 캐싱하면 매 프레임 문자열 해시 연산을 피할 수 있다.
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private MeshEntry[] meshEntries;
    private float timer;

    private GameObject poolContainer;
    private PoolItem[] pool;
    private Queue<int> freeIndices;
    private List<ActiveItem> activeItems;

    void Awake()
    {
        movementController = GetComponent<PlayerMovementController>();

        SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

        meshEntries = new MeshEntry[skinnedRenderers.Length];
        for (int i = 0; i < skinnedRenderers.Length; i++)
        {
            meshEntries[i] = new MeshEntry
            {
                skinned = skinnedRenderers[i],
                transform = skinnedRenderers[i].transform
            };
        }

        InitPool();
    }

    private void InitPool()
    {
        int maxConcurrentSpawns = Mathf.CeilToInt(fadeDuration / executionSpawnInterval) + 1;
        int poolSize = maxConcurrentSpawns * meshEntries.Length;

        if (unlitShader == null)
        {
            Debug.LogError("DashAfterimageEffect: unlitShader is not assigned. Afterimage effect disabled.");
            enabled = false;
            return;
        }

        pool = new PoolItem[poolSize];
        freeIndices = new Queue<int>(poolSize);
        activeItems = new List<ActiveItem>(poolSize);

        poolContainer = new GameObject("AfterimagePool");

        for (int i = 0; i < poolSize; i++)
        {
            GameObject afterimageObject = new GameObject("Afterimage");
            afterimageObject.transform.SetParent(poolContainer.transform, false);
            afterimageObject.SetActive(false);

            MeshFilter meshFilter = afterimageObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = afterimageObject.AddComponent<MeshRenderer>();

            // URP에서 런타임으로 투명 머티리얼을 생성할 때는 셰이더 속성을 직접 설정해야 한다.
            // Inspector에서 Surface Type을 Transparent로 바꾸면 Unity가 내부적으로 아래 값들을 자동 설정하지만,
            // 코드로 생성하면 그 과정이 생략되므로 수동으로 동일하게 맞춰야 한다.
            Material material = new Material(unlitShader);

            // Surface Type: 0 = Opaque(불투명), 1 = Transparent(반투명)
            material.SetFloat("_Surface", 1f);

            // Blend Mode: 0 = Alpha, 1 = Premultiply, 2 = Additive, 3 = Multiply
            // Alpha 블렌딩은 픽셀 색상을 알파값에 따라 뒤 오브젝트와 혼합한다.
            material.SetFloat("_Blend", 0f);

            // GPU 블렌딩 공식: 최종색 = (SrcColor × SrcBlend) + (DstColor × DstBlend)
            // SrcAlpha / OneMinusSrcAlpha 조합이 일반적인 알파 블렌딩이다.
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

            // ZWrite: 깊이 버퍼 기록 여부. 투명 오브젝트는 0(끔)으로 설정해야
            // 뒤에 있는 오브젝트가 가려지지 않는다.
            material.SetInt("_ZWrite", 0);

            // URP 셰이더 내부 분기용 키워드. 이 키워드가 없으면 셰이더가
            // Transparent 경로로 컴파일되지 않아 투명도가 적용되지 않는다.
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            // RenderQueue: 투명 오브젝트는 불투명 오브젝트(2000)보다 나중에 그려야
            // 깊이 정렬이 올바르게 된다. Transparent = 3000.
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            meshRenderer.material = material;

            pool[i] = new PoolItem { gameObject = afterimageObject, filter = meshFilter, bakedMesh = new Mesh(), material = material };
            freeIndices.Enqueue(i);
        }
    }

    void Update()
    {
        if (pool == null) return;

        if (!movementController.IsExecutionDashing)
        {
            timer = 0f;
        }
        else
        {
            timer += Time.unscaledDeltaTime;
            if (timer >= executionSpawnInterval)
            {
                timer = 0f;
                SpawnAfterimage(executionAfterimageColor);
            }
        }

        UpdateActiveItems();
    }

    private void SpawnAfterimage(Color color)
    {
        foreach (MeshEntry entry in meshEntries)
        {
            if (freeIndices.Count == 0) continue;

            int idx = freeIndices.Dequeue();
            PoolItem item = pool[idx];

            entry.skinned.BakeMesh(item.bakedMesh);
            item.filter.sharedMesh = item.bakedMesh;

            item.gameObject.transform.SetPositionAndRotation(entry.transform.position, entry.transform.rotation);
            item.gameObject.transform.localScale = Vector3.one;
            item.material.SetColor(BaseColorId, color);
            item.gameObject.SetActive(true);

            activeItems.Add(new ActiveItem { poolIndex = idx, elapsed = 0f, startColor = color });
        }
    }

    private void UpdateActiveItems()
    {
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            ActiveItem active = activeItems[i];
            active.elapsed += Time.unscaledDeltaTime;

            if (active.elapsed >= fadeDuration)
            {
                pool[active.poolIndex].gameObject.SetActive(false);
                freeIndices.Enqueue(active.poolIndex);
                activeItems.RemoveAt(i);
                continue;
            }

            float alpha = Mathf.Lerp(active.startColor.a, 0f, active.elapsed / fadeDuration);
            pool[active.poolIndex].material.SetColor(BaseColorId,
                new Color(active.startColor.r, active.startColor.g, active.startColor.b, alpha));

            activeItems[i] = active;
        }
    }

    private void OnDestroy()
    {
        if (pool == null) return;
        foreach (PoolItem item in pool)
        {
            Destroy(item.bakedMesh);
            Destroy(item.material);
        }
        Destroy(poolContainer);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Common (비워두면 런타임에 자동 해결)")]
    [Tooltip("비워두면 'Player' 태그 오브젝트를 자동 검색한다. 프리팹화 시 권장.")]
    [SerializeField] private Transform target;
    [Tooltip("비워두면 씬에서 CameraShakeListener를 자동 검색한다.")]
    [SerializeField] private CameraShakeListener cameraShakeListener;
    [Tooltip("비워두면 씬에서 EnemyDeathEffectListener를 자동 검색한다.")]
    [SerializeField] private EnemyDeathEffectListener enemyDeathEffectListener;
    [SerializeField] private float spawnArea = 100f;
    [SerializeField] private float minSpawnDistance = 10f;
    [SerializeField] private float interWaveDelay = 3f;

    [Header("Wave 1 - Basic")]
    [SerializeField] private GameObject basicEnemyPrefab;
    [SerializeField] private GameObject machineGunPrefab;
    [SerializeField] private int wave1EnemyCount = 8;
    [SerializeField] private int wave1MaxConcurrent = 4;
    [SerializeField] private float wave1SpawnInterval = 3f;

    [Header("Wave 2 - Basic + Elite")]
    [SerializeField] private int wave2EnemyCount = 10;
    [SerializeField] private int wave2MaxConcurrent = 5;
    [SerializeField] private float wave2SpawnInterval = 2.5f;
    [SerializeField] private List<GameObject> elitePrefabs = new List<GameObject>();

    [Header("Wave 3 - Boss")]
    [SerializeField] private GameObject bossPrefab;

    [Header("Debug")]
    [Tooltip("체크 시 Wave1/2를 건너뛰고 보스만 스폰한다. StageSelect에서 런타임에 설정된다.")]
    [SerializeField] private bool skipToBoss;
    public bool SkipToBoss { get { return skipToBoss; } set { skipToBoss = value; } }

    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCleared;
    public event Action OnAllWavesCleared;
    public event Action<BossEnemy> OnBossSpawned;

    private HashSet<Enemy> aliveEnemies = new HashSet<Enemy>();
    private WaitForSeconds wave1SpawnWait;
    private WaitForSeconds wave2SpawnWait;
    private WaitForSeconds interWaveWait;

    void Start()
    {
        ResolveSceneReferences();
        wave1SpawnWait = new WaitForSeconds(wave1SpawnInterval);
        wave2SpawnWait = new WaitForSeconds(wave2SpawnInterval);
        interWaveWait = new WaitForSeconds(interWaveDelay);
        StartCoroutine(RunWaves());
    }

    private void ResolveSceneReferences()
    {
        if (target == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                target = playerObject.transform;
            }
            else
            {
                Debug.LogError("EnemySpawner: 'Player' 태그를 가진 오브젝트를 찾을 수 없습니다.");
            }
        }

        if (cameraShakeListener == null)
        {
            cameraShakeListener = FindFirstObjectByType<CameraShakeListener>();
        }

        if (enemyDeathEffectListener == null)
        {
            enemyDeathEffectListener = FindFirstObjectByType<EnemyDeathEffectListener>();
        }
    }

    void OnDestroy()
    {
        foreach (Enemy enemy in aliveEnemies)
        {
            if (enemy != null) enemy.OnDeath -= OnEnemyDied;
        }
    }

    private IEnumerator RunWaves()
    {
        if (skipToBoss)
        {
            yield return StartCoroutine(RunBossWave());
        }
        else
        {
            yield return StartCoroutine(RunBasicWave());
            yield return interWaveWait;
            yield return StartCoroutine(RunMixedWave());
            yield return interWaveWait;
            yield return StartCoroutine(RunBossWave());
        }

        if (OnAllWavesCleared != null) OnAllWavesCleared.Invoke();
    }

    private IEnumerator RunBasicWave()
    {
        if (OnWaveStarted != null) OnWaveStarted.Invoke(1);

        int spawned = 0;
        while (spawned < wave1EnemyCount)
        {
            yield return new WaitUntil(() => aliveEnemies.Count < wave1MaxConcurrent);
            SpawnBasicEnemy();
            spawned++;
            yield return wave1SpawnWait;
        }

        yield return new WaitUntil(() => aliveEnemies.Count == 0);
        if (OnWaveCleared != null) OnWaveCleared.Invoke(1);
    }

    private IEnumerator RunMixedWave()
    {
        if (OnWaveStarted != null) OnWaveStarted.Invoke(2);

        int eliteSpawnIndex = wave2EnemyCount / 2;
        int spawned = 0;
        while (spawned < wave2EnemyCount)
        {
            yield return new WaitUntil(() => aliveEnemies.Count < wave2MaxConcurrent);
            SpawnBasicEnemy();
            if (spawned == eliteSpawnIndex)
            {
                SpawnRandomElite();
            }
            spawned++;
            yield return wave2SpawnWait;
        }

        yield return new WaitUntil(() => aliveEnemies.Count == 0);
        if (OnWaveCleared != null) OnWaveCleared.Invoke(2);
    }

    private IEnumerator RunBossWave()
    {
        if (OnWaveStarted != null) OnWaveStarted.Invoke(3);
        SpawnBoss();
        yield return new WaitUntil(() => aliveEnemies.Count == 0);
        if (OnWaveCleared != null) OnWaveCleared.Invoke(3);
    }

    private void SpawnBasicEnemy()
    {
        if (basicEnemyPrefab == null) return;

        Vector3 position = GenerateSpawnPosition();
        GameObject enemyObject = Instantiate(basicEnemyPrefab, position, Quaternion.identity, transform);

        if (!enemyObject.TryGetComponent<Enemy>(out Enemy enemy))
        {
            Debug.LogWarning("EnemySpawner: basicEnemyPrefab에 Enemy 컴포넌트가 없음. 파괴합니다.");
            Destroy(enemyObject);
            return;
        }

        if (machineGunPrefab != null)
        {
            GameObject weaponInstance = Instantiate(machineGunPrefab);
            if (weaponInstance.TryGetComponent<Weapon>(out Weapon weaponComponent))
            {
                enemy.EquipWeapon(weaponComponent);
            }
            else
            {
                Destroy(weaponInstance);
            }
        }

        EnemyHitFeedback hitFeedback = enemyObject.AddComponent<EnemyHitFeedback>();
        hitFeedback.Initialize(enemy);

        if (target != null) enemy.SetTarget(target);
        RegisterSpawnedEnemy(enemy);
    }

    private void SpawnRandomElite()
    {
        if (elitePrefabs == null || elitePrefabs.Count == 0) return;

        int index = UnityEngine.Random.Range(0, elitePrefabs.Count);
        GameObject selectedElitePrefab = elitePrefabs[index];
        if (selectedElitePrefab == null) return;

        Vector3 position = GenerateSpawnPosition();
        GameObject eliteObject = Instantiate(selectedElitePrefab, position, Quaternion.identity, transform);

        if (!eliteObject.TryGetComponent<Enemy>(out Enemy enemy))
        {
            Debug.LogWarning("EnemySpawner: elitePrefab에 Enemy 컴포넌트가 없음. 파괴합니다.");
            Destroy(eliteObject);
            return;
        }

        // 엘리트는 Start()에서 SetupTargetIfNeeded/SetupWeaponIfNeeded를 자체 처리한다.
        // 명시적으로 target을 주입해 플레이어 탐색 실패를 방지한다.
        if (target != null) enemy.SetTarget(target);
        RegisterSpawnedEnemy(enemy);
    }

    private void SpawnBoss()
    {
        if (bossPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: bossPrefab이 설정되지 않음. 웨이브3를 건너뜁니다.");
            return;
        }

        Vector3 bossSpawnPosition = transform.position + new Vector3(0f, bossPrefab.transform.position.y, 0f);
        GameObject bossObject = Instantiate(bossPrefab, bossSpawnPosition, Quaternion.identity, transform);

        if (!bossObject.TryGetComponent<BossEnemy>(out BossEnemy bossEnemy))
        {
            Debug.LogWarning("EnemySpawner: bossPrefab에 BossEnemy 컴포넌트가 없음. 파괴합니다.");
            Destroy(bossObject);
            return;
        }

        if (target != null) bossEnemy.ActivateBoss(target);
        RegisterSpawnedEnemy(bossEnemy);
        if (OnBossSpawned != null) OnBossSpawned.Invoke(bossEnemy);
    }

    private void RegisterSpawnedEnemy(Enemy enemy)
    {
        enemy.OnDeath += OnEnemyDied;
        if (cameraShakeListener != null) cameraShakeListener.RegisterEnemy(enemy);
        if (enemyDeathEffectListener != null) enemyDeathEffectListener.RegisterEnemy(enemy);
        aliveEnemies.Add(enemy);
    }

    private void OnEnemyDied(Enemy enemy)
    {
        aliveEnemies.Remove(enemy);
    }

    private Vector3 GenerateSpawnPosition()
    {
        float x = UnityEngine.Random.Range(-spawnArea, spawnArea);
        float z = UnityEngine.Random.Range(-spawnArea, spawnArea);
        Vector3 position = new Vector3(transform.position.x + x, 0f, transform.position.z + z);

        if (target == null) return position;

        float distance = Vector3.Distance(position, target.position);
        if (distance < minSpawnDistance)
        {
            Vector3 direction = (position - target.position).normalized;
            position = target.position + direction * (minSpawnDistance + 0.1f);
            position.y = 0f;
        }

        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
        {
            position = hit.position;
        }

        return position;
    }

#if UNITY_EDITOR
    // Scene 뷰에서 스폰 범위를 시각적으로 확인하기 위한 에디터 전용 Gizmo.
    private void OnDrawGizmosSelected()
    {
        // SpawnArea — spawner 위치 기준 ±spawnArea의 X/Z 정사각형 범위
        Vector3 boxCenter = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 boxSize = new Vector3(spawnArea * 2f, 0.01f, spawnArea * 2f);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.15f);
        Gizmos.DrawCube(boxCenter, boxSize);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireCube(boxCenter, boxSize);

        // minSpawnDistance — 런타임에는 플레이어 기준. 에디터에선 target 또는 Player 태그 오브젝트를 참조
        Transform reference = target;
        if (reference == null)
        {
            GameObject tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null) reference = tagged.transform;
        }
        Vector3 sphereCenter = reference != null
            ? new Vector3(reference.position.x, 0f, reference.position.z)
            : boxCenter;
        Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.9f);
        Gizmos.DrawWireSphere(sphereCenter, minSpawnDistance);
    }
#endif
}

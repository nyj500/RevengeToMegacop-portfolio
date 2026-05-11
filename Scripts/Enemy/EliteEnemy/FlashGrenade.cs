using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class FlashGrenade : MonoBehaviour
{
    [Header("Landing")]
    [SerializeField] private Rigidbody rb;

    [Header("Spin")]
    [SerializeField] private float minSpinSpeed = 3f;
    [SerializeField] private float maxSpinSpeed = 8f;

    [Header("Landed Rotation")]
    [SerializeField] private float landedTiltZ = 90f;
    [SerializeField] private bool randomizeLandedYaw = true;

    [Header("Indicator")]
    [SerializeField] private float indicatorRadius = 5f;
    [SerializeField] private int indicatorSegments = 40;
    [SerializeField] private float indicatorYOffset = 0.01f;
    [SerializeField] private Material indicatorMaterial;

    [Header("Explosion")]
    [SerializeField] private float explodeDelayAfterLanding = 0.75f;
    [SerializeField] private bool destroyOnExplode = true;

    [Header("Explosion VFX")]
    [SerializeField] private float explosionVfxStartScale = 0.2f;
    [SerializeField] private float explosionVfxDuration = 0.12f;

    [Header("Explosion SFX")]
    [SerializeField] private AudioClip flashbangExplosionClip;

    [Header("Flashbang Audio Effect")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string lowPassCutoffParameter = "LowPassCutoff";
    [SerializeField] private float muffledStartCutoff = 800f;

    [Header("Flash Effect")]
    [SerializeField] private float flashIntensity = 1f;
    [SerializeField] private float flashDuration = 1.2f;
    [SerializeField] private string playerCameraTag = "MainCamera";

    [Header("Flash Curve")]
    [SerializeField, Range(0f, 1f)] private float fullBlindPortion = 0.18f;
    [SerializeField, Range(0f, 1f)] private float softenEndPortion = 0.75f;
    [SerializeField, Range(0f, 1f)] private float softenedIntensityRatio = 0.6f;

    private bool hasLanded = false;
    private bool hasExploded = false;
    private float landingTimer = 0f;
    private GameObject landingIndicator;

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    private void Start()
    {
        ApplyRandomSpin();
    }

    private void Update()
    {
        if (!hasLanded || hasExploded)
            return;

        landingTimer -= Time.deltaTime;

        if (landingTimer <= 0f)
        {
            Explode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasLanded)
            return;

        Land();
    }

    private void ApplyRandomSpin()
    {
        if (rb == null)
            return;

        float spinX = Random.Range(minSpinSpeed, maxSpinSpeed) * RandomSign();
        float spinY = Random.Range(minSpinSpeed, maxSpinSpeed) * RandomSign();
        float spinZ = Random.Range(minSpinSpeed, maxSpinSpeed) * RandomSign();

        rb.angularVelocity = new Vector3(spinX, spinY, spinZ);
    }

    private float RandomSign()
    {
        return Random.value < 0.5f ? -1f : 1f;
    }

    private void Land()
    {
        hasLanded = true;
        landingTimer = explodeDelayAfterLanding;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        ApplyLandedRotation();
        CreateLandingIndicator();
    }

    private void ApplyLandedRotation()
    {
        float landedYaw = randomizeLandedYaw ? Random.Range(0f, 360f) : transform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0f, landedYaw, landedTiltZ);
    }

    private void CreateLandingIndicator()
    {
        landingIndicator = new GameObject("FlashRangeIndicator");

        Vector3 indicatorPosition = new Vector3(
            transform.position.x,
            indicatorYOffset,
            transform.position.z
        );

        landingIndicator.transform.position = indicatorPosition;
        landingIndicator.transform.rotation = Quaternion.identity;

        MeshFilter mf = landingIndicator.AddComponent<MeshFilter>();
        MeshRenderer mr = landingIndicator.AddComponent<MeshRenderer>();

        mf.mesh = GenerateCircleMesh(indicatorRadius, indicatorSegments);

        if (indicatorMaterial != null)
        {
            mr.material = indicatorMaterial;
        }

        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    private void Explode()
    {
        hasExploded = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFXAtPoint(flashbangExplosionClip, transform.position);
        }

        PlayExplosionVfx();

        Collider[] hits = Physics.OverlapSphere(transform.position, indicatorRadius);

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Player"))
                continue;

            Camera playerCamera = FindPlayerCamera(hit);

            if (playerCamera != null)
            {
                FlashScreenOverlay overlay = playerCamera.GetComponentInChildren<FlashScreenOverlay>(true);

                if (overlay == null)
                {
                    overlay = FlashScreenOverlay.Create(playerCamera.transform);
                }

                overlay.PlayFlash(
                    flashIntensity,
                    flashDuration,
                    fullBlindPortion,
                    softenEndPortion,
                    softenedIntensityRatio
                );

                StartDetachedMuffleEffect();
            }

            break;
        }

        if (landingIndicator != null)
        {
            Destroy(landingIndicator);
            landingIndicator = null;
        }

        HideGrenadeImmediately();

        if (destroyOnExplode)
        {
            Destroy(gameObject);
        }
    }

    private void HideGrenadeImmediately()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }
    }

    private void StartDetachedMuffleEffect()
    {
        if (audioMixer == null)
            return;

        GameObject runnerObject = new GameObject("FlashbangAudioMuffleRunner");
        DetachedMuffleRunner runner = runnerObject.AddComponent<DetachedMuffleRunner>();

        runner.Play(
            audioMixer,
            lowPassCutoffParameter,
            muffledStartCutoff,
            flashDuration,
            fullBlindPortion,
            softenEndPortion,
            softenedIntensityRatio
        );
    }

    private void PlayExplosionVfx()
    {
        Vector3 vfxPosition = transform.position;
        float explosionVfxEndScale = indicatorRadius * 2f;

        ExplosionBurstVfx.Create(
            vfxPosition,
            explosionVfxStartScale,
            explosionVfxEndScale,
            explosionVfxDuration
        );
    }

    private Camera FindPlayerCamera(Collider playerHit)
    {
        Camera childCamera = playerHit.GetComponentInChildren<Camera>(true);
        if (childCamera != null)
            return childCamera;

        Camera parentCamera = playerHit.GetComponentInParent<Camera>(true);
        if (parentCamera != null)
            return parentCamera;

        GameObject taggedCameraObject = GameObject.FindGameObjectWithTag(playerCameraTag);
        if (taggedCameraObject != null)
        {
            Camera taggedCamera = taggedCameraObject.GetComponent<Camera>();
            if (taggedCamera != null)
                return taggedCamera;
        }

        return Camera.main;
    }

    private Mesh GenerateCircleMesh(float radius, int segments)
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;

        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.Deg2Rad * (i * angleStep);
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            vertices[i + 1] = new Vector3(x, 0f, z);
        }

        for (int i = 0; i < segments; i++)
        {
            int current = i + 1;
            int next = (i + 1) % segments + 1;

            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = next;
            triangles[i * 3 + 2] = current;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, indicatorRadius);
    }

    private class FlashScreenOverlay : MonoBehaviour
    {
        private Canvas canvas;
        private Image flashImage;

        private float currentIntensity;
        private float currentDuration;
        private float currentFullBlindPortion;
        private float currentSoftenEndPortion;
        private float currentSoftenedIntensityRatio;
        private float timeRemaining;

        public static FlashScreenOverlay Create(Transform cameraTransform)
        {
            GameObject root = new GameObject("FlashScreenOverlay");
            root.transform.SetParent(cameraTransform, false);

            FlashScreenOverlay overlay = root.AddComponent<FlashScreenOverlay>();
            overlay.Initialize();

            return overlay;
        }

        private void Initialize()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            gameObject.AddComponent<GraphicRaycaster>();

            GameObject imageObject = new GameObject("FlashImage");
            imageObject.transform.SetParent(transform, false);

            flashImage = imageObject.AddComponent<Image>();
            flashImage.color = new Color(0f, 0f, 0f, 0f);
            flashImage.raycastTarget = false;

            RectTransform rect = flashImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public void PlayFlash(
            float intensity,
            float duration,
            float fullBlindPortion,
            float softenEndPortion,
            float softenedIntensityRatio)
        {
            if (duration <= 0f)
                return;

            intensity = Mathf.Clamp01(intensity);
            fullBlindPortion = Mathf.Clamp01(fullBlindPortion);
            softenEndPortion = Mathf.Clamp(softenEndPortion, fullBlindPortion, 1f);
            softenedIntensityRatio = Mathf.Clamp01(softenedIntensityRatio);

            if (timeRemaining > 0f)
            {
                float currentAlpha = flashImage != null ? flashImage.color.a : 0f;
                if (intensity <= currentAlpha)
                    return;
            }

            currentIntensity = intensity;
            currentDuration = duration;
            currentFullBlindPortion = fullBlindPortion;
            currentSoftenEndPortion = softenEndPortion;
            currentSoftenedIntensityRatio = softenedIntensityRatio;
            timeRemaining = duration;

            if (flashImage != null)
            {
                flashImage.color = new Color(0f, 0f, 0f, currentIntensity);
            }
        }

        private void Update()
        {
            if (flashImage == null)
                return;

            if (timeRemaining <= 0f)
            {
                flashImage.color = new Color(0f, 0f, 0f, 0f);
                return;
            }

            timeRemaining -= Time.unscaledDeltaTime;

            float elapsed = currentDuration - Mathf.Max(timeRemaining, 0f);
            float normalizedTime = currentDuration > 0f
                ? Mathf.Clamp01(elapsed / currentDuration)
                : 1f;

            float alpha;

            if (normalizedTime <= currentFullBlindPortion)
            {
                alpha = currentIntensity;
            }
            else if (normalizedTime <= currentSoftenEndPortion)
            {
                float t = Mathf.InverseLerp(currentFullBlindPortion, currentSoftenEndPortion, normalizedTime);
                float start = currentIntensity;
                float end = currentIntensity * currentSoftenedIntensityRatio;
                alpha = Mathf.Lerp(start, end, t);
            }
            else
            {
                float t = Mathf.InverseLerp(currentSoftenEndPortion, 1f, normalizedTime);
                float start = currentIntensity * currentSoftenedIntensityRatio;
                alpha = Mathf.Lerp(start, 0f, t);
            }

            flashImage.color = new Color(0f, 0f, 0f, alpha);
        }
    }

    private class DetachedMuffleRunner : MonoBehaviour
    {
        private AudioMixer mixer;
        private string cutoffParameter;
        private float startCutoff;
        private float duration;
        private float fullBlindPortion;
        private float softenEndPortion;
        private float softenedIntensityRatio;

        public void Play(
            AudioMixer targetMixer,
            string targetCutoffParameter,
            float muffledCutoff,
            float effectDuration,
            float fullBlind,
            float softenEnd,
            float softenedRatio)
        {
            mixer = targetMixer;
            cutoffParameter = targetCutoffParameter;
            startCutoff = muffledCutoff;
            duration = effectDuration;
            fullBlindPortion = Mathf.Clamp01(fullBlind);
            softenEndPortion = Mathf.Clamp(softenEnd, fullBlindPortion, 1f);
            softenedIntensityRatio = Mathf.Clamp01(softenedRatio);

            DontDestroyOnLoad(gameObject);
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            if (mixer == null)
            {
                Destroy(gameObject);
                yield break;
            }

            float originalCutoff = 22000f;
            bool hasCutoffParameter = mixer.GetFloat(cutoffParameter, out originalCutoff);

            if (!hasCutoffParameter)
                originalCutoff = 22000f;

            float middleCutoff = Mathf.Lerp(startCutoff, originalCutoff, 1f - softenedIntensityRatio);

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float normalizedTime = duration > 0f
                    ? Mathf.Clamp01(elapsed / duration)
                    : 1f;

                float currentCutoff;

                if (normalizedTime <= fullBlindPortion)
                {
                    currentCutoff = startCutoff;
                }
                else if (normalizedTime <= softenEndPortion)
                {
                    float t = Mathf.InverseLerp(fullBlindPortion, softenEndPortion, normalizedTime);
                    currentCutoff = Mathf.Lerp(startCutoff, middleCutoff, t);
                }
                else
                {
                    float t = Mathf.InverseLerp(softenEndPortion, 1f, normalizedTime);
                    currentCutoff = Mathf.Lerp(middleCutoff, originalCutoff, t);
                }

                mixer.SetFloat(cutoffParameter, currentCutoff);

                yield return null;
            }

            mixer.SetFloat(cutoffParameter, originalCutoff);

            Destroy(gameObject);
        }
    }

    private class ExplosionBurstVfx : MonoBehaviour
    {
        private MeshRenderer meshRenderer;
        private float duration;
        private float elapsedTime;
        private float startScale;
        private float endScale;

        public static ExplosionBurstVfx Create(
            Vector3 worldPosition,
            float startScale,
            float endScale,
            float duration)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "FlashExplosionBurst";
            sphere.transform.position = worldPosition;

            Collider sphereCollider = sphere.GetComponent<Collider>();
            if (sphereCollider != null)
            {
                Destroy(sphereCollider);
            }

            ExplosionBurstVfx burst = sphere.AddComponent<ExplosionBurstVfx>();
            burst.Initialize(startScale, endScale, duration);

            return burst;
        }

        private void Initialize(float startScaleValue, float endScaleValue, float durationValue)
        {
            startScale = Mathf.Max(0.01f, startScaleValue);
            endScale = Mathf.Max(startScale, endScaleValue);
            duration = Mathf.Max(0.01f, durationValue);

            transform.localScale = Vector3.one * startScale;

            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Material runtimeMaterial = new Material(Shader.Find("Sprites/Default"));
                runtimeMaterial.color = new Color(1f, 1f, 1f, 0.95f);
                meshRenderer.material = runtimeMaterial;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
            }
        }

        private void Update()
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / duration);

            float scale = Mathf.Lerp(startScale, endScale, t);
            transform.localScale = Vector3.one * scale;

            if (meshRenderer != null && meshRenderer.material != null)
            {
                float alpha = 1f - t;
                alpha *= alpha;

                Color color = meshRenderer.material.color;
                color.a = alpha;
                meshRenderer.material.color = color;
            }

            if (elapsedTime >= duration)
            {
                if (meshRenderer != null && meshRenderer.material != null)
                {
                    Destroy(meshRenderer.material);
                }

                Destroy(gameObject);
            }
        }
    }
}
using UnityEngine;
using System.Collections;

public class MeshParticleAnimator : MonoBehaviour
{
    [Header("Mesh Reference")]
    [Tooltip("Drag the Salt GameObject's Renderer here in the Inspector")]
    public Renderer meshRenderer;

    [Header("Animation Settings")]
    public float duration = 2.0f;

    [Range(0f, 1f)]
    public float targetAlpha = 1.0f;

    public float targetScale = 2.0f;

    private Coroutine animationCoroutine;
    private Material meshMaterial;

    private void Start()
    {
        if (meshRenderer != null)
        {
            meshMaterial = meshRenderer.material;

            SetAlphaAndScale(0f, 0f);
            meshRenderer.enabled = false;
        }
    }

    public void StartMeshAnimation()
    {
        if (meshRenderer == null || meshMaterial == null) return;

        meshRenderer.enabled = true;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(AnimateMesh());
    }

    private IEnumerator AnimateMesh()
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float currentScale = Mathf.Lerp(0f, targetScale, elapsedTime / duration);
            float currentAlpha = Mathf.Lerp(0f, targetAlpha, elapsedTime / duration);

            SetAlphaAndScale(currentAlpha, currentScale);

            yield return null;
        }

        SetAlphaAndScale(targetAlpha, targetScale);
    }

    private void SetAlphaAndScale(float alpha, float scale)
    {
        if (meshRenderer != null)
        {
            // Applies scale ONLY to the salt object, not the main animation object
            meshRenderer.transform.localScale = new Vector3(scale, scale, scale);
        }

        if (meshMaterial != null)
        {
            Color color = meshMaterial.color;
            color.a = alpha;
            meshMaterial.color = color;
        }
    }
}
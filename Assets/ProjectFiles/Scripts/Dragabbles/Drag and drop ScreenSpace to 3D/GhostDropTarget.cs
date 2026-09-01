using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace DeterminingMassofaBodyUsingMeterscale
{
    [RequireComponent(typeof(Collider))]
    public class GhostDropTarget : MonoBehaviour
    {
        [Header("Identification")]
        [SerializeField] private string correctItemID;

        [Header("Renderers To Control")]
        [SerializeField] private List<Renderer> targetRenderers = new();

        [Header("Highlight Material")]
        [SerializeField] private Material highlightMaterial;

        [Header("UI Image Feedback (shown on correct drop)")]
        [SerializeField] private Image resultImage;
        [SerializeField] private Sprite resultSprite;
        [SerializeField] private bool hideResultImageOnStart = true;

        [Header("Result Image Highlight (shown only while dragging)")]
        [SerializeField] private GameObject resultImageHighlight;

        public event Action OnCorrectDropped;

        private readonly Dictionary<Renderer, Material[]> originalMaterials =
            new();

        private bool completed;

        private void Awake()
        {
            CacheOriginalMaterials();
        }

        private void Start()
        {
            completed = false;
            ApplyHighlightMaterial();

            if (hideResultImageOnStart && resultImage != null)
                resultImage.gameObject.SetActive(false);

            // Highlight is drag-driven, so it starts hidden.
            if (resultImageHighlight != null)
                resultImageHighlight.SetActive(false);
        }

        private void CacheOriginalMaterials()
        {
            originalMaterials.Clear();

            foreach (var rend in targetRenderers)
            {
                if (rend == null) continue;
                originalMaterials[rend] = rend.sharedMaterials;
            }
        }

        private void ApplyHighlightMaterial()
        {
            if (highlightMaterial == null) return;

            foreach (var rend in targetRenderers)
            {
                if (rend == null) continue;

                var materials = rend.sharedMaterials;

                for (int i = 0; i < materials.Length; i++)
                    materials[i] = highlightMaterial;

                rend.sharedMaterials = materials;
            }
        }

        private void RestoreOriginalMaterials()
        {
            foreach (var pair in originalMaterials)
            {
                if (pair.Key == null) continue;
                pair.Key.sharedMaterials = pair.Value;
            }
        }

        private void ShowResultImage()
        {
            if (resultImage == null) return;

            if (resultSprite != null)
                resultImage.sprite = resultSprite;

            var color = resultImage.color;
            color.a = 1f;
            resultImage.color = color;

            resultImage.gameObject.SetActive(true);
        }

        /// <summary>
        /// Call from the drag item's OnBeginDrag to show this target's
        /// highlight while the user is dragging.
        /// </summary>
        public void ShowDragHighlight()
        {
            if (completed) return; // already solved, don't re-show
            if (resultImageHighlight != null)
                resultImageHighlight.SetActive(true);
        }

        /// <summary>
        /// Call from the drag item's OnEndDrag, regardless of whether the
        /// drop was correct, incorrect, or missed entirely.
        /// </summary>
        public void HideDragHighlight()
        {
            if (resultImageHighlight != null)
                resultImageHighlight.SetActive(false);
        }

        public bool TryDrop(UIDragItem item)
        {
            if (completed || item == null)
                return false;

            if (item.ItemID != correctItemID)
                return false;

            completed = true;

            RestoreOriginalMaterials();
            item.gameObject.SetActive(false);
            ShowResultImage();
            HideDragHighlight();

            PageNavigationController.RequestNavigationUnlock();
            OnCorrectDropped?.Invoke();
            return true;
        }
    }
}
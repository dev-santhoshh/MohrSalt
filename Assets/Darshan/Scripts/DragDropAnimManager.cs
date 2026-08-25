using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// -----------------------------------------------------------------
// One mechanism per page. Drag is restricted to X/Y (Z locked)
// by default, or to a single axis if you choose X or Y.
// Movement is smoothed with Lerp.
// On correct snap the assigned Animator is enabled; otherwise
// it stays disabled.
// -----------------------------------------------------------------
public class DragDropAnimManager : MonoBehaviour
{
    public enum DragAxis
    {
        XY,   // move freely in X and Y, lock Z
        X,    // only X
        Y     // only Y
    }

    [System.Serializable]
    public class PageEntry
    {
        public int pageIndex;

        [Tooltip("The collider on the object the user drags.")]
        public Collider dragTarget;

        [Tooltip("Trigger collider marking the valid drop zone. Must have 'Is Trigger' checked.")]
        public Collider snapZone;

        [Tooltip("Max rotation difference (degrees) from snapZone's rotation to count as a valid drop. Leave at 180 to ignore rotation entirely and only check overlap.")]
        public float snapAngle = 180f;

        [Tooltip("Which axis/axes the object may move on while dragging. XY = X+Y (Z locked).")]
        public DragAxis dragAxis = DragAxis.XY;

        [Tooltip("Animator on the dragged object. Enabled only after a successful snap; otherwise kept disabled.")]
        public Animator objectAnimator;

        [Tooltip("Renderers to highlight while this page's object is waiting to be dragged.")]
        public List<Renderer> targetRenderers = new List<Renderer>();

        public Material highlightMaterial;
        public AnimationSource animation;

        [Header("UI / Objects To Disable On Successful Snap")]
        [Tooltip("Any UI or objects (bottle UI, beaker UI, prompts, hints, etc.) to disable the moment the snap succeeds. Add as many as this page needs.")]
        public List<GameObject> objectsToDisableOnSnap = new List<GameObject>();

        [Header("Post-Animation Transition")]
        [Tooltip("Handles disabling old slide objects, moving camera, enabling next slide objects, then unlocking nav. Called automatically once the animation finishes.")]
        public SlideTransitionHandler transitionHandler;

        [HideInInspector] public List<Material> originalMaterials;
    }

    [Header("Per-Page Drag-Drop Entries (one per page)")]
    public List<PageEntry> entries = new List<PageEntry>();

    [Header("Drag Detection")]
    public Camera raycastCamera;
    public LayerMask draggableLayers = ~0;

    [Header("Smooth Drag")]
    [Tooltip("Higher = snappier follow, lower = smoother / laggy. Typical range 8–20.")]
    [Range(1f, 40f)]
    public float dragSmoothSpeed = 15f;

    private int currentPageIndex = -1;
    private readonly HashSet<int> finishedPages = new HashSet<int>();
    private bool dragging = false;
    private Transform draggedTransform;
    private Vector3 dragPlaneOffset;
    private float lockedZ;
    private Vector3 dragStartPosition;
    private Quaternion dragStartRotation;
    private Vector3 smoothTargetPos;   // the position we are lerping toward

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += SetPageContext;
    }

    private void OnDisable()
    {
        PageNavigationController.OnPageChanged -= SetPageContext;
    }

    private void OnDestroy()
    {
        PageNavigationController.OnPageChanged -= SetPageContext;
    }

    private void Start()
    {
        if (raycastCamera == null)
            raycastCamera = Camera.main;

        foreach (var e in entries)
        {
            if (e != null && e.objectAnimator != null)
                e.objectAnimator.enabled = false;
        }

        SetPageContext(PageNavigationController.CurrentIndex);
    }

    private void Update()
    {
        if (Pointer.current == null) return;
        if (currentPageIndex < 0) return;
        if (finishedPages.Contains(currentPageIndex)) return;

        PageEntry entry = FindEntry(currentPageIndex);
        if (entry == null || entry.dragTarget == null || entry.snapZone == null) return;

        if (raycastCamera == null) raycastCamera = Camera.main;
        if (raycastCamera == null) return;

        if (!dragging && Pointer.current.press.wasPressedThisFrame)
        {
            TryBeginDrag(entry);
        }
        else if (dragging && Pointer.current.press.isPressed)
        {
            ContinueDrag(entry);
        }
        else if (dragging && Pointer.current.press.wasReleasedThisFrame)
        {
            EndDrag(currentPageIndex, entry);
        }
    }

    private void TryBeginDrag(PageEntry entry)
    {
        Ray ray = raycastCamera.ScreenPointToRay(Pointer.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, draggableLayers))
            return;

        if (hit.collider != entry.dragTarget &&
            hit.collider.transform.GetComponentInParent<Collider>() != entry.dragTarget)
            return;

        draggedTransform = entry.dragTarget.transform;
        dragStartPosition = draggedTransform.position;
        dragStartRotation = draggedTransform.rotation;
        lockedZ = draggedTransform.position.z;
        smoothTargetPos = draggedTransform.position;   // start from current position
        dragging = true;

        Vector3 pointerWorld = ScreenToXYPlanePoint(Pointer.current.position.ReadValue());
        dragPlaneOffset = draggedTransform.position - pointerWorld;

        if (entry.objectAnimator != null)
            entry.objectAnimator.enabled = false;
    }

    private void ContinueDrag(PageEntry entry)
    {
        if (draggedTransform == null) return;

        Vector3 pointerWorld = ScreenToXYPlanePoint(Pointer.current.position.ReadValue());
        Vector3 idealPos = pointerWorld + dragPlaneOffset;
        idealPos.z = lockedZ;

        Vector3 current = draggedTransform.position;
        switch (entry.dragAxis)
        {
            case DragAxis.X:
                idealPos = new Vector3(idealPos.x, current.y, lockedZ);
                break;
            case DragAxis.Y:
                idealPos = new Vector3(current.x, idealPos.y, lockedZ);
                break;
            case DragAxis.XY:
            default:
                break;
        }

        smoothTargetPos = idealPos;
        draggedTransform.position = Vector3.Lerp(
            draggedTransform.position,
            smoothTargetPos,
            1f - Mathf.Exp(-dragSmoothSpeed * Time.deltaTime)
        );
    }

    private void EndDrag(int pageIndex, PageEntry entry)
    {
        dragging = false;
        EvaluateDrop(pageIndex, entry);
        draggedTransform = null;
    }

    private Vector3 ScreenToXYPlanePoint(Vector2 screenPos)
    {
        Ray ray = raycastCamera.ScreenPointToRay(screenPos);
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, lockedZ));
        if (plane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        return draggedTransform != null ? draggedTransform.position : Vector3.zero;
    }

    private void EvaluateDrop(int pageIndex, PageEntry entry)
    {
        Transform obj = entry.dragTarget.transform;
        bool overlapping = entry.dragTarget.bounds.Intersects(entry.snapZone.bounds);
        float angle = Quaternion.Angle(obj.rotation, entry.snapZone.transform.rotation);

        Debug.Log($"[DragDrop] Page {pageIndex} release check — overlapping={overlapping}, angle={angle:F2} (max {entry.snapAngle})");

        if (overlapping && angle <= entry.snapAngle)
        {
            Debug.Log($"[DragDrop] Page {pageIndex} — SNAP PASSED, snapping and triggering animation.");
            obj.position = entry.snapZone.transform.position;
            obj.rotation = entry.snapZone.transform.rotation;

            if (entry.objectsToDisableOnSnap != null)
            {
                foreach (var go in entry.objectsToDisableOnSnap)
                {
                    if (go != null) go.SetActive(false);
                }
            }

            if (entry.objectAnimator != null)
                entry.objectAnimator.enabled = true;

            OnSnapped(pageIndex, entry);
        }
        else
        {
            Debug.Log($"[DragDrop] Page {pageIndex} — snap FAILED, out of tolerance. Returning to start position.");
            obj.position = dragStartPosition;
            obj.rotation = dragStartRotation;

            if (entry.objectAnimator != null)
                entry.objectAnimator.enabled = false;
        }
    }

    private void OnSnapped(int pageIndex, PageEntry entry)
    {
        finishedPages.Add(pageIndex);
        ClearHighlight(entry);

        if (entry.animation != null && entry.animation.IsValid)
        {
            Debug.Log($"[DragDrop] Page {pageIndex} — OnSnapped: valid AnimationSource found (director={(entry.animation.director != null ? entry.animation.director.name : "none")}, animator={(entry.animation.animator != null ? entry.animation.animator.name : "none")}). Starting Play().");
            StartCoroutine(entry.animation.Play(this, () =>
            {
                Debug.Log($"[DragDrop] Page {pageIndex} — animation Play() completed.");

                if (entry.transitionHandler != null)
                {
                    entry.transitionHandler.RunTransition();
                }
                else
                {
                    Debug.LogWarning($"[DragDrop] Page {pageIndex}: no transitionHandler assigned - unlocking navigation directly.");
                    PageNavigationController.RequestNavigationUnlock();
                }
            }));
        }
        else
        {
            Debug.LogWarning($"[DragDropAnimManager] Page {pageIndex}: no valid AnimationSource - unlocking immediately.");
            PageNavigationController.RequestNavigationUnlock();
        }
    }

    private void SetPageContext(int pageIndex)
    {
        PageEntry previousEntry = FindEntry(currentPageIndex);
        if (previousEntry != null)
            ClearHighlight(previousEntry);

        currentPageIndex = pageIndex;
        dragging = false;
        draggedTransform = null;

        PageEntry entry = FindEntry(pageIndex);
        if (entry == null || finishedPages.Contains(pageIndex))
            return;

        if (entry.objectAnimator != null)
            entry.objectAnimator.enabled = false;

        ApplyHighlight(entry);
    }

    private void ApplyHighlight(PageEntry entry)
    {
        if (entry.targetRenderers == null || entry.targetRenderers.Count == 0 || entry.highlightMaterial == null)
            return;

        if (entry.originalMaterials == null || entry.originalMaterials.Count != entry.targetRenderers.Count)
        {
            entry.originalMaterials = new List<Material>();
            foreach (var r in entry.targetRenderers)
                entry.originalMaterials.Add(r != null ? r.material : null);
        }

        foreach (var r in entry.targetRenderers)
            if (r != null) r.material = entry.highlightMaterial;
    }

    private void ClearHighlight(PageEntry entry)
    {
        if (entry.targetRenderers == null || entry.originalMaterials == null) return;
        int count = Mathf.Min(entry.targetRenderers.Count, entry.originalMaterials.Count);
        for (int i = 0; i < count; i++)
            if (entry.targetRenderers[i] != null && entry.originalMaterials[i] != null)
                entry.targetRenderers[i].material = entry.originalMaterials[i];
    }

    private PageEntry FindEntry(int pageIndex)
    {
        return entries.Find(e => e != null && e.pageIndex == pageIndex);
    }

    public bool OwnsPage(int pageIndex) => FindEntry(pageIndex) != null;
}
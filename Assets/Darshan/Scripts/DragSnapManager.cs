using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class DragSnapManager : MonoBehaviour
{
    public enum DragAxis { XYZ, X, Y }

    [System.Serializable]
    public class PageEntry
    {
        public int pageIndex;
        public Collider dragTarget;
        public Collider snapZone;
        public float snapThreshold = 1.5f;
        public DragAxis dragAxis = DragAxis.XYZ;
        public List<Renderer> targetRenderers = new List<Renderer>();
        public Material highlightMaterial;
        public GameObject ghostObject;
        public List<GameObject> objectsToDisableOnSnap = new List<GameObject>();

        [Tooltip("World-space UI (e.g. a tooltip/prompt) tied to this drag object. Hidden the instant dragging starts. If the drop is WRONG (object returns to its start position) it is shown again. If the drop is CORRECT it stays hidden for good.")]
        public GameObject dragWorldUI;

        [HideInInspector] public List<Material> originalMaterials;
    }

    public List<PageEntry> entries = new List<PageEntry>();
    public Camera raycastCamera;
    public LayerMask draggableLayers = ~0;
    [Range(1f, 40f)] public float dragSmoothSpeed = 15f;

    private int currentPageIndex = -1;
    private readonly HashSet<int> finishedPages = new HashSet<int>();
    private bool dragging = false;
    private Transform draggedTransform;
    private Vector3 dragPlaneOffset;
    private float lockedZ;
    private Vector3 dragStartPosition;
    private Quaternion dragStartRotation;
    private Vector3 smoothTargetPos;

    private void OnEnable() => PageNavigationController.OnPageChanged += SetPageContext;
    private void OnDisable() => PageNavigationController.OnPageChanged -= SetPageContext;
    private void OnDestroy() => PageNavigationController.OnPageChanged -= SetPageContext;

    private void Start()
    {
        if (raycastCamera == null) raycastCamera = Camera.main;
        SetPageContext(PageNavigationController.CurrentIndex);
    }

    private void Update()
    {
        if (Pointer.current == null || currentPageIndex < 0 || finishedPages.Contains(currentPageIndex)) return;

        PageEntry entry = FindEntry(currentPageIndex);
        if (entry == null || entry.dragTarget == null || raycastCamera == null) return;

        if (!dragging && Pointer.current.press.wasPressedThisFrame) TryBeginDrag(entry);
        else if (dragging && Pointer.current.press.isPressed) ContinueDrag(entry);
        else if (dragging && Pointer.current.press.wasReleasedThisFrame) EndDrag(currentPageIndex, entry);
    }

    private void TryBeginDrag(PageEntry entry)
    {
        Ray ray = raycastCamera.ScreenPointToRay(Pointer.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, draggableLayers)) return;

        if (hit.collider != entry.dragTarget && hit.collider.transform.GetComponentInParent<Collider>() != entry.dragTarget) return;

        if (entry.ghostObject != null) entry.ghostObject.SetActive(true);

        draggedTransform = entry.dragTarget.transform;
        dragStartPosition = draggedTransform.position;
        dragStartRotation = draggedTransform.rotation;
        lockedZ = draggedTransform.position.z;
        smoothTargetPos = draggedTransform.position;
        dragging = true;

        Vector3 pointerWorld = ScreenToXYPlanePoint(Pointer.current.position.ReadValue());
        dragPlaneOffset = draggedTransform.position - pointerWorld;

        // Hide the world-space UI the moment dragging starts.
        if (entry.dragWorldUI != null) entry.dragWorldUI.SetActive(false);
    }

    private void ContinueDrag(PageEntry entry)
    {
        if (draggedTransform == null) return;

        Vector3 pointerWorld = ScreenToXYPlanePoint(Pointer.current.position.ReadValue());
        Vector3 idealPos = pointerWorld + dragPlaneOffset;
        idealPos.z = lockedZ;

        switch (entry.dragAxis)
        {
            case DragAxis.X: idealPos = new Vector3(idealPos.x, draggedTransform.position.y, lockedZ); break;
            case DragAxis.Y: idealPos = new Vector3(draggedTransform.position.x, idealPos.y, lockedZ); break;
        }

        smoothTargetPos = idealPos;
        draggedTransform.position = Vector3.Lerp(draggedTransform.position, smoothTargetPos, 1f - Mathf.Exp(-dragSmoothSpeed * Time.deltaTime));
    }

    private void EndDrag(int pageIndex, PageEntry entry)
    {
        dragging = false;

        if (draggedTransform != null)
        {
            if (IsReadyToSnap(entry))
            {
                PerformSnap(pageIndex, entry);
            }
            else
            {
                Debug.LogWarning("❌ FAILED TO SNAP: Returning to start position.");
                if (entry.ghostObject != null) entry.ghostObject.SetActive(false);
                entry.dragTarget.transform.position = dragStartPosition;
                entry.dragTarget.transform.rotation = dragStartRotation;

                // Wrong drop -> object is back at its start position, so show the UI again.
                if (entry.dragWorldUI != null) entry.dragWorldUI.SetActive(true);
            }
            draggedTransform = null;
        }
    }

    private Vector3 ScreenToXYPlanePoint(Vector2 screenPos)
    {
        Ray ray = raycastCamera.ScreenPointToRay(screenPos);
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, lockedZ));
        if (plane.Raycast(ray, out float enter)) return ray.GetPoint(enter);
        return draggedTransform != null ? draggedTransform.position : Vector3.zero;
    }

    private bool IsReadyToSnap(PageEntry entry)
    {
        Physics.SyncTransforms();
        Debug.Log("--- SNAP DEBUG START ---");

        if (entry.ghostObject != null && entry.ghostObject.activeInHierarchy)
        {
            Collider ghostCol = entry.ghostObject.GetComponent<Collider>();
            if (ghostCol != null)
            {
                bool ghostIntersect = entry.dragTarget.bounds.Intersects(ghostCol.bounds);
                Debug.Log($"Ghost Collider Intersect: {ghostIntersect}");
                if (ghostIntersect) return true;
            }
            else
            {
                Debug.LogWarning("Ghost object does not have a collider attached!");
            }

            float distToGhost = Vector3.Distance(entry.dragTarget.transform.position, entry.ghostObject.transform.position);
            Debug.Log($"Distance to Ghost: {distToGhost} (Threshold: {entry.snapThreshold})");
            if (distToGhost <= entry.snapThreshold) return true;
        }
        else
        {
            Debug.Log("Ghost object is null or inactive.");
        }

        if (entry.snapZone != null)
        {
            bool snapZoneIntersect = entry.dragTarget.bounds.Intersects(entry.snapZone.bounds);
            Debug.Log($"Snap Zone Collider Intersect: {snapZoneIntersect}");
            if (snapZoneIntersect) return true;

            float distToSnapZone = Vector3.Distance(entry.dragTarget.transform.position, entry.snapZone.transform.position);
            Debug.Log($"Distance to Snap Zone: {distToSnapZone} (Threshold: {entry.snapThreshold})");
            if (distToSnapZone <= entry.snapThreshold) return true;
        }
        else
        {
            Debug.Log("Snap Zone is null.");
        }

        return false;
    }

    private void PerformSnap(int pageIndex, PageEntry entry)
    {
        Debug.Log("✅ SUCCESS: Snapping object!");
        Transform obj = entry.dragTarget.transform;

        if (entry.ghostObject != null)
        {
            obj.position = entry.ghostObject.transform.position;
            obj.rotation = entry.ghostObject.transform.rotation;
            entry.ghostObject.SetActive(false);
        }
        else if (entry.snapZone != null)
        {
            obj.position = entry.snapZone.transform.position;
            obj.rotation = dragStartRotation;
        }

        if (entry.objectsToDisableOnSnap != null)
        {
            foreach (var go in entry.objectsToDisableOnSnap)
            {
                if (go != null) go.SetActive(false);
            }
        }

        // Correct drop -> world UI stays hidden for good on this page.
        OnSnapped(pageIndex, entry);
    }

    private void OnSnapped(int pageIndex, PageEntry entry)
    {
        finishedPages.Add(pageIndex);
        ClearHighlight(entry);
        PageNavigationController.RequestNavigationUnlock();
    }

    private void SetPageContext(int pageIndex)
    {
        PageEntry previousEntry = FindEntry(currentPageIndex);
        if (previousEntry != null)
        {
            ClearHighlight(previousEntry);

            // If we navigated away before this page was solved, restore
            // the world-space UI so it's waiting when the user returns.
            if (!finishedPages.Contains(currentPageIndex) && previousEntry.dragWorldUI != null)
                previousEntry.dragWorldUI.SetActive(true);
        }

        currentPageIndex = pageIndex;
        dragging = false;
        draggedTransform = null;

        PageEntry entry = FindEntry(pageIndex);
        if (entry == null || finishedPages.Contains(pageIndex)) return;

        if (entry.ghostObject != null) entry.ghostObject.SetActive(false);

        ApplyHighlight(entry);
    }

    private void ApplyHighlight(PageEntry entry)
    {
        if (entry.targetRenderers == null || entry.targetRenderers.Count == 0 || entry.highlightMaterial == null) return;
        if (entry.originalMaterials == null || entry.originalMaterials.Count != entry.targetRenderers.Count)
        {
            entry.originalMaterials = new List<Material>();
            foreach (var r in entry.targetRenderers) entry.originalMaterials.Add(r != null ? r.material : null);
        }
        foreach (var r in entry.targetRenderers) if (r != null) r.material = entry.highlightMaterial;
    }

    private void ClearHighlight(PageEntry entry)
    {
        if (entry.targetRenderers == null || entry.originalMaterials == null) return;
        int count = Mathf.Min(entry.targetRenderers.Count, entry.originalMaterials.Count);
        for (int i = 0; i < count; i++) if (entry.targetRenderers[i] != null && entry.originalMaterials[i] != null) entry.targetRenderers[i].material = entry.originalMaterials[i];
    }

    private PageEntry FindEntry(int pageIndex) => entries.Find(e => e != null && e.pageIndex == pageIndex);
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PagePositionController : MonoBehaviour
{
    [System.Serializable]
    public class PositionEntry
    {
        [Tooltip("The object to move.")]
        public Transform target;

        [Tooltip("Where it should end up (position + rotation) when this page loads.")]
        public Transform destination;

        [Tooltip("How long the move takes, in seconds. Set to 0 for an instant snap.")]
        public float moveDuration = 1f;
    }

    [System.Serializable]
    public class PositionPageData
    {
        [Tooltip("Which page index this entry applies to. Matches PageNavigationController's page index.")]
        public int pageIndex;

        [Header("Page Info")]
        public string pageName;

        [Header("Objects To Reposition For This Page")]
        public List<PositionEntry> positions;
    }

    [Header("Pages (assign the page index you want each entry to run on)")]
    [SerializeField] private List<PositionPageData> pages = new List<PositionPageData>();

    [Header("Easing")]
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private int currentPageIndex = -1;

    private readonly Dictionary<Transform, Coroutine> activeMoves = new Dictionary<Transform, Coroutine>();

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += HandlePageChanged;
    }

    private void OnDisable()
    {
        PageNavigationController.OnPageChanged -= HandlePageChanged;
    }

    private void Start()
    {
        HandlePageChanged(PageNavigationController.CurrentIndex);
    }

    private void HandlePageChanged(int index)
    {
        currentPageIndex = index;

        PositionPageData page = FindPage(index);
        if (page == null || page.positions == null)
            return;

        ApplyPositions(page);
    }

    private void ApplyPositions(PositionPageData page)
    {
        foreach (var entry in page.positions)
        {
            if (entry == null || entry.target == null || entry.destination == null)
                continue;

            if (activeMoves.TryGetValue(entry.target, out Coroutine existing) && existing != null)
            {
                StopCoroutine(existing);
            }

            Coroutine routine = StartCoroutine(MoveRoutine(entry));
            activeMoves[entry.target] = routine;
        }
    }

    private IEnumerator MoveRoutine(PositionEntry entry)
    {
        Transform obj = entry.target;
        Vector3 startPos = obj.position;
        Quaternion startRot = obj.rotation;
        Vector3 endPos = entry.destination.position;
        Quaternion endRot = entry.destination.rotation;

        if (entry.moveDuration <= 0f)
        {
            obj.position = endPos;
            obj.rotation = endRot;
            yield break;
        }

        float t = 0f;
        while (t < entry.moveDuration)
        {
            float progress = ease.Evaluate(t / entry.moveDuration);
            obj.position = Vector3.Lerp(startPos, endPos, progress);
            obj.rotation = Quaternion.Slerp(startRot, endRot, progress);
            t += Time.deltaTime;
            yield return null;
        }

        obj.position = endPos;
        obj.rotation = endRot;
    }

    private PositionPageData FindPage(int index)
    {
        return pages.Find(p => p != null && p.pageIndex == index);
    }
}
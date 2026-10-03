using UnityEngine;

namespace OrbitGuard
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaLayout : MonoBehaviour
    {
        Rect previous;
        Vector2Int resolution;
        void Update()
        {
            var area = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (area == previous && size == resolution) return;
            previous = area; resolution = size;
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(area.xMin / Mathf.Max(1, size.x), area.yMin / Mathf.Max(1, size.y));
            rect.anchorMax = new Vector2(area.xMax / Mathf.Max(1, size.x), area.yMax / Mathf.Max(1, size.y));
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}

using UnityEngine;

namespace UXAssist.UI;

public static class Util
{

    public static RectTransform NormalizeRectWithTopLeft(Component cmp, float left, float top, Transform parent = null)
    {
        if (cmp.transform is not RectTransform rect) return null;
        if (parent != null)
        {
            rect.SetParent(parent, false);
        }
        rect.anchorMax = new Vector2(0f, 1f);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition3D = new Vector3(left, -top, 0f);
        return rect;
    }

    public static RectTransform NormalizeRectWithTopRight(Component cmp, float right, float top, Transform parent = null)
    {
        if (cmp.transform is not RectTransform rect) return null;
        if (parent != null)
        {
            rect.SetParent(parent, false);
        }
        rect.anchorMax = new Vector2(1f, 1f);
        rect.anchorMin = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition3D = new Vector3(-right, -top, 0f);
        return rect;
    }

    public static RectTransform NormalizeRectWithBottomLeft(Component cmp, float left, float bottom, Transform parent = null)
    {
        if (cmp.transform is not RectTransform rect) return null;
        if (parent != null)
        {
            rect.SetParent(parent, false);
        }
        rect.anchorMax = new Vector2(0f, 0f);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition3D = new Vector3(left, bottom, 0f);
        return rect;
    }

    public static RectTransform NormalizeRectWithMargin(Component cmp, float top, float left, float bottom, float right, Transform parent = null)
    {
        if (cmp.transform is not RectTransform rect) return null;
        if (parent != null)
        {
            rect.SetParent(parent, false);
        }
        rect.anchoredPosition3D = Vector3.zero;
        rect.localScale = Vector3.one;
        rect.anchorMax = Vector2.one;
        rect.anchorMin = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMax = new Vector2(-right, -top);
        rect.offsetMin = new Vector2(left, bottom);
        return rect;
    }

    public static RectTransform NormalizeRectCenter(GameObject go, float width = 0, float height = 0)
    {
        if (go.transform is not RectTransform rect) return null;
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        if (width > 0 && height > 0)
        {
            rect.sizeDelta = new Vector2(width, height);
        }
        return rect;
    }

}

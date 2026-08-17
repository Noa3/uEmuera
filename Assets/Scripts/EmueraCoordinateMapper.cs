using UnityEngine;
using uEmuera.Drawing;
using uEmuera.Forms;

/// <summary>
/// Converts Unity pointer coordinates into the client-pixel coordinate system
/// expected by the Emuera input layer. The mapper is deliberately independent
/// of a concrete Unity scene so desktop, scaled Canvas and touch input use the
/// same state consumed by MOUSEX/MOUSEY/MOUSEB and button hit testing.
/// </summary>
public static class EmueraCoordinateMapper
{
    private static RectTransform target_;
    private static Camera event_camera_;

    public static void Bind(RectTransform target, Camera eventCamera = null)
    {
        target_ = target;
        event_camera_ = eventCamera;
    }

    public static Point UpdateFromUnity()
    {
        Vector2 screenPosition;
        if (Input.touchCount > 0)
            screenPosition = Input.GetTouch(0).position;
        else
            screenPosition = Input.mousePosition;

        Vector2 clientTopLeft = ToClientPixels(screenPosition);
        var point = new Point(
            Mathf.RoundToInt(clientTopLeft.x),
            Mathf.RoundToInt(clientTopLeft.y));
        Control.MousePosition = point;
        return point;
    }

    private static Vector2 ToClientPixels(Vector2 screenPosition)
    {
        if (target_ == null)
            return new Vector2(screenPosition.x, Screen.height - screenPosition.y);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                target_, screenPosition, event_camera_, out Vector2 local))
            return new Vector2(screenPosition.x, Screen.height - screenPosition.y);

        Rect rect = target_.rect;
        if (rect.width <= 0f || rect.height <= 0f)
            return new Vector2(screenPosition.x, Screen.height - screenPosition.y);

        float normalizedX = Mathf.Clamp01((local.x - rect.xMin) / rect.width);
        float normalizedTopY = Mathf.Clamp01((rect.yMax - local.y) / rect.height);
        return new Vector2(normalizedX * Screen.width, normalizedTopY * Screen.height);
    }
}

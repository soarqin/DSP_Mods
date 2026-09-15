namespace UXAssist.Common.Utils;

/// <summary>
/// Shared player input probes used by the automation features (auto-cruise, auto-construct) to
/// detect a manual override.
/// </summary>
public static class PlayerInputUtil
{
    /// <summary>
    /// Returns true while the player holds any movement key that would fight an automated route.
    /// Only the six actual movement axes are considered: <c>PlayerController.GetInput</c> maps them
    /// to <c>input0.x/y</c> (strafe/forward) and <c>input1.y</c> (vertical thrust), which are the
    /// inputs every automated motion path writes to. Camera, build and UI keys are deliberately
    /// excluded so they do not cancel automation.
    /// </summary>
    public static bool HasManualMovementInput() =>
        VFInput._pullUp.pressing || VFInput._pushDown.pressing ||
        VFInput._moveLeft.pressing || VFInput._moveRight.pressing ||
        VFInput._moveForward.pressing || VFInput._moveBackward.pressing;
}

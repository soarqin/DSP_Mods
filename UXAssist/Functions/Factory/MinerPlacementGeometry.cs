using UnityEngine;

namespace UXAssist.Functions.Factory;

// Work with individual native colliders, never a bounding rectangle around the miner's feet.
internal static class MinerPlacementGeometry
{
    internal readonly struct Box
    {
        public readonly Vector3 Center, Extents, X, Y, Z, Bounds;

        public Box(ColliderData collider)
        {
            Center = collider.pos;
            Extents = collider.ext;
            X = collider.q * Vector3.right;
            Y = collider.q * Vector3.up;
            Z = collider.q * Vector3.forward;
            Bounds = new Vector3(
                Mathf.Abs(X.x) * Extents.x + Mathf.Abs(Y.x) * Extents.y + Mathf.Abs(Z.x) * Extents.z,
                Mathf.Abs(X.y) * Extents.x + Mathf.Abs(Y.y) * Extents.y + Mathf.Abs(Z.y) * Extents.z,
                Mathf.Abs(X.z) * Extents.x + Mathf.Abs(Y.z) * Extents.y + Mathf.Abs(Z.z) * Extents.z);
        }

        public Vector3 ToLocal(Vector3 point)
        {
            var offset = point - Center;
            return new Vector3(Vector3.Dot(offset, X), Vector3.Dot(offset, Y), Vector3.Dot(offset, Z));
        }
    }

    public static bool Intersects(Box a, Box b)
    {
        var delta = b.Center - a.Center;
        // Reject distant individual colliders before the full separating-axis test. This
        // does not replace the independent foot colliders with one enclosing footprint.
        if (Mathf.Abs(delta.x) > a.Bounds.x + b.Bounds.x + 0.00001f ||
            Mathf.Abs(delta.y) > a.Bounds.y + b.Bounds.y + 0.00001f ||
            Mathf.Abs(delta.z) > a.Bounds.z + b.Bounds.z + 0.00001f) return false;
        if (Separated(a.X, delta, a, b) || Separated(a.Y, delta, a, b) || Separated(a.Z, delta, a, b) ||
            Separated(b.X, delta, a, b) || Separated(b.Y, delta, a, b) || Separated(b.Z, delta, a, b)) return false;
        return !Separated(Vector3.Cross(a.X, b.X), delta, a, b) &&
               !Separated(Vector3.Cross(a.X, b.Y), delta, a, b) &&
               !Separated(Vector3.Cross(a.X, b.Z), delta, a, b) &&
               !Separated(Vector3.Cross(a.Y, b.X), delta, a, b) &&
               !Separated(Vector3.Cross(a.Y, b.Y), delta, a, b) &&
               !Separated(Vector3.Cross(a.Y, b.Z), delta, a, b) &&
               !Separated(Vector3.Cross(a.Z, b.X), delta, a, b) &&
               !Separated(Vector3.Cross(a.Z, b.Y), delta, a, b) &&
               !Separated(Vector3.Cross(a.Z, b.Z), delta, a, b);
    }

    private static bool Separated(Vector3 axis, Vector3 delta, Box a, Box b)
    {
        if (axis.sqrMagnitude < 1e-10f) return false;
        var radius = a.Extents.x * Mathf.Abs(Vector3.Dot(axis, a.X)) +
                     a.Extents.y * Mathf.Abs(Vector3.Dot(axis, a.Y)) +
                     a.Extents.z * Mathf.Abs(Vector3.Dot(axis, a.Z)) +
                     b.Extents.x * Mathf.Abs(Vector3.Dot(axis, b.X)) +
                     b.Extents.y * Mathf.Abs(Vector3.Dot(axis, b.Y)) +
                     b.Extents.z * Mathf.Abs(Vector3.Dot(axis, b.Z));
        return Mathf.Abs(Vector3.Dot(delta, axis)) > radius;
    }

    public static bool Intersects(Box box, ColliderData collider)
    {
        switch (collider.shape)
        {
            case EColliderShape.Box:
                return Intersects(box, new Box(collider));
            case EColliderShape.Sphere:
                return PointDistanceSquared(box.ToLocal(collider.pos), box.Extents) <= collider.radius * collider.radius;
            case EColliderShape.Capsule:
                return SegmentDistanceSquared(box.ToLocal(collider.pos - collider.ext),
                    box.ToLocal(collider.pos + collider.ext), box.Extents) <= collider.radius * collider.radius;
            default:
                return true;
        }
    }

    private static float PointDistanceSquared(Vector3 p, Vector3 ext)
    {
        var x = Mathf.Max(0f, Mathf.Abs(p.x) - ext.x);
        var y = Mathf.Max(0f, Mathf.Abs(p.y) - ext.y);
        var z = Mathf.Max(0f, Mathf.Abs(p.z) - ext.z);
        return x * x + y * y + z * z;
    }

    // The distance is quadratic between the segment's crossings of the six box planes.
    // Minimize each interval exactly, including zero-length segments and parallel axes.
    internal static float SegmentDistanceSquared(Vector3 start, Vector3 end, Vector3 ext)
    {
        var direction = end - start;
        var best = Mathf.Min(PointDistanceSquared(start, ext), PointDistanceSquared(end, ext));
        var t = 0f;
        for (var interval = 0; interval < 7 && t < 1f; interval++)
        {
            var next = 1f;
            for (var axis = 0; axis < 3; axis++)
            {
                var d = direction[axis];
                if (Mathf.Abs(d) < 1e-10f) continue;
                var low = (-ext[axis] - start[axis]) / d;
                var high = (ext[axis] - start[axis]) / d;
                if (low > t && low < next) next = low;
                if (high > t && high < next) next = high;
            }
            var midpoint = start + direction * ((t + next) * 0.5f);
            var a = 0f;
            var b = 0f;
            for (var axis = 0; axis < 3; axis++)
            {
                if (Mathf.Abs(midpoint[axis]) <= ext[axis]) continue;
                var boundary = midpoint[axis] < 0f ? -ext[axis] : ext[axis];
                a += direction[axis] * direction[axis];
                b += direction[axis] * (start[axis] - boundary);
            }
            var closest = a > 0f ? Mathf.Clamp(-b / a, t, next) : t;
            best = Mathf.Min(best, PointDistanceSquared(start + direction * closest, ext));
            t = next;
        }
        return best;
    }
}

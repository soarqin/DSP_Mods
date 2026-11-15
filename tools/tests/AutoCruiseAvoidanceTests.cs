using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

// Checks the auto-cruise obstacle avoidance of a built UXAssist.dll without Unity. The avoidance math and
// its inputs are private to PlayerPatch.AutoNavigation, so they are reached through reflection.
internal static class AutoCruiseAvoidanceTests
{
    private const BindingFlags Members =
        BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private const double PlanetRadius = 200.0;
    private const double GasGiantRadius = 800.0;
    private const double TakeoffAltitude = 49.0;

    private static readonly VectorLF3 Up = new(0.0, 1.0, 0.0);
    private static readonly VectorLF3 East = new(1.0, 0.0, 0.0);
    private static readonly VectorLF3 North = new(0.0, 0.0, 1.0);

    // A mecha that has just left a planet centred on the origin, still moving along the ground, with a
    // destination far away in a direction that no obstacle of these checks blocks.
    private static readonly VectorLF3 Mecha = Up * (PlanetRadius + TakeoffAltitude);
    private static readonly VectorLF3 Target = North * (100.0 * GalaxyData.AU);

    private static Type _navigation;
    private static Type _obstacle;
    private static int _checks;
    private static int _failures;

    private static void Check(bool condition, string message)
    {
        _checks++;
        if (condition) return;
        _failures++;
        Console.WriteLine("FAIL  " + message);
    }

    private static MemberInfo Member(string name)
    {
        MemberInfo[] members = _navigation.GetMember(name, Members);
        if (members.Length != 1)
            throw new MissingMemberException($"PlayerPatch.AutoNavigation.{name} was not found; update this check to match the renamed member.");
        return members[0];
    }

    private static double Constant(string name) => (double)((FieldInfo)Member(name)).GetRawConstantValue();

    private static object Obstacle(VectorLF3 position, double radius, double safetyMargin) =>
        Activator.CreateInstance(_obstacle, Members, null, [position, radius, safetyMargin], null);

    private static VectorLF3 Direction(VectorLF3 heading, params object[] obstacles)
    {
        ((FieldInfo)Member("_targetUniversePosition")).SetValue(null, Target);
        ((FieldInfo)Member("_targetUniverseVelocity")).SetValue(null, default(VectorLF3));
        ((FieldInfo)Member("_targetArriveThresholdRadius")).SetValue(null, 4000.0);
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(_obstacle));
        foreach (object obstacle in obstacles) list.Add(obstacle);
        return (VectorLF3)((MethodInfo)Member("ComputeDirection")).Invoke(null, [Mecha, heading, list]);
    }

    private static int Main(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("Usage: AutoCruiseAvoidanceTests <UXAssist.dll>");
        Assembly mod = Assembly.LoadFrom(args[0]);
        _navigation = mod.GetType("UXAssist.Patches.PlayerPatch+AutoNavigation", true);
        _obstacle = mod.GetType("UXAssist.Patches.PlayerPatch+Obstacle", true);
        double planetMargin = Constant("PlanetSafeRadius");
        double hiveMargin = Constant("HiveSafeRadius");

        object planet = Obstacle(default, PlanetRadius, planetMargin);

        Check(VectorLF3.Dot(Direction(East), North) > 0.999,
            "With nothing in the way the mecha must head straight for the target.");

        VectorLF3 takeoff = Direction(East, planet);
        Check(VectorLF3.Dot(takeoff, Up) > 0.85,
            $"Taking off from a planet must climb away from it (heading vs. local up {VectorLF3.Dot(takeoff, Up):0.000}).");

        // A hive's range is wide enough to cover a planet next to its orbit. Here the hive is straight
        // overhead, so "away from the hive" points into the ground.
        object hive = Obstacle(Mecha + Up * 8000.0, 0.0, hiveMargin);
        VectorLF3 underHive = Direction(East, planet, hive);
        Check((underHive - takeoff).magnitude < 1e-9,
            $"A hive range that overlaps the planet must not change the take-off heading (heading vs. local up {VectorLF3.Dot(underHive, Up):0.000}).");

        // A moon close to its gas giant: on the ground of the moon's giant-facing side the mecha is inside
        // the giant's range too, and "away from the giant" points into the ground. The giant is listed
        // first, as the planets are in orbit order.
        VectorLF3 toGiant = (Up * 0.5 + East * Math.Sqrt(0.75)) * 2520.0;
        object giant = Obstacle(Mecha + toGiant, GasGiantRadius, planetMargin);
        foreach (VectorLF3 heading in new[] { East, -East, North })
        {
            VectorLF3 fromMoon = Direction(heading, giant, planet);
            Check(VectorLF3.Dot(fromMoon, Up) > 0.75,
                $"Taking off from a moon inside its gas giant's range must climb away from the moon (heading vs. local up {VectorLF3.Dot(fromMoon, Up):0.000}).");
        }

        // Away from any surface a hive is still left behind.
        object nearbyHive = Obstacle(Mecha + East * 8000.0, 0.0, hiveMargin);
        VectorLF3 fromHive = Direction(North, nearbyHive);
        Check(VectorLF3.Dot(fromHive, -East) > 0.85,
            $"Inside a hive's range alone the mecha must fly away from the hive (heading vs. away {VectorLF3.Dot(fromHive, -East):0.000}).");

        Console.WriteLine(_failures == 0
            ? $"Auto-cruise avoidance checks passed ({_checks} checks)."
            : $"Auto-cruise avoidance checks: {_failures} of {_checks} failed.");
        return _failures == 0 ? 0 : 1;
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static UXAssist.Production.ProductionMath;

namespace UXAssist.Production;

public sealed class BlueprintDescriptionLabels
{
    public string FactoryPower { get; }
    public string LogisticsPower { get; }
    public string Missing { get; }
    public string Excess { get; }
    public string Unknown { get; }
    public string KnownPortion { get; }
    public string InputLine { get; }
    public string OutputLine { get; }
    public string ResearchLine { get; }

    public BlueprintDescriptionLabels(string factoryPower, string logisticsPower, string missing, string excess,
        string unknown, string knownPortion, string inputLine, string outputLine, string researchLine)
    {
        FactoryPower = factoryPower;
        LogisticsPower = logisticsPower;
        Missing = missing;
        Excess = excess;
        Unknown = unknown;
        KnownPortion = knownPortion;
        InputLine = inputLine;
        OutputLine = outputLine;
        ResearchLine = researchLine;
    }
}

public sealed class BlueprintDescriptionText
{
    public const int MaximumDescriptionLength = 2000;

    public string Power { get; }
    public string Warnings { get; }
    public string Input { get; }
    public string Output { get; }
    public string Research { get; }
    public string Description { get; }

    public BlueprintDescriptionText(string power, string warnings, string input, string output, string research,
        string inputLine, string outputLine, string researchLine)
    {
        Power = power;
        Warnings = warnings;
        Input = input;
        Output = output;
        Research = research;
        var lines = new List<string>();
        if (warnings.Length > 0) lines.Add(warnings);
        if (input.Length > 0) lines.Add(inputLine + input);
        if (output.Length > 0) lines.Add(outputLine + output);
        if (research.Length > 0) lines.Add(researchLine + research);
        Description = string.Join("\n", lines);
    }
}

public static class BlueprintDescriptionFormatter
{
    private const double Tolerance = 1e-9;
    private static readonly string[] PowerUnits = { "W", "kW", "MW", "GW", "TW" };

    public static BlueprintDescriptionText Format(ProductionReport report, Func<int, string> itemTag,
        BlueprintDescriptionLabels labels)
    {
        if (report == null || report.Status == ProductionStatus.Failed ||
            report.Status == ProductionStatus.DataNotReady)
            throw new InvalidOperationException("A usable blueprint production report is required.");
        if (itemTag == null) throw new ArgumentNullException(nameof(itemTag));
        if (labels == null) throw new ArgumentNullException(nameof(labels));

        var breakdown = report.PowerBreakdown ??
                        throw new InvalidOperationException("A blueprint power breakdown is required.");
        var power = new List<string>();
        if (breakdown.KnownFactoryConsumptionWatts > 0 || !breakdown.FactoryComplete)
            power.Add(labels.FactoryPower + " " + FormatPower(breakdown.FactoryConsumptionWatts, labels));
        if (breakdown.KnownLogisticsConsumptionWatts > 0 || !breakdown.LogisticsComplete)
            power.Add(labels.LogisticsPower + " " + FormatPower(breakdown.LogisticsConsumptionWatts, labels));
        var missing = new List<string>();
        var excess = new List<string>();
        var input = new List<string>();
        var output = new SortedDictionary<int, double>();
        var research = new SortedDictionary<int, double>();
        foreach (var flow in report.ItemFlows.Values.OrderBy(flow => flow.ItemId))
        {
            var shortage = Math.Min(flow.IntermediateShortage,
                flow.SteadyStateExternalSupply ?? flow.RequiredExternalSupply);
            if (shortage > Tolerance)
                missing.Add(FormatItem(flow.ItemId, shortage, itemTag));
        }

        if (report.MaterialComplete)
        {
            foreach (var flow in report.ItemFlows.Values.OrderBy(flow => flow.ItemId))
            {
                if (flow.OverbuildSurplus.GetValueOrDefault() > Tolerance)
                    excess.Add(FormatItem(flow.ItemId, flow.OverbuildSurplus.Value, itemTag));
            }
        }

        foreach (var flow in report.ItemFlows.Values.OrderBy(flow => flow.ItemId))
        {
            var requiredSupply = flow.SteadyStateExternalSupply ?? flow.RequiredExternalSupply;
            if (requiredSupply > Tolerance && flow.IntermediateShortage <= Tolerance)
                input.Add(FormatItem(flow.ItemId, requiredSupply, itemTag));

            var rate = flow.IsFinalProduct ? Math.Max(0, flow.NetFlow) : 0;
            if (report.MaterialComplete) rate += flow.CoproductSurplus.GetValueOrDefault();
            if (rate > Tolerance)
            {
                if (flow.IsResearchProduct) research[flow.ItemId] = rate;
                else output[flow.ItemId] = rate;
            }
        }

        var formattedOutput = output.Select(flow => FormatItem(flow.Key, flow.Value, itemTag));
        var formattedResearch = research.Select(flow => FormatItem(flow.Key, flow.Value, itemTag));
        var warnings = new List<string>();
        if (missing.Count > 0)
            warnings.Add(labels.Missing + FormatMaterial(missing, report.MaterialComplete, labels));
        if (excess.Count > 0)
            warnings.Add(labels.Excess + FormatMaterial(excess, report.MaterialComplete, labels));
        return new BlueprintDescriptionText(string.Join(" | ", power),
            string.Join("\n", warnings),
            FormatMaterial(input, report.MaterialComplete, labels),
            FormatMaterial(formattedOutput, report.MaterialComplete, labels),
            FormatMaterial(formattedResearch, report.MaterialComplete, labels),
            labels.InputLine, labels.OutputLine, labels.ResearchLine);
    }

    private static string FormatMaterial(IEnumerable<string> entries, bool complete,
        BlueprintDescriptionLabels labels)
    {
        var text = string.Join(", ", entries);
        if (text.Length == 0) return "";
        return complete ? text : text + " " + labels.KnownPortion;
    }

    private static string FormatItem(int itemId, double rate, Func<int, string> itemTag)
    {
        var tag = itemTag(itemId);
        if (string.IsNullOrEmpty(tag))
            throw new InvalidOperationException("An item in the blueprint report has no native icon tag.");
        return tag + " x" + FormatNumber(rate);
    }

    private static string FormatPower(double? watts, BlueprintDescriptionLabels labels)
    {
        if (!watts.HasValue) return labels.Unknown;
        var value = watts.Value;
        var unit = 0;
        // Choose the unit after rounding so that 999.999 W displays as 1kW rather than 1000W.
        while (unit < PowerUnits.Length - 1 && RoundForDisplay(value) >= 1000)
        {
            value /= 1000;
            unit++;
        }

        return FormatNumber(value) + PowerUnits[unit];
    }

    private static double RoundForDisplay(double value)
    {
        return Math.Round(value, value < 1 ? 3 : 2, MidpointRounding.AwayFromZero);
    }

    private static string FormatNumber(double value)
    {
        if (!FiniteNonnegative(value))
            throw new InvalidOperationException("A blueprint report contains an invalid rate or power value.");
        var rounded = RoundForDisplay(value);
        if (rounded > 0 || value == 0)
            return rounded.ToString(value < 1 ? "0.###" : "0.##", CultureInfo.InvariantCulture);
        var precise = value.ToString("0.######", CultureInfo.InvariantCulture);
        return precise != "0" ? precise : value.ToString("0.###E+0", CultureInfo.InvariantCulture);
    }
}

public sealed class BlueprintDescriptionField
{
    public string Title { get; }
    public string Value { get; }
    public IReadOnlyList<string> MatchingTitles { get; }

    public BlueprintDescriptionField(string title, string value, params string[] matchingTitles)
    {
        Title = title;
        Value = value;
        MatchingTitles = Array.AsReadOnly(matchingTitles);
    }

    public bool Matches(string title)
    {
        return string.Equals(Title, title, StringComparison.Ordinal) ||
               MatchingTitles.Any(candidate => string.Equals(candidate, title, StringComparison.Ordinal));
    }
}

public enum BlueprintFieldUpdateResult
{
    Updated,
    CapacityExceeded,
    InvalidFields
}

public static class BlueprintDescriptionFields
{
    public static bool IsNativeFormatValid(string serialized)
    {
        if (string.IsNullOrEmpty(serialized)) return true;
        if (!serialized.EndsWith(";", StringComparison.Ordinal)) return false;
        var parts = serialized.Split(';');
        for (var index = 0; index < parts.Length - 1; index++)
        {
            var delimiter = parts[index].IndexOf(':');
            if (delimiter < 0 || parts[index].IndexOf(':', delimiter + 1) >= 0) return false;
        }

        return true;
    }

    // Updates the first matching field in place and drops other matches; a null value removes them all. A new field
    // is appended only when it fits, so other fields are never removed to make room.
    public static BlueprintFieldUpdateResult TryUpdate(string existing, BlueprintDescriptionField field,
        int maximumFields, Func<string, string> escape, Func<string, string> unescape, out string updated)
    {
        updated = existing;
        if (field == null || escape == null || unescape == null)
            throw new ArgumentNullException(field == null ? nameof(field) : escape == null ? nameof(escape) : nameof(unescape));
        if (maximumFields < 0 || !IsNativeFormatValid(existing))
            return BlueprintFieldUpdateResult.InvalidFields;

        var generated = field.Value == null ? null : escape(field.Title) + ":" + escape(field.Value);
        var parts = (existing ?? "").Split(';');
        var entries = new List<string>();
        var replaced = false;
        for (var index = 0; index < parts.Length - 1; index++)
        {
            if (!field.Matches(unescape(parts[index].Substring(0, parts[index].IndexOf(':')))))
            {
                entries.Add(parts[index]);
            }
            else if (generated != null && !replaced)
            {
                entries.Add(generated);
                replaced = true;
            }
        }

        if (generated != null && !replaced)
        {
            if (entries.Count >= maximumFields) return BlueprintFieldUpdateResult.CapacityExceeded;
            entries.Add(generated);
        }

        updated = string.Concat(entries.Select(entry => entry + ";"));
        return BlueprintFieldUpdateResult.Updated;
    }
}

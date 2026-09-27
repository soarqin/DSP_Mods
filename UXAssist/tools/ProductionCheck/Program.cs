using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CheatEnabler.Patches.Factory;
using UXAssist.Common.GameConstants;
using UXAssist.Production;

internal static class Program
{
    private static int _checks;

    private static void Main()
    {
        OrdinaryEvaluation();
        CoproductsAndTargetOrder();
        IntermediateBoundary();
        ExternalSurplusAndNaturalOverrides();
        ClosedLoopAndInfeasible();
        LargeRatesConserve();
        ProliferatorSupply();
        ProliferatorGradesAndFinishedSpraying();
        BlackBoxBalancesAndThreshold();
        BlackBoxMixedModesAndDeficits();
        BlueprintJointProduction();
        BlueprintSteadyStateInputs();
        BlueprintMixedProducersAndCycles();
        BlueprintProliferationAndCompleteness();
        BlueprintDescriptionFormattingAndFields();
        LogisticsChargingPower();
        FractionationFlow();
        AccumulatorRoundTrip();
        RenewableGeneration();
        MiningResources();
        GasCollection();
        FuelGenerators();
        RayReceiverPowerAndPhotons();
        LauncherCyclesAndAmmunition();
        ResearchHashesAndMatrices();
        PlannerAuxiliaryPower();
        InvalidConfiguration();
        BeltSignalStatistics();
        BeltSignalPreset();
        Console.WriteLine($"ProductionCheck: {_checks} checks passed.");
    }

    private static void OrdinaryEvaluation()
    {
        var catalog = Catalog();
        var evaluator = new ProcessEvaluator();
        var recipe = catalog.Recipes[101];
        var building = catalog.Buildings[10];
        Check(evaluator.TryEvaluate(catalog, recipe, building, ProliferationMode.None, 0, null,
            out var plain, out _), "ordinary process evaluates");
        Equal(1, plain.CyclesPerBuildingPerMinute, "a 3600-tick recipe completes one cycle per minute");
        var oneSecond = new ProductionRecipe(105, ProductionRecipeCategory.Assemble, 60, true, recipe.Inputs,
            recipe.Outputs);
        Check(evaluator.TryEvaluate(catalog, oneSecond, building, ProliferationMode.None, 0, null,
            out var nativeSecond, out _), "one-second process evaluates");
        Equal(60, nativeSecond.CyclesPerBuildingPerMinute,
            "native 60 ticks per second give a 60-tick recipe 60 cycles per minute");
        Equal(1000, plain.WorkingPowerWatts, "working watts");
        Check(evaluator.TryEvaluate(catalog, recipe, building, ProliferationMode.Speedup, 4, null,
            out var speedup, out _), "speedup process evaluates");
        Equal(2, speedup.CyclesPerBuildingPerMinute, "speedup capacity");
        Equal(1000, speedup.WorkingPowerWatts / 2.5, "speedup power multiplier");
        Check(evaluator.TryEvaluate(catalog, recipe, building, ProliferationMode.ExtraProducts, 4, null,
            out var extra, out _), "extra process evaluates");
        Equal(2.5, extra.OutputsPerCycle[2], "extra product count");
        Equal(1, extra.InputsPerCycle[1], "extra mode input count");
        Equal(1, extra.CyclesPerBuildingPerMinute, "extra mode speed");
    }

    private static void CoproductsAndTargetOrder()
    {
        var planner = new ProductionPlanner(Catalog());
        var request = Request((3, 60), (4, 30));
        var first = planner.Calculate(request);
        Check(first.MaterialComplete && first.PowerComplete, "multi-target plan complete");
        Equal(30, first.ItemFlows[1].ExternalImports, "ore import reused across targets");
        Equal(0, first.Groups.Where(group => group.Process.RecipeId == 103)
            .Sum(group => group.ExecutionsPerMinute), "selected but unnecessary recipe unused");
        Equal(30, first.ItemFlows[4].Delivered, "coproduct target delivered once");
        Equal(60, first.ItemFlows[3].GrossProduction, "first product produced");

        var reverse = planner.Calculate(Request((4, 30), (3, 60)));
        foreach (var flow in first.ItemFlows)
        {
            Equal(flow.Value.GrossProduction, reverse.ItemFlows[flow.Key].GrossProduction, "target order production");
            Equal(flow.Value.ExternalImports, reverse.ItemFlows[flow.Key].ExternalImports, "target order imports");
        }

        var duplicate = planner.Calculate(Request((3, 15), (3, 45), (4, 30)));
        Equal(60, duplicate.ItemFlows[3].Delivered, "duplicate targets merged");
        Equal(30, duplicate.ItemFlows[1].ExternalImports, "duplicate targets do not duplicate inputs");
    }

    private static void IntermediateBoundary()
    {
        var request = Request((3, 60));
        request.ExternalMaterials.Add(2);
        var report = new ProductionPlanner(Catalog()).Calculate(request);
        Check(report.MaterialComplete, "intermediate boundary complete");
        Equal(60, report.ItemFlows[2].ExternalImports, "intermediate supplied externally");
        Check(!report.ItemFlows.ContainsKey(1), "upstream ore no longer required");
        Equal(60000, report.Power.ConsumptionWatts, "power is based on utilized building capacity");
    }

    private static void ExternalSurplusAndNaturalOverrides()
    {
        var planner = new ProductionPlanner(Catalog());
        var request = Request((3, 60), (4, 30));
        request.KnownExternalSurplus[2] = 20;
        request.KnownExternalSurplus[4] = 10;
        var report = planner.Calculate(request);
        Check(report.MaterialComplete, "joint plan accepts two known external supplies");
        Equal(20, report.ItemFlows[1].ExternalImports,
            "known intermediate supply displaces upstream ore imports");
        Equal(20, report.ItemFlows[2].KnownExternalSupply,
            "known intermediate is not counted as a mined external import");
        Equal(10, report.ItemFlows[4].Surplus,
            "external coproduct supply becomes a visible final surplus");
        Equal(0, report.Groups.Where(group => group.Process.RecipeId == 103)
            .Sum(group => group.ExecutionsPerMinute), "external surplus does not introduce an extra recipe");
        foreach (var flow in report.ItemFlows.Values)
            Equal(flow.Delivered + flow.Surplus,
                flow.NetFlow + flow.ExternalImports + flow.KnownExternalSupply,
                $"item {flow.ItemId} conserves total production and supply");

        request.ExternalMaterials.Add(2);
        var boundary = planner.Calculate(request);
        Equal(40, boundary.ItemFlows[2].ExternalImports,
            "external intermediate boundary stops upstream expansion after known supply");
        Equal(0, boundary.ItemFlows[1].ExternalImports,
            "unused coproduct recipe does not import upstream ore across the boundary");

        var catalog = new ProductionCatalog(new[]
            {
                new ProductionItem(11, 301, true, false, 0, 0),
                new ProductionItem(12, 0, true, false, 0, 0),
                new ProductionItem(13, 302, false, true, 0, 0)
            }, new[]
            {
                Recipe(301, new[] { (12, 2.0) }, new[] { (11, 1.0) }),
                Recipe(302, new[] { (12, 2.0) }, new[] { (13, 1.0) })
            }, Array.Empty<ProductionBuilding>(), new[] { 0.0 }, new[] { 0.0 }, new[] { 1.0 });
        var nativeResource = new ProductionPlanner(catalog);
        var natural = nativeResource.Calculate(new ProductionPlanRequest
        {
            Targets = { new ProductionTarget(11, 1) }
        });
        Equal(1, natural.ItemFlows[11].ExternalImports,
            "natural item is external even when a default manufacturing recipe exists");
        var explicitRecipe = new ProductionPlanRequest();
        explicitRecipe.Targets.Add(new ProductionTarget(11, 1));
        explicitRecipe.RecipeByItem[11] = 301;
        var manufactured = nativeResource.Calculate(explicitRecipe);
        Equal(2, manufactured.ItemFlows[12].ExternalImports,
            "explicit natural-item recipe uses its actual inputs");
        Check(!manufactured.ItemFlows[11].ExternalImports.Equals(1),
            "explicit natural-item recipe replaces natural import");
        var darkFog = new ProductionPlanRequest();
        darkFog.Targets.Add(new ProductionTarget(13, 1));
        darkFog.RecipeByItem[13] = 302;
        Equal(1, nativeResource.Calculate(darkFog).ItemFlows[13].ExternalImports,
            "Dark Fog resource remains external even with an explicit recipe");
    }

    private static void ClosedLoopAndInfeasible()
    {
        var items = new[]
        {
            new ProductionItem(11, 201, false, false, 0, 0),
            new ProductionItem(12, 202, false, false, 0, 0)
        };
        var productive = new[]
        {
            Recipe(201, new[] { (12, 1.0) }, new[] { (11, 2.0) }),
            Recipe(202, new[] { (11, 1.0) }, new[] { (12, 2.0) })
        };
        var catalog = new ProductionCatalog(items, productive, Array.Empty<ProductionBuilding>(),
            new[] { 0.0 }, new[] { 0.0 }, new[] { 1.0 });
        var result = new ProductionPlanner(catalog).Calculate(Request((11, 3)));
        Check(result.MaterialComplete && !result.PowerComplete, "cyclic material plan with unspecified power");
        Equal(2, result.Groups.Single(group => group.Process.RecipeId == 201).ExecutionsPerMinute,
            "cyclic first process rate");
        Equal(1, result.Groups.Single(group => group.Process.RecipeId == 202).ExecutionsPerMinute,
            "cyclic second process rate");
        Equal(0, result.ItemFlows[12].Surplus, "cyclic intermediate balance");

        var noGain = new[]
        {
            Recipe(201, new[] { (12, 1.0) }, new[] { (11, 1.0) }),
            Recipe(202, new[] { (11, 1.0) }, new[] { (12, 1.0) })
        };
        var impossible = new ProductionCatalog(items, noGain, Array.Empty<ProductionBuilding>(),
            new[] { 0.0 }, new[] { 0.0 }, new[] { 1.0 });
        var failure = new ProductionPlanner(impossible).Calculate(Request((11, 3)));
        Check(failure.Diagnostics.Single().Code == ProductionDiagnosticCode.Infeasible, "infeasible loop diagnosed");
    }

    private static void LargeRatesConserve()
    {
        // Uneven ratios, coproducts, and spraying accumulate rounding that scales with the ledger rates.
        var random = new Random(12345);
        var items = Enumerable.Range(1, 8).Select(id => new ProductionItem(id, 0, true, false, 0, 0)).ToList();
        items.Add(new ProductionItem(9, 909, false, false, 4, 60));
        var recipes = new List<ProductionRecipe>
        {
            new ProductionRecipe(909, ProductionRecipeCategory.Assemble, 3600, true,
                new[] { new KeyValuePair<int, double>(1, 1.7), new KeyValuePair<int, double>(2, 0.9) },
                new[] { new KeyValuePair<int, double>(9, 1) })
        };
        for (var id = 10; id < 70; id++)
        {
            items.Add(new ProductionItem(id, 1000 + id, false, false, 0, 0));
            var inputs = new Dictionary<int, double>();
            for (var count = random.Next(1, 4); count > 0; count--)
            {
                var input = random.Next(1, id);
                if (input == 9) input = 1;
                inputs[input] = inputs.GetValueOrDefault(input) + Math.Round(random.NextDouble() * 5 + 0.1, 3);
            }

            var outputs = new Dictionary<int, double> { [id] = Math.Round(random.NextDouble() * 3 + 0.2, 3) };
            if (random.NextDouble() < 0.3 && id > 11)
                outputs[random.Next(10, id)] = Math.Round(random.NextDouble() + 0.05, 3);
            recipes.Add(new ProductionRecipe(1000 + id, ProductionRecipeCategory.Assemble,
                60 + random.Next(0, 600), random.NextDouble() < 0.7, inputs, outputs));
        }

        var catalog = new ProductionCatalog(items, recipes,
            new[] { new ProductionBuilding(10, ProductionRecipeCategory.Assemble, 1.5, 270000, 12000) },
            new[] { 0.0, 0.25, 0.5, 0.75, 1.0 },
            new[] { 0.0, 0.125, 0.2, 0.225, 0.25 },
            new[] { 1.0, 1.3, 1.7, 2.1, 2.5 });
        foreach (var scale in new[] { 1e-3, 1, 1e7 })
        {
            var request = Request((69, 7.3 * scale), (65, 3.1 * scale), (58, 1.9 * scale));
            request.ProliferationEnabled = true;
            request.ProliferatorItemId = 9;
            request.SelfSprayProliferator = true;
            request.SprayDeliveredItems.Add(69);
            var report = new ProductionPlanner(catalog).Calculate(request);
            Check(report.MaterialComplete && report.PowerComplete,
                $"a plan scaled by {scale} conserves every ledger row");
        }
    }

    private static void ProliferatorSupply()
    {
        var planner = new ProductionPlanner(Catalog());
        var request = Request((2, 2));
        request.ProliferationEnabled = true;
        request.ProliferatorItemId = 5;
        request.SelfSprayProliferator = true;
        var report = planner.Calculate(request);
        Check(report.MaterialComplete, "self-spraying plan complete");
        var sprayDemand = report.Groups.Sum(group => group.Process.ProliferationMode == ProliferationMode.None
            ? 0 : group.Process.InputsPerCycle.Values.Sum() * group.ExecutionsPerMinute) / 74;
        Equal(sprayDemand, report.Groups.Sum(group => group.GrossConsumption.GetValueOrDefault(5) -
            group.Process.InputsPerCycle.GetValueOrDefault(5) * group.ExecutionsPerMinute),
            "spray demand included in ledger");
        Equal(0, report.ItemFlows[5].Surplus, "spray production balances use");

        request.SprayDeliveredItems.Add(2);
        var finished = planner.Calculate(request);
        Equal(2.0 / 74, finished.ItemFlows[5].GrossConsumption -
            finished.Groups.Sum(group => group.GrossConsumption.GetValueOrDefault(5)),
            "finished product spraying charged separately");
    }

    private static void ProliferatorGradesAndFinishedSpraying()
    {
        var grades = new[]
        {
            (id: 11, level: 1, sprays: 12, effective: 12.0, bonus: 0.125),
            (id: 12, level: 2, sprays: 24, effective: 27.0, bonus: 0.2),
            (id: 13, level: 3, sprays: 60, effective: 74.0, bonus: 0.25)
        };
        var items = new List<ProductionItem>
        {
            new ProductionItem(1, 0, true, false, 0, 0),
            new ProductionItem(2, 101, false, false, 0, 0)
        };
        var recipes = new List<ProductionRecipe>
        {
            Recipe(101, new[] { (1, 1.0) }, new[] { (2, 1.0) })
        };
        foreach (var grade in grades)
        {
            items.Add(new ProductionItem(grade.id, 200 + grade.level,
                false, false, grade.level, grade.sprays));
            recipes.Add(Recipe(200 + grade.level, new[] { (1, 2.0) },
                new[] { (grade.id, 1.0) }, productive: false));
        }

        var catalog = new ProductionCatalog(items, recipes, Array.Empty<ProductionBuilding>(),
            new[] { 0.0, 0.25, 0.5, 0.75, 1.0 },
            new[] { 0.0, 0.125, 0.2, 0.25, 0.3 },
            new[] { 1.0, 1.3, 1.7, 2.1, 2.5 });
        Equal(3, catalog.MaximumProliferationLevel,
            "available bottles, not the last table index, set maximum proliferation level");

        foreach (var grade in grades)
        {
            var request = new ProductionPlanRequest
            {
                ProliferationEnabled = true,
                ProliferatorItemId = grade.id,
                SelfSprayProliferator = true
            };
            request.Targets.Add(new ProductionTarget(2, 1));
            request.ProliferationModeByItem[grade.id] = ProliferationMode.None;
            var planner = new ProductionPlanner(catalog);
            var intermediateOnly = planner.Calculate(request);
            Check(intermediateOnly.MaterialComplete,
                $"grade {grade.level} can manufacture its own proliferator supply");

            request.SprayDeliveredItems.Add(2);
            var selfSprayed = planner.Calculate(request);
            var targetCycles = 1 / (1 + grade.bonus);
            var bottlesUsed = (targetCycles + 1) / grade.effective;
            Check(selfSprayed.MaterialComplete,
                $"grade {grade.level} self-sprayed supply remains feasible");
            Equal(bottlesUsed, selfSprayed.ItemFlows[grade.id].GrossConsumption,
                $"grade {grade.level} bottle use includes finished-product spray exactly once");
            Equal(bottlesUsed, selfSprayed.ItemFlows[grade.id].GrossProduction,
                $"grade {grade.level} proliferator manufacturing balances sprayed demand");
            Equal(1 / grade.effective,
                selfSprayed.ItemFlows[grade.id].GrossConsumption -
                intermediateOnly.ItemFlows[grade.id].GrossConsumption,
                $"grade {grade.level} finished-product spray has no duplicate charge");
            Equal(targetCycles + 2 * bottlesUsed, selfSprayed.ItemFlows[1].ExternalImports,
                $"grade {grade.level} manufacturing uses recipe-derived raw materials");

            request.SelfSprayProliferator = false;
            var untreated = planner.Calculate(request);
            Equal((targetCycles + 1) / grade.sprays,
                untreated.ItemFlows[grade.id].GrossConsumption,
                $"grade {grade.level} disabled self-spraying uses base bottle capacity");
        }
    }

    private static void InvalidConfiguration()
    {
        var planner = new ProductionPlanner(null);
        Check(planner.Calculate(Request((2, 1))).Status == ProductionStatus.DataNotReady,
            "preload not ready reported");
        var catalog = Catalog();
        var evaluator = new ProcessEvaluator();
        var nonproductive = Recipe(301, new[] { (1, 1.0) }, new[] { (2, 1.0) }, false);
        Check(!evaluator.TryEvaluate(catalog, nonproductive, null, ProliferationMode.ExtraProducts, 4,
            null, out _, out var diagnostic) &&
              diagnostic.Code == ProductionDiagnosticCode.UnsupportedProliferation,
            "nonproductive recipe rejects extra-products mode");
        var request = Request((2, 1));
        request.BuildingByRecipe.Clear();
        request.BuildingByCategory.Clear();
        var partial = new ProductionPlanner(catalog).Calculate(request);
        Check(partial.MaterialComplete && !partial.PowerComplete && partial.Power == null,
            "missing building does not invent watts");
        var cancelled = new CancellationToken(true);
        Check(new ProductionPlanner(catalog).Calculate(Request((2, 1)), cancelled)
                  .Diagnostics.Single().Code == ProductionDiagnosticCode.Cancelled, "cancellation diagnosed");
        var emptyPlan = new ProductionPlanner(catalog).Calculate(new ProductionPlanRequest());
        Check(emptyPlan.MaterialComplete && emptyPlan.PowerComplete && emptyPlan.ItemFlows.Count == 0,
            "empty production plan is a complete empty report");
        var emptyBox = new FactoryBlackBoxAnalyzer(catalog).Analyze(new FactoryBlackBoxRequest());
        Check(emptyBox.MaterialComplete && emptyBox.PowerComplete && emptyBox.Groups.Count == 0,
            "empty building selection is a complete empty report");
        var staleCapture = new FactoryBlackBoxRequest();
        staleCapture.CaptureDiagnostics.Add(new ProductionDiagnostic(ProductionDiagnosticCode.InvalidRequest,
            "A selected object no longer exists."));
        var incompleteBox = new FactoryBlackBoxAnalyzer(catalog).Analyze(staleCapture);
        Check(!incompleteBox.MaterialComplete && !incompleteBox.PowerComplete &&
              incompleteBox.Status == ProductionStatus.Failed,
            "a stale object discarded during capture cannot become a complete empty analysis");
    }

    private static void FractionationFlow()
    {
        var catalog = new ProductionCatalog(new[]
        {
            new ProductionItem(1, 0, true, false, 0, 0),
            new ProductionItem(2, 501, false, false, 0, 0),
            new ProductionItem(3, 0, true, false, 4, 60)
        }, new[]
        {
            new ProductionRecipe(501, ProductionRecipeCategory.Fractionate, 1, false,
                new[] { new KeyValuePair<int, double>(1, 100) },
                new[] { new KeyValuePair<int, double>(2, 1) })
        }, new[]
        {
            new ProductionBuilding(30, ProductionRecipeCategory.None, 0, 1000, 100,
                ProductionBuildingKind.Fractionator)
        }, new[] { 0.0, 0.25, 0.5, 0.75, 1.0 }, new[] { 0.0, 0.125, 0.2, 0.225, 0.25 },
            new[] { 1.0, 1.3, 1.7, 2.1, 2.5 });
        var request = new FactoryBlackBoxRequest();
        request.Buildings.Add(new ProductionBuildingSnapshot(301, 30, 501, 1, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double>
            {
                ["CirculatingItemsPerMinute"] = 7200,
                ["StackSize"] = 4
            }));
        var analyzer = new FactoryBlackBoxAnalyzer(catalog);
        var plain = analyzer.Analyze(request);
        Check(plain.MaterialComplete && plain.PowerComplete, "native fractionation inputs valid");
        Equal(72, plain.ItemFlows[1].RequiredExternalSupply, "circulating fluid not an external import");
        Equal(72, plain.ItemFlows[2].GrossProduction, "native fractionation probability");
        Equal(5500, plain.Power.ConsumptionWatts, "native fractionation power uses items per second");
        Equal(7200, plain.Groups[0].Process.CirculatingItemsPerBuildingPerMinute,
            "fractionation reports circulation separately");

        request.ProliferationEnabled = true;
        var sprayed = analyzer.Analyze(request);
        Equal(144, sprayed.ItemFlows[2].GrossProduction, "native fractionation speedup probability");
        Equal(13750, sprayed.Power.ConsumptionWatts, "native fractionation proliferation power");

        request.ProliferationEnabled = false;
        request.Buildings[0] = new ProductionBuildingSnapshot(301, 30, 501, 1, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double>
            {
                ["CirculatingItemsPerMinute"] = 1800,
                ["StackSize"] = 1
            });
        Equal(1000, analyzer.Analyze(request).Power.ConsumptionWatts,
            "an unstacked full buffer does not raise fractionation power");

        var bad = new FactoryBlackBoxRequest();
        bad.Buildings.Add(new ProductionBuildingSnapshot(301, 30, 501, 1, ProliferationMode.None));
        Check(new FactoryBlackBoxAnalyzer(catalog).Analyze(bad).Diagnostics.Single().Code ==
              ProductionDiagnosticCode.MissingOperatingParameter, "missing throughput diagnosed");
    }

    private static void AccumulatorRoundTrip()
    {
        var catalog = new ProductionCatalog(new[]
        {
            new ProductionItem(10, 0, true, false, 0, 0),
            new ProductionItem(11, 601, false, false, 0, 0),
            new ProductionItem(6, 0, true, false, 4, 60)
        }, new[]
        {
            new ProductionRecipe(601, ProductionRecipeCategory.Exchange, 1, false,
                new[] { new KeyValuePair<int, double>(10, 1) },
                new[] { new KeyValuePair<int, double>(11, 1) })
        }, new[]
        {
            new ProductionBuilding(100, ProductionRecipeCategory.None, 0, 0, 0,
                ProductionBuildingKind.Exchanger, exchangeRateWatts: 3600,
                accumulatorEnergyJoules: 216000, emptyAccumulatorItemId: 10, fullAccumulatorItemId: 11)
        }, new[] { 0.0, 0.25, 0.5, 0.75, 1.0 },
            new[] { 0.0, 0.125, 0.2, 0.225, 0.25 },
            new[] { 1.0, 1.3, 1.7, 2.1, 2.5 });
        var request = new FactoryBlackBoxRequest();
        request.Buildings.Add(new ProductionBuildingSnapshot(1001, 100, 0, 1, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double> { ["Mode0"] = 1 }));
        request.Buildings.Add(new ProductionBuildingSnapshot(1002, 100, 0, 1, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double> { ["Mode0"] = -1 }));
        var analyzer = new FactoryBlackBoxAnalyzer(catalog);
        var plain = analyzer.Analyze(request);
        Check(plain.MaterialComplete && plain.PowerComplete, "accumulator round trip complete");
        Equal(1, plain.Groups[0].GrossProduction[11], "native charge item rate");
        Equal(1, plain.Groups[1].GrossProduction[10], "native discharge item rate");
        Equal(0, plain.ItemFlows[10].NetFlow, "empty accumulator balances");
        Equal(0, plain.ItemFlows[11].NetFlow, "full accumulator balances");
        Equal(3600, plain.Power.ConsumptionWatts, "charging draws grid power");
        Equal(3600, plain.Power.AccumulatorChargingWatts, "charging capacity separately reported");
        Equal(3600, plain.Power.AccumulatorDischargingWatts, "discharging capacity separately reported");
        Equal(0, plain.Power.GenerationWatts, "discharging is not generation");

        request.ProliferationEnabled = true;
        var sprayed = analyzer.Analyze(request);
        Equal(2, sprayed.Groups[0].GrossProduction[11], "sprayed charge speed");
        Equal(2, sprayed.Groups[1].GrossProduction[10], "sprayed discharge speed");
        Equal(7200, sprayed.Power.AccumulatorChargingWatts, "sprayed charge power");
        Equal(7200, sprayed.Power.AccumulatorDischargingWatts, "sprayed discharge capacity");
        Check(!sprayed.ItemFlows.ContainsKey(6), "black box assumes externally sprayed accumulators");

        var plan = Request((11, 2));
        plan.BuildingByRecipe[601] = 100;
        plan.ProliferationEnabled = true;
        plan.ProliferatorItemId = 6;
        var result = new ProductionPlanner(catalog).Calculate(plan);
        Check(result.MaterialComplete && result.PowerComplete, "accumulator charging plan complete");
        Equal(2, result.ItemFlows[10].ExternalImports, "empty accumulators imported once");
        Equal(2.0 / 60, result.ItemFlows[6].ExternalImports, "spray charged for charging inputs");
        Equal(7200, result.Power.AccumulatorChargingWatts, "planner reports grid charging demand");
    }

    private static void RenewableGeneration()
    {
        var catalog = new ProductionCatalog(Array.Empty<ProductionItem>(), Array.Empty<ProductionRecipe>(),
            new[]
            {
                new ProductionBuilding(90, ProductionRecipeCategory.None, 0, 0, 0,
                    ProductionBuildingKind.RenewableGenerator, ratedGenerationWatts: 3600,
                    renewableSource: RenewablePowerSource.Wind),
                new ProductionBuilding(91, ProductionRecipeCategory.None, 0, 0, 0,
                    ProductionBuildingKind.RenewableGenerator, ratedGenerationWatts: 5400,
                    renewableSource: RenewablePowerSource.Solar),
                new ProductionBuilding(92, ProductionRecipeCategory.None, 0, 0, 0,
                    ProductionBuildingKind.RenewableGenerator, ratedGenerationWatts: 7200,
                    renewableSource: RenewablePowerSource.Geothermal)
            }, new[] { 0.0 }, new[] { 0.0 }, new[] { 1.0 });
        var request = new FactoryBlackBoxRequest();
        request.Buildings.Add(new ProductionBuildingSnapshot(90, 90, 0, 1, ProliferationMode.None));
        request.Buildings.Add(new ProductionBuildingSnapshot(91, 91, 0, 1, ProliferationMode.None));
        request.Buildings.Add(new ProductionBuildingSnapshot(92, 92, 0, 1, ProliferationMode.None));
        var report = new FactoryBlackBoxAnalyzer(catalog).Analyze(request);
        Check(report.MaterialComplete && report.PowerComplete && report.Diagnostics.Count == 0,
            "ideal renewable generation complete");
        Equal(16200, report.Power.GenerationWatts, "wind solar geothermal use ideal rated generation");
        Equal(0, report.Power.ConsumptionWatts, "renewable generation is not demand");
        Check(report.ItemFlows.Count == 0, "renewable generators do not invent products");
    }

    private static void MiningResources()
    {
        var catalog = new ProductionCatalog(new[]
        {
            new ProductionItem(1, 0, true, false, 0, 0),
            new ProductionItem(2, 0, true, false, 0, 0),
            new ProductionItem(3, 0, true, false, 0, 0)
        }, Array.Empty<ProductionRecipe>(), new[]
        {
            new ProductionBuilding(20, ProductionRecipeCategory.None, 0, 1200, 0,
                ProductionBuildingKind.Miner, minerKind: ProductionMinerKind.Vein, miningPeriodTicks: 600000),
            new ProductionBuilding(21, ProductionRecipeCategory.None, 0, 1400, 0,
                ProductionBuildingKind.Miner, minerKind: ProductionMinerKind.Oil, miningPeriodTicks: 600000),
            new ProductionBuilding(22, ProductionRecipeCategory.None, 0, 1000, 0,
                ProductionBuildingKind.Miner, minerKind: ProductionMinerKind.Water, miningPeriodTicks: 600000)
        }, new[] { 0.0 }, new[] { 0.0 }, new[] { 1.0 });
        var request = new FactoryBlackBoxRequest { ProliferationEnabled = true };
        request.Buildings.Add(new ProductionBuildingSnapshot(20, 20, 0, 1, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double>
            {
                ["ResourceItemId"] = 1,
                ["VeinCount"] = 4,
                ["MiningSpeedMultiplier"] = 2
            }));
        request.Buildings.Add(new ProductionBuildingSnapshot(21, 21, 0, 1, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double>
            {
                ["ResourceItemId"] = 2,
                ["OilUnits"] = 2.5,
                ["MiningSpeedMultiplier"] = 2
            }));
        request.Buildings.Add(new ProductionBuildingSnapshot(22, 22, 0, 1, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double>
            {
                ["ResourceItemId"] = 3,
                ["MiningSpeedMultiplier"] = 2
            }));
        var analyzer = new FactoryBlackBoxAnalyzer(catalog);
        var report = analyzer.Analyze(request);
        Check(report.MaterialComplete && report.PowerComplete, "native vein oil water mining complete");
        Equal(480, report.ItemFlows[1].GrossProduction, "vein coverage multiplies native rate");
        Equal(300, report.ItemFlows[2].GrossProduction, "oil resource factor retained");
        Equal(120, report.ItemFlows[3].GrossProduction, "water pump uses planet resource type");
        Equal(3600, report.Power.ConsumptionWatts, "each mining facility draws working power");
        Check(!report.ItemFlows.ContainsKey(0), "miners never invent zero-ID resources");

        request.Buildings[0] = new ProductionBuildingSnapshot(20, 20, 0, 1, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double>
            {
                ["ResourceItemId"] = 1,
                ["VeinCount"] = 4,
                ["MiningSpeedMultiplier"] = 2,
                ["MachineSpeedFactor"] = 1.5,
                ["SpeedDamper"] = 0.02
            });
        var fast = analyzer.Analyze(request);
        Equal(720, fast.ItemFlows[1].GrossProduction,
            "machine speed scales output while a transient output damper is ignored at full load");
        Equal(1200 * 2.25 + 1400 + 1000, fast.Power.ConsumptionWatts,
            "miner power follows the square of machine speed");

        request.Buildings[0] = new ProductionBuildingSnapshot(20, 20, 0, 1, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double>
            {
                ["ResourceItemId"] = 1,
                ["MiningSpeedMultiplier"] = 2
            });
        var missing = analyzer.Analyze(request);
        Check(!missing.MaterialComplete && !missing.PowerComplete,
            "unknown vein coverage does not claim a complete report");
        Check(missing.Diagnostics.Single().Code == ProductionDiagnosticCode.MissingOperatingParameter,
            "missing vein coverage diagnosed");
    }

    private static void FuelGenerators()
    {
        var catalog = new ProductionCatalog(new[]
        {
            new ProductionItem(10, 0, true, false, 0, 0, heatValueJoules: 216000,
                fuelTypeMask: 1, productiveFuel: true),
            new ProductionItem(11, 0, true, false, 0, 0, heatValueJoules: 216000,
                fuelTypeMask: 1),
            new ProductionItem(12, 0, true, false, 0, 0, heatValueJoules: 216000,
                fuelTypeMask: 2),
            new ProductionItem(1804, 0, true, false, 0, 0, heatValueJoules: 1000000,
                fuelTypeMask: 4, productiveFuel: true, starOutputMultiplier: 2),
            new ProductionItem(50, 0, true, false, 4, 60)
        }, Array.Empty<ProductionRecipe>(), new[]
        {
            new ProductionBuilding(30, ProductionRecipeCategory.None, 0, 0, 0,
                ProductionBuildingKind.FuelGenerator, ratedGenerationWatts: 1000,
                fuelMask: 1, fuelUseWatts: 500),
            new ProductionBuilding(31, ProductionRecipeCategory.None, 0, 0, 0,
                ProductionBuildingKind.FuelGenerator, ratedGenerationWatts: 10000,
                fuelMask: 4, fuelUseWatts: 5000)
        }, new[] { 0.0, 0.25, 0.5, 0.75, 1.0 },
            new[] { 0.0, 0.125, 0.2, 0.225, 0.25 },
            new[] { 1.0, 1.3, 1.7, 2.1, 2.5 });
        var analyzer = new FactoryBlackBoxAnalyzer(catalog);
        var request = new FactoryBlackBoxRequest();
        request.Buildings.Add(new ProductionBuildingSnapshot(30, 30, 0, 1, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double> { ["FuelItemId"] = 10 }));
        var plain = analyzer.Analyze(request);
        Check(plain.MaterialComplete && plain.PowerComplete, "native fuel generator complete");
        Equal(1000, plain.Power.GenerationWatts, "rated generator output without proliferation");
        Equal(500.0 * 60 / 216000, plain.ItemFlows[10].GrossConsumption,
            "productive fuel burn rate without proliferation");

        request.ProliferationEnabled = true;
        var sprayedProductive = analyzer.Analyze(request);
        Equal(1250, sprayedProductive.Power.GenerationWatts,
            "productive fuel increases generator output by extra-products bonus");
        Equal(plain.ItemFlows[10].GrossConsumption, sprayedProductive.ItemFlows[10].GrossConsumption,
            "productive fuel output gain does not burn extra fuel");
        Check(!sprayedProductive.ItemFlows.ContainsKey(50), "black box does not manufacture sprays");

        request.Buildings[0] = new ProductionBuildingSnapshot(30, 30, 0, 1, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double> { ["FuelItemId"] = 11 });
        var sprayedOrdinary = analyzer.Analyze(request);
        Equal(2000, sprayedOrdinary.Power.GenerationWatts, "ordinary fuel acceleration increases output");
        Equal(500.0 * 2 * 60 / 216000, sprayedOrdinary.ItemFlows[11].GrossConsumption,
            "ordinary fuel acceleration also burns more fuel");

        request.Buildings[0] = new ProductionBuildingSnapshot(30, 30, 0, 1, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double> { ["FuelItemId"] = 12 });
        Check(analyzer.Analyze(request).Diagnostics.Single().Code ==
              ProductionDiagnosticCode.MissingOperatingParameter, "native fuel mask rejects incompatible fuel");

        var evaluator = new ProcessEvaluator();
        var invalidFuelMode = new Dictionary<string, double> { ["FuelItemId"] = 11 };
        Check(!evaluator.TryEvaluate(catalog, null, catalog.Buildings[30],
                  ProliferationMode.ExtraProducts, 4, invalidFuelMode, out _, out var fuelDiagnostic) &&
              fuelDiagnostic.Code == ProductionDiagnosticCode.UnsupportedProliferation,
            "explicit unsupported fuel productivity mode is rejected");

        request.ProliferationEnabled = false;
        var starSettings = new Dictionary<string, double>
        {
            ["FuelItemId"] = 1804,
            ["Mode0"] = 1,
            ["BoostEnabled"] = 1
        };
        request.Buildings[0] = new ProductionBuildingSnapshot(31, 31, 0, 1, ProliferationMode.None,
            operatingParameters: starSettings);
        var star = analyzer.Analyze(request);
        Equal(2000000, star.Power.GenerationWatts, "star boost and special fuel native multiplier");
        Equal(60, star.ItemFlows[1804].GrossConsumption,
            "star generation accounts for boosted fuel burn");

        starSettings["BoostEnabled"] = 0;
        request.Buildings[0] = new ProductionBuildingSnapshot(31, 31, 0, 1, ProliferationMode.None,
            operatingParameters: starSettings);
        Equal(20000, analyzer.Analyze(request).Power.GenerationWatts,
            "a stored star boost flag outside sandbox mode keeps only the special fuel multiplier");

        starSettings.Remove("BoostEnabled");
        request.Buildings[0] = new ProductionBuildingSnapshot(31, 31, 0, 1, ProliferationMode.None,
            operatingParameters: starSettings);
        var unknownBoost = analyzer.Analyze(request);
        Check(!unknownBoost.PowerComplete && unknownBoost.Diagnostics.Single().Code ==
              ProductionDiagnosticCode.MissingOperatingParameter,
            "a boosted star blueprint needs the sandbox boost state");
    }

    private static void BlackBoxBalancesAndThreshold()
    {
        var items = new[]
        {
            new ProductionItem(1, 0, true, false, 0, 0),
            new ProductionItem(2, 401, false, false, 0, 0),
            new ProductionItem(3, 402, false, false, 0, 0)
        };
        var recipes = new[]
        {
            new ProductionRecipe(401, ProductionRecipeCategory.Assemble, 3600, true,
                new[] { new KeyValuePair<int, double>(1, 1) },
                new[] { new KeyValuePair<int, double>(2, 60) }),
            new ProductionRecipe(402, ProductionRecipeCategory.Assemble, 3600, true,
                new[] { new KeyValuePair<int, double>(2, 150) },
                new[] { new KeyValuePair<int, double>(3, 1) })
        };
        var catalog = new ProductionCatalog(items, recipes,
            new[]
            {
                new ProductionBuilding(10, ProductionRecipeCategory.Assemble, 1, 1000, 100),
                new ProductionBuilding(20, ProductionRecipeCategory.Assemble, 2, 3000, 300)
            }, new[] { 0.0, 1.0 }, new[] { 0.0, 0.25 }, new[] { 1.0, 2.5 });
        var analyzer = new FactoryBlackBoxAnalyzer(catalog);
        var request = new FactoryBlackBoxRequest();
        request.Buildings.Add(new ProductionBuildingSnapshot(100, 10, 401, 1, ProliferationMode.None));
        request.Buildings.Add(new ProductionBuildingSnapshot(101, 20, 401, 1, ProliferationMode.None));
        request.Buildings.Add(new ProductionBuildingSnapshot(102, 10, 402, 1, ProliferationMode.None));
        var report = analyzer.Analyze(request);
        Check(report.MaterialComplete && report.PowerComplete, "fixed-building black box complete");
        Equal(180, report.ItemFlows[2].GrossProduction, "tiers retain separate capacities");
        Equal(150, report.ItemFlows[2].GrossConsumption, "downstream runs at full load");
        Equal(30, report.ItemFlows[2].Surplus, "fractional intermediate balance retained");
        Equal(0, report.ItemFlows[2].OverbuildSurplus.Value,
            "sub-building ratio headroom is not redundant whole-building capacity");
        Equal(60, report.ItemFlows[2].SingleBuildingSurplusThreshold, "smallest actual producer threshold");
        Check(!report.ItemFlows[2].IsExcessIntermediate, "sub-threshold surplus is normally balanced");
        Equal(2, report.ItemFlows[2].ProducerGroups.Count, "producer groups retained");
        Equal(1, report.ItemFlows[2].ConsumerGroups.Count, "consumer groups retained");
        Equal(3, report.ItemFlows[1].RequiredExternalSupply, "shortage is not silently suppressed");
        Equal(1, report.ItemFlows[3].Surplus, "unconsumed output is final product");
        Check(report.ItemFlows[3].IsFinalProduct, "final output is unthresholded");

        request.Buildings.RemoveAt(2);
        request.Buildings.Add(new ProductionBuildingSnapshot(102, 10, 402, 1, ProliferationMode.None,
            speedFactor: 0.8));
        var atThreshold = analyzer.Analyze(request);
        Equal(60, atThreshold.ItemFlows[2].Surplus, "intermediate at threshold");
        Equal(60, atThreshold.ItemFlows[2].OverbuildSurplus.Value,
            "a removable whole building accounts for the true overbuild");
        Check(atThreshold.ItemFlows[2].IsExcessIntermediate, "threshold reached qualifies as excess");

        request.Buildings.Add(new ProductionBuildingSnapshot(102, 10, 402, 1, ProliferationMode.None));
        var duplicate = analyzer.Analyze(request);
        Check(duplicate.Diagnostics.Single().Code == ProductionDiagnosticCode.DuplicateSelection,
            "duplicate object IDs diagnosed and deduplicated");
        Equal(60, duplicate.ItemFlows[2].Surplus, "duplicate objects do not double-count");

        request.Buildings.Clear();
        request.Buildings.Add(new ProductionBuildingSnapshot(200, 10, 0, 1, ProliferationMode.None));
        var idle = analyzer.Analyze(request);
        Equal(100, idle.Power.ConsumptionWatts, "unconfigured building contributes idle demand");
    }

    private static void BlackBoxMixedModesAndDeficits()
    {
        var catalog = new ProductionCatalog(new[]
            {
                new ProductionItem(1, 0, true, false, 0, 0),
                new ProductionItem(2, 501, false, false, 0, 0),
                new ProductionItem(3, 0, false, false, 3, 60),
                new ProductionItem(4, 502, false, false, 0, 0)
            }, new[]
            {
                Recipe(501, new[] { (1, 1.0) }, new[] { (2, 1.0) }),
                Recipe(502, new[] { (2, 3.0) }, new[] { (4, 0.5) })
            }, new[]
            {
                new ProductionBuilding(10, ProductionRecipeCategory.Assemble, 1, 1000, 100),
                new ProductionBuilding(20, ProductionRecipeCategory.Assemble, 2, 3000, 300)
            }, new[] { 0.0, 0.25, 0.5, 1.0, 1.5 },
            new[] { 0.0, 0.125, 0.2, 0.25, 0.3 },
            new[] { 1.0, 1.3, 1.7, 2.5, 3.0 });
        var request = new FactoryBlackBoxRequest { ProliferationEnabled = true };
        request.Buildings.Add(new ProductionBuildingSnapshot(1, 10, 501, 1,
            ProliferationMode.ExtraProducts));
        request.Buildings.Add(new ProductionBuildingSnapshot(2, 20, 501, 1,
            ProliferationMode.Speedup));
        request.Buildings.Add(new ProductionBuildingSnapshot(-3, 10, 501, 1,
            ProliferationMode.None));
        request.Buildings.Add(new ProductionBuildingSnapshot(4, 10, 502, 3,
            ProliferationMode.None));
        var analyzer = new FactoryBlackBoxAnalyzer(catalog);
        var sprayed = analyzer.Analyze(request);
        Check(sprayed.MaterialComplete && sprayed.PowerComplete,
            "selected building tiers retain their distinct native modes");
        Equal(6.25, sprayed.ItemFlows[2].GrossProduction,
            "mixed extra, speedup, and plain modes produce distinct rates");
        Equal(9, sprayed.ItemFlows[2].GrossConsumption,
            "downstream runs at full capacity despite intermediate shortage");
        Equal(2.75, sprayed.ItemFlows[2].RequiredExternalSupply,
            "theoretical intermediate shortage is not hidden by downstream output");
        Equal(1.5, sprayed.ItemFlows[4].Surplus,
            "fractional final-product rate remains unrounded");
        Equal(6, sprayed.ItemFlows[1].RequiredExternalSupply,
            "raw resource shortage is reported alongside intermediate shortage");
        Equal(14000, sprayed.Power.ConsumptionWatts,
            "mixed manufacturing and unsprayed consumers use separate power modes");

        request.ProliferationEnabled = false;
        var plain = analyzer.Analyze(request);
        Equal(4, plain.ItemFlows[2].GrossProduction,
            "global no-proliferation removes speed and extra-product effects");
        Equal(5000 + 3000, plain.Power.ConsumptionWatts,
            "global no-proliferation restores native power for every tier");
        Equal(1.5, plain.ItemFlows[4].GrossProduction,
            "disabling proliferation does not throttle a shorted downstream recipe");
    }

    private static void LogisticsChargingPower()
    {
        var catalog = new ProductionCatalog(new[]
        {
            new ProductionItem(1, 0, true, false, 0, 0),
            new ProductionItem(2, 101, false, false, 0, 0)
        }, new[] { Recipe(101, new[] { (1, 1.0) }, new[] { (2, 1.0) }) }, new[]
        {
            new ProductionBuilding(10, ProductionRecipeCategory.Assemble, 1, 1000, 100),
            new ProductionBuilding(20, ProductionRecipeCategory.None, 0, 3000000, 100,
                ProductionBuildingKind.Logistics),
            new ProductionBuilding(21, ProductionRecipeCategory.None, 0, 15000000, 100,
                ProductionBuildingKind.Logistics),
            new ProductionBuilding(22, ProductionRecipeCategory.None, 0, 300000, 100,
                ProductionBuildingKind.Logistics),
            new ProductionBuilding(23, ProductionRecipeCategory.None, 0, 600000, 100,
                ProductionBuildingKind.Logistics)
        }, new[] { 0.0 }, new[] { 0.0 }, new[] { 1.0 });
        var request = new FactoryBlackBoxRequest();
        request.Buildings.Add(new ProductionBuildingSnapshot(1, 10, 101, 1, ProliferationMode.None));
        request.Buildings.Add(new ProductionBuildingSnapshot(2, 20, 0, 2, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double> { ["ChargePowerWatts"] = 3000000 }));
        request.Buildings.Add(new ProductionBuildingSnapshot(3, 20, 0, 1, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double> { ["ChargePowerWatts"] = 4000000 }));
        request.Buildings.Add(new ProductionBuildingSnapshot(4, 21, 0, 1, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double> { ["ChargePowerWatts"] = 15000000 }));
        request.Buildings.Add(new ProductionBuildingSnapshot(5, 22, 0, 1, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double> { ["ChargePowerWatts"] = 300000 }));
        request.Buildings.Add(new ProductionBuildingSnapshot(6, 23, 0, 1, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double> { ["ChargePowerWatts"] = 600000 }));
        var analyzer = new FactoryBlackBoxAnalyzer(catalog);
        var report = analyzer.Analyze(request);
        Check(report.MaterialComplete && report.PowerComplete,
            "selected logistics chargers have complete power and no material demand");
        Equal(25901000, report.Power.ConsumptionWatts,
            "theoretical grid demand sums each selected facility's configured charging limit");
        Equal(25901000, report.Power.PeakConsumptionWatts,
            "selected facilities use the same peak and full-load charging power");
        Equal(6000000, report.Groups[1].ConsumptionWatts.Value,
            "multiple identical chargers multiply their configured charging power");
        Equal(4000000, report.Groups[2].ConsumptionWatts.Value,
            "separately configured chargers of the same type stay independent");
        Equal(1, report.ItemFlows[2].GrossProduction,
            "logistics charging does not change manufacturing capacity");
        Equal(0, report.Power.AccumulatorChargingWatts,
            "logistics charging is not accumulator exchange");

        request.Buildings[1] = new ProductionBuildingSnapshot(2, 20, 0, 2, ProliferationMode.None);
        var missing = analyzer.Analyze(request);
        Check(missing.MaterialComplete && !missing.PowerComplete && missing.Power == null &&
              missing.Groups[1].ConsumptionWatts == null &&
              missing.Diagnostics.Any(diagnostic =>
                  diagnostic.Code == ProductionDiagnosticCode.MissingOperatingParameter &&
                  diagnostic.BuildingItemId == 20),
            "missing logistics settings invalidate power without losing material results");
        Equal(1, missing.ItemFlows[2].GrossProduction,
            "manufacturing still reports materials with an unknown charger");

        foreach (var invalidWatts in new[] { -1.0, double.NaN, double.PositiveInfinity })
        {
            request.Buildings[1] = new ProductionBuildingSnapshot(2, 20, 0, 2, ProliferationMode.None,
                operatingParameters: new Dictionary<string, double> { ["ChargePowerWatts"] = invalidWatts });
            var invalid = analyzer.Analyze(request);
            Check(invalid.MaterialComplete && !invalid.PowerComplete && invalid.Power == null &&
                  invalid.Diagnostics.Any(diagnostic =>
                      diagnostic.Code == ProductionDiagnosticCode.InvalidRequest &&
                      diagnostic.BuildingItemId == 20),
                "invalid logistics charge power is diagnosed independently of material flows");
        }

        request.Buildings[1] = new ProductionBuildingSnapshot(2, 20, 0, 2, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double> { ["ChargePowerWatts"] = 3000000 });
        request.Buildings.Add(request.Buildings[1]);
        var duplicate = analyzer.Analyze(request);
        Equal(25901000, duplicate.Power.ConsumptionWatts,
            "repeated object selections do not double-count logistics charging");
        Check(duplicate.Diagnostics.Any(diagnostic => diagnostic.Code == ProductionDiagnosticCode.DuplicateSelection),
            "repeated logistics facility selection is diagnosed");
    }

    private static void BeltSignalStatistics()
    {
        var report = new ProductionPlanner(Catalog()).Calculate(Request((3, 1)));
        Check(report.MaterialComplete, "belt source plan has complete materials without selected special power");
        var rates = BeltSignalSourceStats.FromReport(report, 3);
        Check(rates.All(rate => rate.ItemId != 3), "delivered target is not added to upstream product register");
        Equal(0.5, rates.Single(rate => rate.ItemId == 1).ProductionPerItem,
            "external raw material is a source product");
        Equal(0.5, rates.Single(rate => rate.ItemId == 1).ConsumptionPerItem,
            "external raw material is also consumed by the recipe");
        Equal(0.5, rates.Single(rate => rate.ItemId == 4).ProductionPerItem,
            "coproduct is recorded without consumption");

        var registers = new int[100];
        var consumption = new int[100];
        var stats = new BeltSignalSourceStats(rates);
        stats.Apply(1, registers, consumption);
        stats.Apply(1, registers, consumption);
        Equal(1, registers[1], "fractional source production is retained across generated items");
        Equal(1, consumption[1], "fractional source consumption is retained independently");
        Equal(2, registers[2], "intermediate production uses the gross ledger");
        Equal(2, consumption[2], "intermediate consumption uses the gross ledger");
        Equal(1, registers[4], "coproduct is not produced twice across two insertions");
        Equal(0, consumption[4], "unused coproduct is not consumed");
        Equal(0, registers[3], "generated target is not counted by upstream statistics");

        var asymmetrical = new ProductionReport(ProductionStatus.Complete, true, true,
            new[] { new ProductionItemFlow(7, 0.25, 0.75, 0, 0, 0, 0, 0,
                false, false, 0, Array.Empty<int>(), Array.Empty<int>()) },
            Array.Empty<ProductionGroupFlow>(), Array.Empty<ProductionDiagnostic>(), null);
        var independent = new BeltSignalSourceStats(BeltSignalSourceStats.FromReport(asymmetrical, 8));
        for (var insertion = 0; insertion < 4; insertion++)
            independent.Apply(1, registers, consumption);
        Equal(1, registers[7], "quarter-rate production has its own fractional progress");
        Equal(3, consumption[7], "three-quarter-rate consumption has its own fractional progress");

        var natural = new ProductionPlanner(Catalog()).Calculate(Request((1, 1)));
        Check(BeltSignalSourceStats.FromReport(natural, 1).Count == 0,
            "an externally sourced target has no duplicate statistics");
    }

    private static void BeltSignalPreset()
    {
        var items = new[]
        {
            new ProductionItem(ItemIds.Water, 0, true, false, 0, 0),
            new ProductionItem(ItemIds.ProliferatorMkIII, 201, false, false, 4, 75),
            new ProductionItem(ItemIds.Steel, 202, false, false, 0, 0),
            new ProductionItem(ItemIds.SpaceWarper, 203, false, false, 0, 0),
            new ProductionItem(ItemIds.TitaniumGlass, 204, false, false, 0, 0)
        };
        var recipes = new[]
        {
            Recipe(201, new[] { (ItemIds.Water, 1.0) },
                new[] { (ItemIds.ProliferatorMkIII, 1.0) }),
            Recipe(202, new[] { (ItemIds.Water, 1.0) }, new[] { (ItemIds.Steel, 1.0) }),
            Recipe(203, new[] { (ItemIds.Water, 1.0) }, new[] { (ItemIds.SpaceWarper, 1.0) }),
            Recipe(204, new[] { (ItemIds.Water, 1.0) }, new[] { (ItemIds.TitaniumGlass, 1.0) },
                productive: false)
        };
        var catalog = new ProductionCatalog(items, recipes, Array.Empty<ProductionBuilding>(),
            new[] { 0.0, 0.25, 0.5, 0.75, 1.0 },
            new[] { 0.0, 0.125, 0.2, 0.225, 0.25 },
            new[] { 1.0, 1.3, 1.7, 2.1, 2.5 });
        var request = BeltSignalSourcePreset.Create(catalog, ItemIds.Steel, true, true);
        Check(request.ExternalMaterials.SetEquals(new[] { ItemIds.Water }),
            "only known natural belt source IDs become external materials");
        Equal(ItemIds.ProliferatorMkIII, request.ProliferatorItemId,
            "belt policy uses the available proliferator item");
        Check(request.SelfSprayProliferator && request.SprayDeliveredItems.Contains(ItemIds.Steel),
            "self-spray and delivered-item spraying are request settings");
        Check(request.ProliferationModeByItem[ItemIds.Steel] == ProliferationMode.ExtraProducts,
            "productive legacy extra-products choice maps to a native mode");
        Check(request.ProliferationModeByItem[ItemIds.SpaceWarper] == ProliferationMode.None,
            "legacy disabled item keeps its no-proliferation preset");
        Check(request.ProliferationModeByItem[ItemIds.TitaniumGlass] == ProliferationMode.Speedup,
            "unsupported extra-products choice falls back to native speedup");
        var report = new ProductionPlanner(catalog).Calculate(request);
        Check(report.MaterialComplete, "belt preset resolves the proliferator's recipe and raw materials");
        Check(report.ItemFlows[ItemIds.ProliferatorMkIII].GrossConsumption > 0,
            "belt preset accounts for finished-item and upstream spray consumption");

        var updatedItems = new[]
        {
            items[0], items[2], new ProductionItem(9999, 205, false, false, 4, 80)
        };
        var updatedRecipes = new[]
        {
            recipes[1], Recipe(205, new[] { (ItemIds.Water, 1.0) }, new[] { (9999, 1.0) })
        };
        var updatedCatalog = new ProductionCatalog(updatedItems, updatedRecipes,
            Array.Empty<ProductionBuilding>(), new[] { 0.0, 0.25, 0.5, 0.75, 1.0 },
            new[] { 0.0, 0.125, 0.2, 0.225, 0.25 },
            new[] { 1.0, 1.3, 1.7, 2.1, 2.5 });
        var updated = BeltSignalSourcePreset.Create(updatedCatalog, ItemIds.Steel, true, false);
        Equal(9999, updated.ProliferatorItemId, "updated game data selects a valid proliferator instead of a stale ID");
    }

    private static void RayReceiverPowerAndPhotons()
    {
        var receiver = new ProductionBuilding(51, ProductionRecipeCategory.None, 1, 0, 0,
            ProductionBuildingKind.RayReceiver, ratedGenerationWatts: 1000,
            powerProductItemId: 456, powerProductEnergyJoules: 100000, catalystMask: 1);
        var items = new[]
        {
            new ProductionItem(123, 0, false, false, 0, 0,
                catalystTypeMask: 1, catalystAbilityMultiplier: 2),
            new ProductionItem(456, 999, false, false, 0, 0),
            new ProductionItem(789, 0, false, false, 4, 75)
        };
        var photonRecipe = new ProductionRecipe(999, ProductionRecipeCategory.PhotonStore, 60, false,
            Array.Empty<KeyValuePair<int, double>>(),
            new[] { new KeyValuePair<int, double>(456, 1) });
        var catalog = new ProductionCatalog(items, new[] { photonRecipe }, new[] { receiver },
            new[] { 0.0, 0.25, 0.5, 0.75, 1.0 },
            new[] { 0.0, 0.125, 0.2, 0.225, 0.25 },
            new[] { 1.0, 1.3, 1.7, 2.1, 2.5 });
        var parameters = new Dictionary<string, double>
        {
            ["Mode0"] = 456, ["LensItemId"] = 0, ["SolarEnergyLossRate"] = 0.25
        };
        var analyzer = new FactoryBlackBoxAnalyzer(catalog);
        var request = new FactoryBlackBoxRequest();
        request.Buildings.Add(new ProductionBuildingSnapshot(1, 51, 0, 1,
            ProliferationMode.None, operatingParameters: parameters));
        var photons = analyzer.Analyze(request);
        Check(photons.MaterialComplete && photons.PowerComplete, "native photon process evaluates at full warmup");
        Equal(12, photons.ItemFlows[456].GrossProduction, "photon rate uses eightfold Dyson power");
        Equal(0, photons.Power.GenerationWatts, "photon mode does not contribute grid generation");
        Equal(20000 / 0.85, photons.Power.DysonSphereRequirementWatts,
            "photon Dyson demand uses warmed-up loss technology");

        parameters["Mode0"] = 0;
        request.Buildings[0] = new ProductionBuildingSnapshot(1, 51, 0, 1,
            ProliferationMode.None, operatingParameters: parameters);
        var grid = analyzer.Analyze(request);
        Check(!grid.ItemFlows.ContainsKey(456), "grid mode does not invent photon products");
        Equal(2500, grid.Power.GenerationWatts, "grid mode reports receiver generation");
        Equal(2500 / 0.85, grid.Power.DysonSphereRequirementWatts,
            "grid mode keeps Dyson power separate from local generation");

        parameters["Mode0"] = 456;
        parameters["LensItemId"] = 123;
        request.Buildings[0] = new ProductionBuildingSnapshot(1, 51, 0, 1,
            ProliferationMode.None, operatingParameters: parameters);
        request.ProliferationEnabled = true;
        var sprayed = analyzer.Analyze(request);
        Equal(48, sprayed.ItemFlows[456].GrossProduction,
            "lens ability and native speedup multiply photon production");
        Equal(0.1, sprayed.ItemFlows[123].GrossConsumption,
            "ray receiver consumes one lens per 36000 ticks, not 3600 ticks");
        Equal(80000 / 0.85, sprayed.Power.DysonSphereRequirementWatts,
            "lens and proliferation change the Dyson requirement");
        request.ProliferationEnabled = false;
        var unproliferated = analyzer.Analyze(request);
        Equal(24, unproliferated.ItemFlows[456].GrossProduction,
            "disabling global proliferation leaves lens ability but removes speedup");
        Equal(0.1, unproliferated.ItemFlows[123].GrossConsumption,
            "unproliferated lens still wears at its native rate");

        var plan = new ProductionPlanRequest();
        plan.Targets.Add(new ProductionTarget(456, 24));
        plan.ExternalMaterials.Add(123);
        plan.BuildingByRecipe[999] = 51;
        plan.OperatingParametersByRecipe[999] = parameters;
        var planned = new ProductionPlanner(catalog).Calculate(plan);
        Check(planned.MaterialComplete && planned.PowerComplete, "photon planning uses the ray receiver adapter");
        Equal(0.1, planned.ItemFlows[123].ExternalImports,
            "planner accounts for lenses as external inputs when selected");
        Equal(1, planned.Groups.Single().EquivalentBuildings.Value,
            "photon plan uses equivalent receivers for peak and Dyson demand");

        var unbuilt = new ProductionPlanRequest();
        unbuilt.Targets.Add(new ProductionTarget(456, 24));
        var photonsOnly = new ProductionPlanner(catalog).Calculate(unbuilt);
        Check(photonsOnly.MaterialComplete && !photonsOnly.PowerComplete && photonsOnly.Power == null,
            "photons without a selected receiver keep material flows and leave power incomplete");
        Equal(24, photonsOnly.ItemFlows[456].GrossProduction, "receiver-independent photon output is planned");
        unbuilt.OperatingParametersByRecipe[999] = new Dictionary<string, double> { ["LensItemId"] = 123 };
        Check(new ProductionPlanner(catalog).Calculate(unbuilt).Diagnostics.Single().Code ==
              ProductionDiagnosticCode.MissingBuilding, "lens wear without a selected receiver is not invented");

        parameters.Remove("SolarEnergyLossRate");
        request.Buildings[0] = new ProductionBuildingSnapshot(1, 51, 0, 1,
            ProliferationMode.None, operatingParameters: parameters);
        var missingTechnology = analyzer.Analyze(request);
        Check(missingTechnology.MaterialComplete && !missingTechnology.PowerComplete,
            "missing solar-loss technology does not discard photon material results");
        Check(missingTechnology.Groups.Single().DysonSphereRequirementWatts == null,
            "unknown Dyson power is not reported as zero");
        Check(missingTechnology.Diagnostics.Any(d => d.Code == ProductionDiagnosticCode.MissingOperatingParameter),
            "missing solar-loss technology is diagnosed");
        Check(missingTechnology.PowerBreakdown.FactoryComplete &&
              missingTechnology.PowerBreakdown.FactoryConsumptionWatts == 0,
            "unknown Dyson requirement does not erase known factory consumption");
        plan.OperatingParametersByRecipe[999] = parameters;
        var plannedWithoutLoss = new ProductionPlanner(catalog).Calculate(plan);
        Check(plannedWithoutLoss.MaterialComplete && !plannedWithoutLoss.PowerComplete &&
              plannedWithoutLoss.PowerBreakdown.FactoryComplete &&
              plannedWithoutLoss.PowerBreakdown.FactoryConsumptionWatts == 0,
            "planner retains calculable factory watts while Dyson requirement is unknown");
    }

    private static void LauncherCyclesAndAmmunition()
    {
        var catalog = new ProductionCatalog(new[]
            {
                new ProductionItem(101, 0, false, false, 0, 0),
                new ProductionItem(102, 0, false, false, 0, 0),
                new ProductionItem(103, 0, false, false, 4, 75)
            }, Array.Empty<ProductionRecipe>(), new[]
            {
                new ProductionBuilding(71, ProductionRecipeCategory.None, 0, 1200, 120,
                    ProductionBuildingKind.Ejector, launchChargeTicks: 10,
                    launchCooldownTicks: 20, ammunitionItemId: 101),
                new ProductionBuilding(72, ProductionRecipeCategory.None, 0, 3600, 360,
                    ProductionBuildingKind.Silo, launchChargeTicks: 60,
                    launchCooldownTicks: 60, ammunitionItemId: 102)
            }, new[] { 0.0, 0.25, 0.5, 0.75, 1.0 },
            new[] { 0.0, 0.125, 0.2, 0.225, 0.25 },
            new[] { 1.0, 1.3, 1.7, 2.1, 2.5 });
        var ejectorSettings = new Dictionary<string, double> { ["Mode1"] = 0, ["LaunchAvailable"] = 1 };
        var siloSettings = new Dictionary<string, double> { ["Mode0"] = 0, ["LaunchAvailable"] = 1 };
        var request = new FactoryBlackBoxRequest();
        request.Buildings.Add(new ProductionBuildingSnapshot(1, 71, 0, 1,
            ProliferationMode.None, operatingParameters: ejectorSettings));
        request.Buildings.Add(new ProductionBuildingSnapshot(2, 72, 0, 1,
            ProliferationMode.None, operatingParameters: siloSettings));
        var analyzer = new FactoryBlackBoxAnalyzer(catalog);
        var ordinary = analyzer.Analyze(request);
        Check(ordinary.MaterialComplete && ordinary.PowerComplete, "both launch facilities evaluate");
        Equal(120, ordinary.ItemFlows[101].GrossConsumption, "sails consumed per native ejector cycle");
        Equal(30, ordinary.ItemFlows[102].GrossConsumption, "rockets consumed per native silo cycle");
        Equal(150, ordinary.LaunchesPerMinute, "launch rate reported without invented item outputs");
        Equal(4800, ordinary.Power.ConsumptionWatts, "launchers draw working power at full load");
        Check(ordinary.ItemFlows.Values.All(flow => flow.GrossProduction == 0),
            "launches do not appear as fabricated product items");

        request.ProliferationEnabled = true;
        var sprayed = analyzer.Analyze(request);
        Equal(240, sprayed.ItemFlows[101].GrossConsumption, "ammo speedup shortens both ejector phases");
        Equal(60, sprayed.ItemFlows[102].GrossConsumption, "ammo speedup shortens both silo phases");
        Equal(12000, sprayed.Power.ConsumptionWatts, "ammo proliferation raises native launcher power");

        ejectorSettings["Mode1"] = 1;
        ejectorSettings["BoostEnabled"] = 1;
        request.Buildings[0] = new ProductionBuildingSnapshot(1, 71, 0, 1,
            ProliferationMode.None, operatingParameters: ejectorSettings);
        var accelerated = analyzer.Analyze(request);
        Equal(1800, accelerated.ItemFlows[101].GrossConsumption,
            "native boosted ejector cannot complete a phase in less than one tick");
        request.ProliferationEnabled = false;
        accelerated = analyzer.Analyze(request);
        Equal(1200, accelerated.ItemFlows[101].GrossConsumption,
            "disabling the global switch removes ammunition speedup even with sandbox boost");

        ejectorSettings.Remove("BoostEnabled");
        request.Buildings[0] = new ProductionBuildingSnapshot(1, 71, 0, 1,
            ProliferationMode.None, operatingParameters: ejectorSettings);
        var missingBoost = analyzer.Analyze(request);
        Check(!missingBoost.MaterialComplete && missingBoost.Diagnostics.Any(d =>
                d.Code == ProductionDiagnosticCode.MissingOperatingParameter),
            "native sandbox boost state is required for a boosted launcher blueprint");

        ejectorSettings["LaunchAvailable"] = 0;
        ejectorSettings["Mode1"] = 0;
        request.Buildings[0] = new ProductionBuildingSnapshot(1, 71, 0, 1,
            ProliferationMode.None, operatingParameters: ejectorSettings);
        var idle = analyzer.Analyze(request);
        Equal(120, idle.Power.ConsumptionWatts - 3600, "launcher without an orbit draws only idle demand");
        Equal(30, idle.LaunchesPerMinute, "launcher without an orbit does not consume ammunition");
    }

    private static void GasCollection()
    {
        var catalog = new ProductionCatalog(new[]
            {
                new ProductionItem(111, 0, true, false, 0, 0),
                new ProductionItem(112, 0, true, false, 0, 0),
                new ProductionItem(113, 0, false, false, 4, 75)
            }, Array.Empty<ProductionRecipe>(), new[]
            {
                new ProductionBuilding(81, ProductionRecipeCategory.None, 0, 1000, 100,
                    ProductionBuildingKind.Collector, collectorSpeedMultiplier: 5)
            }, new[] { 0.0, 0.25, 0.5, 0.75, 1.0 },
            new[] { 0.0, 0.125, 0.2, 0.225, 0.25 },
            new[] { 1.0, 1.3, 1.7, 2.1, 2.5 });
        var settings = new Dictionary<string, double>
        {
            ["GasCount"] = 2, ["GasTotalHeat"] = 1000, ["MiningSpeedMultiplier"] = 2,
            ["GasItemId0"] = 111, ["GasSpeedPerSecond0"] = 0.5,
            ["GasItemId1"] = 112, ["GasSpeedPerSecond1"] = 0.2
        };
        var request = new FactoryBlackBoxRequest { ProliferationEnabled = true };
        request.Buildings.Add(new ProductionBuildingSnapshot(1, 81, 0, 1,
            ProliferationMode.None, operatingParameters: settings));
        var analyzer = new FactoryBlackBoxAnalyzer(catalog);
        var report = analyzer.Analyze(request);
        Check(report.MaterialComplete && report.PowerComplete, "gas collection uses captured planet resources");
        Equal(270, report.ItemFlows[111].GrossProduction,
            "native gas self-power recovery and mining technology scale per-second speed to a minute");
        Equal(108, report.ItemFlows[112].GrossProduction,
            "second gas resource is not lost by a single-output adapter");
        Equal(0, report.Power.ConsumptionWatts, "collector working energy is recovered gas, not grid demand");

        settings.Remove("GasSpeedPerSecond1");
        request.Buildings[0] = new ProductionBuildingSnapshot(1, 81, 0, 1,
            ProliferationMode.None, operatingParameters: settings);
        var missing = analyzer.Analyze(request);
        Check(!missing.MaterialComplete && missing.Diagnostics.Any(d =>
                d.Code == ProductionDiagnosticCode.MissingOperatingParameter),
            "unavailable gas composition is diagnosed instead of inventing outputs");
        Check(missing.PowerBreakdown.FactoryComplete && missing.PowerBreakdown.FactoryConsumptionWatts == 0,
            "missing gas composition does not turn collector self-fueling into grid consumption");
    }

    private static void ResearchHashesAndMatrices()
    {
        var tech = new ProductionTechnology(401, new[]
        {
            new KeyValuePair<int, double>(6001, 2), new KeyValuePair<int, double>(6002, 1)
        });
        var otherTech = new ProductionTechnology(402, new[]
        {
            new KeyValuePair<int, double>(6001, 4), new KeyValuePair<int, double>(6002, 2),
            new KeyValuePair<int, double>(6003, 3), new KeyValuePair<int, double>(6004, 1),
            new KeyValuePair<int, double>(6005, 5), new KeyValuePair<int, double>(6006, 1)
        });
        var catalog = new ProductionCatalog(new[]
            {
                new ProductionItem(6001, 0, false, false, 0, 0),
                new ProductionItem(6002, 0, false, false, 0, 0),
                new ProductionItem(6003, 0, false, false, 0, 0),
                new ProductionItem(6004, 0, false, false, 0, 0),
                new ProductionItem(6005, 0, false, false, 0, 0),
                new ProductionItem(6006, 0, false, false, 0, 0),
                new ProductionItem(7000, 0, false, false, 4, 75)
            }, Array.Empty<ProductionRecipe>(), new[]
            {
                new ProductionBuilding(91, ProductionRecipeCategory.Research, 1, 1000, 100)
            }, new[] { 0.0, 0.25, 0.5, 0.75, 1.0 },
            new[] { 0.0, 0.125, 0.2, 0.225, 0.25 },
            new[] { 1.0, 1.3, 1.7, 2.1, 2.5 }, new[] { tech, otherTech }, Enumerable.Range(6001, 6));
        var settings = new Dictionary<string, double>
        {
            ["ResearchMode"] = 1, ["TechId"] = 401, ["ResearchSpeed"] = 0.5
        };
        var request = new FactoryBlackBoxRequest();
        request.Buildings.Add(new ProductionBuildingSnapshot(1, 91, 0, 1,
            ProliferationMode.Speedup, operatingParameters: settings));
        var analyzer = new FactoryBlackBoxAnalyzer(catalog);
        var plain = analyzer.Analyze(request);
        Check(plain.MaterialComplete && plain.PowerComplete, "lab research mode has full-load flows");
        Equal(1800, plain.ResearchHashesPerMinute, "native research speed sets base hashes per minute");
        Equal(1, plain.ItemFlows[6001].GrossConsumption,
            "matrix consumption uses technology's points per hash");
        Equal(0.5, plain.ItemFlows[6002].GrossConsumption, "fractional matrix consumption is retained");
        Equal(0, plain.ItemFlows[6002].GrossProduction, "research hashes are not item products");
        Equal(1000, plain.Power.ConsumptionWatts, "unsprayed research uses native working power");

        request.ProliferationEnabled = true;
        var sprayed = analyzer.Analyze(request);
        Equal(2250, sprayed.ResearchHashesPerMinute, "native proliferation grants extra research hashes");
        Equal(1, sprayed.ItemFlows[6001].GrossConsumption,
            "extra hashes do not consume additional matrices");
        Equal(2500, sprayed.Power.ConsumptionWatts,
            "research-mode lab uses native proliferation power increase");
        Check(sprayed.Groups.Single().Process.ProliferationMode == ProliferationMode.ExtraProducts,
            "research mode does not mistake copied assembler speedup setting for hash mode");

        settings["TechId"] = 999;
        request.Buildings[0] = new ProductionBuildingSnapshot(1, 91, 0, 2,
            ProliferationMode.None, operatingParameters: settings);
        var unrecognizedTech = analyzer.Analyze(request);
        Check(!unrecognizedTech.MaterialComplete &&
              unrecognizedTech.Diagnostics.Any(d => d.Code == ProductionDiagnosticCode.InvalidRequest) &&
              unrecognizedTech.PowerBreakdown.FactoryComplete &&
              unrecognizedTech.PowerBreakdown.FactoryConsumptionWatts == 5000,
            "every research-mode lab retains full working power when its technology cannot be evaluated");
        Check(unrecognizedTech.PowerComplete && unrecognizedTech.Power?.ConsumptionWatts == 5000,
            "invalid research technology does not hide an independently known full-load power total");

        settings.Remove("TechId");
        request.Buildings[0] = new ProductionBuildingSnapshot(1, 91, 0, 1,
            ProliferationMode.None, operatingParameters: settings);
        var missing = analyzer.Analyze(request);
        Check(!missing.MaterialComplete && missing.Diagnostics.Any(d =>
                d.Code == ProductionDiagnosticCode.MissingOperatingParameter),
            "missing technology cannot silently report zero matrices");
        Check(missing.PowerBreakdown.FactoryComplete &&
              missing.PowerBreakdown.FactoryConsumptionWatts == 2500,
            "research power remains calculable without the technology when proliferation is enabled");
        Check(missing.PowerComplete && missing.Power?.ConsumptionWatts == 2500,
            "missing research technology does not hide rated total power");

        request.Buildings.Add(new ProductionBuildingSnapshot(2, 91, 0, 2,
            ProliferationMode.None, operatingParameters: new Dictionary<string, double>
            {
                ["ResearchMode"] = 1, ["TechId"] = 401, ["ResearchSpeed"] = 0.5
            }));
        var mixedResearch = analyzer.Analyze(request);
        Check(!mixedResearch.MaterialComplete && mixedResearch.PowerBreakdown.FactoryComplete &&
              mixedResearch.PowerBreakdown.FactoryConsumptionWatts == 7500,
            "all research-mode labs add their rated power even when settings differ");
        Check(mixedResearch.PowerComplete && mixedResearch.Power?.ConsumptionWatts == 7500,
            "every research-mode lab contributes to the independent power total");
        request.Buildings.RemoveAt(1);

        var noSprayCatalog = new ProductionCatalog(catalog.Items.Values.Where(item => item.Id != 7000),
            Array.Empty<ProductionRecipe>(), catalog.Buildings.Values,
            new[] { 0.0 }, new[] { 0.0 }, new[] { 1.0 }, catalog.Technologies.Values,
            catalog.ResearchMatrixItemIds);
        var noSpray = new FactoryBlackBoxAnalyzer(noSprayCatalog).Analyze(request);
        Check(noSpray.PowerComplete && noSpray.Power?.ConsumptionWatts == 1000,
            "without a supported spray level, research mode still budgets full base power");

        request.Buildings[0] = new ProductionBuildingSnapshot(1, 91, 0, 2,
            ProliferationMode.None, operatingParameters: new Dictionary<string, double> { ["ResearchMode"] = 1 });
        request.ProliferationEnabled = false;
        var blueprintLab = analyzer.Analyze(request);
        Check(!blueprintLab.MaterialComplete && blueprintLab.PowerBreakdown.FactoryComplete &&
              blueprintLab.PowerBreakdown.FactoryConsumptionWatts == 2000,
            "blueprint research labs report working watts without transient technology or speed");
        Check(blueprintLab.PowerComplete && blueprintLab.Power?.ConsumptionWatts == 2000,
            "an unconfigured research blueprint reports known full-load power separately from unknown matrices");
        var researchDescription = BlueprintDescriptionFormatter.Format(blueprintLab, TestItemTag,
            new BlueprintDescriptionLabels("Factory power", "Logistics power", "Missing: ", "Excess: ", "Unknown",
                "(known portion)", "Input: ", "Output: ", "Research: "));
        Check(researchDescription.Power == "Factory power 2kW" && researchDescription.Description == "",
            "blueprint descriptions show research power without inventing matrix materials");

        request.Buildings[0] = new ProductionBuildingSnapshot(1, 91, 0, 2,
            ProliferationMode.None, operatingParameters: new Dictionary<string, double>
            {
                ["ResearchMode"] = 1, ["ResearchMatrixSink"] = 1,
                ["TechId"] = 999, ["ResearchSpeed"] = 0.01
            });
        var assumed = analyzer.Analyze(request);
        Check(assumed.MaterialComplete && assumed.PowerComplete && assumed.Diagnostics.Count == 0,
            "blueprint labs do not require a valid research technology or speed");
        Check(assumed.ItemFlows.Count == 0 && assumed.ResearchHashesPerMinute == 0,
            "labs alone do not invent matrix inputs or research hashes");
        var assumedDescription = BlueprintDescriptionFormatter.Format(assumed, TestItemTag,
            new BlueprintDescriptionLabels("Factory power", "Logistics power", "Missing: ", "Excess: ", "Unknown",
                "(known portion)", "Input: ", "Output: ", "Research: "));
        Check(assumedDescription.Power == "Factory power 2kW" && assumedDescription.Description == "",
            "research-only blueprints show power but no imaginary matrix demand");

        request.ProliferationEnabled = true;
        var assumedSprayed = analyzer.Analyze(request);
        Check(assumedSprayed.MaterialComplete && assumedSprayed.PowerComplete &&
              assumedSprayed.ItemFlows.Count == 0,
            "spraying research stations does not invent matrix consumption");
        Equal(5000, assumedSprayed.Power.ConsumptionWatts,
            "every research station still uses its full sprayed power");
        var unsprayableResearch = new FactoryBlackBoxAnalyzer(noSprayCatalog).Analyze(request);
        Check(unsprayableResearch.MaterialComplete && unsprayableResearch.PowerComplete,
            "a catalog without proliferation keeps research material accounting complete");
        Equal(2000, unsprayableResearch.Power.ConsumptionWatts,
            "unavailable proliferation does not change full-load research power");
        var missingMatricesCatalog = new ProductionCatalog(catalog.Items.Values,
            catalog.Recipes.Values, catalog.Buildings.Values,
            new[] { 0.0, 0.25, 0.5, 0.75, 1.0 },
            new[] { 0.0, 0.125, 0.2, 0.225, 0.25 },
            new[] { 1.0, 1.3, 1.7, 2.1, 2.5 },
            researchMatrixItemIds: catalog.ResearchMatrixItemIds);
        var missingMatrices = new FactoryBlackBoxAnalyzer(missingMatricesCatalog).Analyze(request);
        Check(missingMatrices.MaterialComplete && missingMatrices.PowerComplete &&
              missingMatrices.ItemFlows.Count == 0,
            "blueprint matrix routing does not depend on a catalog of unlocked technologies");
        var noNativeIdsCatalog = new ProductionCatalog(catalog.Items.Values, catalog.Recipes.Values,
            catalog.Buildings.Values, new[] { 0.0, 0.25, 0.5, 0.75, 1.0 },
            new[] { 0.0, 0.125, 0.2, 0.225, 0.25 },
            new[] { 1.0, 1.3, 1.7, 2.1, 2.5 }, catalog.Technologies.Values);
        var noNativeIds = new FactoryBlackBoxAnalyzer(noNativeIdsCatalog).Analyze(request);
        Check(!noNativeIds.MaterialComplete && noNativeIds.PowerComplete &&
              noNativeIds.Diagnostics.Any(d => d.Code == ProductionDiagnosticCode.MissingOperatingParameter),
            "missing native matrix IDs remain unknown without hiding research power");
        request.ProliferationEnabled = false;

        var matrixCatalog = new ProductionCatalog(catalog.Items.Values.Concat(new[]
            {
                new ProductionItem(1, 0, true, false, 0, 0),
                new ProductionItem(2, 0, true, false, 0, 0),
                new ProductionItem(8000, 0, false, false, 0, 0)
            }), new[]
            {
                Recipe(910, new[] { (1, 1.0) }, new[]
                {
                    (6001, 2.0), (6002, 1.0), (6003, 1.0),
                    (6004, 1.0), (6005, 1.0), (6006, 1.0)
                }),
                Recipe(911, new[] { (6001, 1.0) }, new[] { (8000, 1.0) }),
                Recipe(912, new[] { (2, 1.0) }, new[] { (6001, 2.0) })
            }, catalog.Buildings.Values.Concat(new[]
            {
                new ProductionBuilding(92, ProductionRecipeCategory.Assemble, 1, 100, 10),
                new ProductionBuilding(93, ProductionRecipeCategory.Assemble, 1, 200, 20)
            }), new[] { 0.0, 0.25, 0.5, 0.75, 1.0 },
            new[] { 0.0, 0.125, 0.2, 0.225, 0.25 },
            new[] { 1.0, 1.3, 1.7, 2.1, 2.5 }, catalog.Technologies.Values,
            catalog.ResearchMatrixItemIds);
        var matrixRequest = new FactoryBlackBoxRequest();
        matrixRequest.Buildings.Add(new ProductionBuildingSnapshot(1, 92, 910, 3, ProliferationMode.None));
        matrixRequest.Buildings.Add(new ProductionBuildingSnapshot(2, 93, 911, 1, ProliferationMode.None));
        matrixRequest.Buildings.Add(new ProductionBuildingSnapshot(3, 92, 912, 3, ProliferationMode.None));
        var withoutLab = new FactoryBlackBoxAnalyzer(matrixCatalog).Analyze(matrixRequest);
        Equal(0, withoutLab.ItemFlows[2].SteadyStateExternalSupply.Value,
            "without a research station, excess single-matrix production slows upstream");
        Equal(6, withoutLab.ItemFlows[6001].OverbuildSurplus.Value,
            "without a research station, unnecessary matrix production is excess");
        Check(withoutLab.ItemFlows[6002].IsFinalProduct,
            "without a research station, exported matrix production remains visible");
        var withoutLabText = BlueprintDescriptionFormatter.Format(withoutLab, TestItemTag,
            new BlueprintDescriptionLabels("Factory power", "Logistics power", "Missing: ", "Excess: ", "Unknown",
                "(known portion)", "Input: ", "Output: ", "Research: "));
        Check(withoutLabText.Research == "" && withoutLabText.Warnings.Contains("Excess: " + TestItemTag(6001)) &&
              withoutLabText.Output.Contains(TestItemTag(6002) + " x3"),
            "without a research station, matrices use ordinary output and excess rules");

        matrixRequest.Buildings.Add(new ProductionBuildingSnapshot(4, 91, 0, 1,
            ProliferationMode.None, operatingParameters: new Dictionary<string, double>
            {
                ["ResearchMode"] = 1, ["ResearchMatrixSink"] = 1,
                ["TechId"] = 999, ["ResearchSpeed"] = 0.01
            }));
        var withLab = new FactoryBlackBoxAnalyzer(matrixCatalog).Analyze(matrixRequest);
        Check(withLab.MaterialComplete && withLab.PowerComplete,
            "one research station accepts every matrix made inside the blueprint");
        foreach (var itemId in Enumerable.Range(6001, 6))
        {
            var flow = withLab.ItemFlows[itemId];
            Equal(itemId == 6001 ? 12 : 3, flow.GrossProduction,
                "research does not replace the known gross matrix production ledger");
            Equal(itemId == 6001 ? 1 : 0, flow.GrossConsumption,
                "a research sink does not invent numerical matrix consumption");
            Check(flow.IsFinalProduct && flow.IsResearchProduct && flow.OverbuildSurplus == 0 &&
                  flow.CoproductSurplus == 0,
                "matrices absorbed by research remain final products without excess warnings");
        }
        Check(!withLab.ItemFlows[6001].IsExcessIntermediate,
            "matrix production absorbed by research is not flagged as overbuild");
        Equal(3, withLab.ItemFlows[1].SteadyStateExternalSupply.Value,
            "six-matrix production retains its full raw input throughput");
        Equal(3, withLab.ItemFlows[2].SteadyStateExternalSupply.Value,
            "one research station prevents throttling additional matrix-only producers");
        Equal(1800, withLab.Power.ConsumptionWatts,
            "one research station contributes its full power independently of matrix routing");
        var withLabText = BlueprintDescriptionFormatter.Format(withLab, TestItemTag,
            new BlueprintDescriptionLabels("Factory power", "Logistics power", "Missing: ", "Excess: ", "Unknown",
                "(known portion)", "Input: ", "Output: ", "Research: "));
        var researchOutput = string.Join(", ", Enumerable.Range(6001, 6)
            .Select(itemId => TestItemTag(itemId) + " x" + (itemId == 6001 ? 11 : 3)));
        Check(withLabText.Input == TestItemTag(1) + " x3, " + TestItemTag(2) + " x3" &&
              withLabText.Output == TestItemTag(8000) + " x1" &&
              withLabText.Research == researchOutput && withLabText.Warnings == "" &&
              withLabText.Description == "Input: " + withLabText.Input + "\nOutput: " + withLabText.Output +
              "\nResearch: " + researchOutput,
            "the net matrix output appears once in research, separate from ordinary final products");

        matrixRequest.Buildings[3] = new ProductionBuildingSnapshot(4, 91, 0, 2,
            ProliferationMode.None, operatingParameters: new Dictionary<string, double>
            {
                ["ResearchMode"] = 1, ["ResearchMatrixSink"] = 1,
                ["TechId"] = 401, ["ResearchSpeed"] = 1000000
            });
        var twoLabs = new FactoryBlackBoxAnalyzer(matrixCatalog).Analyze(matrixRequest);
        Equal(3, twoLabs.ItemFlows[2].SteadyStateExternalSupply.Value,
            "lab count and technology-specific research speed never throttle matrix producers");
        Equal(2800, twoLabs.Power.ConsumptionWatts,
            "each additional lab still contributes its rated power");

        var noTechMatrixCatalog = new ProductionCatalog(matrixCatalog.Items.Values,
            matrixCatalog.Recipes.Values, matrixCatalog.Buildings.Values,
            new[] { 0.0, 0.25, 0.5, 0.75, 1.0 },
            new[] { 0.0, 0.125, 0.2, 0.225, 0.25 },
            new[] { 1.0, 1.3, 1.7, 2.1, 2.5 },
            researchMatrixItemIds: matrixCatalog.ResearchMatrixItemIds);
        var noTechMatrixReport = new FactoryBlackBoxAnalyzer(noTechMatrixCatalog).Analyze(matrixRequest);
        Check(noTechMatrixReport.MaterialComplete && noTechMatrixReport.ItemFlows[6001].IsFinalProduct,
            "matrix producers remain final research output without technology records");
        Equal(3, noTechMatrixReport.ItemFlows[2].SteadyStateExternalSupply.Value,
            "unlocked-technology data cannot change upstream matrix throughput");

        var oneMatrixRequest = new FactoryBlackBoxRequest();
        oneMatrixRequest.Buildings.Add(matrixRequest.Buildings[2]);
        oneMatrixRequest.Buildings.Add(matrixRequest.Buildings[3]);
        var oneMatrix = new FactoryBlackBoxAnalyzer(matrixCatalog).Analyze(oneMatrixRequest);
        Check(oneMatrix.MaterialComplete && oneMatrix.ItemFlows.Keys.OrderBy(itemId => itemId)
                  .SequenceEqual(new[] { 2, 6001 }) && oneMatrix.ItemFlows[6001].IsFinalProduct &&
              oneMatrix.ItemFlows[6001].IsResearchProduct,
            "research output includes only the matrix types made in the blueprint");
        Equal(3, oneMatrix.ItemFlows[2].SteadyStateExternalSupply.Value,
            "a single produced matrix type retains full upstream inputs");
        var oneMatrixText = BlueprintDescriptionFormatter.Format(oneMatrix, TestItemTag,
            new BlueprintDescriptionLabels("Factory power", "Logistics power", "Missing: ", "Excess: ", "Unknown",
                "(known portion)", "Input: ", "Output: ", "Research: "));
        Check(oneMatrixText.Output == "" && oneMatrixText.Research == TestItemTag(6001) + " x6" &&
              oneMatrixText.Description == "Input: " + oneMatrixText.Input +
              "\nResearch: " + oneMatrixText.Research,
            "research-only output does not add an empty ordinary output line");

        var externalMatrixRequest = new FactoryBlackBoxRequest();
        externalMatrixRequest.Buildings.Add(matrixRequest.Buildings[1]);
        externalMatrixRequest.Buildings.Add(matrixRequest.Buildings[3]);
        var externalMatrix = new FactoryBlackBoxAnalyzer(matrixCatalog).Analyze(externalMatrixRequest);
        Equal(1, externalMatrix.ItemFlows[6001].RequiredExternalSupply,
            "other buildings still require externally supplied matrices when none are made");
        Equal(1, externalMatrix.ItemFlows[6001].SteadyStateExternalSupply.Value,
            "a research lab does not erase known external matrix requirements");

        request.Buildings[0] = new ProductionBuildingSnapshot(1, 91, 0, 1,
            ProliferationMode.None);
        var idle = analyzer.Analyze(request);
        Equal(100, idle.Power.ConsumptionWatts, "unconfigured lab contributes native idle demand");
    }

    private static void PlannerAuxiliaryPower()
    {
        var baseCatalog = Catalog();
        var catalog = new ProductionCatalog(baseCatalog.Items.Values,
            baseCatalog.Recipes.Values, baseCatalog.Buildings.Values.Concat(new[]
            {
                new ProductionBuilding(90, ProductionRecipeCategory.None, 0, 200, 20,
                    ProductionBuildingKind.Auxiliary)
            }), new[] { 0.0, 0.25, 0.5, 0.75, 1.0 },
            new[] { 0.0, 0.125, 0.2, 0.225, 0.25 },
            new[] { 1.0, 1.3, 1.7, 2.1, 2.5 });
        var planner = new ProductionPlanner(catalog);
        var request = Request((3, 60));
        var ordinary = planner.Calculate(request);
        request.AuxiliaryBuildings.Add(new ProductionBuildingSnapshot(1, 90, 0, 2,
            ProliferationMode.None));
        var withAuxiliary = planner.Calculate(request);
        Check(withAuxiliary.MaterialComplete && withAuxiliary.PowerComplete,
            "layout-supplied auxiliary building has complete power accounting");
        Equal(ordinary.Power.ConsumptionWatts + 400, withAuxiliary.Power.ConsumptionWatts,
            "two sorters contribute rated working demand without recipe material changes");
        Equal(400, withAuxiliary.Groups.Single(group => group.Process?.BuildingItemId == 90).ConsumptionWatts.Value,
            "auxiliary facility is independently attributable in the group breakdown");
        Check(withAuxiliary.Power.Scope == ProductionPowerScope.ProductionBuildingsAndAuxiliary,
            "planner explicitly identifies auxiliary power scope");
        Equal(ordinary.ItemFlows[1].ExternalImports, withAuxiliary.ItemFlows[1].ExternalImports,
            "auxiliary layout does not change solved materials");

        request.AuxiliaryBuildings.Add(new ProductionBuildingSnapshot(1, 90, 0, 1,
            ProliferationMode.None));
        var duplicate = planner.Calculate(request);
        Equal(withAuxiliary.Power.ConsumptionWatts, duplicate.Power.ConsumptionWatts,
            "duplicate auxiliary IDs are not counted twice");
        Check(duplicate.Diagnostics.Any(d => d.Code == ProductionDiagnosticCode.DuplicateSelection),
            "duplicate auxiliary IDs are diagnosed");

        request.AuxiliaryBuildings.Clear();
        request.AuxiliaryBuildings.Add(new ProductionBuildingSnapshot(2, 999, 0, 1,
            ProliferationMode.None));
        var missing = planner.Calculate(request);
        Check(missing.MaterialComplete && !missing.PowerComplete,
            "missing auxiliary rating does not discard complete material ledger");
        Check(!missing.PowerBreakdown.FactoryComplete && missing.PowerBreakdown.LogisticsComplete,
            "planner marks factory consumption incomplete without inventing logistics demand");
    }

    private static void BlueprintJointProduction()
    {
        var analyzer = new FactoryBlackBoxAnalyzer(BlueprintCatalog());
        var request = BlueprintRequest(2);
        var two = analyzer.Analyze(request);
        Check(two.MaterialComplete && two.PowerComplete, "two joint-production buildings are complete");
        Equal(120, two.ItemFlows[2].GrossProduction, "two upstream buildings run at full load");
        Equal(120, two.ItemFlows[2].GrossConsumption, "downstream demand stays at full load");
        Equal(0, two.ItemFlows[2].OverbuildSurplus.Value, "both upstream buildings are necessary");
        Equal(0, two.ItemFlows[3].OverbuildSurplus.Value, "joint output is not falsely redundant");
        Equal(90, two.ItemFlows[3].CoproductSurplus.Value, "necessary joint production leaves B90");
        Equal(4000, two.Power.ConsumptionWatts, "two upstream and one downstream consume full power");

        request.Buildings[0] = new ProductionBuildingSnapshot(1, 10, 610, 3, ProliferationMode.None);
        var three = analyzer.Analyze(request);
        Equal(180, three.ItemFlows[2].GrossProduction, "three upstream buildings retain their gross output");
        Equal(60, three.ItemFlows[2].OverbuildSurplus.Value, "third building's A60 is excess");
        Equal(60, three.ItemFlows[3].OverbuildSurplus.Value, "third building's B60 is excess");
        Equal(90, three.ItemFlows[3].CoproductSurplus.Value,
            "B90 from necessary buildings remains a separate coproduct");
        Equal(150, three.ItemFlows[3].Surplus, "original B150 balance is not replaced by reduced output");
        Equal(5000, three.Power.ConsumptionWatts, "the redundant building still consumes full power");
        Equal(3, three.ItemFlows[1].RequiredExternalSupply,
            "full-load raw material accounting remains unchanged");
        Equal(2, three.ItemFlows[1].SteadyStateExternalSupply.Value,
            "an idle redundant joint producer consumes no steady-state raw material");

        var reordered = new FactoryBlackBoxRequest();
        reordered.Buildings.Add(request.Buildings[1]);
        reordered.Buildings.Add(new ProductionBuildingSnapshot(103, 10, 610, 1, ProliferationMode.None));
        reordered.Buildings.Add(new ProductionBuildingSnapshot(101, 10, 610, 1, ProliferationMode.None));
        reordered.Buildings.Add(new ProductionBuildingSnapshot(102, 10, 610, 1, ProliferationMode.None));
        var reverse = analyzer.Analyze(reordered);
        foreach (var itemId in new[] { 2, 3 })
        {
            Equal(three.ItemFlows[itemId].OverbuildSurplus.Value, reverse.ItemFlows[itemId].OverbuildSurplus.Value,
                "reordered equivalent buildings retain one shared overbuild budget");
            Equal(three.ItemFlows[itemId].CoproductSurplus.Value, reverse.ItemFlows[itemId].CoproductSurplus.Value,
                "reordered equivalent buildings retain their coproduct amounts");
        }
        Equal(three.ItemFlows[1].SteadyStateExternalSupply.Value,
            reverse.ItemFlows[1].SteadyStateExternalSupply.Value,
            "steady-state inputs do not depend on blueprint building order");
    }

    private static void BlueprintSteadyStateInputs()
    {
        var catalog = BlueprintCatalog();
        var fractionalCatalog = new ProductionCatalog(catalog.Items.Values,
            catalog.Recipes.Values.Where(recipe => recipe.Id != 611).Concat(new[]
            {
                Recipe(611, new[] { (2, 90.0), (3, 30.0) }, new[] { (4, 1.0) })
            }), catalog.Buildings.Values, new[] { 0.0, 0.25, 0.5, 0.75, 1.0 },
            new[] { 0.0, 0.125, 0.2, 0.225, 0.25 },
            new[] { 1.0, 1.3, 1.7, 2.1, 2.5 });
        var fractional = new FactoryBlackBoxAnalyzer(fractionalCatalog).Analyze(BlueprintRequest(2));
        Equal(2, fractional.ItemFlows[1].RequiredExternalSupply,
            "the full-load report retains raw consumption below one-building headroom");
        Equal(1.5, fractional.ItemFlows[1].SteadyStateExternalSupply.Value,
            "part of an upstream building slows when its output exceeds downstream demand");
        Equal(0, fractional.ItemFlows[2].OverbuildSurplus.Value,
            "fractional throttling does not create a false whole-building overbuild warning");
        Equal(0, fractional.ItemFlows[2].CoproductSurplus.Value,
            "the limiting joint product's fractional headroom is not an unavoidable coproduct");
        Equal(90, fractional.ItemFlows[3].CoproductSurplus.Value,
            "necessary whole joint producers retain the other product's rated coproduct");

        var singleOutput = new FactoryBlackBoxRequest();
        singleOutput.Buildings.Add(new ProductionBuildingSnapshot(1, 10, 612, 1, ProliferationMode.None));
        singleOutput.Buildings.Add(new ProductionBuildingSnapshot(2, 20, 611, 1, ProliferationMode.None));
        var single = new FactoryBlackBoxAnalyzer(catalog).Analyze(singleOutput);
        Equal(0.5, single.ItemFlows[1].SteadyStateExternalSupply.Value,
            "single-output headroom reduces upstream raw inputs without losing final capacity");
        Equal(120, single.ItemFlows[2].SteadyStateExternalSupply.Value,
            "unavailable intermediate material remains an external input");

        var chainCatalog = new ProductionCatalog(new[]
            {
                new ProductionItem(1, 0, true, false, 0, 0),
                new ProductionItem(2, 710, false, false, 0, 0),
                new ProductionItem(3, 711, false, false, 0, 0),
                new ProductionItem(4, 712, false, false, 0, 0)
            }, new[]
            {
                Recipe(710, new[] { (1, 1.0) }, new[] { (2, 4.0) }),
                Recipe(711, new[] { (2, 4.0) }, new[] { (3, 2.0) }),
                Recipe(712, new[] { (3, 1.0) }, new[] { (4, 1.0) })
            }, new[] { new ProductionBuilding(10, ProductionRecipeCategory.Assemble, 1, 1000, 100) },
            new[] { 0.0 }, new[] { 0.0 }, new[] { 1.0 });
        var chainRequest = new FactoryBlackBoxRequest();
        chainRequest.Buildings.Add(new ProductionBuildingSnapshot(1, 10, 710, 1, ProliferationMode.None));
        chainRequest.Buildings.Add(new ProductionBuildingSnapshot(2, 10, 711, 1, ProliferationMode.None));
        chainRequest.Buildings.Add(new ProductionBuildingSnapshot(3, 10, 712, 1, ProliferationMode.None));
        var chain = new FactoryBlackBoxAnalyzer(chainCatalog).Analyze(chainRequest);
        Equal(0.5, chain.ItemFlows[1].SteadyStateExternalSupply.Value,
            "downstream throttling propagates through multiple upstream stages");
        Equal(0, chain.ItemFlows[2].SteadyStateExternalSupply.Value,
            "throttling does not invent external supply for balanced intermediates");
        Equal(1, chain.ItemFlows[4].GrossProduction,
            "final rated output remains unchanged when upstream inputs are capped");
        Equal(3000, chain.Power.ConsumptionWatts,
            "rated power remains distinct from estimated steady-state materials");
    }

    private static void BlueprintMixedProducersAndCycles()
    {
        var analyzer = new FactoryBlackBoxAnalyzer(BlueprintCatalog());
        var request = BlueprintRequest(3);
        request.Buildings.Add(new ProductionBuildingSnapshot(3, 10, 612, 1, ProliferationMode.None));
        var mixed = analyzer.Analyze(request);
        Equal(240, mixed.ItemFlows[3].GrossProduction, "single and joint B producers share the ledger");
        Equal(30, mixed.ItemFlows[3].GrossConsumption, "shared B keeps its downstream consumption");
        Equal(120, mixed.ItemFlows[3].OverbuildSurplus.Value,
            "one joint and one single-output building are both redundant");
        Equal(90, mixed.ItemFlows[3].CoproductSurplus.Value,
            "B90 from necessary joint production is not counted as excess again");
        Equal(60, mixed.ItemFlows[2].OverbuildSurplus.Value,
            "all products of the removable joint building are accounted for");
        Equal(6000, mixed.Power.ConsumptionWatts, "mixed producers keep original full-load power");
        Equal(2, mixed.ItemFlows[1].SteadyStateExternalSupply.Value,
            "shared intermediate producers use one steady-state raw material budget");

        var catalog = BlueprintCatalog();
        var consumedJointCatalog = new ProductionCatalog(catalog.Items.Values,
            catalog.Recipes.Values.Where(recipe => recipe.Id != 611).Concat(new[]
            {
                Recipe(611, new[] { (2, 60.0), (3, 90.0) }, new[] { (4, 1.0) })
            }), catalog.Buildings.Values, new[] { 0.0 }, new[] { 0.0 }, new[] { 1.0 });
        var consumedJointRequest = BlueprintRequest(1);
        consumedJointRequest.Buildings.Add(new ProductionBuildingSnapshot(3, 10, 612, 1,
            ProliferationMode.None));
        var consumedJoint = new FactoryBlackBoxAnalyzer(consumedJointCatalog).Analyze(consumedJointRequest);
        Equal(30, consumedJoint.ItemFlows[3].Surplus,
            "mixed producers retain their full-load ratio headroom");
        Equal(0, consumedJoint.ItemFlows[3].OverbuildSurplus.Value,
            "both mixed producers are needed at the whole-building level");
        Equal(0, consumedJoint.ItemFlows[3].CoproductSurplus.Value,
            "fully consumed joint output does not turn single-product headroom into a coproduct");
        Equal(1.5, consumedJoint.ItemFlows[1].SteadyStateExternalSupply.Value,
            "single-product headroom still reduces steady-state input");
        var consumedJointText = BlueprintDescriptionFormatter.Format(consumedJoint, TestItemTag,
            new BlueprintDescriptionLabels("Factory power", "Logistics power", "Missing: ", "Excess: ", "Unknown",
                "(known portion)", "Input: ", "Output: ", "Research: "));
        Check(!consumedJointText.Output.Contains(TestItemTag(3)),
            "the description does not export internally consumed joint products");

        var sharedJointCatalog = new ProductionCatalog(catalog.Items.Values,
            catalog.Recipes.Values.Where(recipe => recipe.Id != 611 && recipe.Id != 612).Concat(new[]
            {
                Recipe(611, new[] { (2, 60.0), (3, 90.0), (5, 60.0) }, new[] { (4, 1.0) }),
                Recipe(612, new[] { (1, 1.0) }, new[] { (3, 60.0), (5, 60.0) })
            }), catalog.Buildings.Values, new[] { 0.0 }, new[] { 0.0 }, new[] { 1.0 });
        var sharedJoint = new FactoryBlackBoxAnalyzer(sharedJointCatalog).Analyze(consumedJointRequest);
        Equal(30, sharedJoint.ItemFlows[3].CoproductSurplus.Value,
            "necessary joint producers share internal consumption before exposing a coproduct");
        Equal(0, sharedJoint.ItemFlows[3].OverbuildSurplus.Value,
            "shared coproduct output does not imply a removable joint producer");
        Equal(2, sharedJoint.ItemFlows[1].SteadyStateExternalSupply.Value,
            "both necessary joint producers retain their steady-state input");

        var reversedResults = new ProductionCatalog(catalog.Items.Values,
            catalog.Recipes.Values.Where(recipe => recipe.Id != 610).Concat(new[]
            {
                Recipe(610, new[] { (1, 1.0) }, new[] { (3, 60.0), (2, 60.0) })
            }), catalog.Buildings.Values,
            new[] { 0.0, 0.25, 0.5, 0.75, 1.0 },
            new[] { 0.0, 0.125, 0.2, 0.225, 0.25 },
            new[] { 1.0, 1.3, 1.7, 2.1, 2.5 });
        var alternate = new FactoryBlackBoxAnalyzer(reversedResults).Analyze(request);
        Equal(mixed.ItemFlows[3].OverbuildSurplus.Value, alternate.ItemFlows[3].OverbuildSurplus.Value,
            "overbuild does not depend on a recipe's result array order");
        Equal(mixed.ItemFlows[3].CoproductSurplus.Value, alternate.ItemFlows[3].CoproductSurplus.Value,
            "coproduct does not depend on a recipe's result array order");

        var shortage = analyzer.Analyze(BlueprintRequest(1));
        Equal(60, shortage.ItemFlows[2].IntermediateShortage,
            "joint production with insufficient A reports an intermediate shortage");
        Equal(60, shortage.ItemFlows[2].RequiredExternalSupply,
            "intermediate shortages remain external material requirements");
        Equal(60, shortage.ItemFlows[2].SteadyStateExternalSupply.Value,
            "stable throughput never worsens an existing intermediate shortage");
        Equal(0, shortage.ItemFlows[2].OverbuildSurplus.Value, "a needed producer cannot be removed");
        Equal(30, shortage.ItemFlows[3].CoproductSurplus.Value,
            "a known A shortage does not hide independently known B coproduct");
        Equal(3000, shortage.Power.ConsumptionWatts, "shortage does not throttle downstream power");

        request.Buildings.Clear();
        request.Buildings.Add(new ProductionBuildingSnapshot(1, 10, 620, 1, ProliferationMode.None));
        var returned = analyzer.Analyze(request);
        Equal(1, returned.ItemFlows[2].GrossProduction, "returned material keeps gross production");
        Equal(1, returned.ItemFlows[2].GrossConsumption, "returned material keeps gross consumption");
        Equal(0, returned.ItemFlows[2].RequiredExternalSupply,
            "internal return does not create a false external input");
        Check(!returned.ItemFlows[2].IsFinalProduct, "a pure material return is not a final product");
        Equal(60, returned.ItemFlows[3].Surplus, "the same recipe still supplies its net output");

        request.Buildings.Clear();
        request.Buildings.Add(new ProductionBuildingSnapshot(1, 10, 621, 1, ProliferationMode.None));
        request.Buildings.Add(new ProductionBuildingSnapshot(2, 10, 622, 1, ProliferationMode.None));
        var cycle = analyzer.Analyze(request);
        foreach (var itemId in new[] { 5, 6 })
        {
            Equal(1, cycle.ItemFlows[itemId].GrossProduction, "cyclic production stays gross");
            Equal(1, cycle.ItemFlows[itemId].GrossConsumption, "cyclic consumption stays gross");
            Equal(0, cycle.ItemFlows[itemId].IntermediateShortage, "balanced cycle has no missing inputs");
            Equal(0, cycle.ItemFlows[itemId].OverbuildSurplus.Value, "balanced cycle has no excess");
            Equal(0, cycle.ItemFlows[itemId].CoproductSurplus.Value, "balanced cycle has no coproduct");
        }
    }

    private static void BlueprintProliferationAndCompleteness()
    {
        var analyzer = new FactoryBlackBoxAnalyzer(BlueprintCatalog());
        var sprayedRequest = new FactoryBlackBoxRequest { ProliferationEnabled = true };
        sprayedRequest.Buildings.Add(new ProductionBuildingSnapshot(1, 10, 610, 1,
            ProliferationMode.ExtraProducts));
        sprayedRequest.Buildings.Add(new ProductionBuildingSnapshot(2, 10, 610, 1,
            ProliferationMode.Speedup));
        var sprayed = analyzer.Analyze(sprayedRequest);
        Equal(195, sprayed.ItemFlows[2].GrossProduction,
            "highest captured proliferation keeps distinct extra-product and speedup modes");
        Check(sprayed.Groups[0].Process.ProliferationMode == ProliferationMode.ExtraProducts &&
              sprayed.Groups[1].Process.ProliferationMode == ProliferationMode.Speedup &&
              sprayed.Groups.All(group => group.Process.ProliferationLevel == 4),
            "each building retains its native mode at the highest supported level");
        Check(!sprayed.ItemFlows.ContainsKey(7), "externally supplied input coating adds no inferred spray flow");
        sprayedRequest.ProliferationEnabled = false;
        var unsprayed = analyzer.Analyze(sprayedRequest);
        Equal(120, unsprayed.ItemFlows[2].GrossProduction, "unchecked proliferation restores plain capacity");
        Equal(2000, unsprayed.Power.ConsumptionWatts, "unchecked proliferation restores plain power");

        var request = BlueprintRequest(2);
        request.Buildings.Add(new ProductionBuildingSnapshot(3, 30, 0, 1, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double> { ["ChargePowerWatts"] = 600000000 }));
        var complete = analyzer.Analyze(request);
        Equal(4000, complete.PowerBreakdown.FactoryConsumptionWatts.Value,
            "factory consumption excludes selected logistics chargers");
        Equal(600000000, complete.PowerBreakdown.LogisticsConsumptionWatts.Value,
            "logistics consumption uses configured charging maximum");
        Equal(600004000, complete.Power.ConsumptionWatts, "the existing total still includes both categories");
        Equal(0, complete.Power.AccumulatorChargingWatts,
            "logistics charging does not become accumulator exchange");

        request.Buildings.Add(new ProductionBuildingSnapshot(4, 31, 0, 1, ProliferationMode.None));
        request.Buildings.Add(new ProductionBuildingSnapshot(5, 32, 0, 1, ProliferationMode.None));
        var withAuxiliaries = analyzer.Analyze(request);
        Equal(5100, withAuxiliaries.PowerBreakdown.FactoryConsumptionWatts.Value,
            "sorters and spray coaters add to factory rather than logistics consumption");
        Equal(600000000, withAuxiliaries.PowerBreakdown.LogisticsConsumptionWatts.Value,
            "non-logistics auxiliary watts do not change charging limits");
        request.Buildings.RemoveRange(3, 2);

        request.Buildings[2] = new ProductionBuildingSnapshot(3, 30, 0, 1, ProliferationMode.None);
        var missingLogistics = analyzer.Analyze(request);
        Check(missingLogistics.MaterialComplete && !missingLogistics.PowerComplete &&
              missingLogistics.PowerBreakdown.FactoryComplete &&
              !missingLogistics.PowerBreakdown.LogisticsComplete,
            "unknown logistics charging does not discard known factory watts");
        Equal(4000, missingLogistics.PowerBreakdown.FactoryConsumptionWatts.Value,
            "factory category remains usable when logistics power is unknown");

        request.Buildings[0] = new ProductionBuildingSnapshot(1, 10, 999, 2, ProliferationMode.None);
        request.Buildings[2] = new ProductionBuildingSnapshot(3, 30, 0, 1, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double> { ["ChargePowerWatts"] = 600000000 });
        var missingFactory = analyzer.Analyze(request);
        Check(!missingFactory.MaterialComplete && !missingFactory.PowerComplete &&
              !missingFactory.PowerBreakdown.FactoryComplete && missingFactory.PowerBreakdown.LogisticsComplete,
            "unknown factory process does not discard known logistics charging");
        Equal(600000000, missingFactory.PowerBreakdown.LogisticsConsumptionWatts.Value,
            "known logistics consumption remains independently reportable");

        request = BlueprintRequest(3);
        request.Buildings.Add(new ProductionBuildingSnapshot(3, 40, 0, 2, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double> { ["MachineSpeedFactor"] = 1.5 }));
        var partialMaterials = analyzer.Analyze(request);
        Check(!partialMaterials.MaterialComplete && !partialMaterials.PowerComplete &&
              partialMaterials.PowerBreakdown.FactoryComplete,
            "a miner lacking blueprint resource context retains calculable native demand");
        Equal(22500, partialMaterials.PowerBreakdown.FactoryConsumptionWatts.Value,
            "miner speed-ratio watts add to the other full-load factory consumers");
        Check(partialMaterials.ItemFlows[3].OverbuildSurplus == null &&
              partialMaterials.ItemFlows[3].CoproductSurplus == null,
            "partial materials cannot establish overbuild or coproduct conclusions");
        Equal(3, partialMaterials.ItemFlows[1].RequiredExternalSupply,
            "partial materials retain the full-load input ledger");
        Equal(2, partialMaterials.ItemFlows[1].SteadyStateExternalSupply.Value,
            "unrelated unknown settings do not prevent capping known upstream inputs");
        Equal(1, partialMaterials.ItemFlows[4].NetFlow,
            "partial material estimates keep independently known rated final outputs");

        var unknownRecipeRequest = BlueprintRequest(3);
        unknownRecipeRequest.Buildings.Add(new ProductionBuildingSnapshot(3, 10, 999, 1,
            ProliferationMode.None));
        var unknownRecipe = analyzer.Analyze(unknownRecipeRequest);
        Equal(3, unknownRecipe.ItemFlows[1].SteadyStateExternalSupply.Value,
            "unknown downstream recipe must not be mistaken for zero demand");
        Check(!unknownRecipe.ItemFlows[4].IsFinalProduct,
            "an unrecognized downstream recipe cannot establish a final product");

        var partialCatalog = new ProductionCatalog(BlueprintCatalog().Items.Values,
            BlueprintCatalog().Recipes.Values.Concat(new[]
            {
                new ProductionRecipe(613, ProductionRecipeCategory.Fractionate, 3600, false,
                    new[] { new KeyValuePair<int, double>(2, 1) },
                    new[] { new KeyValuePair<int, double>(4, 1) })
            }), BlueprintCatalog().Buildings.Values.Concat(new[]
            {
                new ProductionBuilding(50, ProductionRecipeCategory.Fractionate, 1, 300, 30,
                    ProductionBuildingKind.Fractionator)
            }), new[] { 0.0 }, new[] { 0.0 }, new[] { 1.0 });
        var incompleteDownstreamRequest = BlueprintRequest(3);
        incompleteDownstreamRequest.Buildings.Add(new ProductionBuildingSnapshot(3, 50, 613, 1,
            ProliferationMode.None));
        var incompleteDownstream = new FactoryBlackBoxAnalyzer(partialCatalog).Analyze(incompleteDownstreamRequest);
        Check(incompleteDownstream.Diagnostics.Any(diagnostic =>
              diagnostic.Code == ProductionDiagnosticCode.MissingOperatingParameter),
            "missing fractionator throughput leaves its known input demand unmodeled");
        Equal(3, incompleteDownstream.ItemFlows[1].SteadyStateExternalSupply.Value,
            "an unresolved but known downstream input prevents underestimating its upstream supply");
    }

    private static void BlueprintDescriptionFormattingAndFields()
    {
        var labels = new BlueprintDescriptionLabels("工厂耗电", "物流耗电", "缺少：", "多余：", "未知",
            "（已知部分）", "输入：", "输出：", "研究：");
        var joint = new FactoryBlackBoxAnalyzer(BlueprintCatalog()).Analyze(BlueprintRequest(3));
        var text = BlueprintDescriptionFormatter.Format(joint, TestItemTag, labels);
        Check(text.Power == "工厂耗电 5kW", "absent logistics consumption is omitted");
        var warnings = "多余：" + TestItemTag(2) + " x60, " + TestItemTag(3) + " x60";
        Check(text.Input == TestItemTag(1) + " x2",
            "input excludes warnings and reflects upstream throughput capped by downstream demand");
        Check(text.Output == TestItemTag(3) + " x90, " + TestItemTag(4) + " x1",
            "coproduct appears once alongside the final product");
        Check(text.Warnings == warnings && text.Description == warnings + "\n输入：" + text.Input +
              "\n输出：" + text.Output,
            "excess items share one labeled line before input and output");
        Check(!text.Input.Contains(((char)10).ToString()) && !text.Output.Contains(((char)10).ToString()),
            "each material line retains comma-separated entries without embedded breaks");

        var shortage = BlueprintDescriptionFormatter.Format(
            new FactoryBlackBoxAnalyzer(BlueprintCatalog()).Analyze(BlueprintRequest(1)), TestItemTag, labels);
        Check(shortage.Description.StartsWith("缺少：" + TestItemTag(2) + " x60\n输入：") &&
              !shortage.Input.Contains("缺少"),
            "shortage warnings are placed before inputs rather than inside them");

        var warningOnlyReport = new ProductionReport(ProductionStatus.Complete, true, true,
            new[]
            {
                new ProductionItemFlow(52, 2, 3, 0, 0, 0, 0, 1,
                    false, false, 0, Array.Empty<int>(), Array.Empty<int>(), 1, 0, 0),
                new ProductionItemFlow(53, 1, 3, 0, 0, 0, 0, 2,
                    false, false, 0, Array.Empty<int>(), Array.Empty<int>(), 2, 0, 0),
                new ProductionItemFlow(54, 3, 0, 0, 0, 0, 3, 0,
                    false, true, 3, Array.Empty<int>(), Array.Empty<int>(), 0, 3, 0)
            },
            Array.Empty<ProductionGroupFlow>(), Array.Empty<ProductionDiagnostic>(),
            new ProductionPower(0, 0, 0, 0, 0, 0, ProductionPowerScope.SelectedFacilities),
            new ProductionPowerBreakdown(0, 0, true, true));
        var warningOnly = BlueprintDescriptionFormatter.Format(warningOnlyReport, TestItemTag, labels);
        var warningLines = "缺少：" + TestItemTag(52) + " x1, " + TestItemTag(53) + " x2" +
                           "\n多余：" + TestItemTag(54) + " x3";
        Check(warningOnly.Warnings == warningLines && warningOnly.Description == warningLines &&
              warningOnly.Input == "" && warningOnly.Output == "" && warningOnly.Research == "",
            "missing and excess use separate aligned lines without repeating labels or adding empty lines");

        var powerOnly = new ProductionReport(ProductionStatus.Complete, true, true,
            Array.Empty<ProductionItemFlow>(), Array.Empty<ProductionGroupFlow>(),
            Array.Empty<ProductionDiagnostic>(), new ProductionPower(3100000000, 3100000000, 0, 0, 0, 0,
                ProductionPowerScope.SelectedFacilities),
            new ProductionPowerBreakdown(2500000000, 600000000, true, true));
        var large = BlueprintDescriptionFormatter.Format(powerOnly, TestItemTag, labels);
        Check(large.Power == "工厂耗电 2.5GW | 物流耗电 600MW",
            "gigawatt and megawatt display matches the native Chinese field example");
        Check(large.Input == "" && large.Output == "" && large.Description == "",
            "empty material categories add no description lines");

        var logisticsRequest = new FactoryBlackBoxRequest();
        logisticsRequest.Buildings.Add(new ProductionBuildingSnapshot(1, 30, 0, 1, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double> { ["ChargePowerWatts"] = 600000000 }));
        var logisticsOnly = BlueprintDescriptionFormatter.Format(
            new FactoryBlackBoxAnalyzer(BlueprintCatalog()).Analyze(logisticsRequest), TestItemTag, labels);
        Check(logisticsOnly.Power == "物流耗电 600MW" && logisticsOnly.Description == "",
            "logistics-only blueprints do not show empty factory or material categories");
        logisticsRequest.Buildings[0] = new ProductionBuildingSnapshot(1, 30, 0, 1, ProliferationMode.None);
        var unknownLogistics = BlueprintDescriptionFormatter.Format(
            new FactoryBlackBoxAnalyzer(BlueprintCatalog()).Analyze(logisticsRequest), TestItemTag, labels);
        Check(unknownLogistics.Power == "物流耗电 未知",
            "unknown charging demand is not omitted as an absent building");

        var request = BlueprintRequest(2);
        request.Buildings.Add(new ProductionBuildingSnapshot(3, 30, 0, 1, ProliferationMode.None,
            operatingParameters: new Dictionary<string, double> { ["ChargePowerWatts"] = 600000000 }));
        request.Buildings[0] = new ProductionBuildingSnapshot(1, 10, 999, 2, ProliferationMode.None);
        var missingFactory = new FactoryBlackBoxAnalyzer(BlueprintCatalog()).Analyze(request);
        var partial = BlueprintDescriptionFormatter.Format(missingFactory, TestItemTag, labels);
        Check(partial.Power == "工厂耗电 未知 | 物流耗电 600MW",
            "unknown factory demand cannot become zero or erase known logistics power");
        Check(partial.Input.Contains("（已知部分）"),
            "known material inputs are explicitly identified as partial");

        request = BlueprintRequest(3);
        request.Buildings.Add(new ProductionBuildingSnapshot(3, 40, 0, 1, ProliferationMode.None));
        var partialProduction = new FactoryBlackBoxAnalyzer(BlueprintCatalog()).Analyze(request);
        var knownText = BlueprintDescriptionFormatter.Format(partialProduction, TestItemTag, labels);
        Check(knownText.Input.Contains("（已知部分）") && knownText.Output.Contains("（已知部分）") &&
              !knownText.Input.Contains("多余") && !knownText.Output.Contains(TestItemTag(3)),
            "partial materials retain known rates without inferring excess or coproducts");
        Check(knownText.Input.Contains(TestItemTag(1) + " x2") &&
              !knownText.Input.Contains(TestItemTag(1) + " x3"),
            "partial descriptions cap known material inputs at downstream throughput");

        request = new FactoryBlackBoxRequest();
        request.Buildings.Add(new ProductionBuildingSnapshot(3, 40, 0, 1, ProliferationMode.None));
        var noMaterial = new FactoryBlackBoxAnalyzer(BlueprintCatalog()).Analyze(request);
        partial = BlueprintDescriptionFormatter.Format(noMaterial, TestItemTag, labels);
        Check(partial.Input == "" && partial.Output == "" && partial.Description == "",
            "unknown materials without known entries do not create empty lines");
        Check(partial.Power == "工厂耗电 4kW",
            "the miner's independent power remains available without its resource context");

        var failed = false;
        try
        {
            BlueprintDescriptionFormatter.Format(ProductionReport.Failure(new ProductionDiagnostic(
                ProductionDiagnosticCode.DataNotReady, "Data is not ready.")), TestItemTag, labels);
        }
        catch (InvalidOperationException)
        {
            failed = true;
        }

        Check(failed, "unavailable catalog cannot be formatted as a complete zero report");

        var fractional = new ProductionReport(ProductionStatus.Complete, true, true,
            new[]
            {
                new ProductionItemFlow(42, 0.125, 0, 0, 0, 0, 0.125, 0,
                    true, false, 0, Array.Empty<int>(), Array.Empty<int>()),
                new ProductionItemFlow(43, 1.2, 0, 0, 0, 0, 1.2, 0,
                    true, false, 0, Array.Empty<int>(), Array.Empty<int>())
            }, Array.Empty<ProductionGroupFlow>(), Array.Empty<ProductionDiagnostic>(),
            new ProductionPower(0, 0, 0, 0, 0, 0, ProductionPowerScope.SelectedFacilities),
            new ProductionPowerBreakdown(0, 0, true, true));
        var rounded = BlueprintDescriptionFormatter.Format(fractional, TestItemTag, labels);
        Check(rounded.Output == TestItemTag(42) + " x0.125, " + TestItemTag(43) + " x1.2",
            "display rounding retains small nonzero rates and omits unnecessary trailing zeros");
        Check(rounded.Power == "" && rounded.Input == "" &&
              rounded.Description == "输出：" + rounded.Output,
            "no consumers means no power field and an output-only report has one line");

        var inputOnlyReport = new ProductionReport(ProductionStatus.Complete, true, true,
            new[] { new ProductionItemFlow(51, 0, 3, 0, 0, 0, 0, 3,
                false, false, 0, Array.Empty<int>(), Array.Empty<int>()) },
            Array.Empty<ProductionGroupFlow>(), Array.Empty<ProductionDiagnostic>(),
            new ProductionPower(0, 0, 0, 0, 0, 0, ProductionPowerScope.SelectedFacilities),
            new ProductionPowerBreakdown(0, 0, true, true));
        var inputOnly = BlueprintDescriptionFormatter.Format(inputOnlyReport, TestItemTag, labels);
        Check(inputOnly.Output == "" && inputOnly.Description == "输入：" + inputOnly.Input,
            "an input-only report omits the empty output line");

        var longReport = new ProductionReport(ProductionStatus.Complete, true, true,
            Enumerable.Range(1000, 140).Select(itemId => new ProductionItemFlow(itemId, 1.2, 0, 0, 0, 0, 1.2, 0,
                true, false, 0, Array.Empty<int>(), Array.Empty<int>())),
            Array.Empty<ProductionGroupFlow>(), Array.Empty<ProductionDiagnostic>(),
            new ProductionPower(0, 0, 0, 0, 0, 0, ProductionPowerScope.SelectedFacilities),
            new ProductionPowerBreakdown(0, 0, true, true));
        var longText = BlueprintDescriptionFormatter.Format(longReport, TestItemTag, labels);
        Check(longText.Description.Length > BlueprintDescriptionText.MaximumDescriptionLength &&
              !longText.Description.Contains("\n") && longText.Description.Contains(TestItemTag(1139)),
            "long material lines are not truncated and can be refused at the native description limit");
        TestNativeFields();
    }

    private static void TestNativeFields()
    {
        var firstFields = TestBlueprintFields("2.5GW");
        var result = BlueprintDescriptionFields.TryUpdate("Other:unchanged;", firstFields, 20,
            EscapeNativeField, UnescapeNativeField, out var generated);
        Check(result == BlueprintFieldUpdateResult.Updated && generated == "Other:unchanged;Power:2.5GW;",
            "only the power field is appended while unrelated fields remain intact");

        var escapedFields = TestBlueprintFields("2.5GW:250; " + (char)92 + "extra");
        result = BlueprintDescriptionFields.TryUpdate("", escapedFields, 20,
            EscapeNativeField, UnescapeNativeField, out var escaped);
        var escapedPower = escaped.Split(';')[0];
        Check(result == BlueprintFieldUpdateResult.Updated &&
              UnescapeNativeField(escapedPower.Substring(escapedPower.IndexOf(':') + 1)) == escapedFields[0].Value,
            "native backslash, semicolon, and colon escaping still round-trips power fields");

        result = BlueprintDescriptionFields.TryUpdate(generated,
            TestBlueprintFields("3GW"), 20,
            EscapeNativeField, UnescapeNativeField, out var repeated);
        Check(result == BlueprintFieldUpdateResult.Updated && repeated == "Other:unchanged;Power:3GW;",
            "repeated generation updates the power field without appending duplicates");

        result = BlueprintDescriptionFields.TryUpdate(
            "Note:keep;Input (per minute):manual;Output (per minute):manual;Power:old;", firstFields, 20,
            EscapeNativeField, UnescapeNativeField, out var preserved);
        Check(result == BlueprintFieldUpdateResult.Updated &&
              preserved == "Note:keep;Input (per minute):manual;Output (per minute):manual;Power:2.5GW;",
            "only the power field is updated; input and output custom fields remain untouched");

        var partialExisting = "Unrelated:hand edited;Input:manual;";
        result = BlueprintDescriptionFields.TryUpdate(partialExisting,
            TestBlueprintFields("100MW"), 20,
            EscapeNativeField, UnescapeNativeField, out var updated);
        Check(result == BlueprintFieldUpdateResult.Updated &&
              updated == "Unrelated:hand edited;Input:manual;Power:100MW;",
            "a custom input field remains unchanged when power is appended");

        var chineseFields = new[]
        {
            new BlueprintDescriptionField("耗电", "新功率", "Power", "耗电")
        };
        result = BlueprintDescriptionFields.TryUpdate("Power:old;输入:manual;Output (per minute):manual;",
            chineseFields, 3, EscapeNativeField, UnescapeNativeField, out var translated);
        Check(result == BlueprintFieldUpdateResult.Updated &&
              translated == "耗电:新功率;输入:manual;Output (per minute):manual;",
            "English and Chinese power titles match while other localized fields remain unchanged");

        var duplicateNames = "Power:old;耗电:old;Note:Power Input Output;输入（每分钟）:manual;Output:manual;";
        result = BlueprintDescriptionFields.TryUpdate(duplicateNames,
            TestBlueprintFields("new"), 5, EscapeNativeField, UnescapeNativeField,
            out var keptDuplicates);
        Check(result == BlueprintFieldUpdateResult.Updated &&
              keptDuplicates == "Power:new;Note:Power Input Output;输入（每分钟）:manual;Output:manual;",
            "duplicate power slots are merged without modifying other titled fields");

        var noPowerFields = TestBlueprintFields(null);
        result = BlueprintDescriptionFields.TryUpdate("Other:keep;Power:old;耗电:old;输入:manual;Output:manual;",
            noPowerFields, 20, EscapeNativeField, UnescapeNativeField, out var removed);
        Check(result == BlueprintFieldUpdateResult.Updated && removed == "Other:keep;输入:manual;Output:manual;",
            "a blueprint without consumers removes only stale power fields");
        result = BlueprintDescriptionFields.TryUpdate("", noPowerFields, 20,
            EscapeNativeField, UnescapeNativeField, out var empty);
        Check(result == BlueprintFieldUpdateResult.Updated && empty == "",
            "empty power and material categories do not create any custom fields");

        var full = string.Concat(Enumerable.Range(0, 20).Select(index => "Custom" + index + ":keep;"));
        result = BlueprintDescriptionFields.TryUpdate(full, firstFields, 20,
            EscapeNativeField, UnescapeNativeField, out var rejected);
        Check(result == BlueprintFieldUpdateResult.CapacityExceeded && rejected == full,
            "a missing power field at native capacity causes an all-or-nothing refusal");
        result = BlueprintDescriptionFields.TryUpdate(full, noPowerFields, 20,
            EscapeNativeField, UnescapeNativeField, out updated);
        Check(result == BlueprintFieldUpdateResult.Updated && updated == full,
            "description-only generation needs no custom-field slot even at native capacity");
        var onePresent = "Power:old;" + string.Concat(Enumerable.Range(0, 19)
            .Select(index => "Custom" + index + ":keep;"));
        result = BlueprintDescriptionFields.TryUpdate(onePresent, firstFields, 20,
            EscapeNativeField, UnescapeNativeField, out updated);
        Check(result == BlueprintFieldUpdateResult.Updated &&
              updated == "Power:2.5GW;" + onePresent.Substring("Power:old;".Length),
            "existing power fields can be updated at the native field limit");
        var otherFieldsAtCapacity = "Input:manual;Output:manual;" + string.Concat(Enumerable.Range(0, 18)
            .Select(index => "Custom" + index + ":keep;"));
        result = BlueprintDescriptionFields.TryUpdate(otherFieldsAtCapacity, firstFields, 20,
            EscapeNativeField, UnescapeNativeField, out rejected);
        Check(result == BlueprintFieldUpdateResult.CapacityExceeded && rejected == otherFieldsAtCapacity,
            "other input and output fields cannot be removed to make room for power");

        result = BlueprintDescriptionFields.TryUpdate("Unterminated:old", firstFields, 20,
            EscapeNativeField, UnescapeNativeField, out rejected);
        Check(result == BlueprintFieldUpdateResult.InvalidFields && rejected == "Unterminated:old",
            "invalid native serialization is rejected without changing existing text");
        foreach (var malformed in new[] { "MissingDelimiter;", "Too:many:delimiters;", "Valid:value;;" })
        {
            Check(!BlueprintDescriptionFields.IsNativeFormatValid(malformed),
                "malformed native fields are detected before the inspector collects pending edits");
            result = BlueprintDescriptionFields.TryUpdate(malformed, firstFields, 20,
                EscapeNativeField, UnescapeNativeField, out rejected);
            Check(result == BlueprintFieldUpdateResult.InvalidFields && rejected == malformed,
                "malformed native fields cannot be partially replaced or silently discarded");
        }
    }

    private static BlueprintDescriptionField[] TestBlueprintFields(string power)
    {
        return new[] { new BlueprintDescriptionField("Power", power, "耗电") };
    }

    private static string EscapeNativeField(string value)
    {
        var serialized = new System.Text.StringBuilder();
        foreach (var character in value)
        {
            if (character == (char)92) serialized.Append((char)92).Append((char)92);
            else if (character == ';') serialized.Append((char)92).Append('b');
            else if (character == ':') serialized.Append((char)92).Append('c');
            else serialized.Append(character);
        }

        return serialized.ToString();
    }

    private static string UnescapeNativeField(string value)
    {
        var text = new System.Text.StringBuilder();
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] != (char)92 || index + 1 >= value.Length)
            {
                text.Append(value[index]);
                continue;
            }

            index++;
            switch (value[index])
            {
                case 'b': text.Append(';'); break;
                case 'c': text.Append(':'); break;
                case (char)92: text.Append((char)92); break;
                default: text.Append((char)92).Append(value[index]); break;
            }
        }

        return text.ToString();
    }

    private static string TestItemTag(int itemId)
    {
        return ((char)92).ToString() + "item" + itemId + "-;";
    }

    private static FactoryBlackBoxRequest BlueprintRequest(int upstreamBuildings)
    {
        var request = new FactoryBlackBoxRequest();
        request.Buildings.Add(new ProductionBuildingSnapshot(1, 10, 610, upstreamBuildings,
            ProliferationMode.None));
        request.Buildings.Add(new ProductionBuildingSnapshot(2, 20, 611, 1, ProliferationMode.None));
        return request;
    }

    private static ProductionCatalog BlueprintCatalog()
    {
        return new ProductionCatalog(new[]
        {
            new ProductionItem(1, 0, true, false, 0, 0),
            new ProductionItem(2, 610, false, false, 0, 0),
            new ProductionItem(3, 610, false, false, 0, 0),
            new ProductionItem(4, 611, false, false, 0, 0),
            new ProductionItem(5, 621, false, false, 0, 0),
            new ProductionItem(6, 622, false, false, 0, 0),
            new ProductionItem(7, 0, false, false, 4, 60)
        }, new[]
        {
            Recipe(610, new[] { (1, 1.0) }, new[] { (2, 60.0), (3, 60.0) }),
            Recipe(611, new[] { (2, 120.0), (3, 30.0) }, new[] { (4, 1.0) }),
            Recipe(612, new[] { (1, 1.0) }, new[] { (3, 60.0) }),
            Recipe(620, new[] { (1, 1.0), (2, 1.0) }, new[] { (2, 1.0), (3, 60.0) }),
            Recipe(621, new[] { (5, 1.0) }, new[] { (6, 1.0) }),
            Recipe(622, new[] { (6, 1.0) }, new[] { (5, 1.0) })
        }, new[]
        {
            new ProductionBuilding(10, ProductionRecipeCategory.Assemble, 1, 1000, 100),
            new ProductionBuilding(20, ProductionRecipeCategory.Assemble, 1, 2000, 200),
            new ProductionBuilding(30, ProductionRecipeCategory.None, 0, 600000000, 0,
                ProductionBuildingKind.Logistics),
            new ProductionBuilding(31, ProductionRecipeCategory.None, 0, 900, 50,
                ProductionBuildingKind.Auxiliary),
            new ProductionBuilding(32, ProductionRecipeCategory.None, 0, 200, 10,
                ProductionBuildingKind.Auxiliary),
            new ProductionBuilding(40, ProductionRecipeCategory.None, 0, 4000, 200,
                ProductionBuildingKind.Miner, minerKind: ProductionMinerKind.Vein, miningPeriodTicks: 60000)
        }, new[] { 0.0, 0.25, 0.5, 0.75, 1.0 },
            new[] { 0.0, 0.125, 0.2, 0.225, 0.25 },
            new[] { 1.0, 1.3, 1.7, 2.1, 2.5 });
    }

    private static ProductionPlanRequest Request(params (int id, double rate)[] targets)
    {
        var request = new ProductionPlanRequest();
        foreach (var target in targets) request.Targets.Add(new ProductionTarget(target.id, target.rate));
        request.BuildingByCategory[ProductionRecipeCategory.Assemble] = 10;
        return request;
    }

    private static ProductionCatalog Catalog()
    {
        var items = new[]
        {
            new ProductionItem(1, 0, true, false, 0, 0),
            new ProductionItem(2, 101, false, false, 0, 0),
            new ProductionItem(3, 102, false, false, 0, 0),
            new ProductionItem(4, 103, false, false, 0, 0),
            new ProductionItem(5, 104, false, false, 4, 60)
        };
        var recipes = new[]
        {
            Recipe(101, new[] { (1, 1.0) }, new[] { (2, 2.0) }),
            Recipe(102, new[] { (2, 1.0) }, new[] { (3, 1.0), (4, 0.5) }),
            Recipe(103, new[] { (1, 1.0) }, new[] { (4, 1.0) }),
            Recipe(104, new[] { (1, 1.0) }, new[] { (5, 1.0) })
        };
        return new ProductionCatalog(items, recipes,
            new[] { new ProductionBuilding(10, ProductionRecipeCategory.Assemble, 1, 1000, 200) },
            new[] { 0.0, 0.25, 0.5, 0.75, 1.0 },
            new[] { 0.0, 0.125, 0.2, 0.225, 0.25 },
            new[] { 1.0, 1.3, 1.7, 2.1, 2.5 });
    }

    // Synthetic recipes last 3600 ticks, so a unit-speed building completes one cycle per minute.
    private static ProductionRecipe Recipe(int id, (int id, double count)[] input,
        (int id, double count)[] output, bool productive = true)
    {
        return new ProductionRecipe(id, ProductionRecipeCategory.Assemble, 3600, productive,
            input.Select(flow => new KeyValuePair<int, double>(flow.id, flow.count)),
            output.Select(flow => new KeyValuePair<int, double>(flow.id, flow.count)));
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }

    private static void Equal(double expected, double actual, string message)
    {
        Check(Math.Abs(expected - actual) < 1e-6, $"{message}: expected {expected}, got {actual}");
    }
}

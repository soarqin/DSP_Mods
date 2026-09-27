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
    }

    private static void ResearchHashesAndMatrices()
    {
        var tech = new ProductionTechnology(401, new[]
        {
            new KeyValuePair<int, double>(6001, 2), new KeyValuePair<int, double>(6002, 1)
        });
        var catalog = new ProductionCatalog(new[]
            {
                new ProductionItem(6001, 0, false, false, 0, 0),
                new ProductionItem(6002, 0, false, false, 0, 0),
                new ProductionItem(7000, 0, false, false, 4, 75)
            }, Array.Empty<ProductionRecipe>(), new[]
            {
                new ProductionBuilding(91, ProductionRecipeCategory.Research, 1, 1000, 100)
            }, new[] { 0.0, 0.25, 0.5, 0.75, 1.0 },
            new[] { 0.0, 0.125, 0.2, 0.225, 0.25 },
            new[] { 1.0, 1.3, 1.7, 2.1, 2.5 }, new[] { tech });
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

        settings.Remove("TechId");
        request.Buildings[0] = new ProductionBuildingSnapshot(1, 91, 0, 1,
            ProliferationMode.None, operatingParameters: settings);
        var missing = analyzer.Analyze(request);
        Check(!missing.MaterialComplete && missing.Diagnostics.Any(d =>
                d.Code == ProductionDiagnosticCode.MissingOperatingParameter),
            "missing technology cannot silently report zero matrices");

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

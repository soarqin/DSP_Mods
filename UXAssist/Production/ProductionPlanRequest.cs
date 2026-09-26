using System.Collections.Generic;

namespace UXAssist.Production;

public sealed class ProductionTarget
{
    public int ItemId { get; }
    public double ItemsPerMinute { get; }

    public ProductionTarget(int itemId, double itemsPerMinute)
    {
        ItemId = itemId;
        ItemsPerMinute = itemsPerMinute;
    }
}

public sealed class ProductionPlanRequest
{
    public List<ProductionTarget> Targets { get; } = new List<ProductionTarget>();
    public HashSet<int> ExternalMaterials { get; } = new HashSet<int>();
    public Dictionary<int, int> RecipeByItem { get; } = new Dictionary<int, int>();
    public Dictionary<int, ProliferationMode> ProliferationModeByItem { get; } =
        new Dictionary<int, ProliferationMode>();
    public Dictionary<int, int> BuildingByRecipe { get; } = new Dictionary<int, int>();
    public Dictionary<ProductionRecipeCategory, int> BuildingByCategory { get; } =
        new Dictionary<ProductionRecipeCategory, int>();
    public Dictionary<int, double> KnownExternalSurplus { get; } = new Dictionary<int, double>();
    public HashSet<int> SprayDeliveredItems { get; } = new HashSet<int>();
    public Dictionary<int, Dictionary<string, double>> OperatingParametersByRecipe { get; } =
        new Dictionary<int, Dictionary<string, double>>();
    public List<ProductionBuildingSnapshot> AuxiliaryBuildings { get; } = new List<ProductionBuildingSnapshot>();
    public bool ProliferationEnabled { get; set; }
    public int ProliferatorItemId { get; set; }
    public bool SelfSprayProliferator { get; set; }
    public int IterationBudget { get; set; } = 20000;
}

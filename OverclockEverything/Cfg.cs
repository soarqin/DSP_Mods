namespace OverclockEverything;

public struct Cfg
{
    public Cfg()
    {
    }

    public readonly uint[] BeltSpeed = {
        1, 2, 5
    };
    public int SorterSpeedMultiplier = 1;
    public int SorterPowerConsumptionMultiplier = 1;
    public int AssembleSpeedMultiplier = 1;
    public int AssemblePowerConsumptionMultiplier = 1;
    public int ResearchSpeedMultiplier = 1;
    public int LabPowerConsumptionMultiplier = 1;
    public int MinerSpeedMultiplier = 1;
    public int MinerPowerConsumptionMultiplier = 1;
    public long PowerGenerationMultiplier = 1;
    public long PowerFuelConsumptionMultiplier = 1;
    public long PowerSupplyAreaMultiplier = 1;
    public int EjectMultiplier = 1;
    public int SiloMultiplier = 1;
    public int InventoryStackMultiplier = 1;
}

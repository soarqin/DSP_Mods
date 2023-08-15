using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Mono.Cecil;

namespace DustbinPreloader;

public static class Preloader
{
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("Dustbin Preloader");      
    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

    public static void Patch(AssemblyDefinition assembly)
    {
        var gameModule = assembly.MainModule;
        try
        {
            // Add field: int StorageComponent.IsDustbin;
            gameModule.GetType("StorageComponent").AddFied("IsDustbin", gameModule.TypeSystem.Boolean);
        }
        catch (Exception e)
        {
            Logger.LogError("Failed to add `bool StorageComponent.IsDustbin`!");
            Logger.LogError(e);
        }
        try
        {
            // Add field: int TankComponent.IsDustbin;
            gameModule.GetType("TankComponent").AddFied("IsDustbin", gameModule.TypeSystem.Boolean);
        }
        catch (Exception e)
        {
            Logger.LogError("Failed to add `bool TankComponent.IsDustbin`!");
            Logger.LogError(e);
        }
    }

    private static void AddFied(this TypeDefinition typeDefinition, string fieldName, TypeReference fieldType)
    {
        var newField = new FieldDefinition(fieldName, FieldAttributes.Public, fieldType);
        typeDefinition.Fields.Add(newField);
        Logger.LogDebug("Add " + newField);
    }
}

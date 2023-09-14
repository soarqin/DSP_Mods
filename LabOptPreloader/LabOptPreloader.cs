using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Mono.Cecil;

namespace LabOptPreloader;

public static class Preloader
{
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("LabOpt Preloader");      
    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

    public static void Patch(AssemblyDefinition assembly)
    {
        var gameModule = assembly.MainModule;
        try
        {
            // Add field: int LabComponent.rootLabId;
            gameModule.GetType("LabComponent").AddFied("rootLabId", gameModule.TypeSystem.Int32);
        }
        catch (Exception e)
        {
            Logger.LogError("Failed to add `int LabComponent.rootLabId`!");
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

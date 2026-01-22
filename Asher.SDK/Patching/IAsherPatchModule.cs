using HarmonyLib;
using System;
using System.Collections.Generic;

public interface IAsherPatchModule
{
    string Name { get; }
    IEnumerable<Type> GetPatchTypes();
    void Apply(Harmony harmony);
}

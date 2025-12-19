namespace Asher.Core.Models
{
    public class HarmonyPatchInfo : BindableBase
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string AssemblyPath { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new();
        public List<string> Conflicts { get; set; } = new();
        public int Priority { get; set; } = 1000;
        public HarmonyPatchType PatchType { get; set; } = HarmonyPatchType.Prefix;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    public class HarmonyValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public string TargetMethod { get; set; } = string.Empty;
        public string AssemblyName { get; set; } = string.Empty;
    }

    public enum HarmonyPatchType
    {
        Prefix,
        Postfix,
        Transpiler,
        Finalizer
    }

    ///// <summary>
    ///// Example Harmony patch that demonstrates the patching system
    ///// This would be compiled into a separate DLL and loaded by Asher
    ///// </summary>
    //public class ExampleHarmonyPatch
    //{
    //    /// <summary>
    //    /// Prefix patch that runs before the original method
    //    /// </summary>
    //    /// <param name="__instance">The instance of the class (if instance method)</param>
    //    /// <param name="__result">The return value (can be modified)</param>
    //    /// <returns>true to continue execution, false to skip the original method</returns>
    //    public static bool Prefix(object __instance, ref object __result)
    //    {
    //        // This runs before the original method
    //        // You can modify parameters, skip execution, etc.

    //        // Example: Log that the method was called
    //        Console.WriteLine("ExampleHarmonyPatch: Method called");

    //        // Return true to continue with the original method
    //        return true;
    //    }

    //    /// <summary>
    //    /// Postfix patch that runs after the original method
    //    /// </summary>
    //    /// <param name="__instance">The instance of the class (if instance method)</param>
    //    /// <param name="__result">The return value (can be modified)</param>
    //    public static void Postfix(object __instance, ref object __result)
    //    {
    //        // This runs after the original method
    //        // You can modify the return value, log results, etc.

    //        // Example: Log the result
    //        Console.WriteLine($"ExampleHarmonyPatch: Method completed with result: {__result}");
    //    }

    //    /// <summary>
    //    /// Transpiler patch that modifies the IL code
    //    /// </summary>
    //    /// <param name="instructions">The original IL instructions</param>
    //    /// <returns>The modified IL instructions</returns>
    //    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    //    {
    //        // This modifies the IL code of the method
    //        // Advanced usage for complex modifications

    //        foreach (var instruction in instructions)
    //        {
    //            // Example: Add logging before each instruction
    //            yield return instruction;
    //        }
    //    }

    //    /// <summary>
    //    /// Finalizer patch that handles exceptions
    //    /// </summary>
    //    /// <param name="__exception">The exception that occurred</param>
    //    /// <returns>The exception to throw (can be null to suppress)</returns>
    //    public static Exception Finalizer(Exception __exception)
    //    {
    //        // This handles exceptions from the original method
    //        // You can log, modify, or suppress exceptions

    //        if (__exception != null)
    //        {
    //            Console.WriteLine($"ExampleHarmonyPatch: Exception caught: {__exception.Message}");
    //        }

    //        // Return null to suppress the exception, or return the original exception
    //        return __exception;
    //    }
    //}

    ///// <summary>
    ///// Example patch for a specific game method
    ///// This would target a method in Dust: An Elysian Tail
    ///// </summary>
    //public class GameMethodPatch
    //{
    //    /// <summary>
    //    /// Example patch for a game method
    //    /// This is just an example - the actual method would depend on the game's code
    //    /// </summary>
    //    public static void Postfix(ref int health)
    //    {
    //        // Example: Modify player health
    //        if (health < 10)
    //        {
    //            health = 10; // Ensure minimum health
    //            Console.WriteLine("GameMethodPatch: Ensured minimum health of 10");
    //        }
    //    }
    //}
}

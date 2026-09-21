using Asher.Services.Interfaces;
using System.Collections;

namespace Asher.Services.Platform
{
    public sealed class SystemEnvironmentProvider : IEnvironmentProvider
    {
        public IReadOnlyDictionary<string, string> GetEnvironmentVariables()
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                if (entry.Key is string key && entry.Value is string value)
                    values[key] = value;
            }

            return values;
        }
    }
}

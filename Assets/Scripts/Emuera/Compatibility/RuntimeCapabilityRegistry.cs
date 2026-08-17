using System;
using System.Collections.Generic;

namespace MinorShift.Emuera.Compatibility
{
    /// <summary>
    /// Runtime-side registry inspected by the parity generator.
    /// This registry describes runtime boundaries, not support claims.
    /// </summary>
    public static class RuntimeCapabilityRegistry
    {
        public const string Emuera = "runtime.emuera";
        public const string EraElectron = "runtime.eraelectron";

        public static IReadOnlyCollection<string> RegisteredRuntimes { get; } =
            new[] { Emuera, EraElectron };
    }
}

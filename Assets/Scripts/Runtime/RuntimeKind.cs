namespace uEmuera.Runtime
{
    /// <summary>
    /// Identifies which game runtime family a game package requires.
    /// Add new values here when a third runtime is introduced; do not
    /// re-number existing values.
    /// </summary>
    public enum RuntimeKind
    {
        /// <summary>Runtime not yet detected or explicitly unset.</summary>
        Unknown = 0,

        /// <summary>
        /// Traditional Emuera/EraBasic/EM+EE interpreter.
        /// Entry: ERB directory. Config: emuera.config.
        /// </summary>
        Emuera = 1,

        /// <summary>
        /// EraElectron (JavaScript / Vue / Node) runtime.
        /// Entry: main JS bundle. Config: ere.config.json or equivalent.
        /// </summary>
        EraElectron = 2,
    }
}

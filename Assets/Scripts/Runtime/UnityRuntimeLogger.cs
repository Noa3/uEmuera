namespace uEmuera.Runtime
{
    /// <summary>
    /// <see cref="IRuntimeLogger"/> that forwards messages to Unity's Debug log.
    /// </summary>
    public sealed class UnityRuntimeLogger : IRuntimeLogger
    {
        readonly string _prefix;

        public UnityRuntimeLogger(string prefix = "")
        {
            _prefix = string.IsNullOrEmpty(prefix) ? "" : prefix + " ";
        }

        public void Info(string message) =>
            UnityEngine.Debug.Log(_prefix + message);

        public void Warn(string message) =>
            UnityEngine.Debug.LogWarning(_prefix + message);

        public void Error(string message, System.Exception exception = null)
        {
            if (exception != null)
                UnityEngine.Debug.LogException(exception);
            else
                UnityEngine.Debug.LogError(_prefix + message);
        }
    }
}

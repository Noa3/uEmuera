using System;
using MinorShift.Emuera.Sub;

namespace MinorShift.Emuera.GameProc
{
    /// <summary>
    /// Original failure captured while compiling a deferred ERB file.
    /// Keeping this separate from FunctionCompileState prevents a later lookup from
    /// replacing a parser error with a misleading "function not found" message.
    /// </summary>
    internal sealed class LazyCompileFailure
    {
        public readonly string DisplayName;
        public readonly string FilePath;
        public readonly string FunctionName;
        public readonly string Message;
        public readonly ScriptPosition Position;

        LazyCompileFailure(string displayName, string filePath, string functionName,
            string message, ScriptPosition position)
        {
            DisplayName = displayName ?? string.Empty;
            FilePath = filePath ?? string.Empty;
            FunctionName = functionName ?? string.Empty;
            Message = string.IsNullOrEmpty(message) ? "Deferred ERB compilation failed" : message;
            Position = position;
        }

        public static LazyCompileFailure CreateException(string displayName, string filePath, Exception exception)
        {
            EmueraException emueraException = exception as EmueraException;
            return new LazyCompileFailure(
                displayName,
                filePath,
                string.Empty,
                exception == null ? null : exception.Message,
                emueraException == null ? null : emueraException.Position);
        }

        public static LazyCompileFailure CreateParser(string displayName, string filePath, FunctionLabelLine label)
        {
            return new LazyCompileFailure(
                displayName,
                filePath,
                label == null ? string.Empty : label.LabelName,
                label == null ? null : label.ErrMes,
                label == null ? null : label.Position);
        }

        public static LazyCompileFailure CreateLine(string displayName, string filePath, LogicalLine line)
        {
            return new LazyCompileFailure(
                displayName,
                filePath,
                line == null || line.ParentLabelLine == null ? string.Empty : line.ParentLabelLine.LabelName,
                line == null ? null : line.ErrMes,
                line == null ? null : line.Position);
        }

        public static LazyCompileFailure CreateGeneric(string displayName, string filePath)
        {
            return new LazyCompileFailure(displayName, filePath, string.Empty, null, null);
        }

        public CodeEE ToCodeException()
        {
            return Position == null ? new CodeEE(Message) : new CodeEE(Message, Position);
        }

        public override string ToString()
        {
            string location = Position == null ? FilePath : Position.ToString();
            return string.Format("@{0} {1}: {2}", FunctionName, location, Message);
        }
    }
}

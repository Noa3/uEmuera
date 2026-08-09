using uEmuera.Drawing;

namespace MinorShift.Emuera.GameView
{
    /// <summary>
	/// Colored
	/// </summary>
	abstract partial class AConsoleColoredPart : AConsoleDisplayPart
    {
        public Color pColor { get { return Color; } }
        public Color pButtonColor { get { return Color; } }
    }
}

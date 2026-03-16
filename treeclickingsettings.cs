using System.Windows.Forms;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ExileCore.Shared.Attributes;

namespace TreeClicking
{
    public class TreeClickingSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(true);

        [Menu("Toggle Loop Hotkey",
            "Press once to START the loop, press again to STOP.")]
        public HotkeyNode Hotkey { get; set; } = new HotkeyNode(Keys.Oemtilde); // ` key

        [Menu("Womb Slot (1-4)",
            "Which womb slot to Ctrl+Click. Slot 1 = first/leftmost (index 0), Slot 4 = last (index 3).")]
        public RangeNode<int> WombSlot { get; set; } = new RangeNode<int>(1, 1, 4);

        [Menu("Tree Open Wait (ms)",
            "How long to wait for the Genesis Tree window to open after clicking the ground object.")]
        public RangeNode<int> TreeOpenWaitMs { get; set; } = new RangeNode<int>(800, 100, 5000);

        [Menu("Popup Wait (ms)",
            "Delay between the womb left-click and the Ctrl+Click.")]
        public RangeNode<int> PopupWaitMs { get; set; } = new RangeNode<int>(214, 10, 5000);

        [Menu("Loop Delay (ms)",
            "Delay after each completed cycle before starting the next one.")]
        public RangeNode<int> LoopDelayMs { get; set; } = new RangeNode<int>(990, 50, 5000);

        [Menu("Click Min Duration (ms)", "Minimum mouse button hold time per click.")]
        public RangeNode<int> ClickMinDurationMs { get; set; } = new RangeNode<int>(10, 5, 200);

        [Menu("Click Max Duration (ms)", "Maximum mouse button hold time per click.")]
        public RangeNode<int> ClickMaxDurationMs { get; set; } = new RangeNode<int>(44, 10, 500);
    }
}

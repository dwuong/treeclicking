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
            "Which womb slot to click:\n" +
            "  1 = Equipment Item Womb\n" +
            "  2 = Currency Item Womb\n" +
            "  3 = Unique Item Womb\n" +
            "  4 = Mysterious Item Womb")]
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

        [Menu("Smooth Mouse Movement", "Human-like cursor movement before each click.")]
        public SmoothMoveSettings SmoothMove { get; set; } = new SmoothMoveSettings();
    }

    [Submenu(CollapsedByDefault = true)]
    public class SmoothMoveSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(true);

        [Menu("Min Move Duration (ms)", "Minimum time for cursor to travel to target.", 1)]
        public RangeNode<int> MinMoveDurationMs { get; set; } = new RangeNode<int>(250, 5, 500);

        [Menu("Max Move Duration (ms)", "Maximum time for cursor to travel to target.", 2)]
        public RangeNode<int> MaxMoveDurationMs { get; set; } = new RangeNode<int>(300, 10, 1000);

        [Menu("Max Random Offset (px)", "Max pixel wobble from the straight path.", 3)]
        public RangeNode<int> MaxRandomOffset { get; set; } = new RangeNode<int>(3, 0, 20);

        [Menu("Min Step Delay (ms)", "Minimum delay between interpolation steps.", 4)]
        public RangeNode<int> MinStepDelayMs { get; set; } = new RangeNode<int>(1, 0, 10);

        [Menu("Max Step Delay (ms)", "Maximum delay between interpolation steps.", 5)]
        public RangeNode<int> MaxStepDelayMs { get; set; } = new RangeNode<int>(3, 1, 20);
    }
}

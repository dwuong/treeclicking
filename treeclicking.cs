using System;
using System.Linq;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;

namespace TreeClicking
{
    public class TreeClicking : BaseSettingsPlugin<TreeClickingSettings>
    {
        public static TreeClicking Instance { get; private set; }

        private SyncTask<bool> _operation;
        private bool _loopRunning = false;

        private SharpDX.Vector2 WindowOffset => GameController.Window.GetWindowRectangleTimeCache.TopLeft;
        private ExileCore.PoEMemory.MemoryObjects.Camera Camera => GameController.Game.IngameState.Camera;

        public override bool Initialise()
        {
            Instance = this;
            Name     = "TreeClicking";
            return base.Initialise();
        }

        public override void Render()
        {
            if (!Settings.Enable.Value)                return;
            if (!GameController.Window.IsForeground()) return;
            if (GameController.IsLoading)              return;

            // Hotkey toggles loop on/off
            if (Settings.Hotkey.PressedOnce())
            {
                _loopRunning = !_loopRunning;
                LogMessage($"[TreeClicking] Loop {(_loopRunning ? "STARTED" : "STOPPED")}.");

                // If we just stopped, kill any running operation
                if (!_loopRunning)
                    _operation = null;
            }

            if (!_loopRunning) return;

            // Keep pumping the current operation, or start a new one if idle
            if (_operation != null)
            {
                TaskUtils.RunOrRestart(ref _operation, () => null);
            }
            else
            {
                _operation = RunAsync();
                TaskUtils.RunOrRestart(ref _operation, () => null);
            }
        }

        private async SyncTask<bool> RunAsync()
        {
            try
            {
                var cfg      = Settings;
                var ingameUi = GameController.IngameState.IngameUi;

                // ── STEP 1: Click the TreeTree ground label if tree not open ──
                var genesisTreeWindow = ingameUi.Children?.ElementAtOrDefault(31);
                bool treeAlreadyOpen  = genesisTreeWindow != null && genesisTreeWindow.IsVisible;

                if (!treeAlreadyOpen)
                {
                    var treeLabel = FindTreeTreeLabel();
                    if (treeLabel == null)
                    {
                        LogMessage("[TreeClicking] No visible TreeTree ground label — waiting.");
                        // Yield a few frames then let Render() retry via the loop
                        await TaskUtils.NextFrame();
                        await TaskUtils.NextFrame();
                        return true;
                    }

                    var labelPt = GetLabelClickPoint(treeLabel);
                    if (labelPt == System.Drawing.Point.Empty)
                    {
                        LogMessage("[TreeClicking] Could not resolve TreeTree label screen position.");
                        return true;
                    }

                    LogMessage($"[TreeClicking] Clicking TreeTree at {labelPt}.");
                    Mouse.PauseContainment();
                    SmoothClick(labelPt, cfg);
                    Mouse.ResumeContainment();

                    // Wait for Genesis Tree window to open
                    var treeOpenDeadline = DateTime.Now.AddMilliseconds(cfg.TreeOpenWaitMs.Value);
                    while (DateTime.Now < treeOpenDeadline)
                    {
                        await TaskUtils.NextFrame();
                        genesisTreeWindow = ingameUi.Children?.ElementAtOrDefault(31);
                        if (genesisTreeWindow != null && genesisTreeWindow.IsVisible) break;
                    }

                    if (genesisTreeWindow == null || !genesisTreeWindow.IsVisible)
                    {
                        LogMessage("[TreeClicking] Genesis Tree window did not open — retrying.");
                        return true;
                    }

                    LogMessage("[TreeClicking] Genesis Tree opened.");
                }

                // ── STEP 2: Womb slot container: GenesisTreeWindow[2][0] ──────
                // PathFromRoot confirmed: 31->2->0->x->0 where x = slot index
                var wombSlotContainer = genesisTreeWindow
                    .Children?.ElementAtOrDefault(2)
                    ?.Children?.ElementAtOrDefault(0);

                if (wombSlotContainer == null)
                {
                    LogError("[TreeClicking] Cannot reach womb slot container [2][0].");
                    return true;
                }

                // ── STEP 3: Pick configured slot (1-4 → index 0-3) ───────────
                // Slot 1 (index 0) = Equipment Item Womb
                // Slot 2 (index 1) = Currency Item Womb
                // Slot 3 (index 2) = Unique Item Womb
                // Slot 4 (index 3) = Mysterious Item Womb
                int slotIndex    = cfg.WombSlot.Value - 1;
                var wombSlotWrap = wombSlotContainer.Children?.ElementAtOrDefault(slotIndex);
                var wombSlot     = wombSlotWrap?.Children?.ElementAtOrDefault(0) ?? wombSlotWrap;

                if (wombSlot == null || !wombSlot.IsVisible)
                {
                    LogMessage($"[TreeClicking] Womb slot {cfg.WombSlot.Value} (index {slotIndex}) not visible.");
                    return true;
                }

                var slotCenter = GetScreenCenter(wombSlot);
                LogMessage($"[TreeClicking] Clicking womb slot {cfg.WombSlot.Value} at {slotCenter}.");

                // ── STEP 4: Left-click to open womb popup ─────────────────────
                Mouse.PauseContainment();
                SmoothClick(slotCenter, cfg);
                Mouse.ResumeContainment();

                // Wait for popup
                var popupDeadline = DateTime.Now.AddMilliseconds(cfg.PopupWaitMs.Value);
                while (DateTime.Now < popupDeadline)
                    await TaskUtils.NextFrame();

                // ── STEP 5: Ctrl+Click same spot — grow instantly ─────────────
                Mouse.PauseContainment();
                Keyboard.KeyDown(Keys.ControlKey);
                SmoothClick(slotCenter, cfg);
                Keyboard.KeyUp(Keys.ControlKey);
                Mouse.ResumeContainment();

                LogMessage("[TreeClicking] Cycle done — looping back.");

                // ── STEP 6: Loop delay before next cycle ──────────────────────
                var loopDeadline = DateTime.Now.AddMilliseconds(cfg.LoopDelayMs.Value);
                while (DateTime.Now < loopDeadline)
                    await TaskUtils.NextFrame();
            }
            catch (Exception ex)
            {
                LogError($"[TreeClicking] Error: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                Keyboard.KeyUp(Keys.ControlKey);
                Mouse.ResumeContainment();
                _operation = null; // Render() will immediately queue the next cycle
            }

            return true;
        }

        // ══════════════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════════════

        private LabelOnGround FindTreeTreeLabel()
        {
            var labels = GameController.IngameState.IngameUi.ItemsOnGroundLabelsVisible;
            if (labels == null) return null;
            foreach (var label in labels)
            {
                if (label?.ItemOnGround == null || label.Label == null || !label.IsVisible) continue;
                var meta = label.ItemOnGround.Metadata ?? string.Empty;
                if (meta.Contains("TreeTree", StringComparison.OrdinalIgnoreCase))
                    return label;
            }
            return null;
        }

        private System.Drawing.Point GetLabelClickPoint(LabelOnGround label)
        {
            try
            {
                var rect = label.Label.GetClientRectCache;
                if (rect.Width > 0 && rect.Height > 0)
                    return new System.Drawing.Point((int)rect.Center.X, (int)rect.Center.Y);
            }
            catch { }
            try
            {
                var sp = Camera.WorldToScreen(label.ItemOnGround.Pos);
                if (sp.X > 0 && sp.Y > 0)
                    return new System.Drawing.Point((int)sp.X, (int)sp.Y);
            }
            catch { }
            return System.Drawing.Point.Empty;
        }

        /// <summary>
        /// Moves the cursor smoothly to <paramref name="target"/> (if smooth move is enabled)
        /// then performs a left click with randomised hold duration.
        /// </summary>
        private void SmoothClick(System.Drawing.Point target, TreeClickingSettings cfg)
        {
            var sm = cfg.SmoothMove;
            if (sm.Enable.Value)
            {
                Mouse.SmoothMoveTo(
                    target,
                    sm.MinMoveDurationMs.Value,
                    sm.MaxMoveDurationMs.Value,
                    sm.MaxRandomOffset.Value,
                    sm.MinStepDelayMs.Value,
                    sm.MaxStepDelayMs.Value);
            }
            Mouse.LeftClick(target, cfg.ClickMinDurationMs.Value, cfg.ClickMaxDurationMs.Value);
        }

        private System.Drawing.Point GetScreenCenter(ExileCore.PoEMemory.Element element)
        {
            var rect = element.GetClientRect();
            return new System.Drawing.Point(
                (int)(rect.Center.X + WindowOffset.X),
                (int)(rect.Center.Y + WindowOffset.Y));
        }
    }
}

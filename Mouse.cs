using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System;
using System.Windows.Forms;

/// <summary>
/// Mouse input helper for the TreeClicking plugin.
/// Stripped-down version — no Autopilot/Settings dependency.
/// Smooth-move is available via SmoothMoveTo() for callers that need it;
/// the default LeftClick/MoveCursor path is a direct SetCursorPos.
/// </summary>
public static class Mouse
{
    private static Random random = new Random();

    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

    // ── Mouse containment (ClipCursor) ────────────────────────────────────────

    [DllImport("user32.dll")]
    private static extern bool ClipCursor(ref RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool ClipCursor(IntPtr lpRect); // IntPtr.Zero = release

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    private static bool _isContained = false;
    private static bool _isPaused    = false;

    /// <summary>True while containment is intentionally paused by an action.</summary>
    public static bool IsPaused => _isPaused;

    /// <summary>
    /// Clips the cursor to the given screen rectangle (game window bounds).
    /// Call every frame while the game window is in the foreground.
    /// </summary>
    public static void ContainToRect(System.Drawing.Rectangle rect)
    {
        if (_isPaused) return;
        var r = new RECT { Left = rect.Left, Top = rect.Top, Right = rect.Right, Bottom = rect.Bottom };
        ClipCursor(ref r);
        _isContained = true;
    }

    /// <summary>
    /// Releases the cursor clip unconditionally.
    /// Call when the game window loses focus or containment is disabled.
    /// </summary>
    public static void ReleaseContainment()
    {
        ClipCursor(IntPtr.Zero);
        _isContained = false;
        _isPaused    = false;
    }

    /// <summary>Temporarily suspends containment before a plugin-driven mouse action.</summary>
    public static void PauseContainment()
    {
        if (!_isContained) return;
        ClipCursor(IntPtr.Zero);
        _isPaused = true;
    }

    /// <summary>Resumes containment after a plugin action finishes.</summary>
    public static void ResumeContainment() => _isPaused = false;

    // ── Button flag constants ─────────────────────────────────────────────────

    private const uint MOUSEEVENTF_LEFTDOWN  = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP    = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP   = 0x0010;

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    // ── Position helpers ──────────────────────────────────────────────────────

    /// <summary>Returns the current cursor position in screen coordinates.</summary>
    public static System.Drawing.Point GetCursorPosition() => Cursor.Position;

    public static void SetCursorPosition(System.Drawing.Point position) => SetCursorPos(position.X, position.Y);

    // ── Left click ────────────────────────────────────────────────────────────

    /// <summary>Moves to position and performs a left click with a randomised press duration.</summary>
    public static void LeftClick(System.Drawing.Point position, int minPressDurationMs = 10, int maxPressDurationMs = 50)
    {
        MoveCursor(position);
        mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)position.X, (uint)position.Y, 0, 0);
        Thread.Sleep(random.Next(minPressDurationMs, maxPressDurationMs + 1));
        mouse_event(MOUSEEVENTF_LEFTUP, (uint)position.X, (uint)position.Y, 0, 0);
    }

    // ── Right click ───────────────────────────────────────────────────────────

    public static void RightClick(System.Drawing.Point position, int minPressDurationMs = 10, int maxPressDurationMs = 50)
    {
        MoveCursor(position);
        mouse_event(MOUSEEVENTF_RIGHTDOWN, (uint)position.X, (uint)position.Y, 0, 0);
        Thread.Sleep(random.Next(minPressDurationMs, maxPressDurationMs + 1));
        mouse_event(MOUSEEVENTF_RIGHTUP, (uint)position.X, (uint)position.Y, 0, 0);
    }

    // ── Movement helpers ──────────────────────────────────────────────────────

    /// <summary>Moves the cursor to the target position (direct, no smooth move).</summary>
    public static void Hover(System.Drawing.Point position) => MoveCursor(position);

    // ── Internal cursor movement ──────────────────────────────────────────────

    private static void MoveCursor(System.Drawing.Point target)
    {
        SetCursorPos(target.X, target.Y);
    }

    /// <summary>
    /// Smooth cursor move with explicit parameters.
    /// Duration, offset, and step delays are controlled by the caller.
    /// </summary>
    public static void SmoothMoveTo(
        System.Drawing.Point target,
        int minDurationMs, int maxDurationMs,
        int maxRandomOffset,
        int minStepDelayMs, int maxStepDelayMs)
    {
        System.Drawing.Point start = Cursor.Position;
        int dx = target.X - start.X;
        int dy = target.Y - start.Y;
        if (dx == 0 && dy == 0) { SetCursorPos(target.X, target.Y); return; }

        int durationMs = random.Next(minDurationMs, maxDurationMs + 1);
        int stepDelay  = random.Next(minStepDelayMs, maxStepDelayMs + 1);
        int steps      = stepDelay > 0 ? Math.Max(1, durationMs / stepDelay) : 1;

        for (int i = 1; i <= steps; i++)
        {
            float t    = (float)i / steps;
            int   offX = maxRandomOffset > 0 ? random.Next(-maxRandomOffset, maxRandomOffset + 1) : 0;
            int   offY = maxRandomOffset > 0 ? random.Next(-maxRandomOffset, maxRandomOffset + 1) : 0;
            int   curX = (int)(start.X + dx * t) + (i < steps ? offX : 0);
            int   curY = (int)(start.Y + dy * t) + (i < steps ? offY : 0);
            SetCursorPos(curX, curY);
            if (stepDelay > 0) Thread.Sleep(stepDelay);
        }

        SetCursorPos(target.X, target.Y);
    }
}
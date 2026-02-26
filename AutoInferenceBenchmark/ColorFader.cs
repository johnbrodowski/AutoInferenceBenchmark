namespace AutoInferenceBenchmark;

/// <summary>
/// Non-blocking, cancellable color transitions and continuous pulse animations for
/// any WinForms control (or any arbitrary property via an <see cref="Action{Color}"/> setter).
///
/// All methods are fire-and-forget safe — <see cref="OperationCanceledException"/> is
/// swallowed internally. Control lifetime is guarded with <c>IsHandleCreated</c> /
/// <c>IsDisposed</c> checks before every <see cref="Control.BeginInvoke"/> call.
///
/// <example><code>
/// // One-shot fade (ForeColor):
/// _ = ColorFader.FadeForeAsync(label, Color.Red, Color.LimeGreen, 400);
///
/// // Continuous pulse — cancel to stop:
/// CancellationTokenSource cts = ColorFader.PulseFore(label, Color.Green, Color.LimeGreen);
/// cts.Cancel();
///
/// // Generic setter — drives any Color property (chart series, custom draw, etc.):
/// _ = ColorFader.FadeAsync(c => series.Color = c, Color.Blue, Color.Red, 300);
/// </code></example>
/// </summary>
public static class ColorFader
{
    // ── Core fade ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fades from <paramref name="from"/> to <paramref name="to"/> over
    /// <paramref name="durationMs"/> ms in <paramref name="steps"/> discrete steps,
    /// applying each blended color via <paramref name="apply"/>.
    /// </summary>
    public static async Task FadeAsync(
        Action<Color> apply,
        Color from, Color to,
        int durationMs = 300,
        int steps = 20,
        CancellationToken ct = default)
    {
        if (steps < 1) steps = 1;
        int delayMs = Math.Max(1, durationMs / steps);
        try
        {
            for (int i = 0; i <= steps; i++)
            {
                ct.ThrowIfCancellationRequested();
                apply(Blend(from, to, (float)i / steps));
                if (i < steps)
                    await Task.Delay(delayMs, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
    }

    // ── Control convenience wrappers ─────────────────────────────────────────────

    /// <summary>Fades a control's <see cref="Control.ForeColor"/>.</summary>
    public static Task FadeForeAsync(Control ctrl, Color from, Color to,
        int durationMs = 300, int steps = 20, CancellationToken ct = default)
        => FadeAsync(
            c => SafeInvoke(ctrl, () => ctrl.ForeColor = c),
            from, to, durationMs, steps, ct);

    /// <summary>Fades a control's <see cref="Control.BackColor"/>.</summary>
    public static Task FadeBackAsync(Control ctrl, Color from, Color to,
        int durationMs = 300, int steps = 20, CancellationToken ct = default)
        => FadeAsync(
            c => SafeInvoke(ctrl, () => ctrl.BackColor = c),
            from, to, durationMs, steps, ct);

    // ── Pulse (continuous ping-pong) ─────────────────────────────────────────────

    /// <summary>
    /// Starts a continuous ping-pong animation between <paramref name="a"/> and
    /// <paramref name="b"/>. Returns a <see cref="CancellationTokenSource"/> —
    /// call <c>.Cancel()</c> to stop the animation.
    /// </summary>
    public static CancellationTokenSource Pulse(
        Action<Color> apply,
        Color a, Color b,
        int halfPeriodMs = 700,
        int steps = 20)
    {
        var cts = new CancellationTokenSource();
        _ = RunPulseAsync(apply, a, b, halfPeriodMs, steps, cts.Token);
        return cts;
    }

    /// <summary>Pulse convenience wrapper — animates <see cref="Control.ForeColor"/>.</summary>
    public static CancellationTokenSource PulseFore(Control ctrl, Color a, Color b,
        int halfPeriodMs = 700, int steps = 20)
        => Pulse(c => SafeInvoke(ctrl, () => ctrl.ForeColor = c), a, b, halfPeriodMs, steps);

    /// <summary>Pulse convenience wrapper — animates <see cref="Control.BackColor"/>.</summary>
    public static CancellationTokenSource PulseBack(Control ctrl, Color a, Color b,
        int halfPeriodMs = 700, int steps = 20)
        => Pulse(c => SafeInvoke(ctrl, () => ctrl.BackColor = c), a, b, halfPeriodMs, steps);

    // ── Helpers ───────────────────────────────────────────────────────────────────

    /// <summary>Linear ARGB interpolation between two colors.</summary>
    public static Color Blend(Color from, Color to, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return Color.FromArgb(
            (int)(from.A + (to.A - from.A) * t),
            (int)(from.R + (to.R - from.R) * t),
            (int)(from.G + (to.G - from.G) * t),
            (int)(from.B + (to.B - from.B) * t));
    }

    private static void SafeInvoke(Control ctrl, Action action)
    {
        if (ctrl.IsHandleCreated && !ctrl.IsDisposed)
            ctrl.BeginInvoke(action);
    }

    private static async Task RunPulseAsync(Action<Color> apply, Color a, Color b,
        int halfPeriodMs, int steps, CancellationToken ct)
    {
        try
        {
            bool forward = true;
            while (!ct.IsCancellationRequested)
            {
                await FadeAsync(apply, forward ? a : b, forward ? b : a, halfPeriodMs, steps, ct);
                forward = !forward;
            }
        }
        catch (OperationCanceledException) { }
    }
}

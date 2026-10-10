namespace AutoClicker;

public class MainForm : Form
{
    private const int HotkeyStartStop = 1, HotkeyPickClick = 2, HotkeyPickText = 3;

    private readonly Label _clickPosLabel = new() { Text = "Not set", AutoSize = true, Margin = new Padding(3, 6, 3, 3) };
    private readonly Label _textPosLabel = new() { Text = "Not set", AutoSize = true, Margin = new Padding(3, 6, 3, 3) };
    private readonly CheckBox _doClick = new() { Text = "Click at position", Checked = true, AutoSize = true };
    private readonly CheckBox _doType = new() { Text = "Type random paragraph at position", Checked = true, AutoSize = true };
    private readonly CheckBox _rightClick = new() { Text = "Right button", AutoSize = true };
    private readonly NumericUpDown _clickInterval = Num(50, 3_600_000, 1000);
    private readonly NumericUpDown _typeInterval = Num(100, 3_600_000, 5000);
    private readonly NumericUpDown _sentences = Num(1, 50, 3);
    private readonly NumericUpDown _charDelay = Num(0, 1000, 20);
    private readonly NumericUpDown _repeat = Num(0, 1_000_000, 0);
    private readonly CheckBox _pressEnter = new() { Text = "Press Enter after typing", AutoSize = true };
    private readonly Button _startStop = new() { Text = "Start (F8)", Height = 36, Dock = DockStyle.Fill };
    private readonly Label _status = new() { Text = "Idle", AutoSize = true, Margin = new Padding(3, 6, 3, 3) };
    private readonly TextBox _preview = new() { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, Height = 110 };

    private Point? _clickPos, _textPos;
    private CancellationTokenSource? _cts;

    public MainForm()
    {
        Text = "Auto Clicker - Mouse & Keyboard Automation";
        ClientSize = new Size(520, 580);
        MinimumSize = Size;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;

        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(10), AutoScroll = true };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var pickClick = new Button { Text = "Set in 3s (F6)", AutoSize = true };
        pickClick.Click += async (_, _) => _clickPos = await PickAfterDelay(_clickPosLabel);
        var pickText = new Button { Text = "Set in 3s (F7)", AutoSize = true };
        pickText.Click += async (_, _) => _textPos = await PickAfterDelay(_textPosLabel);

        Row(grid, _doClick, _rightClick, null);
        Row(grid, "Click position:", _clickPosLabel, pickClick);
        Row(grid, "Click every (ms):", _clickInterval, null);
        Row(grid, _doType, null, null);
        Row(grid, "Text position:", _textPosLabel, pickText);
        Row(grid, "Type every (ms):", _typeInterval, null);
        Row(grid, "Sentences per paragraph:", _sentences, null);
        Row(grid, "Delay per character (ms):", _charDelay, null);
        Row(grid, _pressEnter, null, null);
        Row(grid, "Repeat count (0 = forever):", _repeat, null);
        Row(grid, "Status:", _status, null);

        var hint = new Label
        {
            Text = "Move the mouse over the target and press F6 / Ctrl+Alt+1 (click spot) or F7 / Ctrl+Alt+2 (text field) to capture it, " +
                   "or use the buttons. F8 / Ctrl+Alt+3 starts/stops from anywhere.",
            AutoSize = true,
            MaximumSize = new Size(480, 0),
        };
        Row(grid, hint, null, null);
        Row(grid, _startStop, null, null);
        Row(grid, new Label { Text = "Last typed paragraph:", AutoSize = true }, null, null);
        Row(grid, _preview, null, null);

        Controls.Add(grid);
    }

    private static NumericUpDown Num(int min, int max, int value) =>
        new() { Minimum = min, Maximum = max, Value = value, Width = 120 };

    private static void Row(TableLayoutPanel g, object a, Control? b, Control? c)
    {
        int r = g.RowCount++;
        if (a is Control ca)
        {
            g.Controls.Add(ca, 0, r);
            if (b == null) g.SetColumnSpan(ca, 3);
        }
        else
        {
            g.Controls.Add(new Label { Text = (string)a, AutoSize = true, Margin = new Padding(3, 6, 3, 3) }, 0, r);
        }
        if (b != null) g.Controls.Add(b, 1, r);
        if (c != null) g.Controls.Add(c, 2, r);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        const uint ctrlAlt = 0x0001 | 0x0002; // MOD_ALT | MOD_CONTROL
        // F-keys may need Fn on laptops, so Ctrl+Alt+1/2/3 work as well.
        bool ok = NativeMethods.RegisterHotKey(Handle, HotkeyPickClick, NativeMethods.MOD_NONE, (uint)Keys.F6)
                & NativeMethods.RegisterHotKey(Handle, HotkeyPickText, NativeMethods.MOD_NONE, (uint)Keys.F7)
                & NativeMethods.RegisterHotKey(Handle, HotkeyStartStop, NativeMethods.MOD_NONE, (uint)Keys.F8);
        ok &= NativeMethods.RegisterHotKey(Handle, HotkeyPickClick + 10, ctrlAlt, (uint)Keys.D1)
            & NativeMethods.RegisterHotKey(Handle, HotkeyPickText + 10, ctrlAlt, (uint)Keys.D2)
            & NativeMethods.RegisterHotKey(Handle, HotkeyStartStop + 10, ctrlAlt, (uint)Keys.D3);
        if (!ok) _status.Text = "Some hotkeys could not be registered - use the buttons";
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cts?.Cancel();
        foreach (int i in new[] { 1, 2, 3, 11, 12, 13 }) NativeMethods.UnregisterHotKey(Handle, i);
        base.OnFormClosing(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY)
        {
            switch (m.WParam.ToInt32() % 10)
            {
                case HotkeyPickClick: _clickPos = CaptureCursor(_clickPosLabel); break;
                case HotkeyPickText: _textPos = CaptureCursor(_textPosLabel); break;
                case HotkeyStartStop: Toggle(); break;
            }
        }
        base.WndProc(ref m);
    }

    private static Point CaptureCursor(Label label)
    {
        var p = Cursor.Position;
        label.Text = $"X={p.X}, Y={p.Y}";
        return p;
    }

    private static async Task<Point> PickAfterDelay(Label label)
    {
        for (int i = 3; i > 0; i--)
        {
            label.Text = $"Move mouse... {i}";
            await Task.Delay(1000);
        }
        return CaptureCursor(label);
    }

    private void Toggle()
    {
        if (_cts != null) { _cts.Cancel(); return; }
        if (!_doClick.Checked && !_doType.Checked) { MessageBox.Show("Enable at least one action."); return; }
        if (_doClick.Checked && _clickPos is null) { MessageBox.Show("Set the click position first: hover over the target and press F6 (or Ctrl+Alt+1), or press the Set button and wait for the countdown."); return; }
        if (_doType.Checked && _textPos is null) { MessageBox.Show("Set the text position first: hover over the text field and press F7 (or Ctrl+Alt+2), or press the Set button and wait for the countdown."); return; }
        _ = RunAsync();
    }

    private async Task RunAsync()
    {
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        _startStop.Text = "Stop (F8)";
        _status.Text = "Starting...";

        bool click = _doClick.Checked, type = _doType.Checked, right = _rightClick.Checked, enter = _pressEnter.Checked;
        int clickMs = (int)_clickInterval.Value, typeMs = (int)_typeInterval.Value;
        int sentences = (int)_sentences.Value, charDelay = (int)_charDelay.Value, repeat = (int)_repeat.Value;
        var cp = _clickPos;
        var tp = _textPos;

        try
        {
            await Task.Delay(1500, ct); // time to move away from the window
            long start = Environment.TickCount64, nextClick = 0, nextType = 0;
            int done = 0;
            while (repeat == 0 || done < repeat)
            {
                ct.ThrowIfCancellationRequested();
                long now = Environment.TickCount64 - start;
                if (click && now >= nextClick)
                {
                    NativeMethods.Click(cp!.Value, right);
                    nextClick = now + clickMs;
                    _status.Text = $"Running - actions: {++done}";
                }
                if (type && now >= nextType && (repeat == 0 || done < repeat))
                {
                    NativeMethods.Click(tp!.Value); // focus the target field
                    await Task.Delay(150, ct);
                    string text = ParagraphGenerator.Create(sentences);
                    _preview.Text = text;
                    foreach (char c in text)
                    {
                        ct.ThrowIfCancellationRequested();
                        NativeMethods.TypeChar(c);
                        if (charDelay > 0) await Task.Delay(charDelay, ct);
                    }
                    if (enter) NativeMethods.TypeChar('\n');
                    nextType = Environment.TickCount64 - start + typeMs;
                    _status.Text = $"Running - actions: {++done}";
                }
                await Task.Delay(10, ct);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _startStop.Text = "Start (F8)";
            _status.Text = "Stopped";
        }
    }
}


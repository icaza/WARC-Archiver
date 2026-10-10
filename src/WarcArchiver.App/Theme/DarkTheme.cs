using System.Reflection;
using System.Runtime.InteropServices;

namespace WarcArchiver.App.Theme;

/// <summary>
/// Colour palette plus the few things a Designer file cannot express: the dark ToolStrip renderer, the dimmed look of
/// disabled buttons, double-buffered grids, dark scroll bars and a dark title bar (Windows 10 1809+ / Windows 11).
/// Colours, fonts, borders and layout are NOT set here: they are plain properties in MainForm.Designer.cs, so the Visual
/// Studio designer shows and keeps exactly what the application looks like. The palette below mirrors the values used there.
/// </summary>
internal static class DarkTheme
{
    public static readonly Color Background = Color.FromArgb(18, 20, 24);
    public static readonly Color Surface = Color.FromArgb(26, 29, 35);
    public static readonly Color SurfaceRaised = Color.FromArgb(34, 38, 46);
    public static readonly Color SurfaceHover = Color.FromArgb(44, 49, 59);
    public static readonly Color Border = Color.FromArgb(52, 58, 68);
    public static readonly Color Text = Color.FromArgb(230, 232, 235);
    public static readonly Color TextMuted = Color.FromArgb(154, 163, 175);
    public static readonly Color Accent = Color.FromArgb(76, 154, 255);
    public static readonly Color AccentHover = Color.FromArgb(105, 172, 255);
    public static readonly Color AccentPressed = Color.FromArgb(52, 126, 224);
    public static readonly Color AccentText = Color.FromArgb(10, 16, 28);
    public static readonly Color Success = Color.FromArgb(63, 185, 80);
    public static readonly Color Warning = Color.FromArgb(210, 153, 34);
    public static readonly Color Danger = Color.FromArgb(248, 81, 73);
    public static readonly Color Selection = Color.FromArgb(38, 79, 140);
    public static readonly Color RowAlternate = Color.FromArgb(30, 33, 40);

    /// <summary>Button.Tag value that marks the main call-to-action button of a page.</summary>
    public const string PrimaryTag = "primary";

    /// <summary>Button.Tag value for the borderless sidebar navigation buttons.</summary>
    public const string NavTag = "nav";

    /// <summary>
    /// Adds the run-time-only behaviour to a form whose look is already defined in its Designer file. It never assigns
    /// colours, fonts or borders, so anything changed in the designer takes effect as drawn.
    /// </summary>
    public static void Apply(Control root)
    {
        foreach (Control control in Walk(root))
        {
            switch (control)
            {
                case Button button:
                    ArmButton(button);
                    break;

                case TextBox { Multiline: true } textBox:
                    UseDarkScrollBars(textBox);
                    break;

                case DataGridView grid:
                    PrepareGrid(grid);
                    break;

                case ToolStrip strip:
                    // The renderer cannot be stored in a Designer file (it is not serialisable), so it is set here.
                    strip.Renderer = new DarkToolStripRenderer();
                    break;
            }
        }
    }

    /// <summary>Asks Windows to draw this window's title bar in dark mode.</summary>
    public static void UseDarkTitleBar(Form form)
    {
        void Apply()
        {
            try
            {
                int enabled = 1;
                // Attribute 20 on Windows 10 20H1+/11, attribute 19 on early Windows 10 builds.
                if (NativeMethods.DwmSetWindowAttribute(form.Handle, 20, ref enabled, sizeof(int)) != 0)
                {
                    NativeMethods.DwmSetWindowAttribute(form.Handle, 19, ref enabled, sizeof(int));
                }
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                // Optional polish only.
            }
        }

        if (form.IsHandleCreated)
        {
            Apply();
        }
        else
        {
            form.HandleCreated += (_, _) => Apply();
        }
    }

    public static void UseDarkScrollBars(Control control)
    {
        void Apply()
        {
            try
            {
                NativeMethods.SetWindowTheme(control.Handle, "DarkMode_Explorer", null);
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                // Optional polish only.
            }
        }

        if (control.IsHandleCreated)
        {
            Apply();
        }
        else
        {
            control.HandleCreated += (_, _) => Apply();
        }
    }

    /// <summary>
    /// Disabled flat buttons keep their face colour, so the text is dimmed (and a primary button turns grey) to make the
    /// state obvious on a dark surface. The "enabled" colours are whatever the Designer file gives the button.
    /// Navigation buttons are skipped: <c>MainForm.ShowPage</c> owns their colours.
    /// </summary>
    static void ArmButton(Button button)
    {
        string? tag = button.Tag as string;
        if (string.Equals(tag, NavTag, StringComparison.Ordinal))
        {
            return;
        }

        bool primary = string.Equals(tag, PrimaryTag, StringComparison.Ordinal);
        Color enabledBack = button.BackColor;
        Color enabledFore = button.ForeColor;

        void ApplyEnabledLook()
        {
            button.ForeColor = button.Enabled ? enabledFore : TextMuted;
            button.Cursor = button.Enabled ? Cursors.Hand : Cursors.Default;
            if (primary)
            {
                button.BackColor = button.Enabled ? enabledBack : Border;
            }
        }

        button.EnabledChanged += (_, _) => ApplyEnabledLook();
        ApplyEnabledLook();
    }

    /// <summary>DataGridView double buffering is not exposed publicly; without it, adding rows live flickers.</summary>
    static void PrepareGrid(DataGridView grid)
    {
        typeof(DataGridView)
            .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(grid, true);

        UseDarkScrollBars(grid);
    }

    static IEnumerable<Control> Walk(Control root)
    {
        yield return root;
        foreach (Control child in root.Controls)
        {
            foreach (Control descendant in Walk(child))
            {
                yield return descendant;
            }
        }
    }

    static class NativeMethods
    {
        [DllImport("dwmapi.dll")]
        internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        internal static extern int SetWindowTheme(IntPtr hwnd, string? subAppName, string? subIdList);
    }
}

using System.Globalization;

namespace WarcArchiver.App;

internal static class Program
{
    const string SingleInstanceName = @"Local\WarcArchiver.SingleInstance.v1";

    [STAThread]
    static void Main()
    {
        // One instance at a time: two processes appending to the same audit log would defeat its hash chain.
        using var mutex = new Mutex(initiallyOwned: true, SingleInstanceName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show(
                "WARC Archiver is already running.",
                "WARC Archiver",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        // The interface is en-US only; invariant formatting matches it and avoids loading ICU.
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ShowUnexpectedError(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                ShowUnexpectedError(ex);
            }
        };

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        GC.KeepAlive(mutex);
    }

    static void ShowUnexpectedError(Exception ex)
    {
        MessageBox.Show(
            "An unexpected error occurred:\n\n" + ex.Message + "\n\nNo archive was modified. If this keeps happening, please report it.",
            "WARC Archiver",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}

// SPDX-License-Identifier: GPL-2.0-or-later
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BlackModKit
{
    internal static class Program
    {
        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int processId);

        [STAThread]
        private static int Main(string[] args)
        {
            if (args != null && args.Length > 0 && string.Equals(args[0], "--selftest", StringComparison.OrdinalIgnoreCase))
            {
                AttachConsole(-1);
                return SelfTest.Run();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => ReportError(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => ReportError(e.ExceptionObject as Exception);
            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                ReportError(ex);
                return 1;
            }
            return 0;
        }

        private static void ReportError(Exception ex)
        {
            try
            {
                string log = Path.Combine(Path.GetTempPath(), "blackmodkit-error.log");
                File.AppendAllText(log, DateTime.Now.ToString("s") + Environment.NewLine + (ex == null ? "erro desconhecido" : ex.ToString()) + Environment.NewLine + Environment.NewLine);
            }
            catch { }
            MessageBox.Show("O ModKit encontrou um erro:\n\n" + (ex == null ? "erro desconhecido" : ex.Message) +
                            "\n\nDetalhes em %TEMP%\\blackmodkit-error.log.", "BLACK PC ModKit",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

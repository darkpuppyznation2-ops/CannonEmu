using System;
using System.Windows.Forms;

namespace CannonEmuFrontend
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainWindow());
        }
    }
}

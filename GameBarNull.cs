using System;
using System.Windows.Forms;

namespace GameBarNull
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Silently consume any ms-gamebar:// URI and exit.
            // No window, no tray icon, no output — just acknowledge and discard.
        }
    }
}

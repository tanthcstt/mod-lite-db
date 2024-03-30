using LiteDB;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string pythonDll = @"C:\Users\ASUS\AppData\Local\Programs\Python\Python37\python37.dll";
            Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", pythonDll);
            PythonEngine.Initialize();

            using (Py.GIL())
            {
                dynamic sys = Py.Import("sys");
                dynamic sysPath = sys.path;
                sysPath.append(@"D:\"); // Adjust the path if needed

                // Now import the module
                dynamic visionapi = Py.Import("visionapi");

                // Use the module as needed
            }

            // Shutdown Python runtime
            PythonEngine.Shutdown();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}

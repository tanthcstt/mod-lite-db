using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using ICSharpCode.TextEditor.Util;

using LiteDB;
namespace LiteDB.Studio
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
           // Task.Run(() => StartPythonProcess());
            Application.ApplicationExit += OnExit;
           
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(args.Length == 0 ? null : args[0]));
           
        }

        private static void OnExit(object sender, EventArgs eventArgs)
        {
            Application.ApplicationExit -= OnExit;
            AppSettingsManager.PersistData();
        }

      

    }
}

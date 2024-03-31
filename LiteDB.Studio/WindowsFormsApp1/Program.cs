using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.ML;
using TensorFlow;
using Microsoft.ML.Data;

namespace WindowsFormsApp1
{
    internal static class Program
    {
       

        [STAThread]
        static void Main()
        {


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        static void StartPythonProcess()
        {
           
        }

      

       
    }
}

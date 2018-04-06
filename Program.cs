using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Jint.Native;
using Jint.Runtime;




namespace FormWithConsole
{
    internal static class NativeMethods
    {
        // http://msdn.microsoft.com/en-us/library/ms681944(VS.85).aspx
        /// <summary>
        ///     Allocates a new console for the calling process.
        /// </summary>
        /// <returns>nonzero if the function succeeds; otherwise, zero.</returns>
        /// <remarks>
        ///     A process can be associated with only one console,
        ///     so the function fails if the calling process already has a console.
        /// </remarks>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int AllocConsole();

        // http://msdn.microsoft.com/en-us/library/ms683150(VS.85).aspx
        /// <summary>
        ///     Detaches the calling process from its console.
        /// </summary>
        /// <returns>nonzero if the function succeeds; otherwise, zero.</returns>
        /// <remarks>
        ///     If the calling process is not already attached to a console,
        ///     the error code returned is ERROR_INVALID_PARAMETER (87).
        /// </remarks>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int FreeConsole();
    }
}

namespace FormWithConsole
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void TrueMain(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            NativeMethods.AllocConsole();
            Console.WriteLine("Debug Console");
            Jint.Repl.Program.Main2(args);
            Application.Run(new Form());
            NativeMethods.FreeConsole();
        }
    }
}


namespace Jint.Repl
{
    internal class Program
    {
        internal static void Main2(string[] args)
        {
            var engine = new Engine(cfg => cfg.AllowClr()).SetValue("print", new Action<object>(Console.WriteLine));

            var filename = args.Length > 0 ? args[0] : "";
            if (!string.IsNullOrEmpty(filename))
            {
                if (!File.Exists(filename)) Console.WriteLine("Could not find file: {0}", filename);

                var script = File.ReadAllText(filename);
                var result = engine.GetValue(engine.Execute(script).GetCompletionValue());
                return;
            }

            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            var version = fvi.FileVersion;

            Console.WriteLine("Welcome to Jint ({0})", version);
            Console.WriteLine("Type 'exit' to leave, 'print()' to write on the console.");
            Console.WriteLine();

            var defaultColor = Console.ForegroundColor;
            while (true)
            {
                Console.ForegroundColor = defaultColor;
                Console.Write("jint> ");
                var input = Console.ReadLine();
                if (input == "exit") return;

                try
                {
                    var result = engine.GetValue(engine.Execute(input).GetCompletionValue());
                    if (result.Type != Types.None && result.Type != Types.Null && result.Type != Types.Undefined)
                    {
                        var str = TypeConverter.ToString(engine.Json.Stringify(engine.Json,
                            Arguments.From(result, Undefined.Instance, "  ")));
                        Console.WriteLine("=> {0}", str);
                    }
                }
                catch (JavaScriptException je)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(je.ToString());
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e.Message);
                }
            }
        }
    }
}

namespace haxyhax
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            FormWithConsole.Program.TrueMain(args);
        }
    }
}
using Schemy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rll
{
    class Program
    {

        static List<string> OutputLines = new List<string>();
        static List<string> ErrorLines = new List<string>();
        static List<string[]> Highlighters = new List<string[]>();

        public static void InteractProcess(Process p)
        {
           

            p.OutputDataReceived += (o, d) =>
            {
                if (d.Data != null)
                {
                    OutputLines.Add(d.Data);
                    Console.WriteLine($"Out: {d.Data}");
                }


            };
            p.ErrorDataReceived += (o, d) =>
            {
                if (d.Data != null)
                {
                    ErrorLines.Add(d.Data);
                    Console.WriteLine($"Err: {d.Data}");
                }
            };
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
        }
        public static Interpreter CreateItpl()
        {
            Interpreter.CreateSymbolTableDelegate appExt = _ => new Dictionary<Symbol, object>()
            {
                { Symbol.FromString("highlight-add"), NativeProcedure.Create<string,List<object>, object>((group, l) =>
                {

                    Highlighters.Add( new[] { group }.Concat(l.Cast<string>()).ToArray());
                    return None.Instance;
                }) },
                { Symbol.FromString("highlight-show"), NativeProcedure.Create(() =>
                {
                    foreach (var h in Highlighters)
                    {
                        var toSearch = h.Skip(1);
                        var found = OutputLines.Where(line => toSearch.Any(subs => line.Contains(subs))).ToArray();
                        if (found.Length >0)
                        {
                            Console.WriteLine(h[0]);
                            foreach (var l in found) {
                                Console.WriteLine(l);
                            }

                        }
                    };
                    return None.Instance;
                }) },


            };


            Interpreter.CreateSymbolTableDelegate processExt = _ => new Dictionary<Symbol, object>()
            {
                { Symbol.FromString("psi-exe"), NativeProcedure.Create<ProcessStartInfo, string, object>((psi, s) => psi.FileName = s) },
                { Symbol.FromString("psi-arg"), NativeProcedure.Create<ProcessStartInfo, string, object>((psi, s) => psi.Arguments = s) },
                { Symbol.FromString("psi-dir"), NativeProcedure.Create<ProcessStartInfo, string, object>((psi, s) => psi.WorkingDirectory = s) },
                { Symbol.FromString("psi-shell"), NativeProcedure.Create<ProcessStartInfo, bool, object>((psi, s) => psi.UseShellExecute = s) },
                { Symbol.FromString("psi-createnowindow"), NativeProcedure.Create<ProcessStartInfo, bool, object>((psi, s) => psi.CreateNoWindow = s) },
                { Symbol.FromString("psi-redirect-stdout"), NativeProcedure.Create<ProcessStartInfo, bool, object>((psi, s) => psi.RedirectStandardOutput = s) },
                { Symbol.FromString("psi-redirect-stderr"), NativeProcedure.Create<ProcessStartInfo, bool, object>((psi, s) => psi.RedirectStandardError = s) },

                { Symbol.FromString("ps-new"), NativeProcedure.Create(() => {
                    var p = new Process();
                    return p;
                }) },
                { Symbol.FromString("ps-psi"), NativeProcedure.Create<Process, ProcessStartInfo>(p => p.StartInfo) },
                { Symbol.FromString("ps-interact"), NativeProcedure.Create<Process,object>((p) => {
                    InteractProcess(p);
                    return None.Instance;
                })},
                { Symbol.FromString("ps-wait"), NativeProcedure.Create<Process, int>(p => {
                    p.WaitForExit();
                    return p.ExitCode;
                }) },

                { Symbol.FromString("print"), NativeProcedure.Create<object, object>(o => {
                    Console.WriteLine($"{o}");
                    return None.Instance;
                }) },

                { Symbol.FromString("os-exit"), NativeProcedure.Create<int, object>(exitCode => {
                    System.Environment.Exit(exitCode);
                    return None.Instance;
                }) },
            };
            var itpl = new Interpreter(new[] { processExt, appExt });
            return itpl;

        }
        static void Main(string[] args)
        {
            var script = args[0];
            var itp = CreateItpl();
            var r = itp.Evaluate(File.OpenText(script));
            Console.WriteLine(r.Error);
        }
    }
}

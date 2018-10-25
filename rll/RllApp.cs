using Schemy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rll
{
    static class RllConfig
    {
        public static int TruncateCol = 0;
        public static string[] ShowOnly = null;
    }
    public static class RllApp
    {

        static List<string> OutputLines = new List<string>();
        static List<string> ErrorLines = new List<string>();
        static List<string[]> Highlighters = new List<string[]>();

        static void PushOutput(string o)
        {
            var col = RllConfig.TruncateCol;
            var toAdd = col == 0 || o.Length < col ? o : o.Substring(0, col) + "...";
            // let's add original to summary
            OutputLines.Add(o);
            if (RllConfig.ShowOnly != null && !RllConfig.ShowOnly.Any(p => o.Contains(p))) {
                return;
            } 
            Console.WriteLine(toAdd);
        }
        public static void InteractProcess(Process p)
        {
            p.OutputDataReceived += (o, d) =>
            {
                if (d.Data != null)
                {
                    PushOutput(d.Data);
                }
            };
            p.ErrorDataReceived += (o, d) =>
            {
                if (d.Data != null)
                {
                    PushOutput(d.Data);
                    ErrorLines.Add(d.Data);
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
                { Symbol.FromString("guess-file"), NativeProcedure.Create<string, List<object>, string>((defaultName, l) =>
                {
                    var found = l.Cast<string>().FirstOrDefault(e => File.Exists(e));
                    return found == null ? defaultName : found;
                }) },


                { Symbol.FromString("cf-truncate"), NativeProcedure.Create<int, object>(num => RllConfig.TruncateCol = num ) },
                { Symbol.FromString("cf-show-only"), NativeProcedure.Create<List<object>, object>(patterns => RllConfig.ShowOnly = patterns.Cast<string>().ToArray()) },

            };
            var itpl = new Interpreter(new[] { processExt, appExt });
            return itpl;

        }

        public static void RunRepl()
        {
            var itp = CreateItpl();
            itp.REPL(Console.In, Console.Out, "Schemy> ", new[] { "Entering repl" }); 
        }

        public static void RllMain()
        {
            var args = System.Environment.GetCommandLineArgs();
            
            var myname = args[0];
            var mybin = Path.GetFileNameWithoutExtension(myname);
            var mypath = Path.GetDirectoryName(myname);
            string script = null;
            if (mybin == "rll")
            {
                if (args.Length == 1)
                {
                    RunRepl();

                }
                script = args[1];

            } else
            {
                var tries =
                    new[] { $"rll_{myname}.ss", $"rll/{myname}.ss" }
                    .Select(t => Path.Combine(mypath, t));


                script = tries.FirstOrDefault(File.Exists);
                if (script == null)
                {
                    Console.WriteLine("Rll: did not find any of: " + tries.ToString());
                    System.Environment.Exit(1);
                        
                }
            }

            var itp = CreateItpl();
            var r = itp.Evaluate(File.OpenText(script));
            Console.WriteLine(r.Error);
        }
    }
}

using Schemy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
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
        private static Interpreter AppInterpreter;

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
            p.StartInfo.UseShellExecute = false;
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

        // rll-run
        public static Process ConvenienceRun(string cmd, string arg, string cwd, bool interact)
        {
            var p = new Process();
            var psi = p.StartInfo;
            psi.FileName = cmd;
            psi.Arguments = arg;
            psi.UseShellExecute = false;
            if (cwd != null)
            {
                psi.WorkingDirectory = cwd;
            }
            if (interact)
            {
                InteractProcess(p);
            } else
            {
                p.Start();
            }
            return p;
        }

        private static Symbol Sym(string s) => Symbol.FromString(s);

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
                { Symbol.FromString("cf-truncate"), NativeProcedure.Create<int, object>(num => RllConfig.TruncateCol = num ) },
                { Symbol.FromString("repl"), NativeProcedure.Create(() => { RunRepl(); return None.Instance; }) },

                { Symbol.FromString("cf-show-only"), NativeProcedure.Create<List<object>, object>(patterns => RllConfig.ShowOnly = patterns.Cast<string>().ToArray()) },
                { Symbol.FromString("rll-run-capture"), NativeProcedure.Create<string, string, Process>((cmd, arg) =>
                    ConvenienceRun(cmd, arg, null, true)) },
                { Symbol.FromString("os-system"), NativeProcedure.Create<string, string, Process >((cmd, arg) =>
                    ConvenienceRun(cmd, arg, null, false)) },
                { Symbol.FromString("unzip"), NativeProcedure.Create<string, string, None >((zipfile, targetdir) =>
                {
                    ZipFile.ExtractToDirectory(zipfile, targetdir);
                    return None.Instance;

                }) },
                { Symbol.FromString("wget"), NativeProcedure.Create<string, string, None >((url, fname) =>
                {
                    new WebClient().DownloadFile(url, fname);                    
                    return None.Instance;
                }) },
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
                { Symbol.FromString("ps-start"), NativeProcedure.Create<string, string, Process>((cmd, arg) => {
                    var p = Process.Start(cmd,arg);
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
                    Console.WriteLine(Schemy.Utils.PrintExpr(o));
                    return None.Instance;
                }) },

                { Symbol.FromString("os-exit"), NativeProcedure.Create<int, object>(exitCode => {
                    System.Environment.Exit(exitCode);
                    return None.Instance;
                }) },
                { Symbol.FromString("cd"), NativeProcedure.Create<string, None>(path => {
                    System.Environment.CurrentDirectory = path;
                    return None.Instance;
                }) },

                { Symbol.FromString("pwd"), NativeProcedure.Create(() => System.Environment.CurrentDirectory) },

                { Symbol.FromString("path-join"), NativeProcedure.Create<List<object>, string> (parts => Path.Combine(parts.Cast<string>().ToArray())) },
                { Sym("path-tempfile"), NativeProcedure.Create(() => Path.GetTempFileName()) },
                { Sym("path-temppath"), NativeProcedure.Create(() => Path.GetTempPath()) },
                { Sym("path-remove"), NativeProcedure.Create<string, None>(pth => {
                    File.Delete(pth);
                    return None.Instance;
                }) },
                { Sym("help"), NativeProcedure.Create(() => AppInterpreter.Environment.store.Keys.Select(k => k.AsString).Cast<object>().ToList() ) }, 
                { Symbol.FromString("s-join"), NativeProcedure.Create<string, List<object>, string> ((sep, strings) => String.Join(sep, strings.Cast<string>().ToArray())) },
                { Symbol.FromString("guess-file"), NativeProcedure.Create<string, List<object>, string>((defaultName, l) =>
                {
                    var found = l.Cast<string>().FirstOrDefault(e => File.Exists(e));
                    return found == null ? defaultName : found;
                }) },
            };
            var itpl = new Interpreter(new[] { appExt });
            return itpl;

        }

        public static void RunRepl()
        {
            AppInterpreter.REPL(Console.In, Console.Out, "Schemy> ", new[] { "Entering repl, try (help) for commands" }); 
        }

        public static void SetVar(string var, object val)
        {
            AppInterpreter.DefineGlobal(Symbol.FromString(var), val);

        }

        public static void RllMain()
        {
            AppInterpreter = CreateItpl();
            var args = System.Environment.GetCommandLineArgs();
            
            var myname = args[0];
            var mybin = Path.GetFileNameWithoutExtension(myname);
            var mypath = Path.GetDirectoryName(myname);
            string script = null;
            SetVar("dp0", mypath);
            if (mybin == "rll")
            {
                if (args.Length == 1)
                {
                    RunRepl();
                    return;

                }
                script = args[1];
                SetVar("args", String.Join(" ", args.Skip(2)));
            } else
            {
                var tries =
                    new[] { $"rll_{mybin}.ss", $"{mybin}.ss", $"rll\\{mybin}.ss" }
                    .Select(t => Path.Combine(mypath, t)).ToArray();


                script = tries.FirstOrDefault(File.Exists);
                if (script == null)
                {
                    Console.WriteLine("Rll: did not find any of: " + String.Join(", ", tries));
                    System.Environment.Exit(1);                        
                }
                SetVar("args", String.Join(" ", args.Skip(1))); 
            }
            var r = AppInterpreter.Evaluate(File.OpenText(script));
            if (r.Error != null)
            {
                Console.WriteLine(r.Error);
                System.Environment.Exit(2);
            }
        }
    }
}

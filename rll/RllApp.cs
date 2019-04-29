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

        // rll-run
        public static Process ConvenienceRun(string cmd, string arg, string cwd)
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
            p.Start();
            return p;
        }

        private static Symbol Sym(string s) => Symbol.FromString(s);

        public static Interpreter CreateItpl()
        {
            var v1Symbols = CoreFunctionsV1();
            Interpreter.CreateSymbolTableDelegate appExt = _ => v1Symbols;
            
            var itpl = new Interpreter(new[] { appExt });
            return itpl;
        }

        private static Dictionary<Symbol, object> CoreFunctionsV1()
        {
            var v1Symbols = new Dictionary<Symbol, object>()
            {
                {
                    Symbol.FromString("repl"), NativeProcedure.Create(() =>
                    {
                        RunRepl();
                        return None.Instance;
                    })
                },

                {
                    Symbol.FromString("os-system"), NativeProcedure.Create<string, string, Process>((cmd, arg) =>
                        ConvenienceRun(cmd, arg, null))
                },
                {
                    Symbol.FromString("unzip"), NativeProcedure.Create<string, string, None>((zipfile, targetdir) =>
                    {
                        ZipFile.ExtractToDirectory(zipfile, targetdir);
                        return None.Instance;
                    })
                },
                {
                    Symbol.FromString("wget"), NativeProcedure.Create<string, string, None>((url, fname) =>
                    {
                        new WebClient().DownloadFile(url, fname);
                        return None.Instance;
                    })
                },

                {
                    Symbol.FromString("ps-wait"), NativeProcedure.Create<Process, int>(p =>
                    {
                        p.WaitForExit();
                        return p.ExitCode;
                    })
                },

                {
                    Symbol.FromString("print"), NativeProcedure.Create<object, object>(o =>
                    {
                        Console.WriteLine(Schemy.Utils.PrintExpr(o));
                        return None.Instance;
                    })
                },

                {
                    Symbol.FromString("os-exit"), NativeProcedure.Create<int, object>(exitCode =>
                    {
                        System.Environment.Exit(exitCode);
                        return None.Instance;
                    })
                },
                {
                    Symbol.FromString("cd"), NativeProcedure.Create<string, None>(path =>
                    {
                        System.Environment.CurrentDirectory = path;
                        return None.Instance;
                    })
                },

                {Symbol.FromString("pwd"), NativeProcedure.Create(() => System.Environment.CurrentDirectory)},
                {Sym("getenv"), NativeProcedure.Create<string, string>((s) => System.Environment.GetEnvironmentVariable(s))},

                {
                    Sym("path-join"),
                    NativeProcedure.Create<List<object>, string>(parts => Path.Combine(parts.Cast<string>().ToArray()))
                },
                {Sym("path-tempfile"), NativeProcedure.Create(() => Path.GetTempFileName())},
                {Sym("path-temppath"), NativeProcedure.Create(() => Path.GetTempPath())},
                {Sym("path-random"), NativeProcedure.Create(() => Path.GetRandomFileName())},
                {
                    Sym("path-remove"), NativeProcedure.Create<string, None>(pth =>
                    {
                        File.Delete(pth);
                        return None.Instance;
                    })
                },
                {
                    Sym("s-format"), NativeProcedure.Create<string, List<object>, string>((formatString, args) =>
                        String.Format(formatString, args.ToArray()))
                },
                {
                    Sym("help"),
                    NativeProcedure.Create(() =>
                        AppInterpreter.Environment.store.Keys.Select(k => k.AsString).Cast<object>().ToList())
                },
                {
                    Symbol.FromString("s-join"),
                    NativeProcedure.Create<string, List<object>, string>((sep, strings) =>
                        String.Join(sep, strings.Cast<string>().ToArray()))
                },
                {
                    Symbol.FromString("guess-file"), NativeProcedure.Create<string, List<object>, string>((defaultName, l) =>
                    {
                        var found = l.Cast<string>().FirstOrDefault(e => File.Exists(e));
                        return found == null ? defaultName : found;
                    })
                },
            };
            return v1Symbols;
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
            if (mybin.Equals("rll", StringComparison.OrdinalIgnoreCase))
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

            using (var fs = File.OpenRead(script))            
            using (var scriptStream = new StreamReader(fs))
            {
                var r = AppInterpreter.Evaluate(scriptStream);
                if (r.Error == null) return;
                Console.WriteLine(r.Error);
                System.Environment.Exit(2);
            }
        }
    }
}

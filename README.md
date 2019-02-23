# rll

Create small .exe launchers scripted with [Schemy](https://github.com/Microsoft/schemy), a very small Scheme interpreter for dotnet.

## Installation

To experiment with it:
```
$ dotnet tool install -g rll
```

To create .exe launchers for your apps, unzip the binaries from https://github.com/vivainio/rll/releases and rename accordingly (see "Creating launchers" below)



## Use cases

- You want to create a launcher that calls something (python, another exe, F# interactive, you name it) with custom arguments.
- You don't want to create .cmd/.bat launcher because they don't exit cleanly
("Terminate batch job (Y/N)?" prompt can't be disabled).
- You are on Windows, for better or worse. PowerShell is not an option because it's fat, slow to launch and bad.
- The launcher .exe needs to be small enough to sensibly add in version control (5kb). Equivalent Go/Rust binaries are > 1MB, Python script launchers (created by pip) are 100kb.

Of course the "launcher" is just a script that may or may not launch stuff.

Here's an example script that downloads and installs Python 3:

```scheme
(define install-python (lambda ()
    (define tgt (path-tempfile))
    (define python-args '(
            "/passive"
            "TargetDir=c:\\Python3"
            "InstallAllUsers=1"
            "InstallLauncherAllUsers=1"
            "Include_pip=1"
            "Include_test=0"
            "Include_doc=0"
            "Shortcuts=0"
            "AssociateFiles=0"
            "PrependPath=0"
            "Include_tcltk=0"

        ))
    (define args-combined (s-join " " python-args))
    (wget "https://www.python.org/ftp/python/3.6.8/python-3.6.8-amd64.exe" tgt)
    (ps-wait (os-system tgt args-combined))
    (path-remove tgt)
))
```

## Creating launchers

Assuming this scheme script is saved as c:\myapp\install_python.ss (or c:\myapp\rll\install_python.ss), and rll.exe (5kb binary) has been SAVED AS c:\myapp\install_python.exe, and librll.dll (40kb) exists in c:\myapp, running install_python.exe will run the script. That is, the name of the launcher .exe is used to find the script to run.

In addition to usual Scheme stuff (define, lambda), this calls built-in helper functions: path-tempfile, s-join, wget, os-system, ps-wait and path-remove.

For list of built in functions, run "rll" without arguments to launch the repl, then run (help):

```
> rll
Entering repl, try (help) for commands
Schemy> (+ 1 2)
3
Schemy> (help)
("repl" "os-system" "unzip" "wget" "ps-wait" "print" "os-exit" "cd" "pwd" "getenv" "path-join" "path-tempfile" "path-temppath" "path-random" "path-remove" "s-format" "help" "s-join" "guess-file" "dp0")
```

## License

MIT


# rll
Rocket Launcher Runner

Create small .exe launchers scripted with [Schemy](https://github.com/Microsoft/schemy), a very small Scheme interpreter for dotnet.

Use case

- You want to create a launcher that calls something (python, another exe, F# interactive, you name it) with custom arguments
- You don't want to create .cmd/.bat launcher because they don't exit cleanly
("Terminate batch job (Y/N)?" prompt can't be disabled)
- The launcher needs to be small

Of course the "launcher" is just a script that may or may not launch stuff.

Here's an example script that downloads and installs Python 3

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

In addition to usual Scheme stuff (define, lambda), this calls built-in helper functions: path-tempfile, s-join, wget, os-system, ps-wait and path-remove.

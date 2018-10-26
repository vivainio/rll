; example: run msbuild14 (preferably), or msbuild.exe in path
(define MSBUILD_PLACES `(
    "C:/Program Files (x86)/MSBuild/14.0/Bin/MSBuild.exe"
    "C:/Windows/Microsoft.NET/Framework64/v4.0.30319/MSBuild.exe"
))

(define msbuild (guess-file
    "msbuild.exe" MSBUILD_PLACES
    ))

(print msbuild)
(os-exit
    (ps-wait (rll-run msbuild args)))

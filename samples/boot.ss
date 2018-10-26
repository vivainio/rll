
(define MSBUILD_PLACES `(
    "C:/Program Files (x86)/MSBuild/14.0/Bin/MSBuild.exe"
    "C:/Windows/Microsoft.NET/Framework64/v4.0.30319/MSBuild.exe"
))

(define run (lambda (cmd arg workdir)
    (define p (ps-new))
    (define si (ps-psi p))
    (psi-exe si cmd)
    (psi-arg si arg)
    (psi-dir si workdir)
    ; (psi-createnowindow si 0)
    ;(psi-redirect-stdout si 1)
    ;(psi-redirect-stderr si 1)
    (psi-shell si 0)
    (ps-interact p)
    (ps-wait p)))

(define msbuild (guess-file
    "msbuild.exe" MSBUILD_PLACES
    ))

(print msbuild)


(cf-truncate 250)
(cf-show-only '("Done Building"))

(highlight-add "*** Warnings:" '("warning"))
(highlight-add "*** Errors:" '("error"))
(run msbuild "BuildSilverlight.xml" "c:/p2p/src/sl")
(rl-save-output "out.txt")

;(define guessed (guess-file "a.txt" '("rll.exe" "c.txt")))

;(run "cmd.exe" "/c tree" "c:/p")
;(highlight-show)
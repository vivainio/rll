
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
    (os-system tgt args-combined)
    (path-remove tgt)
))

(install-python)

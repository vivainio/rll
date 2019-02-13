(define pf
    (getenv "ProgramFiles(x86)"))

(define fwtemplate  "{0}/Microsoft SDKs/F#/{1}/Framework/v4.0/fsi.exe")
(define versions '( "10.1" "4.1" "4.0"))

(define pathforversion (lambda (v) (s-format fwtemplate (list pf v))))

(define all (map pathforversion versions))

(define chosen (guess-file "fsi.exe" all ))

(os-exit
    (ps-wait (os-system chosen args)))


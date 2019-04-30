(define p1 (os-system "sleep" "100"))
(define p2 (os-system "sleep" "5"))
(define all-procs (list p1 p2))

(print all-procs)

(define (kill-all)
  (print "killall")
  (map os-kill all-procs)
)

(define (NOTIFY event)
  (print event)

  (cond
    ((equal? event "service-start") (print "process starting"))
    ((equal? event "exit") (kill-all))
  )
)

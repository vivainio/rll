(define (NOTIFY event)
  (print event)

  (cond
    ((equal? event "service-start") (
        (print "process starting")
        (os-system "sleep" "10")
        (print "process stopping")
        ))
    ((equal? event "exit") (kill-all))
  )
)

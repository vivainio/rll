(define (NOTIFY event)
  (print event)
  (cond
    ((equal? event "exit") (print "Exit event")
    ((equal? event "other") (print "other-event")))))


(print "starting")
(os-exit 2)
(print "ended")
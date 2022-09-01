# Launch wrapper for Windows Service

- Add your launch logic to MyWinService.ss
- Install this service from DOS prompt with:

```
$ SC CREATE "SomeServiceName" binpath= "C:\Path\To\MyWinService.exe"
```

You can rename the exe and .ss file as you wish. Just ensure the names are the same.

Project page for the launcher: https://github.com/vivainio/rll
from __future__ import print_function

import os, shutil, glob

prjdir = "Runner"
version = "1.1"


def c(s):
    print(">", s)
    err = os.system(s)
    assert not err


def nuke(pth):
    if os.path.isdir(pth):
        shutil.rmtree(pth)


def rm_globs(*globs):
    for g in globs:
        files = glob.glob(g)
        for f in files:
            print("Del", f)
            os.remove(f)


nuke(prjdir + "/bin")
nuke(prjdir + "/obj")
nuke("deploy")

c("msbuild rll.sln /p:Configuration=Release")
os.mkdir("deploy")
os.chdir("Runner/bin")

rm_globs("Release/*.pdb", "Release/*.xml", "Release/*.config")
os.rename("Release", "rll")

c("7za a ../../deploy/rll-%s.zip rll" % version)

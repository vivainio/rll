from __future__ import print_function

import os, shutil, glob, textwrap
from pathlib import Path

prjdir = "Runner"
version = "1.3"

ROOT = Path(__file__).absolute().parent

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


version_file_template = textwrap.dedent(
    """
public static class GeneratedVersionInfo
{
    public const string Version = "%s";
}
"""
)

git_tag = os.popen("git describe --tags", "r").read().strip()
open("rll/GeneratedVersionInfo.cs", "w").write(version_file_template % git_tag)

nuke(prjdir + "/bin")
nuke(prjdir + "/obj")
nuke("deploy")
nuke("MyWinService/bin")
nuke("MyWinService/obj")

c("msbuild rll.sln /p:Configuration=Release")
os.mkdir("deploy")
os.chdir("Runner/bin")

rm_globs("Release/*.pdb", "Release/*.xml", "Release/*.config")
os.rename("Release", "rll")

c("7z a ../../deploy/rll-%s.zip rll" % version)

os.chdir(ROOT / "MyWinService/bin")
os.rename("Release", "MyWinService")

c(f"7z a {ROOT/'deploy/MyWinService'}-{version}.zip MyWinService")

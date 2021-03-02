The organization of this extension is a bit complicated.
===

# tl;dr -

0. Requirements:
  - nodejs v12.16.1.
  - npm v6.14.11.
  - git **with symlink support**
    _or_
  - Latest stable [PowerShell 7](https://github.com/PowerShell/PowerShell/releases/tag/v7.1.2) on the path
    _and_
    [Developer Mode](https://docs.microsoft.com/en-us/windows/apps/get-started/enable-your-device-for-development) enabled for Windows.
1. Open a terminal in either the `stable/` or `insiders/` directory.
2. `npm install`
3. Open the appropriate VS Code, e.g., for stable: `code .`, for insiders: `code-insiders .`
4. F5.

# The complicated bits:

The vast majority of the code is shared between the stable and insiders versions of the extension and it lives in the
`common/` directory.  To enable the `stable/` and `insiders/` directories to build, however, symlinks were added to
properly pull `common/` into the source tree under `(stable|insiders)/src/common`.

If you have git configured to handel symlinks then you're good to go.  If not, you'll need to run the script
`.\ensure-symlinks.ps1` whenever you switch from a branch that previously didn't have the symlinks.

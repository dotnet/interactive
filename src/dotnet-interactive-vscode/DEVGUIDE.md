The organization of this extension is a bit complicated.
===

# tl;dr -

1. Open a terminal in this directory.
2. `npm run install:all`
3. Open this directory in VS Code (Stable or Insiders is fine.)
4. F5.

# The complicated bits:

There are really 4 different projects:

- `src/interfaces` contains common interfaces needed across the remaining 3 projects.  The most important file is
`notebook.ts` which is meant to closely copy the necessary interfaces in the latest Insiders (i.e., Beta) version of
VS Code.
- `src/vscode/stable` contains the parts of the extension that _only_ work against VS Code Stable.
- `src/vscode/insiders` contains the parts of the extension that _only_ work against VS Code Insiders.
- The main project is at the root of this directory and is where most of the development will happen.

From there, your regular develop/F5/breakpoint cycle will be fine **with the following exception**: If you need to
change or debug anything in one of the 3 sub-projects (`src/insiders`, `src/vscode/stable`, `src/vscode/insiders`)
then you'll have to perform the following steps:

1. Make the appropriate code change.
2. Re-run `npm run install:all` to rebuild and re-install the sub-project packages.
3. **The most important bit**: your breakpoints won't bind in the original source files; you'll have to open the
generated `.js` files in `node_modules`.  E.g., if you wanted to step through the file `src/vscode/stable/functions.ts`
then after performing steps 1 and 2, you'll need to open the file `node_modules/vscode-stable/out/functions.js`.

module internal FsAutoComplete.InteractiveDirectives

open System
open System.Text.RegularExpressions

/// Remove escaping from standard (non verbatim & non triple quotes) string
let private unescapeStandardString (s: string) =
  let mutable result = ""
  let mutable escaped = false
  let mutable unicodeHeaderChar = '?'
  let mutable remainingUnicodeChars = 0
  let mutable currentUnicodeChars = ""

  for i in [ 0 .. s.Length - 1 ] do
    let c = s.[i]
    if remainingUnicodeChars > 0 then
      if (c >= 'A' && c <= 'Z')
         || (c >= 'a' && c <= 'z')
         || (c >= '0' && c <= '9') then
        currentUnicodeChars <- currentUnicodeChars + string (c)
        remainingUnicodeChars <- remainingUnicodeChars - 1

        if remainingUnicodeChars = 0 then
          result <-
            result
            + string (char (Convert.ToUInt32(currentUnicodeChars, 16)))
      else
        // Invalid unicode sequence, bail out
        result <-
          result
          + "\\"
          + string (unicodeHeaderChar)
          + currentUnicodeChars
          + string (c)
        remainingUnicodeChars <- 0
    else if escaped then
      escaped <- false
      match c with
      | 'b' -> result <- result + "\b"
      | 'n' -> result <- result + "\n"
      | 'r' -> result <- result + "\r"
      | 't' -> result <- result + "\t"
      | '\\' -> result <- result + "\\"
      | '"' -> result <- result + "\""
      | ''' -> result <- result + "'"
      | 'u' ->
          unicodeHeaderChar <- 'u'
          currentUnicodeChars <- ""
          remainingUnicodeChars <- 4
      | 'U' ->
          unicodeHeaderChar <- 'U'
          currentUnicodeChars <- ""
          remainingUnicodeChars <- 8
      | _ -> result <- result + "\\" + string (c)
    else if c = '\\' then
      escaped <- true
    else
      result <- result + string (c)

  if remainingUnicodeChars > 0 then
    result <-
      result
      + "\\"
      + string (unicodeHeaderChar)
      + currentUnicodeChars
  else if escaped then
    result <- result + "\\"

  result


let private loadRegex = Regex(@"#load\s+")
let private standardStringRegex = Regex(@"^""(((\\"")|[^""])*)""")
let private verbatimStringRegex = Regex(@"^@""((""""|[^""])*)""")
let private tripleStringRegex = Regex(@"^""""""(.*?)""""""")

// The following function can be probably moved to general Utils, as it's general purpose.
// Or is there already such function?

/// Get the string starting at index in any of the string forms (standard, verbatim or triple quotes)
let private tryParseStringFromStart (s: string) (index: int) =
  let s = s.Substring(index)
  let verbatim = verbatimStringRegex.Match(s)
  if verbatim.Success then
    let s = verbatim.Groups.[1].Value
    Some(s.Replace("\"\"", "\""))
  else
    let triple = tripleStringRegex.Match(s)
    if triple.Success then
      let s = triple.Groups.[1].Value
      Some s
    else
      let standard = standardStringRegex.Match(s)
      if standard.Success then
        let s = standard.Groups.[1].Value
        Some(unescapeStandardString s)
      else
        None

/// Parse the content of a "#load" instruction at the given line. Returns the script file name on success.
let internal tryParseLoad (line: string) (column: int) =
  let potential =
    seq {
      let matches = loadRegex.Matches(line)
      for i in [ 0 .. matches.Count - 1 ] do
        let m = matches.[i]
        if m.Index <= column then yield m
    }

  match potential |> Seq.tryLast with
  | Some m ->
      let stringIndex = m.Index + m.Length
      tryParseStringFromStart line stringIndex
  | None -> None

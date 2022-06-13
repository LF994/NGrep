// ----------------------------------------------
// NGrep.cs:
// Uses base class from CScanDir.cs based on crdargs.cs
// Build: "csc ngrep.cs cscandir.cs crdargs.cs"
// ----------------------------------------------
using System;
using System.IO;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;


class NGrep : CScanDir {
[STAThread]
public static int Main(string[] args)  {
  NGrep rObj = new NGrep();
  int rc = rObj.Setup(args);
  if (rc == 0)
     rObj.Process();
  else
     rObj.Help();
  return 0;
} /* end Main() */


// flags to control Process():
private bool   m_bIgnoreCase;
private bool   m_bUseRegExp;      // use regular expressions instead of literal
private bool   m_bJustFiles;      // reports only filenames with match
private bool   m_bShowLineNumber; // as prefix of matched line
private bool   m_bShowFileName;   // as prefix of matched line
private bool   m_bFirstMatchOnly;
private bool   m_bShowLineCount;
private bool   m_bMatchWord;
private bool   m_bShowHelp;
private bool   m_bArgsErr;

// processing variables:
private string m_sFileLine;       // line from file just read
private int    m_nFileLineNo;     // 1-based number
private int    m_nLineMatchCount; // matches found in current file
private int    m_nFilesProcessed;
private int    m_nFilesMatched;    // how many files ve at least one match
private int    m_nLineMatchTotal;  // matched lines in all files
private int    m_nLinesTotal;      // total lines in all files
private bool   m_bStop;            // don't need to continue to read file
private Encoding m_objEncoding;    // encoding at the input file

// objects:
private string m_sMatchFormat;  // how to output matched line
private string m_sPattern;      // specified in first positional argiment
private Regex  m_objRegExp;     // does the matches, constructed once

// pointer to LineMatch methods:
private delegate int MatchMethodPtr();   // typedef
private MatchMethodPtr m_pMatchMethod;   // member variable


public NGrep() { // ctor
  m_bIgnoreCase = false;
  m_bUseRegExp  = false;
  m_bJustFiles  = false;
  m_bFirstMatchOnly = false;
  m_bShowLineNumber = true;  // as prefix of matched line
  m_bShowFileName   = false; // as prefix of matched line
  m_bShowLineCount = false;
  m_bMatchWord  = false;
  m_bShowHelp   = false;
  m_bArgsErr    = false;
  m_sPattern    = null;
  m_objEncoding = null;
  m_nFilesProcessed = 0;
  m_sMatchFormat = null;
} /* end ctor()*/


public int Setup(string[] args) {
  int rc = base.Setup(args, "NGREP");
  // base does call to overriden DoFlagChar() for
  // environment variable and each "-flag"
  if (rc != 0)
    return rc;

  // all positional arguments placed into m_sArgs by base, check it:
  if (m_sArgs == null || m_sArgs.Length == 0)
    return 2; // no positional arguments in command line

  // only 1 positional argument, it can be "?" for help
  if (m_sArgs.Length == 1    &&
      m_sArgs[0].Length == 1 &&
      m_sArgs[0][0] == '?') {
    m_bShowHelp = true;
    return 4;  // only 1 positional is not enough to run
  }

  rc = SetupMatch();
  if (rc != 0)
    return rc;

  rc = SetupOutput();
  return rc;
} /* end Setup() */


// how to do line matching
public int SetupMatch() {
  // we have pattern at [0] and at least one file in [1] and next
  m_sPattern  = m_sArgs[0];
  m_objRegExp = null;
  m_pMatchMethod = null;

  if (m_bMatchWord) { // match word, do it by RegExp:
    m_bUseRegExp = true;
    m_sPattern   = "\\b" + m_sPattern + "\\b";
  }

  // setup m_pMatchMethod delegate using flags:
  if (m_bUseRegExp) {
    m_pMatchMethod = this.LineMatchRegExp;
    SetupRegExp();
  }
  else { // simple literal match
    if (m_bIgnoreCase) {
      m_sPattern = m_sPattern.ToUpper();
      m_pMatchMethod = this.LineMatchLiteralNoCase;
    }
    else {  // literal case sensitive match
      m_pMatchMethod = this.LineMatchLiteral;
    }
  }

  int rc = 0; // OK ?
  if (m_pMatchMethod == null) { // just in case
    Console.WriteLine("Setup error: no match method selected");
    rc = 8;
  }
  if (m_bUseRegExp && m_objRegExp == null) {
    rc = 12; // error in regular expression already reported
  }

  return rc;
} /* end SetupMatch() */



public int SetupRegExp() {
  RegexOptions nOpt = RegexOptions.Compiled;
  if (m_bIgnoreCase)
    nOpt |= RegexOptions.IgnoreCase;

  try {
    m_objRegExp = new Regex(m_sPattern, nOpt);
  }
  catch (Exception e) {
    m_objRegExp = null;
    Console.WriteLine(
      "Error:\r\n{0}\r\nCheck regular expression syntax",
      e.Message);
  }
  return 0;
} /* end SetupRegExp() */


// setup matched line output format: {0-1-2}/ used for
// WriteLine(Format, m_sFileName, m_nFileLineNo, m_sFileLine)
// in DoMatchedLine()
public int SetupOutput() {
  if (m_bShowLineNumber && m_bShowFileName)
    m_sMatchFormat = "{0}({1}): {2}"; // as "filename(nn): MatchedLine"
  else if (m_bShowLineNumber)
    m_sMatchFormat = "{1,3}: {2}";    // as "nnn: MatchedLine"
  else if (m_bShowFileName)
    m_sMatchFormat = "{0}: {2}";      // as "filename: MatchedLine
  else
    m_sMatchFormat = "{2}";           // just MatchedLine
  return 0;
}

// process flag argument characters.
// Format is -f[-], where optional - postfix means OFF
public override int DoFlagBool(char cFlg, bool bMode, string sFlag) {
  switch ( cFlg ) { // -flag interpretation
  case 'l': // file names only
  case '0': // don't show matched lines
    m_bJustFiles = bMode;
    break;
  case '1': // first match only
    m_bFirstMatchOnly = bMode;
    break;
  case 'c': // show matched lines count
    m_bShowLineCount = bMode;
    break;
  case 'r': // use regular expression
    m_bUseRegExp = bMode;
  break;
  case 'i': // ignore case
    m_bIgnoreCase = bMode;
    break;
  case 'w': // match word
    m_bMatchWord = bMode;
    break;
  case 'd': // recursive process subdirectories
  case 's':
    m_bSubDir = bMode; // base class
    break;
  case 'n':  // show line number in matched line
    m_bShowLineNumber = bMode;
    break;
  case 'f': // show filename in matched line
    m_bShowFileName = bMode;
    break;
  case '?':
  case 'h':
    m_bShowHelp = true;
    break;
  default:
    Console.WriteLine("Unknown flag <{0}> in <{1}> ignored",
                  cFlg, sFlag);
    m_bArgsErr = true;
    break;
  }
  return 0;
} /* end DoFlagBool() */


public override int DoKeyVal(string sKey, string sVal) {
  // Compare(s1,s2,true) means ignore case
  if (string.Compare(sKey, "cp", true) == 0)
     SetupEncoding(sVal);
  // else if () ...

  return 0;
} /* end DoKeyVal() */


// input file encoding: default is UTF8, can be set in -cp:XXX
public int SetupEncoding(string sVal) {
  int nCode = -5;
  if (!int.TryParse(sVal, out nCode))
     nCode = -1;

  try {
    if (nCode >= 0)
      m_objEncoding = Encoding.GetEncoding(nCode);
    else
      m_objEncoding = Encoding.GetEncoding(sVal);
  }
  catch {
    m_objEncoding = null;
  }

  return 0;
} /* end SetupEncoding() */


public int Process() {
  if (m_sArgs == null || m_sArgs.Length < 1) {
    Console.WriteLine("No files specified!");
    return -1;
  }

  m_nFilesProcessed = 0;
  m_nLineMatchTotal = 0; // matched lines in all files
  m_nFilesMatched   = 0; // files with at least one matched line
  m_nLinesTotal     = 0; // lines in all files

  if (m_sArgs.Length == 1) {
    DoScanDir("*");
  }
  else {
    int j = 1;
    for (; j < m_sArgs.Length; ++j)
      DoScanDir(m_sArgs[j]);
  }

  FinalReport();
  return 0;
} /* end Process() */


public void FinalReport() { // after all files done
  if (m_nFilesProcessed == 0) {
    Console.Write("No files found in:");
    int j = 1;
    for (; j < m_sArgs.Length; ++j)
      Console.Write(" {0}" , m_sArgs[j]);
    Console.Write("\r\n");
  }
  else if (m_nFilesProcessed > 0) {
    if (m_nLineMatchTotal == 0)  // no even single match in all files
      Console.WriteLine(
        "No <{0}> found in {1} file[s] {2} lines, RegEx is {3}",
            m_sPattern , m_nFilesProcessed, m_nLinesTotal,
            m_bUseRegExp ? "ON" : "OFF");
    else if (m_bShowLineCount && m_nLineMatchTotal > 0)
      Console.WriteLine("Total {0} line[s] matched in {1} of {2} files",
        m_nLineMatchTotal, m_nFilesMatched, m_nFilesProcessed);
  }
} /* end FinalReport() */


StreamReader OpenTextFile() {
  if (m_objEncoding == null) // not set, use default
    m_objEncoding = Encoding.UTF8;

  //Encoding.UTF8;           // default mode for sRdr=File.OpenText()
  // some encodings:
  //Encoding.GetEncoding(866); // codepage by int ID
  //Encoding.GetEncoding("cp866"); // codepage by int ID
  //Encoding.GetEncoding(1251);
  //Encoding.GetEncoding("windows-1251");
  //Encoding.GetEncoding("koi8-r");
  //Encoding.ASCII;              // 7 bits, only 0-127
  //Encoding.Unicode;            // LittleEndian, 16 bits per char
  //  == GetEncoding(1200)
  //  == GetEncoding("utf-16")
  //Encoding.BigEndianUnicode;   // marker xFF xFE at the beginning
  //  == GetEncoding(1201)
  //  == GetEncoding("unicodeFFFE")
  //Encoding.ASCIIBigEndianUnicode;
  //  == GetEncoding("utf-7")

  Stream   sInp   =  File.OpenRead(m_sFullPath);
  StreamReader sRdr = new StreamReader(sInp, m_objEncoding);
  return sRdr;
}


public override int DoFileName() {
  ++m_nFilesProcessed;
  m_nFileLineNo     = 0; // 1-based line No (after ReadLine)
  m_nLineMatchCount = 0; // lines matced in this file
  m_bStop = false;

  try   {
    StreamReader sRdr = OpenTextFile(); // by m_sFullPath
    while ((m_sFileLine = sRdr.ReadLine()) != null)  {
      m_nLinesTotal++;
      m_nFileLineNo++;
      if (m_pMatchMethod() > 0) // this line match ?
        DoMatchedLine();        // yes, report it
      if (m_bStop)
        break;
    } /* end while() */
    sRdr.Close();

    if (m_bShowLineCount && m_nLineMatchCount > 0)
      Console.WriteLine(" {0} Lines matched in {1}",
                    m_nLineMatchCount, m_sFileName);
  }
  catch (SecurityException)   {
    Console.WriteLine("File {0} in {1}: Security Exception",
                    m_sFileName, m_sPath);
  }
  catch (FileNotFoundException)    {
    Console.WriteLine("File {0}: Not Found in {1}",
                    m_sFileName , m_sPath);
  }

  return 0;
} /* end DoFileName() */


// may set m_bStop to true when reading no more needed
public void DoMatchedLine() {
  ++m_nLineMatchTotal;   // in all files
  ++m_nLineMatchCount;   // in this file

  if (m_nLineMatchCount == 1) { // first match line just found
    ++m_nFilesMatched;
    if (m_bJustFiles) { // need report only files with match
      m_bStop = true;   // first found, so stop process this file
      Console.WriteLine(m_sFullPath);
      return;
    }
    if (!m_bShowFileName)
      Console.WriteLine("File {0}:",  m_sFullPath);
    // else: filename to be shown on each matched line
  }

  // report matched line:
  Console.WriteLine(m_sMatchFormat, // format string, see SetupOutput()
          m_sFileName, m_nFileLineNo, m_sFileLine);

  if (m_bFirstMatchOnly)
    m_bStop = true;
} /* end DoMatchedLine() */


// --------------------------------------------------------------
// line match methods: return 0 when m_sFileLine
// does not match pattern, othervise return non-0.
// Member variable m_pMatchMethod points to one of LineMatchXXX
// --------------------------------------------------------------

// case-sensitive match m_sFileLine against m_sPattern
public int LineMatchLiteral() {
  int rc = m_sFileLine.IndexOf(m_sPattern);
  //Console.WriteLine("Match <{0}> by <{1}>: rc=<{2}>",
  //         m_sFileLine, m_sPattern, rc );
  if (rc >= 0)
    rc = 1;
  else
    rc = 0;
  return rc;
}


// m_sPattern already in upper case,
// m_sFileLine converted to upper before literal match
public int LineMatchLiteralNoCase() {
  int rc = 0;
  if (m_sFileLine.ToUpper().IndexOf(m_sPattern) >= 0)
    rc = 1; // found
  return rc;
}


// Proper regular expression already in m_objRegex,
// it can be created with or without IgnoreCase,
// so one method handles both case sensitive and insensitive
// along with word match (see Setup())
public int LineMatchRegExp() {
  Match mtch = m_objRegExp.Match(m_sFileLine);
  return (mtch.Success) ? 1 : 0;
}


static char BoolChar(bool bVar) {
  return bVar ? '+' : '-';
}


protected void HelpGeneral() {
Console.WriteLine(@".NET NGrep 0.00
Syntax:  NGREP [-options] pattern file[s]
Options, current defaults shown after flag as + or -:
 -r{0}: Regular expression search (use ""ngrep ? -r"" for RegExp help)
 -l{1}: File Names only (same as -0)
 -1{2}: First Match only to report
 -c{3}: Match Count only to report
 -n{4}: Show line number in matched line
 -f{5}: Show file name in matched line
 -i{6}: Ignore case (literal or regular expressions)
 -d{7}: Search subdirectories (same as -s)
 -w{8}: Word search (as RegEx ""\bPatt\b"")
  Default options can be changed in {9} environment variable
 and overridden in command line. Postfix ""-"" means OFF.",
// ------------------------------------------------------------
// show {defaults} from ctor overridden by environment variable:
// ------------------------------------------------------------
  BoolChar(m_bUseRegExp),      // {0}
  BoolChar(m_bJustFiles),      // {1}
  BoolChar(m_bFirstMatchOnly), // {2}
  BoolChar(m_bShowLineCount),  // {3}
  BoolChar(m_bShowLineNumber), // {4}
  BoolChar(m_bShowFileName),   // {5}
  BoolChar(m_bIgnoreCase),     // {6}
  BoolChar(m_bSubDir),         // {7}
  BoolChar(m_bMatchWord),      // {8}
  m_sEnvVarName );             // {9}

  if (m_sEnvVarValue != null && m_sEnvVarValue.Length > 0)  {
    Console.WriteLine(" Currently: {0}={1}",
         m_sEnvVarName, m_sEnvVarValue);
  }
} /* end HelpGeneral() */


protected void HelpRegExp() {
Console.Write(@"NGREP Regular Expressions:
 .        - match any character
 \t       - match TAB character
 \s       - match any whitespace character
 \S       - match any NON whitespace character
 C*       - match zero or more of C characters
 C+       - match one or more C characters
 C{n}     - match exactly n of C characters
 C{n,m}   - match at least n, but not more than m
 \C       - quote next [special] character C to match it as themself
 ^patt    - match patt at the beginning of line
   Note: after Win98 character ^ (caret) treated as quotation character
   at the command line, so you have to specify ^^ for each single ^.
   Use single ^ to quote < or | or > when you need it at the pattern.
 Patt$    - match Patt at the end of line
 \b       - match word bound (word begin or end)
 \bPatt   - Patt matched at the word begin
 Patt\b   - Patt matched at the word end
 \bPatt\b - whole word Patt matched (it is how -w implemented here)
 \B       - non-word bound: ""er\B"" matches ""er"" in  ""verb"", not in ""never""
  See more in .NET/Perl-5 manuals. Use ""ngrep ? -r-"" for general help.
");
} /* end HeplRegExp() */


public void Help() {
  if (!m_bShowHelp || m_bArgsErr) // short help
    Console.WriteLine(
      "Use: ngrep [-irwcd] pattern file[s]\r\nUse: \"ngrep -?\" for help");
  else if (m_bShowHelp && m_bUseRegExp)
    HelpRegExp();
  else if (m_bShowHelp)
    HelpGeneral();
  else  // very short help
    Console.Write("Use \"ngrep ?\" for help");
} /* end Help() */

} /* end class NGrep */

/* eof ngrep.cs */

// ------------------------
// CReadArgs class: process comand line arguments,
// following syntax assumed:
// -opts  OR /op-s+  (letters and +- as postfix, one letter is one bool,
// - as postfix means OFF, + is also OK and ignored
// (or means ON, may override when option is OFF by default)
// Another syntax for options is:
// -key=val OR /key:val - multiple character name and value
// All others are considered as positional arguments,
// accumulated in m_sArgs
// Use csc /define:_RDATEST crdargs.cs to build test app
// where commnd line arguments will be "interpreted"
// See test sample how to use base class:
// override abstracts DoKeyVal() and DoFlagBool().
// Normally these methods used to setup member variables
// controlling further processing.
// Place real business code into any derived method
// (here: Process()) and call base method Setup() first, see main().
// ------------------------
using System;
using System.IO;
using System.Collections;


#if _RDATEST
// base class usage sample and test, just dump
// processed arguments to stdout from command line
class CMyArgs : CReadArgs {
public static void Main(string[] arrArg)  {
  CMyArgs rObj = new CMyArgs();
  rObj.Setup(arrArg, "TSARG");
  rObj.Process();
} /* end Main() */


public CMyArgs() { // ctor
}


// process boolean flags:
public override int DoFlagBool(char cFlg, bool bMode, string sFlag) {
  Console.WriteLine("DoFlagBool: Flag={0} Mode={1} From={2}",
                    cFlg, bMode, sFlag);
  return 0;
}

// process named flags:
public override int DoKeyVal(string sKey, string sVal) {
  Console.WriteLine("DoKeyVal: Key=<{0}> Val=<{1}>",
                    sKey, sVal);
  return 0;
}

// "processing": in this test sample it is dump
// of all positional arguments from m_sArgs.
// All flags from command line already processed during Setup()
public int Process() {
  if (m_sArgs == null || m_sArgs.Length <= 0) {
    Console.WriteLine("Process: no positional arguments!");
    return -1;
  }
  int j = 0;
  for (; j < m_sArgs.Length; ++j)
    Console.WriteLine("  [{0}] = <{1}>",
         j , m_sArgs[j] );

  Console.WriteLine("Process: {0} args done", m_sArgs.Length);
  return 0;
} /* end Process */
} /* end class */

#endif
/* end of conditional "test driver"*/


// base class

public abstract class CReadArgs {
// next two called from base class to derived during Setup()
// and typically are used to setup internal flags/variables
// from command line for subsequent Process() call, see Main().
public abstract int DoFlagBool(char cFlg, bool bMode, string sFlag);
public abstract int DoKeyVal(string sKey, string sVal);

protected string[] m_sArgs;        // non-flag arguments in original order
protected string   m_sEnvVarName;  // default flags may be here
protected string   m_sEnvVarValue;

public CReadArgs() {
  m_sArgs = null;
  m_sEnvVarName = null;
}

// command line: -flag and positional arguments separated here
public virtual int Setup(string[] args, string sEnvVarName) {
  int rc = 0;
  string sArg = null;
  if (sEnvVarName != null && sEnvVarName.Length > 0) {
    m_sEnvVarName = sEnvVarName;
    sArg = Environment.GetEnvironmentVariable(sEnvVarName);
  }
  if (sArg != null && sArg.Length > 0) {
    m_sEnvVarValue = sArg;
    rc = DoFlagArg(sArg);
  }
  if (rc != 0)
    return rc;

  // accumulate here positional args in original order:
  ArrayList arrArgs = new ArrayList();
  int j = 0;
  for (; j < args.Length; ++j) {
    sArg = args[j];
    if (sArg[0] == '-' || sArg[0] == '/')
      rc += DoFlagArg(sArg);
    else
      arrArgs.Add(sArg);
  } /* end for() */

  // apply positional arguments into member:
  m_sArgs = (string[])arrArgs.ToArray(typeof(string));
  return rc;
} /* end Setup() */


// interpret '-flag' argument.
// character can be postfixed by `-` to reverse meaning
public virtual int DoFlagArg(string sFlag) {
  if (sFlag == null)
    return 0;
  int nLng = sFlag.Length;
  if (nLng == 0)
    return 0;
  if ( CheckKeyVal(sFlag) ) // is it -key:val ?
    return 0;               // yes, done

  // we have here 'boolean' single character
  // flags (no : or = inside)
  bool bMode;
  char cFlg;
  int j = 0;
  for (; j < nLng;  j++) {
    cFlg = sFlag[j];
    if (cFlg == '-' || cFlg == '/' || cFlg == '+')
      continue;

    if (cFlg >= 'A' && cFlg <= 'Z')
      cFlg = (char)(cFlg + 'a' - 'A'); // tolower
    bMode = true;
    if (j < nLng-1 && sFlag[j+1] == '-')  // flag with OFF postfix
      bMode = false;

    DoFlagBool(cFlg, bMode, sFlag); // call to abstract method
  } /* end for(j) */

  return 0;
} /* end DoFlagArg() */


public virtual bool CheckKeyVal(string sFlag) {
  int nLng = sFlag.Length;
  int nPos = 1; // skip first - or /
  while (nPos < nLng) {
    if (sFlag[nPos] == '=' || sFlag[nPos] == ':')
      break;
    else
      ++nPos;
  }
  if (nPos >= nLng) // key:val separator not found
    return false;

  string sKey = sFlag.Substring(1, nPos - 1);
  string sVal = sFlag.Substring(nPos + 1);
  DoKeyVal(sKey , sVal); // call to derived class
  return true;
} /* end CheckKeyVal() */
} /* end class CReadArgs */

/* eof CrdArgs.cs */

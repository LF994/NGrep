// ------------------------
// ScanD.cs: scan for files by mask
// Uses base class from CScanDir.cs
// Build: "csc scsnd.cs cscandir.cs"
// ------------------------
using System;
using System.Collections;


class MyScan : CScanDir {
[STAThread]
public static void Main(string[] args)  {
  MyScan rObj = new MyScan();

  int rc = rObj.Setup(args, null);
  if (rc == 0)
     rObj.Process();
  else
     Console.WriteLine("Error in arguments");
  Console.WriteLine("Done");
} /* end Main() */


public int Process() {
  if (m_sArgs == null || m_sArgs.Length <= 0)
    return -1;
  int j = 0;
  for (; j < m_sArgs.Length; ++j)
    DoScanDir(m_sArgs[j]);
  return 0;
} /* end Process() */


public override int DoKeyVal(string sKey, string sVal) {
  return 0;
}

public override int DoFlagBool(char cFlg, bool bMode, string sFlag) {
  switch ( cFlg ) { // -flag interpretation
  case 'd':
  case 'r':
  case 's':
    m_bSubDir = bMode;
    break;
  default:
    Console.WriteLine("ScanD: Unknown flag {0} ignored in <{1}>",
                  cFlg, sFlag);
    break;
  }
  return 0;
} /* end DoFlagArg() */


public override int DoFileName() {
  Console.WriteLine("  {0}", m_sFullPath);
  return 0;
}
} /* end class MyScan */

/* eof scand.cs */

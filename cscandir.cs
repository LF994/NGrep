// ------------------------
// Process directory scan for file names by Path\Name.Ext
// mask with possible wildcards in Name.Ext
// ------------------------
using System;
using System.IO;
using System.Collections;


public abstract class CScanDir : CReadArgs {
// process file in derived class: m_sFullPath, m_sPath, m_sFileName
// already set before this call, typically by derived class.
// After that base class calls to DoScanDir() and receive calls
// to overridden DoFileName(): variables m_FileName / Path/FullPath
// already set at this moment. FileCount/DirCount also updated here.
// Flag m_bSubDir may ne set by derived to enable subrirectory scan.
public abstract int DoFileName();

protected bool     m_bSubDir;
protected int      m_nFileCount;
protected int      m_nDirCount;
protected string   m_sPath;
protected string   m_sFileName;
protected string   m_sFullPath;  // above path + name

public CScanDir() {
  m_bSubDir = false;
  m_nFileCount = 0;
  m_nDirCount  = 0;
}


public void DoScanDir(string sArg) { // example: "c:\*.ext"
  string sPath;
  string sMask;
  SplitPath(sArg, out sPath, out sMask);
  DoScanDir(sPath, sMask);
} /* end DoScanDir() */


public void DoScanDir(string sPath, string sMask) {
  ++m_nDirCount;
  string[] fileList = Directory.GetFiles(sPath, sMask);
  int i = 0;
  for (; i < fileList.Length; ++i) {
    ++m_nFileCount;
    m_sFullPath = fileList[i];
    SplitPath(m_sFullPath, out m_sPath, out m_sFileName);
    DoFileName();
  }

  if ( !m_bSubDir )
    return;

  // Get recursively from subdirectories
  string[] sDirList = Directory.GetDirectories(sPath);
  for (i = 0; i < sDirList.Length; ++i)
    DoScanDir(sDirList[i], sMask); // recursive call here

  return;
} /* end DoScanDir() */


static void SplitPath(string sPath, out string sDir, out string sName) {
  int i = sPath.Length;
  while (i > 0) {
    char ch = sPath[i - 1];
    if (ch == '\\' || ch == '/' || ch == ':')
      break;
    i--;
  }

  sName = sPath.Substring(i);
  sDir  = sPath.Substring(0, i);
  if (sDir.Length == 0)
     sDir = Environment.CurrentDirectory;
  sDir = Path.GetFullPath(sDir);
  // out sDir and sName: returned
} /* end SplitPath() */
} /* end class CScandir */

/* eof CScanDir.cs */

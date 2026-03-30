// Copyright 2026, Patrice CHARBONNEAU
//                 a.k.a. Sigma3Wolf
//                 oIId: v2.00/2032/160e0e6a3176a8c4235332aa8e0d422c
//                 All rights reserved.
//                 https://b13.ca/
//
// This source code is licensed under the [BSD 3-Clause "New" or "Revised" License] found
// in the LICENSE file in the root directory of this source tree.

#region Usage and dependency
//*************************************************************************************************//
//** WARNING: If you modify this file, you MUST rename it to exclude the version number :WARNING **//
//*************************************************************************************************//
//      Usage: Use Interrop to get a directory Tree, Use Event (not mandatory)
// Dependency:
#endregion Usage and dependency

#region History
//    History:
// v2.00 - 2025-07-25:	Initial release;
// v2.01 - 2025-08-10:  Exclude list can now include file;
//                      File list is now returned as a list;
//                      Adding BasePath and options;
// v2.02 - 2025-08-16:  Adding recursivity settings;
// v2.03 - 2025-08-18:  IncludeBasePath default to false;
//                      Reverse order of pblnIncludeDirectorty and pblnIncludeFile;
//                      Fix a bug where directory were added even if pblnIncludeDirectorty was false;
// v2.04 - 2025-09-28:  Add Empty scan for structure and progression Event; Improve efficience (speed);
// v2.05 - 2025-09-28:  Add accepted file extention;
// v2.06 - 2026-02-10:  Mainly cosmetic, fixed a public to private function;
// v2.07 - 2026-02-11:  Fix a progressBar logic;
// v2.08 - 2026-03-14:  Add GetPhysicalDrives();
//                      Add IsFolderValid();
//                      Add HasSubDirectories();
// v2.09 - 2026-03-16:  Mod Licence heading;
//                      oIId changed;
// v2.10 - 2026-03-26:  Transfering old function from v1.03;
//                      Removing static;
//                      Adding IsValidPath;
//                      Adding Progress Event;
//                      now using b13 namespace;
// v2.11 - 2026-03-27:  removing unused parent Form object;
// v2.12 - 2026-03-29:  replacing IsNullOrEmpty by IsNullOrWhiteSpace;

#endregion History

#region b13 namespace
#pragma warning disable IDE0130
namespace b13;
#pragma warning restore IDE0130
#endregion b13 namespace

#region ** Example of use **
//Example of use:
//  //valid for version 2026.3.17.23850
//  this.InitSearch();
//  private void InitSearch() {
//      List<string> lstExtention = [];
//      StructDirectoryEx.SetParent(this);
//      StructDirectoryEx.BasePath = AppExPath.GetAppPath;
//
//      StructDirectoryEx.ExcludedScan.Add("[D].vs");
//      StructDirectoryEx.OnDirectoryEvent += this.OnDirectoryEvent;
//
//      StructDirectoryEx.ScanFilename = false;
//      StructDirectoryEx.IncludeDirectory = true;  //will turn on anyway if [StructDirectoryEx.ScanFilename = false]
//
//      lstExtention.Add(".cs");
//      lstExtention.Add(".razor");
//      lstExtention.Add(".css");
//      lstExtention.Add(".js");
//      lstExtention.Add(".htm");
//      lstExtention.Add(".html");
//      StructDirectoryEx.ExtentionF = lstExtention;
//  }

//  private void CmdScan_Click(object? sender, EventArgs e) {
//      //public var:
//      //glstScan = new List<string>();
//
//      lstFiles.Items.Clear(); // ListBox object
//      StructDirectoryEx.DoScanDirStruct(out glstScan);
//  }

//  private void OnDirectoryEvent(object? sender, StructDirectoryEx.MyEventArgs e) {
//      int lngDebug = 0;
//      switch (e.ArgEventNo) {
//          case 1:
//              //Receiving the number of directory after a base scan
//              //but we don't really need it because we are working in %
//              toolStripProgressBar1.Value = 0;
//              toolStripProgressBar1.Maximum = 100;
//
//              //lngDebug = e.ArgValue1;
//              // [e.ArgValue1] contain the maximum number of element depending on your scan type
//              break;
//
//          case 2:
//              //Receiving the current directory progression of full scan
//              if (e.ArgValue1 > 0) {
//                  // [e.ArgValue1] contain the current element toward the maximum depending on your scan type
//
//                  // we are working in percentage here
//                  lngDebug = e.ArgValue1;
//                  progressBar1.Value = e.ArgValue2;
//              } else if (e.ArgValue1 == -1) {
//                  //progress is now 100% (if it's not there is a logic bug in the library)
//                  if (glstScan != null && glstScan.Count > 0) {
//                      lstFiles.Items.AddRange([.. glstScan]);
//                      UpdateHorizontalScroll(lstFiles);
//                  }
//              } else {
//                  //Error [e.ArgValue1] occur
//              }
//              break;
//
//          default:
//              throw new NotImplementedException();
//      }
//      Application.DoEvents();
//  }
#endregion ** Example of use **

internal class StructDirectoryEx {
    #region Declaration
    public enum GroupOperation {
        IncludingOnly,
        ExcludingAll
    }

    private const int MAX_PATH = 260;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private struct WIN32_FIND_DATA {
        public FileAttributes dwFileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint dwReserved0;
        public uint dwReserved1;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
        public string cFileName;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool FindClose(IntPtr hFindFile);

    // ***************************************************************
    private string m_strBasePath = "";
    #endregion Declaration

    #region Constructor
    //There isn't one yet
    //public void SetParent(Form pobjForm) {
    //    if (UserForm == null) {
    //        UserForm = pobjForm;
    //    }
    //}

    //private Form? UserForm {
    //    get;
    //    set;
    //}
    #endregion Constructor

    #region Property
    public string BasePath {
        get {
            return m_strBasePath;
        }
        set {
            m_strBasePath = value;
            if (!string.IsNullOrWhiteSpace(m_strBasePath) && m_strBasePath.EndsWith(System.IO.Path.DirectorySeparatorChar)) {
                m_strBasePath = m_strBasePath.Substring(0, m_strBasePath.Length - 1);
            }
        }
    }

    public bool ScanFilename { get; set; } = true;

    public bool Recursive { get; set; } = true;

    public bool Cancel { get; set; } = false;

    public bool IncludeFullPath { get; set; } = false;

    public bool IncludeDirectory { get; set; } = false;

    //List of accepted/excluded file extention to look for when scanning
    public List<string> ExtentionF { get; set; } = [];

    //List of accepted/excluded directory extention to look for when scanning
    public List<string> ExtentionD { get; set; } = [];

    //Excluded [D]DirName and [F]Filename from scan
    public List<string> ExcludedScan { get; set; } = [];
    #endregion Property

    #region public Functions
    //Validate if the directory can be accessed
    public static Boolean IsValidPath(String pstrPath) {
        Boolean blnReturnValue = false;

        try {
            List<String> lstScan = new List<String>(Directory.EnumerateDirectories(pstrPath));
            blnReturnValue = true;
        } catch {
        }

        return blnReturnValue;
    }

    public List<string> GetPhysicalDrives() {
        List<string> lstRet = [];

        try {
            // 1. Récupération de tous les lecteurs logiques du système
            DriveInfo[] arrDrives = DriveInfo.GetDrives();

            foreach (DriveInfo objDrive in arrDrives) {
                // 2. On ne conserve que les lecteurs prêts (évite les erreurs sur CD/USB vides)
                if (objDrive.IsReady == true) {
                    if (objDrive.DriveType == DriveType.Fixed) {
                        lstRet.Add(objDrive.Name);
                        System.Diagnostics.Debug.WriteLine($"Lecteur trouvé : {objDrive.Name}");
                    }
                }
            }
        } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"Erreur lors de la lecture des lecteurs : {ex.Message}");
        }

        return lstRet;
    }

    public bool IsFolderValid(string pstrPath) {
        bool blnRet = false;

        if (!string.IsNullOrWhiteSpace(pstrPath)) {
            blnRet = System.IO.Directory.Exists(pstrPath);
        }

        return blnRet;
    }

    public bool HasSubDirectories(string pstrPath) {
        bool blnRet = false;

        try {
            if (System.IO.Directory.Exists(pstrPath)) {
                // On filtre pour ignorer les attributs 'Hidden' ou 'System'
                blnRet = new System.IO.DirectoryInfo(pstrPath)
                    .EnumerateDirectories()
                    .Any(objDir => (objDir.Attributes & (System.IO.FileAttributes.Hidden | System.IO.FileAttributes.System)) == 0);
            }

            //// On s'arrête au premier élément trouvé (très performant)
            //blnRet = System.IO.Directory.EnumerateDirectories(pstrPath).Any();
        } catch (Exception ex) {
            // Règle 8 : Debug au lieu de Console
            System.Diagnostics.Debug.WriteLine($"Accès refusé ou erreur : {ex.Message}");
        }

        return blnRet;
    }

    public string[] GetVisibleDirectories(string pstrPath) {
        string[] strRet = [];

        try {
            if (System.IO.Directory.Exists(pstrPath)) {
                // Récupération initiale via la méthode demandée
                string[] strAllDirs = System.IO.Directory.GetDirectories(pstrPath);

                // Filtrage pour exclure System et Hidden
                strRet = strAllDirs.Where(strSubPath => {
                    System.IO.FileAttributes objAttr = System.IO.File.GetAttributes(strSubPath);
                    return (objAttr & System.IO.FileAttributes.Hidden) == 0 &&
                           (objAttr & System.IO.FileAttributes.System) == 0;
                }).ToArray();
            }
        } catch (System.Exception ex) {
            System.Diagnostics.Debug.WriteLine($"Erreur lors de la lecture des répertoires : {ex.Message}");
        }

        return strRet;
    }

    public string GetEntryName(string pstrEntry, string pstrSpecific = "") {
        string strRet = "";

        // 1. Protection contre les chaînes trop courtes
        if (pstrEntry.Length >= 3) {
            // 2. Utilisation d'un Span pour la lecture du préfixe (évite une allocation string)
            ReadOnlySpan<char> span = pstrEntry.AsSpan();

            // 3. Comparaison directe
            if (pstrSpecific.Length > 0) {
                if (pstrEntry[..3] == pstrSpecific) {
                    strRet = pstrEntry[3..];
                }
            } else {
                if (span.StartsWith("[F]") || span.StartsWith("[D]")) {
                    strRet = pstrEntry[3..];
                }
            }
        }

        if (strRet.Length == 0) {
            throw new NotImplementedException($"this is an invalid Entry [{pstrEntry}]");
        }
        return strRet;
    }

    public void DoScanDirStruct(out List<string> plstScan, GroupOperation penmExtOperation = GroupOperation.IncludingOnly) {
        //Force a Directory Scan
        plstScan = [];

        //FileScanEnum.ReportDirectoryOnly:
        //int lngSuccess = 0;    // 0 : success, -1 : RESERVED for end progressBar, -2, and less : Error
        int lngMaximum = this.ScanDirStruct(out int lngDir, out int lngFiles, penmExtOperation);
        if (lngMaximum >= 0) {
            if (lngMaximum > 0) {
                Cancel = false;
                int lngTotalDir = 0;
                int lngTotalFiles = 0;
                _ = ScanDirectoryEx(lngMaximum, out _, out _, ref lngTotalDir, ref lngTotalFiles, ref plstScan, penmExtOperation, BasePath, false);
                plstScan.Sort(StringComparer.OrdinalIgnoreCase);
            }

            //All Operation completed
            // -1 : RESERVED for end progressBar end of progress
            RaiseEvent_DirectoryProgress(-1, 100);
        } else {
            // User cancelled or there were an error
            if (lngMaximum == -1) {
                //Error code must be less then -1, this value is reserved
                //we could fix this by using a new event for Error instead of [RaiseMyEvent_DirectoryProgress]
                throw new Exception("Oups! Error -1");
            }
            RaiseEvent_DirectoryProgress(lngMaximum, 0);
        }
    }
    #endregion public Functions

    #region private Section for ScanDirStruct
    // use DoScanDirStruct() for your program
    //Scan the directory structure only, retourne number of directory and optionally file
    private int ScanDirStruct(out int plngDir, out int plngFiles, GroupOperation penmExtOperation) {
        //reset data
        plngDir = 0;
        plngFiles = 0;

        Cancel = false;
        int lngSuccess;    // 0 : success, -1 : RESERVED for end progressBar, -2, and less : Error

        if (BasePath.Length > 2) {
            lngSuccess = ScanDirStructEx(BasePath, out int lngDir, out plngFiles, penmExtOperation);

            if (lngSuccess == 0) {
                // we need to remove basePath, to understand, check
                // logic [if (lngMaximum > 0)] in DoScanDirStruct
                plngDir = lngDir - 1;   

                if (ScanFilename) {
                    lngSuccess = plngFiles;
                    RaiseEvent_ScanCompleted(plngFiles, plngDir);
                } else {
                    lngSuccess = plngDir;
                    RaiseEvent_ScanCompleted(plngDir, plngFiles);
                }
            }
        } else {
            lngSuccess = -4;
        }

        return lngSuccess;
    }

    //INTERNAL: The real work is here, you don't call this one.
    //This is to allow a progressbar when doing a full (Dir + file) scan.
    //This is ScanDirStruct, understand it is FORCED to be recursive, if no recursive is needed, progression status isn't eitheir

    //private bool ScanDirStructEx(string pstrRootPath, out int plngDir, out int plngFiles, GroupOperation penmExtOperation) {
    private int ScanDirStructEx(string pstrRootPath, out int plngDir, out int plngFiles, GroupOperation penmExtOperation) {
        int lngSuccess = 0;    // 0 : success, -1 : RESERVED for end progressBar, -2, and less : Error

        plngDir = 0;
        plngFiles = 0;

        if (!Cancel) {
            string strData;

            //Search for excluded directory
            bool blnIncluded = true;

            int lngInstrRev = pstrRootPath.LastIndexOf(System.IO.Path.DirectorySeparatorChar);
            if (lngInstrRev > -1) {
                strData = pstrRootPath.Substring(lngInstrRev + 1);
                blnIncluded = CheckExtentionD(strData, penmExtOperation, out _, false);
            }

            //Check if this directory is excluded from scan
            if (blnIncluded) {
                string searchPath = System.IO.Path.Combine(pstrRootPath, "*");
                IntPtr hFind = FindFirstFile(searchPath, out WIN32_FIND_DATA findData);
                if (hFind != new IntPtr(-1)) {
                    plngDir++;

                    try {
                        bool blnNextFile = true;
                        do {
                            string strTmpPath = System.IO.Path.Combine(pstrRootPath, findData.cFileName);

                            // Check if it's a directory
                            if ((findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory) {
                                // Skip . and ..
                                if (findData.cFileName != "." && findData.cFileName != "..") {
                                    //blnSuccess = ScanDirStructEx(strTmpPath, out int lngDir, out int lngFiles, penmExtOperation);
                                    lngSuccess = ScanDirStructEx(strTmpPath, out int lngDir, out int lngFiles, penmExtOperation);
                                    if (lngSuccess == 0) {
                                        plngDir = plngDir + lngDir;
                                        plngFiles = plngFiles + lngFiles;
                                    } else {
                                        Cancel = true;
                                    }

                                    //this way we don't update for empty directory 
                                    Application.DoEvents();
                                }
                            } else {
                                //we don't take into account included or excluded file
                                //this is a total file count for progressbar
                                plngFiles++;
                            }

                            blnNextFile = FindNextFile(hFind, out findData);
                        } while (!Cancel && blnNextFile);
                    } catch {
                        //there were an error
                        lngSuccess = -3;
                    } finally {
                        FindClose(hFind);
                    }
                }
            }
        } else {
            //user Cancelled
            lngSuccess = -2;
        }

        return lngSuccess;
    }
    #endregion private Section for ScanDirStruct

    #region private Section for ScanDirectory
    private bool ScanDirectoryEx(int plngMaximum, out int plngDir, out int plngFiles, ref int plngTotalDir, ref int plngTotalFile, ref List<string> plstOutputList, GroupOperation penmExtOperation, string pstrRootPath, bool pblnBaseIsNotRoot) {
        //Earch time a file is HIT, it should EventIt to raise progression % based on a total of plngMaximum
        bool blnSuccess = true;

        plngDir = 0;
        plngFiles = 0;
        int lngLocalDir = 0;
        int lngLocalFile = 0;

        if (!Cancel) {
            string strData;
            int lngInstrRev;

            //Search for excluded directory
            strData = pstrRootPath;

            //Check if this directory is excluded from scan
            bool blnIncluded = CheckExtentionD(strData, penmExtOperation, out string strExtracted, pblnBaseIsNotRoot);
            if (blnIncluded) {
                if (pblnBaseIsNotRoot) {
                    if (IncludeDirectory || (!IncludeDirectory && !ScanFilename)) {
                        plstOutputList.Add(strExtracted);
                    }
                }

                string searchPath = System.IO.Path.Combine(pstrRootPath, "*");
                IntPtr hFind = FindFirstFile(searchPath, out WIN32_FIND_DATA findData);
                if (hFind != new IntPtr(-1)) {
                    try {
                        bool blnNextFile = true;
                        do {
                            string strTmpPath = System.IO.Path.Combine(pstrRootPath, findData.cFileName);

                            // Check if it's a directory
                            if ((findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory) {
                                // Skip . and ..
                                if (findData.cFileName != "." && findData.cFileName != "..") {
                                    if (Recursive) {
                                        // Recursively list subdirectories
                                        List<string> lstSubScanOutput = [];
                                        
                                        if (lngLocalFile != 0) {
                                            //b13x: we need to reevaluate this... it work but...
                                            //shouldn't we use output of following ScanDirectoryEx ?
                                            //placing it BEFORE ScanDirectoryEx does give more frequen uptade
                                            //remember this is only for progress bar
                                            plngTotalFile += lngLocalFile;
                                            lngLocalFile = 0;

                                            if (ScanFilename) {
                                                int lngPercent = (int)((plngTotalFile * 100.0) / plngMaximum);
                                                RaiseEvent_DirectoryProgress(plngTotalFile, lngPercent);
                                            }
                                        }
                                        blnSuccess = ScanDirectoryEx(plngMaximum, out int lngDir, out int lngFiles, ref plngTotalDir, ref plngTotalFile, ref lstSubScanOutput, penmExtOperation, strTmpPath, true);

                                        plngDir = plngDir + lngDir;
                                        plngFiles = plngFiles + lngFiles;

                                        if (!blnSuccess) {
                                            Cancel = true;
                                        } else if (lstSubScanOutput.Count > 0) {
                                            plstOutputList.AddRange(lstSubScanOutput);
                                        }
                                    } else {
                                        if (IncludeDirectory || (!IncludeDirectory && !ScanFilename)) {
                                            blnIncluded = CheckExtentionD(strTmpPath, penmExtOperation, out strExtracted, pblnBaseIsNotRoot);
                                            if (blnIncluded) {
                                                plstOutputList.Add(strExtracted);
                                            }
                                        }
                                    }

                                    //this way we don't update for empty directory 
                                    Application.DoEvents();
                                }
                            } else {
                                //we don't take into account included or excluded name
                                //this is a total object count for progressbar
                                lngLocalFile++;
                                
                                if (ScanFilename) {
                                    //Search for excluded file
                                    bool blnExcludedFile = false;
                                    lngInstrRev = strTmpPath.LastIndexOf(System.IO.Path.DirectorySeparatorChar);
                                    if (lngInstrRev > -1) {
                                        //StructDirectoryEx.lstExcludedScan.Add("[F]" + MyFile);
                                        //string strSearch = "[F]" + strTmpPath.Substring(lngInstrRev + 1);
                                        string strSearch = string.Concat("[F]", strTmpPath.AsSpan(lngInstrRev + 1));
                                        blnExcludedFile = ExcludedScan.Contains(strSearch, StringComparer.InvariantCultureIgnoreCase);
                                    }

                                    if (!blnExcludedFile) {
                                        strData = strTmpPath;
                                        blnIncluded = CheckExtentionF(strData, penmExtOperation, out strExtracted, true);
                                        if (blnIncluded) {
                                            plngFiles++;
                                            plstOutputList.Add(strExtracted);
                                        }
                                    }
                                    Application.DoEvents();
                                }
                            }

                            blnNextFile = FindNextFile(hFind, out findData);
                        } while (!Cancel && blnNextFile);
                    } catch {
                        blnSuccess = false;
                    } finally {
                        FindClose(hFind);
                    }
                }

                // We finished a directory
                if (pblnBaseIsNotRoot) {
                    lngLocalDir++;
                }

                // let's raise events if count change
                if (ScanFilename) {
                    if (lngLocalFile != 0) {
                        plngTotalFile += lngLocalFile;
                        lngLocalFile = 0;

                        int lngPercent = (int)((plngTotalFile * 100.0) / plngMaximum);
                        RaiseEvent_DirectoryProgress(plngTotalFile, lngPercent);
                    }
                } else {
                    if (lngLocalDir != 0) {
                        plngTotalDir += lngLocalDir;
                        lngLocalDir = 0;

                        int lngPercent = (int)((plngTotalDir * 100.0) / plngMaximum);
                        RaiseEvent_DirectoryProgress(plngTotalDir, lngPercent);
                    }
                }
            }
        } else {
            blnSuccess = false;
        }

        return blnSuccess;
    }

    private bool CheckExtentionF(string pstrData, GroupOperation penmExtOperation, out string pstrExtracted, bool pblnDoExtract) {
        bool blnRet = CheckExtention(pstrData, penmExtOperation, ExtentionF, "[F]", out pstrExtracted, pblnDoExtract);
        return blnRet;
    }

    private bool CheckExtentionD(string pstrData, GroupOperation penmExtOperation, out string pstrExtracted, bool pblnDoExtract) {
        bool blnRet = CheckExtention(pstrData, penmExtOperation, ExtentionD, "[D]", out pstrExtracted, pblnDoExtract);
        return blnRet;
    }

    private bool CheckExtention(string pstrData, GroupOperation penmExtOperation, List<string> plstExtention, string pstrType, out string pstrExtracted, bool pblnDoExtract) {
        bool blnRet = true; //Assume included
        pstrExtracted = "";

        if (plstExtention.Count > 0) {
            //Get File Extention
            string strExt = Path.GetExtension(pstrData);

            //Test for accepted or denied extention
            if (penmExtOperation == GroupOperation.IncludingOnly) {
                if (!plstExtention.Contains(strExt)) {
                    blnRet = false;
                }
            } else {
                if (plstExtention.Contains(strExt)) {
                    blnRet = false;
                }
            }
        }

        //Extract file/Dir name
        if (blnRet) {
            string strShort = pstrData;
            int lngInstrRev = pstrData.LastIndexOf(System.IO.Path.DirectorySeparatorChar);
            if (lngInstrRev > -1) {
                strShort = pstrData.Substring(lngInstrRev + 1);
            }
            blnRet = !ExcludedScan.Contains(pstrType + strShort, StringComparer.InvariantCultureIgnoreCase);

            if ((pblnDoExtract) && (!IncludeFullPath)) {
                if (pstrData.Length > BasePath.Length) {
                    if (pstrData.StartsWith(BasePath, StringComparison.OrdinalIgnoreCase)) {
                        //pstrExtracted = pstrType + pstrData.Substring(BasePath.Length + 1);
                        pstrExtracted = string.Concat(pstrType, pstrData.AsSpan(BasePath.Length + 1));
                    }
                }
            }
        }

        return blnRet;
    }
    #endregion private Section for ScanDirectory

    #region EVENT section
    //https://www.tutorialsteacher.com/csharp/csharp-event
    // Using example:
    //public Form1() {
    //    InitializeComponent();
    //    StructDirectoryEx.OnDirectoryEvent += this.OnDirectoryEvent;
    //    StructDirectoryEx.SetParent(this);
    //}

    //private void OnDirectoryEvent(object? sender, StructDirectoryEx.MyEventArgs e) {
    //    //e.ArgEventTime
    //    ShowPercent(e.ArgCount);
    //}

    //class for data passed to the event
    public class DirectoryEventArgs : EventArgs {
        public DateTime ArgEventTime {
            get; set;
        } = DateTime.Now;

        //ArgEventNo :
        //              1 => Return the Number of Directory from baseScan
        //              2 => Return the sequentialId of the Directory it just finished completed
        //              not exist yet 3 => Return the sequentialId of the File it just finished completed
        public int ArgEventNo {
            get; set;
        } = 0;

        public int ArgValue1 {
            get; set;
        } = 0;

        public int ArgValue2 {
            get; set;
        } = 0;

        public string ArgData {
            get; set;
        } = "";
    }

    public event EventHandler<DirectoryEventArgs>? OnDirectoryEvent; // event

    private void RaiseEvent(DirectoryEventArgs pEventArgs) {
        //if ProcessCompleted is not null then call delegate
        if (OnDirectoryEvent != null) {
            OnDirectoryEvent.Invoke(this, pEventArgs);
            //Application.DoEvents();
        } else {
            //no events registered
            //debug.WriteLine("MyEvent is not registered in main program");
        }
    }

    private void RaiseEvent_ScanCompleted(int plngTotalValue1, int plngTotalValue2) {
        //calling the event
        DirectoryEventArgs eventArgs = new DirectoryEventArgs {
            ArgEventNo = 1,
            ArgValue1 = plngTotalValue1,
            ArgValue2 = plngTotalValue2
        };
        RaiseEvent(eventArgs);
    }

    //Return the sequentialId of the Directory it just finished scanning
    private void RaiseEvent_DirectoryProgress(int plngValue, int plngPercent) {
        //calling the event
        DirectoryEventArgs eventArgs = new DirectoryEventArgs {
            ArgEventNo = 2,
            ArgValue1 = plngValue,
            ArgValue2 = plngPercent
        };
        RaiseEvent(eventArgs);
    }
    #endregion EVENT section
}

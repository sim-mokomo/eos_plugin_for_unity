using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEditor;
using UnityEngine;
using System.IO;
using PlayEveryWare.EpicOnlineServices;

public class EOSOnPostprocessBuild_Windows:  IPostprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }
    private string[] postBuildFiles = {
    };

    //-------------------------------------------------------------------------
    private static string GetPackageName()
    {
        return "com.playeveryware.eos";
    }

    //-------------------------------------------------------------------------
    private static string GetPathToEOSBin()
    {
        string projectPathToBin = Path.Combine(Application.dataPath, "../bin/");
        string packagePathToBin = Path.GetFullPath("Packages/" + GetPackageName() + "/bin~/");

        if (Directory.Exists(packagePathToBin))
        {
            return packagePathToBin;
        }
        else if (Directory.Exists(projectPathToBin))
        {
            return projectPathToBin;
        }
        return "";
    }

    //-------------------------------------------------------------------------
    private static string GetPathToPlatformSepecificAssetsForWindows()
    {
        string packagePathname = Path.GetFullPath("Packages/" + GetPackageName() + "/PlatformSpecificAssets~/EOS/Windows/");
        string platformSpecificPathname = Path.Combine(Application.dataPath, "../PlatformSpecificAssets/EOS/Windows/");
        string pathToInstallFrom = "";
        // If the Plugin is installed with StreamAssets, install them
        if (Directory.Exists(packagePathname))
        {
            // Install from package path
            pathToInstallFrom = packagePathname;
        }
        else if (Directory.Exists(platformSpecificPathname))
        {
            pathToInstallFrom = platformSpecificPathname;
        }

        return pathToInstallFrom;
    }

    //-------------------------------------------------------------------------
    private static void InstallBootStrapper(BuildReport report, string pathToEOSBootStrapperTool, string pathToEOSBootStrapper)
    {
        string installPathForExe = report.summary.outputPath;
        string installDirectory = Path.GetDirectoryName(installPathForExe);
        string installPathForEOSBootStrapper = Path.Combine(installDirectory, "EOSBootStrapper.exe");
        string bootStrapperArgs = ""
           + " --source-bootstrapper-path " + "\"" + pathToEOSBootStrapper + "\""
           + " --target-bootstrapper-path " + "\"" + installPathForEOSBootStrapper + "\""
           + " --target-application-path "  + "\"" + installPathForExe + "\""
        ;

        var procInfo = new System.Diagnostics.ProcessStartInfo();
        procInfo.FileName = pathToEOSBootStrapperTool;
        procInfo.Arguments = bootStrapperArgs;
        procInfo.UseShellExecute = false;
        procInfo.WorkingDirectory = installDirectory;
        procInfo.RedirectStandardOutput = true;
        procInfo.RedirectStandardError = true;

        var process = new System.Diagnostics.Process { StartInfo = procInfo };
        process.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler((sender, e) => {
            if(!EmptyPredicates.IsEmptyOrNull(e.Data))
            {
                Debug.Log(e.Data);
            }
        });

        process.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler((sender, e) =>{
            if(!EmptyPredicates.IsEmptyOrNull(e.Data))
            {
                Debug.LogError(e.Data);
            }
        });

        bool didStart = process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        process.Close();

    }

    //-------------------------------------------------------------------------
    private void InstallFiles(BuildReport report)
    {
        string destDir = Path.GetDirectoryName(report.summary.outputPath);
        string pathToInstallFrom = GetPathToPlatformSepecificAssetsForWindows();

        if (!EmptyPredicates.IsEmptyOrNull(pathToInstallFrom))
        {
            foreach (var fileToInstall in postBuildFiles)
            {
                string fileToInstallPathName = Path.Combine(pathToInstallFrom, fileToInstall);

                if (File.Exists(fileToInstallPathName))
                {
                    string fileToInstallParentDirectory = Path.GetDirectoryName(Path.Combine(destDir, fileToInstall));

                    if (!Directory.Exists(fileToInstallParentDirectory))
                    {
                        Directory.CreateDirectory(fileToInstallParentDirectory);
                    }
                    string destPathname = Path.Combine(fileToInstallParentDirectory, Path.GetFileName(fileToInstallPathName));

                    if (File.Exists(destPathname))
                    {
                        File.SetAttributes(destPathname, File.GetAttributes(destPathname) & ~FileAttributes.ReadOnly);
                    }

                    File.Copy(fileToInstallPathName, destPathname, true);
                }
                else
                {
                    Debug.LogError("Missing platform specific file: " + fileToInstall);
                }
            }
        }
    }

    //-------------------------------------------------------------------------
    public void OnPostprocessBuild(BuildReport report)
    {
        // Get the output path, and install the launcher if on a target that supports it
        if (report.summary.platform == BuildTarget.StandaloneWindows || report.summary.platform == BuildTarget.StandaloneWindows64)
        {
            InstallFiles(report);
            
            string pathToEOSBootStrapperTool = GetPathToEOSBin() + "/EOSBootstrapperTool.exe";
            string pathToEOSBootStrapper = GetPathToEOSBin() + "/EOSBootStrapper.exe";
            InstallBootStrapper(report, pathToEOSBootStrapperTool, pathToEOSBootStrapper);
        }
    }
}
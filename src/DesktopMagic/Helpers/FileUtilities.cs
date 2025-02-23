using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopMagic.Helpers;

internal static class FileUtilities
{
    public static bool ExistsOnPath(string fileName)
    {
        return GetFullPath(fileName) is not null;
    }

    public static string? GetFullPath(string fileName)
    {
        if (File.Exists(fileName))
        {
            return Path.GetFullPath(fileName);
        }

        string? values = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrEmpty(values))
        {
            return null;
        }

        foreach (string path in values.Split(Path.PathSeparator))
        {
            string fullPath = Path.Combine(path, fileName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }
        return null;
    }

    public static bool HasAssociatedProgram(string extension)
    {
        return GetAssociatedProgram(extension) is not null;
    }

    public static string? GetAssociatedProgram(string extension)
    {
        uint pcchOut = 0;
        _ = AssocQueryString(AssocF.Verify, AssocStr.Executable, extension, null, null, ref pcchOut);

        StringBuilder pszOut = new StringBuilder((int)pcchOut);
        _ = AssocQueryString(AssocF.Verify, AssocStr.Executable, extension, null, pszOut, ref pcchOut);

        if (File.Exists(pszOut.ToString()))
        {
            return pszOut.ToString();
        }

        return null;
    }

    [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string? pszExtra, [Out] StringBuilder? pszOut, [In][Out] ref uint pcchOut);

    [Flags]
    public enum AssocF
    {
        Init_NoRemapCLSID = 0x1,
        Init_ByExeName = 0x2,
        Open_ByExeName = 0x2,
        Init_DefaultToStar = 0x4,
        Init_DefaultToFolder = 0x8,
        NoUserSettings = 0x10,
        NoTruncate = 0x20,
        Verify = 0x40,
        RemapRunDll = 0x80,
        NoFixUps = 0x100,
        IgnoreBaseClass = 0x200
    }

    public enum AssocStr
    {
        Command = 1,
        Executable,
        FriendlyDocName,
        FriendlyAppName,
        NoOpen,
        ShellNewValue,
        DDECommand,
        DDEIfExec,
        DDEApplication,
        DDETopic
    }
}
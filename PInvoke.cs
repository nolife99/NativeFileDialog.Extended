namespace NativeFileDialog.Extended;

using System.Runtime.InteropServices;

internal static unsafe partial class PInvoke
{
    public enum Result
    {
        NFD_ERROR, NFD_OKAY, NFD_CANCEL
    }

    const string LibName = "nfd";

    [LibraryImport(LibName)] public static partial Result NFD_Init();
    [LibraryImport(LibName)] public static partial void NFD_Quit();

    [LibraryImport(LibName)]
    public static partial Result NFD_OpenDialogU8(out nint outPath, void* filterList, int filterCount, byte* defaultPath);

    [LibraryImport(LibName)]
    public static partial Result NFD_OpenDialogN(out nint outPath, void* filterList, int filterCount, char* defaultPath);

    [LibraryImport(LibName)] public static partial Result NFD_OpenDialogMultipleU8(out nint outPaths,
        FilterU8* filterList,
        int filterCount,
        byte* defaultPath);

    [LibraryImport(LibName)] public static partial Result NFD_OpenDialogMultipleN(out nint outPaths,
        FilterN* filterList,
        int filterCount,
        char* defaultPath);

    [LibraryImport(LibName)] public static partial Result NFD_PathSet_GetCount(nint pathSet, out int count);
    [LibraryImport(LibName)] public static partial Result NFD_PathSet_GetPathU8(nint pathSet, int index, out nint outPath);
    [LibraryImport(LibName)] public static partial Result NFD_PathSet_GetPathN(nint pathSet, int index, out nint outPath);

    [LibraryImport(LibName)] public static partial void NFD_PathSet_FreePathU8(nint filePath);
    [LibraryImport(LibName)] public static partial void NFD_PathSet_FreePathN(nint filePath);

    [LibraryImport(LibName)] public static partial Result NFD_SaveDialogU8(out nint outPath,
        FilterU8* filterList,
        int filterCount,
        byte* defaultPath,
        byte* defaultName);

    [LibraryImport(LibName)] public static partial Result NFD_SaveDialogN(out nint outPath,
        FilterN* filterList,
        int filterCount,
        char* defaultPath,
        char* defaultName);

    [LibraryImport(LibName)] public static partial Result NFD_PickFolderU8(out nint outPath, byte* defaultPath);
    [LibraryImport(LibName)] public static partial Result NFD_PickFolderN(out nint outPath, char* defaultPath);

    [LibraryImport(LibName)] public static partial nint NFD_GetError();
    [LibraryImport(LibName)] public static partial void NFD_ClearError();

    [LibraryImport(LibName)] public static partial void NFD_FreePathN(nint filePath);
    [LibraryImport(LibName)] public static partial void NFD_FreePathU8(nint filePath);

    public struct FilterU8
    {
        public byte* Name;
        public byte* Spec;
    }

    public struct FilterN
    {
        public char* Name;
        public char* Spec;
    }
}
namespace NfdExt;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
///     Native file dialog extended wrapper
/// </summary>
public static unsafe class NFD
{
    static string GetError()
    {
        var error = PInvoke.NFD_GetError();
        PInvoke.NFD_ClearError();
        return Marshal.PtrToStringAnsi(error);
    }

    /// <summary>
    ///     Opens a file picker dialog with extesion filters
    /// </summary>
    /// <param name="defaultPath">Default Path</param>
    /// <param name="filterList">Filter List</param>
    /// <returns>Path to file, can be empty</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string OpenDialog(ReadOnlySpan<char> defaultPath, IEnumerable<KeyValuePair<string, string>> filterList)
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            OpenDialogN(defaultPath, filterList) :
            OpenDialogU8(defaultPath, filterList);

    /// <summary>
    ///     Opens a file picker dialog
    /// </summary>
    /// <param name="defaultPath">Default Path</param>
    /// <returns>Path to file, can be empty</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string OpenDialog(ReadOnlySpan<char> defaultPath) => OpenDialog(defaultPath, []);

    /// <summary>
    ///     Opens a file picker dialog for multiple files with extension filters
    /// </summary>
    /// <param name="defaultPath">Default Path</param>
    /// <param name="filterList">Filter List</param>
    /// <returns>Paths to files, can be empty</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] OpenDialogMultiple(ReadOnlySpan<char> defaultPath, IEnumerable<KeyValuePair<string, string>> filterList)
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            OpenDialogMultipleN(defaultPath, filterList) :
            OpenDialogMultipleU8(defaultPath, filterList);

    /// <summary>
    ///     Opens a file picker dialog for multiple files
    /// </summary>
    /// <param name="defaultPath">Default Path</param>
    /// <returns>Paths to files, can be empty</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] OpenDialogMultiple(ReadOnlySpan<char> defaultPath)
        => OpenDialogMultiple(defaultPath, []);

    /// <summary>
    ///     Opens a file save dialog with extension filters
    /// </summary>
    /// <param name="defaultPath">Default Path</param>
    /// <param name="defaultName">Default Name</param>
    /// <param name="filterList">Filter List</param>
    /// <returns>Path to file, can be empty</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string SaveDialog(ReadOnlySpan<char> defaultPath, ReadOnlySpan<char> defaultName, IEnumerable<KeyValuePair<string, string>> filterList)
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            SaveDialogN(defaultPath, defaultName, filterList) :
            SaveDialogU8(defaultPath, defaultName, filterList);

    /// <summary>
    ///     Opens a file save dialog
    /// </summary>
    /// <param name="defaultPath">Default Path</param>
    /// <param name="defaultName">Default Name</param>
    /// =
    /// <returns>Path to file, can be empty</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string SaveDialog(ReadOnlySpan<char> defaultPath, ReadOnlySpan<char> defaultName)
        => SaveDialog(defaultPath, defaultName, []);

    /// <summary>
    ///     Opens a folder picker dialog
    /// </summary>
    /// <param name="defaultPath">Default Path</param>
    /// <returns>Path to folder, can be empty</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string PickFolder(ReadOnlySpan<char> defaultPath) => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
        PickFolderN(defaultPath) :
        PickFolderU8(defaultPath);

    static string PickFolderN(ReadOnlySpan<char> defaultPath)
    {
        PInvoke.NFD_Init().ThrowOnError();
        try
        {
            fixed (char* ptr = defaultPath)
            {
                PInvoke.NFD_PickFolderN(out var path, ptr).ThrowOnError();
                var str = Marshal.PtrToStringUni(path);
                PInvoke.NFD_FreePathN(path);

                return str;
            }
        }
        finally
        {
            PInvoke.NFD_Quit();
        }
    }

    static string PickFolderU8(ReadOnlySpan<char> defaultPath)
    {
        PInvoke.NFD_Init().ThrowOnError();
        try
        {
            Span<byte> defaultPathUtf8 = stackalloc byte[Encoding.UTF8.GetByteCount(defaultPath)];
            Encoding.UTF8.GetBytes(defaultPath, defaultPathUtf8);

            fixed (byte* ptr = defaultPathUtf8)
            {
                PInvoke.NFD_PickFolderU8(out var path, ptr).ThrowOnError();
                var str = Marshal.PtrToStringUTF8(path);
                PInvoke.NFD_FreePathU8(path);

                return str;
            }
        }
        finally
        {
            PInvoke.NFD_Quit();
        }
    }

    static string SaveDialogN(ReadOnlySpan<char> defaultPath, ReadOnlySpan<char> defaultName, IEnumerable<KeyValuePair<string, string>> filterList)
    {
        PInvoke.NFD_Init().ThrowOnError();

        var list = filterList.ToArray();
        var filters = stackalloc PInvoke.FilterN[list.Length];
        Span<nint> allocLists = stackalloc nint[list.Length * 2];

        list.ToFilterListN(filters, allocLists);

        try
        {
            fixed (char* defaultPathPtr = defaultPath)
            fixed (char* defaultNamePtr = defaultName)
            {
                PInvoke.NFD_SaveDialogN(out var path, filters, list.Length, defaultPathPtr, defaultNamePtr).ThrowOnError();

                var str = Marshal.PtrToStringUni(path);
                PInvoke.NFD_FreePathN(path);

                return str;
            }
        }
        finally
        {
            foreach (var ptr in allocLists) Marshal.FreeCoTaskMem(ptr);
            PInvoke.NFD_Quit();
        }
    }

    static string SaveDialogU8(ReadOnlySpan<char> defaultPath, ReadOnlySpan<char> defaultName, IEnumerable<KeyValuePair<string, string>> filterList)
    {
        PInvoke.NFD_Init().ThrowOnError();

        var list = filterList.ToArray();
        var filters = stackalloc PInvoke.FilterU8[list.Length];
        Span<nint> allocLists = stackalloc nint[list.Length * 2];

        list.ToFilterListU8(filters, allocLists);

        try
        {
            Span<byte> defaultPathUtf8 = stackalloc byte[Encoding.UTF8.GetByteCount(defaultPath)];
            Encoding.UTF8.GetBytes(defaultPath, defaultPathUtf8);

            Span<byte> defaultNameUtf8 = stackalloc byte[Encoding.UTF8.GetByteCount(defaultName)];
            Encoding.UTF8.GetBytes(defaultName, defaultNameUtf8);

            fixed (byte* defaultPathPtr = defaultPathUtf8)
            fixed (byte* defaultNamePtr = defaultNameUtf8)
            {
                PInvoke.NFD_SaveDialogU8(out var path, filters, list.Length, defaultPathPtr, defaultNamePtr).ThrowOnError();

                var str = Marshal.PtrToStringUTF8(path);
                PInvoke.NFD_FreePathU8(path);

                return str;
            }
        }
        finally
        {
            foreach (var ptr in allocLists) Marshal.FreeCoTaskMem(ptr);
            PInvoke.NFD_Quit();
        }
    }

    static string[] OpenDialogMultipleN(ReadOnlySpan<char> defaultPath, IEnumerable<KeyValuePair<string, string>> filterList)
    {
        PInvoke.NFD_Init().ThrowOnError();

        var list = filterList.ToArray();
        var filters = stackalloc PInvoke.FilterN[list.Length];
        Span<nint> allocLists = stackalloc nint[list.Length * 2];

        list.ToFilterListN(filters, allocLists);

        try
        {
            fixed (char* defaultPathPtr = defaultPath)
            {
                PInvoke.NFD_OpenDialogMultipleN(out var ptr, filters, list.Length, defaultPathPtr).ThrowOnError();
                PInvoke.NFD_PathSet_GetCount(ptr, out var count);

                var array = new string[count];
                for (var i = 0; i < count; ++i)
                {
                    PInvoke.NFD_PathSet_GetPathN(ptr, i, out var path);
                    array[i] = Marshal.PtrToStringUni(path);
                    PInvoke.NFD_PathSet_FreePathN(path);
                }

                PInvoke.NFD_FreePathN(ptr);

                return array;
            }
        }
        finally
        {
            foreach (var ptr in allocLists) Marshal.FreeCoTaskMem(ptr);
            PInvoke.NFD_Quit();
        }
    }

    static string[] OpenDialogMultipleU8(ReadOnlySpan<char> defaultPath, IEnumerable<KeyValuePair<string, string>> filterList)
    {
        PInvoke.NFD_Init().ThrowOnError();

        var list = filterList.ToArray();
        var filters = stackalloc PInvoke.FilterU8[list.Length];
        Span<nint> allocLists = stackalloc nint[list.Length * 2];

        list.ToFilterListU8(filters, allocLists);

        try
        {
            Span<byte> defaultPathUtf8 = stackalloc byte[Encoding.UTF8.GetByteCount(defaultPath)];
            Encoding.UTF8.GetBytes(defaultPath, defaultPathUtf8);

            fixed (byte* defaultPathPtr = defaultPathUtf8)
            {
                PInvoke.NFD_OpenDialogMultipleU8(out var ptr, filters, list.Length, defaultPathPtr).ThrowOnError();
                PInvoke.NFD_PathSet_GetCount(ptr, out var count);

                var array = new string[count];
                for (var i = 0; i < count; ++i)
                {
                    PInvoke.NFD_PathSet_GetPathU8(ptr, i, out var path);
                    array[i] = Marshal.PtrToStringUTF8(path);
                    PInvoke.NFD_PathSet_FreePathU8(path);
                }

                PInvoke.NFD_FreePathU8(ptr);

                return array;
            }
        }
        finally
        {
            foreach (var ptr in allocLists) Marshal.FreeCoTaskMem(ptr);
            PInvoke.NFD_Quit();
        }
    }

    static string OpenDialogN(ReadOnlySpan<char> defaultPath, IEnumerable<KeyValuePair<string, string>> filterList)
    {
        PInvoke.NFD_Init().ThrowOnError();

        var list = filterList.ToArray();
        var filters = stackalloc PInvoke.FilterN[list.Length];
        Span<nint> allocLists = stackalloc nint[list.Length * 2];

        list.ToFilterListN(filters, allocLists);

        try
        {
            fixed (char* defaultPathPtr = defaultPath)
            {
                PInvoke.NFD_OpenDialogN(out var path, filters, list.Length, defaultPathPtr).ThrowOnError();
                var str = Marshal.PtrToStringUni(path);
                PInvoke.NFD_FreePathN(path);

                return str;
            }
        }
        finally
        {
            foreach (var ptr in allocLists) Marshal.FreeCoTaskMem(ptr);
            PInvoke.NFD_Quit();
        }
    }

    static string OpenDialogU8(ReadOnlySpan<char> defaultPath, IEnumerable<KeyValuePair<string, string>> filterList)
    {
        PInvoke.NFD_Init().ThrowOnError();

        var list = filterList.ToArray();
        var filters = stackalloc PInvoke.FilterU8[list.Length];
        Span<nint> allocLists = stackalloc nint[list.Length * 2];

        list.ToFilterListU8(filters, allocLists);

        try
        {
            Span<byte> defaultPathUtf8 = stackalloc byte[Encoding.UTF8.GetByteCount(defaultPath)];
            Encoding.UTF8.GetBytes(defaultPath, defaultPathUtf8);

            fixed (byte* defaultPathPtr = defaultPathUtf8)
            {
                PInvoke.NFD_OpenDialogU8(out var path, filters, list.Length, defaultPathPtr).ThrowOnError();
                var str = Marshal.PtrToStringUTF8(path);
                PInvoke.NFD_FreePathU8(path);

                return str;
            }
        }
        finally
        {
            foreach (var ptr in allocLists) Marshal.FreeCoTaskMem(ptr);
            PInvoke.NFD_Quit();
        }
    }

    static void ThrowOnError(this PInvoke.Result result)
    {
        if (result is PInvoke.Result.NFD_ERROR) throw new ExternalException(GetError());
    }

    static void ToFilterListU8(this KeyValuePair<string, string>[] dict, PInvoke.FilterU8* filters, Span<nint> allocs)
    {
        for (var i = 0; i < dict.Length; ++i)
        {
            var name = Marshal.StringToCoTaskMemUTF8(dict[i].Key);
            var spec = Marshal.StringToCoTaskMemUTF8(dict[i].Value);

            allocs[i * 2] = name;
            allocs[i * 2 + 1] = spec;

            filters[i] = new() { Name = (byte*)name, Spec = (byte*)spec };
        }
    }

    static void ToFilterListN(this KeyValuePair<string, string>[] dict, PInvoke.FilterN* filters, Span<nint> allocs)
    {
        for (var i = 0; i < dict.Length; i++)
        {
            var name = Marshal.StringToCoTaskMemUni(dict[i].Key);
            var spec = Marshal.StringToCoTaskMemUni(dict[i].Value);

            allocs[i * 2] = name;
            allocs[i * 2 + 1] = spec;

            filters[i] = new() { Name = (char*)name, Spec = (char*)spec };
        }
    }
}
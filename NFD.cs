namespace NfdExt;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
///     Wraps the native file dialog library.
/// </summary>
public static unsafe class NFD
{
    static string GetError()
    {
        var error = PInvoke.NFD_GetError();
        PInvoke.NFD_ClearError();
        return Marshal.PtrToStringUTF8(error);
    }

    /// <summary>
    ///     Displays a standard dialog box that prompts the user to open a file.
    /// </summary>
    /// <param name="defaultPath">The initial path or directory displayed by the file dialog box.</param>
    /// <param name="filterList">The file name filter string.</param>
    /// <returns>A string containing the file name selected in the file dialog box.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string OpenDialog(ReadOnlySpan<char> defaultPath, IReadOnlyCollection<KeyValuePair<string, string>> filterList)
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            OpenDialogN(defaultPath, filterList) :
            OpenDialogU8(defaultPath, filterList);

    /// <summary>
    ///     Displays a standard dialog box that prompts the user to open a file.
    /// </summary>
    /// <param name="defaultPath">The initial path or directory displayed by the file dialog box.</param>
    /// <returns>A string containing the file name selected in the file dialog box.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string OpenDialog(ReadOnlySpan<char> defaultPath) => OpenDialog(defaultPath, []);

    /// <summary>
    ///     Displays a standard dialog box that prompts the user to open multiple files.
    /// </summary>
    /// <param name="defaultPath">The initial path or directory displayed by the file dialog box.</param>
    /// <param name="filterList">The file name filter string.</param>
    /// <returns>A string array containing the file names selected in the file dialog box.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] OpenDialogMultiple(ReadOnlySpan<char> defaultPath,
        IReadOnlyCollection<KeyValuePair<string, string>> filterList) => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
        OpenDialogMultipleN(defaultPath, filterList) :
        OpenDialogMultipleU8(defaultPath, filterList);

    /// <summary>
    ///     Displays a standard dialog box that prompts the user to open multiple files.
    /// </summary>
    /// <param name="defaultPath">The initial path or directory displayed by the file dialog box.</param>
    /// <returns>A string array containing the file names selected in the file dialog box.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] OpenDialogMultiple(ReadOnlySpan<char> defaultPath) => OpenDialogMultiple(defaultPath, []);

    /// <summary>
    ///     Prompts the user to select a location for saving a file.
    /// </summary>
    /// <param name="defaultPath">The initial path or directory displayed by the file dialog box.</param>
    /// <param name="defaultName">The initial file name to be saved.</param>
    /// <param name="filterList">The file name filter string.</param>
    /// <returns>A string containing the file name selected in the file dialog box.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string SaveDialog(ReadOnlySpan<char> defaultPath,
        ReadOnlySpan<char> defaultName,
        IReadOnlyCollection<KeyValuePair<string, string>> filterList) => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
        SaveDialogN(defaultPath, defaultName, filterList) :
        SaveDialogU8(defaultPath, defaultName, filterList);

    /// <summary>
    ///     Prompts the user to select a location for saving a file.
    /// </summary>
    /// <param name="defaultPath">The initial path or directory displayed by the file dialog box.</param>
    /// <param name="defaultName">The initial file name to be saved.</param>
    /// <returns>A string containing the file name selected in the file dialog box.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string SaveDialog(ReadOnlySpan<char> defaultPath, ReadOnlySpan<char> defaultName)
        => SaveDialog(defaultPath, defaultName, []);

    /// <summary>
    ///     Prompts the user to select a folder.
    /// </summary>
    /// <param name="defaultPath">The initial path or directory displayed by the file dialog box.</param>
    /// <returns>A string containing the file name selected in the file dialog box.</returns>
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

            PInvoke.NFD_PickFolderU8(out var path, (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(defaultPathUtf8)))
                .ThrowOnError();

            var str = Marshal.PtrToStringUTF8(path);
            PInvoke.NFD_FreePathU8(path);

            return str;
        }
        finally
        {
            PInvoke.NFD_Quit();
        }
    }

    static string SaveDialogN(ReadOnlySpan<char> defaultPath,
        ReadOnlySpan<char> defaultName,
        IReadOnlyCollection<KeyValuePair<string, string>> filterList)
    {
        PInvoke.NFD_Init().ThrowOnError();

        var filters = stackalloc PInvoke.FilterN[filterList.Count];
        Span<nint> allocLists = stackalloc nint[filterList.Count * 2];

        filterList.ToFilterListN(filters, allocLists);

        try
        {
            fixed (char* defaultPathPtr = defaultPath)
            fixed (char* defaultNamePtr = defaultName)
            {
                PInvoke.NFD_SaveDialogN(out var path, filters, filterList.Count, defaultPathPtr, defaultNamePtr).ThrowOnError();

                var str = Marshal.PtrToStringUni(path);
                PInvoke.NFD_FreePathN(path);

                return str;
            }
        }
        finally
        {
            foreach (ref var ptr in allocLists) Marshal.FreeCoTaskMem(ptr);
            PInvoke.NFD_Quit();
        }
    }

    static string SaveDialogU8(ReadOnlySpan<char> defaultPath,
        ReadOnlySpan<char> defaultName,
        IReadOnlyCollection<KeyValuePair<string, string>> filterList)
    {
        PInvoke.NFD_Init().ThrowOnError();

        var filters = stackalloc PInvoke.FilterU8[filterList.Count];
        Span<nint> allocLists = stackalloc nint[filterList.Count * 2];

        filterList.ToFilterListU8(filters, allocLists);

        try
        {
            Span<byte> defaultPathUtf8 = stackalloc byte[Encoding.UTF8.GetByteCount(defaultPath)];
            Encoding.UTF8.GetBytes(defaultPath, defaultPathUtf8);

            Span<byte> defaultNameUtf8 = stackalloc byte[Encoding.UTF8.GetByteCount(defaultName)];
            Encoding.UTF8.GetBytes(defaultName, defaultNameUtf8);

            PInvoke.NFD_SaveDialogU8(out var path,
                    filters,
                    filterList.Count,
                    (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(defaultPathUtf8)),
                    (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(defaultNameUtf8)))
                .ThrowOnError();

            var str = Marshal.PtrToStringUTF8(path);
            PInvoke.NFD_FreePathU8(path);

            return str;
        }
        finally
        {
            foreach (ref var ptr in allocLists) Marshal.FreeCoTaskMem(ptr);
            PInvoke.NFD_Quit();
        }
    }

    static string[] OpenDialogMultipleN(ReadOnlySpan<char> defaultPath,
        IReadOnlyCollection<KeyValuePair<string, string>> filterList)
    {
        PInvoke.NFD_Init().ThrowOnError();

        var filters = stackalloc PInvoke.FilterN[filterList.Count];
        Span<nint> allocLists = stackalloc nint[filterList.Count * 2];

        filterList.ToFilterListN(filters, allocLists);

        try
        {
            fixed (char* defaultPathPtr = defaultPath)
            {
                PInvoke.NFD_OpenDialogMultipleN(out var ptr, filters, filterList.Count, defaultPathPtr).ThrowOnError();
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
            foreach (ref var ptr in allocLists) Marshal.FreeCoTaskMem(ptr);
            PInvoke.NFD_Quit();
        }
    }

    static string[] OpenDialogMultipleU8(ReadOnlySpan<char> defaultPath,
        IReadOnlyCollection<KeyValuePair<string, string>> filterList)
    {
        PInvoke.NFD_Init().ThrowOnError();

        var filters = stackalloc PInvoke.FilterU8[filterList.Count];
        Span<nint> allocLists = stackalloc nint[filterList.Count * 2];

        filterList.ToFilterListU8(filters, allocLists);

        try
        {
            Span<byte> defaultPathUtf8 = stackalloc byte[Encoding.UTF8.GetByteCount(defaultPath)];
            Encoding.UTF8.GetBytes(defaultPath, defaultPathUtf8);

            PInvoke.NFD_OpenDialogMultipleU8(out var ptr,
                    filters,
                    filterList.Count,
                    (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(defaultPathUtf8)))
                .ThrowOnError();

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
        finally
        {
            foreach (ref var ptr in allocLists) Marshal.FreeCoTaskMem(ptr);
            PInvoke.NFD_Quit();
        }
    }

    static string OpenDialogN(ReadOnlySpan<char> defaultPath, IReadOnlyCollection<KeyValuePair<string, string>> filterList)
    {
        PInvoke.NFD_Init().ThrowOnError();

        var filters = stackalloc PInvoke.FilterN[filterList.Count];
        Span<nint> allocLists = stackalloc nint[filterList.Count * 2];

        filterList.ToFilterListN(filters, allocLists);

        try
        {
            fixed (char* defaultPathPtr = defaultPath)
            {
                PInvoke.NFD_OpenDialogN(out var path, filters, filterList.Count, defaultPathPtr).ThrowOnError();
                var str = Marshal.PtrToStringUni(path);
                PInvoke.NFD_FreePathN(path);

                return str;
            }
        }
        finally
        {
            foreach (ref var ptr in allocLists) Marshal.FreeCoTaskMem(ptr);
            PInvoke.NFD_Quit();
        }
    }

    static string OpenDialogU8(ReadOnlySpan<char> defaultPath, IReadOnlyCollection<KeyValuePair<string, string>> filterList)
    {
        PInvoke.NFD_Init().ThrowOnError();

        var filters = stackalloc PInvoke.FilterU8[filterList.Count];
        Span<nint> allocLists = stackalloc nint[filterList.Count * 2];

        filterList.ToFilterListU8(filters, allocLists);

        try
        {
            Span<byte> defaultPathUtf8 = stackalloc byte[Encoding.UTF8.GetByteCount(defaultPath)];
            Encoding.UTF8.GetBytes(defaultPath, defaultPathUtf8);

            fixed (byte* defaultPathPtr = defaultPathUtf8)
            {
                PInvoke.NFD_OpenDialogU8(out var path, filters, filterList.Count, defaultPathPtr).ThrowOnError();
                var str = Marshal.PtrToStringUTF8(path);
                PInvoke.NFD_FreePathU8(path);

                return str;
            }
        }
        finally
        {
            foreach (ref var ptr in allocLists) Marshal.FreeCoTaskMem(ptr);
            PInvoke.NFD_Quit();
        }
    }

    static void ThrowOnError(this PInvoke.Result result)
    {
        if (result is PInvoke.Result.NFD_ERROR) throw new ExternalException(GetError());
    }

    static void ToFilterListU8(this IReadOnlyCollection<KeyValuePair<string, string>> dict,
        PInvoke.FilterU8* filters,
        Span<nint> allocs)
    {
        var i = 0;
        foreach (var item in dict)
        {
            var name = Marshal.StringToCoTaskMemUTF8(item.Key);
            var spec = Marshal.StringToCoTaskMemUTF8(item.Value);

            allocs[i * 2] = name;
            allocs[i * 2 + 1] = spec;

            filters[i] = new() { Name = (byte*)name, Spec = (byte*)spec };

            ++i;
        }
    }

    static void ToFilterListN(this IReadOnlyCollection<KeyValuePair<string, string>> dict,
        PInvoke.FilterN* filters,
        Span<nint> allocs)
    {
        var i = 0;
        foreach (var item in dict)
        {
            var name = Marshal.StringToCoTaskMemUni(item.Key);
            var spec = Marshal.StringToCoTaskMemUni(item.Value);

            allocs[i * 2] = name;
            allocs[i * 2 + 1] = spec;

            filters[i] = new() { Name = (char*)name, Spec = (char*)spec };

            ++i;
        }
    }
}
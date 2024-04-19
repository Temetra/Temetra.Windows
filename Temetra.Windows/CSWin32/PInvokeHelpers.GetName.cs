using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace Temetra.Windows;

// PInvoke "get name" methods have similar patterns:
// In: Handle to process
// In: Pointer to buffer
// In: Size of buffer
// Out: Number of chars written to buffer

internal static partial class PInvokeHelpers
{
    public static unsafe string GetFullProcessName(uint processId)
	{
		var flags = PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION;

		static bool fn(HANDLE hProcess, PWSTR lpExeName, uint* lpdwSize)
		{
            return PInvoke.QueryFullProcessImageName(hProcess, PROCESS_NAME_FORMAT.PROCESS_NAME_WIN32, lpExeName, lpdwSize);
        }

        return GetName(processId, flags, fn);
	}

    public static unsafe string GetModuleBaseName(uint processId)
	{
		var flags = PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION | PROCESS_ACCESS_RIGHTS.PROCESS_VM_READ;

		static bool fn(HANDLE hProcess, PWSTR lpExeName, uint* lpdwSize)
		{
            *lpdwSize = PInvoke.GetModuleBaseName(hProcess, HMODULE.Null, lpExeName, *lpdwSize);
			return *lpdwSize > 0;
        }

        return GetName(processId, flags, fn);
	}

    public static unsafe string GetPackageFullName(uint processId)
	{
        if (processId == 0)
        {
            return string.Empty;
        }

        var flags = PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION;

		static bool fn(HANDLE hProcess, PWSTR lpExeName, uint* lpdwSize)
		{
            var result = PInvoke.GetPackageFullName(hProcess, lpdwSize, lpExeName);

            // GetPackageFullName includes null terminator
            if (result == WIN32_ERROR.ERROR_SUCCESS)
			{
                *lpdwSize = (uint)*lpdwSize - 1;
            }

            return result == WIN32_ERROR.ERROR_SUCCESS;
        }

        return GetName(processId, flags, fn);
	}

	private unsafe delegate bool PInvokeGetNameDelegate(HANDLE hProcess, PWSTR lpExeName, uint* lpdwSize);

	private static unsafe string GetName(uint processId, PROCESS_ACCESS_RIGHTS flags, PInvokeGetNameDelegate pInvokeGetName)
	{
		const uint MAX_BUFFER_LEN = 32767; 
		
		string result = string.Empty;

		// Open handle to process using access flags
		var hProcess = PInvoke.OpenProcess(flags, false, processId);

		if (!hProcess.IsNull)
		{
			// Prepare span to be used as PWSTR
			Span<char> buffer = stackalloc char[(int)MAX_BUFFER_LEN];

			// Pass size of span to pInvokeGetName
			uint len = MAX_BUFFER_LEN;

			// Pin address of span
			fixed (char* pBuffer = buffer)
			{
				// Call PInvoke method
				if (!pInvokeGetName(hProcess, pBuffer, &len))
				{
                    // Return an empty string if PInvoke method fails
                    len = 0;
				}
			}

			// Close the handle
			PInvoke.CloseHandle(hProcess);

			// Get slice of span and convert to string
			if (len > 0)
			{
                result = buffer[..(int)len].ToString();
			}
		}

		return result;
	}
}

Imports System.Runtime.ConstrainedExecution
Imports System.Runtime.InteropServices
Imports System.Security
Imports System.Text
Public Class Mem

    Public Shared m_BaseAddress As Integer
    Public Shared m_Process As Process
    Public Shared m_MainWindowsHandle As IntPtr

    Private Const PROCESS_ALL_ACCESS = &H1F0FFF
    Private Const MEM_COMMIT = &H1000
    Private Const MEM_RELEASE = &H8000
    Private Const PAGE_READWRITE = &H4

    <DllImport("kernel32.dll")>
    Public Shared Function OpenProcess(ByVal dwDesiredAccess As Integer, ByVal bInheritHandle As Boolean, ByVal dwProcessId As Integer) As IntPtr
    End Function

    <DllImport("kernel32.dll")>
    Public Shared Function ReadProcessMemory(ByVal hProcess As Integer, ByVal lpBaseAddress As Integer, ByVal buffer As Byte(), ByVal size As Integer, ByRef lpNumberOfBytesRead As Integer) As Boolean
    End Function

    <DllImport("kernel32.dll")>
    Public Shared Function WriteProcessMemory(ByVal hProcess As Integer, ByVal lpBaseAddress As Integer, ByVal buffer As Byte(), ByVal size As Integer, ByRef lpNumberOfBytesWritten As Integer) As Boolean
    End Function

    Private Declare Function VirtualAllocEx Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpAddress As IntPtr, ByVal dwSize As UInteger, ByVal flAllocationType As UInteger, ByVal flProtect As UInteger) As Integer
    Private Declare Function VirtualFreeEx Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpAddress As IntPtr, ByVal dwSize As UInteger, ByVal dwFreeType As Integer) As Boolean
    Private Declare Function GetProcAddress Lib "kernel32" (ByVal hModule As IntPtr, ByVal lpProcName As String) As IntPtr
    Private Declare Function GetModuleHandle Lib "kernel32" Alias "GetModuleHandleA" (ByVal lpModuleName As String) As IntPtr
    Private Declare Function CreateRemoteThread Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpThreadAttributes As IntPtr, ByVal dwStackSize As UInteger, ByVal lpStartAddress As IntPtr, ByVal lpParameter As IntPtr, ByVal dwCreationFlags As Integer, ByVal lpThreadId As IntPtr) As IntPtr
    Private Declare Function WaitForSingleObject Lib "kernel32" (ByVal hHandle As IntPtr, ByVal dwMilliseconds As Integer) As Integer
    Private Declare Function CloseHandle Lib "kernel32" (ByVal hObject As IntPtr) As Boolean

    Public Shared Function Initialize(ByVal ProcessName As String, ByRef windowsTitle As String) As Boolean
        If (Process.GetProcessesByName(ProcessName).Length > 0) Then
            m_Process = Process.GetProcessesByName(ProcessName)(0)
            m_BaseAddress = Process.GetProcessesByName(ProcessName)(0).MainModule.BaseAddress.ToInt32
            m_MainWindowsHandle = m_Process.MainWindowHandle

            m_pProcessHandle = OpenProcess(56, False, m_Process.Id)
            windowsTitle = m_Process.MainWindowTitle
            Return True
        End If
        Return False
    End Function

    Public Shared Function Inject(ByVal dllPath As String) As Boolean
        If m_pProcessHandle = 0 Then
            Return False
        End If
        Dim dllBytes As Byte() = Encoding.ASCII.GetBytes(dllPath)
        Dim allocAddress As Integer = VirtualAllocEx(m_pProcessHandle, 0, dllBytes.Length, MEM_COMMIT, PAGE_READWRITE)
        If allocAddress = 0 Then
            Return False
        End If

        Dim kernelMod As Integer = GetModuleHandle("kernel32.dll")
        Dim loadLibAddr = GetProcAddress(kernelMod, "LoadLibraryA")

        If kernelMod = 0 Or loadLibAddr = 0 Then
            Return False
        End If

        WriteProcessMemory(m_pProcessHandle, allocAddress, dllBytes, dllBytes.Length, 0)
        Dim libThread As Integer = CreateRemoteThread(m_pProcessHandle, 0, 0, loadLibAddr, allocAddress, 0, 0)

        If libThread = 0 Then
            Return False
        Else
            WaitForSingleObject(libThread, 5000)
            CloseHandle(libThread)
        End If

        VirtualFreeEx(m_pProcessHandle, allocAddress, dllBytes.Length, MEM_RELEASE)
        'CloseHandle(m_pProcessHandle)

        Return True
    End Function

    Public Shared Function ConvertToFloatArray(ByVal bytes As Byte()) As Single()
        If (bytes.Length Mod 4) > 0 Then
            Throw New ArgumentException
        End If
        Dim numArray As Single() = New Single(CInt(bytes.Length / 4) - 1) {}
        For i As Integer = 0 To numArray.Length - 1
            numArray(i) = BitConverter.ToSingle(bytes, (i * 4))
        Next

        Return numArray
    End Function

    Public Shared Function ReadMatrix(Of T As Structure)(ByVal Adress As Integer, ByVal MatrixSize As Integer) As Single()
        Dim buffer As Byte() = New Byte((Marshal.SizeOf(GetType(T)) * MatrixSize) - 1) {}
        ReadProcessMemory(CInt(m_pProcessHandle), Adress, buffer, buffer.Length, m_iNumberOfBytesRead)
        Return Mem.ConvertToFloatArray(buffer)
    End Function

    Private Shared Function ByteArrayToStructure(Of T As Structure)(ByVal bytes As Byte()) As T
        Dim local As T
        Dim handle As GCHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned)
        Try
            local = DirectCast(Marshal.PtrToStructure(handle.AddrOfPinnedObject, GetType(T)), T)
        Finally
            handle.Free()
        End Try
        Return local
    End Function

    Private Shared Function StructureToByteArray(ByVal obj As Object)
        Dim length As Integer = Marshal.SizeOf(obj)
        Dim dest As Byte() = New Byte(length) {}
        Dim num As IntPtr = Marshal.AllocHGlobal(length)
        Marshal.StructureToPtr(obj, num, True)
        Marshal.Copy(num, dest, 0, length)
        Marshal.FreeHGlobal(num)
        Return dest
    End Function

    Public Shared Function GetEntry(Of T As Structure)(ByVal baseEntry As Integer, ByVal entryId As Integer, Optional ByRef entryAddr As Integer = 0) As T
        entryAddr = ReadMemory(Of Integer)(baseEntry + (&HC * entryId) + &H8)
        Return ReadMemory(Of T)(entryAddr + &H8)
    End Function

    Public Shared Function GetEntryS(Of T As Structure)(ByVal baseEntry As Integer, ByVal entryId As Integer, Optional ByRef entryAddr As Integer = 0) As T
        entryAddr = ReadMemory(Of Integer)(baseEntry + entryId)
        Return ReadMemory(Of T)(entryAddr + &H8)
    End Function

    Public Shared Function GetEntryAddr(ByVal baseEntry As Integer, ByVal entryId As Integer) As Integer
        Return ReadMemory(Of Integer)(baseEntry + (&HC * entryId) + &H8)
    End Function

    'Public Shared Function GetEntryMeta(ByVal baseEntry As Integer, ByVal entryId As Integer, Optional ByRef entryAddr As Integer = 0) As Integer
    '    entryAddr = ReadMemory(Of Integer)(baseEntry + (&HC * entryId) + &H8)
    '    Return ReadMemory(Of Integer)(entryAddr + &H4)
    'End Function


    Public Shared m_iNumberOfBytesRead As Integer = 0
    Public Shared m_pProcessHandle As IntPtr
    Public Shared Function ReadMemory(Of T As Structure)(ByVal Adress As Integer) As T
        Dim buffer As Byte() = New Byte(Marshal.SizeOf(GetType(T)) - 1) {}
        ReadProcessMemory(m_pProcessHandle.ToInt32, Adress, buffer, buffer.Length, m_iNumberOfBytesRead)
        Return ByteArrayToStructure(Of T)(buffer)
    End Function

    Public Shared Function WriteMemory(Of T As Structure)(ByVal Adress As Integer, ByVal write As Object) As T

        Dim buffer As Byte() = StructureToByteArray(write)
        WriteProcessMemory(m_pProcessHandle.ToInt32, Adress, buffer, buffer.Length, m_iNumberOfBytesRead)
        Return ByteArrayToStructure(Of T)(buffer)
    End Function

    Private Declare Function WriteFloatMemory Lib "kernel32" Alias "WriteProcessMemory" (ByVal hProcess As Integer, ByVal lpBaseAddress As Integer, ByRef lpBuffer As Single, ByVal nSize As Integer, ByRef lpNumberOfBytesWritten As Integer) As Integer

    Public Shared Function WriteFloatMem(ByVal Adress As Integer, ByVal write As Single)
        WriteFloatMemory(m_pProcessHandle.ToInt32, Adress, write, 4, Nothing)
    End Function

    Public Shared buffer As Byte()
    Public Shared Function ReadString(ByVal address As Integer, ByVal _Size As Integer) As String
        'Try
        If _Size > 0 And _Size < 256 Then
            buffer = New Byte(_Size - 1) {}
            ReadProcessMemory(Mem.m_pProcessHandle.ToInt32, address, buffer, _Size, m_iNumberOfBytesRead)
            Return Encoding.ASCII.GetString(buffer)
        End If
        Return ""

        'Catch ex As Exception
        'Console.WriteLine(ex.ToString)
        'End Try

    End Function

    Public Shared Function ReadBytes(ByVal address As Integer, ByVal _Size As Integer) As Byte()
        If _Size > 0 And _Size < 20 Then
            buffer = New Byte(_Size - 1) {}
            ReadProcessMemory(Mem.m_pProcessHandle.ToInt32, address, buffer, _Size, m_iNumberOfBytesRead)
            Return buffer
        End If
        Return New Byte() {}
    End Function

End Class

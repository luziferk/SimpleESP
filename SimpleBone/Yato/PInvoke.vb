Imports System
Imports System.Runtime.InteropServices


Friend NotInheritable Class PInvoke

        Private Sub New()
        End Sub

        Public Shared Function GetRealWindowRect(ByVal hwnd As IntPtr, <System.Runtime.InteropServices.Out()> ByRef rect As RECT) As Integer
            Dim windowRect As New RECT()
            Dim clientRect As New RECT()

            Dim result As Integer = GetWindowRect(hwnd, windowRect)
            If GetClientRect(hwnd, clientRect) = 0 Then
                rect = windowRect
                Return result
            End If

            Dim windowWidth As Integer = windowRect.Right - windowRect.Left
            Dim windowHeight As Integer = windowRect.Bottom - windowRect.Top

            If windowWidth = clientRect.Right AndAlso windowHeight = clientRect.Bottom Then
                rect = windowRect
                Return result
            End If

            Dim dif_x As Integer = If(windowWidth > clientRect.Right, windowWidth - clientRect.Right, clientRect.Right - windowWidth)
            Dim dif_y As Integer = If(windowHeight > clientRect.Bottom, windowHeight - clientRect.Bottom, clientRect.Bottom - windowHeight)

            dif_x \= 2
            dif_y \= 2

            windowRect.Left += dif_x
            windowRect.Top += dif_y

            windowRect.Right -= dif_x
            windowRect.Bottom -= dif_y

            rect = windowRect
            Return result
        End Function

#Region "User32"

        <UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet:=CharSet.Unicode)>
        Public Delegate Function CreateWindowEx_t(ByVal dwExStyle As UInteger, ByVal lpClassName As String, ByVal lpWindowName As String, ByVal dwStyle As UInteger, ByVal x As Integer, ByVal y As Integer, ByVal nWidth As Integer, ByVal nHeight As Integer, ByVal hWndParent As IntPtr, ByVal hMenu As IntPtr, ByVal hInstance As IntPtr, ByVal lpParam As IntPtr) As IntPtr
        Public Shared CreateWindowEx As CreateWindowEx_t = WinApi.GetMethod(Of CreateWindowEx_t)("user32.dll", "CreateWindowExW")

        <UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet:=CharSet.Unicode)>
        Public Delegate Function RegisterClassEx_t(ByRef wndclassex As WNDCLASSEX) As UShort
        Public Shared RegisterClassEx As RegisterClassEx_t = WinApi.GetMethod(Of RegisterClassEx_t)("user32.dll", "RegisterClassExW")

        <UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet:=CharSet.Unicode)>
        Public Delegate Function UnregisterClass_t(ByVal lpClassName As String, ByVal hInstance As IntPtr) As Integer
        Public Shared UnregisterClass As UnregisterClass_t = WinApi.GetMethod(Of UnregisterClass_t)("user32.dll", "UnregisterClassW")

        Public Delegate Function SetLayeredWindowAttributes_t(ByVal hwnd As IntPtr, ByVal crKey As UInteger, ByVal bAlpha As Byte, ByVal dwFlags As UInteger) As Boolean
        Public Shared SetLayeredWindowAttributes As SetLayeredWindowAttributes_t = WinApi.GetMethod(Of SetLayeredWindowAttributes_t)("user32.dll", "SetLayeredWindowAttributes")

        Public Delegate Function TranslateMessage_t(ByRef msg As Message) As Integer
        Public Shared TranslateMessage As TranslateMessage_t = WinApi.GetMethod(Of TranslateMessage_t)("user32.dll", "TranslateMessage")

        <UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet:=CharSet.Unicode)>
        Public Delegate Function PeekMessageW_t(ByRef msg As Message, ByVal hwnd As IntPtr, ByVal filterMin As UInteger, ByVal filterMax As UInteger, ByVal removeMsg As UInteger) As Integer
        Public Shared PeekMessageW As PeekMessageW_t = WinApi.GetMethod(Of PeekMessageW_t)("user32.dll", "PeekMessageW")

        <UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet:=CharSet.Unicode)>
        Public Delegate Function DispatchMessage_t(ByRef msg As Message) As Integer
        Public Shared DispatchMessage As DispatchMessage_t = WinApi.GetMethod(Of DispatchMessage_t)("user32.dll", "DispatchMessageW")

        Public Delegate Function MoveWindow_t(ByVal hwnd As IntPtr, ByVal x As Integer, ByVal y As Integer, ByVal width As Integer, ByVal height As Integer, ByVal repaint As Integer) As Integer
        Public Shared MoveWindow As MoveWindow_t = WinApi.GetMethod(Of MoveWindow_t)("user32.dll", "MoveWindow")

        <UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet:=CharSet.Unicode)>
        Public Delegate Function DefWindowProc_t(ByVal hwnd As IntPtr, ByVal msg As WindowsMessage, ByVal wparam As IntPtr, ByVal lparam As IntPtr) As IntPtr
        Public Shared DefWindowProc As DefWindowProc_t = WinApi.GetMethod(Of DefWindowProc_t)("user32.dll", "DefWindowProcW")

        <UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet:=CharSet.Unicode)>
        Public Delegate Function SendMessage_t(ByVal hwnd As IntPtr, ByVal msg As WindowsMessage, ByVal wparam As IntPtr, ByVal lparam As IntPtr) As Integer
        Public Shared SendMessage As SendMessage_t = WinApi.GetMethod(Of SendMessage_t)("user32.dll", "SendMessageW")

        Public Delegate Function UpdateWindow_t(ByVal hWnd As IntPtr) As Boolean
        Public Shared UpdateWindow As UpdateWindow_t = WinApi.GetMethod(Of UpdateWindow_t)("user32.dll", "UpdateWindow")

        Public Delegate Function DestroyWindow_t(ByVal hwnd As IntPtr) As Integer
        Public Shared DestroyWindow As DestroyWindow_t = WinApi.GetMethod(Of DestroyWindow_t)("user32.dll", "DestroyWindow")

        Public Delegate Function ShowWindow_t(ByVal hWnd As IntPtr, ByVal nCmdShow As UInteger) As Integer
        Public Shared ShowWindow As ShowWindow_t = WinApi.GetMethod(Of ShowWindow_t)("user32.dll", "ShowWindow")

        Public Delegate Function WaitMessage_t() As Integer
        Public Shared WaitMessage As WaitMessage_t = WinApi.GetMethod(Of WaitMessage_t)("user32.dll", "WaitMessage")

        Public Delegate Function GetWindowRect_t(ByVal hwnd As IntPtr, <System.Runtime.InteropServices.Out()> ByRef lpRect As RECT) As Integer
        Public Shared GetWindowRect As GetWindowRect_t = WinApi.GetMethod(Of GetWindowRect_t)("user32.dll", "GetWindowRect")

        Public Delegate Function GetClientRect_t(ByVal hwnd As IntPtr, <System.Runtime.InteropServices.Out()> ByRef lpRect As RECT) As Integer
        Public Shared GetClientRect As GetClientRect_t = WinApi.GetMethod(Of GetClientRect_t)("user32.dll", "GetClientRect")

        Public Delegate Function IsWindowVisible_t(ByVal hwnd As IntPtr) As Integer
        Public Shared IsWindowVisible As IsWindowVisible_t = WinApi.GetMethod(Of IsWindowVisible_t)("user32.dll", "IsWindowVisible")

        Public Delegate Function IsWindow_t(ByVal hwnd As IntPtr) As Integer
        Public Shared IsWindow As IsWindow_t = WinApi.GetMethod(Of IsWindow_t)("user32.dll", "IsWindow")

        Public Delegate Function SetWindowPos_t(ByVal hwnd As IntPtr, ByVal hwndInsertAfter As IntPtr, ByVal x As Integer, ByVal y As Integer, ByVal cx As Integer, ByVal cy As Integer, ByVal flags As UInteger) As Integer
        Public Shared SetWindowPos As SetWindowPos_t = WinApi.GetMethod(Of SetWindowPos_t)("user32.dll", "SetWindowPos")

        Public Delegate Function GetWindow_t(ByVal hwnd As IntPtr, ByVal cmd As UInteger) As IntPtr
        Public Shared GetWindow As GetWindow_t = WinApi.GetMethod(Of GetWindow_t)("user32.dll", "GetWindow")

        Public Delegate Function IsProcessDPIAware_t() As Integer
        Public Shared IsProcessDPIAware As IsProcessDPIAware_t = WinApi.GetMethod(Of IsProcessDPIAware_t)("user32.dll", "IsProcessDPIAware")

#End Region

#Region "DwmApi"

        Public Delegate Sub DwmExtendFrameIntoClientArea_t(ByVal hWnd As IntPtr, ByRef pMargins As MARGIN)
        Public Shared DwmExtendFrameIntoClientArea As DwmExtendFrameIntoClientArea_t = WinApi.GetMethod(Of DwmExtendFrameIntoClientArea_t)("dwmapi.dll", "DwmExtendFrameIntoClientArea")

#End Region

#Region "Enums & Structs"

        <StructLayout(LayoutKind.Sequential)>
        Public Structure Message
            Public Hwnd As IntPtr
            Public Msg As WindowsMessage
            Public lParam As IntPtr
            Public wParam As IntPtr
            Public Time As UInteger
            Public X As Integer
            Public Y As Integer
        End Structure

        Public Enum WindowsMessage As UInteger
            WM_NULL = &H0
            WM_CREATE = &H1
            WM_DESTROY = &H2
            WM_MOVE = &H3
            WM_SIZE = &H5
            WM_ACTIVATE = &H6
            WM_SETFOCUS = &H7
            WM_KILLFOCUS = &H8
            WM_ENABLE = &HA
            WM_SETREDRAW = &HB
            WM_SETTEXT = &HC
            WM_GETTEXT = &HD
            WM_GETTEXTLENGTH = &HE
            WM_PAINT = &HF
            WM_CLOSE = &H10
            WM_QUERYENDSESSION = &H11
            WM_QUERYOPEN = &H13
            WM_ENDSESSION = &H16
            WM_QUIT = &H12
            WM_ERASEBKGND = &H14
            WM_SYSCOLORCHANGE = &H15
            WM_SHOWWINDOW = &H18
            WM_WININICHANGE = &H1A
            WM_SETTINGCHANGE = WM_WININICHANGE
            WM_DEVMODECHANGE = &H1B
            WM_ACTIVATEAPP = &H1C
            WM_FONTCHANGE = &H1D
            WM_TIMECHANGE = &H1E
            WM_CANCELMODE = &H1F
            WM_SETCURSOR = &H20
            WM_MOUSEACTIVATE = &H21
            WM_CHILDACTIVATE = &H22
            WM_QUEUESYNC = &H23
            WM_GETMINMAXINFO = &H24
            WM_PAINTICON = &H26
            WM_ICONERASEBKGND = &H27
            WM_NEXTDLGCTL = &H28
            WM_SPOOLERSTATUS = &H2A
            WM_DRAWITEM = &H2B
            WM_MEASUREITEM = &H2C
            WM_DELETEITEM = &H2D
            WM_VKEYTOITEM = &H2E
            WM_CHARTOITEM = &H2F
            WM_SETFONT = &H30
            WM_GETFONT = &H31
            WM_SETHOTKEY = &H32
            WM_GETHOTKEY = &H33
            WM_QUERYDRAGICON = &H37
            WM_COMPAREITEM = &H39
            WM_GETOBJECT = &H3D
            WM_COMPACTING = &H41
            WM_COMMNOTIFY = &H44
            WM_WINDOWPOSCHANGING = &H46
            WM_WINDOWPOSCHANGED = &H47
            WM_POWER = &H48
            WM_COPYDATA = &H4A
            WM_CANCELJOURNAL = &H4B
            WM_NOTIFY = &H4E
            WM_INPUTLANGCHANGEREQUEST = &H50
            WM_INPUTLANGCHANGE = &H51
            WM_TCARD = &H52
            WM_HELP = &H53
            WM_USERCHANGED = &H54
            WM_NOTIFYFORMAT = &H55
            WM_CONTEXTMENU = &H7B
            WM_STYLECHANGING = &H7C
            WM_STYLECHANGED = &H7D
            WM_DISPLAYCHANGE = &H7E
            WM_GETICON = &H7F
            WM_SETICON = &H80
            WM_NCCREATE = &H81
            WM_NCDESTROY = &H82
            WM_NCCALCSIZE = &H83
            WM_NCHITTEST = &H84
            WM_NCPAINT = &H85
            WM_NCACTIVATE = &H86
            WM_GETDLGCODE = &H87
            WM_SYNCPAINT = &H88


            WM_NCMOUSEMOVE = &HA0
            WM_NCLBUTTONDOWN = &HA1
            WM_NCLBUTTONUP = &HA2
            WM_NCLBUTTONDBLCLK = &HA3
            WM_NCRBUTTONDOWN = &HA4
            WM_NCRBUTTONUP = &HA5
            WM_NCRBUTTONDBLCLK = &HA6
            WM_NCMBUTTONDOWN = &HA7
            WM_NCMBUTTONUP = &HA8
            WM_NCMBUTTONDBLCLK = &HA9
            WM_NCXBUTTONDOWN = &HAB
            WM_NCXBUTTONUP = &HAC
            WM_NCXBUTTONDBLCLK = &HAD

            WM_INPUT_DEVICE_CHANGE = &HFE
            WM_INPUT = &HFF

            WM_KEYFIRST = &H100
            WM_KEYDOWN = &H100
            WM_KEYUP = &H101
            WM_CHAR = &H102
            WM_DEADCHAR = &H103
            WM_SYSKEYDOWN = &H104
            WM_SYSKEYUP = &H105
            WM_SYSCHAR = &H106
            WM_SYSDEADCHAR = &H107
            WM_UNICHAR = &H109
            WM_KEYLAST = &H109

            WM_IME_STARTCOMPOSITION = &H10D
            WM_IME_ENDCOMPOSITION = &H10E
            WM_IME_COMPOSITION = &H10F
            WM_IME_KEYLAST = &H10F

            WM_INITDIALOG = &H110
            WM_COMMAND = &H111
            WM_SYSCOMMAND = &H112
            WM_TIMER = &H113
            WM_HSCROLL = &H114
            WM_VSCROLL = &H115
            WM_INITMENU = &H116
            WM_INITMENUPOPUP = &H117
            WM_MENUSELECT = &H11F
            WM_MENUCHAR = &H120
            WM_ENTERIDLE = &H121
            WM_MENURBUTTONUP = &H122
            WM_MENUDRAG = &H123
            WM_MENUGETOBJECT = &H124
            WM_UNINITMENUPOPUP = &H125
            WM_MENUCOMMAND = &H126

            WM_CHANGEUISTATE = &H127
            WM_UPDATEUISTATE = &H128
            WM_QUERYUISTATE = &H129

            WM_CTLCOLORMSGBOX = &H132
            WM_CTLCOLOREDIT = &H133
            WM_CTLCOLORLISTBOX = &H134
            WM_CTLCOLORBTN = &H135
            WM_CTLCOLORDLG = &H136
            WM_CTLCOLORSCROLLBAR = &H137
            WM_CTLCOLORSTATIC = &H138
            MN_GETHMENU = &H1E1

            WM_MOUSEFIRST = &H200
            WM_MOUSEMOVE = &H200
            WM_LBUTTONDOWN = &H201
            WM_LBUTTONUP = &H202
            WM_LBUTTONDBLCLK = &H203
            WM_RBUTTONDOWN = &H204
            WM_RBUTTONUP = &H205
            WM_RBUTTONDBLCLK = &H206
            WM_MBUTTONDOWN = &H207
            WM_MBUTTONUP = &H208
            WM_MBUTTONDBLCLK = &H209
            WM_MOUSEWHEEL = &H20A
            WM_XBUTTONDOWN = &H20B
            WM_XBUTTONUP = &H20C
            WM_XBUTTONDBLCLK = &H20D
            WM_MOUSEHWHEEL = &H20E

            WM_PARENTNOTIFY = &H210
            WM_ENTERMENULOOP = &H211
            WM_EXITMENULOOP = &H212

            WM_NEXTMENU = &H213
            WM_SIZING = &H214
            WM_CAPTURECHANGED = &H215
            WM_MOVING = &H216

            WM_POWERBROADCAST = &H218

            WM_DEVICECHANGE = &H219

            WM_MDICREATE = &H220
            WM_MDIDESTROY = &H221
            WM_MDIACTIVATE = &H222
            WM_MDIRESTORE = &H223
            WM_MDINEXT = &H224
            WM_MDIMAXIMIZE = &H225
            WM_MDITILE = &H226
            WM_MDICASCADE = &H227
            WM_MDIICONARRANGE = &H228
            WM_MDIGETACTIVE = &H229


            WM_MDISETMENU = &H230
            WM_ENTERSIZEMOVE = &H231
            WM_EXITSIZEMOVE = &H232
            WM_DROPFILES = &H233
            WM_MDIREFRESHMENU = &H234

            WM_IME_SETCONTEXT = &H281
            WM_IME_NOTIFY = &H282
            WM_IME_CONTROL = &H283
            WM_IME_COMPOSITIONFULL = &H284
            WM_IME_SELECT = &H285
            WM_IME_CHAR = &H286
            WM_IME_REQUEST = &H288
            WM_IME_KEYDOWN = &H290
            WM_IME_KEYUP = &H291

            WM_MOUSEHOVER = &H2A1
            WM_MOUSELEAVE = &H2A3
            WM_NCMOUSEHOVER = &H2A0
            WM_NCMOUSELEAVE = &H2A2

            WM_WTSSESSION_CHANGE = &H2B1

            WM_TABLET_FIRST = &H2C0
            WM_TABLET_LAST = &H2DF

            WM_CUT = &H300
            WM_COPY = &H301
            WM_PASTE = &H302
            WM_CLEAR = &H303
            WM_UNDO = &H304
            WM_RENDERFORMAT = &H305
            WM_RENDERALLFORMATS = &H306
            WM_DESTROYCLIPBOARD = &H307
            WM_DRAWCLIPBOARD = &H308
            WM_PAINTCLIPBOARD = &H309
            WM_VSCROLLCLIPBOARD = &H30A
            WM_SIZECLIPBOARD = &H30B
            WM_ASKCBFORMATNAME = &H30C
            WM_CHANGECBCHAIN = &H30D
            WM_HSCROLLCLIPBOARD = &H30E
            WM_QUERYNEWPALETTE = &H30F
            WM_PALETTEISCHANGING = &H310
            WM_PALETTECHANGED = &H311
            WM_HOTKEY = &H312

            WM_PRINT = &H317
            WM_PRINTCLIENT = &H318

            WM_APPCOMMAND = &H319

            WM_THEMECHANGED = &H31A

            WM_CLIPBOARDUPDATE = &H31D

            WM_DWMCOMPOSITIONCHANGED = &H31E
            WM_DWMNCRENDERINGCHANGED = &H31F
            WM_DWMCOLORIZATIONCOLORCHANGED = &H320
            WM_DWMWINDOWMAXIMIZEDCHANGE = &H321

            WM_GETTITLEBARINFOEX = &H33F

            WM_HANDHELDFIRST = &H358
            WM_HANDHELDLAST = &H35F

            WM_AFXFIRST = &H360
            WM_AFXLAST = &H37F

            WM_PENWINFIRST = &H380
            WM_PENWINLAST = &H38F

            WM_APP = &H8000

            WM_USER = &H400

            WM_REFLECT = WM_USER + &H1C00
        End Enum

        <StructLayout(LayoutKind.Sequential)>
        Public Structure MARGIN
            Public cxLeftWidth As Integer
            Public cxRightWidth As Integer
            Public cyTopHeight As Integer
            Public cyBottomHeight As Integer
        End Structure

        <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)>
        Public Structure WNDCLASSEX
            Public cbSize As UInteger
            Public style As UInteger
            Public lpfnWndProc As IntPtr
            Public cbClsExtra As Integer
            Public cbWndExtra As Integer
            Public hInstance As IntPtr
            Public hIcon As IntPtr
            Public hCursor As IntPtr
            Public hbrBackground As IntPtr
            Public lpszMenuName As String
            Public lpszClassName As String
            Public hIconSm As IntPtr

            Public Shared Function Size() As UInteger
                Return CUInt(Marshal.SizeOf(ObfuscatorNeedsThis(Of WNDCLASSEX)()))
            End Function

            Private Shared Function ObfuscatorNeedsThis(Of T)() As Type
                Return GetType(T)
            End Function
        End Structure

        <StructLayout(LayoutKind.Sequential)>
        Public Structure POINT
            Public X As Integer
            Public Y As Integer
        End Structure

        <StructLayout(LayoutKind.Sequential)>
        Public Structure RECT
            Public Left As Integer ' x position of upper-left corner
            Public Top As Integer ' y position of upper-left corner
            Public Right As Integer ' x position of lower-right corner
            Public Bottom As Integer ' y position of lower-right corner
        End Structure

#End Region

#Region "LoadLibrary and GetProcAddress"

        Friend NotInheritable Class WinApi

            Private Sub New()
            End Sub
            <DllImport("kernel32.dll", EntryPoint:="GetProcAddress", SetLastError:=False, CharSet:=CharSet.Ansi)>
            Private Shared Function getProcAddress(ByVal hmodule As IntPtr, ByVal procName As String) As IntPtr
            End Function

            <DllImport("kernel32.dll", EntryPoint:="LoadLibraryW", SetLastError:=False, CharSet:=CharSet.Unicode)>
            Private Shared Function loadLibraryW(ByVal lpFileName As String) As IntPtr
            End Function

            <DllImport("kernel32.dll", EntryPoint:="GetModuleHandleW", SetLastError:=False, CharSet:=CharSet.Unicode)>
            Private Shared Function getModuleHandle(ByVal modulename As String) As IntPtr
            End Function

            Public Shared Function GetProcAddress(ByVal modulename As String, ByVal procname As String) As IntPtr
                Dim hModule As IntPtr = getModuleHandle(modulename)

                If hModule = IntPtr.Zero Then
                    hModule = loadLibraryW(modulename)
                End If

                Return getProcAddress(hModule, procname)
            End Function

            Public Shared Function GetMethod(Of T)(ByVal modulename As String, ByVal procname As String) As T
                Dim hModule As IntPtr = getModuleHandle(modulename)

                If hModule = IntPtr.Zero Then
                    hModule = loadLibraryW(modulename)
                End If

                Dim procAddress As IntPtr = getProcAddress(hModule, procname)

#If DEBUG Then
                If hModule = IntPtr.Zero OrElse procAddress = IntPtr.Zero Then
                    Throw New Exception("module: " & modulename & ControlChars.Tab & "proc: " & procname)
                End If
#End If

                Return DirectCast(DirectCast(Marshal.GetDelegateForFunctionPointer(procAddress, ObfuscatorNeedsThis(Of T)()), Object), T)
            End Function

            Private Shared Function ObfuscatorNeedsThis(Of T)() As Type
                Return GetType(T)
            End Function
        End Class

#End Region
End Class
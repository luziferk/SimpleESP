Imports System
Imports System.Threading
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices


Public Enum OverlayWindowNameGenerator
        None
        Random
        Legit
        Executable
        Custom
    End Enum

    Public Class OverlayWindow
        Implements IDisposable

        Public Shared WindowNameGenerator As OverlayWindowNameGenerator = OverlayWindowNameGenerator.Random
        Public Shared CustomWindowName As String = String.Empty
        Public Shared BypassTopmost As Boolean = False

        Private rng As Random
        Private Delegate Function WndProc(ByVal hWnd As IntPtr, ByVal msg As PInvoke.WindowsMessage, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As IntPtr

        Private wndProcPointer As IntPtr
        Private _wndProc As WndProc

        Private windowThread As Thread

        Private randomClassName As String

        Private privateWindowHandle As IntPtr
        Public Property WindowHandle() As IntPtr
            Get
                Return privateWindowHandle
            End Get
            Private Set(ByVal value As IntPtr)
                privateWindowHandle = value
            End Set
        End Property

        Private privateX As Integer
        Public Property X() As Integer
            Get
                Return privateX
            End Get
            Private Set(ByVal value As Integer)
                privateX = value
            End Set
        End Property
        Private privateY As Integer
        Public Property Y() As Integer
            Get
                Return privateY
            End Get
            Private Set(ByVal value As Integer)
                privateY = value
            End Set
        End Property
        Private privateWidth As Integer
        Public Property Width() As Integer
            Get
                Return privateWidth
            End Get
            Private Set(ByVal value As Integer)
                privateWidth = value
            End Set
        End Property
        Private privateHeight As Integer
        Public Property Height() As Integer
            Get
                Return privateHeight
            End Get
            Private Set(ByVal value As Integer)
                privateHeight = value
            End Set
        End Property

        Private privateIsVisible As Boolean
        Public Property IsVisible() As Boolean
            Get
                Return privateIsVisible
            End Get
            Private Set(ByVal value As Boolean)
                privateIsVisible = value
            End Set
        End Property
        Private privateTopmost As Boolean
        Public Property Topmost() As Boolean
            Get
                Return privateTopmost
            End Get
            Private Set(ByVal value As Boolean)
                privateTopmost = value
            End Set
        End Property

        Public Sub New()
            windowThread = New Thread(Sub() windowThreadMethod()) With {.IsBackground = True, .Priority = ThreadPriority.BelowNormal}
            windowThread.Start()

            Do While WindowHandle = IntPtr.Zero
                Thread.Sleep(10)
            Loop
        End Sub

        Public Sub New(ByVal x As Integer, ByVal y As Integer, ByVal width As Integer, ByVal height As Integer)
            windowThread = New Thread(Sub() windowThreadMethod(x, y, width, height)) With {.IsBackground = True, .Priority = ThreadPriority.BelowNormal}
            windowThread.Start()

            Do While WindowHandle = IntPtr.Zero
                Thread.Sleep(10)
            Loop
        End Sub

        Protected Overrides Sub Finalize()
            Dispose(False)
        End Sub

        Private Sub windowThreadMethod(Optional ByVal x As Integer = 0, Optional ByVal y As Integer = 0, Optional ByVal width As Integer = 800, Optional ByVal height As Integer = 600)
        setupInstance(x, y, width, height)

        Do
            Thread.Sleep(1)
        Loop

    End Sub

        Private Sub setupInstance(Optional ByVal x As Integer = 0, Optional ByVal y As Integer = 0, Optional ByVal width As Integer = 800, Optional ByVal height As Integer = 600)
            IsVisible = True
            Topmost = If(BypassTopmost, True, False)

            Me.X = x
            Me.Y = y
            Me.Width = width
            Me.Height = height

            randomClassName = generateRandomString(5, 11)
            Dim randomMenuName As String = generateRandomString(5, 11)

            Dim randomWindowName As String = String.Empty 'generateRandomString(5, 11);

            Select Case WindowNameGenerator
                Case OverlayWindowNameGenerator.None
                    randomWindowName = String.Empty
                Case OverlayWindowNameGenerator.Random
                    randomWindowName = generateRandomString(5, 11)
                Case OverlayWindowNameGenerator.Legit
                    randomWindowName = getLegitWindowName()
                Case OverlayWindowNameGenerator.Executable
                    randomWindowName = getExecutableName()
                Case OverlayWindowNameGenerator.Custom
                    randomWindowName = CustomWindowName
                Case Else
                    randomWindowName = String.Empty
            End Select

            ' prepare method
            Dim _wndProc As WndProc = AddressOf windowProcedure
            RuntimeHelpers.PrepareDelegate(_wndProc)
            wndProcPointer = Marshal.GetFunctionPointerForDelegate(_wndProc)

            Dim wndClassEx As New PInvoke.WNDCLASSEX() With {.cbSize = PInvoke.WNDCLASSEX.Size(), .style = 0, .lpfnWndProc = wndProcPointer, .cbClsExtra = 0, .cbWndExtra = 0, .hInstance = IntPtr.Zero, .hIcon = IntPtr.Zero, .hCursor = IntPtr.Zero, .hbrBackground = IntPtr.Zero, .lpszMenuName = randomMenuName, .lpszClassName = randomClassName, .hIconSm = IntPtr.Zero}

            PInvoke.RegisterClassEx(wndClassEx)

            Dim exStyle As UInteger

            If BypassTopmost Then
                exStyle = &H20 Or &H80000 Or &H80 Or &H8000000
            Else
                exStyle = &H8 Or &H20 Or &H80000 Or &H80 Or &H8000000 ' WS_EX_TOPMOST | WS_EX_TRANSPARENT | WS_EX_LAYERED |WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE
            End If

            WindowHandle = PInvoke.CreateWindowEx(exStyle, randomClassName, randomWindowName, &H80000000L Or &H10000000, Me.X, Me.Y, Me.Width, Me.Height, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero) ' WS_POPUP | WS_VISIBLE -  WS_EX_TOPMOST | WS_EX_TRANSPARENT | WS_EX_LAYERED |WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE

            PInvoke.SetLayeredWindowAttributes(WindowHandle, 0, 255, &H2) '0x1 |
            PInvoke.UpdateWindow(WindowHandle)

            ' TODO: If window is incompatible on some platforms use SetWindowLong to set the style again and UpdateWindow
            ' If you have changed certain window data using SetWindowLong, you must call SetWindowPos for the changes to take effect. Use the following combination for uFlags: SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED. 

            extendFrameIntoClientArea()
        End Sub

        Private Function windowProcedure(ByVal hwnd As IntPtr, ByVal msg As PInvoke.WindowsMessage, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As IntPtr
            Select Case msg
                Case PInvoke.WindowsMessage.WM_DESTROY
                    Return New IntPtr(0)
                Case PInvoke.WindowsMessage.WM_ERASEBKGND
                    PInvoke.SendMessage(WindowHandle, PInvoke.WindowsMessage.WM_PAINT, New IntPtr(0), New IntPtr(0))
                Case PInvoke.WindowsMessage.WM_KEYDOWN
                    Return New IntPtr(0)
                Case PInvoke.WindowsMessage.WM_PAINT
                    Return New IntPtr(0)
                Case PInvoke.WindowsMessage.WM_DWMCOMPOSITIONCHANGED ' needed for windows 7 support
                    extendFrameIntoClientArea()
                    Return New IntPtr(0)
                Case Else
            End Select

            If CInt(msg) = &H2E0 Then ' DPI Changed
                Return New IntPtr(0) ' block DPI Changed message
            End If

            Return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam)
        End Function

        Public Sub extendFrameIntoClientArea()
            'var margin = new MARGIN
            '{
            '    cxLeftWidth = this.X,
            '    cxRightWidth = this.Width,
            '    cyBottomHeight = this.Height,
            '    cyTopHeight = this.Y
            '};

            Dim margin = New PInvoke.MARGIN With {.cxLeftWidth = -1, .cxRightWidth = -1, .cyBottomHeight = -1, .cyTopHeight = -1}

            PInvoke.DwmExtendFrameIntoClientArea(WindowHandle, margin)
        End Sub

        Private Function generateRandomString(ByVal minlen As Integer, ByVal maxlen As Integer) As String
            If rng Is Nothing Then
                rng = New Random()
            End If

            Dim len As Integer = rng.Next(minlen, maxlen)

            Dim chars(len - 1) As Char

            For i As Integer = 0 To chars.Length - 1
                chars(i) = ChrW(rng.Next(97, 123))
            Next i

            Return New String(chars)
        End Function

        Private Function getLegitWindowName() As String
            Dim legitWindows() As String = {"Teamspeak 3", "Steam", "Discord", "Mozilla Firefox"}

            Return legitWindows(rng.Next(0, legitWindows.Length)) ' Note: random max value is exclusive ;)
        End Function

        Private Function getExecutableName() As String
            Dim proc = System.Diagnostics.Process.GetCurrentProcess()
            Dim [mod] = proc.MainModule

            Dim name As String = [mod].FileName

            [mod].Dispose()
            proc.Dispose()

            ' Path class tends to throw errors. microsoft is lazy af
            Return If(name.Contains("\"), System.IO.Path.GetFileNameWithoutExtension(name), name)
        End Function

        Public Sub ShowWindow()
            If IsVisible Then
                Return
            End If

            PInvoke.ShowWindow(WindowHandle, 5)
            extendFrameIntoClientArea()
            IsVisible = True
        End Sub

        Public Sub HideWindow()
            If Not IsVisible Then
                Return
            End If

            PInvoke.ShowWindow(WindowHandle, 0)
            IsVisible = False
        End Sub

        Public Sub MoveWindow(ByVal x As Integer, ByVal y As Integer)
            PInvoke.MoveWindow(WindowHandle, x, y, Width, Height, 1)
            Me.X = x
            Me.Y = y
            extendFrameIntoClientArea()
        End Sub

        Public Sub ResizeWindow(ByVal width As Integer, ByVal height As Integer)
            PInvoke.MoveWindow(WindowHandle, X, Y, width, height, 1)
            Me.Width = width
            Me.Height = height
            extendFrameIntoClientArea()
        End Sub

        Public Sub SetWindowBounds(ByVal x As Integer, ByVal y As Integer, ByVal width As Integer, ByVal height As Integer)
            PInvoke.MoveWindow(WindowHandle, x, y, width, height, 1)
            Me.X = x
            Me.Y = y
            Me.Width = width
            Me.Height = height
            extendFrameIntoClientArea()
        End Sub

#Region "IDisposable Support"
        Private disposedValue As Boolean = False

        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not disposedValue Then
                If disposing Then
                    rng = Nothing
                End If

                If windowThread IsNot Nothing Then
                    windowThread.Abort()
                End If

                Try
                    windowThread.Join()
                Catch

                End Try

                PInvoke.DestroyWindow(WindowHandle)
                PInvoke.UnregisterClass(randomClassName, IntPtr.Zero)

                disposedValue = True
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub
#End Region
    End Class

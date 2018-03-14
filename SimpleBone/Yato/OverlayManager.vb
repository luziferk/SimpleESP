Imports System
Imports System.Threading
Imports System.Runtime.InteropServices

Public Class OverlayManager
        Implements IDisposable

        Private exitThread As Boolean
        Private serviceThread As Thread

        Private privateParentWindowHandle As IntPtr
        Public Property ParentWindowHandle() As IntPtr
            Get
                Return privateParentWindowHandle
            End Get
            Private Set(ByVal value As IntPtr)
                privateParentWindowHandle = value
            End Set
        End Property

        Private privateWindow As OverlayWindow
        Public Property Window() As OverlayWindow
            Get
                Return privateWindow
            End Get
            Private Set(ByVal value As OverlayWindow)
                privateWindow = value
            End Set
        End Property
        Private privateGraphics As Direct2DRenderer
        Public Property Graphics() As Direct2DRenderer
            Get
                Return privateGraphics
            End Get
            Private Set(ByVal value As Direct2DRenderer)
                privateGraphics = value
            End Set
        End Property

        Private privateIsParentWindowVisible As Boolean
        Public Property IsParentWindowVisible() As Boolean
            Get
                Return privateIsParentWindowVisible
            End Get
            Private Set(ByVal value As Boolean)
                privateIsParentWindowVisible = value
            End Set
        End Property

        Private Sub New()

        End Sub

        Public Sub New(ByVal parentWindowHandle As IntPtr, Optional ByVal vsync As Boolean = False, Optional ByVal measurefps As Boolean = False, Optional ByVal antialiasing As Boolean = True)
            Dim options As New Direct2DRendererOptions() With {.AntiAliasing = antialiasing, .Hwnd = IntPtr.Zero, .MeasureFps = measurefps, .VSync = vsync}
            setupInstance(parentWindowHandle, options)
        End Sub

        Public Sub New(ByVal parentWindowHandle As IntPtr, ByVal options As Direct2DRendererOptions)
            setupInstance(parentWindowHandle, options)
        End Sub

        Protected Overrides Sub Finalize()
            Dispose(False)
        End Sub

        Private Sub setupInstance(ByVal parentWindowHandle As IntPtr, ByVal options As Direct2DRendererOptions)
            Me.ParentWindowHandle = parentWindowHandle

            If PInvoke.IsWindow(parentWindowHandle) = 0 Then
                Throw New Exception("The parent window does not exist")
            End If

            Dim bounds As New PInvoke.RECT()
            PInvoke.GetRealWindowRect(parentWindowHandle, bounds)

            Dim x As Integer = bounds.Left
            Dim y As Integer = bounds.Top

            Dim width As Integer = bounds.Right - x
            Dim height As Integer = bounds.Bottom - y

            Window = New OverlayWindow(x, y, width, height)

            options.Hwnd = Window.WindowHandle

            Graphics = New Direct2DRenderer(options)

            serviceThread = New Thread(New ThreadStart(AddressOf windowServiceThread))
            serviceThread.Priority = ThreadPriority.BelowNormal
            serviceThread.IsBackground = True

            serviceThread.Start()
        End Sub

        Private Sub windowServiceThread()
            Dim bounds As New PInvoke.RECT()

            Do While Not exitThread
                Thread.Sleep(100)

                If PInvoke.IsWindowVisible(ParentWindowHandle) = 0 Then
                    If Window.IsVisible Then
                        Window.HideWindow()
                    End If
                    Continue Do
                End If

                If Not Window.IsVisible Then
                    Window.ShowWindow()
                End If

                If OverlayWindow.BypassTopmost Then
                    Dim windowAboveParentWindow As IntPtr = PInvoke.GetWindow(ParentWindowHandle, 3) ' GW_HWNDPREV

                    If windowAboveParentWindow <> Window.WindowHandle Then
                        PInvoke.SetWindowPos(Window.WindowHandle, windowAboveParentWindow, 0, 0, 0, 0, &H10 Or &H2 Or &H1 Or &H4000) ' SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOSIZE | SWP_ASYNCWINDOWPOS
                    End If
                End If

                PInvoke.GetRealWindowRect(ParentWindowHandle, bounds)

                Dim x As Integer = bounds.Left
                Dim y As Integer = bounds.Top

                Dim width As Integer = bounds.Right - x
                Dim height As Integer = bounds.Bottom - y

                If Window.X = x AndAlso Window.Y = y AndAlso Window.Width = width AndAlso Window.Height = height Then
                    Continue Do
                End If

                Window.SetWindowBounds(x, y, width, height)
                Graphics.Resize(width, height)
            Loop
        End Sub

#Region "IDisposable Support"
        Private disposedValue As Boolean = False

        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not disposedValue Then
                If disposing Then
                    ' managed
                End If

                ' unmanaged

                If serviceThread IsNot Nothing Then
                    exitThread = True

                    Try
                        serviceThread.Join()
                    Catch

                    End Try
                End If

                Graphics.Dispose()
                Window.Dispose()

                disposedValue = True
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub
#End Region
    End Class

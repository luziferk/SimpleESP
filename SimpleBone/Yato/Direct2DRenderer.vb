Imports System
Imports System.IO
Imports System.Diagnostics
Imports System.Runtime.InteropServices

Imports SharpDX
Imports SharpDX.Direct2D1
Imports SharpDX.DirectWrite
Imports SharpDX.DXGI
Imports SharpDX.Mathematics.Interop

Imports FontFactory = SharpDX.DirectWrite.Factory
Imports Factory = SharpDX.Direct2D1.Factory


Public Class Direct2DRenderer
        Implements IDisposable

#Region "private vars"

        Private rendererOptions As Direct2DRendererOptions

        Private device As WindowRenderTarget
        Private deviceProperties As HwndRenderTargetProperties

        Private fontFactory As FontFactory
        Private factory As Factory

        Private sharedBrush As SolidColorBrush
        Private sharedFont As TextFormat

        Private isDrawing As Boolean

        'INSTANT VB NOTE: The variable resize was renamed since Visual Basic does not allow variables and other class members to have the same name:
        Private resize_Renamed As Boolean
        Private resizeWidth As Integer
        Private resizeHeight As Integer

        Private stopwatch As New Stopwatch()

        Private internalFps As Integer

#End Region

#Region "public vars"

        Private privateRenderTargetHwnd As IntPtr
        Public Property RenderTargetHwnd() As IntPtr
            Get
                Return privateRenderTargetHwnd
            End Get
            Private Set(ByVal value As IntPtr)
                privateRenderTargetHwnd = value
            End Set
        End Property
        Private privateVSync As Boolean
        Public Property VSync() As Boolean
            Get
                Return privateVSync
            End Get
            Private Set(ByVal value As Boolean)
                privateVSync = value
            End Set
        End Property
        Private privateFPS As Integer
        Public Property FPS() As Integer
            Get
                Return privateFPS
            End Get
            Private Set(ByVal value As Integer)
                privateFPS = value
            End Set
        End Property

        Public Property MeasureFPS() As Boolean

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

#End Region

#Region "construct & destruct"

        Private Sub New()
            Throw New NotSupportedException()
        End Sub

        Public Sub New(ByVal hwnd As IntPtr)
            Dim options = New Direct2DRendererOptions() With {.Hwnd = hwnd, .VSync = False, .MeasureFps = False, .AntiAliasing = False}
            setupInstance(options)
        End Sub

        Public Sub New(ByVal hwnd As IntPtr, ByVal vsync As Boolean)
            Dim options = New Direct2DRendererOptions() With {.Hwnd = hwnd, .VSync = vsync, .MeasureFps = False, .AntiAliasing = False}
            setupInstance(options)
        End Sub

        Public Sub New(ByVal hwnd As IntPtr, ByVal vsync As Boolean, ByVal measureFps As Boolean)
            Dim options = New Direct2DRendererOptions() With {.Hwnd = hwnd, .VSync = vsync, .MeasureFps = measureFps, .AntiAliasing = False}
            setupInstance(options)
        End Sub

        Public Sub New(ByVal hwnd As IntPtr, ByVal vsync As Boolean, ByVal measureFps As Boolean, ByVal antiAliasing As Boolean)
            Dim options = New Direct2DRendererOptions() With {.Hwnd = hwnd, .VSync = vsync, .MeasureFps = measureFps, .AntiAliasing = antiAliasing}
            setupInstance(options)
        End Sub

        Public Sub New(ByVal options As Direct2DRendererOptions)
            setupInstance(options)
        End Sub

        Protected Overrides Sub Finalize()
            Dispose(False)
        End Sub

#End Region

#Region "init & delete"

        Private Sub setupInstance(ByVal options As Direct2DRendererOptions)
            rendererOptions = options

            If options.Hwnd = IntPtr.Zero Then
                Throw New ArgumentNullException(NameOf(options.Hwnd))
            End If

            If PInvoke.IsWindow(options.Hwnd) = 0 Then
                Throw New ArgumentException("The window does not exist (hwnd = 0x" & options.Hwnd.ToString("X") & ")")
            End If

            Dim bounds As New PInvoke.RECT()

            If PInvoke.GetRealWindowRect(options.Hwnd, bounds) = 0 Then
                Throw New Exception("Failed to get the size of the given window (hwnd = 0x" & options.Hwnd.ToString("X") & ")")
            End If

            Me.Width = bounds.Right - bounds.Left
            Me.Height = bounds.Bottom - bounds.Top

            Me.VSync = options.VSync
            Me.MeasureFPS = options.MeasureFps

            deviceProperties = New HwndRenderTargetProperties() With {.Hwnd = options.Hwnd, .PixelSize = New Size2(Me.Width, Me.Height), .PresentOptions = If(options.VSync, PresentOptions.None, PresentOptions.Immediately)}

            Dim renderProperties = New RenderTargetProperties(RenderTargetType.Default, New PixelFormat(Format.R8G8B8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied), 96.0F, 96.0F, RenderTargetUsage.None, FeatureLevel.Level_DEFAULT) ' we use 96.0f because it's the default value. This will scale every drawing by 1.0f (it obviously does not scale anything). Our drawing will be dpi aware!

            factory = New Factory()
            fontFactory = New FontFactory()

            Try
                device = New WindowRenderTarget(factory, renderProperties, deviceProperties)
            Catch e1 As SharpDXException ' D2DERR_UNSUPPORTED_PIXEL_FORMAT
                renderProperties.PixelFormat = New PixelFormat(Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied)
                device = New WindowRenderTarget(factory, renderProperties, deviceProperties)
            End Try

            device.AntialiasMode = AntialiasMode.Aliased ' AntialiasMode.PerPrimitive fails rendering some objects
            ' other than in the documentation: Cleartype is much faster for me than GrayScale
            device.TextAntialiasMode = If(options.AntiAliasing, SharpDX.Direct2D1.TextAntialiasMode.Cleartype, SharpDX.Direct2D1.TextAntialiasMode.Aliased)

            sharedBrush = New SolidColorBrush(device, Nothing)
        End Sub

        Private Sub deleteInstance()
            Try
                sharedBrush.Dispose()
                fontFactory.Dispose()
                factory.Dispose()
                device.Dispose()
            Catch

            End Try
        End Sub

#End Region

#Region "Scenes"

        Public Sub Resize(ByVal width As Integer, ByVal height As Integer)
            resizeWidth = width
            resizeHeight = height
            resize_Renamed = True
        End Sub

        Public Sub BeginScene()
            If device Is Nothing Then
                Return
            End If
            If isDrawing Then
                Return
            End If

            If MeasureFPS AndAlso (Not stopwatch.IsRunning) Then
                stopwatch.Restart()
            End If

            If resize_Renamed Then
                device.Resize(New Size2(resizeWidth, resizeHeight))
                resize_Renamed = False
            End If

            device.BeginDraw()

            isDrawing = True
        End Sub

        Public Function UseScene() As Direct2DScene
            ' really expensive to use but i like the pattern
            Return New Direct2DScene(Me)
        End Function

        Public Sub EndScene()
            If device Is Nothing Then
                Return
            End If
            If Not isDrawing Then
                Return
            End If

            Dim tag_0 As Long = 0L, tag_1 As Long = 0L
            Dim result = device.TryEndDraw(tag_0, tag_1)

            If result.Failure Then
                deleteInstance()
                setupInstance(rendererOptions)
            End If

            If MeasureFPS AndAlso stopwatch.IsRunning Then
                internalFps += 1

                If stopwatch.ElapsedMilliseconds > 1000 Then
                    FPS = internalFps
                    internalFps = 0
                    stopwatch.Stop()
                End If
            End If

            isDrawing = False
        End Sub

        Public Sub ClearScene()
            device.Clear(Nothing)
        End Sub

        Public Sub ClearScene(ByVal color As Direct2DColor)
            device.Clear(color)
        End Sub

        Public Sub ClearScene(ByVal brush As Direct2DBrush)
            device.Clear(brush)
        End Sub

#End Region

#Region "Fonts & Brushes & Bitmaps"

        Public Sub SetSharedFont(ByVal fontFamilyName As String, ByVal size As Single, Optional ByVal bold As Boolean = False, Optional ByVal italic As Boolean = False)
            sharedFont = New TextFormat(fontFactory, fontFamilyName, If(bold, FontWeight.Bold, FontWeight.Normal), If(italic, FontStyle.Italic, FontStyle.Normal), size)
            sharedFont.WordWrapping = SharpDX.DirectWrite.WordWrapping.NoWrap
        End Sub

        Public Function CreateBrush(ByVal color As Direct2DColor) As Direct2DBrush
            Return New Direct2DBrush(device, color)
        End Function

        Public Function CreateBrush(ByVal r As Integer, ByVal g As Integer, ByVal b As Integer, Optional ByVal a As Integer = 255) As Direct2DBrush
            Return New Direct2DBrush(device, New Direct2DColor(r, g, b, a))
        End Function

        Public Function CreateBrush(ByVal r As Single, ByVal g As Single, ByVal b As Single, Optional ByVal a As Single = 1.0F) As Direct2DBrush
            Return New Direct2DBrush(device, New Direct2DColor(r, g, b, a))
        End Function

        Public Function CreateFont(ByVal fontFamilyName As String, ByVal size As Single, Optional ByVal bold As Boolean = False, Optional ByVal italic As Boolean = False) As Direct2DFont
            Return New Direct2DFont(fontFactory, fontFamilyName, size, bold, italic)
        End Function

        Public Function CreateFont(ByVal options As Direct2DFontCreationOptions) As Direct2DFont
            Dim font As New TextFormat(fontFactory, options.FontFamilyName, If(options.Bold, FontWeight.Bold, FontWeight.Normal), options.GetStyle(), options.FontSize)
            font.WordWrapping = If(options.WordWrapping, WordWrapping.Wrap, WordWrapping.NoWrap)
            Return New Direct2DFont(font)
        End Function

        Public Function LoadBitmap(ByVal file As String) As Direct2DBitmap
            Return New Direct2DBitmap(device, file)
        End Function

        Public Function LoadBitmap(ByVal bytes() As Byte) As Direct2DBitmap
            Return New Direct2DBitmap(device, bytes)
        End Function

#End Region

#Region "Primitives"

        Public Sub DrawLine(ByVal start_x As Single, ByVal start_y As Single, ByVal end_x As Single, ByVal end_y As Single, ByVal stroke As Single, ByVal brush As Direct2DBrush)
            device.DrawLine(New RawVector2(start_x, start_y), New RawVector2(end_x, end_y), brush, stroke)
        End Sub

        Public Sub DrawLine(ByVal start_x As Single, ByVal start_y As Single, ByVal end_x As Single, ByVal end_y As Single, ByVal stroke As Single, ByVal color As Direct2DColor)
            sharedBrush.Color = color
            device.DrawLine(New RawVector2(start_x, start_y), New RawVector2(end_x, end_y), sharedBrush, stroke)
        End Sub

        Public Sub DrawRectangle(ByVal x As Single, ByVal y As Single, ByVal width As Single, ByVal height As Single, ByVal stroke As Single, ByVal brush As Direct2DBrush)
            device.DrawRectangle(New RawRectangleF(x, y, x + width, y + height), brush, stroke)
        End Sub

        Public Sub DrawRectangle(ByVal x As Single, ByVal y As Single, ByVal width As Single, ByVal height As Single, ByVal stroke As Single, ByVal color As Direct2DColor)
            sharedBrush.Color = color
            device.DrawRectangle(New RawRectangleF(x, y, x + width, y + height), sharedBrush, stroke)
        End Sub

        Public Sub DrawRectangleEdges(ByVal x As Single, ByVal y As Single, ByVal width As Single, ByVal height As Single, ByVal stroke As Single, ByVal brush As Direct2DBrush)
            Dim length As Integer = CInt(Math.Truncate(((width + height) / 2.0F) * 0.2F))

            Dim first As New RawVector2(x, y)
            Dim second As New RawVector2(x, y + length)
            Dim third As New RawVector2(x + length, y)

            device.DrawLine(first, second, brush, stroke)
            device.DrawLine(first, third, brush, stroke)

            first.Y += height
            second.Y = first.Y - length
            third.Y = first.Y
            third.X = first.X + length

            device.DrawLine(first, second, brush, stroke)
            device.DrawLine(first, third, brush, stroke)

            first.X = x + width
            first.Y = y
            second.X = first.X - length
            second.Y = first.Y
            third.X = first.X
            third.Y = first.Y + length

            device.DrawLine(first, second, brush, stroke)
            device.DrawLine(first, third, brush, stroke)

            first.Y += height
            second.X += length
            second.Y = first.Y - length
            third.Y = first.Y
            third.X = first.X - length

            device.DrawLine(first, second, brush, stroke)
            device.DrawLine(first, third, brush, stroke)
        End Sub

        Public Sub DrawRectangleEdges(ByVal x As Single, ByVal y As Single, ByVal width As Single, ByVal height As Single, ByVal stroke As Single, ByVal color As Direct2DColor)
            sharedBrush.Color = color

            Dim length As Integer = CInt(Math.Truncate(((width + height) / 2.0F) * 0.2F))

            Dim first As New RawVector2(x, y)
            Dim second As New RawVector2(x, y + length)
            Dim third As New RawVector2(x + length, y)

            device.DrawLine(first, second, sharedBrush, stroke)
            device.DrawLine(first, third, sharedBrush, stroke)

            first.Y += height
            second.Y = first.Y - length
            third.Y = first.Y
            third.X = first.X + length

            device.DrawLine(first, second, sharedBrush, stroke)
            device.DrawLine(first, third, sharedBrush, stroke)

            first.X = x + width
            first.Y = y
            second.X = first.X - length
            second.Y = first.Y
            third.X = first.X
            third.Y = first.Y + length

            device.DrawLine(first, second, sharedBrush, stroke)
            device.DrawLine(first, third, sharedBrush, stroke)

            first.Y += height
            second.X += length
            second.Y = first.Y - length
            third.Y = first.Y
            third.X = first.X - length

            device.DrawLine(first, second, sharedBrush, stroke)
            device.DrawLine(first, third, sharedBrush, stroke)
        End Sub

        Public Sub DrawCircle(ByVal x As Single, ByVal y As Single, ByVal radius As Single, ByVal stroke As Single, ByVal brush As Direct2DBrush)
            device.DrawEllipse(New Ellipse(New RawVector2(x, y), radius, radius), brush, stroke)
        End Sub

        Public Sub DrawCircle(ByVal x As Single, ByVal y As Single, ByVal radius As Single, ByVal stroke As Single, ByVal color As Direct2DColor)
            sharedBrush.Color = color
            device.DrawEllipse(New Ellipse(New RawVector2(x, y), radius, radius), sharedBrush, stroke)
        End Sub

        Public Sub DrawEllipse(ByVal x As Single, ByVal y As Single, ByVal radius_x As Single, ByVal radius_y As Single, ByVal stroke As Single, ByVal brush As Direct2DBrush)
            device.DrawEllipse(New Ellipse(New RawVector2(x, y), radius_x, radius_y), brush, stroke)
        End Sub

        Public Sub DrawEllipse(ByVal x As Single, ByVal y As Single, ByVal radius_x As Single, ByVal radius_y As Single, ByVal stroke As Single, ByVal color As Direct2DColor)
            sharedBrush.Color = color
            device.DrawEllipse(New Ellipse(New RawVector2(x, y), radius_x, radius_y), sharedBrush, stroke)
        End Sub

#End Region

#Region "Filled"

        Public Sub FillRectangle(ByVal x As Single, ByVal y As Single, ByVal width As Single, ByVal height As Single, ByVal brush As Direct2DBrush)
            device.FillRectangle(New RawRectangleF(x, y, x + width, y + height), brush)
        End Sub

        Public Sub FillRectangle(ByVal x As Single, ByVal y As Single, ByVal width As Single, ByVal height As Single, ByVal color As Direct2DColor)
            sharedBrush.Color = color
            device.FillRectangle(New RawRectangleF(x, y, x + width, y + height), sharedBrush)
        End Sub

        Public Sub FillCircle(ByVal x As Single, ByVal y As Single, ByVal radius As Single, ByVal brush As Direct2DBrush)
            device.FillEllipse(New Ellipse(New RawVector2(x, y), radius, radius), brush)
        End Sub

        Public Sub FillCircle(ByVal x As Single, ByVal y As Single, ByVal radius As Single, ByVal color As Direct2DColor)
            sharedBrush.Color = color
            device.FillEllipse(New Ellipse(New RawVector2(x, y), radius, radius), sharedBrush)
        End Sub

        Public Sub FillEllipse(ByVal x As Single, ByVal y As Single, ByVal radius_x As Single, ByVal radius_y As Single, ByVal brush As Direct2DBrush)
            device.FillEllipse(New Ellipse(New RawVector2(x, y), radius_x, radius_y), brush)
        End Sub

        Public Sub FillEllipse(ByVal x As Single, ByVal y As Single, ByVal radius_x As Single, ByVal radius_y As Single, ByVal color As Direct2DColor)
            sharedBrush.Color = color
            device.FillEllipse(New Ellipse(New RawVector2(x, y), radius_x, radius_y), sharedBrush)
        End Sub

#End Region

#Region "Bordered"

        Public Sub BorderedLine(ByVal start_x As Single, ByVal start_y As Single, ByVal end_x As Single, ByVal end_y As Single, ByVal stroke As Single, ByVal color As Direct2DColor, ByVal borderColor As Direct2DColor)
            Dim geometry = New PathGeometry(factory)

            Dim sink = geometry.Open()

            Dim half As Single = stroke / 2.0F
            Dim quarter As Single = half / 2.0F

            sink.BeginFigure(New RawVector2(start_x, start_y - half), FigureBegin.Filled)

            sink.AddLine(New RawVector2(end_x, end_y - half))
            sink.AddLine(New RawVector2(end_x, end_y + half))
            sink.AddLine(New RawVector2(start_x, start_y + half))

            sink.EndFigure(FigureEnd.Closed)

            sink.Close()

            sharedBrush.Color = borderColor

            device.DrawGeometry(geometry, sharedBrush, half)

            sharedBrush.Color = color

            device.FillGeometry(geometry, sharedBrush)

            sink.Dispose()
            geometry.Dispose()
        End Sub

        Public Sub BorderedLine(ByVal start_x As Single, ByVal start_y As Single, ByVal end_x As Single, ByVal end_y As Single, ByVal stroke As Single, ByVal brush As Direct2DBrush, ByVal borderBrush As Direct2DBrush)
            Dim geometry = New PathGeometry(factory)

            Dim sink = geometry.Open()

            Dim half As Single = stroke / 2.0F
            Dim quarter As Single = half / 2.0F

            sink.BeginFigure(New RawVector2(start_x, start_y - half), FigureBegin.Filled)

            sink.AddLine(New RawVector2(end_x, end_y - half))
            sink.AddLine(New RawVector2(end_x, end_y + half))
            sink.AddLine(New RawVector2(start_x, start_y + half))

            sink.EndFigure(FigureEnd.Closed)

            sink.Close()

            device.DrawGeometry(geometry, borderBrush, half)

            device.FillGeometry(geometry, brush)

            sink.Dispose()
            geometry.Dispose()
        End Sub

        Public Sub BorderedRectangle(ByVal x As Single, ByVal y As Single, ByVal width As Single, ByVal height As Single, ByVal stroke As Single, ByVal color As Direct2DColor, ByVal borderColor As Direct2DColor)
            Dim half As Single = stroke / 2.0F

            width += x
            height += y

            sharedBrush.Color = color

            device.DrawRectangle(New RawRectangleF(x, y, width, height), sharedBrush, half)

            sharedBrush.Color = borderColor

            device.DrawRectangle(New RawRectangleF(x - half, y - half, width + half, height + half), sharedBrush, half)

            device.DrawRectangle(New RawRectangleF(x + half, y + half, width - half, height - half), sharedBrush, half)
        End Sub

        Public Sub BorderedRectangle(ByVal x As Single, ByVal y As Single, ByVal width As Single, ByVal height As Single, ByVal stroke As Single, ByVal brush As Direct2DBrush, ByVal borderBrush As Direct2DBrush)
            Dim half As Single = stroke / 2.0F

            width += x
            height += y

            device.DrawRectangle(New RawRectangleF(x - half, y - half, width + half, height + half), borderBrush, half)

            device.DrawRectangle(New RawRectangleF(x + half, y + half, width - half, height - half), borderBrush, half)

            device.DrawRectangle(New RawRectangleF(x, y, width, height), brush, half)
        End Sub

        Public Sub BorderedCircle(ByVal x As Single, ByVal y As Single, ByVal radius As Single, ByVal stroke As Single, ByVal color As Direct2DColor, ByVal borderColor As Direct2DColor)
            sharedBrush.Color = color

            Dim ellipse = New Ellipse(New RawVector2(x, y), radius, radius)

            device.DrawEllipse(ellipse, sharedBrush, stroke)

            Dim half As Single = stroke / 2.0F

            sharedBrush.Color = borderColor

            ellipse.RadiusX += half
            ellipse.RadiusY += half

            device.DrawEllipse(ellipse, sharedBrush, half)

            ellipse.RadiusX -= stroke
            ellipse.RadiusY -= stroke

            device.DrawEllipse(ellipse, sharedBrush, half)
        End Sub

        Public Sub BorderedCircle(ByVal x As Single, ByVal y As Single, ByVal radius As Single, ByVal stroke As Single, ByVal brush As Direct2DBrush, ByVal borderBrush As Direct2DBrush)
            Dim ellipse = New Ellipse(New RawVector2(x, y), radius, radius)

            device.DrawEllipse(ellipse, brush, stroke)

            Dim half As Single = stroke / 2.0F

            ellipse.RadiusX += half
            ellipse.RadiusY += half

            device.DrawEllipse(ellipse, borderBrush, half)

            ellipse.RadiusX -= stroke
            ellipse.RadiusY -= stroke

            device.DrawEllipse(ellipse, borderBrush, half)
        End Sub

#End Region

#Region "Geometry"

        Public Sub DrawTriangle(ByVal a_x As Single, ByVal a_y As Single, ByVal b_x As Single, ByVal b_y As Single, ByVal c_x As Single, ByVal c_y As Single, ByVal stroke As Single, ByVal brush As Direct2DBrush)
            Dim geometry = New PathGeometry(factory)

            Dim sink = geometry.Open()

            sink.BeginFigure(New RawVector2(a_x, a_y), FigureBegin.Hollow)
            sink.AddLine(New RawVector2(b_x, b_y))
            sink.AddLine(New RawVector2(c_x, c_y))
            sink.EndFigure(FigureEnd.Closed)

            sink.Close()

            device.DrawGeometry(geometry, brush, stroke)

            sink.Dispose()
            geometry.Dispose()
        End Sub

        Public Sub DrawTriangle(ByVal a_x As Single, ByVal a_y As Single, ByVal b_x As Single, ByVal b_y As Single, ByVal c_x As Single, ByVal c_y As Single, ByVal stroke As Single, ByVal color As Direct2DColor)
            sharedBrush.Color = color

            Dim geometry = New PathGeometry(factory)

            Dim sink = geometry.Open()

            sink.BeginFigure(New RawVector2(a_x, a_y), FigureBegin.Hollow)
            sink.AddLine(New RawVector2(b_x, b_y))
            sink.AddLine(New RawVector2(c_x, c_y))
            sink.EndFigure(FigureEnd.Closed)

            sink.Close()

            device.DrawGeometry(geometry, sharedBrush, stroke)

            sink.Dispose()
            geometry.Dispose()
        End Sub

        Public Sub FillTriangle(ByVal a_x As Single, ByVal a_y As Single, ByVal b_x As Single, ByVal b_y As Single, ByVal c_x As Single, ByVal c_y As Single, ByVal brush As Direct2DBrush)
            Dim geometry = New PathGeometry(factory)

            Dim sink = geometry.Open()

            sink.BeginFigure(New RawVector2(a_x, a_y), FigureBegin.Filled)
            sink.AddLine(New RawVector2(b_x, b_y))
            sink.AddLine(New RawVector2(c_x, c_y))
            sink.EndFigure(FigureEnd.Closed)

            sink.Close()

            device.FillGeometry(geometry, brush)

            sink.Dispose()
            geometry.Dispose()
        End Sub

        Public Sub FillTriangle(ByVal a_x As Single, ByVal a_y As Single, ByVal b_x As Single, ByVal b_y As Single, ByVal c_x As Single, ByVal c_y As Single, ByVal color As Direct2DColor)
            sharedBrush.Color = color

            Dim geometry = New PathGeometry(factory)

            Dim sink = geometry.Open()

            sink.BeginFigure(New RawVector2(a_x, a_y), FigureBegin.Filled)
            sink.AddLine(New RawVector2(b_x, b_y))
            sink.AddLine(New RawVector2(c_x, c_y))
            sink.EndFigure(FigureEnd.Closed)

            sink.Close()

            device.FillGeometry(geometry, sharedBrush)

            sink.Dispose()
            geometry.Dispose()
        End Sub

#End Region

#Region "Special"

        Public Sub DrawBox2D(ByVal x As Single, ByVal y As Single, ByVal width As Single, ByVal height As Single, ByVal stroke As Single, ByVal interiorColor As Direct2DColor, ByVal color As Direct2DColor)
            Dim geometry = New PathGeometry(factory)

            Dim sink = geometry.Open()

            sink.BeginFigure(New RawVector2(x, y), FigureBegin.Filled)
            sink.AddLine(New RawVector2(x + width, y))
            sink.AddLine(New RawVector2(x + width, y + height))
            sink.AddLine(New RawVector2(x, y + height))
            sink.EndFigure(FigureEnd.Closed)

            sink.Close()

            sharedBrush.Color = color

            device.DrawGeometry(geometry, sharedBrush, stroke)

            sharedBrush.Color = interiorColor

            device.FillGeometry(geometry, sharedBrush)

            sink.Dispose()
            geometry.Dispose()
        End Sub

        Public Sub DrawBox2D(ByVal x As Single, ByVal y As Single, ByVal width As Single, ByVal height As Single, ByVal stroke As Single, ByVal interiorBrush As Direct2DBrush, ByVal brush As Direct2DBrush)
            Dim geometry = New PathGeometry(factory)

            Dim sink = geometry.Open()

            sink.BeginFigure(New RawVector2(x, y), FigureBegin.Filled)
            sink.AddLine(New RawVector2(x + width, y))
            sink.AddLine(New RawVector2(x + width, y + height))
            sink.AddLine(New RawVector2(x, y + height))
            sink.EndFigure(FigureEnd.Closed)

            sink.Close()

            device.DrawGeometry(geometry, brush, stroke)

            device.FillGeometry(geometry, interiorBrush)

            sink.Dispose()
            geometry.Dispose()
        End Sub

        Public Sub DrawArrowLine(ByVal start_x As Single, ByVal start_y As Single, ByVal end_x As Single, ByVal end_y As Single, ByVal size As Single, ByVal color As Direct2DColor)
            Dim delta_x As Single = If(end_x >= start_x, end_x - start_x, start_x - end_x)
            Dim delta_y As Single = If(end_y >= start_y, end_y - start_y, start_y - end_y)

            Dim length As Single = CSng(Math.Sqrt(delta_x * delta_x + delta_y * delta_y))

            Dim xm As Single = length - size
            Dim xn As Single = xm

            Dim ym As Single = size
            Dim yn As Single = -ym

            Dim sin As Single = delta_y / length
            Dim cos As Single = delta_x / length

            Dim x As Single = xm * cos - ym * sin + end_x
            ym = xm * sin + ym * cos + end_y
            xm = x

            x = xn * cos - yn * sin + end_x
            yn = xn * sin + yn * cos + end_y
            xn = x

            FillTriangle(start_x, start_y, xm, ym, xn, yn, color)
        End Sub

        Public Sub DrawArrowLine(ByVal start_x As Single, ByVal start_y As Single, ByVal end_x As Single, ByVal end_y As Single, ByVal size As Single, ByVal brush As Direct2DBrush)
            Dim delta_x As Single = If(end_x >= start_x, end_x - start_x, start_x - end_x)
            Dim delta_y As Single = If(end_y >= start_y, end_y - start_y, start_y - end_y)

            Dim length As Single = CSng(Math.Sqrt(delta_x * delta_x + delta_y * delta_y))

            Dim xm As Single = length - size
            Dim xn As Single = xm

            Dim ym As Single = size
            Dim yn As Single = -ym

            Dim sin As Single = delta_y / length
            Dim cos As Single = delta_x / length

            Dim x As Single = xm * cos - ym * sin + end_x
            ym = xm * sin + ym * cos + end_y
            xm = x

            x = xn * cos - yn * sin + end_x
            yn = xn * sin + yn * cos + end_y
            xn = x

            FillTriangle(start_x, start_y, xm, ym, xn, yn, brush)
        End Sub

        Public Sub DrawVerticalBar(ByVal percentage As Single, ByVal x As Single, ByVal y As Single, ByVal width As Single, ByVal height As Single, ByVal stroke As Single, ByVal interiorColor As Direct2DColor, ByVal color As Direct2DColor)
            Dim half As Single = stroke / 2.0F
            Dim quarter As Single = half / 2.0F

            sharedBrush.Color = color

            Dim rect = New RawRectangleF(x - half, y - half, x + width + half, y + height + half)

            device.DrawRectangle(rect, sharedBrush, half)

            If percentage = 0.0F Then
                Return
            End If

            rect.Left += quarter
            rect.Right -= width - (width / 100.0F * percentage) + quarter
            rect.Top += quarter
            rect.Bottom -= quarter

            sharedBrush.Color = interiorColor

            device.FillRectangle(rect, sharedBrush)
        End Sub

        Public Sub DrawVerticalBar(ByVal percentage As Single, ByVal x As Single, ByVal y As Single, ByVal width As Single, ByVal height As Single, ByVal stroke As Single, ByVal interiorBrush As Direct2DBrush, ByVal brush As Direct2DBrush)
            Dim half As Single = stroke / 2.0F
            Dim quarter As Single = half / 2.0F

            Dim rect = New RawRectangleF(x - half, y - half, x + width + half, y + height + half)

            device.DrawRectangle(rect, brush, half)

            If percentage = 0.0F Then
                Return
            End If

            rect.Left += quarter
            rect.Right -= width - (width / 100.0F * percentage) + quarter
            rect.Top += quarter
            rect.Bottom -= quarter

            device.FillRectangle(rect, interiorBrush)
        End Sub

        Public Sub DrawHorizontalBar(ByVal percentage As Single, ByVal x As Single, ByVal y As Single, ByVal width As Single, ByVal height As Single, ByVal stroke As Single, ByVal interiorColor As Direct2DColor, ByVal color As Direct2DColor)
            Dim half As Single = stroke / 2.0F

            sharedBrush.Color = color

            Dim rect = New RawRectangleF(x - half, y - half, x + width + half, y + height + half)

            device.DrawRectangle(rect, sharedBrush, stroke)

            If percentage = 0.0F Then
                Return
            End If

            rect.Left += half
            rect.Right -= half
            rect.Top += height - (height / 100.0F * percentage) + half
            rect.Bottom -= half

            sharedBrush.Color = interiorColor

            device.FillRectangle(rect, sharedBrush)
        End Sub

        Public Sub DrawHorizontalBar(ByVal percentage As Single, ByVal x As Single, ByVal y As Single, ByVal width As Single, ByVal height As Single, ByVal stroke As Single, ByVal interiorBrush As Direct2DBrush, ByVal brush As Direct2DBrush)
            Dim half As Single = stroke / 2.0F
            Dim quarter As Single = half / 2.0F

            Dim rect = New RawRectangleF(x - half, y - half, x + width + half, y + height + half)

            device.DrawRectangle(rect, brush, half)

            If percentage = 0.0F Then
                Return
            End If

            rect.Left += quarter
            rect.Right -= quarter
            rect.Top += height - (height / 100.0F * percentage) + quarter
            rect.Bottom -= quarter

            device.FillRectangle(rect, interiorBrush)
        End Sub

        Public Sub DrawCrosshair(ByVal style As CrosshairStyle, ByVal x As Single, ByVal y As Single, ByVal size As Single, ByVal stroke As Single, ByVal color As Direct2DColor)
            sharedBrush.Color = color

            If style = CrosshairStyle.Dot Then
                FillCircle(x, y, size, color)
            ElseIf style = CrosshairStyle.Plus Then
                DrawLine(x - size, y, x + size, y, stroke, color)
                DrawLine(x, y - size, x, y + size, stroke, color)
            ElseIf style = CrosshairStyle.Cross Then
                DrawLine(x - size, y - size, x + size, y + size, stroke, color)
                DrawLine(x + size, y - size, x - size, y + size, stroke, color)
            ElseIf style = CrosshairStyle.Gap Then
                DrawLine(x - size - stroke, y, x - stroke, y, stroke, color)
                DrawLine(x + size + stroke, y, x + stroke, y, stroke, color)

                DrawLine(x, y - size - stroke, x, y - stroke, stroke, color)
                DrawLine(x, y + size + stroke, x, y + stroke, stroke, color)
            ElseIf style = CrosshairStyle.Diagonal Then
                DrawLine(x - size, y - size, x + size, y + size, stroke, color)
                DrawLine(x + size, y - size, x - size, y + size, stroke, color)
            ElseIf style = CrosshairStyle.Swastika Then
                Dim first As New RawVector2(x - size, y)
                Dim second As New RawVector2(x + size, y)

                Dim third As New RawVector2(x, y - size)
                Dim fourth As New RawVector2(x, y + size)

                Dim haken_1 As New RawVector2(third.X + size, third.Y)
                Dim haken_2 As New RawVector2(second.X, second.Y + size)
                Dim haken_3 As New RawVector2(fourth.X - size, fourth.Y)
                Dim haken_4 As New RawVector2(first.X, first.Y - size)

                device.DrawLine(first, second, sharedBrush, stroke)
                device.DrawLine(third, fourth, sharedBrush, stroke)

                device.DrawLine(third, haken_1, sharedBrush, stroke)
                device.DrawLine(second, haken_2, sharedBrush, stroke)
                device.DrawLine(fourth, haken_3, sharedBrush, stroke)
                device.DrawLine(first, haken_4, sharedBrush, stroke)
            End If
        End Sub

        Public Sub DrawCrosshair(ByVal style As CrosshairStyle, ByVal x As Single, ByVal y As Single, ByVal size As Single, ByVal stroke As Single, ByVal brush As Direct2DBrush)
            If style = CrosshairStyle.Dot Then
                FillCircle(x, y, size, brush)
            ElseIf style = CrosshairStyle.Plus Then
                DrawLine(x - size, y, x + size, y, stroke, brush)
                DrawLine(x, y - size, x, y + size, stroke, brush)
            ElseIf style = CrosshairStyle.Cross Then
                DrawLine(x - size, y - size, x + size, y + size, stroke, brush)
                DrawLine(x + size, y - size, x - size, y + size, stroke, brush)
            ElseIf style = CrosshairStyle.Gap Then
                DrawLine(x - size - stroke, y, x - stroke, y, stroke, brush)
                DrawLine(x + size + stroke, y, x + stroke, y, stroke, brush)

                DrawLine(x, y - size - stroke, x, y - stroke, stroke, brush)
                DrawLine(x, y + size + stroke, x, y + stroke, stroke, brush)
            ElseIf style = CrosshairStyle.Diagonal Then
                DrawLine(x - size, y - size, x + size, y + size, stroke, brush)
                DrawLine(x + size, y - size, x - size, y + size, stroke, brush)
            ElseIf style = CrosshairStyle.Swastika Then
                Dim first As New RawVector2(x - size, y)
                Dim second As New RawVector2(x + size, y)

                Dim third As New RawVector2(x, y - size)
                Dim fourth As New RawVector2(x, y + size)

                Dim haken_1 As New RawVector2(third.X + size, third.Y)
                Dim haken_2 As New RawVector2(second.X, second.Y + size)
                Dim haken_3 As New RawVector2(fourth.X - size, fourth.Y)
                Dim haken_4 As New RawVector2(first.X, first.Y - size)

                device.DrawLine(first, second, brush, stroke)
                device.DrawLine(third, fourth, brush, stroke)

                device.DrawLine(third, haken_1, brush, stroke)
                device.DrawLine(second, haken_2, brush, stroke)
                device.DrawLine(fourth, haken_3, brush, stroke)
                device.DrawLine(first, haken_4, brush, stroke)
            End If
        End Sub

        Private swastikaDeltaTimer As New Stopwatch()
        Private rotationState As Single = 0.0F
        Private lastTime As Integer = 0
        Public Sub RotateSwastika(ByVal x As Single, ByVal y As Single, ByVal size As Single, ByVal stroke As Single, ByVal color As Direct2DColor)
            If Not swastikaDeltaTimer.IsRunning Then
                swastikaDeltaTimer.Start()
            End If

            Dim thisTime As Integer = CInt(swastikaDeltaTimer.ElapsedMilliseconds)

            If Math.Abs(thisTime - lastTime) >= 3 Then
                rotationState += 0.1F
                lastTime = CInt(swastikaDeltaTimer.ElapsedMilliseconds)
            End If

            If thisTime >= 1000 Then
                swastikaDeltaTimer.Restart()
            End If

            If rotationState > size Then
                rotationState = size * -1.0F
            End If

            sharedBrush.Color = color

            Dim first As New RawVector2(x - size, y - rotationState)
            Dim second As New RawVector2(x + size, y + rotationState)

            Dim third As New RawVector2(x + rotationState, y - size)
            Dim fourth As New RawVector2(x - rotationState, y + size)

            Dim haken_1 As New RawVector2(third.X + size, third.Y + rotationState)
            Dim haken_2 As New RawVector2(second.X - rotationState, second.Y + size)
            Dim haken_3 As New RawVector2(fourth.X - size, fourth.Y - rotationState)
            Dim haken_4 As New RawVector2(first.X + rotationState, first.Y - size)

            device.DrawLine(first, second, sharedBrush, stroke)
            device.DrawLine(third, fourth, sharedBrush, stroke)

            device.DrawLine(third, haken_1, sharedBrush, stroke)
            device.DrawLine(second, haken_2, sharedBrush, stroke)
            device.DrawLine(fourth, haken_3, sharedBrush, stroke)
            device.DrawLine(first, haken_4, sharedBrush, stroke)
        End Sub

        Public Sub DrawBitmap(ByVal bmp As Direct2DBitmap, ByVal x As Single, ByVal y As Single, ByVal opacity As Single)
            Dim bitmap As Bitmap = bmp
            device.DrawBitmap(bitmap, New RawRectangleF(x, y, x + bitmap.PixelSize.Width, y + bitmap.PixelSize.Height), opacity, BitmapInterpolationMode.Linear)
        End Sub

        Public Sub DrawBitmap(ByVal bmp As Direct2DBitmap, ByVal opacity As Single, ByVal x As Single, ByVal y As Single, ByVal width As Single, ByVal height As Single)
            Dim bitmap As Bitmap = bmp
            device.DrawBitmap(bitmap, New RawRectangleF(x, y, x + width, y + height), opacity, BitmapInterpolationMode.Linear, New RawRectangleF(0, 0, bitmap.PixelSize.Width, bitmap.PixelSize.Height))
        End Sub

#End Region

#Region "Text"

        Public Sub DrawText(ByVal text As String, ByVal x As Single, ByVal y As Single, ByVal font As Direct2DFont, ByVal color As Direct2DColor)
            sharedBrush.Color = color
            device.DrawText(text, text.Length, font, New RawRectangleF(x, y, Single.MaxValue, Single.MaxValue), sharedBrush, DrawTextOptions.NoSnap, MeasuringMode.Natural)
        End Sub

        Public Sub DrawText(ByVal text As String, ByVal x As Single, ByVal y As Single, ByVal font As Direct2DFont, ByVal brush As Direct2DBrush)
            device.DrawText(text, text.Length, font, New RawRectangleF(x, y, Single.MaxValue, Single.MaxValue), brush, DrawTextOptions.NoSnap, MeasuringMode.Natural)
        End Sub

        Public Sub DrawText(ByVal text As String, ByVal x As Single, ByVal y As Single, ByVal fontSize As Single, ByVal font As Direct2DFont, ByVal color As Direct2DColor)
            sharedBrush.Color = color

            Dim layout = New TextLayout(fontFactory, text, font, Single.MaxValue, Single.MaxValue)

            layout.SetFontSize(fontSize, New TextRange(0, text.Length))

            device.DrawTextLayout(New RawVector2(x, y), layout, sharedBrush, DrawTextOptions.NoSnap)

            layout.Dispose()
        End Sub

        Public Sub DrawText(ByVal text As String, ByVal x As Single, ByVal y As Single, ByVal fontSize As Single, ByVal font As Direct2DFont, ByVal brush As Direct2DBrush)
            Dim layout = New TextLayout(fontFactory, text, font, Single.MaxValue, Single.MaxValue)

            layout.SetFontSize(fontSize, New TextRange(0, text.Length))

            device.DrawTextLayout(New RawVector2(x, y), layout, brush, DrawTextOptions.NoSnap)

            layout.Dispose()
        End Sub

        Public Sub DrawTextWithBackground(ByVal text As String, ByVal x As Single, ByVal y As Single, ByVal font As Direct2DFont, ByVal color As Direct2DColor, ByVal backgroundColor As Direct2DColor)
            Dim layout = New TextLayout(fontFactory, text, font, Single.MaxValue, Single.MaxValue)

            'INSTANT VB WARNING: Instant VB cannot determine whether both operands of this division are integer types - if they are then you should use the VB integer division operator:
            Dim modifier As Single = layout.FontSize / 4.0F

            sharedBrush.Color = backgroundColor

            device.FillRectangle(New RawRectangleF(x - modifier, y - modifier, x + layout.Metrics.Width + modifier, y + layout.Metrics.Height + modifier), sharedBrush)

            sharedBrush.Color = color

            device.DrawTextLayout(New RawVector2(x, y), layout, sharedBrush, DrawTextOptions.NoSnap)

            layout.Dispose()
        End Sub

        Public Sub DrawTextWithBackground(ByVal text As String, ByVal x As Single, ByVal y As Single, ByVal font As Direct2DFont, ByVal brush As Direct2DBrush, ByVal backgroundBrush As Direct2DBrush)
            Dim layout = New TextLayout(fontFactory, text, font, Single.MaxValue, Single.MaxValue)

            'INSTANT VB WARNING: Instant VB cannot determine whether both operands of this division are integer types - if they are then you should use the VB integer division operator:
            Dim modifier As Single = layout.FontSize / 4.0F

            device.FillRectangle(New RawRectangleF(x - modifier, y - modifier, x + layout.Metrics.Width + modifier, y + layout.Metrics.Height + modifier), backgroundBrush)

            device.DrawTextLayout(New RawVector2(x, y), layout, brush, DrawTextOptions.NoSnap)

            layout.Dispose()
        End Sub

        Public Sub DrawTextWithBackground(ByVal text As String, ByVal x As Single, ByVal y As Single, ByVal fontSize As Single, ByVal font As Direct2DFont, ByVal color As Direct2DColor, ByVal backgroundColor As Direct2DColor)
            Dim layout = New TextLayout(fontFactory, text, font, Single.MaxValue, Single.MaxValue)

            layout.SetFontSize(fontSize, New TextRange(0, text.Length))

            Dim modifier As Single = fontSize / 4.0F

            sharedBrush.Color = backgroundColor

            device.FillRectangle(New RawRectangleF(x - modifier, y - modifier, x + layout.Metrics.Width + modifier, y + layout.Metrics.Height + modifier), sharedBrush)

            sharedBrush.Color = color

            device.DrawTextLayout(New RawVector2(x, y), layout, sharedBrush, DrawTextOptions.NoSnap)

            layout.Dispose()
        End Sub

        Public Sub DrawTextWithBackground(ByVal text As String, ByVal x As Single, ByVal y As Single, ByVal fontSize As Single, ByVal font As Direct2DFont, ByVal brush As Direct2DBrush, ByVal backgroundBrush As Direct2DBrush)
            Dim layout = New TextLayout(fontFactory, text, font, Single.MaxValue, Single.MaxValue)

            layout.SetFontSize(fontSize, New TextRange(0, text.Length))

            Dim modifier As Single = fontSize / 4.0F

            device.FillRectangle(New RawRectangleF(x - modifier, y - modifier, x + layout.Metrics.Width + modifier, y + layout.Metrics.Height + modifier), backgroundBrush)

            device.DrawTextLayout(New RawVector2(x, y), layout, brush, DrawTextOptions.NoSnap)

            layout.Dispose()
        End Sub

#End Region

#Region "IDisposable Support"
        Private disposedValue As Boolean = False

        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not disposedValue Then
                If disposing Then
                    ' Free managed objects
                End If

                deleteInstance()

                disposedValue = True
            End If
        End Sub
        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub
#End Region
    End Class

    Public Enum CrosshairStyle
        Dot
        Plus
        Cross
        Gap
        Diagonal
        Swastika
    End Enum

    Public Structure Direct2DRendererOptions
        Public Hwnd As IntPtr
        Public VSync As Boolean
        Public MeasureFps As Boolean
        Public AntiAliasing As Boolean
    End Structure

    Public Class Direct2DFontCreationOptions
        Public FontFamilyName As String

        Public FontSize As Single

        Public Bold As Boolean

        Public Italic As Boolean

        Public WordWrapping As Boolean

        Public Function GetStyle() As FontStyle
            If Italic Then
                Return FontStyle.Italic
            End If
            Return FontStyle.Normal
        End Function
    End Class

    Public Structure Direct2DColor
        Public Red As Single
        Public Green As Single
        Public Blue As Single
        Public Alpha As Single

        Public Sub New(ByVal red As Integer, ByVal green As Integer, ByVal blue As Integer)
            Me.Red = red / 255.0F
            Me.Green = green / 255.0F
            Me.Blue = blue / 255.0F
            Alpha = 1.0F
        End Sub

        Public Sub New(ByVal red As Integer, ByVal green As Integer, ByVal blue As Integer, ByVal alpha As Integer)
            Me.Red = red / 255.0F
            Me.Green = green / 255.0F
            Me.Blue = blue / 255.0F
            Me.Alpha = alpha / 255.0F
        End Sub

        Public Sub New(ByVal red As Single, ByVal green As Single, ByVal blue As Single)
            Me.Red = red
            Me.Green = green
            Me.Blue = blue
            Alpha = 1.0F
        End Sub

        Public Sub New(ByVal red As Single, ByVal green As Single, ByVal blue As Single, ByVal alpha As Single)
            Me.Red = red
            Me.Green = green
            Me.Blue = blue
            Me.Alpha = alpha
        End Sub

        Public Shared Widening Operator CType(ByVal color As Direct2DColor) As RawColor4
            Return New RawColor4(color.Red, color.Green, color.Blue, color.Alpha)
        End Operator

        Public Shared Widening Operator CType(ByVal color As RawColor4) As Direct2DColor
            Return New Direct2DColor(color.R, color.G, color.B, color.A)
        End Operator
    End Structure

    Public Class Direct2DBrush
        Public Property Color() As Direct2DColor
            Get
                Return Brush.Color
            End Get
            Set(ByVal value As Direct2DColor)
                Brush.Color = value
            End Set
        End Property

        Public Brush As SolidColorBrush

        Private Sub New()
            Throw New NotImplementedException()
        End Sub

        Public Sub New(ByVal renderTarget As RenderTarget)
            Brush = New SolidColorBrush(renderTarget, Nothing)
        End Sub

        Public Sub New(ByVal renderTarget As RenderTarget, ByVal color As Direct2DColor)
            Brush = New SolidColorBrush(renderTarget, color)
        End Sub

        Protected Overrides Sub Finalize()
            Brush.Dispose()
        End Sub

        Public Shared Widening Operator CType(ByVal brush As Direct2DBrush) As SolidColorBrush
            Return brush.Brush
        End Operator

        Public Shared Widening Operator CType(ByVal brush As Direct2DBrush) As Direct2DColor
            Return brush.Color
        End Operator

        Public Shared Widening Operator CType(ByVal brush As Direct2DBrush) As RawColor4
            Return brush.Color
        End Operator
    End Class

    Public Class Direct2DFont
        Private factory As FontFactory

        Public Font As TextFormat

        Public Property FontFamilyName() As String
            Get
                Return Font.FontFamilyName
            End Get
            Set(ByVal value As String)
                Dim size As Single = FontSize
                'INSTANT VB NOTE: The variable bold was renamed since Visual Basic does not handle local variables named the same as class members well:
                Dim bold_Renamed As Boolean = Bold
                Dim style As FontStyle = If(Italic, FontStyle.Italic, FontStyle.Normal)
                'INSTANT VB NOTE: The variable wordWrapping was renamed since Visual Basic does not handle local variables named the same as class members well:
                Dim wordWrapping_Renamed As Boolean = WordWrapping

                Font.Dispose()

                Font = New TextFormat(factory, value, If(bold_Renamed, FontWeight.Bold, FontWeight.Normal), style, size)
                Font.WordWrapping = If(wordWrapping_Renamed, SharpDX.DirectWrite.WordWrapping.Wrap, SharpDX.DirectWrite.WordWrapping.NoWrap)
            End Set
        End Property

        Public Property FontSize() As Single
            Get
                Return Font.FontSize
            End Get
            Set(ByVal value As Single)
                Dim familyName As String = FontFamilyName
                'INSTANT VB NOTE: The variable bold was renamed since Visual Basic does not handle local variables named the same as class members well:
                Dim bold_Renamed As Boolean = Bold
                Dim style As FontStyle = If(Italic, FontStyle.Italic, FontStyle.Normal)
                'INSTANT VB NOTE: The variable wordWrapping was renamed since Visual Basic does not handle local variables named the same as class members well:
                Dim wordWrapping_Renamed As Boolean = WordWrapping

                Font.Dispose()

                Font = New TextFormat(factory, familyName, If(bold_Renamed, FontWeight.Bold, FontWeight.Normal), style, value)
                Font.WordWrapping = If(wordWrapping_Renamed, SharpDX.DirectWrite.WordWrapping.Wrap, SharpDX.DirectWrite.WordWrapping.NoWrap)
            End Set
        End Property

        Public Property Bold() As Boolean
            Get
                Return Font.FontWeight = FontWeight.Bold
            End Get
            Set(ByVal value As Boolean)
                Dim familyName As String = FontFamilyName
                Dim size As Single = FontSize
                Dim style As FontStyle = If(Italic, FontStyle.Italic, FontStyle.Normal)
                'INSTANT VB NOTE: The variable wordWrapping was renamed since Visual Basic does not handle local variables named the same as class members well:
                Dim wordWrapping_Renamed As Boolean = WordWrapping

                Font.Dispose()

                Font = New TextFormat(factory, familyName, If(value, FontWeight.Bold, FontWeight.Normal), style, size)
                Font.WordWrapping = If(wordWrapping_Renamed, SharpDX.DirectWrite.WordWrapping.Wrap, SharpDX.DirectWrite.WordWrapping.NoWrap)
            End Set
        End Property

        Public Property Italic() As Boolean
            Get
                Return Font.FontStyle = FontStyle.Italic
            End Get
            Set(ByVal value As Boolean)
                Dim familyName As String = FontFamilyName
                Dim size As Single = FontSize
                'INSTANT VB NOTE: The variable bold was renamed since Visual Basic does not handle local variables named the same as class members well:
                Dim bold_Renamed As Boolean = Bold
                'INSTANT VB NOTE: The variable wordWrapping was renamed since Visual Basic does not handle local variables named the same as class members well:
                Dim wordWrapping_Renamed As Boolean = WordWrapping

                Font.Dispose()

                Font = New TextFormat(factory, familyName, If(bold_Renamed, FontWeight.Bold, FontWeight.Normal), If(value, FontStyle.Italic, FontStyle.Normal), size)
                Font.WordWrapping = If(wordWrapping_Renamed, SharpDX.DirectWrite.WordWrapping.Wrap, SharpDX.DirectWrite.WordWrapping.NoWrap)
            End Set
        End Property

        Public Property WordWrapping() As Boolean
            Get
                Return Font.WordWrapping <> SharpDX.DirectWrite.WordWrapping.NoWrap
            End Get
            Set(ByVal value As Boolean)
                Font.WordWrapping = If(value, SharpDX.DirectWrite.WordWrapping.Wrap, SharpDX.DirectWrite.WordWrapping.NoWrap)
            End Set
        End Property

        Private Sub New()
            Throw New NotImplementedException()
        End Sub

        Public Sub New(ByVal font As TextFormat)
            Me.Font = font
        End Sub

        Public Sub New(ByVal factory As FontFactory, ByVal fontFamilyName As String, ByVal size As Single, Optional ByVal bold As Boolean = False, Optional ByVal italic As Boolean = False)
            Me.factory = factory
            Font = New TextFormat(factory, fontFamilyName, If(bold, FontWeight.Bold, FontWeight.Normal), If(italic, FontStyle.Italic, FontStyle.Normal), size)
            Font.WordWrapping = SharpDX.DirectWrite.WordWrapping.NoWrap
        End Sub

        Protected Overrides Sub Finalize()
            Font.Dispose()
        End Sub

        Public Shared Widening Operator CType(ByVal font As Direct2DFont) As TextFormat
            Return font.Font
        End Operator
    End Class

    Public Class Direct2DScene
        Implements IDisposable

        Private privateRenderer As Direct2DRenderer
        Public Property Renderer() As Direct2DRenderer
            Get
                Return privateRenderer
            End Get
            Private Set(ByVal value As Direct2DRenderer)
                privateRenderer = value
            End Set
        End Property

        Private Sub New()
            Throw New NotImplementedException()
        End Sub

        Public Sub New(ByVal renderer As Direct2DRenderer)
            GC.SuppressFinalize(Me)

            Me.Renderer = renderer
            renderer.BeginScene()
        End Sub

        Protected Overrides Sub Finalize()
            Dispose(False)
        End Sub

        Public Shared Widening Operator CType(ByVal scene As Direct2DScene) As Direct2DRenderer
            Return scene.Renderer
        End Operator

#Region "IDisposable Support"
        Private disposedValue As Boolean = False

        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not disposedValue Then
                If disposing Then
                End If

                Renderer.EndScene()

                disposedValue = True
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
        End Sub
#End Region
    End Class

    Public Class Direct2DBitmap
        Private Shared factory As New SharpDX.WIC.ImagingFactory()

        Public SharpDXBitmap As Bitmap

        Private Sub New()

        End Sub

        Public Sub New(ByVal device As RenderTarget, ByVal bytes() As Byte)
            loadBitmap(device, bytes)
        End Sub

        Public Sub New(ByVal device As RenderTarget, ByVal file As String)
            loadBitmap(device, System.IO.File.ReadAllBytes(file))
        End Sub

        Protected Overrides Sub Finalize()
            SharpDXBitmap.Dispose()
        End Sub

        Private Sub loadBitmap(ByVal device As RenderTarget, ByVal bytes() As Byte)
            Dim stream = New MemoryStream(bytes)
            Dim decoder As New SharpDX.WIC.BitmapDecoder(factory, stream, SharpDX.WIC.DecodeOptions.CacheOnDemand)
            Dim frame = decoder.GetFrame(0)
            Dim converter As New SharpDX.WIC.FormatConverter(factory)
            Try
                ' normal ARGB images (Bitmaps / png tested)
                converter.Initialize(frame, SharpDX.WIC.PixelFormat.Format32bppRGBA1010102)
            Catch
                ' falling back to RGB if unsupported
                converter.Initialize(frame, SharpDX.WIC.PixelFormat.Format32bppRGB)
            End Try
            SharpDXBitmap = Bitmap.FromWicBitmap(device, converter)

            converter.Dispose()
            frame.Dispose()
            decoder.Dispose()
            stream.Dispose()
        End Sub

        Public Shared Widening Operator CType(ByVal bmp As Direct2DBitmap) As Bitmap
            Return bmp.SharpDXBitmap
        End Operator
End Class
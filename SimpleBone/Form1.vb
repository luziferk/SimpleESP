Imports System.Runtime.InteropServices
Imports System.Threading
Public Class Form1

    Public Enum WS As Long
        WS_EX_LAYERED = &H80000
        WS_EX_TRANSPARENT = &H20
    End Enum

    Public Enum GWL
        GWL_WNDPROC = -4
        GWL_HINSTANCE = -6
        GWL_HWNDPARENT = -8
        GWL_STYLE = -16
        GWL_EXSTYLE = -20
        GWL_USERDATA = -21
        GWL_ID
    End Enum

    Public Structure RECT
        Public left As Integer
        Public top As Integer
        Public right As Integer
        Public bottom As Integer
    End Structure

    <DllImport("user32.dll")>
    Public Shared Function SetWindowLong(ByVal hWnd As IntPtr, ByVal nIndex As Integer, ByVal dwNewLong As Integer) As IntPtr
    End Function

    <DllImport("user32.dll")>
    Public Shared Function GetWindowRect(ByVal hWnd As IntPtr, <Out> ByRef lpRect As RECT) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    Public windowsRect As RECT
    Public Const PyGame As Integer = &H20DCA48
    Private windowsTitle As String
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If Mem.Initialize("ros", windowsTitle) Then
            GetWindowRect(Mem.m_MainWindowsHandle, windowsRect)

            Me.Top = windowsRect.top
            Me.Left = windowsRect.left
            Me.Width = windowsRect.right - windowsRect.left
            Me.Height = windowsRect.bottom - windowsRect.top
            SetWindowLong(Me.Handle, GWL.GWL_EXSTYLE, WS.WS_EX_LAYERED Or WS.WS_EX_TRANSPARENT)
            Dim gameInfoThread = New Thread(New ThreadStart(AddressOf GameInfo))
            gameInfoThread.IsBackground = True
            gameInfoThread.Start()

        Else
            MsgBox("Please start game first!")
            Application.Exit()
        End If

    End Sub

    Public Shared Function WorldToScreen(ByVal _viewMatrix As Single(), ByVal pos As Vect3, <Out> ByRef screen As Vect2, ByVal windowWidth As Integer, ByVal windowHeight As Integer) As Boolean
        screen = New Vect2

        Dim clipCoords As New Vect4 With {
            .X = CSng(CDbl(pos.X) * CDbl(_viewMatrix(0)) + (CDbl(pos.Y) * CDbl(_viewMatrix(4))) + (CDbl(pos.Z) * CDbl(_viewMatrix(8)))) + CDbl(_viewMatrix(12)),
            .Y = CSng(CDbl(pos.X) * CDbl(_viewMatrix(1)) + (CDbl(pos.Y) * CDbl(_viewMatrix(5))) + (CDbl(pos.Z) * CDbl(_viewMatrix(9)))) + CDbl(_viewMatrix(13)),
            .Z = CSng(CDbl(pos.X) * CDbl(_viewMatrix(2)) + (CDbl(pos.Y) * CDbl(_viewMatrix(6))) + (CDbl(pos.Z) * CDbl(_viewMatrix(10)))) + CDbl(_viewMatrix(14)),
            .W = CSng(CDbl(pos.X) * CDbl(_viewMatrix(3)) + (CDbl(pos.Y) * CDbl(_viewMatrix(7))) + (CDbl(pos.Z) * CDbl(_viewMatrix(11)))) + CDbl(_viewMatrix(15))
        }
        If (CDbl(clipCoords.W) < 0.100000001490116) Then
            Return False
        End If

        Dim NDC As Vect3
        NDC.X = (clipCoords.X / clipCoords.W)
        NDC.Y = (clipCoords.Y / clipCoords.W)
        NDC.Z = (clipCoords.Z / clipCoords.W)

        screen.X = CSng((CDbl(windowWidth / 2) * CDbl(NDC.X)) + (CDbl(NDC.X) + CDbl(windowWidth / 2)))
        screen.Y = CSng(-(CDbl(windowHeight / 2) * CDbl(NDC.Y)) + (CDbl(NDC.Y) + CDbl(windowHeight / 2)))
        Return True
    End Function

    Private Sub GameInfo()
        Dim overlay = New OverlayWindow(windowsRect.left + 7, windowsRect.top, Me.Width, Me.Height)
        Dim rendererOptions = New Direct2DRendererOptions() With {.AntiAliasing = True, .Hwnd = overlay.WindowHandle, .MeasureFps = True, .VSync = False}
        Dim d2d = New Direct2DRenderer(rendererOptions)
        Dim whiteSmoke = d2d.CreateBrush(0, 0, 0, 0)
        Dim blackBrush = d2d.CreateBrush(0, 0, 0, 255)
        Dim redBrush = d2d.CreateBrush(255, 0, 0, 255)
        Dim greenBrush = d2d.CreateBrush(0, 255, 0, 255)
        Dim blueBrush = d2d.CreateBrush(0, 0, 255, 255)
        Dim font = d2d.CreateFont("Consolas", 10)

        Dim m_pWorld = Mem.ReadMemory(Of Integer)(Mem.m_BaseAddress + PyGame + &H410)
        Dim m_pSceneContext = Mem.ReadMemory(Of Integer)(m_pWorld + &H8)
        Dim cameraBase = Mem.ReadMemory(Of Integer)(m_pSceneContext + &H4)

        While True
            d2d.BeginScene()
            d2d.ClearScene()
            Dim viewMatrix = Mem.ReadMatrix(Of Single)(cameraBase + &HC4, 16)
            Dim visibleCount As Integer = Mem.ReadMemory(Of Integer)(m_pWorld + &H278) 'As CModelFactory->CModelSkeletal 0x28 '0x27c
            Dim pLocalModel As Integer = Mem.ReadMemory(Of Integer)(m_pWorld + &H27C)
            'd2d.DrawText(visibleCount.ToString, 50, 300, font, greenBrush)
            'Dim pLocalModel_m_pAnimator As Integer = Mem.ReadMemory(Of Integer)(pLocalModel + &H328) 'CModelSkeletal->WorldAnimator
            'Dim pLocalModel_m_Transform As Single() = Mem.ReadMatrix(Of Single)(pLocalModel + &H3B0, 16) 'CModelSkeletal->D3DXMATRIX
            'Dim pLocalModel_SpaceNode As Integer = Mem.ReadMemory(Of Integer)(pLocalModel + &H1C)
            Dim pSkeletonList As Integer = Mem.ReadMemory(Of Integer)(m_pWorld + &H290) 'As CModelFactory->CModelSkeletal 0x3C 'pSkeletonList 0x28c
            Static printed As Boolean
            If Not printed Then
                printed = True
                Console.WriteLine("World: 0x{0:X}", m_pWorld)
            End If
            Dim screen As Vect2

            For i As Integer = 0 To visibleCount Step 4
                Dim r_pModel As Integer = Mem.ReadMemory(Of Integer)(pSkeletonList + i)

                Dim SpaceNode As Integer = Mem.ReadMemory(Of Integer)(r_pModel + &H1C)
                Dim m_pAnimator As Integer = Mem.ReadMemory(Of Integer)(r_pModel + &H328) 'CModelSkeletal->WorldAnimator
                Dim m_Position As Single() = Mem.ReadMatrix(Of Single)(r_pModel + &H3B0, 16)

                Dim m_pModelName As String = Mem.ReadString(Mem.ReadMemory(Of Integer)(m_pAnimator + &H528), 30)
                Dim typeName As String = "Unknow"
                Dim isPlayer As Boolean = False
                If m_pModelName.Contains("dataosha_male") Then 'dataosha_male, dataosha_femal
                    typeName = "Player"
                    isPlayer = True
                ElseIf m_pModelName.Contains("dataosha_female") Then 'vehicle
                    typeName = "Player"
                    isPlayer = True
                    'Continue For
                ElseIf m_pModelName.Contains("roadster") Then 'vehicle
                    'Continue For
                    typeName = "Car"
                ElseIf m_pModelName.Contains("jeep") Then 'item\miz
                    typeName = "Jeep"
                    'Continue For
                ElseIf m_pModelName.Contains("door") Then 'item\dts
                    Continue For
                ElseIf m_pModelName.Contains("mizang") Then 'Heritage
                    typeName = "Her"
                    'Continue For
                ElseIf m_pModelName.Contains("halei") Then 'Motor
                    typeName = "Motor"
                    'Continue For
                ElseIf m_pModelName.Contains("buggy") Then 'Buggy
                    typeName = "Car"
                    'Continue For
                ElseIf m_pModelName.Contains("express") Then
                    'typeName = m_pModelName & ", " & Hex(pSkeletonList + i)
                    typeName = "Express"
                    'Continue For
                ElseIf m_pModelName.Contains("crusher") Then
                    typeName = "Car"
                    'Continue For
                ElseIf m_pModelName.Contains("bike") Then
                    typeName = "Bike"
                    'Continue For
                ElseIf m_pModelName.Contains("T$$") Then
                    Continue For
                ElseIf m_pModelName.Contains("plane") Then
                    typeName = "Plane"
                ElseIf m_pModelName.Contains("parachute") Then
                    typeName = "Parachute"
                    Continue For
                Else

                End If

                Dim position As Vect3
                position.X = m_Position(12)
                position.Y = m_Position(13)
                position.Z = m_Position(14)

                'Dim vMax As Vect3 = Mem.ReadMemory(Of Vect3)(SpaceNode + &H1E0)
                'Dim vMin As Vect3 = Mem.ReadMemory(Of Vect3)(SpaceNode + &H1EC)

                If WorldToScreen(viewMatrix, position, screen, Me.Width, Me.Height) Then
                    d2d.DrawText(typeName, screen.X, screen.Y, font, redBrush)
                    If isPlayer Then
                        d2d.DrawLine(CSng(Me.Width / 2), Me.Height, screen.X, screen.Y, 1, greenBrush)
                    End If
                End If
            Next
            d2d.EndScene()


        End While

    End Sub
End Class

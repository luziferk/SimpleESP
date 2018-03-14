Imports System.Runtime.InteropServices
<StructLayout(LayoutKind.Sequential)>
Public Structure Vect2
    Public X As Single
    Public Y As Single
    Public Sub New(ByVal X As Single, ByVal Y As Single)
        X = X
        Y = Y
    End Sub
End Structure

<StructLayout(LayoutKind.Sequential)>
Public Structure Vect3
    Public X As Single
    Public Y As Single
    Public Z As Single
    Public Sub New(ByVal X As Single, ByVal Y As Single, ByVal Z As Single)
        X = X
        Y = Y
        Z = Z
    End Sub
End Structure

<StructLayout(LayoutKind.Sequential)>
Public Structure Vect4
    Public X As Single
    Public Y As Single
    Public Z As Single
    Public W As Single
    Public Sub New(ByVal X As Single, ByVal Y As Single, ByVal Z As Single, ByVal W As Single)
        X = X
        Y = Y
        Z = Z
        W = W
    End Sub
End Structure


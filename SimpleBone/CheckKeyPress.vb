Imports System.Runtime.InteropServices

Public Enum KeyState As Integer
    None = 0
    Press = 1
    Release = 2
End Enum
Public Class CheckKeyPress

    <DllImport("User32.dll")>
    Public Shared Function GetAsyncKeyState(ByVal vKey As Keys) As Short
    End Function

    'Private isPressed As Boolean

    Private registeredKey As New Dictionary(Of Keys, Boolean)

    Public Function getKeyPress(ByVal key As Keys) As KeyState
        If Not registeredKey.ContainsKey(key) Then
            registeredKey.Add(key, False)
        End If
        If (GetAsyncKeyState(key) <> 0) Then

            If Not registeredKey(key) Then
                registeredKey(key) = True
                'Console.WriteLine("Pressed")
                Return KeyState.Press
            End If

        ElseIf registeredKey(key) Then
            'Console.WriteLine("Release")
            registeredKey(key) = False
            Return KeyState.Release
        End If
        Return KeyState.None
    End Function

End Class

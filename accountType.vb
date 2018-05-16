Public Class accountType
    Property accountName As String
    Property user As String
    Property secret As String
    Property isStart As Boolean
    Property sqlId As Integer
    Property port As Integer
    Property autoStart As Boolean

    Public Sub New()
    End Sub

End Class
Public Class accountTypeJSON
    Property accountList As accountType
End Class
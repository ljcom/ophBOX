Public Class accountType
    Property accountName As String
    Property user As String
    Property secret As String
    Property sqlId As Integer
    Public Sub New()
    End Sub

End Class
Public Class accountTypeJSON
    Property accountList As accountType
End Class
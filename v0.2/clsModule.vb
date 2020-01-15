Public Class clsModule
    Public Sub New(ByVal moduleGUID As String, ByVal moduleId As String)
        _moduleGUID = moduleGUID
        _moduleId = moduleId
    End Sub

    Private _moduleGUID As String
    Public Property ModuleGUID() As String
        Get
            Return _moduleGUID
        End Get
        Set(ByVal value As String)
            _moduleGUID = value
        End Set
    End Property

    Private _moduleId As String
    Public Property ModuleId() As String
        Get
            Return _moduleId
        End Get
        Set(ByVal value As String)
            _moduleId = value
        End Set
    End Property

End Class

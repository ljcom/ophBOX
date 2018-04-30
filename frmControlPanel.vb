Imports System.Data
Imports System.Data.Sql
Imports System.Net
Imports System.IO
Imports System.Diagnostics
Imports System.ComponentModel
Imports System.Text
Imports System.DirectoryServices.AccountManagement
Imports System.DirectoryServices.ActiveDirectory.Domain
Imports System.Threading

Public Class frmControlPanel

    Function StartSqlCmdConsole() As Boolean
        Dim p As Process = New Process()
        p.StartInfo.UseShellExecute = False
        p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.RedirectStandardError = True
        p.EnableRaisingEvents = True
        'Application.DoEvents()
        AddHandler p.ErrorDataReceived, AddressOf OutputDataReceived
        AddHandler p.OutputDataReceived, AddressOf OutputDataReceived

        p.StartInfo.FileName = "cmd.exe"
        'p.StartInfo.Arguments = "-S " & pipename
        p.StartInfo.CreateNoWindow = True
        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden


        p.Start()

        p.BeginErrorReadLine()
        p.BeginOutputReadLine()


    End Function

    Public Sub OutputDataReceived(ByVal sender As Object, ByVal e As DataReceivedEventArgs)

        Dim t = IIf(e.Data = "", "", Now() & " " & e.Data & vbCrLf)
        'lastMessage = lastMessage & t

        If Me.InvokeRequired = True Then
            'Me.Invoke(myDelegate, e.Data)
            Me.Invoke(New Action(Sub() Me.tbLog.AppendText(t)))
        Else
            'UpdateTextBox(e.Data)
            Me.tbLog.AppendText(t)
        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        StartSqlCmdConsole()
    End Sub
End Class
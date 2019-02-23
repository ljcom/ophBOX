Imports System.Collections.Generic
Imports System.IO

Imports ophBox.functions

Public Class frmMenu
    Dim lastCmd As Long
    Private accountList As New Dictionary(Of String, accountType)

    Private Sub Button1_Click_1(sender As Object, e As EventArgs)

    End Sub

    Private Sub btnInit_Click(sender As Object, e As EventArgs) Handles btnInit.Click
        lastCmd = 0
        clearOptions()
        Dim accountName = "oph_core"
        Dim curAccount = accountList(accountName)
        Dim ophPath = IIf(My.Settings.isIISExpress = 1 Or My.Settings.OPHPath = "", Directory.GetCurrentDirectory, My.Settings.OPHPath)
        Dim folder = "", folderData = "", folderTemp = ""
        CreateAccount(accountName, True, curAccount, ophPath, folder, folderData, folderTemp)
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        lastCmd = 1
        Me.lblSecret.Visible = True
        Me.lblUserName.Visible = True
        Me.tbSecret.Visible = True
        Me.tbUserName.Visible = True
        Me.btnInit2.Visible = True
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        lastCmd = 2
        Me.lblSecret.Visible = True
        Me.lblUserName.Visible = True
        Me.tbSecret.Visible = True
        Me.tbUserName.Visible = True
        Me.btnInit2.Visible = True
    End Sub

    Private Sub btnInit2_Click(sender As Object, e As EventArgs) Handles btnInit2.Click
        If lastCmd = 0 Then
            'init
        ElseIf lastCmd = 1 Then
            'clone
        Else
            'sync
        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        clearOptions()
        Dim o = New frmOptions()
        o.ShowDialog(Me)
    End Sub
    Sub clearOptions()
        Me.lblSecret.Visible = False
        Me.lblUserName.Visible = False
        Me.tbSecret.Visible = False
        Me.tbUserName.Visible = False
        Me.btnInit2.Visible = False
    End Sub
End Class
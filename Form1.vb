Imports System
Imports System.Data
Imports System.Data.Sql
Imports System.Net
Imports System.IO
Imports System.Diagnostics
Imports System.ComponentModel

Public Class Form1
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles Me.Load

        'If checkInstance("OPERAHOUSE") <> "OPERAHOUSE" Then
        '    installLocalDB()
        '    setLog("localDB installed")
        '    createInstance("OPERAHOUSE")
        '    setLog("OPERAHOUSE created")
        'End If
        'startInstance("OPERAHOUSE")
        'setLog("OPERAHOUSE started")
        'Dim pipename = getPipeName("OPERAHOUSE")
        'setLog("Pipename: " & pipename)

        'Dim iisexpress = getIISLocation()
        'setLog("IIS Express Location: " & iisexpress)

        'If Not localScript("use oph_core", "oph_core", pipename) Then
        '    localScript("create database oph_core", "oph_core", pipename)
        'End If
        ''--always check new update'
        'Dim url = "http://redbean/oph/ophcore/api/sync.aspx?mode=reqcorescript"
        'Dim scriptFile = Directory.GetCurrentDirectory & "\temp\install.sql"
        'runScript(url, pipename, scriptFile, "oph_core")
    End Sub
    Private Sub Form1_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        'stopInstance("OPERAHOUSE")
    End Sub


    Private Sub Button2_Click(sender As Object, e As EventArgs)
        'installLocalDB()
    End Sub


End Class
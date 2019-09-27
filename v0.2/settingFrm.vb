﻿Imports System.IO
Imports System.IO.Compression
Imports System.Net
Imports ophBox.FunctionList

Public Class settingFrm
    Dim f As FunctionList = FunctionList.Instance
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim ophPath = Me.TextBox1.Text
        Dim p_uri = Me.TextBox2.Text 'need to create db
        Dim errStr = ""

        If ophPath = "" Or p_uri = "" Then
            MessageBox.Show("Please fill all blanks before continue.", "Warning")
        Else
            My.Settings.ophFolder = TextBox1.Text
            My.Settings.ophServer = TextBox2.Text
            My.Settings.localServer = Replace(Me.TextBox3.Text, ".", "(local)")
            My.Settings.localUserID = Me.TextBox4.Text
            My.Settings.LocalPwd = Me.TextBox5.Text
            My.Settings.isIISExpress = Me.CheckBox1.Checked
            My.Settings.IISPort = Me.TextBox6.Text

            My.Settings.Save()
        End If

        If Not Directory.Exists(Me.TextBox1.Text & "\OPERAHOUSE") Then

            If MessageBox.Show("Folder not exists, we will setup the OPERAHOUSE folder. Continue?", "Start", MessageBoxButtons.OKCancel) = vbOK Then
                If Not Directory.Exists(Me.TextBox1.Text) Then Directory.CreateDirectory(Me.TextBox1.Text)
                If Not Directory.Exists(Me.TextBox1.Text & "\temp") Then Directory.CreateDirectory(Me.TextBox1.Text & "\temp")
                If Not Directory.Exists(Me.TextBox1.Text & "\data") Then Directory.CreateDirectory(Me.TextBox1.Text & "\data")
                Dim gitLoc = getGITLocation(Me.TextBox1.Text)

                Dim localFile1 = ophPath & "\temp\sync.zip"

                If Not File.Exists(localFile1) Then
                    Dim url = p_uri & "/oph/ophcore/api/sync.aspx?mode=webrequestFile"
                    If f.downloadFilename(url, localFile1) Then
                        f.unZip(localFile1, ophPath & "\temp")
                    Else
                        errStr = "Download is Failed."
                    End If
                End If
                If errStr = "" Then
                    f.runCmd(ophPath & "\temp" & "\build-oph.bat", ophPath)
                    If Not Directory.Exists(ophPath & "\operahouse") Then
                        errStr = "Folder Building is failed"
                    End If
                End If
            End If
        End If

        If errStr = "" Then
            If My.Settings.isIISExpress Then
                Dim iisExpressFolder = getIISLocation(My.Settings.ophFolder)
                f.SetLog("IIS Express Location: " & iisExpressFolder)
                'run iis
                'If iisid = 0 And iisExpressFolder <> "" Then f.runIIS(dataaccount)
            End If

            f.SetLog("Checking IIS Files...")
            Dim dataaccount = "oph"
            Dim port = My.Settings.IISPort
            Dim folderdata = "data"
            Dim foldertemp = "temp"
            f.addAccounttoIIS(dataaccount, ophPath & "\", port, folderdata, foldertemp, False)
            Dim isReady = f.addWebConfig(ophPath & "\")
            f.SetLog("Checking IIS Files completed.")
            If Not isReady Then errStr = "IIS is not set properly."
        End If

        If errStr = "" Then
            Dim pipename = My.Settings.localServer
            Dim uid = My.Settings.localUserID
            Dim pwd = My.Settings.LocalPwd
            Dim odbc = "Data Source=" & pipename & ";Initial Catalog=master;User Id=" & uid & ";password=" & pwd
            Dim ophcore = f.runSQLwithResult("select name from sys.databases where name='oph_core'", odbc)
            If ophcore = "" Then
                Dim tuser = "sam"
                Dim secret = "D627AFEB-9D77-40E4-B060-7C976DA05260"

                If f.createServer(pipename, uid, pwd, tuser, secret, ophPath, My.Settings.ophServer) Then
                    'add to tree
                    Dim isexists = False
                    For Each n In mainFrm.TreeView1.Nodes(0).Nodes
                        If n.text = pipename Then
                            isexists = True
                        End If
                    Next
                    If Not isexists Then
                        Dim x = mainFrm.TreeView1.Nodes(0).Nodes.Add(pipename)
                        x.Tag = "type=2;server=" & pipename & ";uid=" & uid & ";pwd=" & pwd
                        Dim y = x.Nodes.Add("oph")
                        y.Tag = "type=3;dbname=oph_core"
                    End If
                Else
                    errStr = "Local Server is NOT setup properly."
                End If
            End If
        End If

        If errStr = "" Then
            Me.Close()
        Else
            MessageBox.Show(errStr, "Error", MessageBoxButtons.OK)
        End If
    End Sub


    Private Sub settingFrm_Load(sender As Object, e As EventArgs) Handles Me.Load
        Me.TextBox1.Text = My.Settings.ophFolder
        Me.TextBox2.Text = My.Settings.ophServer
        Me.TextBox3.Text = My.Settings.localServer
        Me.TextBox4.Text = My.Settings.localUserID
        Me.TextBox5.Text = My.Settings.LocalPwd
        Me.TextBox6.Text = My.Settings.IISPort
        Me.CheckBox1.Checked = My.Settings.isIISExpress

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Me.Close()
    End Sub

    Private Sub settingFrm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        e.Cancel = cancelAction()
    End Sub
    Function cancelAction()
        Dim cancel = True
        If Not Directory.Exists(Me.TextBox1.Text & "\OPERAHOUSE") Then
            If MessageBox.Show("You need to add your folder or the application will be exit. Do you want to exit?", "Exit", MessageBoxButtons.YesNo) = vbYes Then
                cancel = False
            End If
        Else
            cancel = False
        End If
        Return cancel
    End Function

    Function getGITLocation(ophPath As String) As String
        'Dim ophPath = IIf(My.Settings.isIISExpress = 1 Or My.Settings.OPHPath = "", Directory.GetCurrentDirectory, My.Settings.OPHPath)
        Dim r = "C:\Program Files\GIT\git-bash.exe"
        If r = "" Or Not File.Exists(r) Then
            Dim c = MsgBox("We cannot find GIT. Press Yes to location it for us. Press No to install from our repository or Cancel do it later.", vbYesNoCancel, "GIT")
            If c = vbYes Then
                Dim folder = Me.FolderBrowserDialog1.ShowDialog()
                If File.Exists(folder & "\git-bash.exe") Then
                    r = folder & "\git-bash.exe"
                End If
            ElseIf c = vbNo Then
                f.installGIT(ophPath & "\temp", ophPath & "\data")
                MessageBox.Show("When the GIT has been installed, please press OK to continue.")
                f.SetLog("GIT Installed")
            End If
        End If

        Return r
    End Function
    Function getIISLocation(ophpath) As String
        Dim folderTemp = "temp"
        Dim folderData = "data"
        Dim r = ""
        'If My.Settings.isIISExpress Then
        'Dim ophPath = IIf(My.Settings.isIISExpress = 1 Or My.Settings.OPHPath = "", Directory.GetCurrentDirectory, My.Settings.OPHPath)
        r = My.Settings.IISExpressLocation
        If r = "" Or Not File.Exists(r) Then
            r = f.findFile("C:\Program Files\IIS Express", "iisexpress.exe")
            If r = "" Then
                If MessageBox.Show(Me, "We cannot find IIS Express. We are about to install from our repository.", "IIS Express", vbYesNo) = vbYes Then
                    Dim b = f.installIIS(ophpath & "\" & folderTemp, ophpath & "\" & folderData)
                    r = f.findFile("C:\Program Files\IIS Express", "iisexpress.exe")
                    If r <> "" Then f.SetLog("IIS Express Installed")
                End If
            End If
            r = f.findFile("C:\Program Files\IIS Express", "iisexpress.exe")
            If r <> "" Then My.Settings.IISExpressLocation = r
        End If
            'Else
            'r = True
            'End If
            Return r
    End Function

    Private Sub TextBox3_TextChanged(sender As Object, e As EventArgs) Handles TextBox3.TextChanged
        If Me.TextBox3.Text.IndexOf(".") >= 0 Then
            Me.TextBox3.Text = Me.TextBox3.Text.Replace(".", "(local)")
        End If

    End Sub
End Class
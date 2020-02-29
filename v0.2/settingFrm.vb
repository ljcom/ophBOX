Imports System.IO
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
            My.Settings.isLocalServer = Me.CheckBox2.Checked
            My.Settings.IISPort = Me.TextBox6.Text
            My.Settings.isSQLAuth = Me.RadioButton2.Checked
            My.Settings.Save()
        End If

        If Not Directory.Exists(Me.TextBox1.Text & "\OPERAHOUSE") Then
            Dim r = MessageBox.Show("Folder not exists, we will setup the OPERAHOUSE folder. YES to Continue. No to skip. Cancel to exit.", "Start", MessageBoxButtons.YesNoCancel)
            If r = vbYes Then
                If Not Directory.Exists(Me.TextBox1.Text) Then Directory.CreateDirectory(Me.TextBox1.Text)
                If Not Directory.Exists(Me.TextBox1.Text & "\temp") Then Directory.CreateDirectory(Me.TextBox1.Text & "\temp")
                If Not Directory.Exists(Me.TextBox1.Text & "\data") Then Directory.CreateDirectory(Me.TextBox1.Text & "\data")
                'Dim gitLoc = getGITLocation(Me.TextBox1.Text)

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
                    f.runCmd(ophPath & "\temp" & "\build-oph.bat", ophPath, True)
                    If Not Directory.Exists(ophPath & "\operahouse") Then
                        errStr = "Folder Building is failed"
                    Else
                        'add bin
                        'For Each l In Directory.GetFiles(ophPath & "\temp", "*.dll")
                        'Dim file = Path.GetFileName(l)
                        'FileCopy(l, ophPath & "\operahouse\core\bin")
                        'Next

                    End If
                End If
            ElseIf r = vbNo Then
                Directory.CreateDirectory(Me.TextBox1.Text & "\OPERAHOUSE")
            End If
        End If

        If errStr = "" Then
            If My.Settings.isLocalServer Then
                Dim pipename = My.Settings.localServer
                Dim uid = My.Settings.localUserID
                Dim pwd = My.Settings.LocalPwd
                Dim odbc = "Data Source=" & pipename & ";Initial Catalog=master;" & IIf(uid <> "", "User Id=" & uid & ";password=" & pwd, "trusted connection=yes")
                Dim ophcore = f.runSQLwithResult("select name from sys.databases where name='oph_core'", odbc)
                Dim iisport = Me.TextBox6.Text
                Dim accountid = "oph"
                Dim coredb = "oph_core"
                Dim curnode = mainFrm.TreeView1.SelectedNode

                'If ophcore = "" Then
                '    Dim tuser = "sam"
                '    Dim secret = "D627AFEB-9D77-40E4-B060-7C976DA05260"

                '    If f.createServer(pipename, uid, pwd, tuser, secret, ophPath, My.Settings.ophServer) Then
                '        'add to tree
                '        odbc = "Data Source=" & pipename & ";Initial Catalog=oph_core;User Id=" & uid & ";password=" & pwd
                '        f.runSQLwithResult("
                '             update i
                '             set infovalue=infovalue+';localhost:" & iisport & "/{accountid}'
                '             --select i.* 
                '             from acct a
                '              inner join acctinfo i on a.AccountGUID=i.AccountGUID
                '             where accountid='" & accountid & "' and i.InfoKey like '%address' and infovalue not like '%localhost:" & iisport & "/{accountid}%'

                '             insert into acctinfo (accountguid, infokey, infovalue)
                '             select a.accountguid, 'address', 'localhost:" & iisport & "/{accountid}' 
                '             from acct a
                '              left join acctinfo i on a.AccountGUID=i.AccountGUID and i.infokey='address'
                '             where accountid='" & accountid & "' and i.AccountInfoGUID is null

                '             insert into acctinfo (accountguid, infokey, infovalue)
                '             select a.accountguid, 'whiteaddress', 'localhost:" & iisport & "/{accountid}' 
                '             from acct a
                '              left join acctinfo i on a.AccountGUID=i.AccountGUID and i.infokey='whiteaddress'
                '             where accountid='" & accountid & "' and i.AccountInfoGUID is null

                '             insert into acctinfo (accountguid, infokey, infovalue)
                '             select a.accountguid, 'odbc', 'Data Source=" & pipename & ";Initial Catalog=oph_core;User Id=" & uid & ";password=" & pwd & "' 
                '             from acct a
                '              left join acctinfo i on a.AccountGUID=i.AccountGUID and i.infokey='odbc'
                '             where accountid='" & accountid & "' and i.AccountInfoGUID is null

                '            ", odbc)
                '    Else
                '        errStr = "Local Server is NOT setup properly."
                '    End If
                'End If
                'odbc = "Data Source=" & pipename & ";Initial Catalog=master;User Id=" & uid & ";password=" & pwd
                'ophcore = f.runSQLwithResult("select name from sys.databases where name='oph_core'", odbc)
                'If ophcore <> "" Then
                Dim isexists = False
                For Each n In mainFrm.TreeView1.Nodes(0).Nodes
                    If n.text = pipename Then
                        isexists = True
                    End If
                Next
                If Not isexists Then
                    '        Dim x = mainFrm.TreeView1.Nodes(0).Nodes.Add(pipename)
                    '        x.Tag = "type=2;mode=instance;server=" & pipename & ";uid=" & uid & ";pwd=" & pwd & ";port=" & Me.TextBox6.Text
                    '        Dim y = x.Nodes.Add("oph")
                    '        y.Tag = "type=3;dbname=oph_core"
                    '    End If
                    If IsNothing(curnode) Then curnode = mainFrm.TreeView1.Nodes(0)
                    f.addInstance(pipename, uid, pwd, coredb, iisport, ophPath, curnode)
                End If

            End If
        End If

        If errStr = "" Then
            If My.Settings.isIISExpress Then
                Dim iisExpressFolder = getIISLocation(My.Settings.ophFolder)
                f.SetLog("IIS Express Location: " & iisExpressFolder)
                'run iis
                'If iisid = 0 And iisExpressFolder <> "" Then f.runIIS(dataaccount)

                f.SetLog("Checking IIS Files...")
                Dim dataaccount = "oph"
                Dim port = My.Settings.IISPort
                f.addAccounttoIIS(dataaccount, My.Settings.localServer, ophPath & "\", port, False)
                Dim isReady = f.addWebConfig(ophPath & "\")
                f.SetLog("Checking IIS Files completed.")
                If Not isReady Then errStr = "IIS is not set properly."
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
        Me.CheckBox2.Checked = My.Settings.isLocalServer
        If My.Settings.isSQLAuth Then
            Me.RadioButton2.Checked = True
        Else
            Me.RadioButton1.Checked = True
        End If
        Me.TextBox1.Select()

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

    Private Sub RadioButton1_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton1.CheckedChanged
        'Me.RadioButton1.Checked = True
        'Me.RadioButton2.Checked = False
        Me.TextBox4.Enabled = False
        Me.TextBox5.Enabled = False
    End Sub

    Private Sub RadioButton2_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton2.CheckedChanged
        'Me.RadioButton1.Checked = False
        'Me.RadioButton2.Checked = True
        Me.TextBox4.Enabled = True
        Me.TextBox5.Enabled = True

    End Sub

    Private Sub CheckBox2_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox2.CheckedChanged
        Me.TextBox3.Enabled = CheckBox2.Checked
        Me.TextBox4.Enabled = CheckBox2.Checked
        Me.TextBox5.Enabled = CheckBox2.Checked
        Me.RadioButton1.Enabled = CheckBox2.Checked
        Me.RadioButton2.Enabled = CheckBox2.Checked
    End Sub
End Class
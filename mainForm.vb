Imports System.Net
Imports System.IO
Imports System.Diagnostics
Imports System.Text
Imports System.Windows.Forms
Imports System.Collections.Generic
Imports Newtonsoft.Json.Linq
Imports System.Data.SqlClient
Imports System.Data
Imports System.Drawing

Public Class mainForm
    Private isIISExpress = False
    Private Const folderTemp = "temp"
    Private Const folderData = "data"
    Private pipename As String = ""
    Private eventHandled As Boolean = False
    Private elapsedTime As Integer
    Private iisExpressFolder
    Private iisId As Integer = 0
    Private accountList As New Dictionary(Of String, accountType)

    Private Sub mainForm_Load(sender As Object, e As EventArgs) Handles Me.Load
        Dim jsonText = My.Settings.SavedAccountList

        isIISExpress = My.Settings.isIISExpress

        If Directory.Exists(Directory.GetCurrentDirectory & "\" & folderData & "") Then
            Dim json As JObject = JObject.Parse(jsonText)
            Dim al = json("accountList")
            For Each x In al
                Dim an = x("accountName").ToString
                Me.lbAcount.Items.Add(an)
                If an <> "" Then
                    accountList.Add(an, New accountType With {.user = x("user").ToString, .secret = x("secret").ToString, .sqlId = 0, .port = x("port").ToString, .autoStart = x("autoStart") = "1", .isStart = x("autoStart") = "1"})
                End If
            Next
            Me.Timer1.Enabled = True
        End If

    End Sub

    Private Sub Form1_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Resize
        If Me.WindowState = FormWindowState.Minimized Then
            NotifyIcon1.Visible = True
            NotifyIcon1.Icon = SystemIcons.Application
            NotifyIcon1.BalloonTipIcon = ToolTipIcon.Info
            NotifyIcon1.BalloonTipTitle = "OPHBOX"
            NotifyIcon1.BalloonTipText = "Click here to zoom out!"
            NotifyIcon1.ShowBalloonTip(50000)
            'Me.Hide()
            ShowInTaskbar = False
        End If
    End Sub

    Private Sub NotifyIcon1_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles NotifyIcon1.DoubleClick
        'Me.Show()
        ShowInTaskbar = True
        Me.WindowState = FormWindowState.Normal
        NotifyIcon1.Visible = False
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If Not IsNothing(Me.lbAcount.SelectedItem) Then
            Dim curAccount = accountList(Me.lbAcount.SelectedItem)
            curAccount.isStart = True
            'startSync(Me.lbAcount.SelectedItem)
        Else
            MessageBox.Show(Me, "Please select one of account to start before continue.", "Select Account", vbInformation)
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        If Me.lbAcount.SelectedItem <> "" Then
            Dim curAccount = accountList(Me.lbAcount.SelectedItem)
            'Dim sqlId = 0, iisid = 0
            'stopSync(Me.lbAcount.SelectedItem, curAccount.sqlId)
            curAccount.isStart = False
        Else
            MessageBox.Show("Please choose account before continue.")
        End If
    End Sub
    Sub createAccount(accountName)
        Dim curAccount = accountList(accountName)
        Me.Timer1.Enabled = False
        Dim coreAccount = "oph"
        Dim coreDB = "oph_core"
        Dim dataAccount = accountName
        Dim dataDB = dataAccount & "_data"
        Dim user = curAccount.user
        Dim secret = curAccount.secret
        Dim port = curAccount.port
        Dim autoStart = curAccount.autoStart

        Dim remoteUrl = My.Settings.remoteUrl
        Dim p_uri = remoteUrl & dataAccount '"http://redbean/" & dataAccount
        Dim ftemp = Directory.GetCurrentDirectory() & "\" & folderTemp
        Dim fdata = Directory.GetCurrentDirectory() & "\" & folderData
        Dim uid = "", pwd = ""

        If Not Directory.Exists(ftemp & "") Then
            Directory.CreateDirectory(ftemp & "")
        End If
        If Not Directory.Exists(fdata) Then
            Directory.CreateDirectory(fdata)
        End If
        Dim isLocaldb = My.Settings.isLocalDB
        If isLocaldb Then
            If checkInstance("OPERAHOUSE") <> "OPERAHOUSE" Then
                installLocalDB(Directory.GetCurrentDirectory() & "\" & folderTemp, Directory.GetCurrentDirectory() & "\" & folderData)
                SetLog("localDB installed")
                createInstance("OPERAHOUSE")
                SetLog("OPERAHOUSE created")
            End If

            startInstance("OPERAHOUSE")
            SetLog("OPERAHOUSE started")
            pipename = getPipeName("OPERAHOUSE")
            SetLog("Pipename: " & pipename)
        Else
            pipename = My.Settings.dbInstanceName
            uid = My.Settings.dbUser
            pwd = My.Settings.dbPassword
        End If

        Dim gitloc = getGITLocation()
        SetLog("GIT Location: " & gitloc)


        If Not syncLocalScript("use " & coreDB, coreDB, pipename, uid, pwd) Then
            Dim mdfFile = Directory.GetCurrentDirectory & "\" & folderData & "\" & coreDB & "_data.mdf"
            Dim ldfFile = Directory.GetCurrentDirectory & "\" & folderData & "\" & coreDB & "_log.ldf"
            If isLocaldb Then
                syncLocalScript("CREATE DATABASE " & coreDB & " On ( NAME = " & coreDB & "_data, FILENAME = '" & mdfFile & "') Log ON ( NAME = " & coreDB & "_log, FILENAME = '" & ldfFile & "');", "master", pipename, uid, pwd)
            Else
                syncLocalScript("CREATE DATABASE " & coreDB, "master", pipename, uid, pwd)
            End If

            '--always check new update'
            Dim c_uri = remoteUrl & coreAccount

            Dim url = c_uri & "/ophcore/api/sync.aspx?mode=reqcorescript"
            Dim scriptFile = Directory.GetCurrentDirectory & "\" & folderTemp & "\install_core.sql"
            runScript(url, pipename, scriptFile, coreDB, uid, pwd)

        End If

        Dim localFile = Directory.GetCurrentDirectory & "\" & folderTemp & "\sync.zip"

        If Not File.Exists(localFile) Then
            Dim url = p_uri & "/ophcore/api/sync.aspx?mode=webrequestFile"
            If downloadFilename(url, localFile) Then
                unZip(localFile, Directory.GetCurrentDirectory & "\" & folderTemp)
            End If
        End If

        'download from git
        runCmd(Directory.GetCurrentDirectory & "\" & folderTemp & "\build-oph.bat")


        localFile = Directory.GetCurrentDirectory & "\" & folderTemp & "\webRequest.dll"
        If File.Exists(localFile) Then
            syncLocalScript("EXEC sp_changedbowner 'sa'; ALTER DATABASE " & coreDB & " SET TRUSTWORTHY ON", coreDB, pipename, uid, pwd)
            syncLocalScript("sp_configure 'show advanced options', 1;RECONFIGURE", coreDB, pipename, uid, pwd)
            syncLocalScript("sp_configure 'clr enabled', 1;RECONFIGURE", coreDB, pipename, uid, pwd)

            If isLocaldb Then
                syncLocalScript("if not exists(select * from sys.assemblies where name='webRequest') create assembly webRequest from '" & localFile & "' with PERMISSION_SET = unsafe", coreDB, pipename, uid, pwd)
            Else
                Dim odbc = "Data Source=" & pipename & ";Initial Catalog=" & coreDB & ";uid=" & uid & ";pwd=" & pwd 'My.Settings.odbc
                Dim x = runSQLwithResult("if not exists(select * from sys.assemblies where name='webRequest') select 1", odbc)
                If x = "1" Then MessageBox.Show("Please add webRequest.dll in oph_core database manually. Please OK when continue...")
            End If
        End If

        syncLocalScript("if not exists(select * from sys.objects where name='fn_get_webrequest') begin	declare @sqlstr nvarchar(max)='CREATE FUNCTION [gen].[fn_get_webrequest](@uri [nvarchar](max), @user [nvarchar](255) = N'''', @passwd [nvarchar](255) = N'''') RETURNS [nvarchar](max) WITH EXECUTE AS CALLER AS EXTERNAL NAME [webRequest].[webRequest.Functions].[GET]';	exec sp_executesql @sqlstr; end", coreDB, pipename, uid, pwd)
        syncLocalScript("if not exists(select * from sys.objects where name='fn_post_webrequest') begin	declare @sqlstr nvarchar(max)='CREATE FUNCTION [gen].[fn_post_webrequest](@uri [nvarchar](max), @postdata [nvarchar](max), @user [nvarchar](255) = N'''', @passwd [nvarchar](255) = N'''', @headers [nvarchar](max)) RETURNS [nvarchar](max) WITH EXECUTE AS CALLER AS EXTERNAL NAME [webRequest].[webRequest.Functions].[POST]'; exec sp_executesql @sqlstr; end", coreDB, pipename, uid, pwd)


        If Not syncLocalScript("use " & dataDB, dataDB, pipename, uid, pwd) Then
            Dim sqlstr As String = ""
            Dim token = getToken(dataAccount)

            Dim p_add = p_uri & "/ophcore/api/sync.aspx"
            Dim r = ""
            Dim url = ""
            If token <> "" Then
                url = p_add & "?mode=dbinfo&token=" & token
                r = postHttp(url)
            End If

            Dim sep() As String = {"<?xml version=""1.0"" encoding=""utf-8""?>", "<sqroot>", "<databases>", "<database>", "</database>", "</databases>", "</sqroot>"}
            Dim r1 = r.Split(sep, StringSplitOptions.RemoveEmptyEntries)
            Dim accountGUID As String = ""
            Dim sqlstr2 As String = ""

            Dim sep1() As String = {"<AccountGUID>", "<AccountDBGUID>", "<databasename>", "<isMaster>", "<version>", "</AccountGUID>", "</AccountDBGUID>", "</databasename>", "</isMaster>", "</version>"}
            For Each r1x In r1
                Dim r2 = r1x.Split(sep1, StringSplitOptions.RemoveEmptyEntries)
                If r2.Length = 5 Then
                    accountGUID = r2(0)
                    Dim accountDBGUID = r2(1)
                    Dim dbname = r2(2)
                    Dim ismaster = r2(3)
                    Dim Version = r2(4)
                    Dim mdfFile = Directory.GetCurrentDirectory & "\" & folderData & "\" & dbname & "_data.mdf"
                    Dim ldfFile = Directory.GetCurrentDirectory & "\" & folderData & "\" & dbname & "_log.ldf"
                    If isLocaldb Then
                        syncLocalScript("if not exists(select * from sys.databases where name='" & dbname & "') CREATE DATABASE " & dbname & " On ( NAME = " & dbname & "_data, FILENAME = '" & mdfFile & "') Log ON ( NAME = " & dbname & "_log, FILENAME = '" & ldfFile & "');", "master", pipename, uid, pwd)
                    Else
                        syncLocalScript("if not exists(select * from sys.databases where name='" & dbname & "') CREATE DATABASE " & dbname, "master", pipename, uid, pwd)
                    End If
                    sqlstr2 = sqlstr2 & "use " & coreDB & vbCrLf &
                "if not exists(select * from acct where accountid='" & dataAccount & "') insert into acct (accountguid, accountid) values ('" & accountGUID & "', '" & dataAccount & "')" & vbCrLf &
                "if not exists(select * from acctdbse where accountdbguid='" & accountDBGUID & "') insert into acctdbse (accountdbguid, accountguid, databasename, ismaster, version) values ('" & accountDBGUID & "', '" & accountGUID & "', '" & dbname & "', '" & ismaster & "', '" & Version & "')" & vbCrLf &
                "use " & dataDB & vbCrLf &
                "if not exists(select * from acct where accountid='" & dataAccount & "') insert into acct (accountguid, accountid) values ('" & accountGUID & "', '" & dataAccount & "')" & vbCrLf &
                "if not exists(select * from acctdbse where accountdbguid='" & accountDBGUID & "') insert into acctdbse (accountdbguid, accountguid, databasename, ismaster, version) values ('" & accountDBGUID & "', '" & accountGUID & "', '" & dbname & "', '" & ismaster & "', '" & Version & "')" & vbCrLf
                End If
            Next

            Dim info1() As String = {"<info>", "</info>", "<data ", "/>"}
            For Each r1x In r1
                Dim r2 = r1x.Split(info1, StringSplitOptions.RemoveEmptyEntries)
                If r2.Length > 1 Then
                    For Each r2x In r2
                        Dim r3 = r2x.Split({"key=""", "value=""", """ "}, StringSplitOptions.RemoveEmptyEntries)
                        sqlstr2 = sqlstr2 & "use " & coreDB & vbCrLf &
                    "if not exists(select * from acctinfo where accountguid='" & accountGUID & "' and infokey='" & r3(0) & "') insert into acctinfo (accountguid, infokey, infovalue) values ('" & accountGUID & "', '" & r3(0) & "', '" & r3(1) & "')"
                        sqlstr2 = sqlstr2 & "use " & dataDB & vbCrLf &
                    "if not exists(select * from acctinfo where accountguid='" & accountGUID & "' and infokey='" & r3(0) & "') insert into acctinfo (accountguid, infokey, infovalue) values ('" & accountGUID & "', '" & r3(0) & "', '" & r3(1) & "')"

                    Next

                End If

            Next
            'odbc
            sqlstr2 = sqlstr2 & "use " & coreDB & vbCrLf &
                    "if not exists(select * from acctinfo where accountguid='" & accountGUID & "' and infokey='ODBC') insert into acctinfo (accountguid, infokey, infovalue) values ('" & accountGUID & "', 'ODBC', 'Data Source=(localdb)\operahouse;Initial Catalog=" & dataAccount & "_data;Integrated Security=SSPI;timeout=600')"
            sqlstr2 = sqlstr2 & "use " & dataDB & vbCrLf &
                    "if not exists(select * from acctinfo where accountguid='" & accountGUID & "' and infokey='ODBC') insert into acctinfo (accountguid, infokey, infovalue) values ('" & accountGUID & "', 'ODBC', 'Data Source=(localdb)\operahouse;Initial Catalog=" & dataAccount & "_data;Integrated Security=SSPI;timeout=600')"

            'address
            sqlstr2 = sqlstr2 & "use " & coreDB & vbCrLf &
                    "if not exists(select * from acctinfo where accountguid='" & accountGUID & "' and infokey='address') insert into acctinfo (accountguid, infokey, infovalue) values ('" & accountGUID & "', 'address', 'localhost:" & curAccount.port & "')"
            sqlstr2 = sqlstr2 & "use " & dataDB & vbCrLf &
                    "if not exists(select * from acctinfo where accountguid='" & accountGUID & "' and infokey='address') insert into acctinfo (accountguid, infokey, infovalue) values ('" & accountGUID & "', 'address', 'localhost:" & curAccount.port & "')"

            'white address
            sqlstr2 = sqlstr2 & "use " & coreDB & vbCrLf &
                    "if not exists(select * from acctinfo where accountguid='" & accountGUID & "' and infokey='whiteAddress') insert into acctinfo (accountguid, infokey, infovalue) values ('" & accountGUID & "', 'whiteAddress', 'localhost:" & curAccount.port & "')"
            sqlstr2 = sqlstr2 & "use " & dataDB & vbCrLf &
                    "if not exists(select * from acctinfo where accountguid='" & accountGUID & "' and infokey='whiteAddress') insert into acctinfo (accountguid, infokey, infovalue) values ('" & accountGUID & "', 'whiteAddress', 'localhost:" & curAccount.port & "')"

            url = p_uri & "/ophcore/api/sync.aspx?mode=reqcorescript"
            Dim scriptFile = Directory.GetCurrentDirectory & "\" & folderTemp & "\install_" & dataAccount & ".sql"
            runScript(url, pipename, scriptFile, dataDB, uid, pwd)

            'run after
            syncLocalScript(sqlstr2, "master", pipename, uid, pwd)

        End If

        'setup applicationhost.config
        addAccounttoIIS(dataAccount, Directory.GetCurrentDirectory() & "\", port, False)
        'Dim isReady = addWebConfig(Directory.GetCurrentDirectory() & "\")

        'If isIISExpress Then
        '    iisExpressFolder = getIISLocation()
        '    SetLog("IIS Express Location: " & iisExpressFolder)
        '    'addAccounttoIIS(dataAccount, Directory.GetCurrentDirectory() & "\", port)
        '    'addWebConfig(Directory.GetCurrentDirectory() & "\")
        'End If
        'If isReady Then
        '    If curAccount.isStart Then startSync(accountName)
        'Else
        '    SetLog("IIS not started yet. Web Config is not exists.")
        'End If

        Me.Timer1.Enabled = True
    End Sub
    Sub startSync(accountName)
        Dim curAccount = accountList(accountName)

        Dim coreAccount = "oph"
        Dim coreDB = "oph_core"
        Dim dataAccount = accountName
        Dim dataDB = dataAccount & "_data"
        Dim user = curAccount.user
        Dim secret = curAccount.secret
        Dim port = curAccount.port
        Dim autoStart = curAccount.autoStart

        Dim remoteUrl = My.Settings.remoteUrl
        Dim p_uri = remoteUrl & dataAccount '"http://redbean/" & dataAccount
        Dim uid = "", pwd = ""

        'sync on
        curAccount.isStart = True
        Me.Button2.Enabled = True
        Me.Button1.Enabled = False
        Me.Button4.Enabled = False
        Me.Button7.Enabled = False
        Dim isLocaldb = My.Settings.isLocalDB
        If isLocaldb Then
            If checkInstance("OPERAHOUSE") <> "OPERAHOUSE" Then
                installLocalDB(Directory.GetCurrentDirectory() & "\" & folderTemp, Directory.GetCurrentDirectory() & "\" & folderData)
                SetLog("localDB installed")
                createInstance("OPERAHOUSE")
                SetLog("OPERAHOUSE created")
            End If

            startInstance("OPERAHOUSE")
            SetLog("OPERAHOUSE started")
            pipename = getPipeName("OPERAHOUSE")
            SetLog("Pipename: " & pipename)


        Else
            pipename = My.Settings.dbInstanceName
            uid = My.Settings.dbUser
            pwd = My.Settings.dbPassword
        End If
        createAccount(accountName)

        If isIISExpress Then
            iisExpressFolder = getIISLocation()
            SetLog("IIS Express Location: " & iisExpressFolder)
            'run iis
            If iisId = 0 And iisExpressFolder <> "" Then runIIS(dataAccount)
        End If

        'start sync
        syncLocalScript("exec core.compressdb '" & coreDB & "';", coreDB, pipename, uid, pwd)
        syncLocalScript("exec core.compressdb '" & dataAccount & "_data';", coreDB, pipename, uid, pwd)
        syncLocalScript("exec core.compressdb '" & dataAccount & "_v4';", coreDB, pipename, uid, pwd)
        'Do While curAccount.isStart

        If GetWin32Process("", curAccount.sqlId) <> curAccount.sqlId Or curAccount.sqlId = 0 Then
            SetLog(dataAccount & " Synchronize Starting...")
            Dim cmdstr = "while 1=1 begin exec gen.doSync @p_uri='" & p_uri & "', @paccountid='" & dataAccount & "', @code_preset=null, @isLAN=0, @user='" & user & "', @pwd='" & secret & "', @isdebug=0 end"
            curAccount.sqlId = asynclocalScript(cmdstr, coreDB, pipename, uid, pwd)
        End If
        'Application.DoEvents()
        'Loop


    End Sub

    Function addAccounttoIIS(account, path, port, Optional isRemoved = False) As Integer
        Dim isexists = False ', port As Integer = 8080
        For Each k As String In IO.File.ReadLines(path & folderTemp & "\applicationhost.config")
            If k.Contains("<site name=""" & account & """") Then
                isexists = True
            End If
        Next
        'If Not isexists Then
        Dim n = 1
        For Each k As String In IO.File.ReadLines(path & folderTemp & "\applicationhost.config")
            If k.Contains("<site ") Then
                n = n + 1
            End If
        Next
        Dim newfile As New List(Of String)()
        Dim skipLine As Boolean = False
        Dim curAccount = ""
        Dim nn = 1
        For Each k As String In IO.File.ReadLines(path & folderTemp & "\applicationhost.config")
            If Not isexists Then
                If k.Contains("<siteDefaults>") Then
                    Dim newline = {
                vbTab & vbTab & vbTab & "<site name=""" & account & """ id=""" & n & """>",
                vbTab & vbTab & vbTab & vbTab & "<application path = ""/"" applicationPool=""Clr4IntegratedAppPool"">",
                vbTab & vbTab & vbTab & vbTab & vbTab & "<virtualDirectory path = ""/"" physicalPath=""" & path & "operahouse\core"" />",
                vbTab & vbTab & vbTab & vbTab & vbTab & "<virtualDirectory path = ""/OPHContent/cdn"" physicalPath=""" & path & "cdn"" />",
                vbTab & vbTab & vbTab & vbTab & vbTab & "<virtualDirectory path = ""/OPHContent/log"" physicalPath=""" & path & "log"" />",
                vbTab & vbTab & vbTab & vbTab & vbTab & "<virtualDirectory path = ""/OPHContent/themes"" physicalPath=""" & path & "operahouse\themes"" />",
                vbTab & vbTab & vbTab & vbTab & vbTab & "<virtualDirectory path = ""/OPHContent/documents"" physicalPath=""" & path & "operahouse\documents"" />",
                vbTab & vbTab & vbTab & vbTab & "</application>",
                vbTab & vbTab & vbTab & vbTab & "<bindings>",
                vbTab & vbTab & vbTab & vbTab & vbTab & "<binding protocol = ""http"" bindingInformation=""*:" & port & ":localhost"" />",
                vbTab & vbTab & vbTab & vbTab & "</bindings>",
                vbTab & vbTab & vbTab & "</site>"}

                    For Each line As String In newline
                        newfile.Add(line)
                    Next
                End If
            Else
                If k.Contains("<site name=""" & account & """") Then
                    curAccount = account
                End If
                If curAccount = account And k.Contains("<binding protocol = ""http""") And Not isRemoved Then
                    Dim line = vbTab & vbTab & vbTab & vbTab & vbTab & "<binding protocol = ""http"" bindingInformation=""*:" & port & ":localhost"" />"
                    newfile.Add(line)
                    skipLine = True
                    curAccount = ""
                End If
                If curAccount = account And isRemoved Then
                    If k.Contains("<site name") Or k.Contains("<application path") Or k.Contains("<virtualDirectory") Or k.Contains("</application>") Or k.Contains("<bindings>") Or k.Contains("<binding") Or k.Contains("</bindings>") Then
                        skipLine = True
                    End If
                    If k.Contains("</site>") Then
                        skipLine = True
                        curAccount = ""
                    End If
                End If
            End If
            If k.Contains("<site ") Then
                Dim line = k.Substring(0, k.IndexOf("id") - 1) & " id=""" & nn & """>"
                newfile.Add(line)
                skipLine = True
                nn += 1
            End If
            If Not skipLine Then newfile.Add(k)
            skipLine = False

        Next

        File.Delete(path & folderTemp & "\applicationhost.config")
        System.IO.File.WriteAllLines(path & folderTemp & "\applicationhost.config", newfile.ToArray())

        'End If
        Return port
    End Function

    Function addWebConfig(path) As Boolean
        Dim r = False
        If File.Exists(path & "operahouse\core\sample-web.config") Then
            r = True
            If Not File.Exists(path & "operahouse\core\web.config") Then
                Dim newfile As New List(Of String)()
                For Each k As String In IO.File.ReadLines(path & "operahouse\core\sample-web.config")
                    If k.Contains("<add key=""Sequoia""") Then
                        Dim newline = {
                                    vbTab & "<add key=""Sequoia"" value=""Data Source=(localdb)\operahouse;Initial Catalog=oph_core;Integrated Security=SSPI;password=;timeout=600"" />"}

                        For Each line As String In newline
                            newfile.Add(line)
                        Next
                    Else
                        newfile.Add(k)
                    End If


                Next
                File.Delete(path & folderTemp & "\web.config")
                System.IO.File.WriteAllLines(path & "operahouse\core\web.config", newfile.ToArray())
                r = True
            End If
        End If
        Return r
    End Function
    Function getIISLocation() As String
        Dim r = My.Settings.IISExpressLocation
        If r = "" Or Not File.Exists(r) Then
            r = findFile("C:\Program Files\IIS Express", "iisexpress.exe")
            If r = "" Then
                If MessageBox.Show(Me, "We cannot find IIS Express. We are about to install from our repository.", "IIS Express", vbYesNo) = vbYes Then
                    Dim b = installIIS(Directory.GetCurrentDirectory() & "\" & folderTemp, Directory.GetCurrentDirectory() & "\" & folderData)
                    r = findFile("C:\Program Files\IIS Express", "iisexpress.exe")
                    If r <> "" Then SetLog("IIS Express Installed")
                End If
            End If
        End If
        Return r
    End Function
    Function getGITLocation() As String
        Dim r = "C:\Program Files\GIT\git-bash.exe"
        If r = "" Or Not File.Exists(r) Then
            Dim c = MsgBox("We cannot find GIT. Press Yes to location it for us. Press No to install from our repository or Cancel do it later.", vbYesNoCancel, "GIT")
            If c = vbYes Then
                Dim folder = Me.FolderBrowserDialog1.ShowDialog()
                If File.Exists(folder & "\git-bash.exe") Then
                    r = folder & "\git-bash.exe"
                End If
            ElseIf c = vbNo Then
                installGIT(Directory.GetCurrentDirectory() & "\" & folderTemp, Directory.GetCurrentDirectory() & "\" & folderData)
                SetLog("GIT Installed")
            End If
        End If

        Return r
    End Function

    Function checkInstance(instanceName) As String
        Dim p As Process = New Process()
        p.StartInfo.UseShellExecute = False
        p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.FileName = "cmd.exe"
        p.StartInfo.Arguments = "/C sqllocaldb info " & instanceName
        p.StartInfo.CreateNoWindow = True
        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
        p.Start()

        Dim sOutput As String = p.StandardOutput.ReadToEnd()
        p.WaitForExit()
        SetLog(sOutput)

        Dim pipeName As String = ""
        If Not (sOutput Is Nothing Or sOutput.Trim().Length = 0 Or sOutput.Contains("not recognized") Or sOutput.Contains("doesn't exist")) Then
            For Each info In sOutput.Split(vbCrLf)
                If info.Split(":")(0) = "Name" Then
                    pipeName = info.Split(":")(1).Trim()
                    Exit For
                End If
            Next
        End If
        Return pipeName
    End Function

    Sub runIIS(app)
        eventHandled = False
        elapsedTime = 0

        Dim p As Process = New Process()
        p.StartInfo.UseShellExecute = False
        p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.RedirectStandardError = True
        p.EnableRaisingEvents = True

        AddHandler p.ErrorDataReceived, AddressOf OutputDataReceivedIIS
        AddHandler p.OutputDataReceived, AddressOf OutputDataReceivedIIS

        p.StartInfo.FileName = iisExpressFolder
        p.StartInfo.Arguments = "/config:" & Directory.GetCurrentDirectory & "\" & folderTemp & "\applicationhost.config /systray:false /site:" & app
        p.StartInfo.CreateNoWindow = True
        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden


        p.Start()
        iisId = p.Id

        p.BeginErrorReadLine()
        p.BeginOutputReadLine()

    End Sub

    Function getPipeName(instanceName) As String
        Dim p As Process = New Process()
        p.StartInfo.UseShellExecute = False
        p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.FileName = "cmd.exe"
        p.StartInfo.Arguments = "/C sqllocaldb info " & instanceName
        p.StartInfo.CreateNoWindow = True
        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
        p.Start()

        Dim sOutput As String = p.StandardOutput.ReadToEnd()
        p.WaitForExit()
        SetLog(sOutput)

        Dim pipeName As String = ""
        If Not (sOutput Is Nothing Or sOutput.Trim().Length = 0 Or sOutput.Contains("not recognized")) Then
            For Each info In sOutput.Replace(vbCr, "").Split(vbLf)
                If info.Split(":")(0) = "Instance pipe name" Then
                    pipeName = info.Split(":")(1) & ":" & info.Split(":")(2)
                    Exit For
                End If
            Next
        End If
        Return pipeName
    End Function

    Sub createInstance(instanceName)
        Dim p As Process = New Process()
        p.StartInfo.UseShellExecute = False
        p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.FileName = "cmd.exe"
        p.StartInfo.Arguments = "/C sqllocaldb create " & instanceName
        p.StartInfo.CreateNoWindow = True
        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
        p.Start()

        Dim sOutput As String = p.StandardOutput.ReadToEnd()
        p.WaitForExit()
        SetLog(sOutput)
    End Sub

    Sub deleteInstance(instanceName)
        Dim p As Process = New Process()
        p.StartInfo.UseShellExecute = False
        p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.FileName = "cmd.exe"
        p.StartInfo.Arguments = "/C sqllocaldb delete " & instanceName
        p.StartInfo.CreateNoWindow = True
        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
        p.Start()

        Dim sOutput As String = p.StandardOutput.ReadToEnd()
        p.WaitForExit()
        SetLog(sOutput)
    End Sub
    Sub startInstance(instanceName)
        Dim p As Process = New Process()
        p.StartInfo.UseShellExecute = False
        p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.FileName = "cmd.exe"
        p.StartInfo.Arguments = "/C sqllocaldb start " & instanceName
        p.StartInfo.CreateNoWindow = True
        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
        p.Start()

        Dim sOutput As String = p.StandardOutput.ReadToEnd()
        p.WaitForExit()
        SetLog(sOutput)
    End Sub

    Sub stopSync(accountName, sqlId)
        'accountList(accountName).isStart = False

        SetLog(accountName & " stopping...")
        If GetWin32Process("", sqlId) = sqlId Then
            killProcess(sqlId)
            accountList(accountName).sqlId = 0
        End If

        Dim stillLive = False
        For Each x In accountList
            Dim curAccount = accountList(x.Key.ToString)
            If curAccount.sqlId > 0 Then
                stillLive = True
                Exit For
            End If
        Next

        If Not stillLive Then
            iisId = GetWin32Process("iisexpress", 0) '= iisId
            If iisId Then
                killProcess(iisId)
            End If
            'GetWin32Process("iisexpress", 0)
            Dim isLocaldb = My.Settings.isLocalDB
            If isLocaldb Then stopInstance("OPERAHOUSE")

        End If
        SetLog(accountName & " stopped.")
        Me.Button1.Enabled = True
        Me.Button2.Enabled = False
        Me.Button4.Enabled = True
        Me.Button7.Enabled = False
    End Sub
    Sub killProcess(p)
        If p > 0 Then
            Dim pp = Process.GetProcessById(p)
            If Not pp Is Nothing Then
                pp.Kill()
            End If
        End If
    End Sub
    Sub stopInstance(instanceName)
        Dim p As Process = New Process()
        p.StartInfo.UseShellExecute = False
        p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.FileName = "cmd.exe"
        p.StartInfo.Arguments = "/C sqllocaldb stop " & instanceName
        p.StartInfo.CreateNoWindow = True
        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
        p.Start()

        Dim sOutput As String = p.StandardOutput.ReadToEnd()
        p.WaitForExit()
        SetLog(sOutput)

    End Sub

    Function runScript(url, pipename, scriptFile, db, uid, pwd) As Boolean
        Dim r = True
        If File.Exists(scriptFile) Then File.Delete(scriptFile)
        If downloadFilename(url, scriptFile) Then
            Dim p As Process = New Process()
            p.StartInfo.UseShellExecute = False
            p.StartInfo.RedirectStandardOutput = True
            p.StartInfo.FileName = "sqlcmd.exe"
            p.StartInfo.Arguments = "-S " & pipename & " -d " & db & " -i """ & scriptFile & """" & IIf(uid <> "", " -U " & uid & " -P " & pwd, "")
            p.StartInfo.CreateNoWindow = True
            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
            p.Start()

            Dim sOutput As String = p.StandardOutput.ReadToEnd()
            p.WaitForExit()
            SetLog(sOutput)
        Else
            r = False
        End If
        Return r
    End Function
    Sub runCmd(filename)
        Dim p As Process = New Process()
        p.StartInfo.UseShellExecute = False
        p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.FileName = filename
        p.StartInfo.Arguments = " "
        p.StartInfo.CreateNoWindow = True
        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
        p.Start()
        Dim sOutput As String = p.StandardOutput.ReadToEnd()
        p.WaitForExit()
        SetLog(sOutput)
    End Sub



    Function postHttp(uri As String, Optional postData As String = "", Optional username As String = "", Optional passwd As String = "", Optional headers As String = "") As String
        Dim document As String = ""

        Try
            Dim urix = New Uri(Convert.ToString(uri))
            Dim p = ServicePointManager.FindServicePoint(urix)
            p.Expect100Continue = False

            Dim req As HttpWebRequest = WebRequest.Create(Convert.ToString(uri))
            req.ServicePoint.Expect100Continue = False
            If headers <> "" Then
                For Each x As String In headers.ToString().Split("&")
                    Dim x1 As String() = x.Split("=")
                    req.Headers.Add(x1(0), x1(1))
                Next
            End If

            req.UserAgent = "CLR web client on SQL Server"
            If username <> "" And passwd <> "" Then

                req.Credentials = New NetworkCredential(Convert.ToString(username), Convert.ToString(passwd))
            End If



            ' Submit the POST data
            If postData <> "" Then
                req.Method = "POST"
                req.ContentType = "application/x-www-form-urlencoded"

                Dim postByteArray As Byte() = Encoding.UTF8.GetBytes(Convert.ToString(postData))
                Dim dataStream2 As Stream = req.GetRequestStream()
                dataStream2.Write(postByteArray, 0, postByteArray.Length)
                dataStream2.Close()
            End If

            ' Collect the response, put it in the string variable "document"
            Dim resp As WebResponse = req.GetResponse()
            Dim dataStream3 As Stream = resp.GetResponseStream()
            Dim rdr As StreamReader = New StreamReader(dataStream3)
            document = rdr.ReadToEnd()

            ' Close up And return
            rdr.Close()
            dataStream3.Close()
            resp.Close()


        Catch exc As NullReferenceException

            'send error back
            document = exc.Message

        Catch exc As Exception

            'send error back
            document = exc.Message
        End Try

        Return document
    End Function

    Sub unZip(zipPath, extractPath)
        ZipFile.ExtractToDirectory(zipPath, extractPath)
    End Sub
    Function downloadFilename(url, localpath) As Boolean
        Dim r = True
        Dim wc As New WebClient()
        Try
            wc.DownloadFile(url, localpath)
        Catch ex As Exception
            SetLog(ex.Message)
            r = False
        End Try
        Return r
    End Function

    Function installIIS(ftemp, fdata) As Boolean
        Dim r = True
        Dim url = "http://media.operahouse.systems/iisexpress_x86_en-US.msi" 'x86
        If Environment.Is64BitOperatingSystem Then
            url = "http://media.operahouse.systems/iisexpress_amd64_en-US.msi" '64 bit
        End If

        Dim filename = ftemp & "\iisexpress.msi"
        If Not Directory.Exists(ftemp & "") Then
            Directory.CreateDirectory(ftemp & "")
        End If
        If Not Directory.Exists(fdata) Then
            Directory.CreateDirectory(fdata)
        End If
        Dim isdownloaded = File.Exists(filename)
        If Not File.Exists(filename) Then
            isdownloaded = downloadFilename(url, filename)
        End If
        If isdownloaded Then
            Dim runfilename = """" & filename & """"
            Dim p As Process = New Process()
            'Dim info As New ProcessStartInfo()
            p.StartInfo.FileName = "c:\windows\system32\msiexec.exe"
            p.StartInfo.Arguments = " /i """ & ftemp & "\iisexpress.msi"""
            p.Start()

            p.WaitForExit()
            If findFile("c:\program files", "iisexpress.exe") = "" Then r = False
        Else
            r = False
        End If

        Return r

    End Function

    Function installGIT(ftemp, fdata) As Boolean
        Dim r = True
        Dim url = "https://github.com/git-for-windows/git/releases/download/v2.15.1.windows.2/Git-2.15.1.2-32-bit.exe"
        If Environment.Is64BitOperatingSystem Then
            url = "https://github.com/git-for-windows/git/releases/download/v2.15.1.windows.2/Git-2.15.1.2-64-bit.exe"
        End If

        Dim filename = ftemp & "\git.exe"
        If Not Directory.Exists(ftemp) Then
            Directory.CreateDirectory(ftemp)
        End If
        If Not Directory.Exists(fdata) Then
            Directory.CreateDirectory(fdata)
        End If
        If Not File.Exists(filename) Then
            If downloadFilename(url, filename) Then
                Dim runfilename = """" & filename & """"
                Dim info As New ProcessStartInfo()
                info.FileName = ftemp & "\git.exe"
                info.Arguments = " "
                Process.Start(info)
            Else
                r = False
            End If
        End If
        Return r
    End Function
    Function installLocalDB(ftemp, fdata) As Boolean
        Dim r = True
        'Dim url = "https://go.microsoft.com/fwlink/?linkid=853017"  '2017
        'Dim url = "https://go.microsoft.com/fwlink/?LinkID=799012" '2016
        'Dim url = "http://download.microsoft.com/download/E/A/E/EAE6F7FC-767A-4038-A954-49B8B05D04EB/ExpressAndTools%2032BIT/SQLEXPRWT_x86_ENU.exe" '2014
        'Dim url = "http://download.microsoft.com/download/8/D/D/8DD7BDBA-CEF7-4D8E-8C16-D9F69527F909/ENU/x86/SQLEXPRWT_x86_ENU.exe" '2012
        Dim url = "http://download.microsoft.com/download/8/D/D/8DD7BDBA-CEF7-4D8E-8C16-D9F69527F909/ENU/x86/SqlLocaLDB.MSI" 'localdb 2012

        Dim filename = ftemp & "\sqllocaldb.msi"
        If Not Directory.Exists(ftemp & "") Then
            Directory.CreateDirectory(ftemp & "")
        End If
        If Not Directory.Exists(fdata & "") Then
            Directory.CreateDirectory(fdata & "")
        End If
        If Not File.Exists(filename) Then
            If downloadFilename(url, filename) Then
                Dim runfilename = """" & filename & """"
                Dim info As New ProcessStartInfo()
                info.FileName = "c:\windows\system32\msiexec.exe"
                info.Arguments = " /i """ & ftemp & "\SqlLocalDB.msi"" IACCEPTSQLLOCALDBLICENSETERMS=YES /qn"
                Process.Start(info)
            Else
                r = False
            End If
        End If
        Return r
    End Function
    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        accountList.Remove(Me.lbAcount.SelectedItem)
        If Directory.Exists(Directory.GetCurrentDirectory & "\" & folderData & "") Then
            Dim directoryName As String = Directory.GetCurrentDirectory & "\" & folderData & ""
            For Each deleteFile In Directory.GetFiles(directoryName, Me.lbAcount.SelectedItem & ".*", SearchOption.TopDirectoryOnly)
                Try
                    File.Delete(deleteFile)
                Catch ex As Exception

                End Try

            Next
        End If

        addAccounttoIIS(Me.lbAcount.SelectedItem, "", 0, True)

        Me.lbAcount.Items.RemoveAt(Me.lbAcount.SelectedIndex)
        Dim json = "{""accountList"":[%item%]}"
        For Each j In accountList
            Dim curAccount = accountList(j.Key.ToString)
            Dim jx = "{""accountName"":""" & j.Key.ToString & """,""user"":""" & curAccount.user & """,""secret"":""" & curAccount.secret & """,""port"":""" & curAccount.port & """, ""autoStart"":""" & IIf(curAccount.autoStart, 1, 0) & """}, %item%"
            json = json.Replace("%item%", jx)
        Next
        json = json.Replace(", %item%", "")
        json = json.Replace("%item%", "")
        My.Settings.SavedAccountList = json
        My.Settings.Save()

        If accountList.Count = 0 Then
            stopInstance("OPERAHOUSE")
            deleteInstance("OPERAHOUSE")
            'If Directory.Exists(Directory.GetCurrentDirectory & "\" & folderTemp & "") Then
            '    Dim directoryName As String = Directory.GetCurrentDirectory & "\" & folderTemp & ""
            '    For Each deleteFile In Directory.GetFiles(directoryName, "*.*", SearchOption.TopDirectoryOnly)
            '        Try
            '            File.Delete(deleteFile)
            '        Catch ex As Exception

            '        End Try

            '    Next
            'End If
            If Directory.Exists(Directory.GetCurrentDirectory & "\" & folderData & "") Then
                Dim directoryName As String = Directory.GetCurrentDirectory & "\" & folderData & ""
                For Each deleteFile In Directory.GetFiles(directoryName, "*.*", SearchOption.TopDirectoryOnly)
                    Try
                        File.Delete(deleteFile)
                    Catch ex As Exception

                    End Try

                Next
            End If
        End If
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Me.tbLog.Clear()
    End Sub

    Function asynclocalScript(sqlstr, db, pipename, uid, pwd) As Integer
        eventHandled = False
        elapsedTime = 0

        Dim p As Process = New Process()
        p.StartInfo.UseShellExecute = False
        p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.RedirectStandardError = True
        p.EnableRaisingEvents = True

        AddHandler p.ErrorDataReceived, AddressOf OutputDataReceivedSQL
        AddHandler p.OutputDataReceived, AddressOf OutputDataReceivedSQL

        p.StartInfo.FileName = "sqlcmd.exe"
        p.StartInfo.Arguments = "-S " & pipename & " -Q """ & sqlstr & """" & IIf(db <> "", " -d " & db, "") & IIf(uid <> "", " -U " & uid & " -P " & pwd, "")
        WriteLog(sqlstr)

        p.StartInfo.CreateNoWindow = True
        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden


        p.Start()
        Dim sqlId = p.Id

        p.BeginErrorReadLine()
        p.BeginOutputReadLine()
        Return sqlId
    End Function

    Function syncLocalScript(sqlstr, db, pipename, uid, pwd) As Boolean
        Dim p As Process = New Process()
        p.StartInfo.UseShellExecute = False
        p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.RedirectStandardError = True

        p.StartInfo.FileName = "sqlcmd.exe"
        p.StartInfo.Arguments = "-S " & pipename & " -Q """ & sqlstr & """" & IIf(db <> "", " -d " & db, "") & IIf(uid <> "", " -U " & uid & " -P " & pwd, "")
        p.StartInfo.CreateNoWindow = True
        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden


        p.Start()

        Dim sOutput As String = p.StandardOutput.ReadToEnd()
        p.WaitForExit()
        SetLog(sOutput)

        If (sOutput Is Nothing Or sOutput.Trim().Length = 0 Or sOutput.Contains("does not exist")) Then
            Return False
        Else
            Return True
        End If
    End Function

    Sub SetLog(txt, Optional title = "")
        Dim t = IIf(txt = "", "", Now() & " " & title & ": " & txt & vbCrLf)
        If Me.InvokeRequired Then
            Me.Invoke(New Action(Sub() Me.tbLog.AppendText(t)))
        Else
            Me.tbLog.AppendText(t)

        End If
        'If Len(t) > 0 Then NotifyIcon1.BalloonTipText = t.ToString.TrimStart().Substring(1, 20) & IIf(Len(t) > 20, "...", "")
        WriteLog(t)
    End Sub

    Public Sub OutputDataReceivedIIS(ByVal sender As Object, ByVal e As DataReceivedEventArgs)
        Try
            Dim t = IIf(e.Data = "", "", Now() & " IIS " & e.Data & vbCrLf)
            'lastMessage = lastMessage & t

            If Me.InvokeRequired = True Then
                'Me.Invoke(myDelegate, e.Data)
                Me.Invoke(New Action(Sub() Me.tbLog.AppendText(t)))
            Else
                'UpdateTextBox(e.Data)
                Me.tbLog.AppendText(t)
            End If
            eventHandled = True

            WriteLog(t)
        Catch ex As Exception

        End Try

    End Sub
    Public Sub OutputDataReceivedSQL(ByVal sender As Object, ByVal e As DataReceivedEventArgs)
        Try
            Dim t = IIf(e.Data = "", "", Now() & " SYNC " & e.Data & vbCrLf)
            'lastMessage = lastMessage & t

            If Me.InvokeRequired = True Then
                'Me.Invoke(myDelegate, e.Data)
                Me.Invoke(New Action(Sub() Me.tbLog.AppendText(t)))
            Else
                'UpdateTextBox(e.Data)
                Me.tbLog.AppendText(t)
            End If
            eventHandled = True

            WriteLog(t)
        Catch ex As Exception

        End Try

    End Sub
    Sub WriteLog(logMessage As String)
        Dim path = Directory.GetCurrentDirectory() & "\log"
        path = path & "\" '& "OPHContent\log\"
        Dim logFilepath = path & DateTime.Now().Year & "\" & Strings.Right("0" & DateTime.Now().Month, 2) & "\" & Strings.Right("0" & DateTime.Now().Day, 2) & ".txt"
        Dim logPath = path & DateTime.Now().Year & "\" & Strings.Right("0" & DateTime.Now().Month, 2) & "\"

        If (Not System.IO.Directory.Exists(logPath)) Then
            System.IO.Directory.CreateDirectory(logPath)
        End If
        Try
            Using w As StreamWriter = File.AppendText(logFilepath)
                w.WriteLine("{0}", logMessage)

            End Using

        Catch ex As Exception
            Debug.Write(ex.Message.ToString)
        End Try
    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs)

    End Sub


    Private Sub Timer2_Tick(sender As Object, e As EventArgs)
        Dim file = ""
        runCmd(file)
    End Sub
    Function GetWin32Process(processName As String, sqlId As Integer) As Integer
        Dim r As Integer = 0
        If sqlId > 0 Then
            Try
                Dim x As Process = Process.GetProcessById(sqlId)
                If x.Id = sqlId Then
                    r = sqlId
                End If
            Catch ex As Exception

            End Try
        ElseIf processName <> "" Then

            For Each p As Process In Process.GetProcesses()
                If processName = p.ProcessName Then
                    r = p.Id
                    Exit For
                End If
            Next

        End If
        Return r
    End Function

    Private Sub mainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        For Each x In accountList
            Dim curAccount = accountList(x.Key.ToString)
            stopSync(x.Key.ToString, curAccount.sqlId)
        Next

    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click
        'If Me.lbAcount.SelectedItem <> "" Then


        Dim an = Me.TextBox1.Text
        Dim us = Me.TextBox2.Text
        Dim sc = Me.TextBox3.Text
        Dim pt = Me.TextBox4.Text
        Dim ast As Boolean = Me.CheckBox1.Checked

        Dim isExists = False
        For Each x In Me.lbAcount.Items
            If x.ToString = Me.TextBox1.Text Then
                isExists = True
                Exit For
            End If
        Next

        Dim remoteUrl = My.Settings.remoteUrl
        Dim p_uri = remoteUrl & an '"http://redbean/" & dataAccount

        Dim sqlstr As String = ""

        Dim p_add = p_uri & "/ophcore/api/sync.aspx"
        Dim url = p_add & "?mode=reqtoken&userid=" & us & "&pwd=" & sc
        Dim r = postHttp(url)
        Dim m = ""
        If r.IndexOf("<message>") >= 0 Then
            m = r.Substring(r.IndexOf("<message>") + Len("<message>"), r.IndexOf("</message>") - r.IndexOf("<message>") - Len("<message>"))
        End If

        If Not isExists Then
            If m = "" Then
                accountList.Add(an, New accountType With {.user = us, .secret = sc, .sqlId = 0, .port = pt, .autoStart = ast})
                Me.lbAcount.Items.Add(Me.TextBox1.Text) '
            Else
                MessageBox.Show("Wrong user or secret. Try Again!")
            End If

        Else
            If m = "" Then accountList(an).user = us
            If m = "" Then accountList(an).secret = sc
            accountList(an).port = pt
            accountList(an).autoStart = ast
        End If

        Dim json = "{""accountList"":[%item%]}"
        For Each j In accountList
            Dim curAccount = accountList(j.Key.ToString)
            Dim jx = "{""accountName"":""" & j.Key.ToString & """,""user"":""" & curAccount.user & """,""secret"":""" & curAccount.secret & """,""port"":""" & curAccount.port & """, ""autoStart"":""" & IIf(curAccount.autoStart, 1, 0) & """}, %item%"
            json = json.Replace("%item%", jx)
        Next
        json = json.Replace(", %item%", "")
        json = json.Replace("%item%", "")
        My.Settings.SavedAccountList = json
        My.Settings.Save()

        If accountList(an).autoStart Then accountList(an).isStart = True
        'Else
        'MessageBox.Show("Please choose account before continue.")
        'End If
        'Else
        'MessageBox.Show("Wrong user or secret. Try Again!")
        'End If
    End Sub



    Private Sub lbAcount_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lbAcount.SelectedIndexChanged
        If Not IsNothing(Me.lbAcount.SelectedItem) Then
            Dim curAccount = accountList(Me.lbAcount.SelectedItem)
            'If GetWin32Process("", curAccount.sqlId) <> curAccount.sqlId Then
            Me.TextBox1.Text = Me.lbAcount.SelectedItem
            Me.TextBox2.Text = curAccount.user
            Me.TextBox3.Text = "******************"
            Me.TextBox4.Text = curAccount.port
            Me.CheckBox1.Checked = curAccount.autoStart

            curAccount.sqlId = GetWin32Process("", curAccount.sqlId)
            '      End If
            Me.Button1.Enabled = curAccount.sqlId = 0
            Me.Button2.Enabled = curAccount.sqlId > 0
            Me.Button4.Enabled = curAccount.sqlId = 0
            Me.Button7.Enabled = curAccount.sqlId > 0 AndAlso isStructureDone(Me.lbAcount.SelectedItem)
        End If
    End Sub
    Function isSyncDone(accountName) As Boolean
        Dim r = False
        Dim p_uri = ""
        Dim token = getToken(accountName)

        Dim odbc = "Data Source=" & pipename & ";Initial Catalog=" & accountName & ";Integrated Security=True" 'My.Settings.odbc
        Dim sqlstr = "exec [gen].[dosync_lvl2] '" & accountName & "', null, 'http://redbean/apotek/ophcore/api/sync.aspx', 0, '" & token & "', 0, @statusonly=1, @isdebug=0"
        r = runSQLwithResult(sqlstr, odbc)
        Return Not r
    End Function
    Function isStructureDone(accountName) As Boolean
        Dim r = False
        Dim p_uri = ""
        If pipename <> "" Then
            Dim token = getToken(accountName)

            Dim odbc = "Data Source=" & pipename & ";Initial Catalog=" & "oph_core" & ";Integrated Security=True" 'My.Settings.odbc
            Dim sqlstr = "exec [gen].[dosync_lvl1] '" & accountName & "', null, 'http://redbean/apotek/ophcore/api/sync.aspx', 0, '" & token & "', 0, @statusonly=1, @isdebug=0"
            Try
                r = runSQLwithResult(sqlstr, odbc)
            Catch ex As Exception

            End Try

        End If
        Return Not r
    End Function
    Private Sub Button5_Click_1(sender As Object, e As EventArgs) Handles Button5.Click
        Me.TextBox1.Text = ""
        Me.TextBox2.Text = ""
        Me.TextBox3.Text = ""
        Me.TextBox4.Text = ""

    End Sub

    Private Sub Button7_Click(sender As Object, e As EventArgs) Handles Button7.Click
        If Me.lbAcount.SelectedItem <> "" Then
            Dim curAccount = accountList(Me.lbAcount.SelectedItem)
            If curAccount.isStart Then
                Process.Start("http://localhost:" & curAccount.port & "/")
            End If
        Else
            MessageBox.Show("Please choose account before continue.")
        End If

    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        For Each x In accountList
            Dim curAccount = accountList(x.Key)
            curAccount.sqlId = GetWin32Process("", curAccount.sqlId)
            If curAccount.isStart And curAccount.sqlId = 0 Then
                startSync(x.Key)
            ElseIf Not curAccount.isStart And curAccount.sqlId > 0 Then
                stopSync(x.Key, curAccount.sqlId)
            End If
        Next
    End Sub
    Function findFile(path, pattern) As String
        Dim r As String = ""
        Dim FileLocation As DirectoryInfo =
            New DirectoryInfo(path)

        Try

            For Each File In FileLocation.GetFiles()
                If (File IsNot Nothing) Then
                    If (File.Name.ToLower = pattern.ToString.ToLower) Then
                        r = path & "/" & File.ToString.ToLower
                        Exit For
                        'If (File.ToString.ToLower.Contains("data")) Then fi.Add(File)
                    End If
                End If
            Next
        Catch ex As Exception
            r = ""
        End Try

        If r = "" Then
            Try
                For Each Di In FileLocation.GetDirectories()
                    r = findFile(path & "\" & Di.ToString.ToLower, pattern)
                    If r <> "" Then Exit For
                Next
            Catch ex As Exception
                r = ""
            End Try
        End If
        Return r
    End Function
    Public Function runSQLwithResult(ByVal sqlstr As String, Optional ByVal sqlconstr As String = "") As String
        Dim result As String, contentofError As String

        ' If the connection string is null, usse a default.
        Dim myConnectionString As String = sqlconstr
        'If sqlconstr = "" Then myConnectionString = contentOfdbODBC
        If myConnectionString = "" And sqlconstr = "" Then
            'SignOff()
            Return ""
            Exit Function
        End If

        Dim myConnection As New SqlConnection(myConnectionString)
        Dim myInsertQuery As String = sqlstr
        Dim myCommand As New SqlCommand(myInsertQuery)
        Try
            Dim Reader As SqlClient.SqlDataReader

            myCommand.Connection = myConnection
            myConnection.Open()

            Reader = myCommand.ExecuteReader()

            Reader.Read()
            If Reader.HasRows Then
                result = Reader.GetValue(0).ToString
            Else
                result = ""
            End If

        Catch ex As SqlException
            contentofError = ex.Message & "<br>"
            Return ""
        Catch ex As Exception

            contentofError = ex.Message & "<br>"
            Return ""
        Finally
            myCommand.Connection.Close()
            myConnection.Close()
        End Try
        Return result
    End Function
    Function getToken(dataAccount) As String
        Dim token = ""
        Dim remoteUrl = My.Settings.remoteUrl
        Dim p_uri = remoteUrl & dataAccount
        Dim curAccount = accountList(dataAccount)
        Dim user = curAccount.user
        Dim secret = curAccount.secret

        Dim p_add = p_uri & "/ophcore/api/sync.aspx"
        Dim url = p_add & "?mode=reqtoken&userid=" & user & "&pwd=" & secret
        Dim r = postHttp(url)
        Dim m = ""
        If r.IndexOf("<message>") >= 0 Then
            m = r.Substring(r.IndexOf("<message>") + Len("<message>"), r.IndexOf("</message>") - r.IndexOf("<message>") - Len("<message>"))
        End If

        If m = "" And r.IndexOf("<sessionToken>") >= 0 Then
            token = r.Substring(r.IndexOf("<sessionToken>") + Len("<sessionToken>"), r.IndexOf("</sessionToken>") - r.IndexOf("<sessionToken>") - Len("<sessionToken>"))

        End If
        Return token
    End Function

    Private Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem.Click
        End
    End Sub

    Private Sub OptionsToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles OptionsToolStripMenuItem1.Click
        Dim o = New frmOptions()
        o.ShowDialog()
    End Sub
End Class
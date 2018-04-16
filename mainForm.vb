Imports System.Data
Imports System.Data.Sql
Imports System.Net
Imports System.IO
Imports System.Diagnostics
Imports System.ComponentModel
Imports System.Text
Imports System.DirectoryServices.AccountManagement
Imports System.DirectoryServices.ActiveDirectory.Domain


Public Class mainForm
    Private Const folderTemp = "temp2"
    Private Const folderData = "data"
    Private pipename As String = ""
    Private isStart = False
    Private lastMessage As String = ""

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        startSync()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        stopSync()
    End Sub

    Sub startSync()
        Dim coreAccount = "oph"
        Dim coreDB = "oph_core"
        Dim dataAccount = tbAccount.Text
        Dim dataDB = dataAccount & "_data"
        Dim user = "sam"
        Dim secret = "f41d5e12-1fa4-420a-8a74-bafbdfff3592"
        isStart = True

        Dim p_uri = "http://redbean/" & dataAccount
        'getInstalledSQL()
        'getLocalDB()
        If checkInstance("OPERAHOUSE") <> "OPERAHOUSE" Then
            installLocalDB(Directory.GetCurrentDirectory() & "\" & folderTemp, Directory.GetCurrentDirectory() & "\" & folderData)
            setLog("localDB installed")
            createInstance("OPERAHOUSE")
            setLog("OPERAHOUSE created")
        End If

        startInstance("OPERAHOUSE")
        setLog("OPERAHOUSE started")
        pipename = getPipeName("OPERAHOUSE")
        setLog("Pipename: " & pipename)

        Dim iisexpress = getIISLocation()
        setLog("IIS Express Location: " & iisexpress)

        Dim gitloc = getGITLocation()
        setLog("GIT Location: " & gitloc)

        'startSQLCMDConsole()

        If Not localScript("use " & coreDB, coreDB, pipename) Then
            Dim mdfFile = Directory.GetCurrentDirectory & "\" & folderTemp & "\" & coreDB & "_data.mdf"
            Dim ldfFile = Directory.GetCurrentDirectory & "\" & folderTemp & "\" & coreDB & "_log.ldf"
            localScript("CREATE DATABASE " & coreDB & " On ( NAME = " & coreDB & "_data, FILENAME = '" & mdfFile & "') Log ON ( NAME = " & coreDB & "_log, FILENAME = '" & ldfFile & "');", "master", pipename)


            '--always check new update'
            Dim url = "http://redbean/" & coreAccount & "/ophcore/api/sync.aspx?mode=reqcorescript"
            Dim scriptFile = Directory.GetCurrentDirectory & "\" & folderTemp & "\install_core.sql"
            runScript(url, pipename, scriptFile, coreDB)

            url = "http://redbean/" & dataAccount & "/ophcore/api/sync.aspx?mode=webrequestFile"
            Dim localFile = Directory.GetCurrentDirectory & "\" & folderTemp & "\sync.zip"

            If Not File.Exists(localFile) Then
                If downloadFilename(url, localFile) Then
                    unZip(localFile, Directory.GetCurrentDirectory & "\" & folderTemp)

                    'download from git
                    runCmd(Directory.GetCurrentDirectory & "\" & folderTemp & "\build-oph.bat")

                    localFile = Directory.GetCurrentDirectory & "\" & folderTemp & "\webrequest.dll"
                    If File.Exists(localFile) Then
                        localScript("EXEC sp_changedbowner 'sa'; ALTER DATABASE " & coreDB & " SET TRUSTWORTHY ON", coreDB, pipename)
                        localScript("sp_configure 'show advanced options', 1;RECONFIGURE", coreDB, pipename)
                        'localScript("", coreDB, pipename)
                        localScript("sp_configure 'clr enabled', 1;RECONFIGURE", coreDB, pipename)
                        'localScript("RECONFIGURE", coreDB, pipename)

                        localScript("create assembly webrequest from '" & localFile & "' with PERMISSION_SET = unsafe", coreDB, pipename)
                        localScript("CREATE FUNCTION [gen].[fn_get_webrequest](@uri [nvarchar](max), @user [nvarchar](255) = N'', @passwd [nvarchar](255) = N'') RETURNS [nvarchar](max) WITH EXECUTE AS CALLER AS EXTERNAL NAME [webRequest].[webRequest.Functions].[GET]", coreDB, pipename)
                        localScript("CREATE FUNCTION [gen].[fn_post_webrequest](@uri [nvarchar](max), @postdata [nvarchar](max), @user [nvarchar](255) = N'', @passwd [nvarchar](255) = N'', @headers [nvarchar](max)) RETURNS [nvarchar](max) WITH EXECUTE AS CALLER AS EXTERNAL NAME [webRequest].[webRequest.Functions].[POST]", coreDB, pipename)
                    End If
                End If


            End If
        End If

        If Not localScript("use " & dataDB, dataDB, pipename) Then
            Dim sqlstr As String = ""

            Dim p_add = p_uri & "/ophcore/api/sync.aspx"
            Dim url = p_add & "?mode=reqtoken&userid=" & user & "&pwd=" & secret
            Dim r = postHttp(url)

            Dim token = r.Substring(r.IndexOf("<sessionToken>") + Len("<sessionToken>"), r.IndexOf("</sessionToken>") - r.IndexOf("<sessionToken>") - Len("<sessionToken>"))

            url = p_add & "?mode=dblist&token=" & token
            r = postHttp(url)

            Dim sep() As String = {"<?xml version=""1.0"" encoding=""utf-8""?>", "<sqroot>", "<databases>", "<database>", "</database>", "</databases>", "</sqroot>"}
            Dim r1 = r.Split(sep, StringSplitOptions.RemoveEmptyEntries)

            Dim sep1() As String = {"<AccountGUID>", "<AccountDBGUID>", "<databasename>", "<isMaster>", "<version>", "</AccountGUID>", "</AccountDBGUID>", "</databasename>", "</isMaster>", "</version>"}
            For Each r1x In r1
                Dim r2 = r1x.Split(sep1, StringSplitOptions.RemoveEmptyEntries)
                Dim accountGUID = r2(0)
                Dim accountDBGUID = r2(1)
                Dim dbname = r2(2)
                Dim ismaster = r2(3)
                Dim Version = r2(4)
                Dim mdfFile = Directory.GetCurrentDirectory & "\" & folderTemp & "\" & dbname & "_data.mdf"
                Dim ldfFile = Directory.GetCurrentDirectory & "\" & folderTemp & "\" & dbname & "_log.ldf"
                localScript("if not exists(select * from sys.databases where name='" & dbname & "') CREATE DATABASE " & dbname & " On ( NAME = " & dbname & "_data, FILENAME = '" & mdfFile & "') Log ON ( NAME = " & dbname & "_log, FILENAME = '" & ldfFile & "');", "master", pipename)

                sqlstr = sqlstr & "use " & coreDB & vbCrLf &
                    "if not exists(select * from acct where accountid='" & dataAccount & "') insert into acct (accountguid, accountid) values ('" & accountGUID & "', '" & dataAccount & "')" & vbCrLf &
                    "if not exists(select * from acctdbse where accountdbguid='" & accountDBGUID & "') insert into acctdbse (accountdbguid, accountguid, databasename, ismaster, version) values ('" & accountDBGUID & "', '" & accountGUID & "', '" & dbname & "', '" & ismaster & "', '" & Version & "')" & vbCrLf &
                    "use " & dataDB & vbCrLf &
                    "if not exists(select * from acct where accountid='" & dataAccount & "') insert into acct (accountguid, accountid) values ('" & accountGUID & "', '" & dataAccount & "')" & vbCrLf &
                    "if not exists(select * from acctdbse where accountdbguid='" & accountDBGUID & "') insert into acctdbse (accountdbguid, accountguid, databasename, ismaster, version) values ('" & accountDBGUID & "', '" & accountGUID & "', '" & dbname & "', '" & ismaster & "', '" & Version & "')" & vbCrLf
            Next

            url = "http://redbean/" & dataAccount & "/ophcore/api/sync.aspx?mode=reqcorescript"
            Dim scriptFile = Directory.GetCurrentDirectory & "\" & folderTemp & "\install_" & dataAccount & ".sql"
            runScript(url, pipename, scriptFile, dataDB)

            localScript(sqlstr, "master", pipename)

        End If

        'sync on
        Me.Timer1.Enabled = True

        'setup applicationhost.config
        'run iis
        'runwith config




    End Sub



    Function getIISLocation() As String
        Dim r = My.Resources.ResourceManager.GetObject("IISExpressLocation")
        If r = "" Or Not File.Exists(r) Then
            If File.Exists("C:\Program Files\IIS Express\iisexpress.exe") Then
                r = "C:\Program Files\IIS Express\iisexpress.exe"
            ElseIf File.Exists("C:\Program Files (x86)\IIS Express\iisexpress.exe") Then
                r = "C:\Program Files (x86)\IIS Express\iisexpress.exe"
            Else
                Dim c = MsgBox("We cannot find IIS Express. Press Yes to location it for us. Press No to install from our repository or Cancel do it later.", vbYesNoCancel, "IIS Express")
                If c = vbYes Then
                    Dim folder = Me.FolderBrowserDialog1.ShowDialog()
                    If File.Exists(folder & "\iisexpress.exe") Then
                        r = folder & "\iisexpress.exe"
                    End If
                ElseIf c = vbNo Then
                    installIIS(Directory.GetCurrentDirectory() & "\" & folderTemp, Directory.GetCurrentDirectory() & "\" & folderData)
                    setLog("IIS Express Installed")

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
                setLog("GIT Installed")
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
        setLog(sOutput)

        Dim pipeName As String = ""
        'If sOutput.Contains("doesn't exist") Then
        'getLocalDB()
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
        setLog(sOutput)

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
        setLog(sOutput)
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
        setLog(sOutput)
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
        setLog(sOutput)
    End Sub

    Sub stopSync()
        isStart = False
        stopInstance("OPERAHOUSE")
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
        setLog(sOutput)

    End Sub

    Function runScript(url, pipename, scriptFile, db) As Boolean
        Dim r = True
        If File.Exists(scriptFile) Then File.Delete(scriptFile)
        If downloadFilename(url, scriptFile) Then
            Dim p As Process = New Process()
            p.StartInfo.UseShellExecute = False
            p.StartInfo.RedirectStandardOutput = True
            p.StartInfo.FileName = "sqlcmd.exe"
            p.StartInfo.Arguments = "-S " & pipename & " -d " & db & " -i """ & scriptFile & """"
            p.StartInfo.CreateNoWindow = True
            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
            p.Start()

            Dim sOutput As String = p.StandardOutput.ReadToEnd()
            p.WaitForExit()
            setLog(sOutput)
        Else
            r = False
        End If
        Return r
    End Function
    Sub runCmd(filename)
        Dim info As New ProcessStartInfo()
        info.FileName = filename
        info.Arguments = " "
        Process.Start(info)
    End Sub

    Sub getLocalDB()
        Dim p As Process = New Process()
        p.StartInfo.UseShellExecute = False
        p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.FileName = "cmd.exe"
        p.StartInfo.Arguments = "/C sqllocaldb info"
        p.StartInfo.CreateNoWindow = True
        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
        p.Start()

        Dim sOutput As String = p.StandardOutput.ReadToEnd()
        p.WaitForExit()
        setLog(sOutput)

        'If LocalDb Is Not installed then it will return that 'sqllocaldb' is not recognized as an internal or external command operable program or batch file.
        If (sOutput Is Nothing Or sOutput.Trim().Length = 0 Or sOutput.Contains("not recognized")) Then
        Else
            'Dim instances As String() = sOutput.Split(vbCrLf)
            'Dim lstInstances As List(Of String) = New List < String > ()
            'Me.ListBox1.Items.Clear()

            For Each item As String In sOutput.Split(vbCrLf)
                'Me.ListBox1.Items.Add(item)
            Next
        End If
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
            Else
                'Dim dataStream As Stream = req.GetRequestStream()
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

            '// send error back
            document = exc.Message
            'SqlContext.Pipe.Send(exc.Message)
            '//document = exc.Message;


        Catch exc As Exception

            '// send error back
            document = exc.Message
            'SqlContext.Pipe.Send(exc.Message)
            '//document = exc.Message
        End Try

        Return document
    End Function




    Sub getInstalledSQL()

        Dim sqldatasourceenumerator1 As SqlDataSourceEnumerator = SqlDataSourceEnumerator.Instance
        Dim datatable1 As DataTable = sqldatasourceenumerator1.GetDataSources()
        'Me.ListBox1.Items.Clear()
        For Each row As DataRow In datatable1.Rows
            'Me.ListBox1.Items.Add(row("ServerName") & IIf(IsDBNull(row("InstanceName")), "", "/" & row("InstanceName")))
            'Me.TextBox1.Text = Me.TextBox1.Text & "****************************************" & vbCrLf
            'Me.TextBox1.Text = Me.TextBox1.Text & "Server Name:" + row("ServerName") & vbCrLf
            'Me.TextBox1.Text = Me.TextBox1.Text & "Instance Name:" + row("InstanceName") & vbCrLf
            'Me.TextBox1.Text = Me.TextBox1.Text & "Is Clustered:" + row("IsClustered") & vbCrLf
            'Me.TextBox1.Text = Me.TextBox1.Text & "Version:" + row("Version") & vbCrLf
            'Me.TextBox1.Text = Me.TextBox1.Text & "****************************************" & vbCrLf
        Next
    End Sub
    Sub unZip(zipPath, extractPath)
        ZipFile.ExtractToDirectory(zipPath, extractPath)
    End Sub
    Function downloadFilename(url, localpath) As Boolean
        Dim r = True
        Dim wc As New WebClient()
        Try
            wc.DownloadFile(url, localpath)
        Catch ex As Exception
            setLog(ex.Message)
            r = False
        End Try
        Return r
    End Function

    Function installIIS(ftemp, fdata) As Boolean
        Dim r = True
        Dim url = "http://download.operahouse.systems/iisexpress_x86_en-US.msi" 'x86
        If Environment.Is64BitOperatingSystem Then
            url = "http://download.operahouse.systems/iisexpress_amd64_en-US.msi" '64 bit
        End If

        Dim filename = ftemp & "\iisexpress.msi"
        If Not Directory.Exists(ftemp & "") Then
            Directory.CreateDirectory(ftemp & "")
        End If
        If Not Directory.Exists(fdata) Then
            Directory.CreateDirectory(fdata)
        End If
        If Not File.Exists(filename) Then
            If downloadFilename(url, filename) Then
                Dim runfilename = """" & filename & """"
                Dim info As New ProcessStartInfo()
                info.FileName = "c:\windows\system32\msiexec.exe"
                info.Arguments = " /i """ & ftemp & "\iisexpress.msi"" /qn"
                Process.Start(info)
            Else
                r = False
            End If
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
                'info.Arguments = "/Q /ACTION=install /HIDECONSOLE=1 /UpdateEnabled=0 /FEATURES=SQL " &
                '    "/IACCEPTSQLSERVERLICENSETERMS /INSTANCENAME=OPERAHOUSE " &
                '    "/INSTANCEDIR=""" & fdata & """ " &
                '    "/SECURITYMODE=sql /INSTALLSQLDATADIR=""" & fdata & """ " &
                '    "/SAPWD=""wishforthebest"" /SQLSVCACCOUNT=""client"" /SQLSVCPASSWORD=""wishforthebest"" /SQLSVCSTARTUPTYPE=""Automatic"" " &
                '    "/AGTSVCACCOUNT=""client"" /AGTSVCPASSWORD=""wishforthebest"" /AGTSVCSTARTUPTYPE=1 //INDICATEPROGRESS=1"
                info.Arguments = " /i """ & ftemp & "\SqlLocalDB.msi"" IACCEPTSQLLOCALDBLICENSETERMS=YES /qn"
                Process.Start(info)
            Else
                r = False
            End If
        End If
        Return r
    End Function
    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        stopInstance("OPERAHOUSE")
        deleteInstance("OPERAHOUSE")
        If Directory.Exists(Directory.GetCurrentDirectory & "\" & folderTemp & "") Then
            Dim directoryName As String = Directory.GetCurrentDirectory & "\" & folderTemp & ""
            For Each deleteFile In Directory.GetFiles(directoryName, "*.*", SearchOption.TopDirectoryOnly)
                File.Delete(deleteFile)
            Next
        End If
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Me.tbLog.Clear()
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If isStart Then


            Me.Timer1.Enabled = False

            Dim coreAccount = "oph"
            Dim coreDB = "oph_core"
            Dim dataAccount = tbAccount.Text
            Dim dataDB = dataAccount & "_data"
            Dim user = "sam"
            Dim secret = "f41d5e12-1fa4-420a-8a74-bafbdfff3592"

            Dim p_uri = "http://redbean/" & dataAccount

            If localScript("use " & coreDB, coreDB, pipename) Then
                setLog("Synchronize Starting...")
                'localScript("SET ANSI_NULLS ON", coreDB, pipename)
                'localScript("SET QUOTED_IDENTIFIER ON", coreDB, pipename)
                Dim cmdstr = "exec gen.doSync @p_uri='" & p_uri & "', @s_uri=null, @paccountid='" & dataAccount & "', @saccountid='" & dataAccount & "', @code_preset=null, @isLAN=0, @pwd='" & secret & "', @isdebug=1"
                localScript(cmdstr, coreDB, pipename)
                'doSync(p_uri, coreDB, dataDB, user, secret)
                setLog("Synchronize Completed.")
                Me.Timer1.Enabled = True
            End If
        End If
    End Sub


    Function localScript(sqlstr, db, pipename) As Boolean
        Dim p As Process = New Process()
        p.StartInfo.UseShellExecute = False
        p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.RedirectStandardError = True
        'p.EnableRaisingEvents = True
        'Application.DoEvents()
        'AddHandler p.ErrorDataReceived, AddressOf OutputDataReceived
        'AddHandler p.OutputDataReceived, AddressOf OutputDataReceived

        p.StartInfo.FileName = "sqlcmd.exe"
        p.StartInfo.Arguments = "-S " & pipename & " -Q """ & sqlstr & """" & IIf(db <> "", " -d " & db, "")
        p.StartInfo.CreateNoWindow = True
        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden


        p.Start()

        'p.BeginErrorReadLine()
        'p.BeginOutputReadLine()

        Dim sOutput As String = p.StandardOutput.ReadToEnd()
        p.WaitForExit()
        setLog(sOutput)
        'Dim sOutput = lastMessage

        'If LocalDb Is Not installed Then it will return that 'sqllocaldb' is not recognized as an internal or external command operable program or batch file.
        If (sOutput Is Nothing Or sOutput.Trim().Length = 0 Or sOutput.Contains("does not exist")) Then
            Return False
        Else
            Return True
        End If
        lastMessage = ""
    End Function

    'Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

    '    Dim p As Process()
    '    p.startinfo.filename = "xyz"
    '    p.startinfo.arguments = "...."
    '    p.startinfo.workingdirectory = "some path"
    '    p.startinfo.redirectstandarderror = True
    '    p.startinfo.redirectstandardoutput = True
    '    p.enableraisingevents = True
    '    Application.DoEvents()
    '    AddHandler proc.ErrorDataReceived, AddressOf OutputDataReceived
    '    AddHandler proc.OutputDataReceived, AddressOf OutputDataReceived
    '    p.start()
    '    proc.BeginErrorReadLine()
    '    proc.BeginOutputReadLine()

    'End Sub

    Sub setLog(txt)
        'Exit Sub
        Dim t = IIf(txt = "", "", Now() & " " & txt & vbCrLf)
        If Me.InvokeRequired Then
            Me.Invoke(New Action(Sub() Me.tbLog.AppendText(t)))
        Else
            Me.tbLog.AppendText(t)

        End If

        writeLog(t)
        'Me.tbLog.Select(Me.tbLog.Text.Length + 1, 1)

    End Sub

    'Delegate Sub UpdateTextBoxDelg(text As String)
    'Public myDelegate As UpdateTextBoxDelg = New UpdateTextBoxDelg(AddressOf UpdateTextBox)
    'Public Sub UpdateTextBox(text As String)
    '    Dim t = IIf(text = "", "", Now() & " " & text & vbCrLf)
    '    lastMessage = lastMessage & t

    '    Me.tbLog.Text &= text '& Environment.NewLine
    '    Me.tbLog.SelectionStart = Me.tbLog.Text.Length
    '    Me.tbLog.ScrollToCaret()
    'End Sub

    Public Sub OutputDataReceived(ByVal sender As Object, ByVal e As DataReceivedEventArgs)

        Dim t = IIf(e.Data = "", "", Now() & " " & e.Data & vbCrLf)
        lastMessage = lastMessage & t

        If Me.InvokeRequired = True Then
            'Me.Invoke(myDelegate, e.Data)
            Me.Invoke(New Action(Sub() Me.tbLog.AppendText(t)))
        Else
            'UpdateTextBox(e.Data)
            Me.tbLog.AppendText(t)
        End If
    End Sub

    Sub writeLog(logMessage As String)
        'Dim w As TextWriter
        Dim path = Directory.GetCurrentDirectory() & "\log"
        path = path & "\" '& "OPHContent\log\"
        Dim logFilepath = path & DateTime.Now().Year & "\" & Strings.Right("0" & DateTime.Now().Month, 2) & "\" & Strings.Right("0" & DateTime.Now().Day, 2) & ".txt"
        Dim logPath = path & DateTime.Now().Year & "\" & Strings.Right("0" & DateTime.Now().Month, 2) & "\"

        If (Not System.IO.Directory.Exists(logPath)) Then
            System.IO.Directory.CreateDirectory(logPath)
        End If
        Try
            Using w As StreamWriter = File.AppendText(logFilepath)
                'w.Write(vbCrLf + "Log Entry : ")
                'w.WriteLine("{0} {1}: " + vbCrLf + "{2}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString(), logMessage)
                w.WriteLine("{0}", logMessage)

            End Using

        Catch ex As Exception
            Debug.Write(ex.Message.ToString)
        End Try
    End Sub

End Class
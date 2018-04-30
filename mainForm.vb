
Imports System.Net
Imports System.IO
Imports System.Diagnostics
Imports System.Text
Imports System.Windows.Forms
Imports System.Collections.Generic
Imports Newtonsoft.Json.Linq


Public Class mainForm
    Private Const folderTemp = "temp2"
    Private Const folderData = "data"
    Private pipename As String = ""
    Private isStart = False
    'Private lastMessage As String = ""
    Private eventHandled As Boolean = False
    Private elapsedTime As Integer
    Private iisExpressFolder
    'Private sqlId As Integer
    Private iisId As Integer = 0
    Private accountList As New Dictionary(Of String, accountType)

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If Not IsNothing(Me.lbAcount.SelectedItem) Then
            startSync(Me.lbAcount.SelectedItem)
        Else
            MessageBox.Show(Me, "Please select one of account to start before continue.", "Select Account", vbInformation)
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim curAccount = accountList(Me.lbAcount.SelectedItem)
        'Dim sqlId = 0, iisid = 0
        stopSync(Me.lbAcount.SelectedItem, curAccount.sqlId)
    End Sub

    Sub startSync(accountName)
        Dim curAccount = accountList(accountName)

        Dim coreAccount = "oph"
        Dim coreDB = "oph_core"
        Dim dataAccount = accountName
        Dim dataDB = dataAccount & "_data"
        Dim user = curAccount.user
        Dim secret = curAccount.secret

        Dim p_uri = "http://redbean/" & dataAccount

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

        Dim gitloc = getGITLocation()
        setLog("GIT Location: " & gitloc)

        'startSQLCMDConsole()

        If Not syncLocalScript("use " & coreDB, coreDB, pipename) Then
            Dim mdfFile = Directory.GetCurrentDirectory & "\" & folderData & "\" & coreDB & "_data.mdf"
            Dim ldfFile = Directory.GetCurrentDirectory & "\" & folderData & "\" & coreDB & "_log.ldf"
            syncLocalScript("CREATE DATABASE " & coreDB & " On ( NAME = " & coreDB & "_data, FILENAME = '" & mdfFile & "') Log ON ( NAME = " & coreDB & "_log, FILENAME = '" & ldfFile & "');", "master", pipename)


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

                    localFile = Directory.GetCurrentDirectory & "\" & folderTemp & "\webRequest.dll"
                    If File.Exists(localFile) Then
                        syncLocalScript("EXEC sp_changedbowner 'sa'; ALTER DATABASE " & coreDB & " SET TRUSTWORTHY ON", coreDB, pipename)
                        syncLocalScript("sp_configure 'show advanced options', 1;RECONFIGURE", coreDB, pipename)
                        syncLocalScript("sp_configure 'clr enabled', 1;RECONFIGURE", coreDB, pipename)

                        syncLocalScript("create assembly webRequest from '" & localFile & "' with PERMISSION_SET = unsafe", coreDB, pipename)
                        syncLocalScript("if not exists(select * from sys.objects where name='fn_get_webrequest') begin	declare @sqlstr nvarchar(max)='CREATE FUNCTION [gen].[fn_get_webrequest](@uri [nvarchar](max), @user [nvarchar](255) = N'''', @passwd [nvarchar](255) = N'''') RETURNS [nvarchar](max) WITH EXECUTE AS CALLER AS EXTERNAL NAME [webRequest].[webRequest.Functions].[GET]';	exec sp_executesql @sqlstr; end", coreDB, pipename)
                        syncLocalScript("if not exists(select * from sys.objects where name='fn_post_webrequest') begin	declare @sqlstr nvarchar(max)='CREATE FUNCTION [gen].[fn_post_webrequest](@uri [nvarchar](max), @postdata [nvarchar](max), @user [nvarchar](255) = N'''', @passwd [nvarchar](255) = N'''', @headers [nvarchar](max)) RETURNS [nvarchar](max) WITH EXECUTE AS CALLER AS EXTERNAL NAME [webRequest].[webRequest.Functions].[POST]'; exec sp_executesql @sqlstr; end", coreDB, pipename)
                    End If
                End If
            End If
        End If


        If Not syncLocalScript("use " & dataDB, dataDB, pipename) Then
            Dim sqlstr As String = ""

            Dim p_add = p_uri & "/ophcore/api/sync.aspx"
            Dim url = p_add & "?mode=reqtoken&userid=" & user & "&pwd=" & secret
            Dim r = postHttp(url)

            Dim token = r.Substring(r.IndexOf("<sessionToken>") + Len("<sessionToken>"), r.IndexOf("</sessionToken>") - r.IndexOf("<sessionToken>") - Len("<sessionToken>"))

            url = p_add & "?mode=dbinfo&token=" & token
            r = postHttp(url)

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
                    syncLocalScript("if not exists(select * from sys.databases where name='" & dbname & "') CREATE DATABASE " & dbname & " On ( NAME = " & dbname & "_data, FILENAME = '" & mdfFile & "') Log ON ( NAME = " & dbname & "_log, FILENAME = '" & ldfFile & "');", "master", pipename)

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
            sqlstr2 = sqlstr2 & "use " & coreDB & vbCrLf &
                            "if not exists(select * from acctinfo where accountguid='" & accountGUID & "' and infokey='ODBC') insert into acctinfo (accountguid, infokey, infovalue) values ('" & accountGUID & "', 'ODBC', 'Data Source=(localdb)/operahouse;Initial Catalog=" & dataAccount & "_data;Integrated Security=SSPI;timeout=600')"
            sqlstr2 = sqlstr2 & "use " & dataDB & vbCrLf &
                            "if not exists(select * from acctinfo where accountguid='" & accountGUID & "' and infokey='ODBC') insert into acctinfo (accountguid, infokey, infovalue) values ('" & accountGUID & "', 'ODBC', 'Data Source=(localdb)/operahouse;Initial Catalog=" & dataAccount & "_data;Integrated Security=SSPI;timeout=600')"

            url = "http://redbean/" & dataAccount & "/ophcore/api/sync.aspx?mode=reqcorescript"
            Dim scriptFile = Directory.GetCurrentDirectory & "\" & folderTemp & "\install_" & dataAccount & ".sql"
            runScript(url, pipename, scriptFile, dataDB)

            'run after
            syncLocalScript(sqlstr2, "master", pipename)

        End If

        'sync on
        isStart = True
        Me.Button2.Enabled = True
        Me.Button1.Enabled = False
        Me.Button4.Enabled = False


        'setup applicationhost.config
        iisExpressFolder = getIISLocation()
        setLog("IIS Express Location: " & iisExpressFolder)
        addAccounttoIIS(dataAccount, Directory.GetCurrentDirectory() & "\")
        'run iis
        If iisId = 0 Then runIIS(dataAccount)

        'start sync
        If GetWin32Process("", curAccount.sqlId) <> curAccount.sqlId Or curAccount.sqlId = 0 Then
            setLog("Synchronize Starting...")
            Dim cmdstr = "exec gen.doSync @p_uri='" & p_uri & "', @s_uri=null, @paccountid='" & dataAccount & "', @saccountid='" & dataAccount & "', @code_preset=null, @isLAN=0, @pwd='" & secret & "', @isdebug=0"
            curAccount.sqlId = asynclocalScript(cmdstr, coreDB, pipename)
        End If

    End Sub

    Sub addAccounttoIIS(account, path)
        Dim isexists = False, port As Integer = 8080
        For Each k As String In IO.File.ReadLines(path & folderTemp & "\applicationhost.config")
            If k.Contains("<site name=""" & account & """") Then
                isexists = True
            End If
        Next
        If Not isexists Then
            Dim n = 1
            For Each k As String In IO.File.ReadLines(path & folderTemp & "\applicationhost.config")
                If k.Contains("<site ") Then
                    n = n + 1
                End If
                If k.Contains(port) Then
                    port = port + 1
                End If
            Next
            If Not isexists Then
                Dim newfile As New List(Of String)()

                For Each k As String In IO.File.ReadLines(path & folderTemp & "\applicationhost.config")
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
                    newfile.Add(k)

                Next
                File.Delete(path & folderTemp & "\applicationhost.config")
                System.IO.File.WriteAllLines(path & folderTemp & "\applicationhost.config", newfile.ToArray())
            End If
        End If
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

        AddHandler p.ErrorDataReceived, AddressOf OutputDataReceived
        AddHandler p.OutputDataReceived, AddressOf OutputDataReceived

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

    Sub stopSync(accountName, sqlId)
        isStart = False
        setLog(accountName & " stopping...")
        If GetWin32Process("", sqlId) = sqlId Then
            killProcess(sqlId)
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
            If GetWin32Process("iisexpress", iisId) = iisId Then
                killProcess(iisId)
            End If
            stopInstance("OPERAHOUSE")

        End If
        setLog(accountName & " stopped.")
        Me.Button1.Enabled = True
        Me.Button2.Enabled = False
        Me.Button4.Enabled = True
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
                Try
                    File.Delete(deleteFile)
                Catch ex As Exception

                End Try

            Next
        End If
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Me.tbLog.Clear()
    End Sub

    Function asynclocalScript(sqlstr, db, pipename) As Integer
        eventHandled = False
        elapsedTime = 0

        Dim p As Process = New Process()
        p.StartInfo.UseShellExecute = False
        p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.RedirectStandardError = True
        p.EnableRaisingEvents = True

        AddHandler p.ErrorDataReceived, AddressOf OutputDataReceived
        AddHandler p.OutputDataReceived, AddressOf OutputDataReceived

        p.StartInfo.FileName = "sqlcmd.exe"
        p.StartInfo.Arguments = "-S " & pipename & " -Q """ & sqlstr & """" & IIf(db <> "", " -d " & db, "")
        p.StartInfo.CreateNoWindow = True
        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden


        p.Start()
        Dim sqlId = p.Id

        p.BeginErrorReadLine()
        p.BeginOutputReadLine()
        Return sqlId
    End Function

    Function syncLocalScript(sqlstr, db, pipename) As Boolean
        Dim p As Process = New Process()
        p.StartInfo.UseShellExecute = False
        p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.RedirectStandardError = True

        p.StartInfo.FileName = "sqlcmd.exe"
        p.StartInfo.Arguments = "-S " & pipename & " -Q """ & sqlstr & """" & IIf(db <> "", " -d " & db, "")
        p.StartInfo.CreateNoWindow = True
        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden


        p.Start()

        Dim sOutput As String = p.StandardOutput.ReadToEnd()
        p.WaitForExit()
        setLog(sOutput)

        If (sOutput Is Nothing Or sOutput.Trim().Length = 0 Or sOutput.Contains("does not exist")) Then
            Return False
        Else
            Return True
        End If
    End Function

    Sub SetLog(txt)
        Dim t = IIf(txt = "", "", Now() & " " & txt & vbCrLf)
        If Me.InvokeRequired Then
            Me.Invoke(New Action(Sub() Me.tbLog.AppendText(t)))
        Else
            Me.tbLog.AppendText(t)

        End If

        writeLog(t)
    End Sub

    Public Sub OutputDataReceived(ByVal sender As Object, ByVal e As DataReceivedEventArgs)
        Try
            Dim t = IIf(e.Data = "", "", Now() & " " & e.Data & vbCrLf)
            'lastMessage = lastMessage & t

            If Me.InvokeRequired = True Then
                'Me.Invoke(myDelegate, e.Data)
                Me.Invoke(New Action(Sub() Me.tbLog.AppendText(t)))
            Else
                'UpdateTextBox(e.Data)
                Me.tbLog.AppendText(t)
            End If
            eventHandled = True

            writeLog(t)
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
        Dim isExists = False
        For Each x In Me.lbAcount.Items
            If x.ToString = Me.TextBox1.Text Then
                isExists = True
                Exit For
            End If
        Next
        If Not isExists Then
            Me.lbAcount.Items.Add(Me.TextBox1.Text)
        Else
            MessageBox.Show("Your account has already exists.")
        End If
    End Sub

    Private Sub mainForm_Load(sender As Object, e As EventArgs) Handles Me.Load
        Dim jsonText = My.Resources.SavedAccountList
        Dim json As JObject = JObject.Parse(jsonText)
        Dim al = json("accountList")
        For Each x In al
            Dim an = x("accountName").ToString
            Me.lbAcount.Items.Add(an)
            accountList.Add(an, New accountType With {.user = x("user").ToString, .secret = x("secret").ToString, .sqlId = 0})
        Next
    End Sub

    Private Sub lbAcount_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lbAcount.SelectedIndexChanged
        Dim curAccount = accountList(Me.lbAcount.SelectedItem)
        Me.Button1.Enabled = curAccount.sqlId = 0
        Me.Button2.Enabled = curAccount.sqlId > 0
        Me.Button4.Enabled = curAccount.sqlId = 0
    End Sub
End Class
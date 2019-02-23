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

Public Class functions

    Public Shared Sub CreateAccount(accountName As String, syncStructure As Boolean,
                      curAccount As accountType, ophPath As String,
                      folder As String, folderData As String, folderTemp As String)
        'Dim curAccount = accountList(accountName)

        'Dim ophPath = IIf(My.Settings.isIISExpress = 1 Or My.Settings.OPHPath = "", Directory.GetCurrentDirectory, My.Settings.OPHPath)
        Dim ftemp = ophPath & "\" & folderTemp
        Dim fdata = ophPath & "\" & folderData

        If Not Directory.Exists(ftemp & "") Then
            Directory.CreateDirectory(ftemp & "")
            syncStructure = True    'check again!
        End If
        If Not Directory.Exists(fdata) Then
            Directory.CreateDirectory(fdata)
            syncStructure = True
        End If

        Dim isClearStructure = curAccount.isClearStructure
        If syncStructure Or isClearStructure Then
            Try
                'Me.Timer1.Enabled = False
                Dim coreAccount = "oph"
                Dim coreDB = "oph_core"
                Dim dataAccount = accountName
                Dim dataDB = dataAccount & "_data"
                Dim user = curAccount.user
                Dim secret = curAccount.secret
                Dim port = curAccount.port
                Dim autoStart = curAccount.autoStart

                Dim remoteUrl = My.Settings.remoteUrl
                Dim p_uri = remoteUrl & IIf(remoteUrl.Substring(Len(remoteUrl) - 1, 1) <> "/", "/", "") & dataAccount '"http://springroll/" & dataAccount


                Dim uid = "", pwd = ""
                Dim url = "", odbc = "", address = "", pipename = ""
                Dim token = getToken(dataAccount, user, secret)

                If token <> "" Then
                    Dim isLocaldb = My.Settings.isLocalDB = 1
                    If isLocaldb Then
                        If checkInstance("OPERAHOUSE") <> "OPERAHOUSE" Then
                            installLocalDB(ophPath & "\" & folderTemp, ophPath & "\" & folderData)
                            SetLog("localDB installed")
                            createInstance("OPERAHOUSE")
                            odbc = "Data Source=(localdb)\operahouse;Initial Catalog=" & dataDB & ";integrated security=sspi" 'My.Settings.odbc
                            address = "localhost:" & port
                            SetLog("OPERAHOUSE created")
                        End If

                        startInstance("OPERAHOUSE")
                        SetLog("OPERAHOUSE started")
                        pipename = getPipeName("OPERAHOUSE")
                        SetLog("Pipename: " & pipename, , )
                    Else
                        pipename = My.Settings.dbInstanceName
                        uid = My.Settings.dbUser
                        pwd = My.Settings.dbPassword
                        odbc = "Data Source=" & pipename & ";Initial Catalog=" & dataDB & ";uid=" & uid & ";pwd=" & pwd 'My.Settings.odbc
                        'address = curAccount.address.ToString()

                    End If

                    If syncStructure Then
                        Dim gitloc = getGITLocation(folder, folderTemp, folderData)
                        SetLog("GIT Location: " & gitloc, , )

                        SetLog("Checking core database...")
                        Dim checkCore = True
                        If Not syncLocalScript("use " & coreDB, "master", pipename, uid, pwd) Then
                            Dim mdfFile = ophPath & "\" & folderData & "\" & coreDB & "_data.mdf"
                            Dim ldfFile = ophPath & "\" & folderData & "\" & coreDB & "_log.ldf"
                            If isLocaldb Then
                                'syncLocalScript("CREATE DATABASE " & coreDB & " On ( NAME = " & coreDB & "_data, FILENAME = '" & mdfFile & "', MAXSIZE = UNLIMITED) Log ON ( NAME = " & coreDB & "_log, FILENAME = '" & ldfFile & "', MAXSIZE = UNLIMITED);", "master", pipename, uid, pwd)
                                'syncLocalScript("CREATE DATABASE " & coreDB & " On ( NAME = " & coreDB & "_data, FILENAME = '" & mdfFile & "', MAXSIZE = UNLIMITED) Log ON ( NAME = " & coreDB & "_log, FILENAME = '" & ldfFile & "', MAXSIZE = UNLIMITED);", "master", pipename, uid, pwd)
                                runSQLwithResult("CREATE DATABASE " & coreDB & " On ( NAME = " & coreDB & "_data, FILENAME = '" & mdfFile & "', MAXSIZE = UNLIMITED) Log ON ( NAME = " & coreDB & "_log, FILENAME = '" & ldfFile & "', MAXSIZE = UNLIMITED);", odbc)
                                runSQLwithResult("CREATE DATABASE " & coreDB & " On ( NAME = " & coreDB & "_data, FILENAME = '" & mdfFile & "', MAXSIZE = UNLIMITED) Log ON ( NAME = " & coreDB & "_log, FILENAME = '" & ldfFile & "', MAXSIZE = UNLIMITED);", odbc)
                            Else
                                'syncLocalScript("CREATE DATABASE " & coreDB, "master", pipename, uid, pwd)
                                runSQLwithResult("CREATE DATABASE " & coreDB, odbc)
                            End If

                            syncLocalScript("ALTER DATABASE " & coreDB & " MODIFY FILE (NAME = '" & coreDB & "_log', MAXSIZE = UNLIMITED)", "master", pipename, uid, pwd)
                            SetLog("Building core database completed.")
                        End If
                        '--always check new update'
                        Dim c_uri = remoteUrl & IIf(remoteUrl.Substring(Len(remoteUrl) - 1, 1) <> "/", "/", "") & coreAccount

                        'syncLocalScript("disable trigger table_lock on database", coreDB, pipename, uid, pwd)
                        runSQLwithResult("disable trigger table_lock on database", odbc)

                        url = c_uri & "/ophcore/api/sync.aspx?mode=reqcorescript&token=" & token
                        Dim scriptFile1 = ophPath & "\" & folderTemp & "\install_core.sql"
                        SetLog(scriptFile1, , True)
                        runScript(url, pipename, scriptFile1, coreDB, uid, pwd)
                        If File.Exists(scriptFile1) Then
                            SetLog("Installing core database completed.")
                        Else
                            SetLog("Installing core database NOT completed.")
                            checkCore = False
                        End If

                        If checkCore Then
                            Dim localFile1 = ophPath & "\" & folderTemp & "\sync.zip"

                            If Not File.Exists(localFile1) Then
                                url = p_uri & "/ophcore/api/sync.aspx?mode=webrequestFile"
                                If downloadFilename(url, localFile1) Then
                                    unZip(localFile1, ophPath & "\" & folderTemp)
                                End If
                            End If

                            SetLog("Checking core database completed.")
                        Else
                            SetLog("Checking core database NOT completed.")
                        End If

                        'download from git
                        If My.Settings.noWeb.ToString = "0" Then
                            SetLog("Checking web application folder...")
                            runCmd(ophPath & "\" & folderTemp & "\build-oph.bat", ophPath)
                            SetLog("Checking web application folder completed.")
                        End If

                        SetLog("Checking required files...")
                        Dim localFile2 = ophPath & "\" & folderTemp & "\webRequest.dll"
                        If File.Exists(localFile2) Then
                            syncLocalScript("EXEC sp_changedbowner 'sa'; ALTER DATABASE " & coreDB & " SET TRUSTWORTHY ON", coreDB, pipename, uid, pwd)
                            syncLocalScript("sp_configure 'show advanced options', 1;RECONFIGURE", coreDB, pipename, uid, pwd)
                            syncLocalScript("sp_configure 'clr enabled', 1;RECONFIGURE", coreDB, pipename, uid, pwd)

                            If isLocaldb Then
                                syncLocalScript("if not exists(select * from sys.assemblies where name='webRequest') create assembly webRequest from '" & localFile2 & "' with PERMISSION_SET = unsafe", coreDB, pipename, uid, pwd)
                            Else
                                Dim odbc1 = "Data Source=" & pipename & ";Initial Catalog=" & coreDB & ";uid=" & uid & ";pwd=" & pwd 'My.Settings.odbc
                                Dim x = runSQLwithResult("if not exists(select * from sys.assemblies where name='webRequest') select 1", odbc1)
                                If x = "1" Then MessageBox.Show("Please add webRequest.dll in oph_core database manually. Please OK when continue...")
                            End If
                        End If

                        syncLocalScript("if not exists(select * from sys.objects where name='fn_get_webrequest') begin	declare @sqlstr nvarchar(max)='CREATE FUNCTION [gen].[fn_get_webrequest](@uri [nvarchar](max), @user [nvarchar](255) = N'''', @passwd [nvarchar](255) = N'''') RETURNS [nvarchar](max) WITH EXECUTE AS CALLER AS EXTERNAL NAME [webRequest].[webRequest.Functions].[GET]';	exec sp_executesql @sqlstr; end", coreDB, pipename, uid, pwd)
                        syncLocalScript("if not exists(select * from sys.objects where name='fn_post_webrequest') begin	declare @sqlstr nvarchar(max)='CREATE FUNCTION [gen].[fn_post_webrequest](@uri [nvarchar](max), @postdata [nvarchar](max), @user [nvarchar](255) = N'''', @passwd [nvarchar](255) = N'''', @headers [nvarchar](max)) RETURNS [nvarchar](max) WITH EXECUTE AS CALLER AS EXTERNAL NAME [webRequest].[webRequest.Functions].[POST]'; exec sp_executesql @sqlstr; end", coreDB, pipename, uid, pwd)
                        SetLog("Checking required files completed.")
                    End If

                    Dim sqlstr2 As String = ""
                    Dim sqlstr As String = ""

                    'If Not syncLocalScript("use " & dataDB, dataDB, pipename, uid, pwd) Then

                    SetLog("Checking account databases...")
                    Dim checkAccount = True
                    Dim p_add = p_uri & "/ophcore/api/sync.aspx"
                    Dim r = ""
                    url = ""
                    If token <> "" Then
                        url = p_add & "?mode=dbinfo&token=" & token
                        r = postHttp(url)
                    End If
                    SetLog(url, "", )
                    SetLog(r, "", )
                    If r = "" Then checkAccount = False

                    If checkAccount Then
                        Dim sep() As String = {"<?xml version=""1.0"" encoding=""utf-8""?>", "<sqroot>", "<databases>", "<database>", "</database>", "</databases>", "</sqroot>"}
                        Dim r1 = r.Split(sep, StringSplitOptions.RemoveEmptyEntries)
                        Dim accountGUID As String = ""

                        Dim sep1() As String = {"<AccountGUID>", "<AccountDBGUID>", "<databasename>", "<isMaster>", "<version>", "</AccountGUID>", "</AccountDBGUID>", "</databasename>", "</isMaster>", "</version>"}
                        For Each r1x In r1
                            Dim r2 = r1x.Split(sep1, StringSplitOptions.RemoveEmptyEntries)
                            If r2.Length = 5 Then
                                accountGUID = r2(0)
                                Dim accountDBGUID = r2(1)
                                Dim dbname = r2(2)
                                Dim ismaster = r2(3)
                                Dim Version = r2(4)
                                Dim mdfFile = ophPath & "\" & folderData & "\" & dbname & "_data.mdf"
                                Dim ldfFile = ophPath & "\" & folderData & "\" & dbname & "_log.ldf"
                                If isLocaldb Then
                                    syncLocalScript("if not exists(select * from sys.databases where name='" & dbname & "') CREATE DATABASE " & dbname & " On ( NAME = " & dbname & "_data, FILENAME = '" & mdfFile & "', MAXSIZE = UNLIMITED) Log ON ( NAME = " & dbname & "_log, FILENAME = '" & ldfFile & "', MAXSIZE = UNLIMITED);", "master", pipename, uid, pwd)
                                Else
                                    syncLocalScript("if not exists(select * from sys.databases where name='" & dbname & "') CREATE DATABASE " & dbname, "master", pipename, uid, pwd)
                                End If
                                syncLocalScript("ALTER DATABASE " & dbname & " MODIFY FILE (NAME = '" & dbname & "_log', MAXSIZE = UNLIMITED)", "master", pipename, uid, pwd)

                                If coreDB <> dataDB Then
                                    sqlstr2 = sqlstr2 & "use " & coreDB & vbCrLf &
                                        "if not exists(select * from acct where accountid='" & dataAccount & "') insert into acct (accountguid, accountid) values ('" & accountGUID & "', '" & dataAccount & "')" & vbCrLf &
                                        "if not exists(select * from acctdbse where accountdbguid='" & accountDBGUID & "') insert into acctdbse (accountdbguid, accountguid, databasename, ismaster, version) values ('" & accountDBGUID & "', '" & accountGUID & "', '" & dbname & "', '" & ismaster & "', '" & Version & "')" & vbCrLf &
                                        "use " & dataDB & vbCrLf &
                                        "if not exists(select * from acct where accountid='" & dataAccount & "') insert into acct (accountguid, accountid) values ('" & accountGUID & "', '" & dataAccount & "')" & vbCrLf &
                                        "if not exists(select * from acctdbse where accountdbguid='" & accountDBGUID & "') insert into acctdbse (accountdbguid, accountguid, databasename, ismaster, version) values ('" & accountDBGUID & "', '" & accountGUID & "', '" & dbname & "', '" & ismaster & "', '" & Version & "')" & vbCrLf
                                End If
                                'SetLog(sqlstr2,, True)
                            End If
                        Next

                        'SetLog("Building account databases completed.")

                        Dim info1() As String = {"<info>", "</info>", "<data ", "/>"}
                        For Each r1x In r1
                            Dim r2 = r1x.Split(info1, StringSplitOptions.RemoveEmptyEntries)
                            If r2.Length > 1 Then
                                For Each r2x In r2
                                    Dim r3 = r2x.Split({"key=""", "value=""", """ "}, StringSplitOptions.RemoveEmptyEntries)
                                    If r3.Length > 1 Then   'not empty
                                        sqlstr2 = sqlstr2 & "use " & coreDB & vbCrLf &
                                            "if not exists(select * from acctinfo where accountguid='" & accountGUID & "' and infokey='" & r3(0) & "') insert into acctinfo (accountguid, infokey, infovalue) values ('" & accountGUID & "', '" & r3(0) & "', '" & r3(1).Replace("'", "''") & "') else update acctinfo set infovalue='" & r3(1).Replace("'", "''") & "' where infokey='" & r3(0) & "' and accountguid='" & accountGUID & "'" & vbCrLf
                                        If coreDB <> dataDB Then
                                            sqlstr2 = sqlstr2 & "use " & dataDB & vbCrLf &
                                            "if not exists(select * from acctinfo where accountguid='" & accountGUID & "' and infokey='" & r3(0) & "') insert into acctinfo (accountguid, infokey, infovalue) values ('" & accountGUID & "', '" & r3(0) & "', '" & r3(1).Replace("'", "''") & "') else update acctinfo set infovalue='" & r3(1).Replace("'", "''") & "' where infokey='" & r3(0) & "' and accountguid='" & accountGUID & "'" & vbCrLf
                                        End If
                                    End If
                                Next

                            End If

                        Next

                        'odbc
                        sqlstr2 = sqlstr2 & "use " & coreDB & vbCrLf &
                        "if not exists(select * from acctinfo where accountguid='" & accountGUID & "' and infokey='ODBC') insert into acctinfo (accountguid, infokey, infovalue) values ('" & accountGUID & "', 'odbc', '" & odbc.Replace("'", "''") & "') else update acctinfo set infovalue='" & odbc.Replace("'", "''") & "' where infokey='odbc' and accountguid='" & accountGUID & "'" & vbCrLf
                        If coreDB <> dataDB Then
                            sqlstr2 = sqlstr2 & "use " & dataDB & vbCrLf &
                                "if not exists(select * from acctinfo where accountguid='" & accountGUID & "' and infokey='ODBC') insert into acctinfo (accountguid, infokey, infovalue) values ('" & accountGUID & "', 'odbc', '" & odbc.Replace("'", "''") & "') else update acctinfo set infovalue='" & odbc.Replace("'", "''") & "' where infokey='odbc' and accountguid='" & accountGUID & "'" & vbCrLf
                        End If
                        'SetLog(sqlstr2,, True)
                        'address
                        sqlstr2 = sqlstr2 & "use " & coreDB & vbCrLf &
                            "if not exists(select * from acctinfo where accountguid='" & accountGUID & "' and infokey='address') insert into acctinfo (accountguid, infokey, infovalue) values ('" & accountGUID & "', 'address', '" & address.Replace("'", "''") & "') else update acctinfo set infovalue='" & address.Replace("'", "''") & "' where infokey='address' and accountguid='" & accountGUID & "'" & vbCrLf
                        If coreDB <> dataDB Then
                            sqlstr2 = sqlstr2 & "use " & dataDB & vbCrLf &
                            "if not exists(select * from acctinfo where accountguid='" & accountGUID & "' and infokey='address') insert into acctinfo (accountguid, infokey, infovalue) values ('" & accountGUID & "', 'address', '" & address.Replace("'", "''") & "') else update acctinfo set infovalue='" & address.Replace("'", "''") & "' where infokey='address' and accountguid='" & accountGUID & "'" & vbCrLf
                        End If
                        'SetLog(sqlstr2,, True)
                        'white address
                        sqlstr2 = sqlstr2 & "use " & coreDB & vbCrLf &
                            "if not exists(select * from acctinfo where accountguid='" & accountGUID & "' and infokey='whiteAddress') insert into acctinfo (accountguid, infokey, infovalue) values ('" & accountGUID & "', 'whiteAddress', '" & address.Replace("'", "''") & "') else update acctinfo set infovalue='" & address.Replace("'", "''") & "' where infokey='whiteAddress' and accountguid='" & accountGUID & "'" & vbCrLf
                        If coreDB <> dataDB Then
                            sqlstr2 = sqlstr2 & "use " & dataDB & vbCrLf &
                            "if not exists(select * from acctinfo where accountguid='" & accountGUID & "' and infokey='whiteAddress') insert into acctinfo (accountguid, infokey, infovalue) values ('" & accountGUID & "', 'whiteAddress', '" & address.Replace("'", "''") & "') else update acctinfo set infovalue='" & address.Replace("'", "''") & "' where infokey='whiteAddress' and accountguid='" & accountGUID & "'" & vbCrLf
                        End If
                        'End If
                        'SetLog(sqlstr2,, True)
                        syncLocalScript("disable trigger table_lock on database", coreDB, pipename, uid, pwd)
                        If coreDB <> dataDB Then
                            syncLocalScript("disable trigger table_lock on database", dataDB, pipename, uid, pwd)
                        End If

                        syncLocalScript("update acctinfo set infovalue=''" & Now() & "'' where infokey='lock_hold'", coreDB, pipename, uid, pwd)
                            syncLocalScript("update acctinfo set infovalue=''" & Now() & "'' where infokey='lock_hold'", dataDB, pipename, uid, pwd)

                        If coreDB <> dataDB Then
                            url = p_uri & "/ophcore/api/sync.aspx?mode=reqcorescript&token=" & token
                            Dim scriptFile = ophPath & "\" & folderTemp & "\install_" & dataAccount & ".sql"
                            SetLog(url, , True)
                            SetLog(scriptFile, , True)
                            runScript(url, pipename, scriptFile, dataDB, uid, pwd)
                            SetLog("Installing account databases completed.")
                        End If
                    End If
                        'run after
                        If sqlstr2 <> "" Then
                        'syncLocalScript(sqlstr2, "master", pipename, uid, pwd)
                        runSQLwithResult(sqlstr2, odbc)
                        SetLog("Checking account databases completed.")
                    End If

                    'setup applicationhost.config
                    SetLog("Checking IIS Files...")
                    addAccounttoIIS(dataAccount, ophPath & "\", port, folderData, folderTemp, False)
                    Dim isReady = addWebConfig(ophPath & "\")
                    SetLog("Checking IIS Files completed.")
                    syncStructure = False
                    curAccount.isClearStructure = False
                Else
                    SetLog("User or password is invalid.")
                End If


            Catch ex As Exception
                'If isDebug Then MessageBox.Show(ex.Message)
                SetLog("create account " & ex.Message, , True)
            End Try

            'Me.Timer1.Enabled = True
        End If
    End Sub
    Shared Function getToken(dataAccount, user, secret) As String
        Dim token = ""
        Dim remoteUrl = My.Settings.remoteUrl
        'Dim p_uri = remoteUrl & dataAccount
        Dim p_uri = remoteUrl & IIf(remoteUrl.Substring(Len(remoteUrl) - 1, 1) <> "/", "/", "") & dataAccount '"http://springroll/" & dataAccount
        'Dim curAccount = accountList(dataAccount)
        'Dim user = curAccount.user
        'Dim secret = curAccount.secret

        Dim p_add = p_uri & "/ophcore/api/sync.aspx"
        Dim url = p_add & "?mode=reqtoken&userid=" & user & "&pwd=" & secret
        Dim r = postHttp(url)
        Dim m = ""
        If r.IndexOf("<message>") >= 0 Then
            m = r.Substring(r.IndexOf("<message>") + Len("<message>"), r.IndexOf("</message>") - r.IndexOf("<message>") - Len("<message>"))
        End If

        If m = "" And r.IndexOf("<sessionToken>") >= 0 Then
            token = r.Substring(r.IndexOf("<sessionToken>") + Len("<sessionToken>"), r.IndexOf("</sessionToken>") - r.IndexOf("<sessionToken>") - Len("<sessionToken>"))
        Else
            SetLog(url, True)
            SetLog(r, True)
        End If
        Return token
    End Function
    Shared Function getGITLocation(folder, folderTemp, folderData) As String
        Dim ophPath = IIf(My.Settings.isIISExpress = 1 Or My.Settings.OPHPath = "", Directory.GetCurrentDirectory, My.Settings.OPHPath)
        Dim r = "C:\Program Files\GIT\git-bash.exe"
        If r = "" Or Not File.Exists(r) Then
            Dim c = MsgBox("We cannot find GIT. Press Yes to location it for us. Press No to install from our repository or Cancel do it later.", vbYesNoCancel, "GIT")
            If c = vbYes Then
                'Dim folder = Me.FolderBrowserDialog1.ShowDialog()
                If File.Exists(folder & "\git-bash.exe") Then
                    r = folder & "\git-bash.exe"
                End If
            ElseIf c = vbNo Then
                installGIT(ophPath & "\" & folderTemp, ophPath & "\" & folderData)
                MessageBox.Show("When the GIT has been installed, please press OK to continue.")
                SetLog("GIT Installed")
            End If
        End If

        Return r
    End Function

    Shared Function installGIT(ftemp, fdata) As Boolean
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
    Shared Function installLocalDB(ftemp, fdata) As Boolean
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
    Shared Function downloadFilename(url, localpath) As Boolean
        Dim r = True
        Dim wc As New WebClient()
        Try
            wc.DownloadFile(url, localpath)
        Catch ex As Exception
            SetLog("downloadFileName " & url & " " & localpath & " " & ex.Message, , True)
            r = False
        End Try
        Return r
    End Function
    Shared Function getPipeName(instanceName) As String
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
    Shared Function checkInstance(instanceName) As String
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
    Shared Sub createInstance(instanceName)
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
    Shared Sub deleteInstance(instanceName)
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
    Shared Sub startInstance(instanceName)
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



    Shared Function syncLocalScript(sqlstr, db, pipename, uid, pwd) As Boolean
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
    Shared Function runSQLwithResult(ByVal sqlstr As String, Optional ByVal sqlconstr As String = "") As String
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
    Shared Function runScript(url, pipename, scriptFile, db, uid, pwd) As Boolean
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
            SetLog(sOutput, , )
        Else
            r = False
        End If
        Return r
    End Function

    Shared Sub unZip(zipPath, extractPath)
        ZipFile.ExtractToDirectory(zipPath, extractPath)
    End Sub
    Shared Sub runCmd(filename As String, Optional workingPath As String = "")
        Dim p As Process = New Process()
        p.StartInfo.UseShellExecute = False
        p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.FileName = filename
        If workingPath <> "" Then p.StartInfo.WorkingDirectory = workingPath
        p.StartInfo.Arguments = " "
        p.StartInfo.CreateNoWindow = True
        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
        p.Start()
        Dim sOutput As String = p.StandardOutput.ReadToEnd()
        p.WaitForExit()
        SetLog(sOutput, , )
    End Sub



    Shared Function postHttp(uri As String, Optional postData As String = "", Optional username As String = "", Optional passwd As String = "", Optional headers As String = "") As String
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

    Shared Function addAccounttoIIS(account As String, path As String, port As String, folderData As String, folderTemp As String, Optional isRemoved As Boolean = False) As Integer
        Try

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
        Catch ex As Exception
            WriteLog(path)
        End Try
        'End If
        Return port
    End Function

    Shared Sub SetLog(txt, Optional title = "", Optional isShow = True)
        Dim t = IIf(txt = "", "", Now() & " " & title & ": " & txt & vbCrLf)
        'If isShow Then
        'If Me.InvokeRequired Then
        'Me.Invoke(New Action(Sub() Me.tbLog.AppendText(t)))
        'Else
        'Me.tbLog.AppendText(t)

        'End If
        'End If
        'If Len(t) > 0 Then NotifyIcon1.BalloonTipText = t.ToString.TrimStart().Substring(1, 20) & IIf(Len(t) > 20, "...", "")
        WriteLog(t)
    End Sub
    Shared Sub WriteLog(logMessage As String)
        Dim ophPath = IIf(My.Settings.isIISExpress = 1 Or My.Settings.OPHPath = "", Directory.GetCurrentDirectory, My.Settings.OPHPath)
        Dim path = ophPath & "\log"
        path = path & "\" '& "OPHContent\log\"
        Dim logFilepath = path & DateTime.Now().Year & "\" & Strings.Right("0" & DateTime.Now().Month, 2) & "\ophbox_" & Strings.Right("0" & DateTime.Now().Day, 2) & ".txt"
        Dim logPath = path & DateTime.Now().Year & "\" & Strings.Right("0" & DateTime.Now().Month, 2) & "\"

        If (Not System.IO.Directory.Exists(logPath)) Then
            System.IO.Directory.CreateDirectory(logPath)
        End If
        Try
            Using w As StreamWriter = File.AppendText(logFilepath)
                w.WriteLine("{0}", logMessage)

            End Using

        Catch ex As Exception
            Debug.Write("writelog " & ex.Message.ToString)
        End Try
    End Sub
End Class

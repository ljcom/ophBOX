Imports System.Data.OleDb
Imports System.Data.SqlClient
Imports System.IO
Imports System.IO.Compression
Imports System.Net
Imports System.Text

Public NotInheritable Class FunctionList

    Private Shared oInstance As FunctionList = New FunctionList
    Private sqlError As String

    Private Sub New()
    End Sub

    Public Shared ReadOnly Property Instance As FunctionList
        Get
            Return oInstance
        End Get
    End Property

    Public Function getTag(node As TreeNode, tagName As String) As String
        Dim tagVal = ""
        Dim curTag = node.Tag
        For Each t In curTag.split(";")
            If t.split("=")(0) = tagName Then  'new
                tagVal = t.split("=")(1)
            End If
        Next
        Return tagVal
    End Function

    Public Sub SetLog(txt As String, Optional title As String = "", Optional isShow As Boolean = True)
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
    Public Sub WriteLog(logMessage As String)
        'Dim ophPath = IIf(My.Settings.isIISExpress = 1 Or My.Settings.OPHPath = "", Directory.GetCurrentDirectory, My.Settings.OPHPath)
        Dim ophPath = My.Settings.ophFolder

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
    Public Function runSQLwithResult(ByVal sqlstr As String, Optional ByVal sqlconstr As String = "", Optional ByRef contentofError As String = "") As String
        Dim result As String ', contentofError As String

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
            myCommand.CommandTimeout = 600
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
            contentofError = ex.Message
            MessageBox.Show(contentofError, "Error")
            Return ""
        Catch ex As Exception

            contentofError = ex.Message
            MessageBox.Show(contentofError, "Error")
            Return ""
        Finally
            myCommand.Connection.Close()
            myConnection.Close()
            myConnection.Dispose()
        End Try
        Return result
    End Function

    Public Function setDS(ByRef ds As DataSet, ByRef adapter As SqlDataAdapter, ByVal query As String, ByVal Optional sqlconstr As String = "", Optional ByRef contentofError As String = "") As Boolean
        Dim r As Boolean = False
        Dim myConnectionString As String = sqlconstr

        ds = New DataSet
        Dim conn As New SqlConnection(myConnectionString)
        adapter = New SqlDataAdapter
        Try
            adapter.SelectCommand = New SqlCommand(query, conn)
            adapter.SelectCommand.CommandTimeout = 0
            adapter.Fill(ds)
            ds.AcceptChanges()
            r = True
        Catch ex As SqlException
            contentofError = query & ex.Message & "<br>"
        Catch ex As Exception
            contentofError = query & ex.Message & "<br>"
        Finally
            conn.Close()

        End Try
        Return r

    End Function

    Public Function saveDS(ByRef ds As DataSet, ByRef adapter As SqlDataAdapter, table As String, Optional ByRef contentofError As String = "") As Boolean
        Dim r = False
        Dim builder As SqlCommandBuilder = New SqlCommandBuilder(adapter)
        Try
            adapter.UpdateCommand = builder.GetUpdateCommand(True)
            adapter.InsertCommand = builder.GetInsertCommand(True)
            adapter.DeleteCommand = builder.GetDeleteCommand(True)
            ds.Tables(0).TableName = table
            adapter.Update(ds, table)
            'ds.AcceptChanges
            r = True
        Catch ex As SqlException
            contentofError = table & ex.Message & "<br>"
        Catch ex As Exception
            contentofError = table & ex.Message & "<br>"
        Finally
        End Try
        Return r

    End Function

    Public Function SelectSqlSrvRows(ByVal query As String, ByVal Optional sqlconstr As String = "", Optional ByRef contentofError As String = "") As DataTable

        Dim dt As DataTable = Nothing
        Dim myConnectionString As String = sqlconstr
        'If sqlconstr = "" Then myConnectionString = contentOfdbODBC

        Dim conn As New SqlConnection(myConnectionString)
        Dim adapter As New SqlDataAdapter
        Dim dataSet As New DataSet
        Try
            adapter.SelectCommand = New SqlCommand(query, conn)
            adapter.SelectCommand.CommandTimeout = 0
            adapter.Fill(dataSet)

        Catch ex As SqlException
            contentofError = query & ex.Message & "<br>"
        Catch ex As Exception
            contentofError = query & ex.Message & "<br>"
        Finally
            conn.Close()

        End Try
        adapter = Nothing
        'GC.Collect()
        If dataSet.Tables.Count > 0 Then
            dt = dataSet.Tables(0)
        End If
        Return dt

    End Function
    Public Function syncLocalScript(sqlstr As String, db As String, pipename As String, uid As String, pwd As String, Optional isSQLAuth As Boolean = True) As Boolean
        Dim p As Process = New Process()
        p.StartInfo.UseShellExecute = False
        p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.RedirectStandardError = True

        p.StartInfo.FileName = "sqlcmd.exe"
        p.StartInfo.Arguments = "-S " & pipename & " -Q """ & sqlstr & """" & IIf(db <> "", " -d " & db, "") & IIf(uid <> "", " -U " & uid & " -P " & pwd, "-E")
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

    Public Function runScript(url As String, pipename As String, scriptFile As String, db As String, uid As String, pwd As String, Optional isSQLAuth As Boolean = True, Optional isoverwrite As Boolean = True) As Boolean
        Dim r = True
        If File.Exists(scriptFile) And isoverwrite Then File.Delete(scriptFile)
        If File.Exists(scriptFile) OrElse downloadFilename(url, scriptFile) Then
            Dim p As Process = New Process()
            p.StartInfo.UseShellExecute = False
            'p.StartInfo.RedirectStandardOutput = True
            p.StartInfo.RedirectStandardError = True
            p.StartInfo.FileName = "sqlcmd.exe"
            p.StartInfo.Arguments = "-S " & pipename & " -d " & db & " -i """ & scriptFile & """" & IIf(uid <> "", " -U " & uid & " -P " & pwd, "-E")
            p.StartInfo.CreateNoWindow = True
            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
            p.Start()

            Dim sOutput As String = p.StandardError.ReadToEnd()
            p.WaitForExit()
            SetLog(sOutput, , )
        Else
            r = False
        End If
        Return r
    End Function
    Public Function getToken(remoteUrl As String, user As String, secret As String) As String
        Dim token = ""
        'Dim p_uri = remoteUrl & dataAccount
        Dim p_uri = remoteUrl   '"http://springroll/" & dataAccount
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
    Public Function postHttp(uri As String, Optional postData As String = "", Optional username As String = "", Optional passwd As String = "", Optional headers As String = "") As String
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

    Public Function addAccounttoIIS(account As String, server As String, path As String, port As String, Optional isRemoved As Boolean = False) As Integer
        Dim folderTemp = "temp"
        server = server.Replace("(", "").Replace(")", "").Replace("\", "")
        Try
            If account <> "" Then
                Dim issite = False
                Dim isexists = False ', port As Integer = 8080
                For Each k As String In IO.File.ReadLines(path & folderTemp & "\applicationhost.config")
                    If k.Contains("<site name=""oph" & server & """") Then
                        issite = True
                    End If

                    If k.Contains("<application path = ""/" & account & """") Then
                        isexists = True
                    End If
                Next
                'If Not isexists Then
                Dim n = 1
                If Not issite Then
                    For Each k As String In IO.File.ReadLines(path & folderTemp & "\applicationhost.config")
                        If k.Contains("<site name") Then
                            n = n + 1
                        End If
                    Next
                End If
                Dim newfile As New List(Of String)()
                Dim skipLine As Boolean = False
                Dim curAccount = ""
                Dim cursite = ""
                Dim nn = 1
                For Each k As String In IO.File.ReadLines(path & folderTemp & "\applicationhost.config")
                    If k.Contains("<site ") Then
                        cursite = ""
                        If k.Contains("<site name=""oph" & server & """") Then
                            cursite = server
                        End If
                    End If
                    If k.Contains("<application path") Then
                        If k.Contains("<application path = ""/" & account & """") Then
                            curAccount = account
                        Else
                            curAccount = ""
                        End If
                    End If

                    If Not issite And Not isexists Then
                        If k.Contains("<siteDefaults>") Then
                            Dim newline = {
                                vbTab & vbTab & vbTab & "<site name=""oph" & server & """ id=""" & n & """>",
                                vbTab & vbTab & vbTab & vbTab & "<application path = ""/"" applicationPool=""Clr4IntegratedAppPool"">",
                                vbTab & vbTab & vbTab & vbTab & vbTab & "<virtualDirectory path = ""/"" physicalPath=""" & path & "operahouse\home"" />",
                                vbTab & vbTab & vbTab & vbTab & "</application>",
                                vbTab & vbTab & vbTab & vbTab & "<application path = ""/" & account & """ applicationPool=""Clr4IntegratedAppPool"">",
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
                    ElseIf issite And Not isexists Then
                        If cursite = server And k.Contains("operahouse\home") Then
                            Dim newline = {
                                vbTab & vbTab & vbTab & vbTab & "</application>",   'tutup yang sebelumnya
                                vbTab & vbTab & vbTab & vbTab & "<application path = ""/" & account & """ applicationPool=""Clr4IntegratedAppPool"">",
                                vbTab & vbTab & vbTab & vbTab & vbTab & "<virtualDirectory path = ""/"" physicalPath=""" & path & "operahouse\core"" />",
                                vbTab & vbTab & vbTab & vbTab & vbTab & "<virtualDirectory path = ""/OPHContent/cdn"" physicalPath=""" & path & "cdn"" />",
                                vbTab & vbTab & vbTab & vbTab & vbTab & "<virtualDirectory path = ""/OPHContent/log"" physicalPath=""" & path & "log"" />",
                                vbTab & vbTab & vbTab & vbTab & vbTab & "<virtualDirectory path = ""/OPHContent/themes"" physicalPath=""" & path & "operahouse\themes"" />",
                                vbTab & vbTab & vbTab & vbTab & vbTab & "<virtualDirectory path = ""/OPHContent/documents"" physicalPath=""" & path & "operahouse\documents"" />"
                                }
                            skipLine = True
                            newfile.Add(k)
                            For Each line As String In newline
                                newfile.Add(line)
                            Next

                        End If
                    Else
                    End If
                    If cursite = server And k.Contains("<binding protocol = ""http""") And Not isRemoved Then
                        Dim line = vbTab & vbTab & vbTab & vbTab & vbTab & "<binding protocol = ""http"" bindingInformation=""*:" & port & ":localhost"" />"
                        newfile.Add(line)
                        skipLine = True
                        curAccount = ""
                    End If
                    If cursite = server And curAccount = account And isRemoved Then
                        If k.Contains("<application path") Or k.Contains("<virtualDirectory") Or k.Contains("</application>") Then
                            skipLine = True
                        End If
                        If k.Contains("</site>") Then
                            'skipLine = True
                            curAccount = ""
                        End If
                    End If
                    'If k.Contains("<site ") Then
                    'Dim line = k.Substring(0, k.IndexOf("id") - 1) & " id=""" & nn & """>"
                    'newfile.Add(line)
                    'skipLine = True
                    'nn += 1
                    'End If
                    If Not skipLine Then newfile.Add(k)
                    skipLine = False

                Next

                File.Delete(path & folderTemp & "\applicationhost.config")
                System.IO.File.WriteAllLines(path & folderTemp & "\applicationhost.config", newfile.ToArray())
            End If
        Catch ex As Exception
            WriteLog(path)
        End Try
        'End If
        Return port
    End Function
    Public Function downloadFilename(url, localpath) As Boolean
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

    Public Sub unZip(zipPath, extractPath)
        ZipFile.ExtractToDirectory(zipPath, extractPath)
    End Sub
    Public Sub runCmd(filename As String, Optional workingPath As String = "", Optional isVisible As Boolean = False)
        Dim p As Process = New Process()
        p.StartInfo.UseShellExecute = False
        If isVisible Then p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.RedirectStandardError = True

        p.StartInfo.FileName = filename
        If workingPath <> "" Then p.StartInfo.WorkingDirectory = workingPath
        p.StartInfo.Arguments = " "
        p.StartInfo.CreateNoWindow = Not isVisible
        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
        p.Start()

        'Dim sOutput As String = p.StandardOutput.ReadToEnd()
        Dim sErr As String = p.StandardError.ReadToEnd()
        p.WaitForExit()
        'SetLog(sOutput, , )
        SetLog(sErr, , )
    End Sub

    Public Function installGIT(ftemp, fdata) As Boolean
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

    Public Function addWebConfig(path As String, Optional isforced As Boolean = False) As Boolean
        Dim isLocalDB = 0 'My.Settings.isLocalDB = 1

        Dim r = False
        If File.Exists(path & "operahouse\core\sample-web.config") Then
            r = True
            If isforced And File.Exists(path & "operahouse\core\web.config") Then File.Delete(path & "operahouse\core\web.config")
            If Not File.Exists(path & "operahouse\core\web.config") Then
                Dim newfile As New List(Of String)()
                Dim newline = {}
                For Each k As String In IO.File.ReadLines(path & "operahouse\core\sample-web.config")
                    If k.Contains("<add key=""Sequoia""") Then
                        If isLocalDB Then
                            newline = {
                                    vbTab & "<add key=""Sequoia"" value=""Data Source=(localdb)\operahouse;Initial Catalog=oph_core;Integrated Security=SSPI;timeout=600"" />"}
                        Else
                            Dim serverdb = My.Settings.localServer
                            Dim uid = My.Settings.localUserID
                            Dim pwd = My.Settings.LocalPwd
                            newline = {
                                    vbTab & "<add key=""Sequoia"" value=""Data Source=" & serverdb & ";Initial Catalog=oph_core;user id=" & uid & ";password=" & pwd & ";timeout=600"" />"}
                        End If

                        For Each line As String In newline
                            newfile.Add(line)
                        Next
                    Else
                        newfile.Add(k)
                    End If


                Next
                'File.Delete(path & folderTemp & "\web.config")
                System.IO.File.WriteAllLines(path & "operahouse\core\web.config", newfile.ToArray())
                r = True
            End If
        End If
        Return r
    End Function


    Public Function findFile(path, pattern) As String
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
    Public Function createServer(pipename As String, isSQLAuth As Boolean, uid As String, pwd As String, tuser As String, secret As String, ophPath As String, ophserver As String) As Boolean
        Dim r = False
        Dim Odbc = "Data Source=" & pipename & ";Initial Catalog=master;" & IIf(uid <> "", "User Id=" & uid & ";password=" & pwd, "trusted connection=yes")
        Dim coreDB = "oph_core"
        Dim folderTemp = "temp"
        Dim result = runSQLwithResult("CREATE DATABASE " & coreDB, Odbc)
        If result = "" Then

            Dim c_uri = ophserver & "/oph"
            Dim token = getToken(c_uri, tuser, secret)
            Dim url = c_uri & "/ophcore/api/sync.aspx?mode=reqcorescript&token=" & token
            Dim scriptFile1 = ophPath & "\" & folderTemp & "\install_core.sql"
            SetLog(scriptFile1, , True)

            runScript(url, pipename, scriptFile1, coreDB, uid, pwd, isSQLAuth)
            runScript(url, pipename, scriptFile1, coreDB, uid, pwd, isSQLAuth, False)
            runScript(url, pipename, scriptFile1, coreDB, uid, pwd, isSQLAuth, False)
            If File.Exists(scriptFile1) Then
                Odbc = "Data Source=" & pipename & ";Initial Catalog=oph_core;" & IIf(uid <> "", "User Id=" & uid & ";password=" & pwd, "trusted connection=yes")
                Dim sqlstr = "if not exists(select * from acct where accountid='oph') insert into acct (accountid) values ('oph')"
                runSQLwithResult(sqlstr, Odbc)

                sqlstr = "declare @accountguid uniqueidentifier
                            select @accountguid=accountguid from acct where accountid='oph'
                            if not exists(select * from acctdbse where databasename='oph_core' and accountguid=@accountguid) insert into acctdbse (accountdbguid, accountguid, databasename, ismaster, version) values (newid(), @accountguid, 'oph_core', '1', '4.0')"
                runSQLwithResult(sqlstr, Odbc)
            End If
            Odbc = "Data Source=" & pipename & ";Initial Catalog=oph_core;" & IIf(uid <> "", "User Id=" & uid & ";password=" & pwd, "trusted connection=yes")
            Dim isexists1 = runSQLwithResult("select name from sys.databases where name='oph_core'", Odbc)
            Dim isexists2 = runSQLwithResult("select name from sys.objects where name='acct'", Odbc)
            Dim isexists3 = runSQLwithResult("select accountid from acct where accountid='oph'", Odbc)
            Dim isexists4 = runSQLwithResult("select databasename from acctdbse where databasename='oph_core'", Odbc)

            If isexists1 <> "" And isexists2 <> "" And isexists3 <> "" And isexists4 <> "" Then
                r = True
            End If
        End If
        Return r
    End Function

    Public Function addInstance(pipename, uid, pwd, coreDB, iisport, ophPath, curNode) As Boolean
        Dim r = False
        Dim odbc = "Data Source=" & pipename & ";Initial Catalog=master;" & IIf(uid <> "", "User Id=" & uid & ";password=" & pwd, "trusted connection=yes")
        Dim errStr As String = ""
        Dim ophcore = runSQLwithResult("select name from sys.databases where name='oph_core'", odbc, errStr)
        If errStr <> "" Then
        ElseIf ophcore <> "" Then
            odbc = "Data Source=" & pipename & ";Initial Catalog=" & coreDB & ";" & IIf(uid <> "", "User Id=" & uid & ";password=" & pwd, "trusted connection=yes")
            Dim listofAccount = runSQLwithResult("select ';accountid='+accountid+',dbname='+d.DatabaseName from acct a inner join acctdbse d on d.accountguid=a.accountguid and d.ismaster=1 and version='4.0' order by accountid for xml path('')", odbc)
            If listofAccount <> "" Then
                Dim curTag = curNode.Tag
                For Each t In curTag.split(";")
                    If t.split("=")(0) = "type" And t.split("=")(1) = "1" Then  'new
                        Dim x = curNode.Nodes.Add(pipename)
                        x.Tag = "type=2;mode=instance;uid=" & uid & ";pwd=" & pwd & ";port=" & iisport
                        curNode = x
                    Else
                        curNode.Text = pipename
                        curNode.Tag = "type=2;mode=instance;uid=" & uid & ";pwd=" & pwd & ";port=" & iisport
                    End If
                Next

                curNode.Nodes.Clear()

                For Each a In listofAccount.Split(";")
                    Dim accountid = "", dbname = ""
                    For Each ax In a.Split(",")
                        If ax.Split("=")(0) = "accountid" Then
                            accountid = ax.Split("=")(1)
                        End If
                        If ax.Split("=")(0) = "dbname" Then
                            dbname = ax.Split("=")(1)
                        End If
                    Next
                    If accountid <> "" Then
                        Dim x = curNode.Nodes.Add(accountid)
                        x.Tag = "type=3;dbname=" & dbname
                    End If
                    Dim n = addAccounttoIIS(accountid, pipename, My.Settings.ophFolder & "\", iisport, False)
                    runSQLwithResult("
	                        update i
	                        set infovalue=infovalue+';localhost:" & iisport & "/{accountid}'
	                        --select i.* 
	                        from acct a
		                        inner join acctinfo i on a.AccountGUID=i.AccountGUID
	                        where accountid='" & accountid & "' and i.InfoKey like '%address' and infovalue not like '%localhost:" & iisport & "/{accountid}%'

	                        insert into acctinfo (accountguid, infokey, infovalue)
	                        select a.accountguid, 'address', 'localhost:" & iisport & "/{accountid}' 
	                        from acct a
		                        left join acctinfo i on a.AccountGUID=i.AccountGUID and i.infokey='address'
	                        where accountid='" & accountid & "' and i.AccountInfoGUID is null

	                        insert into acctinfo (accountguid, infokey, infovalue)
	                        select a.accountguid, 'whiteaddress', 'localhost:" & iisport & "/{accountid}' 
	                        from acct a
		                        left join acctinfo i on a.AccountGUID=i.AccountGUID and i.infokey='whiteaddress'
	                        where accountid='" & accountid & "' and i.AccountInfoGUID is null
                            ", odbc)
                Next
                r = True
            End If

        Else
            If MessageBox.Show("oph account is Not exists. Do you want to create one?", "Confirmation", MessageBoxButtons.YesNo) = vbYes Then
                Dim tuser = "sam"
                Dim secret = "D627AFEB-9D77-40E4-B060-7C976DA05260"
                Dim isSQL As Boolean = My.Settings.isSQLAuth
                If createServer(pipename, isSQL, uid, pwd, tuser, secret, ophPath, My.Settings.ophServer) Then
                    Dim x = mainFrm.TreeView1.SelectedNode
                    If (IsNothing(x)) Then x = mainFrm.TreeView1.Nodes(0)
                    If getTag(x, "type") = "1" Then
                        x = x.Nodes.Add(pipename)
                    End If
                    x.Tag = "type=2;mode=instance;uid=" & uid & ";pwd=" & pwd & ";port=" & iisport
                    x.Nodes.Clear()
                    Dim y = x.Nodes.Add("oph")
                    y.Tag = "type=3;dbname=oph_core"

                    SetLog("Installing core database completed.")
                    MessageBox.Show("Installing server is completed")
                    r = True
                Else
                    SetLog("Installing core database NOT completed.")
                    MessageBox.Show("Installing server is NOT completed")
                End If


            End If
        End If
        Return r
    End Function
    Public Function addServer(treeview1 As TreeView, mode As String, pipename As String, uid As String, pwd As String, isNew As Boolean, iisport As String) As Boolean
        Dim r = False

        Dim coreDB = "oph_core"
        Dim folderData As String = "data"
        Dim isLocalDb = False
        Dim ophPath = My.Settings.ophFolder
        Dim remoteUrl = My.Settings.ophServer
        Dim folderTemp = "temp"
        Dim dataAccount = "oph"
        Dim token = ""
        If mode = "url" Then
            token = getToken(pipename, uid, pwd)
            If token <> "" Then
                Dim curnode = treeview1.SelectedNode
                If getTag(mainFrm.TreeView1.SelectedNode, "type") = "1" Then
                    Dim x = mainFrm.TreeView1.SelectedNode.Nodes.Add(pipename)
                    x.Tag = "type=2;mode=" & mode & ";uid=" & uid & ";pwd=" & pwd
                    curnode = x
                Else
                    mainFrm.TreeView1.SelectedNode.Text = pipename
                    mainFrm.TreeView1.SelectedNode.Tag = "type=2;mode=" & mode & ";uid=" & uid & ";pwd=" & pwd
                End If

                Dim urlstr = pipename & "/ophcore/api/sync.aspx?mode=dbinfo&token=" & token
                Dim dbinfo = postHttp(urlstr)

                Dim sep() As String = {"<?xml version=""1.0"" encoding=""utf-8""?>", "<sqroot>", "<databases>", "<database>", "</database>", "</databases>", "</sqroot>"}
                Dim r1 = dbinfo.Split(sep, StringSplitOptions.RemoveEmptyEntries)
                Dim accountGUID As String = ""
                Dim accountid = ""
                Dim info1() As String = {"<info>", "</info>", "<data ", "/>"}
                For Each r1x In r1
                    Dim r2 = r1x.Split(info1, StringSplitOptions.RemoveEmptyEntries)
                    If r2.Length > 1 Then
                        For Each r2x In r2
                            Dim r3 = r2x.Split({"key=""", "value=""", """ "}, StringSplitOptions.RemoveEmptyEntries)
                            If r3.Length > 1 Then   'not empty
                                If r3(0) = "accountid" Then
                                    accountid = r3(1)
                                End If
                            End If
                        Next

                    End If

                Next

                Dim sep1() As String = {"<AccountGUID>", "<AccountDBGUID>", "<databasename>", "<isMaster>", "<version>", "</AccountGUID>", "</AccountDBGUID>", "</databasename>", "</isMaster>", "</version>"}
                For Each r1x In r1
                    Dim r2 = r1x.Split(sep1, StringSplitOptions.RemoveEmptyEntries)
                    If r2.Length = 5 Then
                        accountGUID = r2(0)
                        Dim accountDBGUID = r2(1)
                        Dim dbname = r2(2)
                        Dim ismaster = r2(3)
                        Dim Version = r2(4)
                        If dbname <> "" And ismaster = "1" Then
                            Dim x = curnode.Nodes.Add(accountid)
                            x.Tag = "type=3;dbname=" & dbname
                        End If

                    End If
                Next


                r = True
            End If
        Else
            Dim curnode = mainFrm.TreeView1.SelectedNode
            r = addInstance(pipename, uid, pwd, coreDB, iisport, ophPath, curnode)

        End If

        Return r
    End Function

End Class
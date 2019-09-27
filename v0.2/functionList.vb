Imports System.Data.SqlClient
Imports System.IO
Imports System.IO.Compression
Imports System.Net
Imports System.Text

Public NotInheritable Class FunctionList

    Private Shared oInstance As FunctionList = New FunctionList

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
        End Try
        Return result
    End Function

    Public Function syncLocalScript(sqlstr, db, pipename, uid, pwd) As Boolean
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

    Public Function runScript(url, pipename, scriptFile, db, uid, pwd) As Boolean
        Dim r = True
        If File.Exists(scriptFile) Then File.Delete(scriptFile)
        If downloadFilename(url, scriptFile) Then
            Dim p As Process = New Process()
            p.StartInfo.UseShellExecute = False
            'p.StartInfo.RedirectStandardOutput = True
            p.StartInfo.RedirectStandardError = True
            p.StartInfo.FileName = "sqlcmd.exe"
            p.StartInfo.Arguments = "-S " & pipename & " -d " & db & " -i """ & scriptFile & """" & IIf(uid <> "", " -U " & uid & " -P " & pwd, "")
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

    Public Function addAccounttoIIS(account As String, path As String, port As String, folderData As String, folderTemp As String, Optional isRemoved As Boolean = False) As Integer
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
    Public Sub runCmd(filename As String, Optional workingPath As String = "")
        Dim p As Process = New Process()
        p.StartInfo.UseShellExecute = False
        p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.RedirectStandardError = True

        p.StartInfo.FileName = filename
        If workingPath <> "" Then p.StartInfo.WorkingDirectory = workingPath
        p.StartInfo.Arguments = " "
        p.StartInfo.CreateNoWindow = True
        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
        p.Start()
        Dim sOutput As String = p.StandardOutput.ReadToEnd()
        Dim sErr As String = p.StandardError.ReadToEnd()
        p.WaitForExit()
        SetLog(sOutput, , )
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
    Public Function createServer(pipename, uid, pwd, tuser, secret, ophPath, ophserver) As Boolean
        Dim r = False
        Dim Odbc = "Data Source=" & pipename & ";Initial Catalog=master;User Id=" & uid & ";password=" & pwd & ""
        Dim coreDB = "oph_core"
        Dim folderTemp = "temp"
        Dim result = runSQLwithResult("CREATE DATABASE " & coreDB, Odbc)
        If result = "" Then

            Dim c_uri = ophserver & "/oph"
            Dim token = getToken(c_uri, tuser, secret)
            Dim url = c_uri & "/ophcore/api/sync.aspx?mode=reqcorescript&token=" & token
            Dim scriptFile1 = ophPath & "\" & folderTemp & "\install_core.sql"
            SetLog(scriptFile1, , True)

            runScript(url, pipename, scriptFile1, coreDB, uid, pwd)
            runScript(url, pipename, scriptFile1, coreDB, uid, pwd)
            runScript(url, pipename, scriptFile1, coreDB, uid, pwd)
            If File.Exists(scriptFile1) Then
                Odbc = "Data Source=" & pipename & ";Initial Catalog=oph_core;User Id=" & uid & ";password=" & pwd
                Dim sqlstr = "if not exists(select * from acct where accountid='oph') insert into acct (accountid) values ('oph')"
                runSQLwithResult(sqlstr, Odbc)

                sqlstr = "declare @accountguid uniqueidentifier
                            select @accountguid=accountguid from acct where accountid='oph'
                            if not exists(select * from acctdbse where databasename='oph_core' and accountguid=@accountguid) insert into acctdbse (accountdbguid, accountguid, databasename, ismaster, version) values (newid(), @accountguid, 'oph_core', '1', '4.0')"
                runSQLwithResult(sqlstr, Odbc)
            End If
            Odbc = "Data Source=" & pipename & ";Initial Catalog=oph_core;User Id=" & uid & ";password=" & pwd
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


End Class
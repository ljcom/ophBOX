Imports System
Imports System.Data
Imports System.Data.Sql
Imports System.Net
Imports System.IO
Imports System.Diagnostics
Imports System.ComponentModel

Public Class Form1
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles Me.Load
        'getInstalledSQL()
        'getLocalDB()
        If checkInstance("OPERAHOUSE") = "OPERAHOUSE" Then
            installLocalDB()
            setLog("localDB installed")
            createInstance("OPERAHOUSE")
            setLog("OPERAHOUSE created")
        End If
        startInstance("OPERAHOUSE")
        setLog("OPERAHOUSE started")
        Dim pipename = getPipeName("OPERAHOUSE")
        setLog("Pipename: " & pipename)

        Dim iisexpress = getIISLocation()
        setLog("IIS Express Location: " & iisexpress)

        Dim url = "http://redbean/oph/ophcore/api/sync.aspx?mode=reqcorescript"
        Dim scriptFile = Directory.GetCurrentDirectory & "\temp\install.sql"
        runScript(url, pipename, scriptFile)

    End Sub
    Private Sub Form1_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        stopInstance("OPERAHOUSE")
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
                    installIIS()
                    setLog("IIS Express Installed")

                End If
            End If
        End If

        Return r
    End Function
    Sub installIIS()

        Dim url = "http://download.operahouse.systems/iisexpress_x86_en-US.msi" 'x86
        If Environment.Is64BitOperatingSystem Then
            url = "http://download.operahouse.systems/iisexpress_amd64_en-US.msi" '64 bit
        End If

        Dim filename = Directory.GetCurrentDirectory() & "\temp\iisexpress.msi"
        If Not Directory.Exists(Directory.GetCurrentDirectory() & "\temp") Then
            Directory.CreateDirectory(Directory.GetCurrentDirectory() & "\temp")
        End If
        If Not Directory.Exists(Directory.GetCurrentDirectory() & "\data") Then
            Directory.CreateDirectory(Directory.GetCurrentDirectory() & "\data")
        End If
        If Not File.Exists(filename) Then
            downloadfilename(url, filename)
        End If
        Dim runfilename = """" & filename & """"
        Dim info As New ProcessStartInfo()
        info.FileName = "c:\windows\system32\msiexec.exe"
        info.Arguments = " /i """ & Directory.GetCurrentDirectory() & "\temp\iisexpress.msi"" /qn"
        Process.Start(info)


    End Sub
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

    Private Sub TextBox1_TextChanged(sender As Object, e As EventArgs)

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs)
        installLocalDB()
    End Sub
    Sub installLocalDB()
        'Dim url = "https://go.microsoft.com/fwlink/?linkid=853017"  '2017
        'Dim url = "https://go.microsoft.com/fwlink/?LinkID=799012" '2016
        'Dim url = "http://download.microsoft.com/download/E/A/E/EAE6F7FC-767A-4038-A954-49B8B05D04EB/ExpressAndTools%2032BIT/SQLEXPRWT_x86_ENU.exe" '2014
        'Dim url = "http://download.microsoft.com/download/8/D/D/8DD7BDBA-CEF7-4D8E-8C16-D9F69527F909/ENU/x86/SQLEXPRWT_x86_ENU.exe" '2012
        Dim url = "http://download.microsoft.com/download/8/D/D/8DD7BDBA-CEF7-4D8E-8C16-D9F69527F909/ENU/x86/SqlLocaLDB.MSI" 'localdb 2012

        Dim filename = Directory.GetCurrentDirectory() & "\temp\sqllocaldb.msi"
        If Not Directory.Exists(Directory.GetCurrentDirectory() & "\temp") Then
            Directory.CreateDirectory(Directory.GetCurrentDirectory() & "\temp")
        End If
        If Not Directory.Exists(Directory.GetCurrentDirectory() & "\data") Then
            Directory.CreateDirectory(Directory.GetCurrentDirectory() & "\data")
        End If
        If Not File.Exists(filename) Then
            downloadfilename(url, filename)
        End If
        Dim runfilename = """" & filename & """"
        Dim info As New ProcessStartInfo()
        info.FileName = "c:\windows\system32\msiexec.exe"
        'info.Arguments = "/Q /ACTION=install /HIDECONSOLE=1 /UpdateEnabled=0 /FEATURES=SQL " &
        '    "/IACCEPTSQLSERVERLICENSETERMS /INSTANCENAME=OPERAHOUSE " &
        '    "/INSTANCEDIR=""" & Directory.GetCurrentDirectory() & "\data"" " &
        '    "/SECURITYMODE=sql /INSTALLSQLDATADIR=""" & Directory.GetCurrentDirectory() & "\data"" " &
        '    "/SAPWD=""wishforthebest"" /SQLSVCACCOUNT=""client"" /SQLSVCPASSWORD=""wishforthebest"" /SQLSVCSTARTUPTYPE=""Automatic"" " &
        '    "/AGTSVCACCOUNT=""client"" /AGTSVCPASSWORD=""wishforthebest"" /AGTSVCSTARTUPTYPE=1 //INDICATEPROGRESS=1"
        info.Arguments = " /i """ & Directory.GetCurrentDirectory() & "\temp\SqlLocalDB.msi"" IACCEPTSQLLOCALDBLICENSETERMS=YES /qn"
        Process.Start(info)

    End Sub
    Sub installGIT()
        Dim url = "https://github.com/git-for-windows/git/releases/download/v2.15.1.windows.2/Git-2.15.1.2-32-bit.exe"
        If Environment.Is64BitOperatingSystem Then
            url = "https://github.com/git-for-windows/git/releases/download/v2.15.1.windows.2/Git-2.15.1.2-64-bit.exe"
        End If

        Dim filename = Directory.GetCurrentDirectory() & "\temp\git.exe"
        If Not Directory.Exists(Directory.GetCurrentDirectory() & "\temp") Then
            Directory.CreateDirectory(Directory.GetCurrentDirectory() & "\temp")
        End If
        If Not Directory.Exists(Directory.GetCurrentDirectory() & "\data") Then
            Directory.CreateDirectory(Directory.GetCurrentDirectory() & "\data")
        End If
        If Not File.Exists(filename) Then
            downloadfilename(url, filename)
        End If
        Dim runfilename = """" & filename & """"
        Dim info As New ProcessStartInfo()
        info.FileName = Directory.GetCurrentDirectory() & "\temp\git.exe"
        info.Arguments = " "
        Process.Start(info)

    End Sub
    Sub downloadfilename(url, localpath)
        Dim wc As New WebClient()
        wc.DownloadFile(url, localpath)
    End Sub

    Private Sub Label2_Click(sender As Object, e As EventArgs) Handles Label2.Click

    End Sub

    Function getLocalDB()
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
        If Not (sOutput Is Nothing Or sOutput.Trim().Length = 0 Or sOutput.Contains("not recognized")) Then
            For Each info In sOutput.Split(vbCrLf)
                If info.Split(":")(0) = "Name" Then
                    pipeName = info.Split(":")(1)
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
    Sub runScript(url, pipename, scriptFile)
        If File.Exists(scriptFile) Then File.Delete(scriptFile)
        downloadfilename(url, scriptFile)

        Dim p As Process = New Process()
        p.StartInfo.UseShellExecute = False
        p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.FileName = "sqlcmd.exe"
        p.StartInfo.Arguments = "-S " & pipename & " -i """ & scriptFile & """"
        p.StartInfo.CreateNoWindow = True
        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
        p.Start()

        Dim sOutput As String = p.StandardOutput.ReadToEnd()
        p.WaitForExit()
        setLog(sOutput)
    End Sub
    Sub setLog(txt)
        'Exit Sub
        If Me.InvokeRequired Then
            Me.Invoke(New Action(Sub() Me.TextBox1.Text = IIf(txt = "", "", txt & vbCrLf) & Me.TextBox1.Text))
        Else
            Me.TextBox1.Text = IIf(txt = "", "", txt & vbCrLf) & Me.TextBox1.Text
        End If

    End Sub
End Class
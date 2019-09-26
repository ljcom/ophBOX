Imports System.IO
Imports System.IO.Compression
Imports System.Net

Public Class settingFrm
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim ophPath = Me.TextBox1.Text
        Dim p_uri = Me.TextBox2.Text
        Dim errStr = ""

        If p_uri = "" Or ophPath = "" Then
            MessageBox.Show("Please fill all blanks before continue.", "Warning")
        ElseIf Not Directory.Exists(Me.TextBox1.Text & "\OPERAHOUSE") Then
            My.Settings.ophFolder = TextBox1.Text
            My.Settings.ophServer = TextBox2.Text
            My.Settings.Save()

            If MessageBox.Show("Folder not exists, we will setup the OPERAHOUSE folder. Continue?", "Start", MessageBoxButtons.OKCancel) = vbOK Then
                If Not Directory.Exists(Me.TextBox1.Text) Then Directory.CreateDirectory(Me.TextBox1.Text)
                If Not Directory.Exists(Me.TextBox1.Text & "\temp") Then Directory.CreateDirectory(Me.TextBox1.Text & "\temp")
                If Not Directory.Exists(Me.TextBox1.Text & "\data") Then Directory.CreateDirectory(Me.TextBox1.Text & "\data")
                Dim gitLoc = getGITLocation(Me.TextBox1.Text)

                Dim localFile1 = ophPath & "\temp\sync.zip"

                If Not File.Exists(localFile1) Then
                    Dim url = p_uri & "/oph/ophcore/api/sync.aspx?mode=webrequestFile"
                    If downloadFilename(url, localFile1) Then
                        unZip(localFile1, ophPath & "\temp")
                    Else
                        errStr = "Download is Failed."
                    End If
                End If
                If errStr = "" Then
                    runCmd(ophPath & "\temp" & "\build-oph.bat", ophPath)
                    If Not Directory.Exists(ophPath & "\operahouse") Then
                        errStr = "Folder Building is failed"
                    End If
                End If
            End If
        End If
        If Directory.Exists(Me.TextBox1.Text & "\OPERAHOUSE") Then
            Me.Close()
        Else
            MessageBox.Show(errStr, "Error", MessageBoxButtons.OK)
        End If
    End Sub

    Private Sub settingFrm_Load(sender As Object, e As EventArgs) Handles Me.Load
        Me.TextBox1.Text = My.Settings.ophFolder
        Me.TextBox2.Text = My.Settings.ophServer
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
                installGIT(ophPath & "\temp", ophPath & "\data")
                MessageBox.Show("When the GIT has been installed, please press OK to continue.")
                SetLog("GIT Installed")
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
    Function downloadFilename(url, localpath) As Boolean
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
    Sub unZip(zipPath, extractPath)
        ZipFile.ExtractToDirectory(zipPath, extractPath)
    End Sub
    Sub runCmd(filename As String, Optional workingPath As String = "")
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
    Sub SetLog(txt As String, Optional title As String = "", Optional isShow As Boolean = True)
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
    Sub WriteLog(logMessage As String)
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

End Class
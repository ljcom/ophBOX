Imports System.ComponentModel
Imports System.IO
Imports Newtonsoft.Json
Imports ophBox.FunctionList

Public Class mainFrm
    Dim f As FunctionList = FunctionList.Instance
    Private eventHandled As Boolean = False
    Private elapsedTime As Integer
    Private iisId As Long
    Dim activePort As Dictionary(Of String, Long) = New Dictionary(Of String, Long)

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Me.Close()
    End Sub

    Private Sub TreeView1_AfterSelect(sender As Object, e As TreeViewEventArgs) Handles TreeView1.AfterSelect
        changeContext(e.Node)

        Me.ListView1.Items.Clear()
        Dim i = f.getTag(Me.TreeView1.SelectedNode, "type")
        If i = 3 Then
            'Dim accountid = Me.TreeView1.SelectedNode.Text
            'Me.WebBrowser1.Visible = True
            'Me.ListView1.Visible = False
            'Me.WebBrowser1.ScriptErrorsSuppressed = False
            'Me.WebBrowser1.Navigate("http://localhost:8080/" & accountid)
        Else
            Me.WebBrowser1.Visible = False
            Me.ListView1.Visible = True
            For Each n In Me.TreeView1.SelectedNode.Nodes
                Dim x = Me.ListView1.Items.Add(n.text)
                x.ImageIndex = Val(i) - 1
            Next
        End If
    End Sub

    Private Sub TreeView1_NodeMouseClick(sender As Object, e As TreeNodeMouseClickEventArgs) Handles TreeView1.NodeMouseClick
        TreeView1.SelectedNode = e.Node
        changeContext(e.Node)
    End Sub
    Sub changeContext(node As TreeNode)
        Dim server = node.Text

        If activePort.ContainsKey(server) Then
            Me.StartIISExpessToolStripMenuItem.Text = "&Stop IIS Express"
        Else
            Me.StartIISExpessToolStripMenuItem.Text = "&Start IIS Express"
        End If
        If Not IsNothing(node.Tag) Then
            Dim n = node.Tag.split(";")
            For Each n1 In n
                If n1.split("=")(0) = "type" Then
                    Dim n2 = n1.split("=")(1)
                    If n2 = "1" Then
                        TreeView1.ContextMenuStrip = ContextMenuStrip1
                    ElseIf n2 = "2" Then
                        TreeView1.ContextMenuStrip = ContextMenuStrip2
                    ElseIf n2 = "3" Then
                        TreeView1.ContextMenuStrip = ContextMenuStrip3
                    End If

                End If
            Next
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        settingFrm.ShowDialog()
    End Sub

    Private Sub AddServerToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AddServerToolStripMenuItem.Click
        addServerFrm.ShowDialog()
        saveTree()
    End Sub

    Private Sub AddDatabaseToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AddDatabaseToolStripMenuItem.Click
        addDatabaseFrm.ShowDialog()
        saveTree()
    End Sub

    Private Sub PropertiesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles PropertiesToolStripMenuItem.Click
        addServerFrm.ShowDialog()
        saveTree()
    End Sub

    Private Sub mainFrm_Load(sender As Object, e As EventArgs) Handles Me.Load
        If My.Settings.ophFolder = "" Or Not Directory.Exists(My.Settings.ophFolder & "\OPERAHOUSE") Then
            settingFrm.ShowDialog()
            If Not Directory.Exists(My.Settings.ophFolder & "\OPERAHOUSE") Then
                End
            End If
        Else
            Dim treeData = My.Settings.treeData
            If treeData = "" Then treeData = "{servers:[]}"

            Dim jsonResulttodict = JsonConvert.DeserializeObject(Of Dictionary(Of String, Object))(treeData)
            Dim servers = jsonResulttodict.Item("servers")
            Dim tag = jsonResulttodict.Item("tag")
            Dim t = New TreeNode("Servers")
            t.Tag = tag

            For Each serverData In servers
                Dim d = t.Nodes.Add(serverData("server").value)
                d.tag = serverData("tag").value
                For Each dbData In serverData("dbs")
                    Dim c = d.Nodes.Add(dbData("db").value)
                    c.tag = dbData("tag").value
                Next
            Next
            Me.TreeView1.Nodes.Clear()
            Me.TreeView1.Nodes.Add(t)
        End If
    End Sub

    Private Sub ContextMenuStrip2_Opening(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles ContextMenuStrip2.Opening

    End Sub

    Private Sub DatabasePropertiesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles DatabasePropertiesToolStripMenuItem.Click
        addDatabaseFrm.ShowDialog()
        saveTree()
    End Sub


    Private Sub TreeView1_DoubleClick(sender As Object, e As EventArgs) Handles TreeView1.DoubleClick
        Dim nt = TreeView1.SelectedNode.Tag

        If Not IsNothing(nt) Then
            Dim n = nt.split(";")
            For Each n1 In n
                If n1.split("=")(0) = "type" Then
                    Dim n2 = n1.split("=")(1)
                    If n2 = "1" Then
                    ElseIf n2 = "2" Then
                        addServerFrm.ShowDialog()
                        saveTree()
                    ElseIf n2 = "3" Then
                        addDatabaseFrm.ShowDialog()
                        saveTree()
                    End If

                End If
            Next
        End If

    End Sub
    Sub saveTree()
        Dim treedata = ""
        For Each svr In Me.TreeView1.Nodes
            Dim servers = ""
            For Each s In svr.nodes
                Dim dbs = ""
                For Each d In s.nodes
                    dbs = dbs & ", {""db"":""" & d.text & """, ""tag"":""" & d.tag & """}"
                Next
                If dbs.Substring(0, 1) = "," Then dbs = dbs.Substring(2, dbs.Length - 2)
                servers = servers & ", {""server"":""" & s.text & """, ""dbs"":[" & dbs & "], ""tag"":""" & s.tag & """}"
            Next
            If servers <> "" Then
                If servers.Substring(0, 1) = "," Then servers = servers.Substring(2, servers.Length - 2)
                treedata = "{""servers"":[" & servers & "], ""tag"":""type=1""}"
            End If
        Next
        If treedata = "" Then treedata = "{servers:[], tag:""type=1""}"
        My.Settings.treeData = treedata
        My.Settings.Save()

    End Sub

    Private Sub ToolStripMenuItem4_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem4.Click
        removeList()
    End Sub
    Sub removeList()
        Me.TreeView1.SelectedNode.Remove()
        saveTree()
    End Sub

    Private Sub RemoveDatabaseToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles RemoveDatabaseToolStripMenuItem.Click
        Dim pipename = Me.TreeView1.SelectedNode.Parent.Text
        Dim accountid = Me.TreeView1.SelectedNode.Text
        If accountid <> "oph" Then
            Dim uid = f.getTag(Me.TreeView1.SelectedNode.Parent, "uid")
            Dim pwd = f.getTag(Me.TreeView1.SelectedNode.Parent, "pwd")

            If delDb(accountid, pipename, uid, pwd, True) Then
                Me.TreeView1.SelectedNode.Remove()
                saveTree()
                Dim n = f.addAccounttoIIS(accountid, pipename, My.Settings.ophFolder & "\", My.Settings.IISPort, True)
            End If
        Else
            MessageBox.Show("You cannot delete OPH Database. You may delete the server instead.", "Warning")
        End If
    End Sub
    Function delDb(accountid As String, pipename As String, uid As String, pwd As String, Optional delFile As Boolean = False) As Boolean
        Dim rs = False
        'If accountid <> "oph" Then
        If MessageBox.Show("You are about to " & IIf(delFile, "delete", "remove") & " " & accountid & ". Continue?", "Confirmation", MessageBoxButtons.YesNo) = vbYes Then
            Dim odbc = "Data Source=" & pipename & ";Initial Catalog=oph_core;User Id=" & uid & ";password=" & pwd
            Dim sqlStr = ""
            Dim r = ""
            If delFile Then
                sqlStr = "select 'alter database '+databasename+' set single_user with rollback immediate; drop database '+databasename+';' from acct a inner join acctdbse d on d.accountguid=a.accountguid where a.accountid='" & accountid & "' for xml path('')"
                Dim delStr = f.runSQLwithResult(sqlStr, odbc)
                r = f.runSQLwithResult(delStr, odbc)
            End If
            sqlStr = "delete from d from acct a inner join acctdbse d on d.accountguid=a.accountguid where a.accountid='" & accountid & "'"
            r = f.runSQLwithResult(sqlStr, odbc)

            sqlStr = "delete from acct where accountid='" & accountid & "'"
            r = f.runSQLwithResult(sqlStr, odbc)
            rs = True
        End If
        Return rs
    End Function
    Private Sub TreeView1_KeyDown(sender As Object, e As KeyEventArgs) Handles TreeView1.KeyDown
        If e.KeyData = 46 Then
            If f.getTag(Me.TreeView1.SelectedNode, "type") = "3" Then
                Dim pipename = Me.TreeView1.SelectedNode.Parent.Text
                Dim accountid = Me.TreeView1.SelectedNode.Text
                If accountid <> "oph" Then
                    Dim uid = f.getTag(Me.TreeView1.SelectedNode.Parent, "uid")
                    Dim pwd = f.getTag(Me.TreeView1.SelectedNode.Parent, "pwd")

                    If delDb(accountid, pipename, uid, pwd, True) Then
                        Me.TreeView1.SelectedNode.Remove()
                        saveTree()
                        Dim n = f.addAccounttoIIS(accountid, pipename, My.Settings.ophFolder & "\", My.Settings.IISPort, True)
                    End If
                Else
                    MessageBox.Show("You cannot delete OPH Database. You may delete the server instead.", "Warning")
                End If
            End If
        End If
    End Sub

    Private Sub DeleteServerToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles DeleteServerToolStripMenuItem.Click
        If Me.TreeView1.SelectedNode.Nodes.Count <= 1 Then
            Dim pipename = Me.TreeView1.SelectedNode.Text
            Dim uid = f.getTag(Me.TreeView1.SelectedNode, "uid")
            Dim pwd = f.getTag(Me.TreeView1.SelectedNode, "pwd")

            If delDb("oph", pipename, uid, pwd, True) Then
                saveTree()
                Dim n = f.addAccounttoIIS("oph", pipename, My.Settings.ophFolder & "\", My.Settings.IISPort, True)
            End If
        Else
            MessageBox.Show("Please remove all accounts before continue.")
        End If
    End Sub

    'Private Sub Button3_Click(sender As Object, e As EventArgs)
    '    If Not eventHandled Then
    '        iisId = runIIS("(local)")
    '        Me.Button3.Text = "IIS Stop"
    '        Dim webAddress As String = "http://localhost:" & My.Settings.IISPort
    '        Process.Start(webAddress)
    '    Else
    '        If iisId > 0 Then
    '            Try

    '                For Each p In Process.GetProcessesByName("iisexpress")
    '                    p.Kill()
    '                Next
    '                'Dim p = Process.GetProcessById(iisId)
    '            Catch ex As Exception
    '                MessageBox.Show(ex.Message)
    '            End Try
    '            'iisId = System.Diagnostics.GetWin32Process("iisexpress", 0) '= iisId
    '        End If
    '        eventHandled = False
    '        iisId = 0
    '        Me.Button3.Text = "IIS Start"
    '    End If
    'End Sub
    Function runIIS(server As String) As Long

        Dim ophPath = My.Settings.ophFolder
        eventHandled = False
        elapsedTime = 0
        Dim folderTemp = "temp"
        Dim folderData = "data"

        server = server.Replace("(", "").Replace(")", "").Replace("\", "")

        Dim p As Process = New Process()
        p.StartInfo.UseShellExecute = False
        p.StartInfo.RedirectStandardOutput = True
        p.StartInfo.RedirectStandardError = True
        p.EnableRaisingEvents = True

        AddHandler p.ErrorDataReceived, AddressOf OutputDataReceivedIIS
        AddHandler p.OutputDataReceived, AddressOf OutputDataReceivedIIS

        p.StartInfo.FileName = My.Settings.IISExpressLocation
        p.StartInfo.Arguments = "/config:" & ophPath & "\" & folderTemp & "\applicationhost.config /systray:false /site:oph" & server
        p.StartInfo.CreateNoWindow = True
        p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden


        p.Start()
        Dim iId = p.Id

        p.BeginErrorReadLine()
        p.BeginOutputReadLine()

        Return iId
    End Function
    Public Sub OutputDataReceivedIIS(ByVal sender As Object, ByVal e As DataReceivedEventArgs)
        Try
            Dim t = IIf(e.Data = "", "", Now() & " IIS " & e.Data & vbCrLf)
            'lastMessage = lastMessage & t

            If Me.InvokeRequired = True Then
                'Me.Invoke(myDelegate, e.Data)
                'Me.Invoke(New Action(Sub() Me.tbLog.AppendText(t)))
            Else
                'UpdateTextBox(e.Data)
                'Me.tbLog.AppendText(t)
            End If
            eventHandled = True

            f.WriteLog(t)
        Catch ex As Exception

        End Try

    End Sub

    Private Sub RemoveFromListToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles RemoveFromListToolStripMenuItem.Click
        Dim pipename = Me.TreeView1.SelectedNode.Parent.Text
        Dim accountid = Me.TreeView1.SelectedNode.Text
        If accountid <> "oph" Then
            Dim uid = f.getTag(Me.TreeView1.SelectedNode.Parent, "uid")
            Dim pwd = f.getTag(Me.TreeView1.SelectedNode.Parent, "pwd")

            If delDb(accountid, pipename, uid, pwd, False) Then
                Me.TreeView1.SelectedNode.Remove()
                saveTree()
                Dim n = f.addAccounttoIIS(accountid, pipename, My.Settings.ophFolder & "\", My.Settings.IISPort, True)
            End If
        Else
            MessageBox.Show("You cannot delete OPH Database. You may delete the server instead.", "Warning")
        End If

    End Sub

    Private Sub StartIISExpessToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles StartIISExpessToolStripMenuItem.Click
        Dim server = Me.TreeView1.SelectedNode.Text
        If activePort.ContainsKey(server) Then
            Dim id = activePort(server)
            Try
                Dim p = Process.GetProcessById(id)
                p.Kill()
            Catch ex As Exception

            End Try
            activePort.Remove(server)
        Else
            Dim id = runIIS(server)
            Try
                If Process.GetProcessById(id).Id = id Then
                    Dim port = f.getTag(Me.TreeView1.SelectedNode, "port")
                    Dim webAddress As String = "http://localhost:" & port
                    Process.Start(webAddress)
                    activePort.Add(server, id)
                End If
            Catch ex As Exception
                MessageBox.Show("Service is failed to run.")

            End Try
        End If
    End Sub

    Private Sub mainFrm_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        If activePort.Count > 0 Then
            For Each k In activePort.Keys
                Dim id = activePort(k)
                Try
                    Dim p = Process.GetProcessById(id)
                    p.Kill()
                Catch ex As Exception
                End Try
            Next
        End If
        saveTree()
    End Sub

    Private Sub LoadScriptToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LoadScriptToolStripMenuItem.Click
        Dim sqlstr = "exec.gen.loadmodl '*', 1"
        Dim pipename = Me.TreeView1.SelectedNode.Parent.Text
        Dim uid = f.getTag(Me.TreeView1.SelectedNode.Parent, "uid")
        Dim pwd = f.getTag(Me.TreeView1.SelectedNode.Parent, "pwd")
        Dim accountid = Me.TreeView1.SelectedNode.Text
        Dim datadb = "oph_core"
        If accountid <> "oph" Then datadb = accountid & "_data"

        Dim odbc = "Data Source=" & pipename & ";Initial Catalog=" & datadb & ";User Id=" & uid & ";password=" & pwd
        Dim script = f.runSQLwithResult(sqlstr, odbc)
        SaveFileDialog1.Title = "Backup Script"
        SaveFileDialog1.Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*"
        SaveFileDialog1.FilterIndex = 2
        SaveFileDialog1.RestoreDirectory = True
        SaveFileDialog1.InitialDirectory = My.Settings.ophFolder

        If SaveFileDialog1.ShowDialog() = DialogResult.OK Then
            Dim filename = SaveFileDialog1.FileName
            If filename.IndexOf(".xml") < 0 Then filename = filename & ".xml"
            File.WriteAllText(filename, script)
        End If
    End Sub

    Private Sub SaveScriptToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SaveScriptToolStripMenuItem.Click
        Dim pipename = Me.TreeView1.SelectedNode.Parent.Text
        Dim uid = f.getTag(Me.TreeView1.SelectedNode.Parent, "uid")
        Dim pwd = f.getTag(Me.TreeView1.SelectedNode.Parent, "pwd")
        Dim accountid = Me.TreeView1.SelectedNode.Text
        Dim datadb = "oph_core"
        If accountid <> "oph" Then datadb = accountid & "_data"

        Dim odbc = "Data Source=" & pipename & ";Initial Catalog=" & datadb & ";User Id=" & uid & ";password=" & pwd
        OpenFileDialog1.Title = "Restore Script to database"
        OpenFileDialog1.Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*"
        OpenFileDialog1.InitialDirectory = My.Settings.ophFolder
        If OpenFileDialog1.ShowDialog() = DialogResult.OK Then
            Dim sqlstr = "exec gen.savemodl @file='" & OpenFileDialog1.FileName & "', @updateMode=11"
            f.runSQLwithResult(sqlstr, odbc)
        End If
    End Sub
End Class

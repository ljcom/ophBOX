Imports System.IO
Imports Newtonsoft.Json
Imports ophBox.FunctionList

Public Class mainFrm
    Dim f As FunctionList = FunctionList.Instance
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        End
    End Sub

    Private Sub TreeView1_AfterSelect(sender As Object, e As TreeViewEventArgs) Handles TreeView1.AfterSelect
        changeContext(e.Node)

        Me.ListView1.Items.Clear()
        Dim i = f.getTag(Me.TreeView1.SelectedNode, "type")
        For Each n In Me.TreeView1.SelectedNode.Nodes
            Dim x = Me.ListView1.Items.Add(n.text)
            x.ImageIndex = Val(i) - 1
        Next
    End Sub

    Private Sub TreeView1_NodeMouseClick(sender As Object, e As TreeNodeMouseClickEventArgs) Handles TreeView1.NodeMouseClick
        TreeView1.SelectedNode = e.Node
        changeContext(e.Node)
    End Sub
    Sub changeContext(node As TreeNode)
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
            If servers.Substring(0, 1) = "," Then servers = servers.Substring(2, servers.Length - 2)
            treedata = "{""servers"":[" & servers & "], ""tag"":""type=1""}"
        Next
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
            Dim uid = f.getTag(Me.TreeView1.SelectedNode, "uid")
            Dim pwd = f.getTag(Me.TreeView1.SelectedNode, "pwd")

            delDb(accountid, pipename, uid, pwd)
            saveTree()
        End If
    End Sub
    Sub delDb(accountid As String, pipename As String, uid As String, pwd As String)
        'If accountid <> "oph" Then
        If MessageBox.Show("You are about to delete " & accountid & ". Continue?", "Confirmation", MessageBoxButtons.YesNo) = vbYes Then
            Dim odbc = "Data Source=" & pipename & ";Initial Catalog=oph_core;User Id=" & uid & ";password=" & pwd
            Dim sqlStr = "select 'alter database '+databasename+' set single_user with rollback immediate; drop database '+databasename+';' from acct a inner join acctdbse d on d.accountguid=a.accountguid where a.accountid='" & accountid & "' for xml path('')"
            Dim delStr = f.runSQLwithResult(sqlStr, odbc)
            Dim r = f.runSQLwithResult(delStr, odbc)

            sqlStr = "delete from d from acct a inner join acctdbse d on d.accountguid=a.accountguid where a.accountid='" & accountid & "'"
            r = f.runSQLwithResult(sqlStr, odbc)

            sqlStr = "delete from acct where accountid='" & accountid & "'"
            r = f.runSQLwithResult(sqlStr, odbc)
            Me.TreeView1.SelectedNode.Remove()
        End If
    End Sub
    Private Sub TreeView1_KeyDown(sender As Object, e As KeyEventArgs) Handles TreeView1.KeyDown
        If e.KeyData = 46 Then
            If f.getTag(Me.TreeView1.SelectedNode, "type") = "3" Then
                Dim pipename = Me.TreeView1.SelectedNode.Parent.Text
                Dim accountid = Me.TreeView1.SelectedNode.Text
                If accountid <> "oph" Then
                    Dim uid = f.getTag(Me.TreeView1.SelectedNode.Parent, "uid")
                    Dim pwd = f.getTag(Me.TreeView1.SelectedNode.Parent, "pwd")

                    delDb(accountid, pipename, uid, pwd)
                    saveTree()
                End If
            End If
        End If
    End Sub

    Private Sub DeleteServerToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles DeleteServerToolStripMenuItem.Click
        If Me.TreeView1.SelectedNode.Nodes.Count <= 1 Then
            Dim pipename = Me.TreeView1.SelectedNode.Text
            Dim uid = f.getTag(Me.TreeView1.SelectedNode, "uid")
            Dim pwd = f.getTag(Me.TreeView1.SelectedNode, "pwd")

            delDb("oph", pipename, uid, pwd)
            saveTree()
        Else
            MessageBox.Show("Please remove all accounts before continue.")
        End If
    End Sub
End Class

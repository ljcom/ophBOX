Imports System.ComponentModel
Imports System.Data.SqlClient
Imports System.IO
Imports Newtonsoft.Json
Imports ophBox.FunctionList

Public Class mainFrm
    Const iac = 0 ''account.png
    Const iacd = 1 ''account2.png
    Const iap = 2 ''approval.png
    Const imb = 3 ''blank.png
    Const ich = 4 ''child module.png
    Const ici = 5 ''column info.png
    Const icl = 6 ''column.png
    Const icr = 7 ''core module.png
    Const idb = 8 ''database.png
    Const ir = 9 ''information.png
    Const iin = 10 ''interface.png
    Const ict = 11 ''mail content.png
    Const ist = 12 ''mail setting.png
    Const imd = 13 ''master module.png
    Const imi = 14 ''menu item.png
    Const imn = 15 ''menu.png
    Const imgi = 16 ''module group info.png
    Const img = 17 ''module group.png
    Const imsd = 18 ''module status detail.png
    Const ims = 19 ''module status.png
    Const imo = 20 ''module.png
    Const inb = 21 ''numbering.png
    Const ipr = 22 ''word.png
    Const irp = 23 ''report.png
    Const isc = 24 ''security.png
    Const isv = 25 ''server.png
    Const isvs = 26 ''servers.png
    Const ith = 27 ''theme.png
    Const iths = 28 ''themes.png
    Const itx = 29 ''transcation.png
    Const itl = 30 ''translation.png
    Const iug = 31 ''user groups.png
    Const ius = 32 ''user status.png
    Const iur = 33 ''user.png
    Const iusi = 34 ''userinfo.png
    Const imv = 35 ''view module.png
    Const iwg = 36 ''widget.png
    Const iwd = 37 ''word.png

    Dim f As FunctionList = FunctionList.Instance
    Private eventHandled As Boolean = False
    Private elapsedTime As Integer
    Private iisId As Long
    Private curDB As String
    Private curServer As String
    Private curUID As String
    Private curPwd As String
    Private curODBC As String
    Private curTable As String
    Private ds As DataSet
    Private dsClone As DataSet
    Private sqlDA As SqlDataAdapter
    Private parentField As String
    Private parentValue As String
    Private handleNew = False
    Private prevRow As Integer
    Private prevCell As Integer
    Private curTag As String
    Dim activePort As Dictionary(Of String, Long) = New Dictionary(Of String, Long)
    Dim isLoading = False
    Dim isSaveTree As Boolean = False
    Private Sub mainFrm_Load(sender As Object, e As EventArgs) Handles Me.Load

        Me.DataGridView1.DefaultCellStyle.WrapMode = DataGridViewTriState.True
        Me.DataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells

        If My.Settings.ophFolder = "" Or Not Directory.Exists(My.Settings.ophFolder & "\OPERAHOUSE") Then
            settingFrm.ShowDialog()
            If Not Directory.Exists(My.Settings.ophFolder & "\OPERAHOUSE") Then
                End
            End If
        Else
            refreshServers()

        End If
    End Sub

    Sub selectNode(node As TreeNode)
        changeContext(node)
        Dim showListView = False
        Me.ListView1.Items.Clear()
        Dim i = f.getTag(node, "type")
        Dim il = 0 'i - 1
        If i = 1 Then
            il = 26
            showListView = True

        ElseIf i = 2 Then 'server
            curServer = node.Text
            curUID = f.getTag(node, "uid")
            curPwd = f.getTag(node, "pwd")
            showListView = True
            il = 25
        ElseIf i = 3 Then
            curDB = node.Text & IIf(node.Text = "oph", "_core", "_data")
            'Dim accountid = node.Text
            'Me.WebBrowser1.Visible = True
            'Me.ListView1.Visible = False
            'Me.WebBrowser1.ScriptErrorsSuppressed = False
            'Me.WebBrowser1.Navigate("http://localhost:8080/" & accountid)
            showListView = True
            il = 7
        ElseIf i = 10 Then  'module
            il = 20
            For Each n In node.Nodes
                Dim ix = f.getTag(n, "type")

                If ix = 100 Then  'core
                    retrieveModules(n, 0, "")
                    n.imageindex = icr
                ElseIf ix = 101 Then  'master
                    retrieveModules(n, 1, "")
                    n.imageindex = imd
                ElseIf ix = 104 Then  'transaction
                    retrieveModules(n, 4, "")
                    n.imageindex = itx
                ElseIf ix = 105 Then  'report
                    retrieveModules(n, 5, "")
                    n.imageindex = irp
                ElseIf ix = 106 Then  'blank
                    retrieveModules(n, 6, "")
                    n.imageindex = imb
                ElseIf ix = 107 Then  'view
                    retrieveModules(n, 7, "")
                    n.imageindex = imv
                ElseIf ix = 108 Then  'msta
                    retrieveModuleStatus(n)
                    n.imageindex = ims
                ElseIf ix = 109 Then  'modg
                    retrieveModuleGroup(n)
                    n.imageindex = img
                End If
            Next
            showListView = True
        ElseIf i = 100 Then  'core
            retrieveModules(node, 0, "")
            il = 8
        ElseIf i = 101 Then  'master
            retrieveModules(node, 1, "")
            il = 13
        ElseIf i = 104 Then  'transaction
            retrieveModules(node, 4, "")
            il = 29
        ElseIf i = 105 Then  'report
            retrieveModules(node, 5, "")
            il = 23
        ElseIf i = 106 Then  'blank
            retrieveModules(node, 6, "")
            il = 4
        ElseIf i = 107 Then  'view
            retrieveModules(node, 7, "")
            il = 35
        ElseIf i = 108 Then  'module status
            retrieveModuleStatus(node)
            il = 19
        ElseIf i = 1080 Then  'module status
            retrieveModuleStatusStatus(node)
            il = 19
        ElseIf i = 109 Then  'module group
            retrieveModuleGroup(node)
            il = 17
        ElseIf i = 1090 Then  'module group
            retrieveModulegroupInfo(node)
            il = 17
        ElseIf i = 1000 Then
            For Each n In node.Nodes
                Dim ix = f.getTag(n, "type")
                If ix = 10001 Then
                    retrieveModuleColumn(n, node.Parent.Text)
                    n.imageindex = icl
                ElseIf ix = 10003 Then
                    retrieveModuleChildren(n, node.Parent.Text)
                    n.imageindex = ich
                End If
            Next
            retrieveModuleInfo(node, node.Text)

        ElseIf i = 10002 Then
            retrieveModuleInfo(node, node.Parent.Text)
        ElseIf i = 10003 Then
            'retrieveModuleInfo(node, node.Parent.Text)
            retrieveModules(node, 0, node.Text)
        ElseIf i = 10001 Then
            retrieveModuleColumn(node, node.Parent.Text)
            'ElseIf i = 10003 Then
            'retrieveModuleChildren(node.Parent.Text)
        ElseIf i = 10004 Then
            retrieveModuleApprovals(node, node.Parent.Text)
        ElseIf i = 10005 Then
            retrieveModuleNumbering(node, node.Parent.Text)
        ElseIf i = 10006 Then
            retrieveModuleMail(node, node.Parent.Text)
        ElseIf i = 100010 Then
            retrieveModuleColumnInfo(node, node.Parent.Parent.Text, node.Text)
        ElseIf i = 11 Then  'security
            For Each n In node.Nodes
                Dim ix = f.getTag(n, "type")
                If ix = 110 Then
                    retrieveUser(n)
                    n.imageindex = iur
                ElseIf ix = 111 Then
                    retrieveUserGroup(n)
                    n.imageindex = iug
                End If
            Next
            showListView = True
        ElseIf i = 110 Then  'user
            il = 4
            retrieveUser(node)
        ElseIf i = 1100 Then  'user
            il = 4
            retrieveUserInfo(node)
        ElseIf i = 111 Then  'user group
            retrieveUserGroup(node)
        ElseIf i = 1110 Then  'user group
            retrieveUserGroupModule(node)

        ElseIf i = 12 Then  'interfaces
            For Each n In node.Nodes
                Dim ix = f.getTag(n, "type")
                If ix = 120 Then
                    retrieveTheme(n)
                    n.imageindex = ith
                ElseIf ix = 121 Then
                    retrieveMenu(n)
                    n.imageindex = imn
                ElseIf ix = 122 Then
                    retrieveTranslator(n)
                    n.imageindex = itl
                End If
            Next
            showListView = True
        ElseIf i = 120 Then  'theme
            retrieveTheme(node)
        ElseIf i = 1200 Then  'theme
            retrieveThemePage(node)
        ElseIf i = 121 Then  'menu
            retrieveMenu(node)
        ElseIf i = 1210 Then  'menu
            retrieveMenuSmnu(node)
        ElseIf i = 122 Then  'translator
            retrieveTranslator(node)
        ElseIf i = 1220 Then  'translator
            retrieveTranslatorLang(node)
        ElseIf i = 13 Then  'account
            For Each n In node.Nodes
                Dim ix = f.getTag(n, "type")
                If ix = 131 Then
                    'retrieveDB(n)
                    n.imageindex = idb
                ElseIf ix = 132 Then
                    retrievePar(n)
                    n.imageindex = ipr
                ElseIf ix = 133 Then
                    'retrieveWidget(n)
                    n.imageindex = iwg
                ElseIf ix = 134 Then
                    'retrieveMail(n)

                End If
            Next
            Dim sqlstr = "select accountinfoguid, accountguid, infokey, infovalue from acctinfo order by infokey"
            curTable = "accinfo"
            loadInfo(node, sqlstr, "accountguid")
            'ElseIf i = 130 Then  'info
        ElseIf i = 131 Then  'databases
            retrieveDB(node)
        ElseIf i = 132 Then  'parameters
            retrievePar(node)
        ElseIf i = 133 Then  'widgets
            retrieveWidget(node)
        ElseIf i = 134 Then  'mail
            retrieveMail(node)
        ElseIf i = 1320 Then  'parameters
            retrieveParInfo(node)
        Else
            showListView = True
        End If
        If showListView Then
            Me.WebBrowser1.Visible = False
            Me.DataGridView1.Visible = False
            Me.ListView1.Visible = True
            'il = 0
            For Each n In node.Nodes
                Dim x = Me.ListView1.Items.Add(n.text)
                'Dim t = f.getTag(n, "type")
                'If t = 1 Then il = 21
                'If t = 110 Then il = 4
                x.ImageIndex = n.imageindex
            Next

        End If

    End Sub
    Private Sub TreeView1_AfterSelect(sender As Object, e As TreeViewEventArgs) Handles TreeView1.AfterSelect
        selectNode(e.Node)
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Me.Close()
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
                        'ElseIf n2 >= 100 And n2 <= 107 or n2=10003 Then
                        'TreeView1.ContextMenuStrip = ContextMenuStrip4
                        'ElseIf n2 = 1000 Then
                        'TreeView1.ContextMenuStrip = ContextMenuStrip5
                        'ElseIf n2 = 100 Then
                        'TreeView1.ContextMenuStrip = ContextMenuStrip6
                    Else
                        TreeView1.ContextMenuStrip = Nothing
                    End If

                End If
            Next
        End If
    End Sub

    Function loadData(nodbc As TreeNode, sqlstr As String, pf As String, pfval As String) As Boolean
        Dim pipename = f.getTag(nodbc, "server") 'Me.TreeView1.SelectedNode.Parent.Parent.Parent.Text
        Dim accountid = f.getTag(nodbc, "accountid") 'Me.TreeView1.SelectedNode.Parent.Parent.Text
        Dim db = "oph_core"
        If accountid.ToLower <> "oph" Then
            db = accountid & "_data"
        End If

        Dim uid = f.getTag(nodbc, "uid")
        Dim pwd = f.getTag(nodbc, "pwd")
        Dim Odbc = "Data Source=" & pipename & ";Initial Catalog=" & db & ";" & IIf(uid <> "", "User Id=" & uid & ";password=" & pwd, "trusted connection=yes")
        curODBC = Odbc

        If pf = "accountguid" Then
            pfval = f.runSQLwithResult("select accountguid from acct where accountid='" & accountid & "'", curODBC)
        End If
        Dim dt As DataTable = New DataTable
        parentField = pf
        parentValue = pfval
        Return f.setDS(ds, sqlDA, sqlstr, Odbc)
    End Function

    Sub loadInfo(n As TreeNode, sqlstr As String, Optional pf As String = "", Optional pfval As String = "")
        If loadData(n, sqlstr, pf, pfval) Then
            isLoading = True
            Dim i = f.getTag(Me.TreeView1.SelectedNode, "type")
            Me.WebBrowser1.Visible = False
            Me.ListView1.Visible = False
            Me.DataGridView1.Visible = True
            Me.DataGridView1.DataSource = ds.Tables(0)
            dsClone = ds.Copy
            Dim c As DataGridViewColumn = Me.DataGridView1.Columns(0)
            c.Visible = False
            For z = 0 To Me.DataGridView1.Columns.Count - 1
                If Me.DataGridView1.Columns(z).Name = parentField Then
                    Me.DataGridView1.Columns(z).Visible = False
                End If
                Me.DataGridView1.Columns(z).AutoSizeMode = IIf(z = Me.DataGridView1.Columns.Count - 1, DataGridViewAutoSizeColumnMode.Fill, DataGridViewAutoSizeColumnMode.AllCells)
            Next
            If curTag <> i Then
                prevRow = Me.DataGridView1.Rows.Count - 1
                If prevRow >= 0 Then
                    prevCell = 2
                    Me.DataGridView1.CurrentCell = Me.DataGridView1.Rows(prevRow).Cells(prevCell)
                End If
            End If
            curTag = i
            isLoading = False

        Else
            'error
        End If
    End Sub
    Sub loadDetail(n As TreeNode, sqlstr As String, m As TreeNode, Optional detailFlag As String = "0", Optional pf As String = "", Optional pfval As String = "", Optional imageindex As Integer = 0)
        If loadData(n, sqlstr, pf, pfval) Then
            isLoading = True
            Dim i = f.getTag(Me.TreeView1.SelectedNode, "type")
            If i = detailFlag.Substring(0, detailFlag.Length - 1) Then
                Me.WebBrowser1.Visible = False
                Me.ListView1.Visible = False
                Me.DataGridView1.Visible = True
                Me.DataGridView1.DataSource = ds.Tables(0)
                dsClone = ds.Copy
                Dim c As DataGridViewColumn = Me.DataGridView1.Columns(0)
                c.Visible = False
                For z = 0 To Me.DataGridView1.Columns.Count - 1
                    If Me.DataGridView1.Columns(z).Name = parentField Then
                        Me.DataGridView1.Columns(z).Visible = False
                    End If
                    Me.DataGridView1.Columns(z).AutoSizeMode = IIf(z = Me.DataGridView1.Columns.Count - 1, DataGridViewAutoSizeColumnMode.Fill, DataGridViewAutoSizeColumnMode.AllCells)
                Next
                If curTag <> i Then
                    prevRow = Me.DataGridView1.Rows.Count - 1
                    If prevRow >= 0 Then
                        prevCell = 2
                        Me.DataGridView1.CurrentCell = Me.DataGridView1.Rows(prevRow).Cells(prevCell)
                    End If
                End If
                isLoading = False
            End If
            m.Nodes.Clear()
            Dim dt = ds.Tables(0)
            For ix As Integer = 0 To dt.Rows.Count - 1
                Dim r As String = dt.Rows(ix).Item(2).ToString()
                Dim guid1 As String = dt.Rows(ix).Item(0).ToString()
                Dim t = m.Nodes.Add(r)
                t.Tag = "type=" & detailFlag & ";guid=" & guid1
                'Dim tc = t.Nodes.Add("Info")
                'tc.Tag = "type=100011"
                t.ImageIndex = imageindex
            Next ix
        Else
            'error
        End If
    End Sub
    Sub setCombo(dgv As DataGridView, sqlstr As String, odbc As String, col As Integer, fieldguid As String, fieldname As String)
        Dim combo As DataTable
        combo = f.SelectSqlSrvRows(sqlstr, odbc)
        Try

            For z = 0 To Me.DataGridView1.Rows.Count - 1
                Dim dgvcc As New DataGridViewComboBoxCell
                With dgvcc
                    .FlatStyle = FlatStyle.Flat
                    .DataSource = combo
                    .ValueMember = fieldguid
                    .DisplayMember = fieldname
                    .ValueType = GetType(Guid)
                End With
                dgv.Item(col, z) = dgvcc
                'dgv.Item(col, z) = dgvcc
            Next
        Catch ex As Exception

        End Try

    End Sub
    Sub retrieveModules(n As TreeNode, settingmode As String, parentcode As String)
        Dim pipename = f.getTag(n, "server")
        'Me.TreeView1.SelectedNode.Parent.Parent.Parent.Text
        Dim accountid = f.getTag(n, "accountid") 'Me.TreeView1.SelectedNode.Parent.Parent.Text
        Dim db = "oph_core"
        If accountid.ToLower <> "oph" Then
            db = accountid & "_data"
        End If
        'Dim db = f.getTag(Me.TreeView1.SelectedNode.Parent, "db")

        Dim uid = f.getTag(n, "uid") 'f.getTag(Me.TreeView1.SelectedNode.Parent.Parent.Parent, "uid")
        Dim pwd = f.getTag(n, "pwd") 'f.getTag(Me.TreeView1.SelectedNode.Parent.Parent.Parent, "pwd")
        Dim Odbc = "Data Source=" & pipename & ";Initial Catalog=" & db & ";" & IIf(uid <> "", "User Id=" & uid & ";password=" & pwd, "trusted connection=yes")
        curODBC = Odbc

        Dim guid = f.getTag(n.Parent, "guid")
        Dim filter = IIf(parentcode = "", "parentmoduleguid is null and settingmode=" & settingmode, "parentmoduleguid='" & guid & "'")
        Dim sqlstr = "select moduleguid, accountguid, moduleid, moduledescription, settingmode, accountdbguid, parentmoduleguid, orderno, needlogin, themepageguid, modulestatusguid, modulegroupguid from modl where " & filter & " order by moduleid"

        curTable = "modl"
        parentField = "accountguid"
        parentValue = f.runSQLwithResult("select accountguid from acct where accountid='" & accountid & "'", curODBC)

        If loadData(n, sqlstr, parentField, parentValue) Then

        End If

        'If f.setDS(ds, sqlDA, sqlstr, Odbc) Then
        dsClone = ds.Copy
        Dim i = f.getTag(Me.TreeView1.SelectedNode, "type")
        If i >= 100 And i <= 107 Or i = 10003 Then
            Me.WebBrowser1.Visible = False
            Me.ListView1.Visible = False
            Me.DataGridView1.Visible = True
            Me.DataGridView1.DataSource = ds.Tables(0)

            sqlstr = "select accountdbguid, databasename from acctdbse"
            setCombo(Me.DataGridView1, sqlstr, Odbc, 5, "accountdbguid", "databasename")
            sqlstr = "select moduleguid, moduleid from modl"
            setCombo(Me.DataGridView1, sqlstr, Odbc, 6, "moduleguid", "moduleid")
            sqlstr = "select themepageguid, themecode+' - '+pageurl name from thmepage p inner join thme t on t.themeguid=p.themeguid"
            setCombo(Me.DataGridView1, sqlstr, Odbc, 9, "themepageguid", "name")
            sqlstr = "select modulestatusguid, modulestatusname from msta"
            setCombo(Me.DataGridView1, sqlstr, Odbc, 10, "modulestatusguid", "modulestatusname")
            sqlstr = "select modulegroupguid, modulegroupid from modg"
            setCombo(Me.DataGridView1, sqlstr, Odbc, 11, "modulegroupguid", "modulegroupid")
            If Me.DataGridView1.Columns.Count > 0 Then
                Dim c As DataGridViewColumn = Me.DataGridView1.Columns(0)
                c.Visible = False
                For z = 0 To Me.DataGridView1.Columns.Count - 1
                    If Me.DataGridView1.Columns(z).Name = parentField Then
                        Me.DataGridView1.Columns(z).Visible = False
                    End If
                    Me.DataGridView1.Columns(z).AutoSizeMode = IIf(z = Me.DataGridView1.Columns.Count - 1, DataGridViewAutoSizeColumnMode.Fill, DataGridViewAutoSizeColumnMode.AllCells)
                Next
            End If
            If curTag <> i Then
                prevRow = Me.DataGridView1.Rows.Count - 1
                If prevRow >= 0 Then
                    prevCell = 2
                    Me.DataGridView1.CurrentCell = Me.DataGridView1.Rows(prevRow).Cells(prevCell)
                End If
            End If
            curTag = i

        End If
        n.Nodes.Clear()
        If ds.Tables.Count > 0 Then
            Dim dt = ds.Tables(0)
            For ix As Integer = 0 To dt.Rows.Count - 1
                Dim r As String = dt.Rows(ix).Item(2).ToString()
                Dim guid1 As String = dt.Rows(ix).Item(0).ToString()
                Dim t = n.Nodes.Add(r)
                t.Tag = "type=1000;guid=" & guid1
                t.ImageIndex = n.Parent.ImageIndex
                Dim tc = t.Nodes.Add("Columns")
                tc.Tag = "type=10001"
                'Dim ti = t.Nodes.Add("Info")
                'ti.Tag = "type=10002"
                Dim tm = t.Nodes.Add("Children")
                'tm.Tag = "type=10003"
                tm.Tag = "type=10003;server=" & pipename & ";db=" & accountid & ";uid=" & uid & ";pwd=" & pwd & ";accountid=" & accountid
                tm.ImageIndex = ich
                Dim ta = t.Nodes.Add("Approvals")
                ta.Tag = "type=10004"
                ta.ImageIndex = iap
                Dim tn = t.Nodes.Add("Numbering")
                tn.Tag = "type=10005"
                tn.ImageIndex = inb
                Dim tl = t.Nodes.Add("Mails")
                tl.Tag = "type=10006"
                tl.ImageIndex = ist
            Next ix
        End If

    End Sub
    Sub retrieveModuleInfo(n As TreeNode, code As String)
        Dim guid = f.getTag(n, "guid")
        Dim sqlstr = "select moduleinfoguid, moduleguid, infokey, infovalue from modlinfo where moduleguid='" & guid & "' order by infokey"
        curTable = "modlinfo"
        loadInfo(n.Parent, sqlstr, "moduleguid", guid)
    End Sub

    Sub retrieveModuleColumn(n As TreeNode, code As String)
        Dim guid = f.getTag(n.Parent, "guid")
        Dim sqlstr = "select columnguid, moduleguid, colkey, coltype, titleCaption, colOrder, collength from modlcolm where moduleguid='" & guid & "' order by colOrder"
        curTable = "modlcolm"
        If n.Text = Me.TreeView1.SelectedNode.Text Then
            loadDetail(n.Parent.Parent, sqlstr, n, 100010, "moduleguid", guid, ici)
            sqlstr = "select 'uniqueidentifier' typename, 36 typeid union select 'datetime', 61 union select 'money', 60 union select 'bigint', 127 union select 'bit', 104 union select 'nvarchar', 231 union select 'Non Field', 0"
            setCombo(Me.DataGridView1, sqlstr, curODBC, 3, "typeid", "typename")
        End If
    End Sub
    Sub retrieveModuleApprovals(n As TreeNode, code As String)
        Dim guid = f.getTag(n.Parent, "guid")
        Dim sqlstr = "select ApprovalGUID, ModuleGUID, ApprovalGroupGUID, UpperGroupGUID, Lvl, SQLfilter, ZoneGroup from modlappr where moduleguid='" & guid & "'"
        curTable = "modlappr"
        loadInfo(n.Parent.Parent, sqlstr, "moduleguid", guid)
        sqlstr = "select ugroupguid, groupid from ugrp"
        setCombo(Me.DataGridView1, sqlstr, curODBC, 2, "ugroupguid", "groupid")
        setCombo(Me.DataGridView1, sqlstr, curODBC, 3, "ugroupguid", "groupid")

    End Sub
    Sub retrieveModuleNumbering(n As TreeNode, code As String)
        Dim guid = f.getTag(n.Parent, "guid")
        Dim sqlstr = "select DocNumberGUID, ModuleGUID, Format, Month, No from modldocn where moduleguid='" & guid & "'"
        curTable = "modldocn"
        loadInfo(n.Parent.Parent, sqlstr, "moduleguid", guid)
    End Sub
    Sub retrieveModuleMail(n As TreeNode, code As String)
        Dim guid = f.getTag(n.Parent, "guid")
        Dim sqlstr = "select ModuleMailGUID, ModuleGUID, MailGUID, ActionGUID, TokenStatus, Additional, CC, Subject, Body, ReportAttachment, DefinedTable from modlmail where moduleguid='" & guid & "'"
        curTable = "modlmail"
        loadInfo(n.Parent.Parent, sqlstr, "moduleguid", guid)
        sqlstr = "select mailguid, profilename from mail"
        setCombo(Me.DataGridView1, sqlstr, curODBC, 2, "mailguid", "profilename")

    End Sub

    Sub retrieveModuleChildren(n As TreeNode, code As String)
        retrieveModules(n, 0, code)

    End Sub
    Sub retrieveModuleColumnInfo(n As TreeNode, code As String, colkey As String)
        Dim guid = f.getTag(n, "guid")
        Dim sqlstr = "select columninfoguid, columnguid, infokey, infovalue from modlcolminfo where columnguid='" & guid & "' order by infokey"
        curTable = "modlcolminfo"

        loadInfo(n.Parent.Parent.Parent, sqlstr, "columnguid", guid)
    End Sub

    Sub retrieveModuleStatus(n As TreeNode)
        Dim sqlstr = "select modulestatusguid, accountguid, modulestatusname, modulestatusdescription from msta"
        curTable = "msta"
        loadDetail(n, sqlstr, n, 1080, "accountguid", "", imsd)
    End Sub

    Sub retrieveModuleStatusStatus(n As TreeNode)
        Dim guid = f.getTag(n, "guid")
        Dim sqlstr = "select modulestatusdetailguid, modulestatusguid, stateid, statecode, statename, statedesc, isdefault from mstastat where modulestatusguid='" & guid & "'"
        curTable = "mstastat"
        loadInfo(n.Parent, sqlstr, "modulestatusguid", guid)
    End Sub

    Sub retrieveModuleGroup(n As TreeNode)
        Dim sqlstr = "select modulegroupguid, accountguid, modulegroupid, modulegroupname, modulegroupdescription from modg"
        curTable = "modg"
        loadDetail(n, sqlstr, n, 1090, "accountguid", "", imgi)
    End Sub

    Sub retrieveModulegroupInfo(n As TreeNode)
        Dim guid = f.getTag(n, "guid")
        Dim sqlstr = "select envinfoguid, modulegroupguid, infokey, infovalue from modginfo where modulegroupguid='" & guid & "' order by infokey"
        curTable = "modginfo"
        loadInfo(n.Parent, sqlstr, "modulegroupguid", guid)
    End Sub
    Sub retrieveUser(n As TreeNode)
        Dim sqlstr = "select userGUID, accountguid, userId, userName, email, autologin, expirypwd, userprofilepath from [user]"
        curTable = "user"
        loadDetail(n.Parent, sqlstr, n, 1100, "accountguid", "", iusi)
    End Sub
    Sub retrieveUserInfo(n As TreeNode)
        Dim guid = f.getTag(n, "guid")
        Dim sqlstr = "select userinfoguid, userguid, infokey, infovalue from userinfo where userguid='" & guid & "' order by infokey"
        curTable = "userinfo"
        loadInfo(n.Parent.Parent, sqlstr, "userguid", guid)
    End Sub

    Sub retrieveUserGroup(n As TreeNode)
        Dim sqlstr = "select ugroupguid, accountguid, groupid, groupdescription from ugrp"
        curTable = "ugrp"
        loadDetail(n.Parent, sqlstr, n, 1110, "accountguid", "", ius)
    End Sub
    Sub retrieveUserGroupModule(n As TreeNode)
        Dim guid = f.getTag(n, "guid")
        Dim sqlstr = "select accessguid, ugroupguid, moduleguid, allowaccess, allowadd, allowedit, allowdelete, allowforce, allowwipe from ugrpmodl where ugroupguid='" & guid & "' order by moduleguid"
        curTable = "ugrpmodl"
        loadInfo(n.Parent.Parent, sqlstr, "ugroupguid", guid)
        setCombo(Me.DataGridView1, "select moduleguid, moduleid from modl", curODBC, 2, "moduleguid", "moduleid")
    End Sub

    Sub retrieveTheme(n As TreeNode)
        Dim sqlstr = "select themeGUID, accountguid, themecode, themename, themefolder from thme"
        curTable = "thme"
        loadDetail(n.Parent, sqlstr, n, 1200, "accountguid", "", ith)
    End Sub
    Sub retrieveThemePage(n As TreeNode)
        Dim guid = f.getTag(n, "guid")
        Dim sqlstr = "select themepageguid, themeguid, pageurl, isdefault from thmepage where themeguid='" & guid & "'"
        curTable = "thmepage"
        loadInfo(n.Parent.Parent, sqlstr, "themeguid", guid)
    End Sub
    Sub retrieveMenu(n As TreeNode)
        Dim sqlstr = "select menuguid, accountguid, menucode, menudescription from menu"
        curTable = "menu"
        loadDetail(n.Parent, sqlstr, n, 1210, "accountguid", "", imi)
    End Sub
    Sub retrieveMenuSmnu(n As TreeNode)
        Dim guid = f.getTag(n, "guid")
        Dim sqlstr = "select menudetailguid, menuguid, submenudescription, tag, url, orderno, caption, type, uppersubmenuguid, icon_fa, icon_url from menusmnu where menuguid='" & guid & "'"
        curTable = "menusmnu"
        loadInfo(n.Parent.Parent, sqlstr, "menuguid", guid)
        sqlstr = "select menudetailguid, submenudescription from menusmnu"
        setCombo(Me.DataGridView1, sqlstr, curODBC, 8, "menudetailguid", "submenudescription")
    End Sub
    Sub retrieveTranslator(n As TreeNode)
        Dim sqlstr = "select wordguid, accountguid, originstatements from word"
        curTable = "word"
        loadDetail(n.Parent, sqlstr, n, 1220, "accountguid", "", itl)
    End Sub
    Sub retrieveTranslatorLang(n As TreeNode)
        Dim guid = f.getTag(n, "guid")
        Dim sqlstr = "select translateguid, wordguid, langid, translationwords from wordlang where wordguid='" & guid & "'"
        curTable = "wordlang"
        loadInfo(n.Parent.Parent, sqlstr, "wordguid", guid)
    End Sub

    Sub retrieveDB(n As TreeNode)
        Dim sqlstr = "select accountdbguid, accountguid, databasename, ismaster, version from acctdbse"
        curTable = "acctdbse"
        loadInfo(n.Parent, sqlstr, "accountguid")
    End Sub
    Sub retrievePar(n As TreeNode)
        Dim sqlstr = "select parameterguid, accountguid, parameterid, parameterdescription from para"
        curTable = "para"
        loadDetail(n.Parent, sqlstr, n, 1320, "accountguid", "", ipr)
    End Sub

    Sub retrieveParInfo(n As TreeNode)
        Dim guid = f.getTag(n, "guid")
        Dim sqlstr = "select parametervalueguid, parameterguid, parametervalue, parameterdescription from paravalu where parameterguid='" & guid & "'"
        curTable = "paravalu"
        loadInfo(n.Parent.Parent, sqlstr, "parameterguid", guid)
    End Sub
    Sub retrieveWidget(n As TreeNode)
        Dim sqlstr = "select widgetguid, accountguid, widgetid, widgetdescription, sqlstr from widg"
        curTable = "widg"
        loadInfo(n.Parent, sqlstr, "accountguid")

    End Sub

    Sub retrieveMail(n As TreeNode)
        Dim sqlstr = "select mailguid, accountguid, profilename, description, accountname, emailaddress, displayname, mailservername, port, timeout, bcc from mail"
        curTable = "mail"
        loadInfo(n.Parent, sqlstr, "accountguid")
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


    Sub refreshServers()
        Dim treeData = My.Settings.treeData
        If treeData = "" Then treeData = "{servers:[]}"
        treeData = treeData.Replace("\", "\\")
        Dim jsonResulttodict = JsonConvert.DeserializeObject(Of Dictionary(Of String, Object))(treeData)
        Dim servers = jsonResulttodict.Item("servers")
        Dim tag = jsonResulttodict.Item("tag")
        Dim t = New TreeNode("Servers")
        t.Tag = tag
        t.ImageIndex = isvs
        For Each serverData In servers
            Dim d = t.Nodes.Add(serverData("server").value.replace("\\", "\"))
            d.tag = serverData("tag").value.replace("\\", "\")
            d.ImageIndex = isv
            Dim uid = f.getTag(d, "uid")
            Dim pwd = f.getTag(d, "pwd")
            For Each dbData In serverData("dbs")
                Dim c = d.Nodes.Add(dbData("db").value)
                c.tag = dbData("tag").value
                c.ImageIndex = idb
                Dim md = c.Nodes.Add("Modules")
                md.tag = "type=10"  ';db=" & c.Text
                md.imageindex = imo
                Dim accountid = c.Text

                Dim mdc = md.Nodes.Add("Core")
                mdc.tag = "type=100;server=" & d.Text & ";db=" & c.Text & ";uid=" & uid & ";pwd=" & pwd & ";accountid=" & accountid
                mdc.imageindex = icr
                Dim mdm = md.Nodes.Add("Master")
                mdm.tag = "type=101;server=" & d.Text & ";db=" & c.Text & ";uid=" & uid & ";pwd=" & pwd & ";accountid=" & accountid
                mdm.imageindex = imd
                Dim mdt = md.Nodes.Add("Transaction")
                mdt.tag = "type=104;server=" & d.Text & ";db=" & c.Text & ";uid=" & uid & ";pwd=" & pwd & ";accountid=" & accountid
                mdt.imageindex = itx
                Dim mdr = md.Nodes.Add("Report")
                mdr.tag = "type=105;server=" & d.Text & ";db=" & c.Text & ";uid=" & uid & ";pwd=" & pwd & ";accountid=" & accountid
                mdr.imageindex = irp
                Dim mdb = md.Nodes.Add("Blank")
                mdb.tag = "type=106;server=" & d.Text & ";db=" & c.Text & ";uid=" & uid & ";pwd=" & pwd & ";accountid=" & accountid
                mdb.imageindex = imb
                Dim mdv = md.Nodes.Add("View")
                mdv.tag = "type=107;server=" & d.Text & ";db=" & c.Text & ";uid=" & uid & ";pwd=" & pwd & ";accountid=" & accountid
                mdv.imageindex = imv
                Dim ms = md.Nodes.Add("Module Status")
                ms.tag = "type=108;server=" & d.Text & ";db=" & c.Text & ";uid=" & uid & ";pwd=" & pwd & ";accountid=" & accountid
                ms.imageindex = ims
                Dim mg = md.Nodes.Add("Module Groups")
                mg.tag = "type=109;server=" & d.Text & ";db=" & c.Text & ";uid=" & uid & ";pwd=" & pwd & ";accountid=" & accountid
                mg.imageindex = img

                Dim sc = c.Nodes.Add("Security")
                sc.tag = "type=11;server=" & d.Text & ";db=" & c.Text & ";uid=" & uid & ";pwd=" & pwd & ";accountid=" & accountid
                sc.imageindex = isc
                Dim us As TreeNode = sc.Nodes.Add("Users")
                us.Tag = "type=110;db=" & c.Text
                us.ImageIndex = iur
                Dim ug = sc.Nodes.Add("User Groups")
                ug.tag = "type=111;db=" & c.Text
                ug.ImageIndex = iug

                Dim it = c.Nodes.Add("Interface")
                it.tag = "type=12;server=" & d.Text & ";db=" & c.Text & ";uid=" & uid & ";pwd=" & pwd & ";accountid=" & accountid
                it.imageindex = iin
                Dim th = it.Nodes.Add("Themes")
                th.imageindex = ith
                th.tag = "type=120;db=" & c.Text
                Dim mn = it.Nodes.Add("Menus")
                mn.tag = "type=121;db=" & c.Text
                mn.imageindex = imn
                Dim tr = it.Nodes.Add("Translator")
                tr.tag = "type=122;db=" & c.Text
                tr.imageindex = itl
                Dim ac = c.Nodes.Add("Account")
                ac.tag = "type=13;server=" & d.Text & ";db=" & c.Text & ";uid=" & uid & ";pwd=" & pwd & ";accountid=" & accountid
                ac.imageindex = iac
                Dim db = ac.Nodes.Add("Databases")
                db.tag = "type=131;db=" & c.Text
                db.imageindex = idb
                Dim pr = ac.Nodes.Add("Parameters")
                pr.tag = "type=132;db=" & c.Text
                pr.imageindex = ipr
                Dim wd = ac.Nodes.Add("Widgets")
                wd.tag = "type=133;db=" & c.Text
                wd.imageindex = iwg
                Dim ml = ac.Nodes.Add("Mail")
                ml.tag = "type=134;db=" & c.Text
                ml.imageindex = ist
            Next
        Next
        Me.TreeView1.Nodes.Clear()
        Me.TreeView1.Nodes.Add(t)

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
    Sub saveTree(Optional noRefresh As Boolean = False)
        Dim treedata = ""
        isSaveTree = True
        For Each svr In Me.TreeView1.Nodes
            Dim servers = ""
            For Each s In svr.nodes
                Dim dbs = ""
                For Each d In s.nodes
                    dbs = dbs & ", {""db"":""" & d.text & """, ""tag"":""" & d.tag & """}"
                Next
                If dbs <> "" Then
                    If dbs.Substring(0, 1) = "," Then dbs = dbs.Substring(2, dbs.Length - 2)
                    servers = servers & ", {""server"":""" & s.text & """, ""dbs"":[" & dbs & "], ""tag"":""" & s.tag & """}"
                End If
            Next
            If servers <> "" Then
                If servers.Substring(0, 1) = "," Then servers = servers.Substring(2, servers.Length - 2)
                treedata = "{""servers"":[" & servers & "], ""tag"":""type=1""}"
            End If

        Next
        If treedata = "" Then treedata = "{servers:[], tag:""type=1""}"
        My.Settings.treeData = treedata
        My.Settings.Save()

        If Not noRefresh Then refreshServers()
        isSaveTree = False
    End Sub

    Private Sub ToolStripMenuItem4_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem4.Click
        removeList()
    End Sub
    Sub removeList()
        Me.TreeView1.SelectedNode.Remove()
        saveTree(False)
    End Sub

    Private Sub RemoveDatabaseToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles RemoveDatabaseToolStripMenuItem.Click
        Dim pipename = Me.TreeView1.SelectedNode.Parent.Text
        Dim accountid = Me.TreeView1.SelectedNode.Text
        If accountid.ToLower <> "oph" Then
            Dim uid = f.getTag(Me.TreeView1.SelectedNode.Parent, "uid")
            Dim pwd = f.getTag(Me.TreeView1.SelectedNode.Parent, "pwd")
            Dim issql = pwd <> ""
            If delDb(accountid, pipename, issql, uid, pwd, True) Then
                Me.TreeView1.SelectedNode.Remove()
                saveTree(False)
                Dim n = f.addAccounttoIIS(accountid, pipename, My.Settings.ophFolder & "\", My.Settings.IISPort, True)
            End If
        Else
            MessageBox.Show("You cannot delete OPH Database. You may delete the server instead.", "Warning")
        End If
    End Sub
    Function delDb(accountid As String, pipename As String, isSQL As Boolean, uid As String, pwd As String, Optional delFile As Boolean = False) As Boolean
        Dim rs = False
        'If accountid.ToLower <> "oph" Then
        If MessageBox.Show("You are about to " & IIf(delFile, "delete", "remove") & " " & accountid & ". Continue?", "Confirmation", MessageBoxButtons.YesNo) = vbYes Then
            Dim odbc = "Data Source=" & pipename & ";Initial Catalog=oph_core;" & IIf(uid <> "", "User Id=" & uid & ";password=" & pwd, "trusted connection=yes")
            If isSQL Then
                odbc = "Data Source=" & pipename & ";Initial Catalog=oph_core;" & IIf(uid <> "", "User Id=" & uid & ";password=" & pwd, "trusted connection=yes")
            End If
            Dim sqlStr = ""
            Dim r = ""
            If delFile Then
                If accountid = "oph" Then
                    odbc = "Data Source=" & pipename & ";Initial Catalog=master;" & IIf(uid <> "", "User Id=" & uid & ";password=" & pwd, "trusted connection=yes")
                    sqlStr = "drop database oph_core"
                    Dim delStr = f.runSQLwithResult(sqlStr, odbc)
                    sqlStr = "select name from sys.databases where name='oph_core'"
                    r = f.runSQLwithResult(sqlStr, odbc)
                Else
                    odbc = "Data Source=" & pipename & ";Initial Catalog=oph_core;" & IIf(uid <> "", "User Id=" & uid & ";password=" & pwd, "trusted connection=yes")
                    sqlStr = "select 'drop database '+databasename+';' from acct a inner join acctdbse d on d.accountguid=a.accountguid where a.accountid='" & accountid & "' for xml path('')"
                    Dim delStr = f.runSQLwithResult(sqlStr, odbc)
                    sqlStr = "delete from d from acct a inner join acctdbse d on d.accountguid=a.accountguid where a.accountid='" & accountid & "'"
                    r = f.runSQLwithResult(sqlStr, odbc)

                    sqlStr = "delete from acct where accountid='" & accountid & "'"
                    r = f.runSQLwithResult(sqlStr, odbc)
                End If
            End If
            If r = "" Then
                rs = True
            End If
        End If
        Return rs
    End Function
    Private Sub TreeView1_KeyDown(sender As Object, e As KeyEventArgs) Handles TreeView1.KeyDown
        If e.KeyData = 46 Then
            If f.getTag(Me.TreeView1.SelectedNode, "type") = "3" Then
                Dim pipename = Me.TreeView1.SelectedNode.Parent.Text
                Dim accountid = Me.TreeView1.SelectedNode.Text
                If accountid.ToLower <> "oph" Then
                    Dim uid = f.getTag(Me.TreeView1.SelectedNode.Parent, "uid")
                    Dim pwd = f.getTag(Me.TreeView1.SelectedNode.Parent, "pwd")
                    Dim issql = pwd <> ""
                    If delDb(accountid, pipename, issql, uid, pwd, True) Then
                        Me.TreeView1.SelectedNode.Remove()
                        saveTree(False)
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
            Dim issql = pwd <> ""
            If delDb("oph", pipename, issql, uid, pwd, True) Then
                saveTree(False)
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
        If accountid.ToLower <> "oph" Then
            Dim uid = f.getTag(Me.TreeView1.SelectedNode.Parent, "uid")
            Dim pwd = f.getTag(Me.TreeView1.SelectedNode.Parent, "pwd")
            Dim issql = pwd <> ""
            If delDb(accountid, pipename, issql, uid, pwd, False) Then
                Me.TreeView1.SelectedNode.Remove()
                saveTree(False)
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
        saveTree(False)
    End Sub
    Private Sub LoadLoadScriptToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LoadScriptToolStripMenuItem.Click
        Dim sqlstr = "exec.gen.loadmodl '*', 1"
        Dim pipename = Me.TreeView1.SelectedNode.Parent.Text
        Dim uid = f.getTag(Me.TreeView1.SelectedNode.Parent, "uid")
        Dim pwd = f.getTag(Me.TreeView1.SelectedNode.Parent, "pwd")
        Dim accountid = Me.TreeView1.SelectedNode.Text
        Dim datadb = "oph_core"
        If accountid.ToLower <> "oph" Then datadb = accountid & "_data"

        Dim odbc = "Data Source=" & pipename & ";Initial Catalog=" & datadb & ";" & IIf(uid <> "", "User Id=" & uid & ";password=" & pwd, "trusted connection=yes")
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
        If accountid.ToLower <> "oph" Then datadb = accountid & "_data"

        Dim odbc = "Data Source=" & pipename & ";Initial Catalog=" & datadb & ";" & IIf(uid <> "", "User Id=" & uid & ";password=" & pwd, "trusted connection=yes")
        OpenFileDialog1.Title = "Restore Script to database"
        OpenFileDialog1.Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*"
        OpenFileDialog1.InitialDirectory = My.Settings.ophFolder
        If OpenFileDialog1.ShowDialog() = DialogResult.OK Then
            Dim sqlstr = "exec gen.savemodl @file='" & OpenFileDialog1.FileName & "', @updateMode=11"
            f.runSQLwithResult(sqlstr, odbc)
        End If
    End Sub

    Private Sub DataGridView1_RowEnter(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView1.RowEnter
        'For i = 0 To Me.DataGridView1.Columns.Count - 1
        '    Me.DataGridView1.Item(i, e.RowIndex).Style.BackColor = Color.Yellow
        'Next
    End Sub

    Private Sub DataGridView1_RowLeave(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView1.RowLeave
        'For i = 0 To Me.DataGridView1.Columns.Count - 1
        '    Me.DataGridView1.Item(i, e.RowIndex).Style.BackColor = Color.Empty
        'Next
    End Sub

    Private Sub DataGridView1_RowValidating(sender As Object, e As DataGridViewCellCancelEventArgs) Handles DataGridView1.RowValidating
        If (Me.DataGridView1.IsCurrentRowDirty) Then
            'find real row
            Dim realRow = Nothing
            Dim n = 0
            For n = 0 To dsClone.Tables(0).Rows.Count - 1
                If dsClone.Tables(0).Rows(n).ItemArray(0).ToString = Me.DataGridView1.Rows(e.RowIndex).Cells(0).Value.ToString Then
                    realRow = n
                End If
            Next
            If realRow Is Nothing Then
                dsClone.Tables(0).Rows.Add()
                'n = 1
                realRow = dsClone.Tables(0).Rows.Count - 1
            End If
            n = 0
            For r = n To Me.DataGridView1.Columns.Count - 1
                If Me.DataGridView1.Columns(r).Name = parentField Then
                    dsClone.Tables(0).Rows(realRow).Item(r) = parentValue
                Else
                    dsClone.Tables(0).Rows(realRow).Item(r) = Me.DataGridView1.Rows(e.RowIndex).Cells(r).Value

                End If
            Next
            e.Cancel = Not saveChanges()
            If Not e.Cancel Then Me.Timer1.Enabled = True

        End If
    End Sub

    Function saveChanges() As Boolean
        Dim err As String = ""
        Dim r = False
        If Not handleNew Then
            If Not f.saveDS(dsClone, sqlDA, curTable, err) Then
                MessageBox.Show(err)
            Else
                r = True
            End If
        Else
            r = True
        End If
        Return r
    End Function

    Private Sub DataGridView1_UserDeletingRow(sender As Object, e As DataGridViewRowCancelEventArgs) Handles DataGridView1.UserDeletingRow
        Dim n = 0
        dsClone.Tables(0).Rows(e.Row.Index).Delete()
        e.Cancel = Not saveChanges()
        If Not e.Cancel Then Me.Timer1.Enabled = True
    End Sub

    Private Sub DataGridView1_RowValidated(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView1.RowValidated
    End Sub

    Private Sub mainFrm_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.KeyCode = 116 Then 'f5 
            refreshTree()
        End If
    End Sub

    Sub refreshTree()
        isSaveTree = True
        selectNode(Me.TreeView1.SelectedNode)
        Try
            Me.DataGridView1.CurrentCell = Me.DataGridView1.Rows(prevRow).Cells(prevCell)
        Catch ex As Exception

        End Try
        isSaveTree = False
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Me.Timer1.Enabled = False
        refreshTree()

    End Sub

    Private Sub DataGridView1_DataError(sender As Object, e As DataGridViewDataErrorEventArgs) Handles DataGridView1.DataError
        'Stop
        If e.Exception.HResult = -2147024809 Then
            'Stop
        Else
            MessageBox.Show(e.Exception.Message)
        End If
    End Sub

    Private Sub DataGridView1_Sorted(sender As Object, e As EventArgs) Handles DataGridView1.Sorted
    End Sub

    Private Sub DataGridView1_CellEnter(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView1.CellEnter
        If e.ColumnIndex >= 2 And Not isSaveTree Then
            prevCell = e.ColumnIndex
            prevRow = e.RowIndex
        End If
    End Sub

    Private Sub DataGridView1_SelectionChanged(sender As Object, e As EventArgs) Handles DataGridView1.SelectionChanged
    End Sub

    Private Sub TreeView1_Click(sender As Object, e As EventArgs) Handles TreeView1.Click
        Me.TreeView1.SelectedNode.SelectedImageIndex = Me.TreeView1.SelectedNode.ImageIndex
        'Me.TreeView1.SelectedNode
    End Sub

    Private Sub RefreshToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles RefreshToolStripMenuItem.Click
        Dim curNodeParent As String = Me.TreeView1.SelectedNode.Parent.Text
        Dim curNodeName As String = Me.TreeView1.SelectedNode.Text
        Dim curNodeTag As String = Me.TreeView1.SelectedNode.Tag.ToString
        Dim mode = f.getTag(Me.TreeView1.SelectedNode, "mode")
        Dim uid = f.getTag(Me.TreeView1.SelectedNode, "uid")
        Dim pwd = f.getTag(Me.TreeView1.SelectedNode, "pwd")
        Dim port = f.getTag(Me.TreeView1.SelectedNode, "port")
        If f.addServer(Me.TreeView1, mode, curNodeName, uid, pwd, False, port) Then
            'refreshServers()
            If Not checkNode(Me.TreeView1.TopNode, curNodeParent, curNodeName, curNodeTag) Then

            End If
        End If
    End Sub
    Function checkNode(selNode As TreeNode, parent As String, name As String, tag As String) As Boolean
        Dim isSelected = False
        For Each n In selNode.Nodes
            If Not n.parent.text Is Nothing AndAlso n.parent.text = parent And n.tag.ToString = tag And n.text = name Then
                Me.TreeView1.SelectedNode = n
                isSelected = True
            Else
                checkNode(n, parent, name, tag)
            End If
        Next
        Return isSelected
    End Function
End Class

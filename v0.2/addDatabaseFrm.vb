Imports System.ComponentModel
Imports System.Data.SqlClient
Imports System.IO
Imports ophBox.FunctionList

Public Class addDatabaseFrm
    Dim f As FunctionList = FunctionList.Instance
    Dim canClose = False

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        canClose = True
        Me.Close()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim type = f.getTag(mainFrm.TreeView1.SelectedNode, "type")
        Dim curNode As TreeNode = mainFrm.TreeView1.SelectedNode
        If type = "3" Then
            curNode = mainFrm.TreeView1.SelectedNode.Parent
        End If
        Dim pipename = curNode.Text
        Dim uid = f.getTag(curNode, "uid")
        Dim pwd = f.getTag(curNode, "pwd")
        Dim port = f.getTag(curNode, "port")
        Dim datadb = "oph_core"
        If Me.TextBox1.Text <> "oph" Then datadb = Me.TextBox1.Text & "_data"

        If checkDB(Me.TextBox1.Text, pipename, uid, pwd) Then
            Dim sqlstr = ""
            If addDB(Me.TextBox1.Text, pipename, uid, pwd, "", "", port) Then
                Dim isexists = False
                For Each n In curNode.Nodes
                    If n.text = Me.TextBox1.Text Then isexists = True
                Next
                If Not isexists Then
                    Dim x = curNode.Nodes.Add(Me.TextBox1.Text)
                    x.Tag = "type=3;dbname=" & datadb
                End If
                canClose = True
                Me.Close()
            End If
        Else
            If Me.TextBox3.Text = "" Then
                MessageBox.Show("The password cannot be empty")
            ElseIf Me.TextBox3.Text.Length < 8 Then
                MessageBox.Show("The password  not long enough. (8 character min.) and must use number and special character")
            ElseIf Me.TextBox3.Text <> Me.TextBox4.Text Then
                MessageBox.Show("Your password is not match")
            Else
                Dim adminID = Me.TextBox2.Text
                Dim adminPwd = Me.TextBox3.Text

                If addDB(Me.TextBox1.Text, pipename, uid, pwd, adminID, adminPwd, port) Then
                    Dim x = curNode.Nodes.Add(Me.TextBox1.Text)
                    x.Tag = "type=3;dbname=" & datadb
                    canClose = True
                    Me.Close()
                End If
            End If
        End If
    End Sub
    Function checkDB(accountid, pipename, uid, pwd) As Boolean
        Dim r = False
        Dim db = "oph_core"
        If accountid <> "oph" Then db = accountid & "_data_data"

        Dim odbc = "Data Source=" & pipename & ";Initial Catalog=master;" & IIf(uid <> "", "User Id=" & uid & ";password=" & pwd, "trusted connection=yes")
        Dim sqlstr = "select name from sys.databases where name='" & db & "'"
        Dim x = f.runSQLwithResult(sqlstr, odbc)
        If x <> "" Then r = True
        Return r
    End Function
    Function addDB(accountid, pipename, uid, pwd, adminid, adminpwd, iisport) As Boolean
        Me.Cursor = Cursors.WaitCursor
        Dim r = False
        Dim coreDB = "oph_core"
        Dim datadb = "oph_core"
        Dim ophserver = My.Settings.ophServer
        Dim v4db = ""
        Dim filedb = ""
        Dim msg = ""
        If accountid <> "oph" Then
            datadb = accountid & "_data"
            v4db = accountid & "_v4"
            filedb = accountid & "_file"
        End If
        Dim Odbc = "Data Source=" & pipename & ";Initial Catalog=master;" & IIf(uid <> "", "User Id=" & uid & ";password=" & pwd, "trusted connection=yes")

        Dim result = f.runSQLwithResult("if not exists(select * from sys.databases where name='" & datadb & "') CREATE DATABASE " & datadb, Odbc)
        If v4db <> "" Then f.runSQLwithResult("if not exists(select * from sys.databases where name='" & v4db & "') CREATE DATABASE " & v4db, Odbc)
        If filedb <> "" Then f.runSQLwithResult("if not exists(select * from sys.databases where name='" & filedb & "') CREATE DATABASE " & filedb, Odbc)

        result = f.runSQLwithResult("select name from sys.databases where name like '" & accountid & "%'", Odbc)
        If result <> "" Then

            Dim tuser = "sam"
            Dim secret = "D627AFEB-9D77-40E4-B060-7C976DA05260"
            Dim c_uri = ophserver & "/oph"
            Dim ophpath = My.Settings.ophFolder
            Dim token = ""
            Do While token = ""
                token = f.getToken(c_uri, tuser, secret)
                If token = "" Then
                    If MessageBox.Show("The connection is failed. Do you want to try again?", "Confirm", MessageBoxButtons.YesNo) = vbYes Then
                    Else
                        Exit Do
                    End If
                End If
            Loop

            If token <> "" Then
                Dim folderTemp = "temp"
                Dim url = c_uri & "/ophcore/api/sync.aspx?mode=reqcorescript&isnew=1&token=" & token
                Dim scriptFile1 = ophpath & "\" & folderTemp & "\install_" & accountid & ".sql"
                f.SetLog(scriptFile1, , True)
                f.runScript(url, pipename, scriptFile1, datadb, uid, pwd)
                f.runScript(url, pipename, scriptFile1, datadb, uid, pwd, False)
                f.runScript(url, pipename, scriptFile1, datadb, uid, pwd, False)

                Odbc = "Data Source=" & pipename & ";Initial Catalog=" & datadb & ";" & IIf(uid <> "", "User Id=" & uid & ";password=" & pwd, "trusted connection=yes")
                Dim sqlstr = "select accountguid from acct where accountid='" & accountid & "'"
                Dim accountguid = f.runSQLwithResult(sqlstr, Odbc)


                If accountguid = "" Then
                    'not exists
                    Odbc = "Data Source=" & pipename & ";Initial Catalog=oph_core;" & IIf(uid <> "", "User Id=" & uid & ";password=" & pwd, "trusted connection=yes")
                    sqlstr = "
				        if not exists(select * from oph_core.dbo.acct where accountid='" & accountid & "')
                            insert into oph_core.dbo.acct(accountguid, accountid)
				            values (newid(), '" & accountid & "')

				        if not exists(
                            select * from oph_core.dbo.acctdbse d 
                                inner join oph_core.dbo.acct a on a.accountguid=d.accountguid  
                            where a.accountid='" & accountid & "' and databasename='" & datadb & "')

                            insert into oph_core.dbo.acctdbse (accountguid, databasename, ismaster, version)
                            select accountguid, '" & datadb & "', 1, '4.0'
                            from oph_core.dbo.acct where accountid='" & accountid & "'

				        if not exists(
                            select * from oph_core.dbo.acctdbse d 
                                inner join oph_core.dbo.acct a on a.accountguid=d.accountguid  
                            where a.accountid='" & accountid & "' and databasename='" & accountid & "_v4')

                            insert into oph_core.dbo.acctdbse (accountguid, databasename, ismaster, version)
                            select accountguid, '" & accountid & "_v4', 0, '4.0'
                            from oph_core.dbo.acct where accountid='" & accountid & "'

				        if not exists(
                            select * from oph_core.dbo.acctdbse d 
                                inner join oph_core.dbo.acct a on a.accountguid=d.accountguid  
                            where a.accountid='" & accountid & "' and databasename='" & accountid & "_file')

                            insert into oph_core.dbo.acctdbse (accountguid, databasename, ismaster, version)
                            select accountguid, '" & accountid & "_file', 0, '4.0'
                            from oph_core.dbo.acct where accountid='" & accountid & "'

				        if not exists(
                            select * from oph_core.dbo.acctinfo d 
                                inner join oph_core.dbo.acct a on a.accountguid=d.accountguid  
                            where a.accountid='" & accountid & "' and infokey='address')

	                        insert into acctinfo (accountguid, infokey, infovalue)
                            select accountguid, 'address', 'localhost:" & iisport & "/{accountid}' 
                            from oph_core.dbo.acct where accountid='" & accountid & "'

				        if not exists(
                            select * from oph_core.dbo.acctinfo d 
                                inner join oph_core.dbo.acct a on a.accountguid=d.accountguid  
                            where a.accountid='" & accountid & "' and infokey='whiteaddress')

	                        insert into acctinfo (accountguid, infokey, infovalue)
                            select accountguid, 'whiteaddress', 'localhost:" & iisport & "/{accountid}' 
                            from oph_core.dbo.acct where accountid='" & accountid & "'

				        if not exists(
                            select * from oph_core.dbo.acctinfo d 
                                inner join oph_core.dbo.acct a on a.accountguid=d.accountguid  
                            where a.accountid='" & accountid & "' and infokey='odbc')

	                        insert into acctinfo (accountguid, infokey, infovalue)
                            select accountguid, 'odbc', 'Data Source=" & pipename & ";Initial Catalog=" & datadb & ";" & IIf(uid <> "", "User Id=" & uid & ";password=" & pwd, "trusted connection=yes") & "' 
                            from oph_core.dbo.acct where accountid='" & accountid & "'

				        if not exists(
                            select * from oph_core.dbo.acctinfo d 
                                inner join oph_core.dbo.acct a on a.accountguid=d.accountguid  
                            where a.accountid='" & accountid & "' and infokey='frontpage')

	                        insert into acctinfo (accountguid, infokey, infovalue)
                            select accountguid, 'frontpage', 'modl' 
                            from oph_core.dbo.acct where accountid='" & accountid & "'

				        if not exists(
                            select * from oph_core.dbo.acctinfo d 
                                inner join oph_core.dbo.acct a on a.accountguid=d.accountguid  
                            where a.accountid='" & accountid & "' and infokey='signinpage')

	                        insert into acctinfo (accountguid, infokey, infovalue)
                            select accountguid, 'signinpage', 'login' 
                            from oph_core.dbo.acct where accountid='" & accountid & "'

                        "
                Else
                    Odbc = "Data Source=" & pipename & ";Initial Catalog=" & datadb & ";" & IIf(uid <> "", "User Id=" & uid & ";password=" & pwd, "trusted connection=yes")
                    sqlstr = "
				        if not exists(select * from oph_core.dbo.acct where accountid='" & accountid & "')
                            insert into oph_core.dbo.acct(accountguid, accountid)
				            values ('" & accountguid & "', '" & accountid & "')

                        insert into oph_core.dbo.acctdbse (accountdbguid, accountguid, databasename, ismaster, version)
                        select a.accountdbguid, a.accountguid, a.databasename, a.ismaster, a.version 
                        from acctdbse a
                             left join oph_core.dbo.acctdbse b on a.accountdbguid=b.accountdbguid
                        where a.accountguid='" & accountguid & "' and b.accountdbguid is null

                        insert into oph_core.dbo.acctinfo (accountinfoguid, accountguid, infokey, infovalue)
                        select a.accountinfoguid, a.accountguid, a.infokey, a.infovalue
                        from acctinfo a
                             left join oph_core.dbo.acctinfo b on a.accountinfoguid=b.accountinfoguid
                        where a.accountguid='" & accountguid & "' and b.accountinfoguid is null

                    "

                End If

                result = f.runSQLwithResult(sqlstr, Odbc)
                If result = "" Then
                    'sqlstr = "select accountid from acct where accountid='" & accountid & "'"
                    'result = f.runSQLwithResult(sqlstr, Odbc)

                    'If result <> "" Then
                    Odbc = "Data Source=" & pipename & ";Initial Catalog=" & datadb & ";" & IIf(uid <> "", "User Id=" & uid & ";password=" & pwd, "trusted connection=yes")
                    sqlstr = "
                        insert into acct(accountguid, accountid)
                        select a.accountguid, a.accountid 
                        from oph_core.dbo.acct a
                            left join acct b on a.accountid=b.accountid
                        where a.accountid='" & accountid & "' and b.accountguid is null

                        insert into acctdbse (accountdbguid, accountguid, databasename, ismaster, version)    
                        select i.accountdbguid, i.accountguid, i.databasename, i.ismaster, i.version
                        from oph_core.dbo.acctdbse i
                            inner join oph_core.dbo.acct a on a.accountguid=i.accountguid
                            inner join acct a2 on a2.accountid=a.accountid
                            left join acctdbse i2 on a2.accountguid=i2.accountguid and i2.accountdbguid=i.accountdbguid
                        where i2.accountdbguid is null and a.accountid='" & accountid & "'

                        insert into acctinfo (accountinfoguid, accountguid, infokey, infovalue)    
                        select i.accountinfoguid, i.accountguid, i.infokey, i.infovalue 
                        from oph_core.dbo.acctinfo i
                            inner join oph_core.dbo.acct a on a.accountguid=i.accountguid
                            inner join acct a2 on a2.accountid=a.accountid
                            left join acctinfo i2 on a2.accountguid=i2.accountguid and i2.accountinfoguid=i.accountinfoguid
                        where i2.accountinfoguid is null and a.accountid='" & accountid & "'
                
                    "
                    result = f.runSQLwithResult(sqlstr, Odbc)
                    If result = "" Then
                        url = c_uri & "/ophcore/api/sync.aspx?mode=reqmodules&token=" & token
                        scriptFile1 = ophpath & "\" & folderTemp & "\modules_" & accountid & ".sql"
                        If File.Exists(scriptFile1) Then File.Delete(scriptFile1)
                        Do While Not File.Exists(scriptFile1)
                            f.downloadFilename(url, scriptFile1)

                            If Not File.Exists(scriptFile1) Then
                                If MessageBox.Show("The connection is failed. Do you want to try again?", "Confirm", MessageBoxButtons.YesNo) = vbYes Then
                                Else
                                    Exit Do
                                End If
                            End If
                        Loop

                        If File.Exists(scriptFile1) Then

                            Odbc = "Data Source=" & pipename & ";Initial Catalog=" & datadb & ";" & IIf(uid <> "", "User Id=" & uid & ";password=" & pwd, "trusted connection=yes")
                            sqlstr = "exec gen.savemodl @file='" & scriptFile1 & "', @updatemode=11"
                            f.runSQLwithResult(sqlstr, Odbc)
                            f.runSQLwithResult(sqlstr, Odbc)
                            'f.runScript(url, pipename, scriptFile1, datadb, uid, pwd)
                            If Me.TextBox2.Text <> "" Then
                                sqlstr = "insert into [user] (accountguid, userid, username, email, password) 
                                    select accountguid, '" & Me.TextBox2.Text & "', '" & Me.TextBox2.Text & "', '" & Me.TextBox2.Text & "@', ''
                                    from acct where accountid='" & accountid & "'
                        
                                    declare @userguid uniqueidentifier
                                    select @userguid=userguid from [user] where userid='" & Me.TextBox2.Text & "'
                
                                    exec gen.resetPassword null, '" & Me.TextBox2.Text & "', @userguid, @password=N'" & Me.TextBox3.Text & "', @accountid='" & accountid & "'
                                    
                                       select userid from [user] where userid='" & Me.TextBox2.Text & "'
                                "
                                result = f.runSQLwithResult(sqlstr, Odbc)
                                If result <> "" Then
                                    Dim n = f.addAccounttoIIS(Me.TextBox1.Text, pipename, My.Settings.ophFolder & "\", iisport, False)
                                Else
                                    msg = "User creation is failed."
                                End If
                            End If
                            'r = True
                        End If
                    Else
                        msg = result '"Database failed to be populated."
                    End If

                Else
                    msg = result '"Database failed to be added to oph_core."
                End If

            End If
        Else
            msg = "Database creation is failed."
        End If
        If msg <> "" Then
            'rollback
            MessageBox.Show(msg)
        Else
            r = True
        End If
        Me.Cursor = Cursors.Default
        Return r
    End Function

    Private Sub addDatabaseFrm_Load(sender As Object, e As EventArgs) Handles Me.Load
        canClose = False
        Me.TextBox1.Text = ""
        Me.TextBox2.Text = ""
        Me.TextBox3.Text = ""
        Me.TextBox4.Text = ""
        Me.TextBox1.Select()

        Dim curName = mainFrm.TreeView1.SelectedNode.Text
        'If curName = "Servers" Then
        'Me.Text = "Add Server"
        'Else
        Dim curTag = mainFrm.TreeView1.SelectedNode.Tag
        Dim t = curTag.split(";")
        For Each ct In t
            curName = ""

            If ct.split("=").length > 1 Then curName = ct.split("=")(1)
            If ct.split("=")(0) = "type" Then
                If curName = "3" Then
                    Me.Text = "Server Properties"
                    Me.TextBox1.Text = mainFrm.TreeView1.SelectedNode.Text
                    Me.Button1.Text = "Save"
                    Me.TextBox1.Enabled = False
                    Me.TextBox2.Enabled = False
                    Me.TextBox3.Enabled = False
                    Me.TextBox4.Enabled = False
                Else
                    curName = "Add Database"
                    Me.TextBox1.Text = ""
                    Me.Button1.Text = "Add"
                    Me.TextBox1.Enabled = True
                    Me.TextBox2.Enabled = True
                    Me.TextBox3.Enabled = True
                    Me.TextBox4.Enabled = True
                End If
            End If
        Next

        'End If

    End Sub

    Private Sub addDatabaseFrm_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        e.Cancel = Not canClose
    End Sub

    Private Sub TextBox2_TextChanged(sender As Object, e As EventArgs) Handles TextBox2.TextChanged

    End Sub

    Private Sub Label4_Click(sender As Object, e As EventArgs) Handles Label4.Click

    End Sub

    Private Sub TextBox3_TextChanged(sender As Object, e As EventArgs) Handles TextBox3.TextChanged

    End Sub

    Private Sub Label3_Click(sender As Object, e As EventArgs) Handles Label3.Click

    End Sub

    Private Sub TextBox4_TextChanged(sender As Object, e As EventArgs) Handles TextBox4.TextChanged

    End Sub

    Private Sub Label2_Click(sender As Object, e As EventArgs) Handles Label2.Click

    End Sub

    Private Sub TextBox1_TextChanged(sender As Object, e As EventArgs) Handles TextBox1.TextChanged
        Dim type = f.getTag(mainFrm.TreeView1.SelectedNode, "type")
        Dim curNode = mainFrm.TreeView1.SelectedNode
        If type = "3" Then
            curNode = mainFrm.TreeView1.SelectedNode.Parent
        End If
        Dim pipename = f.getTag(curNode, "server")
        Dim uid = f.getTag(curNode, "uid")
        Dim pwd = f.getTag(curNode, "pwd")
        If checkDB(Me.TextBox1.Text, pipename, uid, pwd) Then
            'f.runSQLwithResult("select * f)
            'Me.TextBox2.Enabled = False
            'Me.TextBox3.Enabled = False
            Me.TextBox4.Enabled = False
        End If
    End Sub
End Class
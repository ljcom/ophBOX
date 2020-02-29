<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class mainFrm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim TreeNode1 As System.Windows.Forms.TreeNode = New System.Windows.Forms.TreeNode("Servers")
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(mainFrm))
        Me.Button1 = New System.Windows.Forms.Button()
        Me.SplitContainer1 = New System.Windows.Forms.SplitContainer()
        Me.TreeView1 = New System.Windows.Forms.TreeView()
        Me.ContextMenuStrip1 = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.AddServerToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ImageList1 = New System.Windows.Forms.ImageList(Me.components)
        Me.DataGridView1 = New System.Windows.Forms.DataGridView()
        Me.WebBrowser1 = New System.Windows.Forms.WebBrowser()
        Me.ListView1 = New System.Windows.Forms.ListView()
        Me.ImageList2 = New System.Windows.Forms.ImageList(Me.components)
        Me.ContextMenuStrip2 = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.AddDatabaseToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem2 = New System.Windows.Forms.ToolStripSeparator()
        Me.StartIISExpessToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.PropertiesToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem4 = New System.Windows.Forms.ToolStripMenuItem()
        Me.DeleteServerToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ContextMenuStrip3 = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.DatabasePropertiesToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.RemoveFromListToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.RemoveDatabaseToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem1 = New System.Windows.Forms.ToolStripSeparator()
        Me.LoadScriptToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.SaveScriptToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.SaveFileDialog1 = New System.Windows.Forms.SaveFileDialog()
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog()
        Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
        Me.ContextMenuStrip4 = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.AddModuleToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ContextMenuStrip5 = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.DeleteModuleToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ContextMenuStrip6 = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.RefreshToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SplitContainer1.Panel1.SuspendLayout()
        Me.SplitContainer1.Panel2.SuspendLayout()
        Me.SplitContainer1.SuspendLayout()
        Me.ContextMenuStrip1.SuspendLayout()
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.ContextMenuStrip2.SuspendLayout()
        Me.ContextMenuStrip3.SuspendLayout()
        Me.ContextMenuStrip4.SuspendLayout()
        Me.ContextMenuStrip5.SuspendLayout()
        Me.SuspendLayout()
        '
        'Button1
        '
        Me.Button1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button1.Font = New System.Drawing.Font("Microsoft Sans Serif", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button1.Location = New System.Drawing.Point(644, 394)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(144, 44)
        Me.Button1.TabIndex = 1
        Me.Button1.Text = "&Close"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'SplitContainer1
        '
        Me.SplitContainer1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.SplitContainer1.Location = New System.Drawing.Point(12, 12)
        Me.SplitContainer1.Name = "SplitContainer1"
        '
        'SplitContainer1.Panel1
        '
        Me.SplitContainer1.Panel1.Controls.Add(Me.TreeView1)
        '
        'SplitContainer1.Panel2
        '
        Me.SplitContainer1.Panel2.Controls.Add(Me.DataGridView1)
        Me.SplitContainer1.Panel2.Controls.Add(Me.WebBrowser1)
        Me.SplitContainer1.Panel2.Controls.Add(Me.ListView1)
        Me.SplitContainer1.Size = New System.Drawing.Size(776, 376)
        Me.SplitContainer1.SplitterDistance = 258
        Me.SplitContainer1.TabIndex = 5
        '
        'TreeView1
        '
        Me.TreeView1.ContextMenuStrip = Me.ContextMenuStrip1
        Me.TreeView1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TreeView1.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TreeView1.ImageIndex = 0
        Me.TreeView1.ImageList = Me.ImageList1
        Me.TreeView1.Location = New System.Drawing.Point(0, 0)
        Me.TreeView1.Name = "TreeView1"
        TreeNode1.Name = "Node0"
        TreeNode1.Tag = "type=1"
        TreeNode1.Text = "Servers"
        Me.TreeView1.Nodes.AddRange(New System.Windows.Forms.TreeNode() {TreeNode1})
        Me.TreeView1.SelectedImageIndex = 0
        Me.TreeView1.Size = New System.Drawing.Size(258, 376)
        Me.TreeView1.TabIndex = 1
        '
        'ContextMenuStrip1
        '
        Me.ContextMenuStrip1.ImageScalingSize = New System.Drawing.Size(24, 24)
        Me.ContextMenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.AddServerToolStripMenuItem})
        Me.ContextMenuStrip1.Name = "ContextMenuStrip1"
        Me.ContextMenuStrip1.Size = New System.Drawing.Size(173, 36)
        '
        'AddServerToolStripMenuItem
        '
        Me.AddServerToolStripMenuItem.Name = "AddServerToolStripMenuItem"
        Me.AddServerToolStripMenuItem.Size = New System.Drawing.Size(172, 32)
        Me.AddServerToolStripMenuItem.Text = "&Add Server"
        '
        'ImageList1
        '
        Me.ImageList1.ImageStream = CType(resources.GetObject("ImageList1.ImageStream"), System.Windows.Forms.ImageListStreamer)
        Me.ImageList1.TransparentColor = System.Drawing.Color.Transparent
        Me.ImageList1.Images.SetKeyName(0, "account-01.png")
        Me.ImageList1.Images.SetKeyName(1, "account2-01.png")
        Me.ImageList1.Images.SetKeyName(2, "approval-01.png")
        Me.ImageList1.Images.SetKeyName(3, "blank-01.png")
        Me.ImageList1.Images.SetKeyName(4, "child module-01.png")
        Me.ImageList1.Images.SetKeyName(5, "column info-01.png")
        Me.ImageList1.Images.SetKeyName(6, "column-01.png")
        Me.ImageList1.Images.SetKeyName(7, "core module-01.png")
        Me.ImageList1.Images.SetKeyName(8, "database-01.png")
        Me.ImageList1.Images.SetKeyName(9, "information-01.png")
        Me.ImageList1.Images.SetKeyName(10, "interface-01.png")
        Me.ImageList1.Images.SetKeyName(11, "mail content-01.png")
        Me.ImageList1.Images.SetKeyName(12, "mail setting-01.png")
        Me.ImageList1.Images.SetKeyName(13, "master module-01.png")
        Me.ImageList1.Images.SetKeyName(14, "menu item2-01.png")
        Me.ImageList1.Images.SetKeyName(15, "menu-01.png")
        Me.ImageList1.Images.SetKeyName(16, "module group info-01-01.png")
        Me.ImageList1.Images.SetKeyName(17, "module group-01.png")
        Me.ImageList1.Images.SetKeyName(18, "module status detail-01.png")
        Me.ImageList1.Images.SetKeyName(19, "module status-01.png")
        Me.ImageList1.Images.SetKeyName(20, "module-01.png")
        Me.ImageList1.Images.SetKeyName(21, "numbering-01.png")
        Me.ImageList1.Images.SetKeyName(22, "parameter-01.png")
        Me.ImageList1.Images.SetKeyName(23, "report-01.png")
        Me.ImageList1.Images.SetKeyName(24, "security-01.png")
        Me.ImageList1.Images.SetKeyName(25, "server-01.png")
        Me.ImageList1.Images.SetKeyName(26, "servers-01.png")
        Me.ImageList1.Images.SetKeyName(27, "theme-01.png")
        Me.ImageList1.Images.SetKeyName(28, "themes-01.png")
        Me.ImageList1.Images.SetKeyName(29, "transcation-01.png")
        Me.ImageList1.Images.SetKeyName(30, "translation-01.png")
        Me.ImageList1.Images.SetKeyName(31, "user groups-01.png")
        Me.ImageList1.Images.SetKeyName(32, "user status-01.png")
        Me.ImageList1.Images.SetKeyName(33, "user-01.png")
        Me.ImageList1.Images.SetKeyName(34, "userinfo-01.png")
        Me.ImageList1.Images.SetKeyName(35, "view module-01.png")
        Me.ImageList1.Images.SetKeyName(36, "widget-01.png")
        Me.ImageList1.Images.SetKeyName(37, "word-01.png")
        '
        'DataGridView1
        '
        Me.DataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.DataGridView1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.DataGridView1.Location = New System.Drawing.Point(0, 0)
        Me.DataGridView1.Name = "DataGridView1"
        Me.DataGridView1.RowHeadersWidth = 30
        Me.DataGridView1.RowTemplate.Height = 28
        Me.DataGridView1.Size = New System.Drawing.Size(514, 376)
        Me.DataGridView1.TabIndex = 2
        '
        'WebBrowser1
        '
        Me.WebBrowser1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.WebBrowser1.Location = New System.Drawing.Point(0, 0)
        Me.WebBrowser1.MinimumSize = New System.Drawing.Size(20, 20)
        Me.WebBrowser1.Name = "WebBrowser1"
        Me.WebBrowser1.Size = New System.Drawing.Size(514, 376)
        Me.WebBrowser1.TabIndex = 1
        Me.WebBrowser1.Visible = False
        '
        'ListView1
        '
        Me.ListView1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.ListView1.Font = New System.Drawing.Font("Microsoft Sans Serif", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ListView1.HideSelection = False
        Me.ListView1.LargeImageList = Me.ImageList2
        Me.ListView1.Location = New System.Drawing.Point(0, 0)
        Me.ListView1.Name = "ListView1"
        Me.ListView1.Size = New System.Drawing.Size(514, 376)
        Me.ListView1.TabIndex = 0
        Me.ListView1.UseCompatibleStateImageBehavior = False
        '
        'ImageList2
        '
        Me.ImageList2.ImageStream = CType(resources.GetObject("ImageList2.ImageStream"), System.Windows.Forms.ImageListStreamer)
        Me.ImageList2.TransparentColor = System.Drawing.Color.Transparent
        Me.ImageList2.Images.SetKeyName(0, "account-01.png")
        Me.ImageList2.Images.SetKeyName(1, "account2-01.png")
        Me.ImageList2.Images.SetKeyName(2, "approval-01.png")
        Me.ImageList2.Images.SetKeyName(3, "blank-01.png")
        Me.ImageList2.Images.SetKeyName(4, "child module-01.png")
        Me.ImageList2.Images.SetKeyName(5, "column info-01.png")
        Me.ImageList2.Images.SetKeyName(6, "column-01.png")
        Me.ImageList2.Images.SetKeyName(7, "core module-01.png")
        Me.ImageList2.Images.SetKeyName(8, "database-01.png")
        Me.ImageList2.Images.SetKeyName(9, "information-01.png")
        Me.ImageList2.Images.SetKeyName(10, "interface-01.png")
        Me.ImageList2.Images.SetKeyName(11, "mail content-01.png")
        Me.ImageList2.Images.SetKeyName(12, "mail setting-01.png")
        Me.ImageList2.Images.SetKeyName(13, "master module-01.png")
        Me.ImageList2.Images.SetKeyName(14, "menu item2-01.png")
        Me.ImageList2.Images.SetKeyName(15, "menu-01.png")
        Me.ImageList2.Images.SetKeyName(16, "module group info-01.png")
        Me.ImageList2.Images.SetKeyName(17, "module group-01.png")
        Me.ImageList2.Images.SetKeyName(18, "module status detail-01.png")
        Me.ImageList2.Images.SetKeyName(19, "module status-01.png")
        Me.ImageList2.Images.SetKeyName(20, "module-01.png")
        Me.ImageList2.Images.SetKeyName(21, "numbering-01.png")
        Me.ImageList2.Images.SetKeyName(22, "parameter-01.png")
        Me.ImageList2.Images.SetKeyName(23, "report-01.png")
        Me.ImageList2.Images.SetKeyName(24, "security-01.png")
        Me.ImageList2.Images.SetKeyName(25, "server-01.png")
        Me.ImageList2.Images.SetKeyName(26, "servers-01.png")
        Me.ImageList2.Images.SetKeyName(27, "theme-01.png")
        Me.ImageList2.Images.SetKeyName(28, "themes-01.png")
        Me.ImageList2.Images.SetKeyName(29, "transcation-01.png")
        Me.ImageList2.Images.SetKeyName(30, "translation-01.png")
        Me.ImageList2.Images.SetKeyName(31, "user groups-01.png")
        Me.ImageList2.Images.SetKeyName(32, "user status-01.png")
        Me.ImageList2.Images.SetKeyName(33, "user-01.png")
        Me.ImageList2.Images.SetKeyName(34, "userinfo-01.png")
        Me.ImageList2.Images.SetKeyName(35, "view module-01.png")
        Me.ImageList2.Images.SetKeyName(36, "widget-01.png")
        Me.ImageList2.Images.SetKeyName(37, "word-01.png")
        '
        'ContextMenuStrip2
        '
        Me.ContextMenuStrip2.ImageScalingSize = New System.Drawing.Size(24, 24)
        Me.ContextMenuStrip2.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.AddDatabaseToolStripMenuItem, Me.ToolStripMenuItem2, Me.StartIISExpessToolStripMenuItem, Me.PropertiesToolStripMenuItem, Me.ToolStripMenuItem4, Me.DeleteServerToolStripMenuItem, Me.RefreshToolStripMenuItem})
        Me.ContextMenuStrip2.Name = "ContextMenuStrip2"
        Me.ContextMenuStrip2.Size = New System.Drawing.Size(274, 235)
        '
        'AddDatabaseToolStripMenuItem
        '
        Me.AddDatabaseToolStripMenuItem.Name = "AddDatabaseToolStripMenuItem"
        Me.AddDatabaseToolStripMenuItem.Size = New System.Drawing.Size(273, 32)
        Me.AddDatabaseToolStripMenuItem.Text = "&Add Database..."
        '
        'ToolStripMenuItem2
        '
        Me.ToolStripMenuItem2.Name = "ToolStripMenuItem2"
        Me.ToolStripMenuItem2.Size = New System.Drawing.Size(270, 6)
        '
        'StartIISExpessToolStripMenuItem
        '
        Me.StartIISExpessToolStripMenuItem.Name = "StartIISExpessToolStripMenuItem"
        Me.StartIISExpessToolStripMenuItem.Size = New System.Drawing.Size(273, 32)
        Me.StartIISExpessToolStripMenuItem.Text = "Start IIS Expess"
        '
        'PropertiesToolStripMenuItem
        '
        Me.PropertiesToolStripMenuItem.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.PropertiesToolStripMenuItem.Name = "PropertiesToolStripMenuItem"
        Me.PropertiesToolStripMenuItem.Size = New System.Drawing.Size(273, 32)
        Me.PropertiesToolStripMenuItem.Text = "Server &Properties..."
        '
        'ToolStripMenuItem4
        '
        Me.ToolStripMenuItem4.Name = "ToolStripMenuItem4"
        Me.ToolStripMenuItem4.Size = New System.Drawing.Size(273, 32)
        Me.ToolStripMenuItem4.Text = "&Remove Server from list"
        '
        'DeleteServerToolStripMenuItem
        '
        Me.DeleteServerToolStripMenuItem.Name = "DeleteServerToolStripMenuItem"
        Me.DeleteServerToolStripMenuItem.Size = New System.Drawing.Size(273, 32)
        Me.DeleteServerToolStripMenuItem.Text = "&Delete Server"
        '
        'ContextMenuStrip3
        '
        Me.ContextMenuStrip3.ImageScalingSize = New System.Drawing.Size(24, 24)
        Me.ContextMenuStrip3.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.DatabasePropertiesToolStripMenuItem, Me.RemoveFromListToolStripMenuItem, Me.RemoveDatabaseToolStripMenuItem, Me.ToolStripMenuItem1, Me.LoadScriptToolStripMenuItem, Me.SaveScriptToolStripMenuItem})
        Me.ContextMenuStrip3.Name = "ContextMenuStrip3"
        Me.ContextMenuStrip3.Size = New System.Drawing.Size(338, 170)
        '
        'DatabasePropertiesToolStripMenuItem
        '
        Me.DatabasePropertiesToolStripMenuItem.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.DatabasePropertiesToolStripMenuItem.ForeColor = System.Drawing.Color.Black
        Me.DatabasePropertiesToolStripMenuItem.Name = "DatabasePropertiesToolStripMenuItem"
        Me.DatabasePropertiesToolStripMenuItem.Size = New System.Drawing.Size(337, 32)
        Me.DatabasePropertiesToolStripMenuItem.Text = "Database &Properties..."
        '
        'RemoveFromListToolStripMenuItem
        '
        Me.RemoveFromListToolStripMenuItem.Name = "RemoveFromListToolStripMenuItem"
        Me.RemoveFromListToolStripMenuItem.Size = New System.Drawing.Size(337, 32)
        Me.RemoveFromListToolStripMenuItem.Text = "&Remove from List"
        '
        'RemoveDatabaseToolStripMenuItem
        '
        Me.RemoveDatabaseToolStripMenuItem.Name = "RemoveDatabaseToolStripMenuItem"
        Me.RemoveDatabaseToolStripMenuItem.Size = New System.Drawing.Size(337, 32)
        Me.RemoveDatabaseToolStripMenuItem.Text = "&Delete Database"
        '
        'ToolStripMenuItem1
        '
        Me.ToolStripMenuItem1.Name = "ToolStripMenuItem1"
        Me.ToolStripMenuItem1.Size = New System.Drawing.Size(334, 6)
        '
        'LoadScriptToolStripMenuItem
        '
        Me.LoadScriptToolStripMenuItem.Name = "LoadScriptToolStripMenuItem"
        Me.LoadScriptToolStripMenuItem.Size = New System.Drawing.Size(337, 32)
        Me.LoadScriptToolStripMenuItem.Text = "&Backup Script..."
        '
        'SaveScriptToolStripMenuItem
        '
        Me.SaveScriptToolStripMenuItem.Name = "SaveScriptToolStripMenuItem"
        Me.SaveScriptToolStripMenuItem.Size = New System.Drawing.Size(337, 32)
        Me.SaveScriptToolStripMenuItem.Text = "&Restore Script to this database..."
        '
        'Button2
        '
        Me.Button2.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button2.Font = New System.Drawing.Font("Microsoft Sans Serif", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button2.Location = New System.Drawing.Point(494, 394)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(144, 44)
        Me.Button2.TabIndex = 6
        Me.Button2.Text = "&Setting..."
        Me.Button2.UseVisualStyleBackColor = True
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.FileName = "OpenFileDialog1"
        '
        'Timer1
        '
        '
        'ContextMenuStrip4
        '
        Me.ContextMenuStrip4.ImageScalingSize = New System.Drawing.Size(24, 24)
        Me.ContextMenuStrip4.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.AddModuleToolStripMenuItem})
        Me.ContextMenuStrip4.Name = "ContextMenuStrip4"
        Me.ContextMenuStrip4.Size = New System.Drawing.Size(185, 36)
        '
        'AddModuleToolStripMenuItem
        '
        Me.AddModuleToolStripMenuItem.Name = "AddModuleToolStripMenuItem"
        Me.AddModuleToolStripMenuItem.Size = New System.Drawing.Size(184, 32)
        Me.AddModuleToolStripMenuItem.Text = "&Add Module"
        '
        'ContextMenuStrip5
        '
        Me.ContextMenuStrip5.ImageScalingSize = New System.Drawing.Size(24, 24)
        Me.ContextMenuStrip5.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.DeleteModuleToolStripMenuItem})
        Me.ContextMenuStrip5.Name = "ContextMenuStrip5"
        Me.ContextMenuStrip5.Size = New System.Drawing.Size(201, 36)
        '
        'DeleteModuleToolStripMenuItem
        '
        Me.DeleteModuleToolStripMenuItem.Name = "DeleteModuleToolStripMenuItem"
        Me.DeleteModuleToolStripMenuItem.Size = New System.Drawing.Size(200, 32)
        Me.DeleteModuleToolStripMenuItem.Text = "&Delete Module"
        '
        'ContextMenuStrip6
        '
        Me.ContextMenuStrip6.ImageScalingSize = New System.Drawing.Size(24, 24)
        Me.ContextMenuStrip6.Name = "ContextMenuStrip6"
        Me.ContextMenuStrip6.Size = New System.Drawing.Size(61, 4)
        '
        'RefreshToolStripMenuItem
        '
        Me.RefreshToolStripMenuItem.Name = "RefreshToolStripMenuItem"
        Me.RefreshToolStripMenuItem.Size = New System.Drawing.Size(273, 32)
        Me.RefreshToolStripMenuItem.Text = "&Refresh"
        '
        'mainFrm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(800, 450)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.SplitContainer1)
        Me.Controls.Add(Me.Button1)
        Me.KeyPreview = True
        Me.Name = "mainFrm"
        Me.Text = "OPH Installer"
        Me.SplitContainer1.Panel1.ResumeLayout(False)
        Me.SplitContainer1.Panel2.ResumeLayout(False)
        CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.SplitContainer1.ResumeLayout(False)
        Me.ContextMenuStrip1.ResumeLayout(False)
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ContextMenuStrip2.ResumeLayout(False)
        Me.ContextMenuStrip3.ResumeLayout(False)
        Me.ContextMenuStrip4.ResumeLayout(False)
        Me.ContextMenuStrip5.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents Button1 As Button
    Friend WithEvents SplitContainer1 As SplitContainer
    Friend WithEvents TreeView1 As TreeView
    Friend WithEvents ListView1 As ListView
    Friend WithEvents ContextMenuStrip1 As ContextMenuStrip
    Friend WithEvents AddServerToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ContextMenuStrip2 As ContextMenuStrip
    Friend WithEvents AddDatabaseToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ToolStripMenuItem2 As ToolStripSeparator
    Friend WithEvents ToolStripMenuItem4 As ToolStripMenuItem
    Friend WithEvents ContextMenuStrip3 As ContextMenuStrip
    Friend WithEvents RemoveDatabaseToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents Button2 As Button
    Friend WithEvents PropertiesToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents DatabasePropertiesToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents DeleteServerToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents RemoveFromListToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents StartIISExpessToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents WebBrowser1 As WebBrowser
    Friend WithEvents ToolStripMenuItem1 As ToolStripSeparator
    Friend WithEvents LoadScriptToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents SaveScriptToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents SaveFileDialog1 As SaveFileDialog
    Friend WithEvents OpenFileDialog1 As OpenFileDialog
    Friend WithEvents DataGridView1 As DataGridView
    Friend WithEvents Timer1 As Timer
    Friend WithEvents ImageList1 As ImageList
    Friend WithEvents ImageList2 As ImageList
    Friend WithEvents ContextMenuStrip4 As ContextMenuStrip
    Friend WithEvents AddModuleToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ContextMenuStrip5 As ContextMenuStrip
    Friend WithEvents DeleteModuleToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ContextMenuStrip6 As ContextMenuStrip
    Friend WithEvents RefreshToolStripMenuItem As ToolStripMenuItem
End Class

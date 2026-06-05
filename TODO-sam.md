Clue: [ ] todo belum selesai, [x] sudah dikerjakan dan dikerjakan codex, [v] sudah dicek manual oleh manusia, [!] belum rapi, masih salah.

## account
 - [x] di sidebar, saat klik subaccount tree, buat di bawahnya list sub account
 - [x] jika klik tree level tiap sub account tampilkan juga acctinfo yang related

## user
 - [v] isi list user: userid, user name, email, expirydate saja
 - [x] di sidebar, jika user diklik, tampilkan list user di bawah tree
 - [x] di tiap list user parameter, tampilkan isi userinfo

## user group
 - [v] urutan groupid, groupdescrption
 - [x] di sidebar, jika user group diklik, tampilkan list user group di bawah tree
 - [x] di tiap list user group parameter, tampilkan isi ugrpmodl

## menu
 - [v] Cannot run metadata query: Token error: 'Invalid column name 'menuname'.' on server ip-172-31-27-162 executing on line 5 (code: 207, state: 1, class: 16)
- [x] urutan MENUCODE , MENUDESCRIPTION,	CREATEDDATE, UPDATEDDATE	
 - [ ] di sidebar, jika menu diklik, tampilkan list menu di bawah tree
 - [ ] di tiap list menu parameter, tampilkan isi menusmnu

## theme
 - [x] di themes, tampilkan list thme
 - [x] di sidebar, jika theme diklik, tampilkan list theme di bawah tree
 - [x] di tiap list theme parameter, tampilkan isi thmepage


## translator
- [x] urutan ORIGINSTATEMENTS, CREATEDDATE, updateddate
 - [ ] di sidebar, jika translator diklik, tampilkan list word di bawah tree
 - [ ] di tiap list word parameter, tampilkan isi wordlang

## module status
 - [v] Cannot run metadata query: Token error: 'Invalid column name 'statusid'.' on server ip-172-31-27-162 executing on line 5 (code: 207, state: 1, class: 16)
 - [x] urutan MODULESTATUSNAME, 	ISDEFAULT, CREATED DATE, updateddate

 ## module group
 - [x] urutan MODULEGROUPID, MODULEGROUPNAME, MODULEGROUPDESCRIPTION		

## parameter
 - [x] urutan PARAMETERID, PARAMETERDESCRIPTION, CREATEDDATE, updateddate
 - [ ] di sidebar, jika parameters diklik, tampilkan list parameter di bawah tree
 - [ ] di tiap list tree parameter, tampilkan isi paravalu

## mail
- [x] urutan PROFILENAME, ACCOUNTNAME, DISPLAYNAME,	EMAILADDRESS, BCC, 	CREATEDDATE, updateddate

## widget
 - [x] urutan widgetid, CREATED DATE	updateddate	

## modules
 - [x] untuk modules dibagi berdasarkan settingmode: 0=core, 1=master, 4=transaction, 5=report, 6=blank, 7=view
 - [x] hapus setting mode dari list di semua anak module
 - [x] urutan: moduleid, moduledescription, settingmode, accountdb, parentmodule, orderno, needlogin, themepage, modulestatus, modulegroup
 - [x] MODULE STATUS ambil modulestatusname dari msta
 - [x] MODULE GROUP ambil modulegroupname dari modg
 - [x] accountdb ambil dari database name dari acctdbse
 - [x] themepage ambil dari pageurl dari thmepage
 - [x] jika core, master, ... view di klik, buat anak tree berupa list of modules. di dalam setiap module ada anak lagi: columns, children, approvals, numbering dan mails 
 - [x] di setiap tree level Columns keluarkan list modlcolm yang berhubungan dengan module tersebut di main panel
 - [x] di sidebar columns, anaknya tampilkan list of colkey
 - [x] di children, tampilkan seluruh module yang parentmodulenya relate dengan parentnya
 - [x] di sidebar children, anaknya tampilkan list of anak module
 - [x] di dalam children module, buat anak tree lagi: columns, children.
 - [x] setiap children berisi anak dari bersifat recursive
 - [x] isi approvals diambil dari dari table modlappr
 - [x] isi numbering diambil dari modldocn
 - [x] isi mails diambil dari modlmail
 - [x] di setiap tree level column: tampilkan list modlcolminfo di main panel. 
 - [x] jika klik tree level module/children, tampilkan di main panel list modlinfo
 - [x] di setiap list tambahkan tombol add, dan langsung membuka overlay kanan kosong

## sidebar:
 - [x] posisi database tree defaultnya collapse.

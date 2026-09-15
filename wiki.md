# OPH5 Rules From OPERAHOUSE Wiki

Source: https://github.com/ljcom/operahouse/wiki

Purpose: this file turns the legacy OPERAHOUSE wiki into implementation rules for OPH5. Treat these rules as the local compatibility contract, especially for permissions, module configuration, columns, browse, forms, reports, and workflow.

## Rule Priority

1. Database metadata is the source of truth.
2. Legacy stored procedures/functions capture behavior that should not be reimplemented manually unless unavoidable.
3. Frontend must not hardcode module order, field visibility, field editability, permissions, status lists, or page layout.
4. When behavior is unclear, prefer the legacy wiki/table semantics over convenience in the React/API shell.

## Core Tables

Use these tables as the primary metadata model:

- Account/config: `acct`, `acctinfo`, `acctdbse`
- Environment/module: `modg`, `modl`, `modlinfo`
- Column metadata: `modlcolm`, `modlcolminfo`
- Security/workflow: `user`, `userinfo`, `userdele`, `ugrp`, `modlappr`
- Theme/layout: `thme`, `thmepage`

## Account Rules

- Account identity comes from `acct`.
- Account settings come from `acctinfo`.
- Database inventory/targets come from `acctdbse`.
- `oph_core` can be used as the root catalog: first resolve the account and its master database from `acctdbse`, then execute account data/procedure work in the resolved `*_data` database.
- If `oph_core.acctinfo` is minimal, merge `acctinfo` from the resolved account data database for company name, theme, frontpage, and configured allowed addresses. Allowed hosts still come only from `acctinfo.address` / `acctinfo.whiteaddress`.
- The first page/module must come from `acctinfo.FrontPage`.
- The login page must come from `acctinfo.SigninPage`.
- The active theme must come from `acctinfo.ThemeCode`.
- SQL connection metadata may exist in `acctinfo.ODBC`.
- Host/account validation must use `acctinfo.Address`.
- `acctinfo.Address` can contain `{accountid}` and must be expanded when validating routes.
- `acctinfo.WhiteAddress` is a captcha/host allow-list concept.
- `acctinfo.debugMode=1` means mail must not be sent to real destinations.
- Cloudflare Turnstile for signin/forgot-password is controlled by API env `TURNSTILE_SITE_KEY` and `TURNSTILE_SECRET_KEY`; only the site key is exposed through bootstrap, and token verification happens server-side.
- `acctinfo.accounttype` controls subaccount inheritance:
  - `independent`: data/user scope is isolated, but root/core metadata and the parent/root `acctdbse` may still be used as fallback when local core settings are missing.
  - `child`: the account may use `acct.parentAccountGUID` for fallback config/database resolution.
- If `acctinfo.accounttype` is missing, an account with `acct.parentAccountGUID` is treated as `child` for backward compatibility.
- Child account documents, storage, audit, and active account scope remain under the child account id/guid.
- Legacy core modules with `modl.settingMode=0` may currently be shared with
  child accounts. Before `routeDepth` enforcement is enabled for an account
  tree, every core module that must remain shared must receive an explicit
  nonzero or wildcard `routeDepth`; `settingMode=0` must not remain an implicit
  route-inheritance grant.
- Auth utility routes such as `/signin`, `/forgot-pwd`, and `/profile` are not environments and must not return `ENV_NOT_FOUND`.

### Signup and Membership Provisioning

- Signup is an auth utility route and may use a dedicated account-configured ThemePage such as `member-signup`.
- The canonical field order is name, email, phone, password, then package.
- A free Buyer signup creates a root-account user in group `buyer`; it does not create `MoMEMB` or a subaccount.
- Paid Agent, Partner, and Merchant signup first creates a pending registration. Passwords must not be retained in pending registration data. User, `MoMEMB`, group membership, and any subaccount are provisioned only after payment has been verified.
- Paid signup follows explicit Signup, Payment, and Complete stages. Complete must be unlocked by server-owned payment status from a signature-verified processor callback, never by browser navigation or a mock action.
- Merchant is the paid level that requests a subaccount. Subaccount creation is a provisioning action, not a public signup-form insert.
- Package identity, price, level, payment requirement, and subaccount flag must be loaded from account data and revalidated by the API.
- New web users use a one-way password credential. Legacy `api.verifyPassword` is still invoked to establish `HostGUID`; malformed output may only be recovered after independent credential verification and confirmation of the matching `userhost` row.

### Falcon One-Level Upliner

- A Falcon `MoMEMB` row may reference zero or one direct upliner through
  `SponsorMemberGUID`; the UI caption is `Upliner`.
- A member cannot be their own upliner.
- Referral commission is earned only by the direct upliner, exactly one level.
  Never walk the upliner chain recursively for multi-level commission.
- Order attribution must snapshot the direct upliner and applicable versioned
  commission rule at transaction time. Later upliner changes do not rewrite
  posted commission history; reversal is recorded in the commission ledger.

### Falcon Marketplace Root/Subaccount Rules

- `falconnet` is the root marketplace account.
- A merchant such as `freeway` is a child/subaccount of `falconnet` and may use
  the root account's routed master database when it has no local `acctdbse` row.
- Merchant product management uses module `MoPRODISKU` through the merchant
  subaccount route. Normal merchant access remains scoped by the active merchant
  account and its `AccountGUID`.
- Root marketplace product access uses module `product`, not `MoPRODISKU`.
- Root module `product` is a view over all `MoPRODISKU` rows. The source query
  does not filter on the source rows' `AccountGUID`.
- The `product` view projects `AccountGUID` as the hardcoded root `falconnet`
  account GUID. This allows ordinary root browse ownership filtering to return
  the aggregated catalog.
- `MoPRODISKU` remains the product source of truth. Do not introduce a separate
  published-listing/index module for Falcon unless the user explicitly changes
  this rule.
- Because the projected `AccountGUID` belongs to the root, merchant/source
  ownership needed for cart, delivery, and transaction routing must not be
  inferred from `product.AccountGUID`. The authoritative separate source-owner
  field/contract is still pending user confirmation.

## Route And Module Resolution Rules

- In an explicit URL `/:account/:env/:code`, resolve the module by `code`.
- In that explicit URL, `env` is route/menu location context. It must not prevent resolving a valid module whose own `ModuleGroupGUID` points to a different `modg`.
- Module IDs must resolve case-insensitively.
- Account home route must evaluate `acctinfo.FrontPage` chain in order and select the first accessible module.
- Environment route must evaluate `modginfo.frontpage` for that environment.
- Frontend must accept redirects from API/bootstrap as source of truth.

### Hierarchical Module Route Scope

`modlinfo.routeDepth` controls at which relative account-tree depths a module
may be resolved. It controls module availability for an account route; it does
not grant user permission or change document-row ownership.

Depth is measured from `modl.AccountGUID`, the module metadata owner, to the
active routed account:

- `0`: module owner only.
- `1`: direct child accounts only.
- `2`: direct grandchildren only.
- `0,1`: module owner and direct children.
- `1,2`: direct children and grandchildren.
- `1+`: all descendants, excluding the module owner.
- `*`: module owner and every descendant depth.

Values are exact allowed depths unless they use the `+` or `*` notation.
Whitespace and duplicate values must be ignored during normalization. Invalid
or negative values other than the documented notation must fail closed.

The canonical fallback is `0`: when no active `modlinfo.routeDepth` row exists,
the resolver must behave exactly as if its value were `0`. Loaders do not need
to materialize a `routeDepth=0` row. An explicit `0` remains valid and
equivalent, primarily for migration or diagnostic use.

Resolution must:

1. build the active account's lineage and calculate its distance from each
   candidate module owner;
2. prefer the closest eligible module owner when the same case-insensitive
   `ModuleID` exists at multiple points in the lineage;
3. reject a candidate whose calculated distance is not allowed by
   `routeDepth`;
4. run `gen.authModl` after route-depth eligibility has been established; and
5. continue using the active routed account GUID for browse, form, save,
   settings, files, reports, jobs, and row-count scope.

The three boundaries are independent:

```text
modlinfo.routeDepth  = account-tree routes where the module can resolve
ugrp / gen.authModl  = users and roles allowed to access the resolved module
document AccountGUID = tenant rows the active account may read or write
```

`modl.AccountGUID` remains metadata ownership and `modl.AccountDBGUID` remains
module deployment/storage binding. Neither becomes document-row ownership.
Route depth must never be used to broaden tenant data predicates.

During rollout, existing shared `settingMode=0` modules must be inventoried and
given explicit route-depth metadata before the resolver starts enforcing this
contract for their account tree. After enforcement is enabled, `settingMode`
does not alter the fallback: an absent `routeDepth` always means `0`.

For Falcon, the intended split is:

| Feature | Example module | `routeDepth` | Permission scope |
|---|---|---:|---|
| Root website | `home` | `0` | Public |
| Root marketplace | `product` | `0` | Public/Buyer |
| Buyer history and chat | `buyer-history`, `buyer-chat` | `0` | Buyer roles |
| Merchant settings | `seller`, `seller-chat` | `1` | Merchant/Admin |
| Merchant product management | `MoPRODISKU` | `1` | Merchant/Admin |

Falcon subaccount menus are still sourced exclusively from the canonical root.
Each root menu item must then be filtered by both the active route depth and
the authenticated permission result. Menu ownership does not override module
route eligibility.

For the `mx4campus` account tree, every `modl.AccountGUID` is the canonical
`mx4campus` root GUID. Root SaaS pages use the canonical absent-`routeDepth`
fallback (`0`), while modules intended for a direct institution/campus
subaccount use `routeDepth=1`. The active routed child account still owns its
account settings, menus, documents, files, audit events, and business-row
scope. `mx4campus_data` bootstrap therefore enforces route depth during public
and authenticated module resolution; `settingMode` is not an inheritance rule
for this tree.

## Module Rules

`modl` defines the module. Important `settingMode` values:

- `0`: core
- `1`: master
- `3`: strategy
- `4`: transaction
- `5`: report
- `6`: blank
- `7`: view

Rules:

- `modl.ModuleID` is the public module code.
- `modl.ModuleDescription` is the user-facing title unless another metadata label overrides it.
- `modl.needLogin` controls public vs authenticated access.
- `modl.ModuleGroupGUID` links to module group/environment, but explicit route env is only location context.
- `modl.ThemePageGUID -> thmepage.pageURL` determines form/browse theme model.
- Setting modes `1`, `3`, and `4` should use the `[db]_v4` database convention.
- `settingMode=5` is report mode and must use report rules, not ordinary browse-table rules.
- For view modules, `modlinfo.InfoKey=view` contains the backing SQL/view definition.
- `modlinfo.isDisabled=1` means the module should not appear in menu and should not be usable.

### Universal Theme Template Contract

- A `ThemeCode`/ThemePage model is one shared frontend template across every
  project, root account, and subaccount.
- Frontend rendering must not branch on an account ID, database name, project
  name, or deployment hostname. A template improvement therefore becomes
  available to every account using that template.
- Account differences belong in metadata, data, permissions, and explicitly
  documented capability settings. These values may enable business behavior
  such as pickup, OTP, notes, or payment methods, but must not select a separate
  account-specific copy of the page layout.
- Ecommerce cart, delivery, payment, completion, history, chat, and seller
  pages follow this contract. The current shared Ecommerce checkout presentation
  is the card/wizard flow; `View Cart` must render it for every Ecommerce
  account.
- Seller Settings values are account state: every read and write uses
  `acctinfo` under the current routed `AccountGUID`, even when the `seller` or
  `product` module metadata is owned by a root account and exposed to a child
  through `routeDepth`.
- Ecommerce shipping is also universal. The API derives each cart item's
  source merchant from the server-side `doc.MoPRODISKU.AccountGUID`, groups the
  cart by that account, and requests one shipping quote per origin. It never
  trusts a browser-supplied merchant ID.
- Every merchant origin comes from that merchant's current `acctinfo`
  (`deliveryOriginAddress`, latitude, longitude, and contact fields). A root
  marketplace origin is not a fallback for a child merchant, and missing
  origin data must return an explicit configuration error.
- Multi-origin delivery keeps one selected rate per merchant and presents the
  aggregate fee to payment. Creating the parent marketplace order, merchant
  allocations, shipment rows, and settlement remains a separate transaction
  contract; delivery quoting alone must not silently invent that orchestration.
- Merchant product publication can opt in through
  `modlinfo.publishRequiresVerifiedOrigin=1`. Saving a draft remains allowed,
  but submit/publish must verify the current account's origin through the
  server-side shipping service first.
- Origin verification is bound to a hash of the current address and
  coordinates. Changing any of those values invalidates the prior
  verification. Marketplace aggregate views must expose only released products
  whose source merchant has a current `deliveryOriginVerificationStatus` of
  `verified`; draft and legacy published rows from unverified merchants remain
  stored but are not listed.

## Module Info Rules

`modlinfo` adds behavior to `modl`.

Important keys:

- `fa`: icon class.
- `Allow<event>`: capability configuration, such as access/add/edit/delete.
- `showDocInfo=1`: show form sidebar/document information.
- `showDocInfo=0`: show menu instead of form sidebar.
- `Script_<event>`: SQL procedure/script for lifecycle events.
- `View_<code>`: related view script for the module.
- `View`: make a module into a view.
- `View_browse_attr`: row/field style metadata for browse.
- `Index_<name>` and `Unique_<name>`: index definitions in `column=...;filter=...` format.
- `DocNo`, `DocRefNo`, `DocId`: document numbering definitions.
- `childKey`: child table field used as child key; default legacy behavior is `parentDocGUID`.
- `parentKey`: parent table field used as parent key; default legacy behavior is `docGUID`.
- `orderField`: default browse sort. Supports multiple fields and `DESC`.
- `browserows`: default browse row count.
- `oldPrimary`: compatibility value for older v3 custom stored procedures.
- `dataFilter`: browse/form data restrictions.
- `allowShowSummaryColumn`: controls summary column visibility in browse.
- `partition_<code>`: partition condition for large tables.
- `routeDepth`: relative account-tree depths where the module may resolve; see
  Hierarchical Module Route Scope.

### Document Number Format

Document numbering formats are stored in `modlinfo.InfoValue` using one of
these keys:

- `DocNo`: document number; requested from `api.getNumber` with `mode=0`.
- `DocRefNo`: document reference number; requested with `mode=1`.
- `DocId`: master/document ID; requested with `mode=2`.

`api.getNumber` recognizes these lowercase brace tokens:

| Token | Result |
|---|---|
| `{nn}` through `{nnnnnnn}` | Sequence padded to 2 through 7 digits |
| `{mm}` | Two-digit month |
| `{m}` | Alphabetic month (`A` through `L`) |
| `{mr}` | Roman-numeral month |
| `{yy}` | Two-digit year |
| `{yyyy}` | Four-digit year |
| `{dd}` | Two-digit day |
| `{d}` | Alphabetic day using the current legacy implementation |
| `{b}` | First character of `acctinfo.branchcode` |
| `{bb}` | Full `acctinfo.branchcode` (currently two characters) |
| `{v1}` | Caller-supplied `@var1` value |

Literal text and punctuation may be placed anywhere in the format. For
example, `{nnnn}.{mm}.{yyyy}.SOR.KL` produces a four-digit sequence followed
by month, year, and the literal module/company suffix.

Sequence scope is inferred from date tokens:

- A format containing `{d}` or `{dd}` uses a daily sequence bucket.
- Otherwise, a format containing `{m}`, `{mm}`, or `{mr}` uses a monthly
  sequence bucket.
- A format without day or month tokens uses a yearly sequence bucket.

Do not use `{mmm}`. The current stored procedure checks it while deciding the
sequence scope but does not replace it in the generated number.

Legacy MX4 desktop formats must be converted before storing them in
`modlinfo`:

| MX4 desktop | OPH4 |
|---|---|
| `<0000>` | `{nnnn}` |
| `<00000>` | `{nnnnn}` |
| `<cm>` | `{mm}` |
| `<yy>` | `{yy}` |
| `<yyyy>` | `{yyyy}` |

Be aware that an MX4 source procedure may have maintained one sequence for a
whole year even when `<cm>` appeared in the rendered number. Directly mapping
`<cm>` to `{mm}` makes OPH4 reset that sequence monthly. Review and seed
`modldocn` accordingly when exact historical sequence continuity is required.

`dataFilter` values:

- `Selfcreation`: only current user's created rows.
- `SingleTransaction`: only one draft transaction is allowed; if one exists open it, otherwise create one.
- `Profile`: only rows related to current user/profile.
- `Account`: only rows related to account.
- `NewOnly`: can submit new data but cannot browse/edit existing data.

`profile` is the standard OPH self-service view contract across accounts:

- use the shared system `ModuleID=profile`, `settingMode=7`, `needLogin=1`, and
  `dataFilter=Profile`;
- it has no browse/list mode and resolves exactly one row whose `DocGUID` equals
  the authenticated session's `UserGUID`; a browser-supplied GUID is not an
  authorization source;
- its minimum source is `dbo.[user]` plus optional `dbo.userinfo`; each account
  may extend the view with domain data such as Falcon `MoMEMB`, but application
  code must not make that extension a global dependency;
- account-specific `modlcolm` must match the actual view columns and decides
  which biodata fields are editable; administrative fields remain read-only;
- joined-view writes must be routed by guarded profile lifecycle procedures or
  equivalent account-aware handlers to their authoritative tables;
- password change/reset is a dedicated authenticated action using the standard
  password procedure, never a profile view column or generic direct update.

## Permission Rules

Permissions must come from legacy permission logic, not frontend guesses.

Required behavior:

- Public access is controlled by `modl.needLogin = 0`.
- Authenticated access must use legacy auth functions, especially `gen.authModl`.
- User-group and workflow details live in `ugrp`, related mapping tables, and `modlappr`.
- Use `gen.authModl(hostguid, moduleGroupGuid)` to get `allowAccess`, `AllowAdd`, `allowedit`, `allowdelete`, `AllowExport`, and related capabilities.
- Legacy auth-function output shapes may differ by account. Any undeclared
  `Allow...` output is permission `0`, never an implicit allow; a function that
  does not return the module identity and `allowAccess` cannot authorize a module.
- Browse action buttons must use permission values:
  - show New only when `allowAdd > 0`
  - show Edit only when `allowEdit > 0`
  - show Delete only when `allowDelete > 0`
  - show Export/Import only when export/import permissions/config allow it
- Per-row actions and toolbar actions must both respect the same permissions.
- Deleting/restoring/wiping/executing/forcing/reopening must go through legacy function semantics.

Permission value meanings for transactions:

- `1`: requester only
- `3`: requester and approver
- `4`: requester, approver, and viewer

These values define which workflow actors may use the capability; they are not
document-status thresholds. A child transaction action uses the effective
status of its parent document when `ParentModuleGUID` and `parentDocGUID`
identify that parent. For example, `allowDelete=4` does not permit deleting a
draft-status child row whose parent transaction is already closed at status
`500`.

Form/child editability must be resolved from all of these inputs together:

- Document status/workflow level. Draft/requester, approval, and released statuses can change whether a field or row is editable.
- Module permission from `gen.authModl`, especially `allowAdd` for new rows/forms and `allowEdit` for existing rows/forms.
- User/group/workflow rights from the current `hostguid`; never infer edit rights only from frontend route state.
- Field metadata such as `isEditable`, `ROZone`, and `HideZone`.
- Save-preview result from `api.save` / `doc.[code]_save_preview`; preview XML may update field values and can mark fields hidden or read-only.

Practical rule: a form or child row should not become read-only merely because it is an existing/detail URL. If the current user has the required edit/add permission for the current status and the field is allowed by metadata and preview state, render it editable. Preview read-only/hidden responses are dynamic overrides and must be applied after each preview call.

## Workflow Rules

Workflow is configured through user groups and module approval.

- `ugrp` controls user inclusion/exclusion, module group inclusion/exclusion, module inclusion/exclusion, and capabilities.
- `modlappr` controls approval levels.
- Level `0` is requester.
- Levels `100-199` are approval levels.
- Levels `400-499` are released levels.
- Upper level defines the approval chain.
- SQL filters can limit which approval group applies.
- SQL filter aliases reserved by legacy behavior: `[code]` and `[codeaprv]`.

Conditional field visibility/editability:

- Use zoning A-Z on approval levels.
- `modlcolminfo.ROZone` makes fields read-only for selected zones.
- `modlcolminfo.HideZone` hides fields for selected zones.

## Column Rules

`modlcolm` defines module fields/columns. `modlcolminfo` defines field behavior.

Visibility/editability:

- Browse visibility uses `isBrowsable=1`.
- Form visibility uses `isViewable`.
- Form editability uses `isEditable`.
- Mandatory field uses `isNullable=0`.
- `isViewable` and `isEditable` support values `1,2,3,4,6,7,8`.

Meaning of `isViewable` / `isEditable` values:

- `1`: new and requester only
- `2`: new only
- `3`: requester and approver only
- `4`: requester, approver, and viewer
- `6`: requester only
- `7`: approver only
- `8`: viewer only

Migrated `isViewable=5` is retained as a general-visible compatibility mode.
Workflow visibility is filtered server-side before form fields are returned;
`isEditable` independently controls whether a visible field can be changed.

Special note:

- In child tables, value `2` means new condition for that child table itself. Other values follow the main parent document status.

Layout/order:

- Use `pageNo`, `sectionNo`, `columnNo`, `rowNo`, and `fieldNo` for form ordering.
- `sectionTitle` labels sections.
- `pageTitle` labels pages/tabs.
- `suffixCaption` displays helper text below a textbox.
- `colAlign` controls alignment: `0=left`, `1=center`, `2=right`.
- `colDigit` controls numeric precision.
- `colTitle` displays column title from the higher ordered metadata.

Primary fields:

- `primaryCol=1`: primary key.
- `primaryCol=2`: document date. (trx)
- `primaryCol=3`: document number. (trx)
- `primaryCol=4`: document reference number. (trx)
- `primaryCol=5`: ID. (master)
- `primaryCol=6`: description. (master)

Untuk primaryCol di commerce pake commerceCol saja.
- `commerceCol=5`: ID. (master)
- `commerceCol=6`: description. (master)
- `commerceCol=7`: price (commerce).
- `commerceCol=8`: unit name (commerce).
- `commerceCol=9`: image url (commerce).
- `commerceCol=10`: stock (commerce).

Untuk header checkout (toCASH):
- `commerceCol=21`: customer name
- `commerceCol=22`: customer phone
- `commerceCol=23`: customer email
- `commerceCol=24`: delivery address
- `commerceCol=25`: delivery method
- `commerceCol=26`: total weight
- `commerceCol=27`: subtotal
- `commerceCol=28`: delivery fee
- `commerceCol=x`: grand total -> penjumlahan saja
- `commerceCol=x`: notes -> tidak perlu

Untuk detail/item checkout (toCASHISKU):
- `commerceCol=x`: parent/header guid -> selalu pakai parentdocguid
- `commerceCol=31`: product/SKU guid
- `commerceCol=x`: product code -> ambil dari master product, misal skuguid_id
- `commerceCol=x`: product name -> ambil dari master product, misal skuguid_name
- `commerceCol=32`: unit guid
- `commerceCol=x`: unit name -> ambil dari combo, misal unitguid_name
- `commerceCol=33`: qty
- `commerceCol=34`: price
- `commerceCol=35`: weight
- `commerceCol=x`: line total (optional)
- `commerceCol=x`: image/reference optional -> ambil dari master product

Untuk detail/item checkout (toCASHPAYM):
- `commerceCol=x`: parent/header guid -> selalu pakai parentdocguid
- `commerceCol=41`: currencyguid
- `commerceCol=42`: amount
- `commerceCol=43`: charge
- `commerceCol=44`: Ref notes


## Field Type Rules

`modlcolminfo.colType` must drive editor rendering. `modlcolm.colType` remains
the physical SQL type identifier of the document-table column and must not be
overwritten with an editor type.

- `11`: text box
- `12`: password; `passwordChar` controls the masking character and defaults to `*`.
- `13`: rich text editor. `EditorSkin` defaults to `CKEditor` when blank.
- `14`: text area
- `15`: URL. `browseWidth` controls browse width in pixels and defaults to 200px.
- `19`: hidden box
- `20`: label/non-field. `html` contains the text and `class` controls the style/color.
- `21`: button/non-field. `fa` controls the icon, `class` the style/color,
  `js` the onclick handler, `preview` whether preview runs, and `script` the
  associated script.
- `31`: checkbox
- `32`: autosuggest
- `33`: token box
- `34`: radio
- `41`: date
- `42`: time
- `43`: date time
- `44`: month/year
- `45`: year
- `51`: media attachment
- `52`: profile image
- `53`: media image; use for view/form image fields, not browse image columns.
- `54`: vector signature
- `56`: get current location
- `57`: set current location

### Radio Button Metadata

Radio fields use a controller field plus optional dependent fields. The
metadata keys are stored in `modlcolminfo` and are case-insensitive.

- A radio controller has `modlcolminfo.colType=34`. Set `radioNo>0` only when
  the radio controls dependent-field visibility.
- A dependent field inside that radio group has the same `radioNo` and
  `radioOrder>0`. Its `radioOrder` must match the selected option `id`.
- `radioNo` replaces the legacy `viewRowType` metadata key.
- `radioOrder` replaces the legacy `viewRowTypeOrder`/`radioType` metadata key.
- The option list is stored in `radioOptions` as semicolon-separated options:
  `id,code,caption,readonly;id,code,caption,readonly`.
- `readonly` is optional and defaults to `0` when omitted.
- The equivalent legacy XML representation is:
  `<radioOptions><radioOption id="radioNo" fieldName="fieldname" caption="caption" /></radioOptions>`.
- In the CSV representation, `id` is the option identifier, `code` is the
  value written to the field, `caption` is the displayed label, and
  `readonly=1` marks that option read-only.
- Example options: `1,1,General,0;2,2,Closing,0;3,3,Automatic,1`.

The API/form renderer uses the controller's selected option to show the
dependent fields whose `radioNo` matches and whose `radioOrder` equals the
selected option `id`. A radio field without a positive `radioNo` can still be
rendered as a simple standalone radio input, but it does not control dependent
field visibility.

### Layout And Numeric Display

- `columnNo=1` places a field in the left column.
- `columnNo=2` places a field in the right column.
- `columnNo=0` makes a full-width field in a separate section.
- `colAlign=0|1|2` means left, center, or right alignment.
- `colDigit` controls displayed numeric precision: `1` means `0.0`, `2` means
  `0.00`, and so on.
- `colTitle` supplies a column title; when multiple title rows exist, the
  metadata row with the higher order takes precedence.
- `sectionTitle` supplies a section heading and `pageTitle` supplies a page
  or tab heading.
- `suffixCaption` displays helper text below a textbox.

Profile image rule:

- `colType=52` should be on page 10, section 1, col 1, row 1. Do not put another field in the same slot.

## Autosuggest And Combo Rules

Autosuggest uses `colType=32`.

Core settings:

- `ComboTable`
- `ComboFieldKey`
- `ComboFieldId`
- `ComboFieldName`
- `ComboWhereField1`
- `ComboWhereField2`
- `ComboWhereField1Mandatory`
- `ComboWhereField2Mandatory`
- `comboFieldSearch`
- `ComboFieldDesc`/`comboFieldDesc` may be used by legacy combo sources that
  expose a separate description field.

Supported `ComboTable` forms:

- table name
- `par(code)`
- `core(code)`
- `doc(code)`
- `Active(code, searchText)`
- `Released(code, searchText)`
- `View(code)` where view resolves as `<parentCode>_<code>`
- `Script(script)` is proposed legacy behavior

Legacy combo source forms:

- `Par(code)` reads parameter values; its key/id/description may use
  `ParameterValueGUID`, `ParameterValue`, `ParameterDescription`, or the
  corresponding legacy parameter aliases.
- `Active(code, searchText)` returns active documents only.
- `Released(code, searchText)` returns released documents only.
- `[code]` returns active/released documents using the default document
  source behavior.
- `View(code)` resolves the generated view convention for the module.
- `Script(script)` is a compatibility proposal and must not be assumed to be
  implemented by the API unless the account explicitly provides it.

Rules:

- For document combos, default key is `DocGUID` if no `ComboFieldKey` is configured.
- If `ComboFieldId` and `ComboFieldName` are both configured and both row
  values are non-empty, the display caption is `id - name` (one space on each
  side of the hyphen). If either value is empty, show only the non-empty value
  without a dangling separator.
- `#combotable#` can be used in `ComboFieldId`/`ComboFieldName` expressions.
- `ComboWhereField1` and `ComboWhereField2` combine as `AND` filters.
- In metadata-driven forms, each `ComboWhereField*` name also identifies the
  sibling form field whose current value is sent as `wf1value`/`wf2value`; the
  combo source must expose a filter column with that same name. Changing the
  sibling value must refresh the dependent combo options.
- If where-field mandatory flags are `0`, empty/null values make the condition optional.
- `comboFieldSearch` participates in search but should not necessarily display in option labels.

Token box uses `colType=33` and follows similar combo source rules.

For token boxes, table/document sources use the same `ComboTable`,
`ComboFieldKey`, `ComboFieldId`, and `ComboFieldName` settings. Parameter token
boxes commonly map the key to `ParameterValueGUID`, the id to `ParameterValue`,
and the description to `ParameterDescription`.

## Formula Rules

Formula metadata defines a derived field calculated from other columns. A
simple expression may reference sibling fields, for example `(qty*price)`.
Legacy formulas may also use a correlated subquery, for example:

```sql
(select sum(amount)
 from doc.ToCASHISKU x
 where x.parentdocguid=doc.ToCASHISKU.docguid)
```

Formula execution and writable/read-only behavior must remain server-owned;
the frontend must not calculate or accept a client-supplied derived value as
authoritative. The old `isParentKey` column flag is obsolete; parent/child
relations use `modlinfo.childKey` and `modlinfo.parentKey`.

## Browse Rules

- Browse columns must come from `modlcolm + modlcolminfo` where `isBrowsable > 0`.
- Browse labels should use `titlecaption`/column title metadata.
- Browse order should use `modlcolm.colOrder`, then stable fallback.
- Browse default sorting should use `modlinfo.orderField` if present.
- If no `orderField`, legacy default is generally updated date descending.
- `browserows` controls default page size when present.
- Status filter options must come from the module's status configuration, not hardcoded lists.
- Combo filter appears in browse for `colType=32` when `ComboFilter=1`.
- Summary column visibility is controlled by `allowShowSummaryColumn`.
- Row styles may come from `View_browse_attr`, which returns `docguid`, `fieldname`, and `style`.
- Legacy extra row actions come from `modlinfo.InfoKey=buttons`. Apply them to
  both root browse rows and embedded child-browse rows, honor each button's
  comma-separated `state` list, and resolve `%rid%` against the clicked row.
  For an embedded child report this is the child row acting as the report's
  parent record, not the containing root form. Legacy report input
  `cid=%rid%` aliases `cid` to canonical parameter `GUID` when the report
  DPLX/procedure declares `@GUID`.

## Form Rules

- Form fields must come from `modlcolm + modlcolminfo`.
- Form layout must follow `pageNo`, `sectionNo`, `columnNo`, `rowNo`, and `fieldNo`.
- `thmepage.pageURL` determines the form model.
- `tab` form supports pages 1-9 as tabs.
- Page 10 is profile/summary panel.
- Child browses should also appear as tabs in tab-form mode.
- `isViewable` controls whether a field is shown.
- `isEditable` controls whether a field is editable.
- `isNullable=0` means required.
- New GUID (`00000000-0000-0000-0000-000000000000`) should render the same metadata layout as detail/view, but editable fields must be input-capable.
- Existing detail/view must be editable when the current user, document status, module permissions, field metadata, and preview state allow editing.

## Child, Descendant, And Parent Rules

- Submodules need `Parent Module`.
- `modlinfo.PageNo` and `SectionNo` can locate children/descendants inside a page/section.
- `childKey` controls the child field used to relate to the parent. Default is `parentDocGUID`.
- `parentKey` controls the parent field used for relation. Default is `docGUID`.
- Child browse modes include:
  - `detail`: detail info
  - `inline`: inline edit in browse
  - `cascade`: first detail module displayed read-only, proposed
- Child browse mode can depend on theme page with format `[page]_child[browsemode]`.
- Export/import nested mode requires child/grandchild export permission too.

## Report Rules

- Reports use `modl.settingMode=5`.
- Report pages should not be treated like ordinary document browse.
- Report configuration comes from `modl`, `modlcolm`, `modlcolminfo`, and `modlinfo`.
- Report parameter fields are module columns.
- Report parameter labels come from column title/caption metadata.
- Report parameter fields should have `isViewable=1` and `isBrowsable=1`.
- For document print reports, use parameter `GUID uniqueidentifier`.
- `modlinfo.STATEID` limits report/print output to specific document states.
- `STATEID` must be enforced by the report-generation API against the referenced
  parent document, not only by disabling the frontend button. A report with
  `STATEID='100','400','500'` must reject a direct generation request for a
  draft document at status `0`.

PDF settings:

- `allowPDF=1`
- `ReportName`: DPLX/report file name without extension.
- `DPLX_<variable>` can hold DPLX XML content.
- `QuerySQL_<number>` or `QuerySQL_<name>` backs report output.

XLS settings:

- `allowXLS=1`: normal XLS.
- `allowXLS=2`: template mode.
- `XLSTitleName`: generated report title/name.
- `XLSTemplate`: template file in report folder.
- `Sheet_<name>`, `Cell_<name>`, and `PivotName` are proposed/advanced XLS behavior.

Scripting:

- `Script_<name>` can create the report stored procedure.
- Report query stored procedures should use `doc.<name>` convention.

## Import And Export Rules

- Export/import module uses normal module settings plus `modlinfo.allowExport=1`.
- `ExportMode=0`: simple table.
- `ExportMode=1`: nested table including children/grandchildren.
- Children/grandchildren must also have `allowExport=1`.
- Exported columns should be `(isViewable=1 and isEditable=1)` or upload/download fields.

## Menu Rules

- THEMEONE has `primaryback` and `sidebar`.
- `primaryback` opens from top-right avatar menu.
- `sidebar` appears on the left.
- Menu item types:
  - `treeview`: parent node that can contain submenu.
  - `label`: opens in same tab.
  - `target`: opens new tab.
- Hierarchy uses `upperSubmenuGUID`.
- Internal URLs may use legacy query format like `?code=xxxx`.
- External URLs can start with `https://`.
- Menu visibility should respect module access.

## Theme Rules

- Default theme is THEMEONE.
- Theme can name all pages it wants to use.
- `tab` supports browse and form modes, multi-page forms, page 10 profile/summary, and mixed fields plus children browse.
- `master` is marked as intended to be replaced by tab. `profile` remains the
  dedicated no-browse self-service view shell defined above.
- Login supports login, signup/create account, choose account, forgot password, verify code, reset password, and proposed social login.
- Report theme supports report history.

## Mail And Notification Rules

- Notification support is mail-based.
- Mail profile must exist in Database Mail and mail module/table.

### Account-Aware API Email

- Node/API email settings may be stored per account in server-only `acctinfo`
  keys prefixed `EMAIL_`; these values must never be returned by bootstrap or
  another public API.
- Supported keys are `EMAIL_ENABLED`, `EMAIL_FROM_ADDRESS`, `EMAIL_FROM_NAME`,
  `EMAIL_REPLY_TO`, `EMAIL_SMTP_HOST`, `EMAIL_SMTP_PORT`, `EMAIL_SMTP_SECURE`,
  `EMAIL_SMTP_USER`, `EMAIL_SMTP_PASS`, `EMAIL_SMTP_IGNORE_TLS_ERRORS`, and
  `EMAIL_TO_ADDRESS`.
- Account `acctinfo` takes precedence over legacy process-level `SMTP_*` /
  `MAIL_*` variables. Environment settings remain a compatibility fallback.
- SMTP authentication belongs in the API process. Do not expose SMTP user or
  password through frontend metadata, logs, error payloads, or tracked loaders.
- Module mail setup chooses action, status, recipients, CC/BCC, and template.
- `sp_mail_before` can run before sending.
- `sp_mail_before` parameters include `@hostGUID`, `@GUID`, and cancel flag.
- Custom mail action uses action `EMAIL`.
- Custom mail can call `gen.mail_creator @hostGUID, @code, @GUID, @action`.
- Mail actions/statuses use parameters:
  - `MACT`: `DELETE`, `EXECUTE`, `FORCE`, `REOPEN`, `SAVE`, `WIPE`, `EMAIL`
  - `MLST`: `0 Draft`, `100 On Approval`, `300 Rejected`, `400 Released`, `500 Force`, `999 Deleted`
- Template fields can use `#fieldname#` syntax.

## Script Rules

Custom lifecycle procedures use `doc.[code]_<event>` naming:

- `doc.[code]_save_before`
- `doc.[code]_save_after`
- `doc.[code]_save_preview`
- `doc.[code]_delete_before`
- `doc.[code]_delete_after`
- `doc.[code]_execute_before`
- `doc.[code]_execute_after`
- `doc.[code]_force_before`
- `doc.[code]_force_after`

Save-before shape:

- `@hostGUID uniqueidentifier`
- `@GUID uniqueidentifier`
- `@parentKey uniqueidentifier`
- all editable fields as optional parameters
- `@msg nvarchar(max) output`

Save-after shape:

- `@hostGUID uniqueidentifier`
- `@guid uniqueidentifier`
- `@parentKey uniqueidentifier`
- `@isupdate bit`
- `@reload bit=0 output`

Save-preview shape:

- `@hostGUID uniqueidentifier`
- `@GUID uniqueidentifier`
- `@parentKey uniqueidentifier`
- editable fields
- `@preview int`
- result is XML fragment without wrapping `<message>`.

`@reload` can force browse/list refresh after operations.

Draft save ownership:

- A draft document (`Status >= 0 AND Status < 100`) is writable by its
  `CreatedUser`.
- A different authenticated user may save, preview, submit, or run a document
  action only when the effective module permission grants `allowForce > 0`.
  `allowEdit` alone does not bypass draft ownership.

## Widget And Dashboard Rules

- Widgets are created through module `widg`.
- Widget IDs must be unique.
- Widget SQL can live in `sqlstr` or module info procedure script.
- `smallBox` returns XML with one `<result>`.
- `graphBox` returns XML with chart type, labels, and datasets.
- Dashboard is a module using dashboard theme.
- Widget columns need `isWidget=1`, `widgetType`, and `widgetNo`.
- Widget layout uses page/section/column/row/field metadata.
- `ColWidth` should be 1-12, default 12.
- Widget procedures should follow `doc.<module>_<name> @hostGUID uniqueidentifier, @sqlfilter nvarchar(max)`.

## Migration Rules

Migration/database structure is driven from `oph_core`.

- Create/copy structure using `core.createdb`.
- Duplicate by inserting account metadata and setting `migrateAccount`.
- Copy structure from another DB by setting:
  - `acctinfo.copyFrom`
  - optional `acctinfo.copyExclude` using `*` delimiter
  - run `core.copyModule [account]`

`acctdbse` migration settings:

- `DatabaseName`: source DB/MDF name.
- `ServerName`: source server, blank for same server.
- `isMaster`: `1` for main database, `0` for child DBs.
- `Version`: source version (`2.0`, `2.1`, `3.0`).
- `MigrateDB`: destination v4 database.

Migration commands:

- `exec core.createdb @accountid, @ismigrate=1`
- `exec core.migratedata @accountid`

## Legacy API Rules

Theme API legacy format:

```text
(url)/ophcore/api/default.aspx?mode=(mode)&code=(code)[&guid=(guid)][&no=(no)][&refno=(refno)][&id=(id)][&hostguid=(hostguid)]
```

Modes:

- `signin`: form fields `userid`, `pwd`
- `account`: account/module/theme/user context
- `browse`: `sqlFilter`, `sortOrder`, `stateId`, `bPageNo`, `bSearchText`
- `view/form`: no extra parameter
- `save` and preview: all form fields, `flag=0` for save, other flags for preview
- `function`: delete, restore, wipe, execute, force, reopen, custom
- `signout`: no parameter

Function parameters:

- `cfunction`
- `cfunctionList`
- `approvaluserguid`
- `pwd`

Autosuggest API format:

```text
(url)/ophcore/api/msg_autosuggest.aspx?code=(code)&guid=(guid)&colkey=(colkey)&hostguid=(hostguid)
```

Autosuggest parameters:

- `wf1value`
- `wf2value`
- `nbRow`, default 20
- `search` or `q`
- `defaultValue`
- `page`, default 1

Download API format:

```text
(url)/ophcore/api/msg_download.aspx?code=(code)&guid=(guid)&fieldAttachment=(fieldName)&hostguid=(hostguid)
(url)/ophcore/api/msg_download.aspx?imageName=(imageName)&hostguid=(hostguid)
```

## OPH5 Implementation Checklist

Use this checklist before implementing or changing browse/form/report behavior:

- Does route resolution use module code as source of truth?
- Does env remain route/menu context for explicit URLs?
- Does access use `gen.authModl` or equivalent legacy function?
- Are action buttons hidden/shown from permission values?
- Are browse columns taken from `isBrowsable` metadata?
- Are form fields taken from `isViewable` metadata?
- Are editable inputs driven by `isEditable` metadata?
- Are page/section/row/field layout values respected?
- Is tab form page 10 treated as summary/profile?
- Are children/descendants using parent/child key metadata?
- Is report mode 5 using report metadata and not ordinary browse?
- Are autosuggest/combo values resolved from combo metadata?
- Are save/delete/execute/force operations routed through legacy function semantics?

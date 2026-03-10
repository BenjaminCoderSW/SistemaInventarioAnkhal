<%@ Page Title="Usuarios" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="Usuarios.aspx.cs" Inherits="GrupoAnkhalInventario.Usuarios" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="css/gridviewPantalla.css" rel="stylesheet" />
    <style>
        /* ── Credencial ──────────────────────────────────────── */
        .credencial-card {
            width: 340px; border-radius: 10px;
            border: 3px solid #ff6a00; background: #fff;
            overflow: hidden; box-shadow: 0 8px 18px rgba(0,0,0,.25);
            font-family: 'Segoe UI', Arial, sans-serif;
        }
        .credencial-header {
            background: #111; color: #fff;
            padding: 8px 12px; display: flex; align-items: center;
        }
        .credencial-header-bar { width:8px;height:36px;border-radius:4px;background:#ff6a00;flex-shrink:0; }
        .credencial-logo-text  { margin-left:10px; }
        .credencial-logo-text .empresa  { font-weight:700;font-size:15px;color:#ff6a00; }
        .credencial-logo-text .subtitulo{ font-size:11px;color:#fff;opacity:.9; }
        .credencial-body  { padding:10px 14px;font-size:13px;color:#111; }
        .credencial-body h5 { font-size:15px;font-weight:600;margin-bottom:4px; }
        .credencial-datos { display:flex;justify-content:space-between;align-items:center;margin-top:6px; }
        .credencial-datos p { margin-bottom:3px; }
        .credencial-footer { background:#f5f5f5;border-top:1px solid #e0e0e0;padding:5px 12px;font-size:11px;text-align:center;color:#555; }
        /* ── Filtros y paginador ─────────────────────────────── */
        .filtros-bar {
            background: #f8f9fa; border: 1px solid #dee2e6;
            border-radius: 8px; padding: 12px 16px; margin-bottom: 14px;
        }
        .filtros-bar label { font-weight:600; font-size:.84rem; color:#003366; margin-bottom:2px; }
        .pager-custom span { background:#003366;color:#fff;font-weight:700;border-radius:4px;padding:4px 9px; }
        .pager-custom a    { padding:4px 9px;border-radius:4px; }
        /* ── Panel info empleado ─────────────────────────────── */
        .panel-empleado {
            background: #eef3fa; border: 1px solid #c3d3e8;
            border-radius: 8px; padding: 14px 16px; margin-bottom: 14px;
        }
        .emp-dato { font-size: .88rem; margin-bottom: 3px; }
        .emp-dato strong { color: #003366; }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <asp:HiddenField ID="hdnMensajePendiente"    runat="server" />
    <asp:HiddenField ID="hdnReabrirModalAgregar" runat="server" Value="0" />

    <div class="d-flex justify-content-between align-items-center mb-3">
        <h2 class="mb-0"><i class="fas fa-users mr-2"></i>Gestión de Usuarios</h2>
        <asp:Button ID="btnAbrirAgregar" runat="server" Text="+ Nuevo Usuario"
            CssClass="btn btn-primary"
            OnClientClick="abrirModalAgregar(); return false;" />
    </div>

    <%-- Barra de filtros --%>
    <div class="filtros-bar">
        <div class="row align-items-end">
            <div class="col-md-5">
                <label>Buscar por nombre, usuario o N° empleado</label>
                <asp:TextBox ID="txtBuscar" runat="server" CssClass="form-control form-control-sm"
                    Placeholder="Escribe para buscar..."></asp:TextBox>
            </div>
            <div class="col-md-3">
                <label>Estado</label>
                <asp:DropDownList ID="ddlFiltrEstado" runat="server" CssClass="form-control form-control-sm">
                    <asp:ListItem Value="">-- Todos --</asp:ListItem>
                    <asp:ListItem Value="1">Activo</asp:ListItem>
                    <asp:ListItem Value="0">Inactivo</asp:ListItem>
                </asp:DropDownList>
            </div>
            <div class="col-md-4 mt-1">
                <asp:Button ID="btnBuscar" runat="server" Text="🔍 Buscar"
                    CssClass="btn btn-primary btn-sm mr-1" OnClick="btnBuscar_Click" />
                <asp:Button ID="btnLimpiar" runat="server" Text="✖ Limpiar"
                    CssClass="btn btn-secondary btn-sm" OnClick="btnLimpiar_Click" />
            </div>
        </div>
    </div>

    <div class="mb-2">
        <small class="text-muted"><asp:Label ID="lblResultados" runat="server"></asp:Label></small>
    </div>

    <%-- GridView --%>
    <div class="table-responsive">
        <asp:GridView ID="gvUsuarios" runat="server"
            AutoGenerateColumns="False"
            CssClass="table table-bordered table-striped table-hover custom-grid"
            AllowPaging="True" AllowCustomPaging="True" PageSize="15"
            OnPageIndexChanging="gvUsuarios_PageIndexChanging"
            PagerStyle-CssClass="pager-custom"
            PagerSettings-Mode="NumericFirstLast"
            PagerSettings-FirstPageText="«"
            PagerSettings-LastPageText="»"
            PagerSettings-PageButtonCount="5">
            <Columns>
                <asp:TemplateField HeaderText="Foto">
                    <ItemTemplate>
                        <asp:Image ID="imgFotoGrid" runat="server"
                            ImageUrl='<%# ObtenerFotoBase64(Eval("Foto")) %>'
                            Width="42" Height="42"
                            CssClass="rounded-circle border"
                            style="object-fit:cover;" />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="NombreCompleto" HeaderText="Nombre" />
                <asp:BoundField DataField="NumeroEmpleado" HeaderText="N° Empleado" />
                <asp:BoundField DataField="Usuario"        HeaderText="Usuario" />
                <asp:BoundField DataField="Rol"            HeaderText="Rol" />
                <asp:BoundField DataField="Email"          HeaderText="Email" />
                <asp:BoundField DataField="Telefono"       HeaderText="Teléfono" />
                <asp:TemplateField HeaderText="Estado">
                    <ItemTemplate>
                        <span class='badge <%# (bool)Eval("Activo") ? "badge-success" : "badge-secondary" %>'>
                            <%# (bool)Eval("Activo") ? "Activo" : "Inactivo" %>
                        </span>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Acciones">
                    <ItemTemplate>
                        <button type="button" class="btn btn-info btn-sm" title="Ver credencial"
                            onclick="abrirModalCredencial(
                                '<%# Eval("Nombre") %>',
                                '<%# Eval("ApellidoPaterno") %>',
                                '<%# Eval("ApellidoMaterno") %>',
                                '<%# Eval("NumeroEmpleado") %>',
                                '<%# Eval("Rol") %>')">
                            <i class="fas fa-id-card"></i>
                        </button>
                        <button type="button" class="btn btn-warning btn-sm" title="Editar"
                            onclick="abrirModalEditar(
                                '<%# Eval("ClaveID") %>',
                                '<%# Eval("RolID") %>',
                                '<%# Server.HtmlEncode((Eval("Nombre")           ?? "").ToString()) %>',
                                '<%# Server.HtmlEncode((Eval("ApellidoPaterno")  ?? "").ToString()) %>',
                                '<%# Server.HtmlEncode((Eval("ApellidoMaterno")  ?? "").ToString()) %>',
                                '<%# Server.HtmlEncode((Eval("NumeroEmpleado")   ?? "").ToString()) %>',
                                '<%# Server.HtmlEncode((Eval("Telefono")         ?? "").ToString()) %>',
                                '<%# Server.HtmlEncode((Eval("TelefonoFamiliar") ?? "").ToString()) %>',
                                '<%# Server.HtmlEncode((Eval("Email")            ?? "").ToString()) %>',
                                '<%# Server.HtmlEncode((Eval("Usuario")          ?? "").ToString()) %>')">
                            <i class="fas fa-edit"></i> Editar
                        </button>
                        <asp:Button runat="server"
                            CssClass='<%# (bool)Eval("Activo") ? "btn btn-danger btn-sm" : "btn btn-success btn-sm" %>'
                            Text='<%# (bool)Eval("Activo") ? "Desactivar" : "Activar" %>'
                            CommandArgument='<%# Eval("ClaveID") %>'
                            OnClick="btnToggleActivo_Click"
                            OnClientClick='<%# ConfirmarToggleJS((bool)Eval("Activo"),
                                (Eval("Nombre") ?? "").ToString() + " " + (Eval("ApellidoPaterno") ?? "").ToString()) %>' />
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </div>


    <%-- ══════════════════════════════════════════════════════
         MODAL AGREGAR
    ══════════════════════════════════════════════════════════ --%>
    <div class="modal fade" id="modalAgregar" tabindex="-1" role="dialog" data-backdrop="static">
        <div class="modal-dialog modal-lg" role="document">
            <div class="modal-content shadow-lg border-0">
                <div class="modal-header text-white" style="background:#003366;">
                    <h5 class="modal-title"><i class="fas fa-user-plus mr-2"></i>Nuevo Usuario de Inventario</h5>
                    <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
                </div>
                <div class="modal-body">

                    <%-- Paso 1: elegir empleado --%>
                    <h6 class="text-primary font-weight-bold border-bottom pb-1">
                        <i class="fas fa-search mr-1"></i> Paso 1 — Seleccionar empleado de asistencia
                    </h6>
                    <div class="row">
                        <div class="col-md-8 form-group">
                            <label>Empleado <span class="text-danger">*</span>
                                <small class="text-muted font-weight-normal">(solo activos sin cuenta en inventario)</small>
                            </label>
                            <asp:DropDownList ID="ddlEmpleado" runat="server"
                                CssClass="form-control" AutoPostBack="true"
                                OnSelectedIndexChanged="ddlEmpleado_SelectedIndexChanged">
                            </asp:DropDownList>
                        </div>
                    </div>

                    <%-- Panel info del empleado --%>
                    <asp:Panel ID="divInfoEmpleado" runat="server" Visible="false">
                        <div class="panel-empleado">
                            <div class="row align-items-center">
                                <div class="col-auto">
                                    <asp:Image ID="imgFotoEmpleado" runat="server"
                                        Width="70" Height="70" CssClass="rounded-circle border"
                                        style="object-fit:cover;"
                                        ImageUrl="dist/img/user2-160x160.jpg" />
                                </div>
                                <div class="col">
                                    <div class="row">
                                        <div class="col-md-6">
                                            <p class="emp-dato"><strong>Nombre:</strong> <asp:Label ID="lblNombreEmp"  runat="server" /></p>
                                            <p class="emp-dato"><strong>N° Empleado:</strong> <asp:Label ID="lblNumEmpEmp" runat="server" /></p>
                                        </div>
                                        <div class="col-md-6">
                                            <p class="emp-dato"><strong>Teléfono:</strong> <asp:Label ID="lblTelEmp"     runat="server" /></p>
                                            <p class="emp-dato"><strong>Tel. Familiar:</strong> <asp:Label ID="lblTelFamEmp" runat="server" /></p>
                                        </div>
                                        <div class="col-12">
                                            <p class="emp-dato mb-0"><strong>Email:</strong> <asp:Label ID="lblEmailEmp" runat="server" /></p>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </asp:Panel>

                    <%-- Paso 2: datos de acceso --%>
                    <h6 class="text-primary font-weight-bold border-bottom pb-1 mt-2">
                        <i class="fas fa-lock mr-1"></i> Paso 2 — Datos de acceso al inventario
                    </h6>
                    <div class="row">
                        <div class="col-md-4 form-group">
                            <label>Rol <span class="text-danger">*</span></label>
                            <asp:DropDownList ID="ddlRolAgregar" runat="server" CssClass="form-control"></asp:DropDownList>
                        </div>
                        <div class="col-md-4 form-group">
                            <label>Usuario de acceso <span class="text-danger">*</span></label>
                            <asp:TextBox ID="txtUsuarioAgregar" runat="server" CssClass="form-control"
                                Placeholder="Nombre de usuario" autocomplete="off" />
                        </div>
                        <div class="col-md-4 form-group">
                            <label>Contraseña <span class="text-danger">*</span></label>
                            <div class="input-group">
                                <asp:TextBox ID="txtClaveAgregar" runat="server" CssClass="form-control"
                                    TextMode="Password" Placeholder="Mínimo 6 caracteres" autocomplete="new-password" />
                                <div class="input-group-append">
                                    <button class="btn btn-outline-secondary" type="button"
                                        onclick="togglePwd('<%=txtClaveAgregar.ClientID%>',this)">
                                        <i class="fas fa-eye"></i>
                                    </button>
                                </div>
                            </div>
                        </div>
                        <div class="col-md-4 form-group">
                            <label>Confirmar contraseña <span class="text-danger">*</span></label>
                            <div class="input-group">
                                <asp:TextBox ID="txtClaveConfirmarAgregar" runat="server" CssClass="form-control"
                                    TextMode="Password" Placeholder="Repite la contraseña" autocomplete="new-password" />
                                <div class="input-group-append">
                                    <button class="btn btn-outline-secondary" type="button"
                                        onclick="togglePwd('<%=txtClaveConfirmarAgregar.ClientID%>',this)">
                                        <i class="fas fa-eye"></i>
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>

                </div>
                <div class="modal-footer bg-light">
                    <asp:Button ID="btnGuardarAgregar" runat="server" Text="Guardar Usuario"
                        CssClass="btn btn-success px-4" OnClick="btnGuardarAgregar_Click" />
                    <button type="button" class="btn btn-secondary px-4" data-dismiss="modal">Cancelar</button>
                </div>
            </div>
        </div>
    </div>


    <%-- ══════════════════════════════════════════════════════
         MODAL EDITAR
    ══════════════════════════════════════════════════════════ --%>
    <div class="modal fade" id="modalEditar" tabindex="-1" role="dialog" data-backdrop="static">
        <div class="modal-dialog modal-lg" role="document">
            <div class="modal-content shadow-lg border-0">
                <div class="modal-header text-white" style="background:#003366;">
                    <h5 class="modal-title"><i class="fas fa-user-edit mr-2"></i>Editar Usuario</h5>
                    <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
                </div>
                <div class="modal-body">
                    <asp:HiddenField ID="hfClaveID" runat="server" />

                    <div class="callout callout-info mb-3" style="font-size:.85rem;">
                        <i class="fas fa-info-circle"></i>
                        Los datos personales se administran en el sistema de asistencia y son de solo lectura aquí.
                    </div>

                    <%-- Todos los datos de asistencia: solo lectura --%>
                    <div class="row">
                        <div class="col-md-5 form-group">
                            <label class="text-muted">Nombre completo</label>
                            <asp:TextBox ID="txtNombreEditarRO"      runat="server" CssClass="form-control" ReadOnly="true" />
                        </div>
                        <div class="col-md-2 form-group">
                            <label class="text-muted">N° Empleado</label>
                            <asp:TextBox ID="txtNumEmpEditarRO"      runat="server" CssClass="form-control" ReadOnly="true" />
                        </div>
                        <div class="col-md-2 form-group">
                            <label class="text-muted">Teléfono</label>
                            <asp:TextBox ID="txtTelEditarRO"         runat="server" CssClass="form-control" ReadOnly="true" />
                        </div>
                        <div class="col-md-3 form-group">
                            <label class="text-muted">Tel. Familiar</label>
                            <asp:TextBox ID="txtTelFamiliarEditarRO" runat="server" CssClass="form-control" ReadOnly="true" />
                        </div>
                        <div class="col-md-6 form-group">
                            <label class="text-muted">Email</label>
                            <asp:TextBox ID="txtEmailEditarRO"       runat="server" CssClass="form-control" ReadOnly="true" />
                        </div>
                    </div>

                    <hr />

                    <%-- Solo Rol, Usuario y contraseña son editables --%>
                    <div class="row">
                        <div class="col-md-4 form-group">
                            <label>Rol <span class="text-danger">*</span></label>
                            <asp:DropDownList ID="ddlRolEditar" runat="server" CssClass="form-control"></asp:DropDownList>
                        </div>
                        <div class="col-md-4 form-group">
                            <label>Usuario de acceso <span class="text-danger">*</span></label>
                            <asp:TextBox ID="txtUsuarioEditar" runat="server" CssClass="form-control" autocomplete="off" />
                        </div>

                        <div class="col-12 mb-1 mt-2">
                            <h6 class="text-muted font-weight-bold border-bottom pb-1">
                                <i class="fas fa-key mr-1"></i> Cambiar contraseña
                                <small class="font-weight-normal">(dejar vacío para no cambiarla)</small>
                            </h6>
                        </div>
                        <div class="col-md-4 form-group">
                            <label>Nueva contraseña</label>
                            <div class="input-group">
                                <asp:TextBox ID="txtNuevaClaveEditar" runat="server" CssClass="form-control"
                                    TextMode="Password" Placeholder="Vacío = sin cambio" autocomplete="new-password" />
                                <div class="input-group-append">
                                    <button class="btn btn-outline-secondary" type="button"
                                        onclick="togglePwd('<%=txtNuevaClaveEditar.ClientID%>',this)">
                                        <i class="fas fa-eye"></i>
                                    </button>
                                </div>
                            </div>
                        </div>
                        <div class="col-md-4 form-group">
                            <label>Confirmar nueva contraseña</label>
                            <div class="input-group">
                                <asp:TextBox ID="txtConfirmarClaveEditar" runat="server" CssClass="form-control"
                                    TextMode="Password" Placeholder="Repite la contraseña" autocomplete="new-password" />
                                <div class="input-group-append">
                                    <button class="btn btn-outline-secondary" type="button"
                                        onclick="togglePwd('<%=txtConfirmarClaveEditar.ClientID%>',this)">
                                        <i class="fas fa-eye"></i>
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="modal-footer">
                    <asp:Button ID="btnGuardarEditar" runat="server" Text="Guardar Cambios"
                        CssClass="btn btn-success" OnClick="btnGuardarEditar_Click" />
                    <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancelar</button>
                </div>
            </div>
        </div>
    </div>


    <%-- ══════════════════════════════════════════════════════
         MODAL CREDENCIAL
    ══════════════════════════════════════════════════════════ --%>
    <div class="modal fade" id="modalCredencial" tabindex="-1" role="dialog">
        <div class="modal-dialog" role="document">
            <div class="modal-content">
                <div class="modal-header text-white" style="background:#003366;">
                    <h5 class="modal-title">Credencial de Empleado</h5>
                    <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
                </div>
                <div class="modal-body d-flex justify-content-center">
                    <div class="credencial-card" id="credencialEmpleado">
                        <div class="credencial-header">
                            <div class="credencial-header-bar"></div>
                            <div class="credencial-logo-text">
                                <div class="empresa"><img src="img/ankhal.png" width="22" alt="" /> GRUPO ANKHAL</div>
                                <div class="subtitulo">Credencial de empleado</div>
                            </div>
                        </div>
                        <div class="credencial-body">
                            <h5 id="cred_nombre"></h5>
                            <div class="credencial-datos">
                                <div>
                                    <p><strong>N° Empleado:</strong><br /><span id="cred_numero"></span></p>
                                    <p><strong>Rol:</strong><br /><span id="cred_area"></span></p>
                                </div>
                                <div id="cred_qr"></div>
                            </div>
                        </div>
                        <div class="credencial-footer">Uso interno · Grupo Ankhal</div>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-primary" onclick="imprimirCredencial()">
                        <i class="fas fa-print mr-1"></i> Imprimir
                    </button>
                    <button type="button" class="btn btn-secondary" data-dismiss="modal">Cerrar</button>
                </div>
            </div>
        </div>
    </div>

    <script src="https://cdnjs.cloudflare.com/ajax/libs/qrcodejs/1.0.0/qrcode.min.js"></script>
    <script type="text/javascript">

        window.addEventListener('load', function () {
            var hdn = document.getElementById('<%= hdnMensajePendiente.ClientID %>');
            if (hdn && hdn.value) {
                try {
                    var d = JSON.parse(hdn.value);
                    var opts = { icon: d.icon, title: d.title, text: d.text, confirmButtonColor: '#003366' };
                    if (d.icon === 'success') { opts.showConfirmButton = false; opts.timer = 2000; }
                    Swal.fire(opts);
                    hdn.value = '';
                } catch (e) { }
            }
            var hdnReabrir = document.getElementById('<%= hdnReabrirModalAgregar.ClientID %>');
            if (hdnReabrir && hdnReabrir.value === '1') {
                hdnReabrir.value = '0';
                $('#modalAgregar').modal('show');
            }
        });

        function abrirModalAgregar() { $('#modalAgregar').modal('show'); }

        function togglePwd(id, btn) {
            var i = document.getElementById(id);
            var ic = btn.querySelector('i');
            i.type = (i.type === 'password') ? 'text' : 'password';
            ic.classList.toggle('fa-eye');
            ic.classList.toggle('fa-eye-slash');
        }

        // Orden parámetros: claveID, rolId, nombre, apPat, apMat, numEmp, tel, telFam, email, usuario
        function abrirModalEditar(claveID, rolId, nombre, apPat, apMat, numEmp, tel, telFam, email, usuario) {
            document.getElementById('<%= hfClaveID.ClientID %>').value                 = claveID;
            document.getElementById('<%= ddlRolEditar.ClientID %>').value              = rolId;
            document.getElementById('<%= txtNombreEditarRO.ClientID %>').value         = (nombre + ' ' + apPat + ' ' + apMat).trim();
            document.getElementById('<%= txtNumEmpEditarRO.ClientID %>').value         = numEmp;
            document.getElementById('<%= txtTelEditarRO.ClientID %>').value            = tel;
            document.getElementById('<%= txtTelFamiliarEditarRO.ClientID %>').value    = telFam;
            document.getElementById('<%= txtEmailEditarRO.ClientID %>').value          = email;
            document.getElementById('<%= txtUsuarioEditar.ClientID %>').value = usuario;
            document.getElementById('<%= txtNuevaClaveEditar.ClientID %>').value = '';
            document.getElementById('<%= txtConfirmarClaveEditar.ClientID %>').value = '';
            $('#modalEditar').modal('show');
        }

        function abrirModalCredencial(nombre, apPat, apMat, numero, rol) {
            document.getElementById('cred_nombre').innerText = (nombre + ' ' + apPat + ' ' + apMat).trim();
            document.getElementById('cred_numero').innerText = numero || '—';
            document.getElementById('cred_area').innerText = rol || '—';
            document.getElementById('cred_qr').innerHTML = '';
            if (numero)
                new QRCode(document.getElementById('cred_qr'), { text: numero, width: 90, height: 90 });
            $('#modalCredencial').modal('show');
        }

        function imprimirCredencial() {
            var html = document.getElementById('credencialEmpleado').outerHTML;
            var w = window.open('', '_blank', 'width=420,height=600');
            w.document.write('<html><head><title>Credencial</title><style>' +
                'body{margin:20px;font-family:Segoe UI,Arial;background:#fff}' +
                '.credencial-card{width:340px;border-radius:10px;border:3px solid #ff6a00;overflow:hidden;box-shadow:0 8px 18px rgba(0,0,0,.2)}' +
                '.credencial-header{background:#111;color:#fff;padding:8px 12px;display:flex;align-items:center}' +
                '.credencial-header-bar{width:8px;height:36px;border-radius:4px;background:#ff6a00;flex-shrink:0}' +
                '.credencial-logo-text{margin-left:10px}.empresa{font-weight:700;font-size:15px;color:#ff6a00}' +
                '.subtitulo{font-size:11px;color:#fff;opacity:.9}.credencial-body{padding:10px 14px;font-size:13px;color:#111}' +
                '.credencial-body h5{font-size:15px;font-weight:600;margin-bottom:4px}' +
                '.credencial-datos{display:flex;justify-content:space-between;align-items:center;margin-top:6px}' +
                '.credencial-datos p{margin-bottom:3px}.credencial-footer{background:#f5f5f5;border-top:1px solid #e0e0e0;padding:5px 12px;font-size:11px;text-align:center;color:#555}' +
                '</style></head><body>' + html + '</body></html>');
            w.document.close(); w.focus(); w.print();
        }

    </script>

</asp:Content>
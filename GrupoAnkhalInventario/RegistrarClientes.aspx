<%@ Page Title="Clientes" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RegistrarClientes.aspx.cs" Inherits="GrupoAnkhalInventario.RegistrarClientes" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="css/gridviewPantalla.css" rel="stylesheet" />
    <style>
        .filtros-bar {
            background: #f8f9fa;
            border: 1px solid #dee2e6;
            border-radius: 8px;
            padding: 15px 20px;
            margin-bottom: 15px;
        }
        .filtros-bar label {
            font-weight: 600;
            font-size: 0.85rem;
            color: #003366;
            margin-bottom: 3px;
        }
        .pager-custom a, .pager-custom span {
            padding: 5px 10px;
            margin: 1px;
            border-radius: 4px;
            font-size: 0.9rem;
        }
        .pager-custom span {
            background-color: #003366;
            color: white;
            font-weight: 700;
            border-radius: 4px;
            padding: 5px 10px;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="container-fluid">
        <div class="row">
            <div class="col-12">
                <div class="card">
                    <div class="card-header" style="background-color: #003366; color: white;">
                        <h3 class="card-title"><i class="fas fa-users"></i> Clientes</h3>
                    </div>
                    <div class="card-body">

                        <div class="mb-3">
                            <asp:Button ID="btnNuevo" runat="server" Text="+ Nuevo Cliente"
                                CssClass="btn btn-success"
                                OnClientClick="abrirModalNuevo(); return false;" />
                        </div>

                        <!-- ── BARRA DE FILTROS ── -->
                        <div class="filtros-bar">
                            <div class="row align-items-end">
                                <div class="col-md-4">
                                    <label>Buscar por Nombre o Contacto</label>
                                    <asp:TextBox ID="txtBuscar" runat="server" CssClass="form-control form-control-sm"
                                        Placeholder="Nombre o contacto..."></asp:TextBox>
                                </div>
                                <div class="col-md-2">
                                    <label>Estado</label>
                                    <asp:DropDownList ID="ddlFiltrEstado" runat="server" CssClass="form-control form-control-sm">
                                        <asp:ListItem Value="">-- Todos --</asp:ListItem>
                                        <asp:ListItem Value="1">Activo</asp:ListItem>
                                        <asp:ListItem Value="0">Inactivo</asp:ListItem>
                                    </asp:DropDownList>
                                </div>
                                <div class="col-md-3 mt-1">
                                    <asp:Button ID="btnBuscar" runat="server" Text="Buscar"
                                        CssClass="btn btn-primary btn-sm mr-1" OnClick="btnBuscar_Click" />
                                    <asp:Button ID="btnLimpiarFiltros" runat="server" Text="Limpiar"
                                        CssClass="btn btn-secondary btn-sm" OnClick="btnLimpiarFiltros_Click" />
                                </div>
                            </div>
                        </div>

                        <!-- Contador de resultados -->
                        <div class="mb-2">
                            <small class="text-muted">
                                <asp:Label ID="lblResultados" runat="server" Text=""></asp:Label>
                            </small>
                        </div>

                        <div class="table-responsive">
                            <asp:GridView ID="gvClientes" runat="server" AutoGenerateColumns="False"
                                CssClass="table table-bordered table-striped custom-grid"
                                AllowPaging="True" AllowCustomPaging="True" PageSize="15"
                                OnPageIndexChanging="gvClientes_PageIndexChanging"
                                PagerStyle-CssClass="pager-custom"
                                PagerSettings-Mode="NumericFirstLast"
                                PagerSettings-FirstPageText="«"
                                PagerSettings-LastPageText="»"
                                PagerSettings-PageButtonCount="5">
                                <Columns>
                                    <asp:BoundField DataField="ClienteID"  HeaderText="ID"        Visible="false" />
                                    <asp:BoundField DataField="Nombre"     HeaderText="Nombre" />
                                    <asp:BoundField DataField="Contacto"   HeaderText="Contacto" />
                                    <asp:BoundField DataField="Telefono"   HeaderText="Tel&eacute;fono" />
                                    <asp:BoundField DataField="Email"      HeaderText="Email" />
                                    <asp:BoundField DataField="Direccion"  HeaderText="Direcci&oacute;n" />
                                    <asp:TemplateField HeaderText="Estado">
                                        <ItemTemplate>
                                            <span class="badge badge-<%# Convert.ToBoolean(Eval("Activo")) ? "success" : "secondary" %>">
                                                <%# Convert.ToBoolean(Eval("Activo")) ? "Activo" : "Inactivo" %>
                                            </span>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderText="Acciones">
                                        <ItemTemplate>
                                            <button type="button" class="btn btn-primary btn-sm"
                                                onclick="abrirModalEditar(
                                                    '<%# Eval("ClienteID") %>',
                                                    '<%# Server.HtmlEncode((Eval("Nombre")    ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Contacto")  ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Telefono")  ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Email")     ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Direccion") ?? "").ToString()) %>'
                                                )">
                                                <i class="fas fa-edit"></i> Editar
                                            </button>
                                            <asp:Button ID="btnToggle" runat="server"
                                                CssClass='<%# Convert.ToBoolean(Eval("Activo")) ? "btn btn-warning btn-sm" : "btn btn-success btn-sm" %>'
                                                Text='<%# Convert.ToBoolean(Eval("Activo")) ? "Desactivar" : "Activar" %>'
                                                CommandArgument='<%# Eval("ClienteID") %>'
                                                OnClientClick='<%# "return confirmarToggle(\"" + Eval("ClienteID") + "\", \"" + Server.HtmlEncode((Eval("Nombre") ?? "").ToString()) + "\", " + Eval("Activo").ToString().ToLower() + ");" %>'
                                                OnClick="btnToggle_Click" />
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                            </asp:GridView>
                        </div>

                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- Hidden para el toggle desde JS -->
    <asp:HiddenField ID="hdnToggleClienteID" runat="server" Value="" />
    <asp:Button ID="btnToggleHidden" runat="server" CssClass="d-none"
        OnClick="btnToggleHidden_Click" />

    <!-- Hidden para mensajes pendientes -->
    <asp:HiddenField ID="hdnMensajePendiente" runat="server" Value="" />

    <!-- ══ MODAL NUEVO CLIENTE ════════════════════════════════════════════════ -->
    <div class="modal fade" id="modalNuevo" tabindex="-1" role="dialog" data-backdrop="static">
        <div class="modal-dialog modal-lg" role="document">
            <div class="modal-content">
                <div class="modal-header" style="background-color: #003366; color: white;">
                    <h5 class="modal-title"><i class="fas fa-user-plus"></i> Nuevo Cliente</h5>
                    <button type="button" class="close text-white" data-dismiss="modal">
                        <span>&times;</span>
                    </button>
                </div>
                <div class="modal-body">
                    <div class="row">
                        <div class="col-md-12">
                            <div class="form-group">
                                <label>Nombre <span style="color:red">*</span></label>
                                <asp:TextBox ID="txtNombre" runat="server" CssClass="form-control"
                                    Placeholder="Nombre completo o raz&oacute;n social" MaxLength="200"></asp:TextBox>
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <label>Contacto</label>
                                <asp:TextBox ID="txtContacto" runat="server" CssClass="form-control"
                                    Placeholder="Nombre del contacto" MaxLength="150"></asp:TextBox>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <label>Tel&eacute;fono</label>
                                <asp:TextBox ID="txtTelefono" runat="server" CssClass="form-control"
                                    Placeholder="Ej: 7391234567" MaxLength="20"></asp:TextBox>
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <label>Email</label>
                                <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control"
                                    Placeholder="correo@empresa.com" MaxLength="150" TextMode="Email"></asp:TextBox>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <label>Direcci&oacute;n</label>
                                <asp:TextBox ID="txtDireccion" runat="server" CssClass="form-control"
                                    Placeholder="Direcci&oacute;n completa" MaxLength="300"></asp:TextBox>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="modal-footer">
                    <asp:Button ID="btnGuardar" runat="server" Text="Guardar"
                        CssClass="btn btn-success"
                        OnClientClick="return validarFormularioNuevo();"
                        OnClick="btnGuardar_Click" />
                    <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancelar</button>
                </div>
            </div>
        </div>
    </div>

    <!-- ══ MODAL EDITAR CLIENTE ═══════════════════════════════════════════════ -->
    <div class="modal fade" id="modalEditar" tabindex="-1" role="dialog" data-backdrop="static">
        <div class="modal-dialog modal-lg" role="document">
            <div class="modal-content">
                <div class="modal-header" style="background-color: #003366; color: white;">
                    <h5 class="modal-title"><i class="fas fa-edit"></i> Editar Cliente</h5>
                    <button type="button" class="close text-white" data-dismiss="modal">
                        <span>&times;</span>
                    </button>
                </div>
                <div class="modal-body">
                    <asp:HiddenField ID="hdnClienteID" runat="server" />
                    <div class="row">
                        <div class="col-md-12">
                            <div class="form-group">
                                <label>Nombre <span style="color:red">*</span></label>
                                <asp:TextBox ID="txtNombreEdit" runat="server" CssClass="form-control"
                                    MaxLength="200"></asp:TextBox>
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <label>Contacto</label>
                                <asp:TextBox ID="txtContactoEdit" runat="server" CssClass="form-control"
                                    MaxLength="150"></asp:TextBox>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <label>Tel&eacute;fono</label>
                                <asp:TextBox ID="txtTelefonoEdit" runat="server" CssClass="form-control"
                                    MaxLength="20"></asp:TextBox>
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <div class="form-group">
                                <label>Email</label>
                                <asp:TextBox ID="txtEmailEdit" runat="server" CssClass="form-control"
                                    MaxLength="150" TextMode="Email"></asp:TextBox>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <label>Direcci&oacute;n</label>
                                <asp:TextBox ID="txtDireccionEdit" runat="server" CssClass="form-control"
                                    MaxLength="300"></asp:TextBox>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="modal-footer">
                    <asp:Button ID="btnGuardarEdit" runat="server" Text="Guardar Cambios"
                        CssClass="btn btn-success"
                        OnClientClick="return validarFormularioEditar();"
                        OnClick="btnGuardarEdit_Click" />
                    <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancelar</button>
                </div>
            </div>
        </div>
    </div>

    <script>

        // ── Al cargar la página, revisar si hay mensaje pendiente ──────────────
        window.addEventListener('load', function () {
            var hdnMsg = document.getElementById('<%= hdnMensajePendiente.ClientID %>');
            if (!hdnMsg) return;
            var raw = hdnMsg.value;
            if (!raw || raw === '') return;

            try {
                var msg = JSON.parse(raw);
                hdnMsg.value = '';

                var swalOpts = {
                    icon: msg.icon,
                    title: msg.title,
                    text: msg.text,
                    confirmButtonColor: '#003366'
                };

                if (msg.icon === 'success') {
                    swalOpts.showConfirmButton = false;
                    swalOpts.timer = 2000;
                }

                if (msg.modal) {
                    swalOpts.showConfirmButton = true;
                    Swal.fire(swalOpts).then(function () {
                        $('#' + msg.modal).modal('show');
                    });
                } else {
                    Swal.fire(swalOpts);
                }
            } catch (e) {
                console.log('Error al parsear mensaje:', e);
            }
        });

        function abrirModalNuevo() {
            $('#modalNuevo').modal('show');
        }

        function abrirModalEditar(id, nombre, contacto, telefono, email, direccion) {
            document.getElementById('<%= hdnClienteID.ClientID %>').value = id;
            document.getElementById('<%= txtNombreEdit.ClientID %>').value    = nombre;
            document.getElementById('<%= txtContactoEdit.ClientID %>').value  = contacto;
            document.getElementById('<%= txtTelefonoEdit.ClientID %>').value  = telefono;
            document.getElementById('<%= txtEmailEdit.ClientID %>').value     = email;
            document.getElementById('<%= txtDireccionEdit.ClientID %>').value = direccion;
            $('#modalEditar').modal('show');
        }

        function validarFormularioNuevo() {
            var nombre = document.getElementById('<%= txtNombre.ClientID %>').value.trim();
            var telef  = document.getElementById('<%= txtTelefono.ClientID %>').value.trim();

            if (nombre === '') {
                Swal.fire({ icon: 'warning', title: 'Campo obligatorio', text: 'El nombre del cliente es obligatorio.', confirmButtonColor: '#003366' });
                return false;
            }
            if (nombre.length < 2) {
                Swal.fire({ icon: 'warning', title: 'Nombre muy corto', text: 'El nombre debe tener al menos 2 caracteres.', confirmButtonColor: '#003366' });
                return false;
            }
            if (telef !== '' && !/^\d{7,20}$/.test(telef)) {
                Swal.fire({ icon: 'warning', title: 'Tel\u00e9fono inv\u00e1lido', text: 'El tel\u00e9fono debe contener solo n\u00fameros (7 a 20 d\u00edgitos).', confirmButtonColor: '#003366' });
                return false;
            }
            return true;
        }

        function validarFormularioEditar() {
            var nombre = document.getElementById('<%= txtNombreEdit.ClientID %>').value.trim();
            var telef  = document.getElementById('<%= txtTelefonoEdit.ClientID %>').value.trim();

            if (nombre === '') {
                Swal.fire({ icon: 'warning', title: 'Campo obligatorio', text: 'El nombre del cliente es obligatorio.', confirmButtonColor: '#003366' });
                return false;
            }
            if (nombre.length < 2) {
                Swal.fire({ icon: 'warning', title: 'Nombre muy corto', text: 'El nombre debe tener al menos 2 caracteres.', confirmButtonColor: '#003366' });
                return false;
            }
            if (telef !== '' && !/^\d{7,20}$/.test(telef)) {
                Swal.fire({ icon: 'warning', title: 'Tel\u00e9fono inv\u00e1lido', text: 'El tel\u00e9fono debe contener solo n\u00fameros (7 a 20 d\u00edgitos).', confirmButtonColor: '#003366' });
                return false;
            }
            return true;
        }

        // Toggle con confirmaci&oacute;n SweetAlert usando __doPostBack
        function confirmarToggle(clienteID, nombre, activo) {
            var accion   = activo ? 'desactivar' : 'activar';
            var icono    = activo ? 'warning' : 'question';
            var btnColor = activo ? '#e0a800' : '#28a745';

            Swal.fire({
                icon: icono,
                title: '\u00bf' + (activo ? 'Desactivar' : 'Activar') + ' cliente?',
                html: '\u00bfEst\u00e1 seguro de <b>' + accion + '</b> al cliente <b>' + nombre + '</b>?',
                showCancelButton: true,
                confirmButtonText: 'S\u00ed, ' + accion,
                cancelButtonText: 'Cancelar',
                confirmButtonColor: btnColor,
                cancelButtonColor: '#6c757d'
            }).then(function (result) {
                if (result.isConfirmed) {
                    document.getElementById('<%= hdnToggleClienteID.ClientID %>').value = clienteID;
                    __doPostBack('<%= btnToggleHidden.UniqueID %>', '');
                }
            });
            return false;
        }

    </script>

</asp:Content>

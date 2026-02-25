<%@ Page Title="Bases / Plantas" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Bases.aspx.cs" Inherits="GrupoAnkhalInventario.Bases" %>

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
                        <h3 class="card-title"><i class="fas fa-warehouse"></i> Bases / Plantas</h3>
                    </div>
                    <div class="card-body">

                        <div class="mb-3">
                            <asp:Button ID="btnNueva" runat="server" Text="＋ Nueva Base"
                                CssClass="btn btn-success"
                                OnClientClick="abrirModalNueva(); return false;" />
                        </div>

                        <!-- ── BARRA DE BÚSQUEDA Y FILTROS ── -->
                        <div class="filtros-bar">
                            <div class="row align-items-end">
                                <div class="col-md-3">
                                    <label>Buscar por Nombre o Código</label>
                                    <asp:TextBox ID="txtBuscar" runat="server" CssClass="form-control form-control-sm"
                                        Placeholder="Nombre o código..."></asp:TextBox>
                                </div>
                                <div class="col-md-2">
                                    <label>Tipo</label>
                                    <asp:DropDownList ID="ddlFiltrTipo" runat="server" CssClass="form-control form-control-sm">
                                        <asp:ListItem Value="">-- Todos --</asp:ListItem>
                                        <asp:ListItem Value="Produccion">Producción</asp:ListItem>
                                        <asp:ListItem Value="Almacen">Almacén</asp:ListItem>
                                        <asp:ListItem Value="Mixta">Mixta</asp:ListItem>
                                    </asp:DropDownList>
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
                                    <asp:Button ID="btnBuscar" runat="server" Text="🔍 Buscar"
                                        CssClass="btn btn-primary btn-sm mr-1" OnClick="btnBuscar_Click" />
                                    <asp:Button ID="btnLimpiarFiltros" runat="server" Text="✖ Limpiar"
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
                            <asp:GridView ID="gvBases" runat="server" AutoGenerateColumns="False"
                                CssClass="table table-bordered table-striped custom-grid"
                                AllowPaging="True" PageSize="15"
                                OnPageIndexChanging="gvBases_PageIndexChanging"
                                PagerStyle-CssClass="pager-custom"
                                PagerSettings-Mode="NumericFirstLast"
                                PagerSettings-FirstPageText="«"
                                PagerSettings-LastPageText="»"
                                PagerSettings-PageButtonCount="5">
                                <Columns>
                                    <asp:BoundField DataField="BaseID"        HeaderText="ID"              Visible="false" />
                                    <asp:BoundField DataField="Codigo"        HeaderText="Código" />
                                    <asp:BoundField DataField="Nombre"        HeaderText="Nombre" />
                                    <asp:BoundField DataField="Tipo"          HeaderText="Tipo" />
                                    <asp:BoundField DataField="Responsable"   HeaderText="Responsable" />
                                    <asp:BoundField DataField="Telefono"      HeaderText="Teléfono" />
                                    <asp:BoundField DataField="MetaTarimas"   HeaderText="Meta Tarimas" />
                                    <asp:BoundField DataField="MetaCajas"     HeaderText="Meta Cajas" />
                                    <asp:BoundField DataField="MetaAccesorios" HeaderText="Meta Accesorios" />
                                    <asp:TemplateField HeaderText="Estatus">
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
                                                    '<%# Eval("BaseID") %>',
                                                    '<%# Eval("Codigo") %>',
                                                    '<%# Server.HtmlEncode((Eval("Nombre")      ?? "").ToString()) %>',
                                                    '<%# Eval("Tipo") %>',
                                                    '<%# Server.HtmlEncode((Eval("Responsable") ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Telefono")    ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Direccion")   ?? "").ToString()) %>',
                                                    '<%# Eval("MetaTarimas") %>',
                                                    '<%# Eval("MetaCajas") %>',
                                                    '<%# Eval("MetaAccesorios") %>'
                                                )">
                                                <i class="fas fa-edit"></i> Editar
                                            </button>
                                            <asp:Button ID="btnToggle" runat="server"
                                                CssClass='<%# Convert.ToBoolean(Eval("Activo")) ? "btn btn-warning btn-sm" : "btn btn-success btn-sm" %>'
                                                Text='<%# Convert.ToBoolean(Eval("Activo")) ? "Desactivar" : "Activar" %>'
                                                CommandArgument='<%# Eval("BaseID") %>'
                                                OnClientClick='<%# "return confirmarToggle(\"" + Eval("BaseID") + "\", \"" + Eval("Nombre") + "\", " + Eval("Activo").ToString().ToLower() + ");" %>'
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
    <asp:HiddenField ID="hdnToggleBaseID" runat="server" Value="" />
    <asp:Button ID="btnToggleHidden" runat="server" CssClass="d-none"
        OnClick="btnToggleHidden_Click" />

    <!-- Hidden para mensajes pendientes (se disparan después del postback) -->
    <asp:HiddenField ID="hdnMensajePendiente" runat="server" Value="" />

    <!-- Modal Nueva Base -->
    <div class="modal fade" id="modalNueva" tabindex="-1" role="dialog" data-backdrop="static">
        <div class="modal-dialog modal-lg" role="document">
            <div class="modal-content">
                <div class="modal-header" style="background-color: #003366; color: white;">
                    <h5 class="modal-title"><i class="fas fa-warehouse"></i> Nueva Base</h5>
                    <button type="button" class="close text-white" data-dismiss="modal">
                        <span>&times;</span>
                    </button>
                </div>
                <div class="modal-body">
                    <div class="row">
                        <div class="col-md-4">
                            <div class="form-group">
                                <label>Código <span style="color:red">*</span></label>
                                <asp:TextBox ID="txtCodigo" runat="server" CssClass="form-control"
                                    Placeholder="Ej: TULA, WEG" MaxLength="20"></asp:TextBox>
                                <small class="text-muted">Se guardará en mayúsculas.</small>
                            </div>
                        </div>
                        <div class="col-md-8">
                            <div class="form-group">
                                <label>Nombre <span style="color:red">*</span></label>
                                <asp:TextBox ID="txtNombre" runat="server" CssClass="form-control"
                                    Placeholder="Nombre completo de la base" MaxLength="150"></asp:TextBox>
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-4">
                            <div class="form-group">
                                <label>Tipo <span style="color:red">*</span></label>
                                <asp:DropDownList ID="ddlTipo" runat="server" CssClass="form-control">
                                    <asp:ListItem Value="">-- Seleccione --</asp:ListItem>
                                    <asp:ListItem Value="Produccion">Producción</asp:ListItem>
                                    <asp:ListItem Value="Almacen">Almacén</asp:ListItem>
                                    <asp:ListItem Value="Mixta">Mixta</asp:ListItem>
                                </asp:DropDownList>
                            </div>
                        </div>
                        <div class="col-md-4">
                            <div class="form-group">
                                <label>Responsable</label>
                                <asp:TextBox ID="txtResponsable" runat="server" CssClass="form-control"
                                    Placeholder="Nombre del responsable" MaxLength="100"></asp:TextBox>
                            </div>
                        </div>
                        <div class="col-md-4">
                            <div class="form-group">
                                <label>Teléfono</label>
                                <asp:TextBox ID="txtTelefono" runat="server" CssClass="form-control"
                                    Placeholder="Ej: 7391234567" MaxLength="20"></asp:TextBox>
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-12">
                            <div class="form-group">
                                <label>Dirección</label>
                                <asp:TextBox ID="txtDireccion" runat="server" CssClass="form-control"
                                    Placeholder="Dirección completa" MaxLength="200"></asp:TextBox>
                            </div>
                        </div>
                    </div>
                    <hr />
                    <h6 style="color:#003366; font-weight:600;">
                        <i class="fas fa-bullseye"></i> Metas Diarias de Producción
                    </h6>
                    <small class="text-muted">Poner 0 si no aplica (ej. almacén puro).</small>
                    <div class="row mt-2">
                        <div class="col-md-4">
                            <div class="form-group">
                                <label>Meta Tarimas / día</label>
                                <asp:TextBox ID="txtMetaTarimas" runat="server" CssClass="form-control"
                                    TextMode="Number" Text="0"></asp:TextBox>
                            </div>
                        </div>
                        <div class="col-md-4">
                            <div class="form-group">
                                <label>Meta Cajas / día</label>
                                <asp:TextBox ID="txtMetaCajas" runat="server" CssClass="form-control"
                                    TextMode="Number" Text="0"></asp:TextBox>
                            </div>
                        </div>
                        <div class="col-md-4">
                            <div class="form-group">
                                <label>Meta Accesorios / día</label>
                                <asp:TextBox ID="txtMetaAccesorios" runat="server" CssClass="form-control"
                                    TextMode="Number" Text="0"></asp:TextBox>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="modal-footer">
                    <asp:Button ID="btnGuardar" runat="server" Text="Guardar"
                        CssClass="btn btn-success"
                        OnClientClick="return validarFormularioNueva();"
                        OnClick="btnGuardar_Click" />
                    <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancelar</button>
                </div>
            </div>
        </div>
    </div>

    <!-- Modal Editar Base -->
    <div class="modal fade" id="modalEditar" tabindex="-1" role="dialog" data-backdrop="static">
        <div class="modal-dialog modal-lg" role="document">
            <div class="modal-content">
                <div class="modal-header" style="background-color: #003366; color: white;">
                    <h5 class="modal-title"><i class="fas fa-edit"></i> Editar Base</h5>
                    <button type="button" class="close text-white" data-dismiss="modal">
                        <span>&times;</span>
                    </button>
                </div>
                <div class="modal-body">
                    <asp:HiddenField ID="hdnBaseID" runat="server" />
                    <div class="row">
                        <div class="col-md-4">
                            <div class="form-group">
                                <label>Código <span style="color:red">*</span></label>
                                <asp:TextBox ID="txtCodigoEdit" runat="server" CssClass="form-control"
                                    MaxLength="20"></asp:TextBox>
                                <small class="text-muted">Se guardará en mayúsculas.</small>
                            </div>
                        </div>
                        <div class="col-md-8">
                            <div class="form-group">
                                <label>Nombre <span style="color:red">*</span></label>
                                <asp:TextBox ID="txtNombreEdit" runat="server" CssClass="form-control"
                                    MaxLength="150"></asp:TextBox>
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-4">
                            <div class="form-group">
                                <label>Tipo <span style="color:red">*</span></label>
                                <asp:DropDownList ID="ddlTipoEdit" runat="server" CssClass="form-control">
                                    <asp:ListItem Value="">-- Seleccione --</asp:ListItem>
                                    <asp:ListItem Value="Produccion">Producción</asp:ListItem>
                                    <asp:ListItem Value="Almacen">Almacén</asp:ListItem>
                                    <asp:ListItem Value="Mixta">Mixta</asp:ListItem>
                                </asp:DropDownList>
                            </div>
                        </div>
                        <div class="col-md-4">
                            <div class="form-group">
                                <label>Responsable</label>
                                <asp:TextBox ID="txtResponsableEdit" runat="server" CssClass="form-control"
                                    MaxLength="100"></asp:TextBox>
                            </div>
                        </div>
                        <div class="col-md-4">
                            <div class="form-group">
                                <label>Teléfono</label>
                                <asp:TextBox ID="txtTelefonoEdit" runat="server" CssClass="form-control"
                                    MaxLength="20"></asp:TextBox>
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-12">
                            <div class="form-group">
                                <label>Dirección</label>
                                <asp:TextBox ID="txtDireccionEdit" runat="server" CssClass="form-control"
                                    MaxLength="200"></asp:TextBox>
                            </div>
                        </div>
                    </div>
                    <hr />
                    <h6 style="color:#003366; font-weight:600;">
                        <i class="fas fa-bullseye"></i> Metas Diarias de Producción
                    </h6>
                    <small class="text-muted">Poner 0 si no aplica.</small>
                    <div class="row mt-2">
                        <div class="col-md-4">
                            <div class="form-group">
                                <label>Meta Tarimas / día</label>
                                <asp:TextBox ID="txtMetaTarimasEdit" runat="server" CssClass="form-control"
                                    TextMode="Number"></asp:TextBox>
                            </div>
                        </div>
                        <div class="col-md-4">
                            <div class="form-group">
                                <label>Meta Cajas / día</label>
                                <asp:TextBox ID="txtMetaCajasEdit" runat="server" CssClass="form-control"
                                    TextMode="Number"></asp:TextBox>
                            </div>
                        </div>
                        <div class="col-md-4">
                            <div class="form-group">
                                <label>Meta Accesorios / día</label>
                                <asp:TextBox ID="txtMetaAccesoriosEdit" runat="server" CssClass="form-control"
                                    TextMode="Number"></asp:TextBox>
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

        function abrirModalNueva() {
            $('#modalNueva').modal('show');
        }

        function abrirModalEditar(id, codigo, nombre, tipo, responsable, telefono, direccion, metaTarimas, metaCajas, metaAccesorios) {
            document.getElementById('<%= hdnBaseID.ClientID %>').value = id;
            document.getElementById('<%= txtCodigoEdit.ClientID %>').value = codigo;
            document.getElementById('<%= txtNombreEdit.ClientID %>').value = nombre;
            document.getElementById('<%= ddlTipoEdit.ClientID %>').value = tipo;
            document.getElementById('<%= txtResponsableEdit.ClientID %>').value    = responsable;
            document.getElementById('<%= txtTelefonoEdit.ClientID %>').value       = telefono;
            document.getElementById('<%= txtDireccionEdit.ClientID %>').value      = direccion;
            document.getElementById('<%= txtMetaTarimasEdit.ClientID %>').value    = metaTarimas;
            document.getElementById('<%= txtMetaCajasEdit.ClientID %>').value      = metaCajas;
            document.getElementById('<%= txtMetaAccesoriosEdit.ClientID %>').value = metaAccesorios;
            $('#modalEditar').modal('show');
        }

        function validarFormularioNueva() {
            var codigo = document.getElementById('<%= txtCodigo.ClientID %>').value.trim();
            var nombre = document.getElementById('<%= txtNombre.ClientID %>').value.trim();
            var tipo   = document.getElementById('<%= ddlTipo.ClientID %>').value;
            var telef  = document.getElementById('<%= txtTelefono.ClientID %>').value.trim();
            var metaT  = parseInt(document.getElementById('<%= txtMetaTarimas.ClientID %>').value) || 0;
            var metaC  = parseInt(document.getElementById('<%= txtMetaCajas.ClientID %>').value) || 0;
            var metaA  = parseInt(document.getElementById('<%= txtMetaAccesorios.ClientID %>').value) || 0;

            if (codigo === '') {
                Swal.fire({ icon: 'warning', title: 'Campo obligatorio', text: 'El código es obligatorio.', confirmButtonColor: '#003366' });
                return false;
            }
            if (codigo.length < 2) {
                Swal.fire({ icon: 'warning', title: 'Código muy corto', text: 'El código debe tener al menos 2 caracteres.', confirmButtonColor: '#003366' });
                return false;
            }
            if (nombre === '') {
                Swal.fire({ icon: 'warning', title: 'Campo obligatorio', text: 'El nombre es obligatorio.', confirmButtonColor: '#003366' });
                return false;
            }
            if (nombre.length < 3) {
                Swal.fire({ icon: 'warning', title: 'Nombre muy corto', text: 'El nombre debe tener al menos 3 caracteres.', confirmButtonColor: '#003366' });
                return false;
            }
            if (tipo === '') {
                Swal.fire({ icon: 'warning', title: 'Campo obligatorio', text: 'Debe seleccionar el tipo de base.', confirmButtonColor: '#003366' });
                return false;
            }
            if (telef !== '' && !/^\d{7,20}$/.test(telef)) {
                Swal.fire({ icon: 'warning', title: 'Teléfono inválido', text: 'El teléfono debe contener solo números (7 a 20 dígitos).', confirmButtonColor: '#003366' });
                return false;
            }
            if (metaT < 0 || metaC < 0 || metaA < 0) {
                Swal.fire({ icon: 'warning', title: 'Metas inválidas', text: 'Las metas no pueden ser negativas.', confirmButtonColor: '#003366' });
                return false;
            }
            return true;
        }

        function validarFormularioEditar() {
            var codigo = document.getElementById('<%= txtCodigoEdit.ClientID %>').value.trim();
            var nombre = document.getElementById('<%= txtNombreEdit.ClientID %>').value.trim();
            var tipo   = document.getElementById('<%= ddlTipoEdit.ClientID %>').value;
            var telef  = document.getElementById('<%= txtTelefonoEdit.ClientID %>').value.trim();
            var metaT  = parseInt(document.getElementById('<%= txtMetaTarimasEdit.ClientID %>').value) || 0;
            var metaC  = parseInt(document.getElementById('<%= txtMetaCajasEdit.ClientID %>').value) || 0;
            var metaA  = parseInt(document.getElementById('<%= txtMetaAccesoriosEdit.ClientID %>').value) || 0;

            if (codigo === '') {
                Swal.fire({ icon: 'warning', title: 'Campo obligatorio', text: 'El código es obligatorio.', confirmButtonColor: '#003366' });
                return false;
            }
            if (codigo.length < 2) {
                Swal.fire({ icon: 'warning', title: 'Código muy corto', text: 'El código debe tener al menos 2 caracteres.', confirmButtonColor: '#003366' });
                return false;
            }
            if (nombre === '') {
                Swal.fire({ icon: 'warning', title: 'Campo obligatorio', text: 'El nombre es obligatorio.', confirmButtonColor: '#003366' });
                return false;
            }
            if (nombre.length < 3) {
                Swal.fire({ icon: 'warning', title: 'Nombre muy corto', text: 'El nombre debe tener al menos 3 caracteres.', confirmButtonColor: '#003366' });
                return false;
            }
            if (tipo === '') {
                Swal.fire({ icon: 'warning', title: 'Campo obligatorio', text: 'Debe seleccionar el tipo de base.', confirmButtonColor: '#003366' });
                return false;
            }
            if (telef !== '' && !/^\d{7,20}$/.test(telef)) {
                Swal.fire({ icon: 'warning', title: 'Teléfono inválido', text: 'El teléfono debe contener solo números (7 a 20 dígitos).', confirmButtonColor: '#003366' });
                return false;
            }
            if (metaT < 0 || metaC < 0 || metaA < 0) {
                Swal.fire({ icon: 'warning', title: 'Metas inválidas', text: 'Las metas no pueden ser negativas.', confirmButtonColor: '#003366' });
                return false;
            }
            return true;
        }

        // Toggle con confirmación SweetAlert usando __doPostBack
        function confirmarToggle(baseID, nombre, activo) {
            var accion   = activo ? 'desactivar' : 'activar';
            var icono    = activo ? 'warning' : 'question';
            var btnColor = activo ? '#e0a800' : '#28a745';

            Swal.fire({
                icon: icono,
                title: '¿' + (activo ? 'Desactivar' : 'Activar') + ' base?',
                html: '¿Está seguro de <b>' + accion + '</b> la base <b>' + nombre + '</b>?',
                showCancelButton: true,
                confirmButtonText: 'Sí, ' + accion,
                cancelButtonText: 'Cancelar',
                confirmButtonColor: btnColor,
                cancelButtonColor: '#6c757d'
            }).then(function (result) {
                if (result.isConfirmed) {
                    document.getElementById('<%= hdnToggleBaseID.ClientID %>').value = baseID;
                    __doPostBack('<%= btnToggleHidden.UniqueID %>', '');
                }
            });
            return false;
        }

    </script>

</asp:Content>

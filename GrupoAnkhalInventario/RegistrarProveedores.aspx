<%@ Page Title="Proveedores" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RegistrarProveedores.aspx.cs" Inherits="GrupoAnkhalInventario.RegistrarProveedores" %>

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
        .nav-tabs .nav-link.active {
            color: #003366;
            font-weight: 600;
        }
        .nav-tabs .nav-link { color: #555; }
        .btn-validar { white-space: nowrap; }
        .field-hint {
            font-size: 0.75rem;
            color: #6c757d;
            margin-top: 2px;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="container-fluid">
        <div class="row">
            <div class="col-12">
                <div class="card">
                    <div class="card-header" style="background-color: #003366; color: white;">
                        <h3 class="card-title"><i class="fas fa-truck"></i> Proveedores</h3>
                    </div>
                    <div class="card-body">

                        <div class="mb-3">
                            <asp:Button ID="btnNuevo" runat="server" Text="+ Nuevo Proveedor"
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

                        <div class="mb-2">
                            <small class="text-muted">
                                <asp:Label ID="lblResultados" runat="server" Text=""></asp:Label>
                            </small>
                        </div>

                        <div class="table-responsive">
                            <asp:GridView ID="gvProveedores" runat="server" AutoGenerateColumns="False"
                                CssClass="table table-bordered table-striped custom-grid"
                                AllowPaging="True" AllowCustomPaging="True" PageSize="15"
                                OnPageIndexChanging="gvProveedores_PageIndexChanging"
                                PagerStyle-CssClass="pager-custom"
                                PagerSettings-Mode="NumericFirstLast"
                                PagerSettings-FirstPageText="«"
                                PagerSettings-LastPageText="»"
                                PagerSettings-PageButtonCount="5">
                                <Columns>
                                    <asp:BoundField DataField="ProveedorID"   HeaderText="ID"           Visible="false" />
                                    <asp:BoundField DataField="Nombre"        HeaderText="Nombre" />
                                    <asp:BoundField DataField="Contacto"      HeaderText="Contacto" />
                                    <asp:BoundField DataField="Telefono"      HeaderText="Tel&eacute;fono" />
                                    <asp:BoundField DataField="Email"         HeaderText="Email" />
                                    <asp:BoundField DataField="RFC"           HeaderText="RFC" />
                                    <asp:BoundField DataField="TipoPersona"   HeaderText="Tipo" />
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
                                                    '<%# Eval("ProveedorID") %>',
                                                    '<%# Server.HtmlEncode((Eval("Nombre")            ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Contacto")          ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Telefono")          ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Email")             ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("PaginaWeb")         ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("TipoEmpresa")       ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Nacionalidad")      ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("TipoPersona")       ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("RazonSocialFiscal") ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("RFC")               ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("RegimenFiscal")     ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("CURP")              ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Banco")             ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("CLABE")             ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("CuentaBancaria")    ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("TitularCuenta")     ?? "").ToString()) %>',
                                                    '<%# Eval("DiasCredito")   ?? "" %>',
                                                    '<%# Eval("LimiteCredito") ?? "" %>',
                                                    '<%# Server.HtmlEncode((Eval("Pais")              ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("CodigoPostal")      ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Estado")            ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Municipio")         ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Colonia")           ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("NumExt")            ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("NumInt")            ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Referencia")        ?? "").ToString()) %>'
                                                )">
                                                <i class="fas fa-edit"></i> Editar
                                            </button>
                                            <asp:Button ID="btnToggle" runat="server"
                                                CssClass='<%# Convert.ToBoolean(Eval("Activo")) ? "btn btn-warning btn-sm" : "btn btn-success btn-sm" %>'
                                                Text='<%# Convert.ToBoolean(Eval("Activo")) ? "Desactivar" : "Activar" %>'
                                                CommandArgument='<%# Eval("ProveedorID") %>'
                                                OnClientClick='<%# "return confirmarToggle(\"" + Eval("ProveedorID") + "\", \"" + Server.HtmlEncode((Eval("Nombre") ?? "").ToString()) + "\", " + Eval("Activo").ToString().ToLower() + ");" %>'
                                                OnClick="btnToggle_Click" />
                                            <button type="button" class="btn btn-info btn-sm"
                                                onclick="abrirModalMateriales('<%# Eval("ProveedorID") %>','<%# Server.HtmlEncode((Eval("Nombre") ?? "").ToString()) %>')">
                                                <i class="fas fa-boxes"></i> Materiales
                                            </button>
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

    <asp:HiddenField ID="hdnToggleProveedorID" runat="server" Value="" />
    <asp:Button ID="btnToggleHidden" runat="server" CssClass="d-none" OnClick="btnToggleHidden_Click" />
    <asp:HiddenField ID="hdnMensajePendiente" runat="server" Value="" />

    <%-- ── HiddenFields para el módulo de materiales por proveedor ── --%>
    <asp:HiddenField ID="hdnMaterialesProveedorID" runat="server" Value="" />
    <asp:HiddenField ID="hdnProveedorMaterialID"   runat="server" Value="" />
    <asp:HiddenField ID="hdnAccionMateriales"      runat="server" Value="" />
    <asp:HiddenField ID="hdnNuevoPrecio"           runat="server" Value="" />
    <asp:HiddenField ID="hdnNuevaNotaPrecio"       runat="server" Value="" />
    <%-- Botones servidor invisibles disparados por __doPostBack desde JS --%>
    <asp:Button ID="btnAgregarMaterial"  runat="server" CssClass="d-none" OnClick="btnAgregarMaterial_Click" />
    <asp:Button ID="btnActualizarPrecio" runat="server" CssClass="d-none" OnClick="btnActualizarPrecio_Click" />
    <asp:Button ID="btnAccionMateriales" runat="server" CssClass="d-none" OnClick="btnAccionMateriales_Click" />

    <!-- ══════════════════════════════════════════════════════════════════════
         MODAL NUEVO PROVEEDOR
    ══════════════════════════════════════════════════════════════════════ -->
    <div class="modal fade" id="modalNuevo" tabindex="-1" role="dialog" data-backdrop="static">
        <div class="modal-dialog modal-xl" role="document">
            <div class="modal-content">
                <div class="modal-header" style="background-color:#003366;color:white;">
                    <h5 class="modal-title"><i class="fas fa-truck"></i> Nuevo Proveedor</h5>
                    <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
                </div>
                <div class="modal-body">
                    <ul class="nav nav-tabs mb-3" id="tabsNuevo" role="tablist">
                        <li class="nav-item"><a class="nav-link active" data-toggle="tab" href="#pane-gen-n"><i class="fas fa-user"></i> Datos Generales</a></li>
                        <li class="nav-item"><a class="nav-link" data-toggle="tab" href="#pane-fis-n"><i class="fas fa-file-invoice"></i> Datos Fiscales</a></li>
                        <li class="nav-item"><a class="nav-link" data-toggle="tab" href="#pane-ban-n"><i class="fas fa-university"></i> Datos Bancarios</a></li>
                        <li class="nav-item"><a class="nav-link" data-toggle="tab" href="#pane-dir-n"><i class="fas fa-map-marker-alt"></i> Direcci&oacute;n</a></li>
                    </ul>
                    <div class="tab-content">

                        <!-- ── TAB GENERALES ── -->
                        <div class="tab-pane fade show active" id="pane-gen-n">
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>Tipo de Persona <span style="color:red">*</span></label>
                                        <asp:DropDownList ID="ddlTipoPersona" runat="server" CssClass="form-control"
                                            onchange="onTipoPersonaChangeNuevo()">
                                            <asp:ListItem Value="">-- Seleccionar --</asp:ListItem>
                                            <asp:ListItem Value="Fisica">Persona F&iacute;sica</asp:ListItem>
                                            <asp:ListItem Value="Moral">Persona Moral</asp:ListItem>
                                        </asp:DropDownList>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-12">
                                    <div class="form-group">
                                        <label>Nombre Comercial <span style="color:red">*</span></label>
                                        <asp:TextBox ID="txtNombre" runat="server" CssClass="form-control"
                                            Placeholder="Nombre comercial o raz&oacute;n social" MaxLength="200"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>Contacto</label>
                                        <asp:TextBox ID="txtContacto" runat="server" CssClass="form-control"
                                            Placeholder="Nombre del contacto" MaxLength="100"></asp:TextBox>
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
                                            Placeholder="correo@empresa.com" MaxLength="100" TextMode="Email"></asp:TextBox>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>P&aacute;gina Web</label>
                                        <asp:TextBox ID="txtPaginaWeb" runat="server" CssClass="form-control"
                                            Placeholder="www.proveedor.com" MaxLength="200"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>Tipo de Empresa</label>
                                        <asp:DropDownList ID="ddlTipoEmpresa" runat="server" CssClass="form-control">
                                            <asp:ListItem Value="">-- Seleccionar --</asp:ListItem>
                                            <asp:ListItem Value="Matriz">Matriz</asp:ListItem>
                                            <asp:ListItem Value="Sucursal">Sucursal</asp:ListItem>
                                        </asp:DropDownList>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>Nacionalidad</label>
                                        <asp:TextBox ID="txtNacionalidad" runat="server" CssClass="form-control"
                                            Placeholder="Ej: Mexicana" MaxLength="100"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- ── TAB FISCAL ── -->
                        <div class="tab-pane fade" id="pane-fis-n">
                            <div class="row">
                                <div class="col-md-12">
                                    <div class="form-group">
                                        <label>Raz&oacute;n Social Fiscal</label>
                                        <asp:TextBox ID="txtRazonSocialFiscal" runat="server" CssClass="form-control"
                                            Placeholder="Nombre exacto registrado en el SAT" MaxLength="300"></asp:TextBox>
                                        <div class="field-hint"><i class="fas fa-info-circle"></i> Debe coincidir exactamente con el SAT para validar sus facturas.</div>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-8">
                                    <div class="form-group">
                                        <label>RFC</label>
                                        <div class="input-group">
                                            <asp:TextBox ID="txtRFC" runat="server" CssClass="form-control"
                                                Placeholder="Ej: ABC010101XYZ" MaxLength="13"
                                                style="text-transform:uppercase;"></asp:TextBox>
                                            <div class="input-group-append">
                                                <button type="button" class="btn btn-outline-primary btn-validar"
                                                    onclick="validarRFC('<%= txtRFC.ClientID %>', '<%= ddlTipoPersona.ClientID %>')">
                                                    <i class="fas fa-check-circle"></i> Validar RFC
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-12">
                                    <div class="form-group">
                                        <label>R&eacute;gimen Fiscal</label>
                                        <asp:DropDownList ID="ddlRegimenFiscal" runat="server" CssClass="form-control">
                                            <asp:ListItem Value="">-- Seleccionar --</asp:ListItem>
                                            <asp:ListItem Value="601" data-tipo="Moral">601 - General de Ley Personas Morales</asp:ListItem>
                                            <asp:ListItem Value="603" data-tipo="Moral">603 - Personas Morales con Fines no Lucrativos</asp:ListItem>
                                            <asp:ListItem Value="620" data-tipo="Moral">620 - Sociedades Cooperativas de Producci&oacute;n</asp:ListItem>
                                            <asp:ListItem Value="623" data-tipo="Moral">623 - Opcional para Grupos de Sociedades</asp:ListItem>
                                            <asp:ListItem Value="624" data-tipo="Moral">624 - Coordinados</asp:ListItem>
                                            <asp:ListItem Value="605" data-tipo="Fisica">605 - Sueldos y Salarios</asp:ListItem>
                                            <asp:ListItem Value="606" data-tipo="Fisica">606 - Arrendamiento</asp:ListItem>
                                            <asp:ListItem Value="608" data-tipo="Fisica">608 - Dem&aacute;s ingresos</asp:ListItem>
                                            <asp:ListItem Value="611" data-tipo="Fisica">611 - Ingresos por Dividendos</asp:ListItem>
                                            <asp:ListItem Value="612" data-tipo="Fisica">612 - Actividades Empresariales y Profesionales</asp:ListItem>
                                            <asp:ListItem Value="614" data-tipo="Fisica">614 - Ingresos por intereses</asp:ListItem>
                                            <asp:ListItem Value="621" data-tipo="Fisica">621 - Incorporaci&oacute;n Fiscal</asp:ListItem>
                                            <asp:ListItem Value="625" data-tipo="Fisica">625 - Plataformas Tecnol&oacute;gicas</asp:ListItem>
                                            <asp:ListItem Value="616" data-tipo="Ambos">616 - Sin obligaciones fiscales</asp:ListItem>
                                            <asp:ListItem Value="622" data-tipo="Ambos">622 - Act. Agr&iacute;colas, Ganaderas, Silv&iacute;colas y Pesqueras</asp:ListItem>
                                            <asp:ListItem Value="626" data-tipo="Ambos">626 - RESICO</asp:ListItem>
                                        </asp:DropDownList>
                                    </div>
                                </div>
                            </div>
                            <div class="row" id="rowCurp">
                                <div class="col-md-8">
                                    <div class="form-group">
                                        <label>CURP <small class="text-muted">(solo Personas F&iacute;sicas)</small></label>
                                        <div class="input-group">
                                            <asp:TextBox ID="txtCURP" runat="server" CssClass="form-control"
                                                Placeholder="18 caracteres" MaxLength="18"
                                                style="text-transform:uppercase;"></asp:TextBox>
                                            <div class="input-group-append">
                                                <button type="button" class="btn btn-outline-secondary btn-validar"
                                                    onclick="validarCURP('<%= txtCURP.ClientID %>')">
                                                    <i class="fas fa-check-circle"></i> Validar CURP
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- ── TAB BANCARIO ── -->
                        <div class="tab-pane fade" id="pane-ban-n">
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>Banco</label>
                                        <asp:TextBox ID="txtBanco" runat="server" CssClass="form-control"
                                            Placeholder="Ej: BBVA, Banorte, HSBC..." MaxLength="100"></asp:TextBox>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>CLABE Interbancaria</label>
                                        <asp:TextBox ID="txtCLABE" runat="server" CssClass="form-control"
                                            Placeholder="18 d&iacute;gitos" MaxLength="18"></asp:TextBox>
                                        <div class="field-hint"><i class="fas fa-info-circle"></i> 18 d&iacute;gitos num&eacute;ricos para transferencias.</div>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>N&uacute;mero de Cuenta</label>
                                        <asp:TextBox ID="txtCuentaBancaria" runat="server" CssClass="form-control"
                                            Placeholder="N&uacute;mero de cuenta bancaria" MaxLength="30"></asp:TextBox>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>Titular de la Cuenta</label>
                                        <asp:TextBox ID="txtTitularCuenta" runat="server" CssClass="form-control"
                                            Placeholder="Nombre del titular" MaxLength="200"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <hr />
                            <div class="row">
                                <div class="col-md-4">
                                    <div class="form-group">
                                        <label>D&iacute;as de Cr&eacute;dito</label>
                                        <asp:TextBox ID="txtDiasCredito" runat="server" CssClass="form-control"
                                            Placeholder="Ej: 30" MaxLength="5" TextMode="Number"></asp:TextBox>
                                        <div class="field-hint">D&iacute;as para pago de facturas del proveedor.</div>
                                    </div>
                                </div>
                                <div class="col-md-4">
                                    <div class="form-group">
                                        <label>L&iacute;mite de Cr&eacute;dito <small class="text-muted">($ MXN)</small></label>
                                        <asp:TextBox ID="txtLimiteCredito" runat="server" CssClass="form-control"
                                            Placeholder="Ej: 50000.00" MaxLength="15" TextMode="Number"></asp:TextBox>
                                        <div class="field-hint">Monto m&aacute;ximo autorizado de compra a cr&eacute;dito. Ingresar en pesos mexicanos.</div>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- ── TAB DIRECCIÓN ── -->
                        <div class="tab-pane fade" id="pane-dir-n">
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>Pa&iacute;s</label>
                                        <asp:TextBox ID="txtPais" runat="server" CssClass="form-control"
                                            Placeholder="Ej: M&eacute;xico" MaxLength="100"></asp:TextBox>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>C&oacute;digo Postal</label>
                                        <asp:TextBox ID="txtCodigoPostal" runat="server" CssClass="form-control"
                                            Placeholder="Ej: 06600" MaxLength="10"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>Estado</label>
                                        <asp:TextBox ID="txtEstado" runat="server" CssClass="form-control"
                                            Placeholder="Ej: Ciudad de M&eacute;xico" MaxLength="100"></asp:TextBox>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>Municipio / Alcald&iacute;a</label>
                                        <asp:TextBox ID="txtMunicipio" runat="server" CssClass="form-control"
                                            Placeholder="Ej: Cuauht&eacute;moc" MaxLength="100"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-12">
                                    <div class="form-group">
                                        <label>Colonia</label>
                                        <asp:TextBox ID="txtColonia" runat="server" CssClass="form-control"
                                            Placeholder="Ej: Doctores" MaxLength="150"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>N&uacute;m. Exterior</label>
                                        <asp:TextBox ID="txtNumExt" runat="server" CssClass="form-control"
                                            Placeholder="Ej: 123" MaxLength="20"></asp:TextBox>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>N&uacute;m. Interior</label>
                                        <asp:TextBox ID="txtNumInt" runat="server" CssClass="form-control"
                                            Placeholder="Ej: Depto. 4B" MaxLength="20"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-12">
                                    <div class="form-group">
                                        <label>Referencia</label>
                                        <asp:TextBox ID="txtReferencia" runat="server" CssClass="form-control"
                                            Placeholder="Ej: Entre calle X y calle Y" MaxLength="300"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                        </div>

                    </div><!-- /tab-content -->
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

    <!-- ══════════════════════════════════════════════════════════════════════
         MODAL EDITAR PROVEEDOR
    ══════════════════════════════════════════════════════════════════════ -->
    <div class="modal fade" id="modalEditar" tabindex="-1" role="dialog" data-backdrop="static">
        <div class="modal-dialog modal-xl" role="document">
            <div class="modal-content">
                <div class="modal-header" style="background-color:#003366;color:white;">
                    <h5 class="modal-title"><i class="fas fa-edit"></i> Editar Proveedor</h5>
                    <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
                </div>
                <div class="modal-body">
                    <asp:HiddenField ID="hdnProveedorID" runat="server" />
                    <ul class="nav nav-tabs mb-3" id="tabsEditar" role="tablist">
                        <li class="nav-item"><a class="nav-link active" data-toggle="tab" href="#pane-gen-e"><i class="fas fa-user"></i> Datos Generales</a></li>
                        <li class="nav-item"><a class="nav-link" data-toggle="tab" href="#pane-fis-e"><i class="fas fa-file-invoice"></i> Datos Fiscales</a></li>
                        <li class="nav-item"><a class="nav-link" data-toggle="tab" href="#pane-ban-e"><i class="fas fa-university"></i> Datos Bancarios</a></li>
                        <li class="nav-item"><a class="nav-link" data-toggle="tab" href="#pane-dir-e"><i class="fas fa-map-marker-alt"></i> Direcci&oacute;n</a></li>
                    </ul>
                    <div class="tab-content">

                        <!-- ── TAB GENERALES ── -->
                        <div class="tab-pane fade show active" id="pane-gen-e">
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>Tipo de Persona <span style="color:red">*</span></label>
                                        <asp:DropDownList ID="ddlTipoPersonaEdit" runat="server" CssClass="form-control"
                                            onchange="onTipoPersonaChangeEdit()">
                                            <asp:ListItem Value="">-- Seleccionar --</asp:ListItem>
                                            <asp:ListItem Value="Fisica">Persona F&iacute;sica</asp:ListItem>
                                            <asp:ListItem Value="Moral">Persona Moral</asp:ListItem>
                                        </asp:DropDownList>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-12">
                                    <div class="form-group">
                                        <label>Nombre Comercial <span style="color:red">*</span></label>
                                        <asp:TextBox ID="txtNombreEdit" runat="server" CssClass="form-control" MaxLength="200"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>Contacto</label>
                                        <asp:TextBox ID="txtContactoEdit" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>Tel&eacute;fono</label>
                                        <asp:TextBox ID="txtTelefonoEdit" runat="server" CssClass="form-control" MaxLength="20"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>Email</label>
                                        <asp:TextBox ID="txtEmailEdit" runat="server" CssClass="form-control" MaxLength="100" TextMode="Email"></asp:TextBox>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>P&aacute;gina Web</label>
                                        <asp:TextBox ID="txtPaginaWebEdit" runat="server" CssClass="form-control" MaxLength="200"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>Tipo de Empresa</label>
                                        <asp:DropDownList ID="ddlTipoEmpresaEdit" runat="server" CssClass="form-control">
                                            <asp:ListItem Value="">-- Seleccionar --</asp:ListItem>
                                            <asp:ListItem Value="Matriz">Matriz</asp:ListItem>
                                            <asp:ListItem Value="Sucursal">Sucursal</asp:ListItem>
                                        </asp:DropDownList>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>Nacionalidad</label>
                                        <asp:TextBox ID="txtNacionalidadEdit" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- ── TAB FISCAL ── -->
                        <div class="tab-pane fade" id="pane-fis-e">
                            <div class="row">
                                <div class="col-md-12">
                                    <div class="form-group">
                                        <label>Raz&oacute;n Social Fiscal</label>
                                        <asp:TextBox ID="txtRazonSocialFiscalEdit" runat="server" CssClass="form-control"
                                            Placeholder="Nombre exacto registrado en el SAT" MaxLength="300"></asp:TextBox>
                                        <div class="field-hint"><i class="fas fa-info-circle"></i> Debe coincidir exactamente con el SAT para validar sus facturas.</div>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-8">
                                    <div class="form-group">
                                        <label>RFC</label>
                                        <div class="input-group">
                                            <asp:TextBox ID="txtRFCEdit" runat="server" CssClass="form-control"
                                                Placeholder="Ej: ABC010101XYZ" MaxLength="13"
                                                style="text-transform:uppercase;"></asp:TextBox>
                                            <div class="input-group-append">
                                                <button type="button" class="btn btn-outline-primary btn-validar"
                                                    onclick="validarRFC('<%= txtRFCEdit.ClientID %>', '<%= ddlTipoPersonaEdit.ClientID %>')">
                                                    <i class="fas fa-check-circle"></i> Validar RFC
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-12">
                                    <div class="form-group">
                                        <label>R&eacute;gimen Fiscal</label>
                                        <asp:DropDownList ID="ddlRegimenFiscalEdit" runat="server" CssClass="form-control">
                                            <asp:ListItem Value="">-- Seleccionar --</asp:ListItem>
                                            <asp:ListItem Value="601" data-tipo="Moral">601 - General de Ley Personas Morales</asp:ListItem>
                                            <asp:ListItem Value="603" data-tipo="Moral">603 - Personas Morales con Fines no Lucrativos</asp:ListItem>
                                            <asp:ListItem Value="620" data-tipo="Moral">620 - Sociedades Cooperativas de Producci&oacute;n</asp:ListItem>
                                            <asp:ListItem Value="623" data-tipo="Moral">623 - Opcional para Grupos de Sociedades</asp:ListItem>
                                            <asp:ListItem Value="624" data-tipo="Moral">624 - Coordinados</asp:ListItem>
                                            <asp:ListItem Value="605" data-tipo="Fisica">605 - Sueldos y Salarios</asp:ListItem>
                                            <asp:ListItem Value="606" data-tipo="Fisica">606 - Arrendamiento</asp:ListItem>
                                            <asp:ListItem Value="608" data-tipo="Fisica">608 - Dem&aacute;s ingresos</asp:ListItem>
                                            <asp:ListItem Value="611" data-tipo="Fisica">611 - Ingresos por Dividendos</asp:ListItem>
                                            <asp:ListItem Value="612" data-tipo="Fisica">612 - Actividades Empresariales y Profesionales</asp:ListItem>
                                            <asp:ListItem Value="614" data-tipo="Fisica">614 - Ingresos por intereses</asp:ListItem>
                                            <asp:ListItem Value="621" data-tipo="Fisica">621 - Incorporaci&oacute;n Fiscal</asp:ListItem>
                                            <asp:ListItem Value="625" data-tipo="Fisica">625 - Plataformas Tecnol&oacute;gicas</asp:ListItem>
                                            <asp:ListItem Value="616" data-tipo="Ambos">616 - Sin obligaciones fiscales</asp:ListItem>
                                            <asp:ListItem Value="622" data-tipo="Ambos">622 - Act. Agr&iacute;colas, Ganaderas, Silv&iacute;colas y Pesqueras</asp:ListItem>
                                            <asp:ListItem Value="626" data-tipo="Ambos">626 - RESICO</asp:ListItem>
                                        </asp:DropDownList>
                                    </div>
                                </div>
                            </div>
                            <div class="row" id="rowCurpEdit">
                                <div class="col-md-8">
                                    <div class="form-group">
                                        <label>CURP <small class="text-muted">(solo Personas F&iacute;sicas)</small></label>
                                        <div class="input-group">
                                            <asp:TextBox ID="txtCURPEdit" runat="server" CssClass="form-control"
                                                Placeholder="18 caracteres" MaxLength="18"
                                                style="text-transform:uppercase;"></asp:TextBox>
                                            <div class="input-group-append">
                                                <button type="button" class="btn btn-outline-secondary btn-validar"
                                                    onclick="validarCURP('<%= txtCURPEdit.ClientID %>')">
                                                    <i class="fas fa-check-circle"></i> Validar CURP
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- ── TAB BANCARIO ── -->
                        <div class="tab-pane fade" id="pane-ban-e">
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>Banco</label>
                                        <asp:TextBox ID="txtBancoEdit" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>CLABE Interbancaria</label>
                                        <asp:TextBox ID="txtCLABEEdit" runat="server" CssClass="form-control" MaxLength="18"></asp:TextBox>
                                        <div class="field-hint"><i class="fas fa-info-circle"></i> 18 d&iacute;gitos num&eacute;ricos para transferencias.</div>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>N&uacute;mero de Cuenta</label>
                                        <asp:TextBox ID="txtCuentaBancariaEdit" runat="server" CssClass="form-control" MaxLength="30"></asp:TextBox>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>Titular de la Cuenta</label>
                                        <asp:TextBox ID="txtTitularCuentaEdit" runat="server" CssClass="form-control" MaxLength="200"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <hr />
                            <div class="row">
                                <div class="col-md-4">
                                    <div class="form-group">
                                        <label>D&iacute;as de Cr&eacute;dito</label>
                                        <asp:TextBox ID="txtDiasCreditoEdit" runat="server" CssClass="form-control" MaxLength="5" TextMode="Number"></asp:TextBox>
                                    </div>
                                </div>
                                <div class="col-md-4">
                                    <div class="form-group">
                                        <label>L&iacute;mite de Cr&eacute;dito <small class="text-muted">($ MXN)</small></label>
                                        <asp:TextBox ID="txtLimiteCreditoEdit" runat="server" CssClass="form-control" MaxLength="15" TextMode="Number"></asp:TextBox>
                                        <div class="field-hint">Ingresar en pesos mexicanos.</div>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- ── TAB DIRECCIÓN ── -->
                        <div class="tab-pane fade" id="pane-dir-e">
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>Pa&iacute;s</label>
                                        <asp:TextBox ID="txtPaisEdit" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>C&oacute;digo Postal</label>
                                        <asp:TextBox ID="txtCodigoPostalEdit" runat="server" CssClass="form-control" MaxLength="10"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>Estado</label>
                                        <asp:TextBox ID="txtEstadoEdit" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>Municipio / Alcald&iacute;a</label>
                                        <asp:TextBox ID="txtMunicipioEdit" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-12">
                                    <div class="form-group">
                                        <label>Colonia</label>
                                        <asp:TextBox ID="txtColoniaEdit" runat="server" CssClass="form-control" MaxLength="150"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>N&uacute;m. Exterior</label>
                                        <asp:TextBox ID="txtNumExtEdit" runat="server" CssClass="form-control" MaxLength="20"></asp:TextBox>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label>N&uacute;m. Interior</label>
                                        <asp:TextBox ID="txtNumIntEdit" runat="server" CssClass="form-control" MaxLength="20"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-12">
                                    <div class="form-group">
                                        <label>Referencia</label>
                                        <asp:TextBox ID="txtReferenciaEdit" runat="server" CssClass="form-control" MaxLength="300"></asp:TextBox>
                                    </div>
                                </div>
                            </div>
                        </div>

                    </div><!-- /tab-content -->
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

    <!-- ══════════════════════════════════════════════════════════════════════
         MODAL MATERIALES DEL PROVEEDOR
    ══════════════════════════════════════════════════════════════════════ -->
    <div class="modal fade" id="modalMateriales" tabindex="-1" role="dialog" data-backdrop="static">
        <div class="modal-dialog" style="max-width:900px;" role="document">
            <div class="modal-content">
                <div class="modal-header" style="background-color:#003366;color:white;">
                    <h5 class="modal-title">
                        <i class="fas fa-boxes"></i>
                        Materiales de <asp:Label ID="lblProveedorNombreMat" runat="server" Text=""></asp:Label>
                    </h5>
                    <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
                </div>
                <div class="modal-body">

                    <%-- ── AGREGAR MATERIAL NUEVO ── --%>
                    <div class="card mb-3">
                        <div class="card-header" style="background:#eaf2f8;color:#003366;font-weight:600;font-size:.9rem;">
                            <i class="fas fa-plus-circle"></i> Agregar material a este proveedor
                        </div>
                        <div class="card-body pb-2">
                            <div class="row align-items-end">
                                <div class="col-md-6">
                                    <div class="form-group mb-2">
                                        <label style="font-size:.85rem;font-weight:600;">Material <span style="color:red">*</span></label>
                                        <asp:DropDownList ID="ddlMaterialAgregar" runat="server" CssClass="form-control form-control-sm">
                                            <asp:ListItem Value="">-- Seleccionar --</asp:ListItem>
                                        </asp:DropDownList>
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="form-group mb-2">
                                        <label style="font-size:.85rem;font-weight:600;">Precio inicial ($) <span style="color:red">*</span></label>
                                        <div class="input-group input-group-sm">
                                            <div class="input-group-prepend"><span class="input-group-text">$</span></div>
                                            <asp:TextBox ID="txtPrecioAgregar" runat="server" CssClass="form-control"
                                                TextMode="Number" Placeholder="0.0000" min="0" step="0.0001"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>
                                <div class="col-md-3 pb-2">
                                    <button type="button" class="btn btn-success btn-sm btn-block"
                                        onclick="confirmarAgregarMaterial()">
                                        <i class="fas fa-plus"></i> Agregar
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>

                    <%-- ── LISTA DE MATERIALES ── --%>
                    <div class="table-responsive">
                        <asp:GridView ID="gvMaterialesProveedor" runat="server"
                            AutoGenerateColumns="False"
                            CssClass="table table-bordered table-sm"
                            EmptyDataText="Este proveedor no tiene materiales registrados."
                            EmptyDataRowStyle-CssClass="text-center text-muted">
                            <Columns>
                                <asp:BoundField DataField="ProveedorMaterialID" Visible="false" />
                                <asp:BoundField DataField="Codigo"        HeaderText="Código" ItemStyle-Width="90px" />
                                <asp:BoundField DataField="Material"      HeaderText="Material" />
                                <asp:BoundField DataField="Unidad"        HeaderText="Unidad base" ItemStyle-Width="90px" />
                                <asp:BoundField DataField="PrecioVigente" HeaderText="Precio vigente ($)"
                                    DataFormatString="{0:N4}" ItemStyle-Width="130px" ItemStyle-CssClass="text-right font-weight-bold" />
                                <asp:BoundField DataField="FechaVigente"  HeaderText="Vigente desde"
                                    DataFormatString="{0:dd/MM/yyyy}" ItemStyle-Width="110px" />
                                <asp:TemplateField HeaderText="Estado" ItemStyle-Width="80px" ItemStyle-CssClass="text-center">
                                    <ItemTemplate>
                                        <span class="badge badge-<%# Convert.ToBoolean(Eval("Activo")) ? "success" : "secondary" %>">
                                            <%# Convert.ToBoolean(Eval("Activo")) ? "Activo" : "Inactivo" %>
                                        </span>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Acciones" ItemStyle-Width="230px">
                                    <ItemTemplate>
                                        <button type="button" class="btn btn-warning btn-sm"
                                            onclick="abrirModalActualizarPrecio('<%# Eval("ProveedorMaterialID") %>','<%# Server.HtmlEncode((Eval("Material") ?? "").ToString()) %>')">
                                            <i class="fas fa-dollar-sign"></i> Precio
                                        </button>
                                        <button type="button" class="btn btn-secondary btn-sm"
                                            onclick="verHistorial('<%# Eval("ProveedorMaterialID") %>')">
                                            <i class="fas fa-history"></i> Historial
                                        </button>
                                        <button type="button" class="btn btn-<%# Convert.ToBoolean(Eval("Activo")) ? "outline-danger" : "outline-success" %> btn-sm"
                                            onclick="confirmarToggleMaterial('<%# Eval("ProveedorMaterialID") %>','<%# Server.HtmlEncode((Eval("Material") ?? "").ToString()) %>',<%# Eval("Activo").ToString().ToLower() %>)">
                                            <%# Convert.ToBoolean(Eval("Activo")) ? "Desactivar" : "Activar" %>
                                        </button>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </div>

                    <%-- ── HISTORIAL DE PRECIOS (inline, oculto por default) ── --%>
                    <div id="divHistorial" style="display:none;" class="mt-3">
                        <h6 style="color:#003366;">
                            <i class="fas fa-history"></i>
                            Historial de precios: <strong><asp:Label ID="lblHistorialMaterial" runat="server" Text=""></asp:Label></strong>
                        </h6>
                        <div class="table-responsive">
                            <asp:GridView ID="gvHistorial" runat="server"
                                AutoGenerateColumns="False"
                                CssClass="table table-bordered table-sm table-striped"
                                EmptyDataText="Sin historial de precios registrado."
                                EmptyDataRowStyle-CssClass="text-center text-muted">
                                <Columns>
                                    <asp:BoundField DataField="FechaInicio"    HeaderText="Vigente desde"
                                        DataFormatString="{0:dd/MM/yyyy}" ItemStyle-Width="120px" />
                                    <asp:BoundField DataField="Precio"         HeaderText="Precio ($)"
                                        DataFormatString="{0:N4}" ItemStyle-Width="120px" ItemStyle-CssClass="text-right" />
                                    <asp:BoundField DataField="RegistradoPor"  HeaderText="Registrado por" />
                                    <asp:BoundField DataField="Notas"          HeaderText="Notas" />
                                </Columns>
                            </asp:GridView>
                        </div>
                        <button type="button" class="btn btn-secondary btn-sm mt-1"
                            onclick="document.getElementById('divHistorial').style.display='none';">
                            <i class="fas fa-times"></i> Cerrar historial
                        </button>
                    </div>

                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-dismiss="modal">Cerrar</button>
                </div>
            </div>
        </div>
    </div>

    <!-- ══════════════════════════════════════════════════════════════════════
         SUB-MODAL: ACTUALIZAR PRECIO
    ══════════════════════════════════════════════════════════════════════ -->
    <div class="modal fade" id="modalPrecio" tabindex="-1" role="dialog" data-backdrop="static">
        <div class="modal-dialog" style="max-width:400px;" role="document">
            <div class="modal-content">
                <div class="modal-header" style="background-color:#d35400;color:white;">
                    <h5 class="modal-title"><i class="fas fa-dollar-sign"></i> Nuevo precio</h5>
                    <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
                </div>
                <div class="modal-body">
                    <p class="small text-muted mb-2">
                        Material: <strong id="spnMaterialPrecio"></strong>
                    </p>
                    <div class="form-group">
                        <label style="font-size:.85rem;font-weight:600;">Precio nuevo ($) <span style="color:red">*</span></label>
                        <div class="input-group input-group-sm">
                            <div class="input-group-prepend"><span class="input-group-text">$</span></div>
                            <input type="number" id="txtNuevoPrecioInput" class="form-control"
                                min="0" step="0.0001" placeholder="0.0000" />
                        </div>
                    </div>
                    <div class="form-group">
                        <label style="font-size:.85rem;font-weight:600;">Fecha de vigencia <span style="color:red">*</span></label>
                        <input type="date" id="txtFechaVigenciaInput" class="form-control form-control-sm" />
                        <small class="text-muted">El precio será vigente a partir de esta fecha.</small>
                    </div>
                    <div class="form-group mb-0">
                        <label style="font-size:.85rem;font-weight:600;">Notas <small class="text-muted">(opcional)</small></label>
                        <textarea id="txtNotaPrecioInput" class="form-control form-control-sm" rows="2" maxlength="300"
                            placeholder="Ej: Aumento por inflación, precio de temporada..."></textarea>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-warning" onclick="guardarNuevoPrecio()">
                        <i class="fas fa-save"></i> Guardar precio
                    </button>
                    <button type="button" class="btn btn-secondary" onclick="cerrarModalPrecio()">Cancelar</button>
                </div>
            </div>
        </div>
    </div>

    <script>
        // ── Mensaje pendiente (postback) ───────────────────────────────────────
        window.addEventListener('load', function () {
            var hdnMsg = document.getElementById('<%= hdnMensajePendiente.ClientID %>');
            if (!hdnMsg) return;
            var raw = hdnMsg.value;
            if (!raw || raw === '') return;
            try {
                var msg = JSON.parse(raw);
                hdnMsg.value = '';

                // icon === '' → apertura silenciosa de modal (sin SweetAlert)
                if (msg.icon === '') {
                    if (msg.modal) $('#' + msg.modal).modal('show');
                    if (msg.showHistorial) {
                        document.getElementById('divHistorial').style.display = 'block';
                    }
                    return;
                }

                var opts = { icon: msg.icon, title: msg.title, text: msg.text, confirmButtonColor: '#003366' };
                if (msg.icon === 'success') { opts.showConfirmButton = false; opts.timer = 2000; }
                if (msg.modal) {
                    opts.showConfirmButton = true;
                    Swal.fire(opts).then(function () {
                        $('#' + msg.modal).modal('show');
                        if (msg.showHistorial) {
                            document.getElementById('divHistorial').style.display = 'block';
                        }
                    });
                } else {
                    Swal.fire(opts);
                }
            } catch (e) { console.log('Error mensaje:', e); }
        });

        // ── Abrir modales ──────────────────────────────────────────────────────
        function abrirModalNuevo() {
            $('#tabsNuevo a[href="#pane-gen-n"]').tab('show');
            document.getElementById('rowCurp').style.display = '';
            $('#modalNuevo').modal('show');
        }

        function abrirModalEditar(id, nombre, contacto, telefono, email, paginaWeb, tipoEmpresa,
            nacionalidad, tipoPersona, razonSocialFiscal, rfc, regimenFiscal,
            curp, banco, clabe, cuentaBancaria, titularCuenta,
            diasCredito, limiteCredito,
            pais, codigoPostal, estado, municipio, colonia, numExt, numInt, referencia) {

            document.getElementById('<%= hdnProveedorID.ClientID %>').value              = id;
            document.getElementById('<%= txtNombreEdit.ClientID %>').value               = nombre;
            document.getElementById('<%= txtContactoEdit.ClientID %>').value             = contacto;
            document.getElementById('<%= txtTelefonoEdit.ClientID %>').value             = telefono;
            document.getElementById('<%= txtEmailEdit.ClientID %>').value                = email;
            document.getElementById('<%= txtPaginaWebEdit.ClientID %>').value            = paginaWeb;
            setDdl('<%= ddlTipoEmpresaEdit.ClientID %>', tipoEmpresa);
            document.getElementById('<%= txtNacionalidadEdit.ClientID %>').value         = nacionalidad;
            setDdl('<%= ddlTipoPersonaEdit.ClientID %>', tipoPersona);
            document.getElementById('<%= txtRazonSocialFiscalEdit.ClientID %>').value    = razonSocialFiscal;
            document.getElementById('<%= txtRFCEdit.ClientID %>').value                  = rfc;
            setDdl('<%= ddlRegimenFiscalEdit.ClientID %>', regimenFiscal);
            document.getElementById('<%= txtCURPEdit.ClientID %>').value                 = curp;
            document.getElementById('<%= txtBancoEdit.ClientID %>').value                = banco;
            document.getElementById('<%= txtCLABEEdit.ClientID %>').value                = clabe;
            document.getElementById('<%= txtCuentaBancariaEdit.ClientID %>').value       = cuentaBancaria;
            document.getElementById('<%= txtTitularCuentaEdit.ClientID %>').value        = titularCuenta;
            document.getElementById('<%= txtDiasCreditoEdit.ClientID %>').value          = diasCredito;
            document.getElementById('<%= txtLimiteCreditoEdit.ClientID %>').value        = limiteCredito;
            document.getElementById('<%= txtPaisEdit.ClientID %>').value                 = pais;
            document.getElementById('<%= txtCodigoPostalEdit.ClientID %>').value         = codigoPostal;
            document.getElementById('<%= txtEstadoEdit.ClientID %>').value               = estado;
            document.getElementById('<%= txtMunicipioEdit.ClientID %>').value            = municipio;
            document.getElementById('<%= txtColoniaEdit.ClientID %>').value              = colonia;
            document.getElementById('<%= txtNumExtEdit.ClientID %>').value               = numExt;
            document.getElementById('<%= txtNumIntEdit.ClientID %>').value               = numInt;
            document.getElementById('<%= txtReferenciaEdit.ClientID %>').value           = referencia;

            onTipoPersonaChangeEdit();
            $('#tabsEditar a[href="#pane-gen-e"]').tab('show');
            $('#modalEditar').modal('show');
        }

        function setDdl(id, value) {
            var sel = document.getElementById(id);
            if (!sel) return;
            for (var i = 0; i < sel.options.length; i++) {
                if (sel.options[i].value === value) { sel.selectedIndex = i; return; }
            }
            sel.selectedIndex = 0;
        }

        // ── Lógica TipoPersona ─────────────────────────────────────────────────
        function onTipoPersonaChangeNuevo() {
            var tipo = document.getElementById('<%= ddlTipoPersona.ClientID %>').value;
            document.getElementById('rowCurp').style.display = (tipo === 'Moral') ? 'none' : '';
            filtrarOpcionesEl(document.getElementById('<%= ddlRegimenFiscal.ClientID %>'), tipo);
        }

        function onTipoPersonaChangeEdit() {
            var tipo = document.getElementById('<%= ddlTipoPersonaEdit.ClientID %>').value;
            document.getElementById('rowCurpEdit').style.display = (tipo === 'Moral') ? 'none' : '';
            filtrarOpcionesEl(document.getElementById('<%= ddlRegimenFiscalEdit.ClientID %>'), tipo);
        }

        function filtrarOpcionesEl(sel, tipo) {
            if (!sel) return;
            for (var i = 0; i < sel.options.length; i++) {
                var opt = sel.options[i];
                var dt  = opt.getAttribute('data-tipo');
                if (!dt) continue;
                var visible = (tipo === '' || dt === 'Ambos' || dt === tipo);
                opt.style.display = visible ? '' : 'none';
                if (!visible && opt.selected) sel.selectedIndex = 0;
            }
        }

        // ── Validar RFC ────────────────────────────────────────────────────────
        function validarRFC(rfcFieldId, tipoDdlId) {
            var rfc  = document.getElementById(rfcFieldId).value.trim().toUpperCase();
            var tipo = tipoDdlId ? document.getElementById(tipoDdlId).value : '';
            document.getElementById(rfcFieldId).value = rfc;
            if (rfc === '') {
                Swal.fire({ icon: 'info', title: 'RFC vac&iacute;o', text: 'Ingrese un RFC para validar.', confirmButtonColor: '#003366' });
                return;
            }
            var regex = /^([A-ZÑ&]{3,4})(\d{2})(0[1-9]|1[0-2])([0-2]\d|3[01])([A-Z\d]{3})$/;
            var fmtOk = regex.test(rfc);
            var longOk = true;
            var msgLong = '';
            if (tipo === 'Moral'  && rfc.length !== 12) { longOk = false; msgLong = 'Para Persona Moral el RFC debe tener 12 caracteres.'; }
            if (tipo === 'Fisica' && rfc.length !== 13) { longOk = false; msgLong = 'Para Persona F&iacute;sica el RFC debe tener 13 caracteres.'; }
            if (fmtOk && longOk) {
                var desc = rfc.length === 12 ? 'Persona Moral (12 caracteres)' : 'Persona F&iacute;sica (13 caracteres)';
                Swal.fire({ icon: 'success', title: 'RFC v&aacute;lido', text: 'Formato correcto. ' + desc, confirmButtonColor: '#003366' });
            } else if (!longOk) {
                Swal.fire({ icon: 'warning', title: 'Longitud incorrecta', text: msgLong, confirmButtonColor: '#003366' });
            } else {
                Swal.fire({ icon: 'error', title: 'RFC inv&aacute;lido', text: 'El RFC no cumple el formato del SAT.', confirmButtonColor: '#003366' });
            }
        }

        // ── Validar CURP ───────────────────────────────────────────────────────
        function validarCURP(curpFieldId) {
            var curp = document.getElementById(curpFieldId).value.trim().toUpperCase();
            document.getElementById(curpFieldId).value = curp;
            if (curp === '') {
                Swal.fire({ icon: 'info', title: 'CURP vac&iacute;a', text: 'Ingrese una CURP para validar.', confirmButtonColor: '#003366' });
                return;
            }
            var regex = /^[A-Z]{1}[AEIOU]{1}[A-Z]{2}\d{2}(0[1-9]|1[0-2])([0-2]\d|3[01])[HM]{1}[A-Z]{2}[B-DF-HJ-NP-TV-Z]{3}[0-9A-Z]{1}\d{1}$/;
            if (regex.test(curp)) {
                Swal.fire({ icon: 'success', title: 'CURP v&aacute;lida', text: 'El formato de la CURP es correcto.', confirmButtonColor: '#003366' });
            } else {
                Swal.fire({ icon: 'error', title: 'CURP inv&aacute;lida', text: 'El formato no es correcto (18 caracteres).', confirmButtonColor: '#003366' });
            }
        }

        // ── Validaciones de formulario ─────────────────────────────────────────
        function validarFormularioNuevo() {
            var nombre = document.getElementById('<%= txtNombre.ClientID %>').value.trim();
            var telef  = document.getElementById('<%= txtTelefono.ClientID %>').value.trim();
            var clabe  = document.getElementById('<%= txtCLABE.ClientID %>').value.trim();
            if (nombre === '') {
                $('#tabsNuevo a[href="#pane-gen-n"]').tab('show');
                Swal.fire({ icon: 'warning', title: 'Campo obligatorio', text: 'El nombre del proveedor es obligatorio.', confirmButtonColor: '#003366' });
                return false;
            }
            if (nombre.length < 2) {
                $('#tabsNuevo a[href="#pane-gen-n"]').tab('show');
                Swal.fire({ icon: 'warning', title: 'Nombre muy corto', text: 'El nombre debe tener al menos 2 caracteres.', confirmButtonColor: '#003366' });
                return false;
            }
            if (telef !== '' && !/^\d{7,20}$/.test(telef)) {
                $('#tabsNuevo a[href="#pane-gen-n"]').tab('show');
                Swal.fire({ icon: 'warning', title: 'Tel&eacute;fono inv&aacute;lido', text: 'El tel&eacute;fono debe contener solo n&uacute;meros (7 a 20 d&iacute;gitos).', confirmButtonColor: '#003366' });
                return false;
            }
            if (clabe !== '' && !/^\d{18}$/.test(clabe)) {
                $('#tabsNuevo a[href="#pane-ban-n"]').tab('show');
                Swal.fire({ icon: 'warning', title: 'CLABE inv&aacute;lida', text: 'La CLABE interbancaria debe tener exactamente 18 d&iacute;gitos num&eacute;ricos.', confirmButtonColor: '#003366' });
                return false;
            }
            return true;
        }

        function validarFormularioEditar() {
            var nombre = document.getElementById('<%= txtNombreEdit.ClientID %>').value.trim();
            var telef  = document.getElementById('<%= txtTelefonoEdit.ClientID %>').value.trim();
            var clabe  = document.getElementById('<%= txtCLABEEdit.ClientID %>').value.trim();
            if (nombre === '') {
                $('#tabsEditar a[href="#pane-gen-e"]').tab('show');
                Swal.fire({ icon: 'warning', title: 'Campo obligatorio', text: 'El nombre del proveedor es obligatorio.', confirmButtonColor: '#003366' });
                return false;
            }
            if (nombre.length < 2) {
                $('#tabsEditar a[href="#pane-gen-e"]').tab('show');
                Swal.fire({ icon: 'warning', title: 'Nombre muy corto', text: 'El nombre debe tener al menos 2 caracteres.', confirmButtonColor: '#003366' });
                return false;
            }
            if (telef !== '' && !/^\d{7,20}$/.test(telef)) {
                $('#tabsEditar a[href="#pane-gen-e"]').tab('show');
                Swal.fire({ icon: 'warning', title: 'Tel&eacute;fono inv&aacute;lido', text: 'El tel&eacute;fono debe contener solo n&uacute;meros (7 a 20 d&iacute;gitos).', confirmButtonColor: '#003366' });
                return false;
            }
            if (clabe !== '' && !/^\d{18}$/.test(clabe)) {
                $('#tabsEditar a[href="#pane-ban-e"]').tab('show');
                Swal.fire({ icon: 'warning', title: 'CLABE inv&aacute;lida', text: 'La CLABE interbancaria debe tener exactamente 18 d&iacute;gitos num&eacute;ricos.', confirmButtonColor: '#003366' });
                return false;
            }
            return true;
        }

        // ══ FUNCIONES MÓDULO MATERIALES POR PROVEEDOR ══════════════════════════

        var _proveedorMaterialIDActual = 0;

        // ── Abrir modal de materiales de un proveedor ──────────────────────────
        function abrirModalMateriales(proveedorID, nombreProveedor) {
            document.getElementById('<%= hdnMaterialesProveedorID.ClientID %>').value = proveedorID;
            document.getElementById('divHistorial').style.display = 'none';
            document.getElementById('<%= hdnAccionMateriales.ClientID %>').value = 'cargar';
            __doPostBack('<%= btnAccionMateriales.UniqueID %>', '');
        }

        // ── Confirmar y disparar agregar material ──────────────────────────────
        function confirmarAgregarMaterial() {
            var mat    = document.getElementById('<%= ddlMaterialAgregar.ClientID %>').value;
            var precio = parseFloat(document.getElementById('<%= txtPrecioAgregar.ClientID %>').value);
            if (!mat) {
                Swal.fire({ icon: 'warning', title: 'Material requerido',
                    text: 'Seleccione un material de la lista.', confirmButtonColor: '#003366' })
                    .then(function () { $('#modalMateriales').modal('show'); });
                return;
            }
            if (isNaN(precio) || precio < 0) {
                Swal.fire({ icon: 'warning', title: 'Precio inválido',
                    text: 'Ingrese un precio inicial mayor o igual a cero.', confirmButtonColor: '#003366' })
                    .then(function () { $('#modalMateriales').modal('show'); });
                return;
            }
            // Disparar postback al botón de servidor invisible
            __doPostBack('<%= btnAgregarMaterial.UniqueID %>', '');
        }

        // ── Abrir sub-modal para actualizar precio ─────────────────────────────
        function abrirModalActualizarPrecio(proveedorMaterialID, materialNombre) {
            _proveedorMaterialIDActual = proveedorMaterialID;
            document.getElementById('spnMaterialPrecio').innerText = materialNombre;
            document.getElementById('txtNuevoPrecioInput').value   = '';
            document.getElementById('txtNotaPrecioInput').value    = '';
            // Fecha de vigencia por defecto = hoy
            var hoy = new Date();
            var iso = hoy.getFullYear() + '-' +
                      String(hoy.getMonth() + 1).padStart(2, '0') + '-' +
                      String(hoy.getDate()).padStart(2, '0');
            document.getElementById('txtFechaVigenciaInput').value = iso;
            $('#modalMateriales').modal('hide');
            $('#modalPrecio').modal('show');
        }

        function cerrarModalPrecio() {
            $('#modalPrecio').modal('hide');
            $('#modalMateriales').modal('show');
        }

        // ── Guardar nuevo precio (desde sub-modal) ─────────────────────────────
        function guardarNuevoPrecio() {
            var precio = parseFloat(document.getElementById('txtNuevoPrecioInput').value);
            var fecha  = document.getElementById('txtFechaVigenciaInput').value;
            var nota   = document.getElementById('txtNotaPrecioInput').value.trim();

            if (isNaN(precio) || precio < 0) {
                Swal.fire({ icon: 'warning', title: 'Precio inválido',
                    text: 'Ingrese un precio mayor o igual a cero.', confirmButtonColor: '#003366' })
                    .then(function () { $('#modalPrecio').modal('show'); });
                return;
            }
            if (!fecha) {
                Swal.fire({ icon: 'warning', title: 'Fecha requerida',
                    text: 'Ingrese la fecha de vigencia del precio.', confirmButtonColor: '#003366' })
                    .then(function () { $('#modalPrecio').modal('show'); });
                return;
            }
            document.getElementById('<%= hdnProveedorMaterialID.ClientID %>').value  = _proveedorMaterialIDActual;
            document.getElementById('<%= hdnNuevoPrecio.ClientID %>').value          = precio.toFixed(4);
            // Empacar fecha y nota separadas por pipe (nota máx 300 chars, no puede contener pipe)
            document.getElementById('<%= hdnNuevaNotaPrecio.ClientID %>').value      = fecha + '|' + nota;
            $('#modalPrecio').modal('hide');
            __doPostBack('<%= btnActualizarPrecio.UniqueID %>', '');
        }

        // ── Ver historial de precios de un material ────────────────────────────
        function verHistorial(proveedorMaterialID) {
            document.getElementById('<%= hdnProveedorMaterialID.ClientID %>').value = proveedorMaterialID;
            document.getElementById('<%= hdnAccionMateriales.ClientID %>').value    = 'historial';
            __doPostBack('<%= btnAccionMateriales.UniqueID %>', '');
        }

        // ── Confirmar y disparar toggle activo/inactivo de material ───────────
        function confirmarToggleMaterial(proveedorMaterialID, materialNombre, activo) {
            var accion = activo ? 'desactivar' : 'activar';
            Swal.fire({
                icon: activo ? 'warning' : 'question',
                title: '¿' + (activo ? 'Desactivar' : 'Activar') + ' material?',
                html: '¿Desea <b>' + accion + '</b> a <b>' + materialNombre + '</b> de este proveedor?',
                showCancelButton: true,
                confirmButtonText: 'Sí, ' + accion,
                cancelButtonText: 'Cancelar',
                confirmButtonColor: activo ? '#e0a800' : '#28a745',
                cancelButtonColor: '#6c757d'
            }).then(function (r) {
                if (r.isConfirmed) {
                    document.getElementById('<%= hdnProveedorMaterialID.ClientID %>').value = proveedorMaterialID;
                    document.getElementById('<%= hdnAccionMateriales.ClientID %>').value    = 'toggle';
                    __doPostBack('<%= btnAccionMateriales.UniqueID %>', '');
                } else {
                    $('#modalMateriales').modal('show');
                }
            });
        }

        // ── Toggle Activo/Inactivo ─────────────────────────────────────────────
        function confirmarToggle(proveedorID, nombre, activo) {
            var accion = activo ? 'desactivar' : 'activar';
            Swal.fire({
                icon: activo ? 'warning' : 'question',
                title: '&iquest;' + (activo ? 'Desactivar' : 'Activar') + ' proveedor?',
                html: '&iquest;Est&aacute; seguro de <b>' + accion + '</b> a <b>' + nombre + '</b>?',
                showCancelButton: true,
                confirmButtonText: 'S&iacute;, ' + accion,
                cancelButtonText: 'Cancelar',
                confirmButtonColor: activo ? '#e0a800' : '#28a745',
                cancelButtonColor: '#6c757d'
            }).then(function (r) {
                if (r.isConfirmed) {
                    document.getElementById('<%= hdnToggleProveedorID.ClientID %>').value = proveedorID;
                    __doPostBack('<%= btnToggleHidden.UniqueID %>', '');
                }
            });
            return false;
        }
    </script>

</asp:Content>

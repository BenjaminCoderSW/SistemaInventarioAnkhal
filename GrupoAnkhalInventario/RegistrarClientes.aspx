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
                                    <asp:BoundField DataField="ClienteID"    HeaderText="ID"           Visible="false" />
                                    <asp:BoundField DataField="Nombre"       HeaderText="Nombre" />
                                    <asp:BoundField DataField="Contacto"     HeaderText="Contacto" />
                                    <asp:BoundField DataField="Telefono"     HeaderText="Tel&eacute;fono" />
                                    <asp:BoundField DataField="Email"        HeaderText="Email" />
                                    <asp:BoundField DataField="RFC"          HeaderText="RFC" />
                                    <asp:BoundField DataField="TipoPersona"  HeaderText="Tipo" />
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
                                                    '<%# Server.HtmlEncode((Eval("Nombre")           ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Contacto")         ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Telefono")         ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Email")            ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("PaginaWeb")        ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("TipoEmpresa")      ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Nacionalidad")     ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("TipoPersona")      ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("RazonSocialFiscal")?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("RFC")              ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("RegimenFiscal")    ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("UsoCFDI")          ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("CURP")             ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Pais")             ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("CodigoPostal")     ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Estado")           ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Municipio")        ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Colonia")          ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("NumExt")           ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("NumInt")           ?? "").ToString()) %>',
                                                    '<%# Server.HtmlEncode((Eval("Referencia")       ?? "").ToString()) %>',
                                                    '<%# Eval("DiasCredito")   ?? "" %>',
                                                    '<%# Eval("LimiteCredito") ?? "" %>'
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

    <asp:HiddenField ID="hdnToggleClienteID" runat="server" Value="" />
    <asp:Button ID="btnToggleHidden" runat="server" CssClass="d-none" OnClick="btnToggleHidden_Click" />
    <asp:HiddenField ID="hdnMensajePendiente" runat="server" Value="" />

    <!-- ══════════════════════════════════════════════════════════════════════
         MODAL NUEVO CLIENTE
    ══════════════════════════════════════════════════════════════════════ -->
    <div class="modal fade" id="modalNuevo" tabindex="-1" role="dialog" data-backdrop="static">
        <div class="modal-dialog modal-xl" role="document">
            <div class="modal-content">
                <div class="modal-header" style="background-color:#003366;color:white;">
                    <h5 class="modal-title"><i class="fas fa-user-plus"></i> Nuevo Cliente</h5>
                    <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
                </div>
                <div class="modal-body">
                    <ul class="nav nav-tabs mb-3" id="tabsNuevo" role="tablist">
                        <li class="nav-item"><a class="nav-link active" data-toggle="tab" href="#pane-gen-n"><i class="fas fa-user"></i> Datos Generales</a></li>
                        <li class="nav-item"><a class="nav-link" data-toggle="tab" href="#pane-fis-n"><i class="fas fa-file-invoice"></i> Datos Fiscales</a></li>
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
                                        <label>P&aacute;gina Web</label>
                                        <asp:TextBox ID="txtPaginaWeb" runat="server" CssClass="form-control"
                                            Placeholder="www.empresa.com" MaxLength="200"></asp:TextBox>
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
                            <div class="row">
                                <div class="col-md-4">
                                    <div class="form-group">
                                        <label>D&iacute;as de Cr&eacute;dito</label>
                                        <asp:TextBox ID="txtDiasCredito" runat="server" CssClass="form-control"
                                            Placeholder="Ej: 30" MaxLength="5" TextMode="Number"></asp:TextBox>
                                        <div class="field-hint">Plazo que le das al cliente para pagar.</div>
                                    </div>
                                </div>
                                <div class="col-md-4">
                                    <div class="form-group">
                                        <label>L&iacute;mite de Cr&eacute;dito <small class="text-muted">($ MXN)</small></label>
                                        <asp:TextBox ID="txtLimiteCredito" runat="server" CssClass="form-control"
                                            Placeholder="Ej: 50000.00" MaxLength="15" TextMode="Number"></asp:TextBox>
                                        <div class="field-hint">Monto m&aacute;ximo de venta a cr&eacute;dito. Ingresar en pesos mexicanos.</div>
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
                                        <div class="field-hint"><i class="fas fa-info-circle"></i> Debe coincidir exactamente con el SAT. Se usa en el CFDI.</div>
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
                            <div class="row">
                                <div class="col-md-12">
                                    <div class="form-group">
                                        <label>Uso CFDI Predeterminado</label>
                                        <asp:DropDownList ID="ddlUsoCFDI" runat="server" CssClass="form-control">
                                            <asp:ListItem Value="">-- Seleccionar --</asp:ListItem>
                                            <asp:ListItem Value="G01" data-tipo="Ambos">G01 - Adquisici&oacute;n de mercanc&iacute;as</asp:ListItem>
                                            <asp:ListItem Value="G02" data-tipo="Ambos">G02 - Devoluciones, descuentos o bonificaciones</asp:ListItem>
                                            <asp:ListItem Value="G03" data-tipo="Ambos">G03 - Gastos en general</asp:ListItem>
                                            <asp:ListItem Value="I01" data-tipo="Ambos">I01 - Construcciones</asp:ListItem>
                                            <asp:ListItem Value="I02" data-tipo="Ambos">I02 - Mobilario y equipo de oficina</asp:ListItem>
                                            <asp:ListItem Value="I03" data-tipo="Ambos">I03 - Equipo de transporte</asp:ListItem>
                                            <asp:ListItem Value="I04" data-tipo="Ambos">I04 - Equipo de c&oacute;mputo y accesorios</asp:ListItem>
                                            <asp:ListItem Value="I05" data-tipo="Ambos">I05 - Dados, troqueles, moldes y herramental</asp:ListItem>
                                            <asp:ListItem Value="I06" data-tipo="Ambos">I06 - Comunicaciones telef&oacute;nicas</asp:ListItem>
                                            <asp:ListItem Value="I07" data-tipo="Ambos">I07 - Comunicaciones satelitales</asp:ListItem>
                                            <asp:ListItem Value="I08" data-tipo="Ambos">I08 - Otra maquinaria y equipo</asp:ListItem>
                                            <asp:ListItem Value="D01" data-tipo="Fisica">D01 - Honorarios m&eacute;dicos y gastos hospitalarios</asp:ListItem>
                                            <asp:ListItem Value="D02" data-tipo="Fisica">D02 - Gastos m&eacute;dicos por incapacidad</asp:ListItem>
                                            <asp:ListItem Value="D03" data-tipo="Fisica">D03 - Gastos funerales</asp:ListItem>
                                            <asp:ListItem Value="D04" data-tipo="Fisica">D04 - Donativos</asp:ListItem>
                                            <asp:ListItem Value="D05" data-tipo="Fisica">D05 - Intereses por cr&eacute;ditos hipotecarios</asp:ListItem>
                                            <asp:ListItem Value="D06" data-tipo="Fisica">D06 - Aportaciones voluntarias al SAR</asp:ListItem>
                                            <asp:ListItem Value="D07" data-tipo="Fisica">D07 - Primas por seguros de gastos m&eacute;dicos</asp:ListItem>
                                            <asp:ListItem Value="D08" data-tipo="Fisica">D08 - Transportaci&oacute;n escolar obligatoria</asp:ListItem>
                                            <asp:ListItem Value="D09" data-tipo="Fisica">D09 - Dep&oacute;sitos en cuentas de ahorro y pensiones</asp:ListItem>
                                            <asp:ListItem Value="D10" data-tipo="Fisica">D10 - Pagos por servicios educativos</asp:ListItem>
                                            <asp:ListItem Value="S01" data-tipo="Ambos">S01 - Sin efectos fiscales</asp:ListItem>
                                            <asp:ListItem Value="CP01" data-tipo="Ambos">CP01 - Pagos</asp:ListItem>
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
                                        <label>C&oacute;digo Postal Fiscal <small class="text-muted">(domicilio SAT)</small></label>
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
                                            Placeholder="Ej: Entre calle X y calle Y, fachada azul" MaxLength="300"></asp:TextBox>
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
         MODAL EDITAR CLIENTE
    ══════════════════════════════════════════════════════════════════════ -->
    <div class="modal fade" id="modalEditar" tabindex="-1" role="dialog" data-backdrop="static">
        <div class="modal-dialog modal-xl" role="document">
            <div class="modal-content">
                <div class="modal-header" style="background-color:#003366;color:white;">
                    <h5 class="modal-title"><i class="fas fa-edit"></i> Editar Cliente</h5>
                    <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
                </div>
                <div class="modal-body">
                    <asp:HiddenField ID="hdnClienteID" runat="server" />
                    <ul class="nav nav-tabs mb-3" id="tabsEditar" role="tablist">
                        <li class="nav-item"><a class="nav-link active" data-toggle="tab" href="#pane-gen-e"><i class="fas fa-user"></i> Datos Generales</a></li>
                        <li class="nav-item"><a class="nav-link" data-toggle="tab" href="#pane-fis-e"><i class="fas fa-file-invoice"></i> Datos Fiscales</a></li>
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
                                        <asp:TextBox ID="txtContactoEdit" runat="server" CssClass="form-control" MaxLength="150"></asp:TextBox>
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
                                        <asp:TextBox ID="txtEmailEdit" runat="server" CssClass="form-control" MaxLength="150" TextMode="Email"></asp:TextBox>
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
                            <div class="row">
                                <div class="col-md-4">
                                    <div class="form-group">
                                        <label>D&iacute;as de Cr&eacute;dito</label>
                                        <asp:TextBox ID="txtDiasCreditoEdit" runat="server" CssClass="form-control" MaxLength="5" TextMode="Number"></asp:TextBox>
                                        <div class="field-hint">Plazo que le das al cliente para pagar.</div>
                                    </div>
                                </div>
                                <div class="col-md-4">
                                    <div class="form-group">
                                        <label>L&iacute;mite de Cr&eacute;dito <small class="text-muted">($ MXN)</small></label>
                                        <asp:TextBox ID="txtLimiteCreditoEdit" runat="server" CssClass="form-control" MaxLength="15" TextMode="Number"></asp:TextBox>
                                        <div class="field-hint">Monto m&aacute;ximo de venta a cr&eacute;dito. Ingresar en pesos mexicanos.</div>
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
                                        <div class="field-hint"><i class="fas fa-info-circle"></i> Debe coincidir exactamente con el SAT. Se usa en el CFDI.</div>
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
                            <div class="row">
                                <div class="col-md-12">
                                    <div class="form-group">
                                        <label>Uso CFDI Predeterminado</label>
                                        <asp:DropDownList ID="ddlUsoCFDIEdit" runat="server" CssClass="form-control">
                                            <asp:ListItem Value="">-- Seleccionar --</asp:ListItem>
                                            <asp:ListItem Value="G01" data-tipo="Ambos">G01 - Adquisici&oacute;n de mercanc&iacute;as</asp:ListItem>
                                            <asp:ListItem Value="G02" data-tipo="Ambos">G02 - Devoluciones, descuentos o bonificaciones</asp:ListItem>
                                            <asp:ListItem Value="G03" data-tipo="Ambos">G03 - Gastos en general</asp:ListItem>
                                            <asp:ListItem Value="I01" data-tipo="Ambos">I01 - Construcciones</asp:ListItem>
                                            <asp:ListItem Value="I02" data-tipo="Ambos">I02 - Mobilario y equipo de oficina</asp:ListItem>
                                            <asp:ListItem Value="I03" data-tipo="Ambos">I03 - Equipo de transporte</asp:ListItem>
                                            <asp:ListItem Value="I04" data-tipo="Ambos">I04 - Equipo de c&oacute;mputo y accesorios</asp:ListItem>
                                            <asp:ListItem Value="I05" data-tipo="Ambos">I05 - Dados, troqueles, moldes y herramental</asp:ListItem>
                                            <asp:ListItem Value="I06" data-tipo="Ambos">I06 - Comunicaciones telef&oacute;nicas</asp:ListItem>
                                            <asp:ListItem Value="I07" data-tipo="Ambos">I07 - Comunicaciones satelitales</asp:ListItem>
                                            <asp:ListItem Value="I08" data-tipo="Ambos">I08 - Otra maquinaria y equipo</asp:ListItem>
                                            <asp:ListItem Value="D01" data-tipo="Fisica">D01 - Honorarios m&eacute;dicos y gastos hospitalarios</asp:ListItem>
                                            <asp:ListItem Value="D02" data-tipo="Fisica">D02 - Gastos m&eacute;dicos por incapacidad</asp:ListItem>
                                            <asp:ListItem Value="D03" data-tipo="Fisica">D03 - Gastos funerales</asp:ListItem>
                                            <asp:ListItem Value="D04" data-tipo="Fisica">D04 - Donativos</asp:ListItem>
                                            <asp:ListItem Value="D05" data-tipo="Fisica">D05 - Intereses por cr&eacute;ditos hipotecarios</asp:ListItem>
                                            <asp:ListItem Value="D06" data-tipo="Fisica">D06 - Aportaciones voluntarias al SAR</asp:ListItem>
                                            <asp:ListItem Value="D07" data-tipo="Fisica">D07 - Primas por seguros de gastos m&eacute;dicos</asp:ListItem>
                                            <asp:ListItem Value="D08" data-tipo="Fisica">D08 - Transportaci&oacute;n escolar obligatoria</asp:ListItem>
                                            <asp:ListItem Value="D09" data-tipo="Fisica">D09 - Dep&oacute;sitos en cuentas de ahorro y pensiones</asp:ListItem>
                                            <asp:ListItem Value="D10" data-tipo="Fisica">D10 - Pagos por servicios educativos</asp:ListItem>
                                            <asp:ListItem Value="S01" data-tipo="Ambos">S01 - Sin efectos fiscales</asp:ListItem>
                                            <asp:ListItem Value="CP01" data-tipo="Ambos">CP01 - Pagos</asp:ListItem>
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
                                        <label>C&oacute;digo Postal Fiscal <small class="text-muted">(domicilio SAT)</small></label>
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
                var opts = { icon: msg.icon, title: msg.title, text: msg.text, confirmButtonColor: '#003366' };
                if (msg.icon === 'success') { opts.showConfirmButton = false; opts.timer = 2000; }
                if (msg.modal) {
                    opts.showConfirmButton = true;
                    Swal.fire(opts).then(function () { $('#' + msg.modal).modal('show'); });
                } else {
                    Swal.fire(opts);
                }
            } catch (e) { console.log('Error mensaje:', e); }
        });

        // ── Abrir modales ──────────────────────────────────────────────────────
        function abrirModalNuevo() {
            $('#tabsNuevo a[href="#pane-gen-n"]').tab('show');
            // Estado inicial: CURP visible (no hay tipo seleccionado)
            document.getElementById('rowCurp').style.display = '';
            $('#modalNuevo').modal('show');
        }

        function abrirModalEditar(id, nombre, contacto, telefono, email, paginaWeb, tipoEmpresa,
            nacionalidad, tipoPersona, razonSocialFiscal, rfc, regimenFiscal, usoCFDI,
            curp, pais, codigoPostal, estado, municipio, colonia, numExt, numInt, referencia,
            diasCredito, limiteCredito) {

            document.getElementById('<%= hdnClienteID.ClientID %>').value             = id;
            document.getElementById('<%= txtNombreEdit.ClientID %>').value             = nombre;
            document.getElementById('<%= txtContactoEdit.ClientID %>').value           = contacto;
            document.getElementById('<%= txtTelefonoEdit.ClientID %>').value           = telefono;
            document.getElementById('<%= txtEmailEdit.ClientID %>').value              = email;
            document.getElementById('<%= txtPaginaWebEdit.ClientID %>').value          = paginaWeb;
            setDdl('<%= ddlTipoEmpresaEdit.ClientID %>', tipoEmpresa);
            document.getElementById('<%= txtNacionalidadEdit.ClientID %>').value       = nacionalidad;
            setDdl('<%= ddlTipoPersonaEdit.ClientID %>', tipoPersona);
            document.getElementById('<%= txtRazonSocialFiscalEdit.ClientID %>').value  = razonSocialFiscal;
            document.getElementById('<%= txtRFCEdit.ClientID %>').value                = rfc;
            setDdl('<%= ddlRegimenFiscalEdit.ClientID %>', regimenFiscal);
            setDdl('<%= ddlUsoCFDIEdit.ClientID %>', usoCFDI);
            document.getElementById('<%= txtCURPEdit.ClientID %>').value               = curp;
            document.getElementById('<%= txtPaisEdit.ClientID %>').value               = pais;
            document.getElementById('<%= txtCodigoPostalEdit.ClientID %>').value       = codigoPostal;
            document.getElementById('<%= txtEstadoEdit.ClientID %>').value             = estado;
            document.getElementById('<%= txtMunicipioEdit.ClientID %>').value          = municipio;
            document.getElementById('<%= txtColoniaEdit.ClientID %>').value            = colonia;
            document.getElementById('<%= txtNumExtEdit.ClientID %>').value             = numExt;
            document.getElementById('<%= txtNumIntEdit.ClientID %>').value             = numInt;
            document.getElementById('<%= txtReferenciaEdit.ClientID %>').value         = referencia;
            document.getElementById('<%= txtDiasCreditoEdit.ClientID %>').value       = diasCredito;
            document.getElementById('<%= txtLimiteCreditoEdit.ClientID %>').value     = limiteCredito;

            // Aplicar filtros según el tipo de persona cargado
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
            filtrarOpcionesEl(document.getElementById('<%= ddlUsoCFDI.ClientID %>'), tipo);
        }

        function onTipoPersonaChangeEdit() {
            var tipo = document.getElementById('<%= ddlTipoPersonaEdit.ClientID %>').value;
            document.getElementById('rowCurpEdit').style.display = (tipo === 'Moral') ? 'none' : '';
            filtrarOpcionesEl(document.getElementById('<%= ddlRegimenFiscalEdit.ClientID %>'), tipo);
            filtrarOpcionesEl(document.getElementById('<%= ddlUsoCFDIEdit.ClientID %>'), tipo);
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
                Swal.fire({ icon: 'info', title: 'RFC vacío', text: 'Ingrese un RFC para validar.', confirmButtonColor: '#003366' });
                return;
            }
            var regex = /^([A-ZÑ&]{3,4})(\d{2})(0[1-9]|1[0-2])([0-2]\d|3[01])([A-Z\d]{3})$/;
            var fmtOk = regex.test(rfc);
            var longOk = true;
            var msgLong = '';
            if (tipo === 'Moral'  && rfc.length !== 12) { longOk = false; msgLong = 'Para Persona Moral el RFC debe tener 12 caracteres.'; }
            if (tipo === 'Fisica' && rfc.length !== 13) { longOk = false; msgLong = 'Para Persona Física el RFC debe tener 13 caracteres.'; }
            if (fmtOk && longOk) {
                var desc = rfc.length === 12 ? 'Persona Moral (12 caracteres)' : 'Persona Física (13 caracteres)';
                Swal.fire({ icon: 'success', title: 'RFC válido', text: 'Formato correcto. ' + desc, confirmButtonColor: '#003366' });
            } else if (!longOk) {
                Swal.fire({ icon: 'warning', title: 'Longitud incorrecta', text: msgLong, confirmButtonColor: '#003366' });
            } else {
                Swal.fire({ icon: 'error', title: 'RFC inválido', text: 'El RFC no cumple el formato del SAT.', confirmButtonColor: '#003366' });
            }
        }

        // ── Validar CURP ───────────────────────────────────────────────────────
        function validarCURP(curpFieldId) {
            var curp = document.getElementById(curpFieldId).value.trim().toUpperCase();
            document.getElementById(curpFieldId).value = curp;
            if (curp === '') {
                Swal.fire({ icon: 'info', title: 'CURP vacía', text: 'Ingrese una CURP para validar.', confirmButtonColor: '#003366' });
                return;
            }
            var regex = /^[A-Z]{1}[AEIOU]{1}[A-Z]{2}\d{2}(0[1-9]|1[0-2])([0-2]\d|3[01])[HM]{1}[A-Z]{2}[B-DF-HJ-NP-TV-Z]{3}[0-9A-Z]{1}\d{1}$/;
            if (regex.test(curp)) {
                Swal.fire({ icon: 'success', title: 'CURP válida', text: 'El formato de la CURP es correcto.', confirmButtonColor: '#003366' });
            } else {
                Swal.fire({ icon: 'error', title: 'CURP inválida', text: 'El formato no es correcto (18 caracteres).', confirmButtonColor: '#003366' });
            }
        }

        // ── Validaciones de formulario ─────────────────────────────────────────
        function validarFormularioNuevo() {
            var nombre = document.getElementById('<%= txtNombre.ClientID %>').value.trim();
            var telef  = document.getElementById('<%= txtTelefono.ClientID %>').value.trim();
            if (nombre === '') {
                $('#tabsNuevo a[href="#pane-gen-n"]').tab('show');
                Swal.fire({ icon: 'warning', title: 'Campo obligatorio', text: 'El nombre del cliente es obligatorio.', confirmButtonColor: '#003366' });
                return false;
            }
            if (nombre.length < 2) {
                $('#tabsNuevo a[href="#pane-gen-n"]').tab('show');
                Swal.fire({ icon: 'warning', title: 'Nombre muy corto', text: 'El nombre debe tener al menos 2 caracteres.', confirmButtonColor: '#003366' });
                return false;
            }
            if (telef !== '' && !/^\d{7,20}$/.test(telef)) {
                $('#tabsNuevo a[href="#pane-gen-n"]').tab('show');
                Swal.fire({ icon: 'warning', title: 'Teléfono inválido', text: 'El teléfono debe contener solo números (7 a 20 dígitos).', confirmButtonColor: '#003366' });
                return false;
            }
            return true;
        }

        function validarFormularioEditar() {
            var nombre = document.getElementById('<%= txtNombreEdit.ClientID %>').value.trim();
            var telef  = document.getElementById('<%= txtTelefonoEdit.ClientID %>').value.trim();
            if (nombre === '') {
                $('#tabsEditar a[href="#pane-gen-e"]').tab('show');
                Swal.fire({ icon: 'warning', title: 'Campo obligatorio', text: 'El nombre del cliente es obligatorio.', confirmButtonColor: '#003366' });
                return false;
            }
            if (nombre.length < 2) {
                $('#tabsEditar a[href="#pane-gen-e"]').tab('show');
                Swal.fire({ icon: 'warning', title: 'Nombre muy corto', text: 'El nombre debe tener al menos 2 caracteres.', confirmButtonColor: '#003366' });
                return false;
            }
            if (telef !== '' && !/^\d{7,20}$/.test(telef)) {
                $('#tabsEditar a[href="#pane-gen-e"]').tab('show');
                Swal.fire({ icon: 'warning', title: 'Teléfono inválido', text: 'El teléfono debe contener solo números (7 a 20 dígitos).', confirmButtonColor: '#003366' });
                return false;
            }
            return true;
        }

        // ── Toggle Activo/Inactivo ─────────────────────────────────────────────
        function confirmarToggle(clienteID, nombre, activo) {
            var accion = activo ? 'desactivar' : 'activar';
            Swal.fire({
                icon: activo ? 'warning' : 'question',
                title: '¿' + (activo ? 'Desactivar' : 'Activar') + ' cliente?',
                html: '¿Está seguro de <b>' + accion + '</b> a <b>' + nombre + '</b>?',
                showCancelButton: true,
                confirmButtonText: 'Sí, ' + accion,
                cancelButtonText: 'Cancelar',
                confirmButtonColor: activo ? '#e0a800' : '#28a745',
                cancelButtonColor: '#6c757d'
            }).then(function (r) {
                if (r.isConfirmed) {
                    document.getElementById('<%= hdnToggleClienteID.ClientID %>').value = clienteID;
                    __doPostBack('<%= btnToggleHidden.UniqueID %>', '');
                }
            });
            return false;
        }
    </script>

</asp:Content>

<%@ Page Title="Materiales" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Materiales.aspx.cs" Inherits="GrupoAnkhalInventario.Materiales" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="css/gridviewPantalla.css" rel="stylesheet" />
    <style>
        /* ── Dashboard de niveles ── */
        .stock-dashboard {
            display: flex;
            gap: 14px;
            margin-bottom: 18px;
            flex-wrap: wrap;
        }
        .stock-card {
            flex: 1;
            min-width: 160px;
            border-radius: 10px;
            padding: 16px 20px;
            color: #fff;
            display: flex;
            align-items: center;
            gap: 14px;
            box-shadow: 0 3px 10px rgba(0,0,0,0.15);
            cursor: pointer;
            transition: transform .15s, box-shadow .15s;
        }
        .stock-card:hover { transform: translateY(-3px); box-shadow: 0 6px 16px rgba(0,0,0,0.2); }
        .stock-card.critico  { background: linear-gradient(135deg,#c0392b,#e74c3c); }
        .stock-card.bajo     { background: linear-gradient(135deg,#d35400,#e67e22); }
        .stock-card.optimo   { background: linear-gradient(135deg,#1e8449,#27ae60); }
        .stock-card.total    { background: linear-gradient(135deg,#1a5276,#2980b9); }
        .stock-card.sin      { background: linear-gradient(135deg,#4a4a4a,#717171); }
        .stock-card .icon    { font-size: 2.2rem; opacity: .9; }
        .stock-card .info .num  { font-size: 2rem; font-weight: 700; line-height:1; }
        .stock-card .info .lbl  { font-size: .78rem; opacity: .9; text-transform: uppercase; letter-spacing:.5px; }

        /* ── Barra de nivel en la tabla ── */
        .nivel-badge {
            display: inline-flex;
            align-items: center;
            gap: 5px;
            padding: 3px 9px;
            border-radius: 12px;
            font-size: .78rem;
            font-weight: 600;
            white-space: nowrap;
        }
        .nivel-critico { background:#fdecea; color:#c0392b; border:1px solid #e74c3c; }
        .nivel-exceso  { background:#fef3e2; color:#d35400; border:1px solid #e67e22; }
        .nivel-optimo  { background:#eafaf1; color:#1e8449; border:1px solid #27ae60; }
        .nivel-sin     { background:#f2f3f4; color:#7f8c8d; border:1px solid #bdc3c7; }

        /* barra de progreso mini */
        .stock-bar-wrap { width:90px; height:8px; background:#e0e0e0; border-radius:4px; display:inline-block; vertical-align:middle; margin-left:5px; }
        .stock-bar-fill { height:100%; border-radius:4px; }

        /* ── Filtros ── */
        .filtros-bar {
            background:#f8f9fa; border:1px solid #dee2e6;
            border-radius:8px; padding:14px 18px; margin-bottom:14px;
        }
        .filtros-bar label { font-weight:600; font-size:.84rem; color:#003366; margin-bottom:2px; }

        /* ── Accordion de bases ── */
        .bases-accordion { background:#f0f4f8; border-radius:6px; padding:10px 14px; margin-top:6px; }
        .bases-accordion table { width:100%; font-size:.83rem; }
        .bases-accordion th { color:#003366; font-weight:600; padding:4px 8px; }
        .bases-accordion td { padding:4px 8px; }
        .bases-accordion tr:hover td { background:#e3eaf3; }

        /* ── Paginador ── */
        .pager-custom span {
            background:#003366; color:#fff; font-weight:700;
            border-radius:4px; padding:4px 9px;
        }
        .pager-custom a { padding:4px 9px; border-radius:4px; }

        .stock-card.activo-filtro { outline: 3px solid #fff; outline-offset: 2px; }

        /* ── Niveles por base ── */
        .btn-xs { padding: 2px 7px; font-size: .75rem; line-height: 1.4; border-radius: 3px; }
        .btn-editar-nivel { white-space: nowrap; }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
<div class="container-fluid">
<div class="row">
<div class="col-12">

    <!-- ══ DASHBOARD DE NIVELES ══════════════════════════════ -->
    <div class="stock-dashboard">
        <div class="stock-card total" onclick="filtrarNivel('')" id="cardTotal">
            <div class="icon"><i class="fas fa-boxes"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblTotal"   runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Total materiales</div>
            </div>
        </div>
        <div class="stock-card sin" onclick="filtrarNivel('sin')" id="cardSin">
            <div class="icon"><i class="fas fa-box-open"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblSin" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">⚪ Sin stock</div>
            </div>
        </div>
        <div class="stock-card critico" onclick="filtrarNivel('critico')" id="cardCritico">
            <div class="icon"><i class="fas fa-exclamation-circle"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblCritico" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">🔴 Stock bajo mínimo</div>
            </div>
        </div>
        <div class="stock-card optimo" onclick="filtrarNivel('optimo')" id="cardOptimo">
            <div class="icon"><i class="fas fa-check-circle"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblOptimo"  runat="server" Text="0"></asp:Label></div>
                <div class="lbl">🟢 Nivel saludable</div>
            </div>
        </div>
        <div class="stock-card bajo" onclick="filtrarNivel('exceso')" id="cardBajo">
            <div class="icon"><i class="fas fa-exclamation-triangle"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblBajo"    runat="server" Text="0"></asp:Label></div>
                <div class="lbl">🟡 Exceso de inventario</div>
            </div>
        </div>
    </div>

    <div class="card">
        <div class="card-header" style="background-color:#003366;color:white;">
            <h3 class="card-title"><i class="fas fa-cubes"></i> Materiales</h3>
        </div>
        <div class="card-body">

            <div class="mb-3">
                <asp:Button ID="btnNuevo" runat="server" Text="＋ Nuevo Material"
                    CssClass="btn btn-success"
                    OnClientClick="abrirModalNuevo(); return false;" />
            </div>

            <!-- ── FILTROS ── -->
            <div class="filtros-bar">
                <div class="row align-items-end">
                    <div class="col-md-3">
                        <label>Buscar por Descripción o Código</label>
                        <asp:TextBox ID="txtBuscar" runat="server" CssClass="form-control form-control-sm"
                            Placeholder="Descripción o código..."></asp:TextBox>
                    </div>
                    <div class="col-md-2">
                        <label>Tipo de material</label>
                        <asp:DropDownList ID="ddlFiltrTipo" runat="server" CssClass="form-control form-control-sm">
                            <asp:ListItem Value="">-- Todos --</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-2">
                        <label>Nivel de stock</label>
                        <asp:DropDownList ID="ddlFiltrNivel" runat="server" CssClass="form-control form-control-sm">
                            <asp:ListItem Value="">-- Todos --</asp:ListItem>
                            <asp:ListItem Value="sin">⚪ Sin stock</asp:ListItem>
                            <asp:ListItem Value="critico">🔴 Bajo mínimo</asp:ListItem>
                            <asp:ListItem Value="optimo">🟢 Nivel saludable</asp:ListItem>
                            <asp:ListItem Value="exceso">🟡 Exceso de inventario</asp:ListItem>
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
                        <asp:Button ID="btnLimpiar" runat="server" Text="✖ Limpiar"
                            CssClass="btn btn-secondary btn-sm" OnClick="btnLimpiar_Click" />
                    </div>
                </div>
            </div>

            <div class="mb-2">
                <small class="text-muted">
                    <asp:Label ID="lblResultados" runat="server"></asp:Label>
                </small>
            </div>

            <!-- ── GRID ── -->
            <div class="table-responsive">
                <asp:GridView ID="gvMateriales" runat="server" AutoGenerateColumns="False"
                    CssClass="table table-bordered table-striped custom-grid"
                    AllowPaging="True" AllowCustomPaging="True" PageSize="15"
                    OnPageIndexChanging="gvMateriales_PageIndexChanging"
                    OnRowDataBound="gvMateriales_RowDataBound"
                    DataKeyNames="MaterialID"
                    PagerStyle-CssClass="pager-custom"
                    PagerSettings-Mode="NumericFirstLast"
                    PagerSettings-FirstPageText="«"
                    PagerSettings-LastPageText="»"
                    PagerSettings-PageButtonCount="5">
                    <Columns>
                        <asp:BoundField DataField="MaterialID"   HeaderText="ID"          Visible="false" />
                        <asp:BoundField DataField="Codigo"       HeaderText="Código" />
                        <asp:BoundField DataField="Descripcion"  HeaderText="Descripción" />
                        <asp:BoundField DataField="TipoNombre"   HeaderText="Tipo" />
                        <asp:BoundField DataField="Subtipo"      HeaderText="Subtipo" />
                        <asp:BoundField DataField="UnidadNombre" HeaderText="Unidad" />
                        <asp:BoundField DataField="PrecioUnitario" HeaderText="Precio" DataFormatString="{0:C2}" />

                        <asp:TemplateField HeaderText="Stock Global">
                            <ItemTemplate>
                                <div>
                                    <span class='nivel-badge <%# GetNivelCss((decimal)Eval("StockGlobal"), (decimal)Eval("StockMinimo"), (decimal)Eval("StockMaximo"), (decimal)Eval("StockOptimo")) %>'>
                                        <%# GetNivelIcon((decimal)Eval("StockGlobal"), (decimal)Eval("StockMinimo"), (decimal)Eval("StockMaximo"), (decimal)Eval("StockOptimo")) %>
                                        <%# string.Format("{0:N2}", Eval("StockGlobal")) %> <%# Eval("UnidadClave") %>
                                    </span>
                                    <div class="stock-bar-wrap">
                                        <div class="stock-bar-fill <%# GetBarCss((decimal)Eval("StockGlobal"), (decimal)Eval("StockMinimo"), (decimal)Eval("StockMaximo"), (decimal)Eval("StockOptimo")) %>"
                                             style="width:<%# GetBarPct((decimal)Eval("StockGlobal"), (decimal)Eval("StockMaximo")) %>%; background:<%# GetBarColor((decimal)Eval("StockGlobal"), (decimal)Eval("StockMinimo"), (decimal)Eval("StockMaximo"), (decimal)Eval("StockOptimo")) %>">
                                        </div>
                                    </div>
                                </div>
                                <small class="text-muted" style="font-size:.72rem;">
                                    ⚪ Sin stock &nbsp;
                                    🔴&lt;<%# string.Format("{0:N2}", Eval("StockMinimo")) %> &nbsp;
                                    🟢≤<%# string.Format("{0:N2}", Eval("StockMaximo")) %> &nbsp;
                                    🟡&gt;<%# string.Format("{0:N2}", Eval("StockMaximo")) %>
                                </small>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Por Base">
                            <ItemTemplate>
                                <button type="button" class="btn btn-info btn-sm"
                                    onclick="toggleAcordeon('acc_<%# Eval("MaterialID") %>', this)">
                                    <i class="fas fa-layer-group"></i> Ver bases
                                </button>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Estatus">
                            <ItemTemplate>
                                <span class='badge badge-<%# Convert.ToBoolean(Eval("Activo")) ? "success" : "secondary" %>'>
                                    <%# Convert.ToBoolean(Eval("Activo")) ? "Activo" : "Inactivo" %>
                                </span>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Acciones">
                            <ItemTemplate>
                                <button type="button" class="btn btn-primary btn-sm"
                                    onclick="abrirModalEditar(
                                        '<%# Eval("MaterialID") %>',
                                        '<%# Eval("Codigo") %>',
                                        '<%# Server.HtmlEncode((Eval("Descripcion") ?? "").ToString()) %>',
                                        '<%# Eval("TipoMaterialID") %>',
                                        '<%# Server.HtmlEncode((Eval("Subtipo") ?? "").ToString()) %>',
                                        '<%# Eval("UnidadMedidaID") ?? 0 %>',
                                        '<%# Eval("PrecioUnitario") %>',
                                        '<%# Eval("StockMinimo") %>',
                                        '<%# Eval("StockMaximo") %>',
                                        '<%# Eval("StockOptimo") %>',
                                        '<%# RowVersionBase64(Eval("RowVersion")) %>',
                                        '<%# Eval("ProveedorPrincipalID") ?? "" %>'
                                    )">
                                    <i class="fas fa-edit"></i> Editar
                                </button>
                                <asp:Button ID="btnToggle" runat="server"
                                    CssClass='<%# Convert.ToBoolean(Eval("Activo")) ? "btn btn-warning btn-sm" : "btn btn-success btn-sm" %>'
                                    Text='<%# Convert.ToBoolean(Eval("Activo")) ? "Desactivar" : "Activar" %>'
                                    CommandArgument='<%# Eval("MaterialID") %>'
                                    OnClientClick='<%# "return confirmarToggle(\"" + Eval("MaterialID") + "\", \"" + Server.HtmlEncode((Eval("Descripcion") ?? "").ToString()) + "\", " + Eval("Activo").ToString().ToLower() + ");" %>'
                                    OnClick="btnToggle_Click" />
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>

        </div><!-- /card-body -->
    </div><!-- /card -->
</div>
</div>
</div>

<!-- ── HIDDEN FIELDS ────────────────────────────── -->
<asp:HiddenField ID="hdnToggleMaterialID" runat="server" Value="" />
<asp:Button    ID="btnToggleHidden"       runat="server" CssClass="d-none" OnClick="btnToggleHidden_Click" />
<asp:Button    ID="btnCargarConversiones" runat="server" CssClass="d-none" OnClick="btnCargarConversiones_Click" />
<asp:HiddenField ID="hdnMensajePendiente" runat="server" Value="" />
<asp:HiddenField ID="hdnNivelFiltro"      runat="server" Value="" />
<asp:HiddenField ID="hdnNivelBaseMaterialID" runat="server" Value="" />
<asp:HiddenField ID="hdnNivelBaseBaseID"     runat="server" Value="" />
<asp:HiddenField ID="hdnNivelBaseMinimo"     runat="server" Value="" />
<asp:HiddenField ID="hdnNivelBaseOptimo"     runat="server" Value="" />
<asp:HiddenField ID="hdnNivelBaseMaximo"     runat="server" Value="" />
<asp:Button ID="btnGuardarNivelBase" runat="server" CssClass="d-none"
    OnClick="btnGuardarNivelBase_Click" />

<!-- ══ PANEL FLOTANTE: Editar niveles por base ══════════════════════════════ -->
<div id="overlayEditorNivel" style="display:none;position:fixed;top:0;left:0;
     width:100%;height:100%;background:rgba(0,0,0,0.45);z-index:9998;"
     onclick="cerrarEditorNivel()"></div>
<div id="panelEditorNivel" style="display:none;position:fixed;top:50%;left:50%;
     transform:translate(-50%,-50%);z-index:9999;background:#fff;
     border:1px solid #003366;border-radius:10px;
     box-shadow:0 8px 30px rgba(0,0,0,0.25);padding:24px 28px;min-width:340px;">
    <h6 style="color:#003366;font-weight:700;margin-bottom:4px;">
        <i class="fas fa-sliders-h"></i> Niveles de stock por base
    </h6>
    <small id="spanNombreBaseEditor" class="text-muted d-block mb-1"></small>
    <p id="notaUnidadBase" class="small fw-semibold text-info mb-3" style="margin-top:0;"></p>
    <div class="form-group mb-2">
        <label style="font-size:.82rem;font-weight:600;">
            Stock mínimo <span class="text-danger">(🔴 por debajo = crítico)</span>
        </label>
        <input type="number" id="inpNivelMin" class="form-control form-control-sm"
               min="0" step="0.01" />
    </div>
    <div class="form-group mb-2">
        <label style="font-size:.82rem;font-weight:600;">
            Stock óptimo <span class="text-muted">(referencia para la barra de progreso)</span>
        </label>
        <input type="number" id="inpNivelOpt" class="form-control form-control-sm"
               min="0" step="0.01" />
    </div>
    <div class="form-group mb-2">
        <label style="font-size:.82rem;font-weight:600;">
            Stock máximo <span style="color:#d35400">(🟡 por encima = exceso)</span>
        </label>
        <input type="number" id="inpNivelMax" class="form-control form-control-sm"
               min="0" step="0.01" />
    </div>
    <small id="spanNivelHint" class="text-muted d-block mb-3" style="font-size:.76rem;"></small>
    <div class="d-flex justify-content-between">
        <button type="button" class="btn btn-success btn-sm"
                onclick="confirmarGuardarNivel()">
            <i class="fas fa-save"></i> Guardar
        </button>
        <button type="button" class="btn btn-secondary btn-sm"
                onclick="cerrarEditorNivel()">
            Cancelar
        </button>
    </div>
</div>

<!-- ══ MODAL NUEVO MATERIAL ═════════════════════════ -->
<div class="modal fade" id="modalNuevo" tabindex="-1" role="dialog" data-backdrop="static">
  <div class="modal-dialog modal-lg" role="document">
    <div class="modal-content">
      <div class="modal-header" style="background-color:#003366;color:white;">
        <h5 class="modal-title"><i class="fas fa-cubes"></i> Nuevo Material</h5>
        <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
      </div>
      <div class="modal-body">
        <!-- Fila 1: Código + Descripción -->
        <div class="row">
          <div class="col-md-3">
            <div class="form-group">
              <label>Código <span style="color:red">*</span></label>
              <asp:TextBox ID="txtCodigo" runat="server" CssClass="form-control" Placeholder="Ej: MAT-001" MaxLength="20"></asp:TextBox>
              <small class="text-muted">Se guardará en mayúsculas.</small>
            </div>
          </div>
          <div class="col-md-9">
            <div class="form-group">
              <label>Descripción <span style="color:red">*</span></label>
              <asp:TextBox ID="txtDescripcion" runat="server" CssClass="form-control" Placeholder="Descripción completa del material" MaxLength="200"></asp:TextBox>
            </div>
          </div>
        </div>
        <!-- Fila 2: Tipo + Subtipo + Unidad -->
        <div class="row">
          <div class="col-md-4">
            <div class="form-group">
              <label>Tipo de material <span style="color:red">*</span></label>
              <asp:DropDownList ID="ddlTipo" runat="server" CssClass="form-control"></asp:DropDownList>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Subtipo</label>
              <asp:TextBox ID="txtSubtipo" runat="server" CssClass="form-control" Placeholder="Opcional" MaxLength="50"></asp:TextBox>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Unidad de medida (base) <span style="color:red">*</span></label>
              <asp:DropDownList ID="ddlUnidad" runat="server" CssClass="form-control"></asp:DropDownList>
            </div>
          </div>
        </div>
        <!-- Fila 3: Proveedor + Precio -->
        <div class="row">
          <div class="col-md-6">
            <div class="form-group">
              <label>Proveedor principal</label>
              <asp:DropDownList ID="ddlProveedorPrincipal" runat="server" CssClass="form-control">
                <asp:ListItem Value="">-- Sin proveedor --</asp:ListItem>
              </asp:DropDownList>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Precio unitario <span style="color:red">*</span></label>
              <div class="input-group">
                <div class="input-group-prepend"><span class="input-group-text">$</span></div>
                <asp:TextBox ID="txtPrecio" runat="server" CssClass="form-control" Placeholder="0.00" TextMode="Number" min="0" step="0.01"></asp:TextBox>
              </div>
            </div>
          </div>
        </div>
        <hr />
        <h6 style="color:#003366;font-weight:600;"><i class="fas fa-layer-group"></i> Niveles de stock General en Ankhal</h6>
        <small class="text-muted d-block mb-1">
            Definen el semáforo: <span style="color:#c0392b">🔴 Bajo mínimo</span> si stock &lt; Mínimo &nbsp;|&nbsp;
            <span style="color:#d35400">🟡 Bajo máximo</span> si stock &lt; Máximo &nbsp;|&nbsp;
            <span style="color:#1e8449">🟢 Óptimo</span> si stock ≥ Óptimo
        </small>
        <p id="notaUnidadNuevo" class="small fw-semibold text-info mb-2" style="margin-top:0;">
            <i class="fas fa-info-circle"></i> Selecciona una unidad de medida para ver en qué unidad configurar los niveles.
        </p>
        <div class="row">
          <div class="col-md-4">
            <div class="form-group">
              <label>Stock mínimo <span style="color:red">*</span> <small class="text-danger">(🔴)</small></label>
              <asp:TextBox ID="txtStockMinimo" runat="server" CssClass="form-control" TextMode="Number" Text="0" min="0" step="0.01"></asp:TextBox>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Stock máximo <span style="color:red">*</span> <small class="text-warning">(🟡)</small></label>
              <asp:TextBox ID="txtStockMaximo" runat="server" CssClass="form-control" TextMode="Number" Text="0" min="0" step="0.01"></asp:TextBox>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Stock óptimo <span style="color:red">*</span> <small class="text-muted">(barra de progreso)</small></label>
              <asp:TextBox ID="txtStockOptimo" runat="server" CssClass="form-control" TextMode="Number" Text="0" min="0" step="0.01"></asp:TextBox>
            </div>
          </div>
        </div>
      </div>
      <div class="modal-footer">
        <asp:Button ID="btnGuardar" runat="server" Text="Guardar"
            CssClass="btn btn-success"
            OnClientClick="return validarNuevo();"
            OnClick="btnGuardar_Click" />
        <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancelar</button>
      </div>
    </div>
  </div>
</div>

<!-- ══ MODAL EDITAR MATERIAL ════════════════════════ -->
<div class="modal fade" id="modalEditar" tabindex="-1" role="dialog" data-backdrop="static">
  <div class="modal-dialog modal-lg" role="document">
    <div class="modal-content">
      <div class="modal-header" style="background-color:#003366;color:white;">
        <h5 class="modal-title"><i class="fas fa-edit"></i> Editar Material</h5>
        <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
      </div>
      <div class="modal-body">
        <asp:HiddenField ID="hdnMaterialID" runat="server" />
        <asp:HiddenField ID="hdnRowVersion" runat="server" />
        <div class="row">
          <div class="col-md-3">
            <div class="form-group">
              <label>Código <span style="color:red">*</span></label>
              <asp:TextBox ID="txtCodigoEdit" runat="server" CssClass="form-control" MaxLength="20"></asp:TextBox>
              <small class="text-muted">Se guardará en mayúsculas.</small>
            </div>
          </div>
          <div class="col-md-9">
            <div class="form-group">
              <label>Descripción <span style="color:red">*</span></label>
              <asp:TextBox ID="txtDescripcionEdit" runat="server" CssClass="form-control" MaxLength="200"></asp:TextBox>
            </div>
          </div>
        </div>
        <div class="row">
          <div class="col-md-4">
            <div class="form-group">
              <label>Tipo de material <span style="color:red">*</span></label>
              <asp:DropDownList ID="ddlTipoEdit" runat="server" CssClass="form-control"></asp:DropDownList>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Subtipo</label>
              <asp:TextBox ID="txtSubtipoEdit" runat="server" CssClass="form-control" MaxLength="50"></asp:TextBox>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Unidad de medida (base)<span style="color:red">*</span></label>
              <asp:DropDownList ID="ddlUnidadEdit" runat="server" CssClass="form-control"></asp:DropDownList>
            </div>
          </div>
        </div>
        <div class="row">
          <div class="col-md-6">
            <div class="form-group">
              <label>Proveedor principal</label>
              <asp:DropDownList ID="ddlProveedorPrincipalEdit" runat="server" CssClass="form-control">
                <asp:ListItem Value="">-- Sin proveedor --</asp:ListItem>
              </asp:DropDownList>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Precio unitario <span style="color:red">*</span></label>
              <div class="input-group">
                <div class="input-group-prepend"><span class="input-group-text">$</span></div>
                <asp:TextBox ID="txtPrecioEdit" runat="server" CssClass="form-control" TextMode="Number" min="0" step="0.01"></asp:TextBox>
              </div>
            </div>
          </div>
        </div>
        <hr />
        <h6 style="color:#003366;font-weight:600;"><i class="fas fa-layer-group"></i> Niveles de stock</h6>
        <p id="notaUnidadEdit" class="small fw-semibold text-info mb-2" style="margin-top:0;"></p>
        <div class="row">
          <div class="col-md-4">
            <div class="form-group">
              <label>Stock mínimo <small class="text-danger">(🔴)</small></label>
              <asp:TextBox ID="txtStockMinimoEdit" runat="server" CssClass="form-control" TextMode="Number" min="0" step="0.01"></asp:TextBox>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Stock máximo <small class="text-warning">(🟡)</small></label>
              <asp:TextBox ID="txtStockMaximoEdit" runat="server" CssClass="form-control" TextMode="Number" min="0" step="0.01"></asp:TextBox>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Stock óptimo <small class="text-muted">(barra de progreso)</small></label>
              <asp:TextBox ID="txtStockOptimoEdit" runat="server" CssClass="form-control" TextMode="Number" min="0" step="0.01"></asp:TextBox>
            </div>
          </div>
        </div>

        <!-- ── Conversiones de unidad ───────────────────────────── -->
        <hr />
        <h6 style="color:#003366;font-weight:600;"><i class="fas fa-exchange-alt"></i> Conversiones de unidad</h6>
        <p class="text-muted small">Permite registrar movimientos en unidades alternativas (ej. cajas, sacos). La cantidad se convierte automáticamente a la unidad base antes de afectar el stock.</p>

        <!-- Lista de conversiones activas -->
        <asp:HiddenField ID="hdnConvMaterialID" runat="server" />
        <div class="table-responsive mb-2">
          <asp:GridView ID="gvConversiones" runat="server" AutoGenerateColumns="False"
              CssClass="table table-bordered table-sm"
              EmptyDataText="Sin conversiones configuradas para este material."
              DataKeyNames="ConversionID"
              OnRowCommand="gvConversiones_RowCommand">
            <Columns>
              <asp:BoundField DataField="UnidadNombre" HeaderText="Unidad origen" />
              <asp:BoundField DataField="Factor" HeaderText="Factor" DataFormatString="{0:N6}" />
              <asp:BoundField DataField="Descripcion" HeaderText="Descripción" />
              <asp:TemplateField HeaderText="">
                <ItemTemplate>
                  <asp:LinkButton ID="lnkEliminarConv" runat="server"
                      CommandName="EliminarConv"
                      CommandArgument='<%# Eval("ConversionID") %>'
                      CssClass="btn btn-sm btn-outline-danger"
                      OnClientClick="return confirm('¿Eliminar esta conversión?');">
                    <i class="fas fa-times"></i>
                  </asp:LinkButton>
                </ItemTemplate>
              </asp:TemplateField>
            </Columns>
          </asp:GridView>
        </div>

        <!-- Agregar nueva conversión -->
        <div class="card card-body bg-light mb-2 p-2">
          <div class="row align-items-end">
            <div class="col-md-4">
              <label class="small font-weight-bold">Unidad origen <span class="text-danger">*</span></label>
              <asp:DropDownList ID="ddlUnidadOrigenConv" runat="server" CssClass="form-control form-control-sm">
              </asp:DropDownList>
            </div>
            <div class="col-md-2">
              <label class="small font-weight-bold">Factor <span class="text-danger">*</span></label>
              <asp:TextBox ID="txtFactorConv" runat="server" CssClass="form-control form-control-sm"
                  TextMode="Number" min="0.000001" step="0.000001" Placeholder="ej: 25"></asp:TextBox>
            </div>
            <div class="col-md-4">
              <label class="small font-weight-bold">Descripción</label>
              <asp:TextBox ID="txtDescConversion" runat="server" CssClass="form-control form-control-sm"
                  Placeholder="ej: 1 caja = 25 kg" MaxLength="200"></asp:TextBox>
            </div>
            <div class="col-md-2">
              <asp:Button ID="btnAgregarConversion" runat="server" Text="+ Agregar"
                  CssClass="btn btn-primary btn-sm btn-block"
                  OnClick="btnAgregarConversion_Click" />
            </div>
          </div>
          <small class="text-muted mt-1 d-block"><strong>Fórmula:</strong> Cantidad capturada &times; Factor = Cantidad en unidad base</small>
        </div>

      </div>
      <div class="modal-footer">
        <asp:Button ID="btnGuardarEdit" runat="server" Text="Guardar Cambios"
            CssClass="btn btn-success"
            OnClientClick="return validarEditar();"
            OnClick="btnGuardarEdit_Click" />
        <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancelar</button>
      </div>
    </div>
  </div>
</div>

<script>
    // ── Mensaje pendiente (mismo patrón que Bases) ────────────────
    window.addEventListener('load', function () {
        var hdnMsg = document.getElementById('<%= hdnMensajePendiente.ClientID %>');
        if (!hdnMsg || !hdnMsg.value) return;
        try {
            var msg = JSON.parse(hdnMsg.value);
            hdnMsg.value = '';
            var opts = { icon: msg.icon, title: msg.title, text: msg.text, confirmButtonColor: '#003366' };
            if (msg.icon === 'success') { opts.showConfirmButton = false; opts.timer = 2000; }
            if (msg.modal) {
                opts.showConfirmButton = true;
                Swal.fire(opts).then(function () { $('#' + msg.modal).modal('show'); });
            } else { Swal.fire(opts); }
        } catch (e) { }
    });

    // ── Acordeón de bases ──────────────────────────────────────────
    function toggleAcordeon(id, btn) {
        var el = document.getElementById(id);
        if (!el) return;
        var visible = el.style.display !== 'none' && el.style.display !== '';
        document.querySelectorAll('[id^="acc_"]').forEach(function (a) { a.style.display = 'none'; });
        document.querySelectorAll('.btn-info.btn-sm.activo-acc').forEach(function (b) {
            b.classList.remove('activo-acc');
            b.innerHTML = '<i class="fas fa-layer-group"></i> Ver bases';
        });
        if (!visible) {
            el.style.display = 'block';
            btn.classList.add('activo-acc');
            btn.innerHTML = '<i class="fas fa-times"></i> Cerrar';
        }
    }

    // ── Filtrar por nivel desde las cards del dashboard ───────────
    function filtrarNivel(nivel) {
        document.getElementById('<%= hdnNivelFiltro.ClientID %>').value = nivel;
        document.getElementById('<%= ddlFiltrNivel.ClientID %>').value = nivel;
        document.getElementById('<%= btnBuscar.ClientID %>').click();
    }

    // ── Limpieza de modal al cerrar (evita backdrop fantasma y bug aria-hidden) ──
    window.addEventListener('load', function () {
        // Mover el foco fuera ANTES de que Bootstrap ponga aria-hidden="true"
        // (sin esto el navegador bloquea aria-hidden y el backdrop queda atrapado)
        $('#modalEditar, #modalNuevo').on('hide.bs.modal', function () {
            if (document.activeElement && $.contains(this, document.activeElement)) {
                document.activeElement.blur();
            }
        });

        // Limpiar forzadamente cualquier residuo una vez que el modal terminó de cerrarse
        $('#modalEditar, #modalNuevo').on('hidden.bs.modal', function () {
            $('body').removeClass('modal-open').css('padding-right', '');
            $('.modal-backdrop').remove();
        });

        // ── Nota de unidad en modal Nuevo: se actualiza al cambiar la unidad ──
        document.getElementById('<%= ddlUnidad.ClientID %>').addEventListener('change', function () {
            var texto = this.options[this.selectedIndex].text;
            var nota  = document.getElementById('notaUnidadNuevo');
            nota.innerHTML = texto && this.value
                ? '<i class="fas fa-info-circle"></i> Configura los niveles en ' + texto
                : '<i class="fas fa-info-circle"></i> Selecciona una unidad de medida para ver en qué unidad configurar los niveles.';
        });

        // ── Nota de unidad en modal Editar: se actualiza si el usuario cambia la unidad ──
        document.getElementById('<%= ddlUnidadEdit.ClientID %>').addEventListener('change', function () {
            var texto = this.options[this.selectedIndex].text;
            var nota  = document.getElementById('notaUnidadEdit');
            nota.innerHTML = texto && this.value
                ? '<i class="fas fa-info-circle"></i> Configura los niveles en ' + texto
                : '';
        });
    });

    // ── Abrir modales ─────────────────────────────────────────────
    function abrirModalNuevo() { $('#modalNuevo').modal('show'); }

    function abrirModalEditar(id, codigo, descripcion, tipoID, subtipo, unidadMedidaID, precio, minimo, maximo, optimo, rowVersion, proveedorPrincipalID) {
        document.getElementById('<%= hdnMaterialID.ClientID %>').value          = id;
        document.getElementById('<%= hdnRowVersion.ClientID %>').value          = rowVersion;
        document.getElementById('<%= hdnConvMaterialID.ClientID %>').value      = id;
        document.getElementById('<%= txtCodigoEdit.ClientID %>').value          = codigo;
        document.getElementById('<%= txtDescripcionEdit.ClientID %>').value     = descripcion;
        document.getElementById('<%= ddlTipoEdit.ClientID %>').value            = tipoID;
        document.getElementById('<%= txtSubtipoEdit.ClientID %>').value         = subtipo;
        document.getElementById('<%= ddlUnidadEdit.ClientID %>').value          = unidadMedidaID;
        document.getElementById('<%= txtPrecioEdit.ClientID %>').value          = precio;
        document.getElementById('<%= txtStockMinimoEdit.ClientID %>').value     = minimo;
        document.getElementById('<%= txtStockMaximoEdit.ClientID %>').value     = maximo;
        document.getElementById('<%= txtStockOptimoEdit.ClientID %>').value     = optimo;
        document.getElementById('<%= ddlProveedorPrincipalEdit.ClientID %>').value = proveedorPrincipalID || '';
        // Actualizar nota de unidad
        var ddlU = document.getElementById('<%= ddlUnidadEdit.ClientID %>');
        var notaEdit = document.getElementById('notaUnidadEdit');
        var textoU = ddlU.options[ddlU.selectedIndex] ? ddlU.options[ddlU.selectedIndex].text : '';
        notaEdit.innerHTML = textoU
            ? '<i class="fas fa-info-circle"></i> Configura los niveles en ' + textoU
            : '';
        // Cargar conversiones via postback (recarga la tabla y el dropdown de unidades)
        document.getElementById('<%= btnCargarConversiones.ClientID %>').click();
    }

    // ── Toggle ────────────────────────────────────────────────────
    function confirmarToggle(matID, descripcion, activo) {
        var accion = activo ? 'desactivar' : 'activar';
        Swal.fire({
            icon: activo ? 'warning' : 'question',
            title: '¿' + (activo ? 'Desactivar' : 'Activar') + ' material?',
            html: '¿Seguro de <b>' + accion + '</b> el material <b>' + descripcion + '</b>?',
            showCancelButton: true,
            confirmButtonText: 'Sí, ' + accion,
            cancelButtonText: 'Cancelar',
            confirmButtonColor: activo ? '#e0a800' : '#28a745',
            cancelButtonColor: '#6c757d'
        }).then(function(r) {
            if (r.isConfirmed) {
                document.getElementById('<%= hdnToggleMaterialID.ClientID %>').value = matID;
                __doPostBack('<%= btnToggleHidden.UniqueID %>', '');
            }
        });
        return false;
    }

    // ── Validaciones cliente ──────────────────────────────────────
    function validarNuevo() {
        return _validar(
            '<%= txtCodigo.ClientID %>',
            '<%= txtDescripcion.ClientID %>',
            '<%= ddlTipo.ClientID %>',
            '<%= ddlUnidad.ClientID %>',
            '<%= txtPrecio.ClientID %>',
            '<%= txtStockMinimo.ClientID %>',
            '<%= txtStockMaximo.ClientID %>',
            '<%= txtStockOptimo.ClientID %>',
            'modalNuevo'
        );
    }
    function validarEditar() {
        return _validar(
            '<%= txtCodigoEdit.ClientID %>',
            '<%= txtDescripcionEdit.ClientID %>',
            '<%= ddlTipoEdit.ClientID %>',
            '<%= ddlUnidadEdit.ClientID %>',
            '<%= txtPrecioEdit.ClientID %>',
            '<%= txtStockMinimoEdit.ClientID %>',
            '<%= txtStockMaximoEdit.ClientID %>',
            '<%= txtStockOptimoEdit.ClientID %>',
            'modalEditar'
        );
    }
    function _validar(idCod, idDesc, idTipo, idUni, idPre, idMin, idMax, idOpt, modal) {
        var cod = document.getElementById(idCod).value.trim();
        var desc = document.getElementById(idDesc).value.trim();
        var tipo = document.getElementById(idTipo).value;
        var uni = document.getElementById(idUni).value;
        var pre = parseFloat(document.getElementById(idPre).value) || 0;
        var min = parseFloat(document.getElementById(idMin).value) || 0;
        var max = parseFloat(document.getElementById(idMax).value) || 0;
        var opt = parseFloat(document.getElementById(idOpt).value) || 0;

        function warn(txt) {
            Swal.fire({ icon: 'warning', title: 'Campo inválido', text: txt, confirmButtonColor: '#003366' })
                .then(function () { $('#' + modal).modal('show'); });
            return false;
        }
        if (!cod) return warn('El código es obligatorio.');
        if (cod.length < 2) return warn('El código debe tener al menos 2 caracteres.');
        if (!desc) return warn('La descripción es obligatoria.');
        if (desc.length < 3) return warn('La descripción debe tener al menos 3 caracteres.');
        if (!tipo) return warn('Debe seleccionar el tipo de material.');
        if (!uni || uni === '0') return warn('La unidad de medida es obligatoria.');
        if (pre < 0) return warn('El precio no puede ser negativo.');
        if (min < 0 || max < 0 || opt < 0) return warn('Los niveles de stock no pueden ser negativos.');
        if (min >= opt) return warn('El Mínimo debe ser menor al Óptimo.');
        if (opt >= max) return warn('El Óptimo debe ser menor al Máximo.');
        return true;
    }

    // ── Editor de niveles por base ────────────────────────────────────────────
    var _editorNivel = { matID: 0, baseID: 0 };

    function abrirEditorNivel(matID, baseID, baseNombre, minActual, optActual, maxActual,
                               tienePropio, globalMin, globalMax, unidadNombre) {
        _editorNivel.matID  = matID;
        _editorNivel.baseID = baseID;

        document.getElementById('spanNombreBaseEditor').textContent = baseNombre;
        var notaBase = document.getElementById('notaUnidadBase');
        notaBase.innerHTML = unidadNombre
            ? '<i class="fas fa-info-circle"></i> Configura los niveles en ' + unidadNombre
            : '';
        document.getElementById('inpNivelMin').value = minActual;
        document.getElementById('inpNivelOpt').value = optActual;
        document.getElementById('inpNivelMax').value = maxActual;

        var hint = tienePropio
            ? 'Esta base tiene umbrales propios configurados.'
            : 'Actualmente usa los umbrales globales del material ' +
              '(Mín=' + globalMin + ' / Máx=' + globalMax + '). ' +
              'Guarda para crear umbrales específicos para esta base.';
        document.getElementById('spanNivelHint').textContent = hint;

        document.getElementById('overlayEditorNivel').style.display = 'block';
        document.getElementById('panelEditorNivel').style.display   = 'block';
        document.getElementById('inpNivelMin').focus();
    }

    function cerrarEditorNivel() {
        document.getElementById('overlayEditorNivel').style.display = 'none';
        document.getElementById('panelEditorNivel').style.display   = 'none';
        _editorNivel.matID  = 0;
        _editorNivel.baseID = 0;
    }

    function confirmarGuardarNivel() {
        var min = parseFloat(document.getElementById('inpNivelMin').value) || 0;
        var opt = parseFloat(document.getElementById('inpNivelOpt').value) || 0;
        var max = parseFloat(document.getElementById('inpNivelMax').value) || 0;

        if (min < 0 || opt < 0 || max < 0) {
            Swal.fire({ icon: 'warning', title: 'Campo inválido',
                text: 'Los niveles no pueden ser negativos.', confirmButtonColor: '#003366' });
            return;
        }
        if (min >= opt) {
            Swal.fire({ icon: 'warning', title: 'Campo inválido',
                text: 'El Mínimo debe ser menor al Óptimo.', confirmButtonColor: '#003366' });
            return;
        }
        if (opt >= max) {
            Swal.fire({ icon: 'warning', title: 'Campo inválido',
                text: 'El Óptimo debe ser menor al Máximo.', confirmButtonColor: '#003366' });
            return;
        }

        document.getElementById('<%= hdnNivelBaseMaterialID.ClientID %>').value = _editorNivel.matID;
        document.getElementById('<%= hdnNivelBaseBaseID.ClientID %>').value     = _editorNivel.baseID;
        document.getElementById('<%= hdnNivelBaseMinimo.ClientID %>').value     = min;
        document.getElementById('<%= hdnNivelBaseOptimo.ClientID %>').value     = opt;
        document.getElementById('<%= hdnNivelBaseMaximo.ClientID %>').value     = max;

        cerrarEditorNivel();
        __doPostBack('<%= btnGuardarNivelBase.UniqueID %>', '');
    }
</script>

</asp:Content>

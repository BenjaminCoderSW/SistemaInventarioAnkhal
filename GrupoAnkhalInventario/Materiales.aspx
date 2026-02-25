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
        .nivel-bajo    { background:#fef3e2; color:#d35400; border:1px solid #e67e22; }
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

        /* activo al filtrar por nivel */
        .stock-card.activo-filtro { outline: 3px solid #fff; outline-offset: 2px; }
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
        <div class="stock-card critico" onclick="filtrarNivel('critico')" id="cardCritico">
            <div class="icon"><i class="fas fa-exclamation-circle"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblCritico" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">🔴 Stock crítico</div>
            </div>
        </div>
        <div class="stock-card bajo" onclick="filtrarNivel('bajo')" id="cardBajo">
            <div class="icon"><i class="fas fa-exclamation-triangle"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblBajo"    runat="server" Text="0"></asp:Label></div>
                <div class="lbl">🟡 Stock bajo</div>
            </div>
        </div>
        <div class="stock-card optimo" onclick="filtrarNivel('optimo')" id="cardOptimo">
            <div class="icon"><i class="fas fa-check-circle"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblOptimo"  runat="server" Text="0"></asp:Label></div>
                <div class="lbl">🟢 Stock óptimo</div>
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
                        <label>Buscar por Nombre o Código</label>
                        <asp:TextBox ID="txtBuscar" runat="server" CssClass="form-control form-control-sm"
                            Placeholder="Nombre o código..."></asp:TextBox>
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
                            <asp:ListItem Value="critico">🔴 Crítico</asp:ListItem>
                            <asp:ListItem Value="bajo">🟡 Bajo</asp:ListItem>
                            <asp:ListItem Value="optimo">🟢 Óptimo</asp:ListItem>
                            <asp:ListItem Value="sin">⚪ Sin stock</asp:ListItem>
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
                    AllowPaging="True" PageSize="15"
                    OnPageIndexChanging="gvMateriales_PageIndexChanging"
                    OnRowDataBound="gvMateriales_RowDataBound"
                    DataKeyNames="MaterialID"
                    PagerStyle-CssClass="pager-custom"
                    PagerSettings-Mode="NumericFirstLast"
                    PagerSettings-FirstPageText="«"
                    PagerSettings-LastPageText="»"
                    PagerSettings-PageButtonCount="5">
                    <Columns>
                        <asp:BoundField DataField="MaterialID"  HeaderText="ID"     Visible="false" />
                        <asp:BoundField DataField="Codigo"      HeaderText="Código" />
                        <asp:BoundField DataField="Nombre"      HeaderText="Nombre" />
                        <asp:BoundField DataField="TipoNombre"  HeaderText="Tipo" />
                        <asp:BoundField DataField="Subtipo"     HeaderText="Subtipo" />
                        <asp:BoundField DataField="Unidad"      HeaderText="Unidad" />
                        <asp:BoundField DataField="PrecioUnitario" HeaderText="Precio" DataFormatString="{0:C2}" />

                        <asp:TemplateField HeaderText="Stock Global">
                            <ItemTemplate>
                                <div>
                                    <span class='nivel-badge <%# GetNivelCss((decimal)Eval("StockGlobal"), (decimal)Eval("StockCritico"), (decimal)Eval("StockMinimo"), (decimal)Eval("StockOptimo")) %>'>
                                        <%# GetNivelIcon((decimal)Eval("StockGlobal"), (decimal)Eval("StockCritico"), (decimal)Eval("StockMinimo"), (decimal)Eval("StockOptimo")) %>
                                        <%# string.Format("{0:N2}", Eval("StockGlobal")) %> <%# Eval("Unidad") %>
                                    </span>
                                    <div class="stock-bar-wrap">
                                        <div class="stock-bar-fill <%# GetBarCss((decimal)Eval("StockGlobal"), (decimal)Eval("StockCritico"), (decimal)Eval("StockMinimo"), (decimal)Eval("StockOptimo")) %>"
                                             style="width:<%# GetBarPct((decimal)Eval("StockGlobal"), (decimal)Eval("StockOptimo")) %>%; background:<%# GetBarColor((decimal)Eval("StockGlobal"), (decimal)Eval("StockCritico"), (decimal)Eval("StockMinimo"), (decimal)Eval("StockOptimo")) %>">
                                        </div>
                                    </div>
                                </div>
                                <small class="text-muted" style="font-size:.72rem;">
                                    🔴&lt;<%# string.Format("{0:N2}", Eval("StockCritico")) %> &nbsp;
                                    🟡&lt;<%# string.Format("{0:N2}", Eval("StockMinimo")) %> &nbsp;
                                    🟢≥<%# string.Format("{0:N2}", Eval("StockOptimo")) %>
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
                                        '<%# Server.HtmlEncode((Eval("Nombre") ?? "").ToString()) %>',
                                        '<%# Eval("TipoMaterialID") %>',
                                        '<%# Server.HtmlEncode((Eval("Subtipo") ?? "").ToString()) %>',
                                        '<%# Server.HtmlEncode((Eval("Unidad") ?? "").ToString()) %>',
                                        '<%# Eval("PrecioUnitario") %>',
                                        '<%# Eval("StockCritico") %>',
                                        '<%# Eval("StockMinimo") %>',
                                        '<%# Eval("StockOptimo") %>'
                                    )">
                                    <i class="fas fa-edit"></i> Editar
                                </button>
                                <asp:Button ID="btnToggle" runat="server"
                                    CssClass='<%# Convert.ToBoolean(Eval("Activo")) ? "btn btn-warning btn-sm" : "btn btn-success btn-sm" %>'
                                    Text='<%# Convert.ToBoolean(Eval("Activo")) ? "Desactivar" : "Activar" %>'
                                    CommandArgument='<%# Eval("MaterialID") %>'
                                    OnClientClick='<%# "return confirmarToggle(\"" + Eval("MaterialID") + "\", \"" + Server.HtmlEncode((Eval("Nombre") ?? "").ToString()) + "\", " + Eval("Activo").ToString().ToLower() + ");" %>'
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
<asp:HiddenField ID="hdnMensajePendiente" runat="server" Value="" />
<asp:HiddenField ID="hdnNivelFiltro"      runat="server" Value="" />

<!-- ══ MODAL NUEVO MATERIAL ═════════════════════════ -->
<div class="modal fade" id="modalNuevo" tabindex="-1" role="dialog" data-backdrop="static">
  <div class="modal-dialog modal-lg" role="document">
    <div class="modal-content">
      <div class="modal-header" style="background-color:#003366;color:white;">
        <h5 class="modal-title"><i class="fas fa-cubes"></i> Nuevo Material</h5>
        <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
      </div>
      <div class="modal-body">
        <!-- Fila 1: Código + Nombre -->
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
              <label>Nombre <span style="color:red">*</span></label>
              <asp:TextBox ID="txtNombre" runat="server" CssClass="form-control" Placeholder="Nombre completo del material" MaxLength="200"></asp:TextBox>
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
              <label>Unidad de medida <span style="color:red">*</span></label>
              <asp:TextBox ID="txtUnidad" runat="server" CssClass="form-control" Placeholder="Ej: kg, lt, pza" MaxLength="30"></asp:TextBox>
            </div>
          </div>
        </div>
        <!-- Fila 3: Precio -->
        <div class="row">
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
        <h6 style="color:#003366;font-weight:600;"><i class="fas fa-layer-group"></i> Niveles de stock</h6>
        <small class="text-muted d-block mb-2">
            Definen el semáforo: <span style="color:#c0392b">🔴 Crítico</span> si stock &lt; Crítico &nbsp;|&nbsp;
            <span style="color:#d35400">🟡 Bajo</span> si stock &lt; Mínimo &nbsp;|&nbsp;
            <span style="color:#1e8449">🟢 Óptimo</span> si stock ≥ Óptimo
        </small>
        <div class="row">
          <div class="col-md-4">
            <div class="form-group">
              <label>Stock crítico <span style="color:red">*</span> <small class="text-danger">(🔴)</small></label>
              <asp:TextBox ID="txtStockCritico" runat="server" CssClass="form-control" TextMode="Number" Text="0" min="0" step="0.01"></asp:TextBox>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Stock mínimo <span style="color:red">*</span> <small class="text-warning">(🟡)</small></label>
              <asp:TextBox ID="txtStockMinimo" runat="server" CssClass="form-control" TextMode="Number" Text="0" min="0" step="0.01"></asp:TextBox>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Stock óptimo <span style="color:red">*</span> <small class="text-success">(🟢)</small></label>
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
              <label>Nombre <span style="color:red">*</span></label>
              <asp:TextBox ID="txtNombreEdit" runat="server" CssClass="form-control" MaxLength="200"></asp:TextBox>
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
              <label>Unidad de medida <span style="color:red">*</span></label>
              <asp:TextBox ID="txtUnidadEdit" runat="server" CssClass="form-control" MaxLength="30"></asp:TextBox>
            </div>
          </div>
        </div>
        <div class="row">
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
        <div class="row">
          <div class="col-md-4">
            <div class="form-group">
              <label>Stock crítico <small class="text-danger">(🔴)</small></label>
              <asp:TextBox ID="txtStockCriticoEdit" runat="server" CssClass="form-control" TextMode="Number" min="0" step="0.01"></asp:TextBox>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Stock mínimo <small class="text-warning">(🟡)</small></label>
              <asp:TextBox ID="txtStockMinimoEdit" runat="server" CssClass="form-control" TextMode="Number" min="0" step="0.01"></asp:TextBox>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Stock óptimo <small class="text-success">(🟢)</small></label>
              <asp:TextBox ID="txtStockOptimoEdit" runat="server" CssClass="form-control" TextMode="Number" min="0" step="0.01"></asp:TextBox>
            </div>
          </div>
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
        // cerrar todos
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

// ── Abrir modales ─────────────────────────────────────────────
function abrirModalNuevo() { $('#modalNuevo').modal('show'); }

function abrirModalEditar(id, codigo, nombre, tipoID, subtipo, unidad, precio, critico, minimo, optimo) {
    document.getElementById('<%= hdnMaterialID.ClientID %>').value          = id;
    document.getElementById('<%= txtCodigoEdit.ClientID %>').value          = codigo;
    document.getElementById('<%= txtNombreEdit.ClientID %>').value          = nombre;
    document.getElementById('<%= ddlTipoEdit.ClientID %>').value            = tipoID;
    document.getElementById('<%= txtSubtipoEdit.ClientID %>').value         = subtipo;
    document.getElementById('<%= txtUnidadEdit.ClientID %>').value          = unidad;
    document.getElementById('<%= txtPrecioEdit.ClientID %>').value          = precio;
    document.getElementById('<%= txtStockCriticoEdit.ClientID %>').value    = critico;
    document.getElementById('<%= txtStockMinimoEdit.ClientID %>').value     = minimo;
    document.getElementById('<%= txtStockOptimoEdit.ClientID %>').value     = optimo;
    $('#modalEditar').modal('show');
}

// ── Toggle ────────────────────────────────────────────────────
function confirmarToggle(matID, nombre, activo) {
    var accion = activo ? 'desactivar' : 'activar';
    Swal.fire({
        icon: activo ? 'warning' : 'question',
        title: '¿' + (activo ? 'Desactivar' : 'Activar') + ' material?',
        html: '¿Seguro de <b>' + accion + '</b> el material <b>' + nombre + '</b>?',
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
        '<%= txtNombre.ClientID %>',
        '<%= ddlTipo.ClientID %>',
        '<%= txtUnidad.ClientID %>',
        '<%= txtPrecio.ClientID %>',
        '<%= txtStockCritico.ClientID %>',
        '<%= txtStockMinimo.ClientID %>',
        '<%= txtStockOptimo.ClientID %>',
        'modalNuevo'
    );
}
function validarEditar() {
    return _validar(
        '<%= txtCodigoEdit.ClientID %>',
        '<%= txtNombreEdit.ClientID %>',
        '<%= ddlTipoEdit.ClientID %>',
        '<%= txtUnidadEdit.ClientID %>',
        '<%= txtPrecioEdit.ClientID %>',
        '<%= txtStockCriticoEdit.ClientID %>',
        '<%= txtStockMinimoEdit.ClientID %>',
        '<%= txtStockOptimoEdit.ClientID %>',
        'modalEditar'
    );
    }
    function _validar(idCod, idNom, idTipo, idUni, idPre, idCrit, idMin, idOpt, modal) {
        var cod = document.getElementById(idCod).value.trim();
        var nom = document.getElementById(idNom).value.trim();
        var tipo = document.getElementById(idTipo).value;
        var uni = document.getElementById(idUni).value.trim();
        var pre = parseFloat(document.getElementById(idPre).value) || 0;
        var crit = parseFloat(document.getElementById(idCrit).value) || 0;
        var min = parseFloat(document.getElementById(idMin).value) || 0;
        var opt = parseFloat(document.getElementById(idOpt).value) || 0;

        function warn(txt) {
            Swal.fire({ icon: 'warning', title: 'Campo inválido', text: txt, confirmButtonColor: '#003366' })
                .then(function () { $('#' + modal).modal('show'); });
            return false;
        }
        if (!cod) return warn('El código es obligatorio.');
        if (cod.length < 2) return warn('El código debe tener al menos 2 caracteres.');
        if (!nom) return warn('El nombre es obligatorio.');
        if (nom.length < 3) return warn('El nombre debe tener al menos 3 caracteres.');
        if (!tipo) return warn('Debe seleccionar el tipo de material.');
        if (!uni) return warn('La unidad de medida es obligatoria.');
        if (pre < 0) return warn('El precio no puede ser negativo.');
        if (crit < 0) return warn('El stock crítico no puede ser negativo.');
        if (min < crit) return warn('El stock mínimo debe ser ≥ stock crítico.');
        if (opt < min) return warn('El stock óptimo debe ser ≥ stock mínimo.');
        return true;
    }
</script>

</asp:Content>

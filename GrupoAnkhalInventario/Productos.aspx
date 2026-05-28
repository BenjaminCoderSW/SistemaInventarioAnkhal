<%@ Page Title="Productos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Productos.aspx.cs" Inherits="GrupoAnkhalInventario.Productos" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="css/gridviewPantalla.css" rel="stylesheet" />
    <style>
        /* ── Dashboard ── */
        .prod-dashboard {
            display: flex; gap: 16px; margin-bottom: 18px; align-items: stretch; flex-wrap: wrap;
        }

        /* Card Total (izquierda) */
        .prod-card-total {
            flex: 0 0 200px;
            border-radius: 10px; padding: 24px 20px;
            color: #fff; display: flex; align-items: center; gap: 16px;
            background: linear-gradient(135deg, #1a5276, #2980b9);
            box-shadow: 0 3px 10px rgba(0,0,0,.18);
        }
        .prod-card-total .icon  { font-size: 2.6rem; opacity: .9; }
        .prod-card-total .num   { font-size: 2.4rem; font-weight: 700; line-height: 1; }
        .prod-card-total .lbl   { font-size: .78rem; opacity: .88; text-transform: uppercase; letter-spacing: .5px; }

        /* Panel de desglose (derecha) */
        .prod-breakdown {
            flex: 1; min-width: 260px;
            border-radius: 10px; border: 1px solid #dee2e6;
            background: #fff; box-shadow: 0 2px 8px rgba(0,0,0,.07);
            overflow: hidden;
        }
        .prod-breakdown-header {
            background: #f0f4f8; border-bottom: 1px solid #dee2e6;
            padding: 9px 16px; font-size: .8rem; font-weight: 700;
            color: #003366; text-transform: uppercase; letter-spacing: .5px;
        }
        .prod-breakdown-body {
            display: grid; grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
            padding: 10px 12px; gap: 4px 8px;
        }
        .tipo-row {
            display: flex; align-items: center; justify-content: space-between;
            padding: 5px 8px; border-radius: 6px; gap: 8px;
        }
        .tipo-row:hover { background: #f0f4f8; }
        .tipo-row .tipo-dot {
            width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0;
        }
        .tipo-row .tipo-nombre {
            flex: 1; font-size: .84rem; color: #333; white-space: nowrap;
            overflow: hidden; text-overflow: ellipsis;
        }
        .tipo-row .tipo-badge {
            background: #003366; color: #fff; border-radius: 12px;
            padding: 1px 9px; font-size: .78rem; font-weight: 700; flex-shrink: 0;
        }
        .tipo-row .tipo-badge.cero {
            background: #ced4da; color: #555;
        }

        /* ── Filtros ── */
        .filtros-bar {
            background:#f8f9fa; border:1px solid #dee2e6;
            border-radius:8px; padding:14px 18px; margin-bottom:14px;
        }
        .filtros-bar label { font-weight:600; font-size:.84rem; color:#003366; margin-bottom:2px; }

        /* ── Modal componentes (ver/editar) ── */
        .comp-view-table th { background:#003366; color:#fff; }
        .comp-view-table td, .comp-view-table th { padding:8px 12px; vertical-align:middle; }
        .comp-edit-row input { width:80px; }

        /* ── Paginador ── */
        .pager-custom span { background:#003366; color:#fff; font-weight:700; border-radius:4px; padding:4px 9px; }
        .pager-custom a    { padding:4px 9px; border-radius:4px; }

        /* ── Accordion stock por base ── */
        .bases-accordion { background:#f0f4f8; border-radius:6px; padding:10px 14px; margin-top:6px; }
        .bases-accordion table { width:100%; font-size:.83rem; }
        .bases-accordion th { color:#003366; font-weight:600; padding:4px 8px; }
        .bases-accordion td { padding:4px 8px; }
        .bases-accordion tr:hover td { background:#e3eaf3; }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
<div class="container-fluid">
<div class="row">
<div class="col-12">

    <!-- ══ DASHBOARD ══════════════════════════════════════ -->
    <div class="prod-dashboard">

        <!-- Card total -->
        <div class="prod-card-total">
            <div class="icon"><i class="fas fa-boxes"></i></div>
            <div>
                <div class="num"><asp:Label ID="lblTotal" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Total<br>productos</div>
            </div>
        </div>

        <!-- Panel desglose por tipo -->
        <div class="prod-breakdown">
            <div class="prod-breakdown-header"><i class="fas fa-tag mr-1"></i> Desglose por tipo</div>
            <div class="prod-breakdown-body">
                <asp:Literal ID="litTiposCards" runat="server"></asp:Literal>
            </div>
        </div>

    </div>

    <div class="card">
        <div class="card-header" style="background-color:#003366;color:white;">
            <h3 class="card-title"><i class="fas fa-cube"></i> Productos</h3>
        </div>
        <div class="card-body">

            <div class="mb-3">
                <asp:Button ID="btnNuevo" runat="server" Text="＋ Nuevo Producto"
                    CssClass="btn btn-success"
                    OnClientClick="abrirModalNuevo(); return false;" />
            </div>

            <!-- ── FILTROS ── -->
            <div class="filtros-bar">
                <div class="row align-items-end">
                    <div class="col-md-4">
                        <label>Buscar por Descripción o Código</label>
                        <asp:TextBox ID="txtBuscar" runat="server" CssClass="form-control form-control-sm"
                            Placeholder="Descripción o código..."></asp:TextBox>
                    </div>
                    <div class="col-md-2">
                        <label>Tipo</label>
                        <asp:DropDownList ID="ddlFiltrTipo" runat="server" CssClass="form-control form-control-sm">
                            <asp:ListItem Value="">-- Todos --</asp:ListItem>
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

            <!-- ── GRID ── -->
            <div class="table-responsive">
                <asp:GridView ID="gvProductos" runat="server" AutoGenerateColumns="False"
                    CssClass="table table-bordered table-striped custom-grid"
                    AllowPaging="True" AllowCustomPaging="True" PageSize="15"
                    OnPageIndexChanging="gvProductos_PageIndexChanging"
                    OnRowDataBound="gvProductos_RowDataBound"
                    DataKeyNames="ProductoID"
                    PagerStyle-CssClass="pager-custom"
                    PagerSettings-Mode="NumericFirstLast"
                    PagerSettings-FirstPageText="«"
                    PagerSettings-LastPageText="»"
                    PagerSettings-PageButtonCount="5">
                    <Columns>
                        <asp:BoundField DataField="ProductoID"  HeaderText="ID"           Visible="false" />
                        <asp:BoundField DataField="Codigo"      HeaderText="Código" />
                        <asp:BoundField DataField="Descripcion" HeaderText="Descripción" />
                        <asp:BoundField DataField="TipoNombre"  HeaderText="Tipo" />
                        <asp:BoundField DataField="PrecioVenta"  HeaderText="Precio Venta" DataFormatString="{0:C2}" />
                        <asp:BoundField DataField="StockGlobal" HeaderText="Stock Global" />
                        <asp:TemplateField HeaderText="Por Base">
                            <ItemTemplate>
                                <button type="button" class="btn btn-info btn-sm"
                                    onclick="toggleAcordeon('acc_<%# Eval("ProductoID") %>', this)">
                                    <i class="fas fa-layer-group"></i> Ver bases
                                </button>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Componentes">
                            <ItemTemplate>
                                <button type="button" class="btn btn-info btn-sm"
                                    onclick="verComponentes('<%# Eval("ProductoID") %>', '<%# Server.HtmlEncode((Eval("Descripcion") ?? "").ToString()) %>')">
                                    <i class="fas fa-list-ul"></i>
                                    Ver
                                    <span class="badge badge-light ml-1"><%# Eval("TotalComponentes") %></span>
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
                                        '<%# Eval("ProductoID") %>',
                                        '<%# Eval("Codigo") %>',
                                        '<%# Server.HtmlEncode((Eval("Descripcion") ?? "").ToString()) %>',
                                        '<%# Eval("TipoProductoID") %>',
                                        '<%# Eval("PrecioVenta") %>',
                                        '<%# RowVersionBase64(Eval("RowVersion")) %>'
                                    )">
                                    <i class="fas fa-edit"></i> Editar
                                </button>
                                <asp:Button ID="btnToggle" runat="server"
                                    CssClass='<%# Convert.ToBoolean(Eval("Activo")) ? "btn btn-warning btn-sm" : "btn btn-success btn-sm" %>'
                                    Text='<%# Convert.ToBoolean(Eval("Activo")) ? "Desactivar" : "Activar" %>'
                                    CommandArgument='<%# Eval("ProductoID") %>'
                                    OnClientClick='<%# "return confirmarToggle(\"" + Eval("ProductoID") + "\", \"" + Server.HtmlEncode((Eval("Descripcion") ?? "").ToString()) + "\", " + Eval("Activo").ToString().ToLower() + ");" %>'
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

<!-- ── HIDDEN FIELDS ── -->
<asp:HiddenField ID="hdnToggleProductoID"  runat="server" Value="" />
<asp:HiddenField ID="hdnMensajePendiente"  runat="server" Value="" />
<asp:HiddenField ID="hdnProductoIDEdit"    runat="server" Value="" />
<asp:Button      ID="btnToggleHidden"      runat="server" CssClass="d-none" OnClick="btnToggleHidden_Click" />
<asp:Button      ID="btnGuardarComponentes" runat="server" CssClass="d-none" OnClick="btnGuardarComponentes_Click" />

<!-- ══ MODAL NUEVO PRODUCTO ══════════════════════════ -->
<div class="modal fade" id="modalNuevo" tabindex="-1" role="dialog" data-backdrop="static">
  <div class="modal-dialog modal-xl" role="document">
    <div class="modal-content">
      <div class="modal-header" style="background-color:#003366;color:white;">
        <h5 class="modal-title"><i class="fas fa-cube"></i> Nuevo Producto</h5>
        <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
      </div>
      <div class="modal-body">
        <div class="row">
          <div class="col-md-3">
            <div class="form-group">
              <label>Código <span style="color:red">*</span></label>
              <asp:TextBox ID="txtCodigo" runat="server" CssClass="form-control" Placeholder="Ej: PROD-001" MaxLength="30"></asp:TextBox>
              <small class="text-muted">Se guardará en mayúsculas.</small>
            </div>
          </div>
          <div class="col-md-9">
            <div class="form-group">
              <label>Descripción <span style="color:red">*</span></label>
              <asp:TextBox ID="txtDescripcion" runat="server" CssClass="form-control" Placeholder="Descripción completa del producto" MaxLength="200"></asp:TextBox>
            </div>
          </div>
        </div>
        <div class="row">
          <div class="col-md-6">
            <div class="form-group">
              <label>Tipo <span style="color:red">*</span></label>
              <asp:DropDownList ID="ddlTipo" runat="server" CssClass="form-control"></asp:DropDownList>
            </div>
          </div>
          <div class="col-md-6">
            <div class="form-group">
              <label>Precio de venta <span style="color:red">*</span></label>
              <div class="input-group">
                <div class="input-group-prepend"><span class="input-group-text">$</span></div>
                <asp:TextBox ID="txtPrecio" runat="server" CssClass="form-control" Placeholder="0.00" TextMode="Number" min="0" step="0.01"></asp:TextBox>
              </div>
            </div>
          </div>
        </div>

        <div class="callout callout-info mt-2 mb-0" style="font-size:.85rem;">
          <i class="fas fa-info-circle"></i> Una vez guardado el producto, usa el botón <strong>"Ver"</strong> en la tabla para configurar sus componentes.
        </div>
      </div>
      <div class="modal-footer">
        <asp:Button ID="btnGuardar" runat="server" Text="Guardar"
            CssClass="btn btn-success"
            OnClientClick="return prepararYValidarNuevo();"
            OnClick="btnGuardar_Click" />
        <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancelar</button>
      </div>
    </div>
  </div>
</div>

<!-- ══ MODAL EDITAR PRODUCTO ════════════════════════ -->
<div class="modal fade" id="modalEditar" tabindex="-1" role="dialog" data-backdrop="static">
  <div class="modal-dialog modal-lg" role="document">
    <div class="modal-content">
      <div class="modal-header" style="background-color:#003366;color:white;">
        <h5 class="modal-title"><i class="fas fa-edit"></i> Editar Producto</h5>
        <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
      </div>
      <div class="modal-body">
        <asp:HiddenField ID="hdnProductoID" runat="server" />
        <asp:HiddenField ID="hdnRowVersion" runat="server" />
        <div class="row">
          <div class="col-md-3">
            <div class="form-group">
              <label>Código <span style="color:red">*</span></label>
              <asp:TextBox ID="txtCodigoEdit" runat="server" CssClass="form-control" MaxLength="30"></asp:TextBox>
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
          <div class="col-md-6">
            <div class="form-group">
              <label>Tipo <span style="color:red">*</span></label>
              <asp:DropDownList ID="ddlTipoEdit" runat="server" CssClass="form-control"></asp:DropDownList>
            </div>
          </div>
          <div class="col-md-6">
            <div class="form-group">
              <label>Precio de venta <span style="color:red">*</span></label>
              <div class="input-group">
                <div class="input-group-prepend"><span class="input-group-text">$</span></div>
                <asp:TextBox ID="txtPrecioEdit" runat="server" CssClass="form-control" TextMode="Number" min="0" step="0.01"></asp:TextBox>
              </div>
            </div>
          </div>
        </div>
        <div class="callout callout-info mt-2 mb-0" style="font-size:.85rem;">
          <i class="fas fa-info-circle"></i> Para editar los componentes usa el botón <strong>"Ver"</strong> en la tabla principal.
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

<!-- ══ MODAL COMPONENTES (Ver + Editar) ══════════════════ -->
<div class="modal fade" id="modalComponentes" tabindex="-1" role="dialog" data-backdrop="static">
  <div class="modal-dialog modal-lg" role="document">
    <div class="modal-content">
      <div class="modal-header" style="background-color:#003366;color:white;">
        <h5 class="modal-title">
          <i class="fas fa-puzzle-piece"></i>
          Componentes de: <span id="spanNombreProducto"></span>
        </h5>
        <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
      </div>
      <div class="modal-body">

        <div id="divTablaComponentes">
          <div class="d-flex justify-content-between align-items-center mb-2" id="divBuscaComp">
            <input type="text" id="txtBuscaComp" class="form-control form-control-sm w-50"
                   placeholder="Buscar por código o nombre..." oninput="filtrarComp()" />
            <div id="divPaginacionComp" style="font-size:.84rem;"></div>
          </div>
          <table class="table table-bordered comp-view-table" id="tblComponentes">
            <thead>
              <tr>
                <th>Material</th>
                <th>Consumo por unidad</th>
                <th>Notas</th>
                <th style="width:80px;">Acción</th>
              </tr>
            </thead>
            <tbody id="tbodyComponentes">
            </tbody>
          </table>
          <div id="divSinCompModal" class="text-muted text-center py-3" style="display:none; font-size:.88rem;">
            <i class="fas fa-info-circle"></i> Este producto no tiene componentes registrados.
          </div>
        </div>

        <hr />
        <h6 style="color:#003366;font-weight:600;"><i class="fas fa-copy"></i> Copiar componentes de otro producto</h6>
        <small class="text-muted d-block mb-2" style="font-size:.8rem;">
          <i class="fas fa-info-circle text-info"></i>
          Busca un producto y copia todos sus componentes a este. Los que ya existen se omiten automáticamente.
        </small>
        <div class="row align-items-end mb-3">
          <div class="col-md-9" style="position:relative;">
            <label style="font-size:.84rem;">Buscar producto origen</label>
            <input type="text" id="txtBuscaProductoClone" class="form-control form-control-sm"
              placeholder="Escribe el código o descripción..." autocomplete="off"
              oninput="filtrarProductosClone(this.value)"
              onblur="setTimeout(function(){ var d=document.getElementById('divResultadosClone'); if(d) d.style.display='none'; }, 200)" />
            <div id="divResultadosClone" style="display:none; position:absolute; z-index:1050;
              width:calc(100% - 30px); max-height:200px; overflow-y:auto; background:#fff;
              border:1px solid #ced4da; border-radius:4px; box-shadow:0 4px 8px rgba(0,0,0,.15);">
            </div>
          </div>
          <div class="col-md-3">
            <button type="button" id="btnEjecutarClone" class="btn btn-info btn-sm w-100"
              disabled onclick="clonarComponentes()">
              <i class="fas fa-copy"></i> Copiar
            </button>
          </div>
        </div>

        <hr />
        <h6 style="color:#003366;font-weight:600;"><i class="fas fa-plus-circle"></i> Agregar componente</h6>
        <small class="text-muted d-block mb-2" style="font-size:.8rem;">
          <i class="fas fa-info-circle text-info"></i>
          Define cuánto de cada material se consume al fabricar <strong>una unidad</strong> del producto.
        </small>
        <div class="row align-items-end">
          <div class="col-md-4">
            <label style="font-size:.84rem;">Material <span style="color:red">*</span></label>
            <div style="position:relative;">
              <input type="text" id="txtBuscarMatComp" class="form-control form-control-sm"
                     placeholder="Escribe código o nombre..." autocomplete="off"
                     oninput="onBuscarMatComp()" />
              <div id="divResultadosMatComp"
                   style="display:none; position:absolute; z-index:9999; width:100%;
                          background:#fff; border:1px solid #ccc; border-radius:4px;
                          max-height:220px; overflow-y:auto;
                          box-shadow:0 2px 8px rgba(0,0,0,.18);">
              </div>
            </div>
          </div>
          <div class="col-md-2">
            <label style="font-size:.84rem;">Unidad</label>
            <select id="ddlUnidadComp" class="form-control form-control-sm">
              <option value="">(base)</option>
            </select>
          </div>
          <div class="col-md-2">
            <label style="font-size:.84rem;">Consumo <span style="color:red">*</span></label>
            <input type="number" id="txtCantConsumoComp" class="form-control form-control-sm" value="0" min="0" step="0.01" />
          </div>
          <div class="col-md-3">
            <label style="font-size:.84rem;">Notas</label>
            <input type="text" id="txtNotasComp" class="form-control form-control-sm" placeholder="Opcional" maxlength="200" />
          </div>
          <div class="col-md-1 mt-1">
            <button type="button" class="btn btn-success btn-sm w-100" onclick="agregarComponenteModal()" title="Agregar">
              <i class="fas fa-plus"></i>
            </button>
          </div>
        </div>

        <asp:HiddenField ID="hdnCompProductoID"    runat="server" Value="" />
        <asp:HiddenField ID="hdnCompAccion"        runat="server" Value="" />
        <asp:HiddenField ID="hdnCompMaterialID"    runat="server" Value="" />
        <asp:HiddenField ID="hdnCompCantConsumo"   runat="server" Value="" />
        <asp:HiddenField ID="hdnCompNotas"         runat="server" Value="" />
        <asp:HiddenField ID="hdnCompPMID"          runat="server" Value="" />
        <asp:HiddenField ID="hdnCompConversionID"  runat="server" Value="" />
        <asp:HiddenField ID="hdnProductoCloneID"   runat="server" Value="" />

      </div>
      <div class="modal-footer">
        <button type="button" class="btn btn-secondary" data-dismiss="modal">Cerrar</button>
      </div>
    </div>
  </div>
</div>

<asp:Literal ID="litJsData" runat="server"></asp:Literal>

<script>
    // ── Toggle accordion de stock por base ──────────────
    function toggleAcordeon(id, btn) {
        var row = document.getElementById(id);
        if (!row) return;
        var visible = row.style.display !== 'none';
        row.style.display = visible ? 'none' : 'table-row';
        btn.innerHTML = visible
            ? '<i class="fas fa-layer-group"></i> Ver bases'
            : '<i class="fas fa-times"></i> Cerrar';
    }

    // ── Restricción: máximo 2 decimales en campos numéricos ──────────
    window.addEventListener('load', function () {
        document.querySelectorAll('input[type="number"][step="0.01"]').forEach(function (el) {
            el.addEventListener('blur', function () {
                if (this.value === '') return;
                var num = parseFloat(this.value);
                if (!isNaN(num)) this.value = parseFloat(num.toFixed(2));
            });
        });
    });

    // ════════════════════════════════════════════════════
    // MENSAJE PENDIENTE
    // ════════════════════════════════════════════════════
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

            if (msg.reopenComp) {
                var pid = document.getElementById('<%= hdnCompProductoID.ClientID %>').value;
                var pnom = document.getElementById('spanNombreProducto')
                    ? document.getElementById('spanNombreProducto').innerText : '';
                if (pid) setTimeout(function () { cargarComponentesModal(pid, pnom); }, 300);
            }
        } catch (e) { }
    });

    function abrirModalNuevo() {
        $('#modalNuevo').modal('show');
    }

    // ════════════════════════════════════════════════════
    // MODAL EDITAR PRODUCTO
    // ════════════════════════════════════════════════════
    function abrirModalEditar(id, codigo, descripcion, tipoID, precio, rowVersion) {
        document.getElementById('<%= hdnProductoID.ClientID %>').value = id;
        document.getElementById('<%= hdnRowVersion.ClientID %>').value = rowVersion;
        document.getElementById('<%= txtCodigoEdit.ClientID %>').value = codigo;
        document.getElementById('<%= txtDescripcionEdit.ClientID %>').value = descripcion;
        document.getElementById('<%= ddlTipoEdit.ClientID %>').value = tipoID;
        document.getElementById('<%= txtPrecioEdit.ClientID %>').value = precio;
        $('#modalEditar').modal('show');
    }

    // ════════════════════════════════════════════════════
    // MODAL COMPONENTES — Ver y editar
    // ════════════════════════════════════════════════════
    var _compModal          = [];
    var _compModalFilter    = '';
    var _compModalPage      = 0;
    var _compModalPageSize  = 25;
    var _productoCloneSeleccionado = null;

    function verComponentes(productoID, descripcion) {
        document.getElementById('<%= hdnCompProductoID.ClientID %>').value = productoID;
        document.getElementById('spanNombreProducto').innerText = descripcion;
        // Resetear sección clone
        var txtClone = document.getElementById('txtBuscaProductoClone');
        if (txtClone) txtClone.value = '';
        var divRes = document.getElementById('divResultadosClone');
        if (divRes) divRes.style.display = 'none';
        var btnClone = document.getElementById('btnEjecutarClone');
        if (btnClone) btnClone.disabled = true;
        _productoCloneSeleccionado = null;
        cargarComponentesModal(productoID, descripcion);
    }

    // ════════════════════════════════════════════════════
    // CLONAR BOM
    // ════════════════════════════════════════════════════
    function filtrarProductosClone(valor) {
        var q = valor.trim().toLowerCase();
        var divRes = document.getElementById('divResultadosClone');
        var btnClone = document.getElementById('btnEjecutarClone');
        _productoCloneSeleccionado = null;
        btnClone.disabled = true;
        if (q.length < 2) { divRes.style.display = 'none'; return; }

        var prodActualID = document.getElementById('<%= hdnCompProductoID.ClientID %>').value;
        var lista = (window._productosParaClone || []).filter(function (p) {
            return String(p.productoID) !== String(prodActualID) &&
                   (p.codigo.toLowerCase().indexOf(q) !== -1 ||
                    p.descripcion.toLowerCase().indexOf(q) !== -1);
        });

        divRes.innerHTML = '';
        if (lista.length === 0) {
            var sinRes = document.createElement('div');
            sinRes.style.cssText = 'padding:8px 10px;';
            sinRes.className = 'text-muted small';
            sinRes.textContent = 'Sin resultados.';
            divRes.appendChild(sinRes);
            divRes.style.display = 'block';
            return;
        }

        lista.slice(0, 50).forEach(function (p) {
            var item = document.createElement('div');
            item.style.cssText = 'padding:6px 10px; cursor:pointer; border-bottom:1px solid #f0f0f0; font-size:.88rem;';
            item.innerHTML = '<strong>[' + escHtml(p.codigo) + ']</strong> ' + escHtml(p.descripcion) +
                             ' <span class="badge badge-secondary ml-1">' + p.totalComponentes + '</span>';
            item.addEventListener('mouseenter', function () { this.style.background = '#f0f4f8'; });
            item.addEventListener('mouseleave', function () { this.style.background = ''; });
            item.addEventListener('mousedown', function () {
                selectProductoClone(p.productoID, p.codigo, p.descripcion, p.totalComponentes);
            });
            divRes.appendChild(item);
        });
        divRes.style.display = 'block';
    }

    function selectProductoClone(productoID, codigo, descripcion, totalComp) {
        _productoCloneSeleccionado = { productoID: productoID, codigo: codigo, descripcion: descripcion, totalComp: totalComp };
        document.getElementById('txtBuscaProductoClone').value = '[' + codigo + '] ' + descripcion;
        document.getElementById('divResultadosClone').style.display = 'none';
        document.getElementById('btnEjecutarClone').disabled = false;
    }

    function clonarComponentes() {
        if (!_productoCloneSeleccionado) return;
        var src = _productoCloneSeleccionado;
        Swal.fire({
            icon: 'question',
            title: '¿Copiar componentes?',
            html: 'Se copiarán los componentes de <b>[' + escHtml(src.codigo) + '] ' + escHtml(src.descripcion) + '</b>.<br>' +
                  '<small class="text-muted">Los materiales que ya existen en este producto se omitirán.</small>',
            showCancelButton: true,
            confirmButtonText: 'Sí, copiar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#003366',
            cancelButtonColor: '#6c757d'
        }).then(function (r) {
            if (r.isConfirmed) {
                document.getElementById('<%= hdnProductoCloneID.ClientID %>').value = src.productoID;
                document.getElementById('<%= hdnCompAccion.ClientID %>').value = 'CLONE';
                __doPostBack('<%= btnGuardarComponentes.UniqueID %>', '');
            } else {
                $('#modalComponentes').modal('show');
            }
        });
    }

    function cargarComponentesModal(productoID, descripcion) {
        _compModalFilter = '';
        _compModalPage   = 0;
        var txtB = document.getElementById('txtBuscaComp');
        if (txtB) txtB.value = '';

        var data = window._componentesData || {};
        _compModal = data[productoID] || [];

        // Resetear autocomplete de material
        document.getElementById('txtBuscarMatComp').value = '';
        document.getElementById('<%= hdnCompMaterialID.ClientID %>').value = '';
        _matCompSeleccionado = null;
        ocultarResultadosMatComp();
        document.getElementById('ddlUnidadComp').innerHTML = '<option value="">(base)</option>';

        renderTablaComp();
        document.getElementById('spanNombreProducto').innerText = descripcion || '';
        document.getElementById('txtCantConsumoComp').value = '0';
        document.getElementById('txtNotasComp').value = '';

        $('#modalComponentes').modal('show');
    }

    // ── Autocomplete AJAX de material en modal BOM ──────────────────
    var _matCompSeleccionado = null;
    var _buscarMatCompTimer  = null;

    function onBuscarMatComp() {
        clearTimeout(_buscarMatCompTimer);
        var q = document.getElementById('txtBuscarMatComp').value.trim();
        _matCompSeleccionado = null;
        document.getElementById('<%= hdnCompMaterialID.ClientID %>').value = '';
        if (q.length < 2) { ocultarResultadosMatComp(); return; }
        _buscarMatCompTimer = setTimeout(function () { ejecutarBusquedaMatComp(q); }, 300);
    }

    function ejecutarBusquedaMatComp(query) {
        fetch('<%= ResolveUrl("~/Productos.aspx/BuscarMateriales") %>', {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify({ query: query })
        })
        .then(function (r) { return r.json(); })
        .then(function (data) { mostrarResultadosMatComp(data.d); })
        .catch(function () { ocultarResultadosMatComp(); });
    }

    function mostrarResultadosMatComp(items) {
        var div = document.getElementById('divResultadosMatComp');
        div.innerHTML = '';
        if (!items || items.length === 0) {
            div.innerHTML = '<div style="padding:8px 10px;color:#888;font-size:.84rem;">Sin resultados</div>';
            div.style.display = '';
            return;
        }
        items.forEach(function (item) {
            var el = document.createElement('div');
            el.style.cssText = 'padding:7px 10px;cursor:pointer;font-size:.84rem;border-bottom:1px solid #f0f0f0;';
            el.textContent   = item.nombre;
            el.onmouseenter  = function () { el.style.background = '#e8f0fe'; };
            el.onmouseleave  = function () { el.style.background = ''; };
            el.onmousedown   = function (e) { e.preventDefault(); seleccionarMatComp(item); };
            div.appendChild(el);
        });
        div.style.display = '';
    }

    function seleccionarMatComp(item) {
        _matCompSeleccionado = item;
        document.getElementById('txtBuscarMatComp').value  = item.nombre;
        document.getElementById('<%= hdnCompMaterialID.ClientID %>').value = item.id;
        ocultarResultadosMatComp();
        poblarUnidadesMatComp(item.conversiones || []);
    }

    function ocultarResultadosMatComp() {
        var d = document.getElementById('divResultadosMatComp');
        if (d) d.style.display = 'none';
    }

    function poblarUnidadesMatComp(conversiones) {
        var sel = document.getElementById('ddlUnidadComp');
        sel.innerHTML = '';
        if (!conversiones || conversiones.length === 0) {
            sel.innerHTML = '<option value="">(base)</option>';
            return;
        }
        conversiones.forEach(function (op) {
            var opt = document.createElement('option');
            opt.value = op.valor;
            opt.text  = op.texto;
            opt.setAttribute('data-factor', op.factor);
            sel.appendChild(opt);
        });
    }

    document.addEventListener('click', function (e) {
        var inp = document.getElementById('txtBuscarMatComp');
        var res = document.getElementById('divResultadosMatComp');
        if (inp && res && !inp.contains(e.target) && !res.contains(e.target))
            ocultarResultadosMatComp();
    });

    // Extrae la abreviatura entre paréntesis: "Litro/s (L)" → "L"
    function abrevUnidad(unidadStr) {
        var m = /\(([^)]+)\)\s*$/.exec(unidadStr || '');
        return m ? m[1] : (unidadStr || '');
    }

    function renderTablaComp() {
        var tbody  = document.getElementById('tbodyComponentes');
        var sinDiv = document.getElementById('divSinCompModal');
        var divPag = document.getElementById('divPaginacionComp');
        tbody.innerHTML = '';

        sinDiv.style.display = (_compModal.length === 0) ? 'block' : 'none';

        // Filtrar
        var q = (_compModalFilter || '').toLowerCase();
        var filtered = q
            ? _compModal.filter(function (c) {
                  return (c.materialCodigo || '').toLowerCase().indexOf(q) >= 0 ||
                         (c.materialNombre || '').toLowerCase().indexOf(q) >= 0;
              })
            : _compModal.slice();

        var total = filtered.length;
        var start = _compModalPage * _compModalPageSize;
        if (start >= total && total > 0) { _compModalPage = 0; start = 0; }
        var end   = Math.min(start + _compModalPageSize, total);
        var pag   = filtered.slice(start, end);

        pag.forEach(function (c) {
            var abrev = abrevUnidad(c.unidad);
            var consumoDisplay;
            if (c.conversionID && c.cantConsumoCap != null) {
                var convs   = window._conversionesMat && window._conversionesMat[c.materialID.toString()];
                var conv    = convs && convs.find(function (op) { return String(op.val) === String(c.conversionID); });
                var unidTxt = conv ? conv.txt.replace(/\s*\[.*?\]\s*$/, '').trim() : '';
                consumoDisplay = '<small class="text-muted d-block">' + escHtml(String(c.cantConsumoCap)) + ' ' + escHtml(unidTxt) + '</small>' +
                                 '<strong>' + escHtml(String(c.cantConsumo)) + '</strong> <span class="text-muted">' + escHtml(abrev) + '</span>';
            } else {
                consumoDisplay = '<strong>' + escHtml(String(c.cantConsumo)) + '</strong> <span class="text-muted">' + escHtml(abrev) + '</span>';
            }

            var tr = document.createElement('tr');
            tr.innerHTML =
                '<td><strong>[' + escHtml(c.materialCodigo) + '] ' + escHtml(c.materialNombre) + '</strong><br><small class="text-muted">' + escHtml(c.unidad) + '</small></td>' +
                '<td>' + consumoDisplay + '<input type="number" class="form-control form-control-sm comp-edit-row mt-1" value="' + c.cantConsumo + '" min="0" step="0.01" id="ccons_' + c.pmID + '" /></td>' +
                '<td><input type="text" class="form-control form-control-sm comp-edit-row" value="' + escHtml(c.notas) + '" maxlength="200" id="cnot_' + c.pmID + '" /></td>' +
                '<td class="text-center">' +
                '<button type="button" class="btn btn-success btn-xs btn-sm mr-1" onclick="guardarCompExistente(' + c.pmID + ', ' + c.materialID + ')" title="Guardar"><i class="fas fa-save"></i></button>' +
                '<button type="button" class="btn btn-danger  btn-xs btn-sm"      onclick="eliminarComp(' + c.pmID + ')" title="Eliminar"><i class="fas fa-trash"></i></button>' +
                '</td>';
            tbody.appendChild(tr);
        });

        // Controles de paginación
        if (divPag) {
            if (total === 0) {
                divPag.innerHTML = q ? '<small class="text-muted">Sin resultados para "<em>' + escHtml(q) + '</em>".</small>' : '';
            } else if (total <= _compModalPageSize && !q) {
                divPag.innerHTML = '<small class="text-muted">' + total + ' componente' + (total !== 1 ? 's' : '') + '</small>';
            } else {
                divPag.innerHTML =
                    '<small class="text-muted mr-2">' + (start + 1) + '–' + end + ' de ' + total + '</small>' +
                    '<button class="btn btn-sm btn-outline-secondary mr-1" ' + (_compModalPage === 0 ? 'disabled' : '') + ' onclick="compPrevPage()">&#8249; Ant</button>' +
                    '<button class="btn btn-sm btn-outline-secondary"      ' + (end >= total ? 'disabled' : '')         + ' onclick="compNextPage()">Sig &#8250;</button>';
            }
        }
    }

    function filtrarComp()  { _compModalFilter = document.getElementById('txtBuscaComp').value; _compModalPage = 0; renderTablaComp(); }
    function compPrevPage() { if (_compModalPage > 0) { _compModalPage--; renderTablaComp(); } }
    function compNextPage() { _compModalPage++; renderTablaComp(); }

    function guardarCompExistente(pmID, materialID) {
        var cantConsumo = parseFloat(document.getElementById('ccons_' + pmID).value) || 0;
        var notas = document.getElementById('cnot_' + pmID).value;

        document.getElementById('<%= hdnCompAccion.ClientID %>').value          = 'UPDATE';
        document.getElementById('<%= hdnCompPMID.ClientID %>').value             = pmID;
        document.getElementById('<%= hdnCompCantConsumo.ClientID %>').value      = cantConsumo;
        document.getElementById('<%= hdnCompNotas.ClientID %>').value            = notas;
        document.getElementById('<%= hdnCompConversionID.ClientID %>').value     = '0'; // edición inline = unidad base
        __doPostBack('<%= btnGuardarComponentes.UniqueID %>', '');
    }

    function eliminarComp(pmID) {
        Swal.fire({
            icon: 'warning', title: '¿Eliminar componente?',
            text: 'Se eliminará este componente del producto.',
            showCancelButton: true, confirmButtonText: 'Sí, eliminar',
            confirmButtonColor: '#e74c3c', cancelButtonColor: '#6c757d'
        }).then(function (r) {
            if (r.isConfirmed) {
                document.getElementById('<%= hdnCompAccion.ClientID %>').value = 'DELETE';
                document.getElementById('<%= hdnCompPMID.ClientID %>').value   = pmID;
                __doPostBack('<%= btnGuardarComponentes.UniqueID %>', '');
            }
        });
    }

    function agregarComponenteModal() {
        var matID       = document.getElementById('<%= hdnCompMaterialID.ClientID %>').value;
        var cantConsumo = parseFloat(document.getElementById('txtCantConsumoComp').value) || 0;
        var notas       = document.getElementById('txtNotasComp').value;
        var ddlU        = document.getElementById('ddlUnidadComp');

        // Extraer ConversionID numérico: "conv:456" → 456, "base:..." → 0
        var rawVal = ddlU ? ddlU.value : '';
        var convID = 0;
        if (rawVal && rawVal.indexOf('conv:') === 0)
            convID = parseInt(rawVal.substring(5)) || 0;

        if (!matID) {
            Swal.fire({ icon: 'warning', title: 'Material requerido',
                text: 'Seleccione un material.', confirmButtonColor: '#003366' })
                .then(function () { $('#modalComponentes').modal('show'); });
            return;
        }

        // Verificar si el material ya existe en el BOM y cuántas veces
        var vecesAgregado = _compModal.filter(function (c) {
            return String(c.materialID) === String(matID);
        }).length;

        function doInsert() {
            document.getElementById('<%= hdnCompAccion.ClientID %>').value           = 'INSERT';
            document.getElementById('<%= hdnCompMaterialID.ClientID %>').value       = matID;
            document.getElementById('<%= hdnCompCantConsumo.ClientID %>').value      = cantConsumo;
            document.getElementById('<%= hdnCompNotas.ClientID %>').value            = notas;
            document.getElementById('<%= hdnCompConversionID.ClientID %>').value     = convID;
            __doPostBack('<%= btnGuardarComponentes.UniqueID %>', '');
        }

        if (vecesAgregado >= 1) {
            var veces = vecesAgregado === 1 ? '1 vez' : vecesAgregado + ' veces';
            var nomMat = _matCompSeleccionado ? _matCompSeleccionado.nombre : 'Este material';
            Swal.fire({
                icon: 'question',
                title: 'Material duplicado',
                html: '<b>' + escHtml(nomMat) + '</b> ya está agregado como componente <b>' + veces + '</b>.<br>¿Desea agregarlo de nuevo?',
                showCancelButton: true,
                confirmButtonText: 'Sí, agregar de nuevo',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#003366',
                cancelButtonColor: '#6c757d'
            }).then(function (r) {
                if (r.isConfirmed) doInsert();
                else $('#modalComponentes').modal('show');
            });
        } else {
            doInsert();
        }
    }

    // ════════════════════════════════════════════════════
    // TOGGLE
    // ════════════════════════════════════════════════════
    function confirmarToggle(prodID, descripcion, activo) {
        var accion = activo ? 'desactivar' : 'activar';
        Swal.fire({
            icon: activo ? 'warning' : 'question',
            title: '¿' + (activo ? 'Desactivar' : 'Activar') + ' producto?',
            html: '¿Seguro de <b>' + accion + '</b> el producto <b>' + descripcion + '</b>?',
            showCancelButton: true, confirmButtonText: 'Sí, ' + accion,
            confirmButtonColor: activo ? '#e0a800' : '#28a745',
            cancelButtonColor: '#6c757d'
        }).then(function (r) {
            if (r.isConfirmed) {
                document.getElementById('<%= hdnToggleProductoID.ClientID %>').value = prodID;
                __doPostBack('<%= btnToggleHidden.UniqueID %>', '');
            }
        });
        return false;
    }

    // ════════════════════════════════════════════════════
    // VALIDACIONES
    // ════════════════════════════════════════════════════
    function prepararYValidarNuevo() {
        return _validarProd(
            '<%= txtCodigo.ClientID %>',
            '<%= txtDescripcion.ClientID %>',
            '<%= ddlTipo.ClientID %>',
            '<%= txtPrecio.ClientID %>',
            'modalNuevo');
    }

    function validarEditar() {
        return _validarProd(
            '<%= txtCodigoEdit.ClientID %>',
            '<%= txtDescripcionEdit.ClientID %>',
            '<%= ddlTipoEdit.ClientID %>',
            '<%= txtPrecioEdit.ClientID %>',
            'modalEditar');
    }

    function _validarProd(idCod, idDesc, idTipo, idPre, modal) {
        var cod = document.getElementById(idCod).value.trim();
        var desc = document.getElementById(idDesc).value.trim();
        var tipo = document.getElementById(idTipo).value;
        var pre = parseFloat(document.getElementById(idPre).value) || 0;

        function warn(txt) {
            Swal.fire({ icon: 'warning', title: 'Campo inválido', text: txt, confirmButtonColor: '#003366' })
                .then(function () { $('#' + modal).modal('show'); });
            return false;
        }
        if (!cod || cod.length < 2) return warn('El código es obligatorio (mínimo 2 caracteres).');
        if (!desc || desc.length < 3) return warn('La descripción es obligatoria (mínimo 3 caracteres).');
        if (!tipo) return warn('Debe seleccionar el tipo de producto.');
        if (pre < 0) return warn('El precio no puede ser negativo.');
        return true;
    }

    // ════════════════════════════════════════════════════
    // UTILIDADES
    // ════════════════════════════════════════════════════
    function escHtml(s) {
        if (!s) return '';
        return String(s)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }
</script>

</asp:Content>
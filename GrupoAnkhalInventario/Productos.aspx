<%@ Page Title="Productos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Productos.aspx.cs" Inherits="GrupoAnkhalInventario.Productos" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="css/gridviewPantalla.css" rel="stylesheet" />
    <style>
        /* ── Dashboard cards ── */
        .prod-dashboard { display:flex; gap:14px; margin-bottom:18px; flex-wrap:wrap; }
        .prod-card {
            flex:1; min-width:150px; border-radius:10px; padding:16px 20px;
            color:#fff; display:flex; align-items:center; gap:14px;
            box-shadow:0 3px 10px rgba(0,0,0,.15);
            transition:transform .15s, box-shadow .15s;
        }
        .prod-card:hover { transform:translateY(-3px); box-shadow:0 6px 16px rgba(0,0,0,.2); }
        .prod-card.total    { background:linear-gradient(135deg,#1a5276,#2980b9); }
        .prod-card.tarima   { background:linear-gradient(135deg,#6c3483,#9b59b6); }
        .prod-card.caja     { background:linear-gradient(135deg,#1e8449,#27ae60); }
        .prod-card.accesorio{ background:linear-gradient(135deg,#d35400,#e67e22); }
        .prod-card .icon  { font-size:2.2rem; opacity:.9; }
        .prod-card .info .num { font-size:2rem; font-weight:700; line-height:1; }
        .prod-card .info .lbl { font-size:.78rem; opacity:.9; text-transform:uppercase; letter-spacing:.5px; }

        /* ── Filtros ── */
        .filtros-bar {
            background:#f8f9fa; border:1px solid #dee2e6;
            border-radius:8px; padding:14px 18px; margin-bottom:14px;
        }
        .filtros-bar label { font-weight:600; font-size:.84rem; color:#003366; margin-bottom:2px; }

        /* ── Componentes en modal (nuevo/editar) ── */
        .comp-row { background:#f8f9fa; border-radius:6px; padding:10px 12px; margin-bottom:8px; border:1px solid #dee2e6; }
        .comp-row:hover { border-color:#003366; }
        .btn-remove-comp { color:#e74c3c; background:none; border:none; font-size:1.1rem; cursor:pointer; padding:0 4px; }
        .btn-remove-comp:hover { color:#c0392b; }
        #divComponentes { max-height:320px; overflow-y:auto; padding-right:4px; }

        /* ── Modal componentes (ver/editar) ── */
        .comp-view-table th { background:#003366; color:#fff; }
        .comp-view-table td, .comp-view-table th { padding:8px 12px; vertical-align:middle; }
        .comp-edit-row input { width:80px; }

        /* ── Paginador ── */
        .pager-custom span { background:#003366; color:#fff; font-weight:700; border-radius:4px; padding:4px 9px; }
        .pager-custom a    { padding:4px 9px; border-radius:4px; }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
<div class="container-fluid">
<div class="row">
<div class="col-12">

    <!-- ══ DASHBOARD ══════════════════════════════════════ -->
    <div class="prod-dashboard">
        <div class="prod-card total">
            <div class="icon"><i class="fas fa-boxes"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblTotal"     runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Total productos</div>
            </div>
        </div>
        <div class="prod-card tarima">
            <div class="icon"><i class="fas fa-pallet"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblTarimas"   runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Tarimas</div>
            </div>
        </div>
        <div class="prod-card caja">
            <div class="icon"><i class="fas fa-box"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblCajas"     runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Cajas</div>
            </div>
        </div>
        <div class="prod-card accesorio">
            <div class="icon"><i class="fas fa-tools"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblAccesorios" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Accesorios</div>
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
                        <label>Buscar por Nombre o Código</label>
                        <asp:TextBox ID="txtBuscar" runat="server" CssClass="form-control form-control-sm"
                            Placeholder="Nombre o código..."></asp:TextBox>
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
                    AllowPaging="True" PageSize="15"
                    OnPageIndexChanging="gvProductos_PageIndexChanging"
                    DataKeyNames="ProductoID"
                    PagerStyle-CssClass="pager-custom"
                    PagerSettings-Mode="NumericFirstLast"
                    PagerSettings-FirstPageText="«"
                    PagerSettings-LastPageText="»"
                    PagerSettings-PageButtonCount="5">
                    <Columns>
                        <asp:BoundField DataField="ProductoID"     HeaderText="ID"          Visible="false" />
                        <asp:BoundField DataField="Codigo"         HeaderText="Código" />
                        <asp:BoundField DataField="Nombre"         HeaderText="Nombre" />
                        <asp:BoundField DataField="TipoNombre"     HeaderText="Tipo" />
                        <asp:BoundField DataField="Descripcion"    HeaderText="Descripción" />
                        <asp:BoundField DataField="PrecioVenta"    HeaderText="Precio Venta" DataFormatString="{0:C2}" />
                        <asp:TemplateField HeaderText="Componentes">
                            <ItemTemplate>
                                <button type="button" class="btn btn-info btn-sm"
                                    onclick="verComponentes('<%# Eval("ProductoID") %>', '<%# Server.HtmlEncode((Eval("Nombre") ?? "").ToString()) %>')">
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
                                        '<%# Server.HtmlEncode((Eval("Nombre") ?? "").ToString()) %>',
                                        '<%# Eval("TipoProductoID") %>',
                                        '<%# Server.HtmlEncode((Eval("Descripcion") ?? "").ToString()) %>',
                                        '<%# Eval("PrecioVenta") %>'
                                    )">
                                    <i class="fas fa-edit"></i> Editar
                                </button>
                                <asp:Button ID="btnToggle" runat="server"
                                    CssClass='<%# Convert.ToBoolean(Eval("Activo")) ? "btn btn-warning btn-sm" : "btn btn-success btn-sm" %>'
                                    Text='<%# Convert.ToBoolean(Eval("Activo")) ? "Desactivar" : "Activar" %>'
                                    CommandArgument='<%# Eval("ProductoID") %>'
                                    OnClientClick='<%# "return confirmarToggle(\"" + Eval("ProductoID") + "\", \"" + Server.HtmlEncode((Eval("Nombre") ?? "").ToString()) + "\", " + Eval("Activo").ToString().ToLower() + ");" %>'
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
              <label>Nombre <span style="color:red">*</span></label>
              <asp:TextBox ID="txtNombre" runat="server" CssClass="form-control" Placeholder="Nombre completo del producto" MaxLength="200"></asp:TextBox>
            </div>
          </div>
        </div>
        <div class="row">
          <div class="col-md-4">
            <div class="form-group">
              <label>Tipo <span style="color:red">*</span></label>
              <asp:DropDownList ID="ddlTipo" runat="server" CssClass="form-control"></asp:DropDownList>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Precio de venta <span style="color:red">*</span></label>
              <div class="input-group">
                <div class="input-group-prepend"><span class="input-group-text">$</span></div>
                <asp:TextBox ID="txtPrecio" runat="server" CssClass="form-control" Placeholder="0.00" TextMode="Number" min="0" step="0.01"></asp:TextBox>
              </div>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Descripción</label>
              <asp:TextBox ID="txtDescripcion" runat="server" CssClass="form-control" Placeholder="Opcional" MaxLength="500"></asp:TextBox>
            </div>
          </div>
        </div>

        <hr />
        <div class="d-flex justify-content-between align-items-center mb-2">
          <h6 style="color:#003366;font-weight:600;margin:0;">
            <i class="fas fa-puzzle-piece"></i> Componentes del producto
            <small class="text-muted font-weight-normal"> (materiales que lo forman — opcional)</small>
          </h6>
          <button type="button" class="btn btn-outline-primary btn-sm" onclick="agregarComponenteNuevo()">
            <i class="fas fa-plus"></i> Agregar componente
          </button>
        </div>
        <div id="divComponentesNuevo">
          <!-- filas de componentes se agregan dinámicamente -->
        </div>
        <div id="divSinComponentes" class="text-muted text-center py-2" style="font-size:.88rem;">
          <i class="fas fa-info-circle"></i> Sin componentes agregados. Puedes dejar vacío o agregar los que necesites.
        </div>

        <!-- Campo oculto para pasar JSON de componentes al servidor -->
        <asp:HiddenField ID="hdnComponentesNuevo" runat="server" Value="[]" />
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

<!-- ══ MODAL EDITAR PRODUCTO (solo datos, sin componentes) ══ -->
<div class="modal fade" id="modalEditar" tabindex="-1" role="dialog" data-backdrop="static">
  <div class="modal-dialog modal-lg" role="document">
    <div class="modal-content">
      <div class="modal-header" style="background-color:#003366;color:white;">
        <h5 class="modal-title"><i class="fas fa-edit"></i> Editar Producto</h5>
        <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
      </div>
      <div class="modal-body">
        <asp:HiddenField ID="hdnProductoID" runat="server" />
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
              <label>Nombre <span style="color:red">*</span></label>
              <asp:TextBox ID="txtNombreEdit" runat="server" CssClass="form-control" MaxLength="200"></asp:TextBox>
            </div>
          </div>
        </div>
        <div class="row">
          <div class="col-md-4">
            <div class="form-group">
              <label>Tipo <span style="color:red">*</span></label>
              <asp:DropDownList ID="ddlTipoEdit" runat="server" CssClass="form-control"></asp:DropDownList>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Precio de venta <span style="color:red">*</span></label>
              <div class="input-group">
                <div class="input-group-prepend"><span class="input-group-text">$</span></div>
                <asp:TextBox ID="txtPrecioEdit" runat="server" CssClass="form-control" TextMode="Number" min="0" step="0.01"></asp:TextBox>
              </div>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Descripción</label>
              <asp:TextBox ID="txtDescripcionEdit" runat="server" CssClass="form-control" MaxLength="500"></asp:TextBox>
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

        <!-- Tabla de componentes existentes -->
        <div id="divTablaComponentes">
          <table class="table table-bordered comp-view-table" id="tblComponentes">
            <thead>
              <tr>
                <th>Material</th>
                <th>Cant. Mín</th>
                <th>Cant. Máx</th>
                <th>Notas</th>
                <th style="width:80px;">Acción</th>
              </tr>
            </thead>
            <tbody id="tbodyComponentes">
              <!-- se llena con JS -->
            </tbody>
          </table>
          <div id="divSinCompModal" class="text-muted text-center py-3" style="display:none; font-size:.88rem;">
            <i class="fas fa-info-circle"></i> Este producto no tiene componentes registrados.
          </div>
        </div>

        <hr />
        <!-- Agregar nuevo componente -->
        <h6 style="color:#003366;font-weight:600;"><i class="fas fa-plus-circle"></i> Agregar componente</h6>
        <div class="row align-items-end">
          <div class="col-md-4">
            <label style="font-size:.84rem;">Material <span style="color:red">*</span></label>
            <select id="ddlMaterialComp" class="form-control form-control-sm">
              <option value="">-- Seleccione --</option>
            </select>
          </div>
          <div class="col-md-2">
            <label style="font-size:.84rem;">Cant. Mín <span style="color:red">*</span></label>
            <input type="number" id="txtCantMinComp" class="form-control form-control-sm" value="0" min="0" step="0.01" />
          </div>
          <div class="col-md-2">
            <label style="font-size:.84rem;">Cant. Máx <span style="color:red">*</span></label>
            <input type="number" id="txtCantMaxComp" class="form-control form-control-sm" value="0" min="0" step="0.01" />
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

        <!-- Hidden fields para postback -->
        <asp:HiddenField ID="hdnCompProductoID"  runat="server" Value="" />
        <asp:HiddenField ID="hdnCompAccion"      runat="server" Value="" />
        <asp:HiddenField ID="hdnCompMaterialID"  runat="server" Value="" />
        <asp:HiddenField ID="hdnCompCantMin"     runat="server" Value="" />
        <asp:HiddenField ID="hdnCompCantMax"     runat="server" Value="" />
        <asp:HiddenField ID="hdnCompNotas"       runat="server" Value="" />
        <asp:HiddenField ID="hdnCompPMID"        runat="server" Value="" />

      </div>
      <div class="modal-footer">
        <button type="button" class="btn btn-secondary" data-dismiss="modal">Cerrar</button>
      </div>
    </div>
  </div>
</div>

<asp:Literal ID="litJsData" runat="server"></asp:Literal>

<script>
    // ════════════════════════════════════════════════════
    // MENSAJE PENDIENTE (mismo patrón que Bases/Materiales)
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

        // Si venía del modal de componentes, reabrirlo con datos actualizados
        if (msg.reopenComp) {
            var pid = document.getElementById('<%= hdnCompProductoID.ClientID %>').value;
            var pnom = document.getElementById('spanNombreProducto') ? document.getElementById('spanNombreProducto').innerText : '';
            if (pid) setTimeout(function () { cargarComponentesModal(pid, pnom); }, 300);
        }
    } catch (e) { }
});

    // ════════════════════════════════════════════════════
    // DATOS DE MATERIALES (para los selects de componentes)
    // ════════════════════════════════════════════════════
    var _materiales = window._materialesData || [];

    // ════════════════════════════════════════════════════
    // MODAL NUEVO — componentes dinámicos
    // ════════════════════════════════════════════════════
    var _compNuevo = []; // array de {materialID, nombre, cantMin, cantMax, notas}

    function abrirModalNuevo() {
        _compNuevo = [];
        renderComponentesNuevo();
        $('#modalNuevo').modal('show');
    }

    function agregarComponenteNuevo() {
        // Crear fila con select de material + inputs
        var idx = _compNuevo.length;
        _compNuevo.push({ materialID: '', nombre: '', cantMin: 0, cantMax: 0, notas: '' });
        renderComponentesNuevo();
    }

    function renderComponentesNuevo() {
        var div = document.getElementById('divComponentesNuevo');
        var sinDiv = document.getElementById('divSinComponentes');
        div.innerHTML = '';

        if (_compNuevo.length === 0) {
            sinDiv.style.display = 'block';
            return;
        }
        sinDiv.style.display = 'none';

        _compNuevo.forEach(function (c, i) {
            var opts = '<option value="">-- Material --</option>';
            _materiales.forEach(function (m) {
                var sel = (m.id == c.materialID) ? 'selected' : '';
                opts += '<option value="' + m.id + '" ' + sel + '>' + escHtml(m.nombre) + ' (' + escHtml(m.unidad) + ')</option>';
            });

            var row = document.createElement('div');
            row.className = 'comp-row row align-items-center';
            row.innerHTML =
                '<div class="col-md-4">' +
                '<label style="font-size:.8rem;">Material <span style="color:red">*</span></label>' +
                '<select class="form-control form-control-sm" onchange="_compNuevo[' + i + '].materialID=this.value; _compNuevo[' + i + '].nombre=this.options[this.selectedIndex].text;">' +
                opts + '</select>' +
                '</div>' +
                '<div class="col-md-2">' +
                '<label style="font-size:.8rem;">Cant. Mín <span style="color:red">*</span></label>' +
                '<input type="number" class="form-control form-control-sm" value="' + c.cantMin + '" min="0" step="0.01" ' +
                'onchange="_compNuevo[' + i + '].cantMin=parseFloat(this.value)||0;" />' +
                '</div>' +
                '<div class="col-md-2">' +
                '<label style="font-size:.8rem;">Cant. Máx <span style="color:red">*</span></label>' +
                '<input type="number" class="form-control form-control-sm" value="' + c.cantMax + '" min="0" step="0.01" ' +
                'onchange="_compNuevo[' + i + '].cantMax=parseFloat(this.value)||0;" />' +
                '</div>' +
                '<div class="col-md-3">' +
                '<label style="font-size:.8rem;">Notas</label>' +
                '<input type="text" class="form-control form-control-sm" value="' + escHtml(c.notas) + '" maxlength="200" ' +
                'onchange="_compNuevo[' + i + '].notas=this.value;" />' +
                '</div>' +
                '<div class="col-md-1 text-center" style="padding-top:20px;">' +
                '<button type="button" class="btn-remove-comp" onclick="eliminarCompNuevo(' + i + ')" title="Eliminar">' +
                '<i class="fas fa-trash-alt"></i>' +
                '</button>' +
                '</div>';
            div.appendChild(row);
        });
    }

    function eliminarCompNuevo(idx) {
        _compNuevo.splice(idx, 1);
        renderComponentesNuevo();
    }

    // ════════════════════════════════════════════════════
    // MODAL EDITAR PRODUCTO
    // ════════════════════════════════════════════════════
    function abrirModalEditar(id, codigo, nombre, tipoID, descripcion, precio) {
        document.getElementById('<%= hdnProductoID.ClientID %>').value = id;
    document.getElementById('<%= txtCodigoEdit.ClientID %>').value = codigo;
    document.getElementById('<%= txtNombreEdit.ClientID %>').value = nombre;
    document.getElementById('<%= ddlTipoEdit.ClientID %>').value = tipoID;
    document.getElementById('<%= txtDescripcionEdit.ClientID %>').value = descripcion;
    document.getElementById('<%= txtPrecioEdit.ClientID %>').value = precio;
        $('#modalEditar').modal('show');
    }

    // ════════════════════════════════════════════════════
    // MODAL COMPONENTES — Ver y editar
    // ════════════════════════════════════════════════════
    var _compModal = []; // componentes cargados actualmente

    function verComponentes(productoID, nombre) {
        document.getElementById('<%= hdnCompProductoID.ClientID %>').value = productoID;
        document.getElementById('spanNombreProducto').innerText = nombre;
        cargarComponentesModal(productoID, nombre);
    }

    function cargarComponentesModal(productoID, nombre) {
        // Obtener componentes del servidor via los datos embebidos en la página
        var data = window._componentesData || {};
        _compModal = data[productoID] || [];

        // Llenar select de materiales
        var sel = document.getElementById('ddlMaterialComp');
        sel.innerHTML = '<option value="">-- Seleccione --</option>';
        _materiales.forEach(function (m) {
            sel.innerHTML += '<option value="' + m.id + '">' + escHtml(m.nombre) + ' (' + escHtml(m.unidad) + ')</option>';
        });

        renderTablaComp();
        document.getElementById('spanNombreProducto').innerText = nombre || '';
        // Limpiar form de agregar
        document.getElementById('txtCantMinComp').value = '0';
        document.getElementById('txtCantMaxComp').value = '0';
        document.getElementById('txtNotasComp').value = '';
        document.getElementById('ddlMaterialComp').value = '';

        $('#modalComponentes').modal('show');
    }

    function renderTablaComp() {
        var tbody = document.getElementById('tbodyComponentes');
        var sinDiv = document.getElementById('divSinCompModal');
        tbody.innerHTML = '';

        if (_compModal.length === 0) {
            sinDiv.style.display = 'block';
            return;
        }
        sinDiv.style.display = 'none';

        _compModal.forEach(function (c) {
            var tr = document.createElement('tr');
            tr.innerHTML =
                '<td><strong>' + escHtml(c.materialNombre) + '</strong><br><small class="text-muted">' + escHtml(c.unidad) + '</small></td>' +
                '<td><input type="number" class="form-control form-control-sm comp-edit-row" value="' + c.cantMin + '" min="0" step="0.01" ' +
                'onchange="c.cantMin=parseFloat(this.value)||0;" id="cmin_' + c.pmID + '" /></td>' +
                '<td><input type="number" class="form-control form-control-sm comp-edit-row" value="' + c.cantMax + '" min="0" step="0.01" ' +
                'onchange="c.cantMax=parseFloat(this.value)||0;" id="cmax_' + c.pmID + '" /></td>' +
                '<td><input type="text" class="form-control form-control-sm comp-edit-row" value="' + escHtml(c.notas) + '" maxlength="200" ' +
                'onchange="c.notas=this.value;" id="cnot_' + c.pmID + '" /></td>' +
                '<td class="text-center">' +
                '<button type="button" class="btn btn-success btn-xs btn-sm mr-1" onclick="guardarCompExistente(' + c.pmID + ', ' + c.materialID + ')" title="Guardar"><i class="fas fa-save"></i></button>' +
                '<button type="button" class="btn btn-danger btn-xs btn-sm" onclick="eliminarComp(' + c.pmID + ')" title="Eliminar"><i class="fas fa-trash"></i></button>' +
                '</td>';
            // cerrar la referencia en el closure
            (function (comp) {
                tr.querySelectorAll('input').forEach(function (inp) {
                    inp.addEventListener('change', function () { });
                });
            })(c);
            tbody.appendChild(tr);
        });
    }

    function guardarCompExistente(pmID, materialID) {
        var c = _compModal.find(function (x) { return x.pmID == pmID; });
        if (!c) return;

        var cantMin = parseFloat(document.getElementById('cmin_' + pmID).value) || 0;
        var cantMax = parseFloat(document.getElementById('cmax_' + pmID).value) || 0;
        var notas = document.getElementById('cnot_' + pmID).value;

        if (cantMax < cantMin) {
            Swal.fire({ icon: 'warning', title: 'Rango inválido', text: 'La cantidad máxima debe ser ≥ a la mínima.', confirmButtonColor: '#003366' })
                .then(function () { $('#modalComponentes').modal('show'); });
            return;
        }

        document.getElementById('<%= hdnCompAccion.ClientID %>').value     = 'UPDATE';
    document.getElementById('<%= hdnCompPMID.ClientID %>').value       = pmID;
    document.getElementById('<%= hdnCompCantMin.ClientID %>').value    = cantMin;
    document.getElementById('<%= hdnCompCantMax.ClientID %>').value    = cantMax;
    document.getElementById('<%= hdnCompNotas.ClientID %>').value      = notas;
    __doPostBack('<%= btnGuardarComponentes.UniqueID %>', '');
}

function eliminarComp(pmID) {
    Swal.fire({
        icon:'warning', title:'¿Eliminar componente?',
        text:'Se eliminará este componente del producto.',
        showCancelButton:true, confirmButtonText:'Sí, eliminar',
        confirmButtonColor:'#e74c3c', cancelButtonColor:'#6c757d'
    }).then(function(r){
        if (r.isConfirmed) {
            document.getElementById('<%= hdnCompAccion.ClientID %>').value = 'DELETE';
            document.getElementById('<%= hdnCompPMID.ClientID %>').value   = pmID;
            __doPostBack('<%= btnGuardarComponentes.UniqueID %>', '');
        }
    });
}

function agregarComponenteModal() {
    var matID   = document.getElementById('ddlMaterialComp').value;
    var cantMin = parseFloat(document.getElementById('txtCantMinComp').value) || 0;
    var cantMax = parseFloat(document.getElementById('txtCantMaxComp').value) || 0;
    var notas   = document.getElementById('txtNotasComp').value;
    var prodID  = document.getElementById('<%= hdnCompProductoID.ClientID %>').value;

    if (!matID) {
        Swal.fire({ icon:'warning', title:'Material requerido', text:'Seleccione un material.', confirmButtonColor:'#003366' })
            .then(function(){ $('#modalComponentes').modal('show'); });
        return;
    }
    if (cantMax < cantMin) {
        Swal.fire({ icon:'warning', title:'Rango inválido', text:'La cantidad máxima debe ser ≥ a la mínima.', confirmButtonColor:'#003366' })
            .then(function(){ $('#modalComponentes').modal('show'); });
        return;
    }
    // Verificar duplicado local
    if (_compModal.find(function(c){ return c.materialID == matID; })) {
        Swal.fire({ icon:'warning', title:'Duplicado', text:'Ese material ya está como componente de este producto.', confirmButtonColor:'#003366' })
            .then(function(){ $('#modalComponentes').modal('show'); });
        return;
    }

    document.getElementById('<%= hdnCompAccion.ClientID %>').value     = 'INSERT';
    document.getElementById('<%= hdnCompMaterialID.ClientID %>').value = matID;
    document.getElementById('<%= hdnCompCantMin.ClientID %>').value    = cantMin;
    document.getElementById('<%= hdnCompCantMax.ClientID %>').value    = cantMax;
    document.getElementById('<%= hdnCompNotas.ClientID %>').value      = notas;
    __doPostBack('<%= btnGuardarComponentes.UniqueID %>', '');
}

// ════════════════════════════════════════════════════
// TOGGLE
// ════════════════════════════════════════════════════
function confirmarToggle(prodID, nombre, activo) {
    var accion = activo ? 'desactivar' : 'activar';
    Swal.fire({
        icon: activo ? 'warning' : 'question',
        title: '¿' + (activo?'Desactivar':'Activar') + ' producto?',
        html: '¿Seguro de <b>' + accion + '</b> el producto <b>' + nombre + '</b>?',
        showCancelButton:true, confirmButtonText:'Sí, '+accion,
        confirmButtonColor: activo ? '#e0a800' : '#28a745',
        cancelButtonColor:'#6c757d'
    }).then(function(r){
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
    if (!_validarProd(
        '<%= txtCodigo.ClientID %>',
        '<%= txtNombre.ClientID %>',
        '<%= ddlTipo.ClientID %>',
        '<%= txtPrecio.ClientID %>',
        'modalNuevo')) return false;

    // Validar componentes
    for (var i = 0; i < _compNuevo.length; i++) {
        var c = _compNuevo[i];
        if (!c.materialID) {
            Swal.fire({ icon:'warning', title:'Componente incompleto',
                text:'El componente #' + (i+1) + ' no tiene material seleccionado.',
                confirmButtonColor:'#003366' }).then(function(){ $('#modalNuevo').modal('show'); });
            return false;
        }
        if (c.cantMax < c.cantMin) {
            Swal.fire({ icon:'warning', title:'Rango inválido',
                text:'El componente #' + (i+1) + ': la cantidad máxima debe ser ≥ a la mínima.',
                confirmButtonColor:'#003366' }).then(function(){ $('#modalNuevo').modal('show'); });
            return false;
        }
        // Duplicados
        for (var j = i+1; j < _compNuevo.length; j++) {
            if (_compNuevo[j].materialID === c.materialID) {
                Swal.fire({ icon:'warning', title:'Componente duplicado',
                    text:'El mismo material aparece más de una vez.',
                    confirmButtonColor:'#003366' }).then(function(){ $('#modalNuevo').modal('show'); });
                return false;
            }
        }
    }

    // Serializar componentes al hidden field
    document.getElementById('<%= hdnComponentesNuevo.ClientID %>').value = JSON.stringify(_compNuevo);
    return true;
}

function validarEditar() {
    return _validarProd(
        '<%= txtCodigoEdit.ClientID %>',
        '<%= txtNombreEdit.ClientID %>',
        '<%= ddlTipoEdit.ClientID %>',
        '<%= txtPrecioEdit.ClientID %>',
        'modalEditar');
    }

    function _validarProd(idCod, idNom, idTipo, idPre, modal) {
        var cod = document.getElementById(idCod).value.trim();
        var nom = document.getElementById(idNom).value.trim();
        var tipo = document.getElementById(idTipo).value;
        var pre = parseFloat(document.getElementById(idPre).value) || 0;

        function warn(txt) {
            Swal.fire({ icon: 'warning', title: 'Campo inválido', text: txt, confirmButtonColor: '#003366' })
                .then(function () { $('#' + modal).modal('show'); });
            return false;
        }
        if (!cod || cod.length < 2) return warn('El código es obligatorio (mínimo 2 caracteres).');
        if (!nom || nom.length < 3) return warn('El nombre es obligatorio (mínimo 3 caracteres).');
        if (!tipo) return warn('Debe seleccionar el tipo de producto.');
        if (pre < 0) return warn('El precio no puede ser negativo.');
        return true;
    }

    // ════════════════════════════════════════════════════
    // UTILIDADES
    // ════════════════════════════════════════════════════
    function escHtml(s) {
        if (!s) return '';
        return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }
</script>

</asp:Content>
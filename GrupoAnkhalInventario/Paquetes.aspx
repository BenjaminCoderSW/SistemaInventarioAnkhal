<%@ Page Title="Paquetes" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="Paquetes.aspx.cs" Inherits="GrupoAnkhalInventario.Paquetes" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="css/gridviewPantalla.css" rel="stylesheet" />
    <style>
        /* ── Dashboard ── */
        .paq-dashboard { display:flex; gap:14px; margin-bottom:18px; flex-wrap:wrap; }
        .paq-card {
            flex:1; min-width:150px; border-radius:10px; padding:16px 20px;
            color:#fff; display:flex; align-items:center; gap:14px;
            box-shadow:0 3px 10px rgba(0,0,0,.15);
            transition:transform .15s, box-shadow .15s;
        }
        .paq-card:hover { transform:translateY(-3px); box-shadow:0 6px 16px rgba(0,0,0,.2); }
        .paq-card.total   { background:linear-gradient(135deg,#1a5276,#2980b9); }
        .paq-card.activos { background:linear-gradient(135deg,#1e8449,#27ae60); }
        .paq-card.inactivos { background:linear-gradient(135deg,#7f8c8d,#95a5a6); }
        .paq-card .icon  { font-size:2.2rem; opacity:.9; }
        .paq-card .info .num { font-size:2rem; font-weight:700; line-height:1; }
        .paq-card .info .lbl { font-size:.78rem; opacity:.9; text-transform:uppercase; letter-spacing:.5px; }

        /* ── Filtros ── */
        .filtros-bar {
            background:#f8f9fa; border:1px solid #dee2e6;
            border-radius:8px; padding:14px 18px; margin-bottom:14px;
        }
        .filtros-bar label { font-weight:600; font-size:.84rem; color:#003366; margin-bottom:2px; }

        /* ── Componentes en modal nuevo ── */
        .comp-row { background:#f8f9fa; border-radius:6px; padding:10px 12px; margin-bottom:8px; border:1px solid #dee2e6; }
        .comp-row:hover { border-color:#003366; }
        .btn-remove-comp { color:#e74c3c; background:none; border:none; font-size:1.1rem; cursor:pointer; padding:0 4px; }
        .btn-remove-comp:hover { color:#c0392b; }
        #divComponentesNuevo { max-height:340px; overflow-y:auto; padding-right:4px; }

        /* ── Modal componentes ver/editar ── */
        .comp-view-table th { background:#003366; color:#fff; }
        .comp-view-table td, .comp-view-table th { padding:8px 12px; vertical-align:middle; }

        /* ── Badge tipo componente ── */
        .badge-producto  { background:#6c3483; color:#fff; }
        .badge-material  { background:#1a5276; color:#fff; }

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
    <div class="paq-dashboard">
        <div class="paq-card total">
            <div class="icon"><i class="fas fa-layer-group"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblTotal"    runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Total paquetes</div>
            </div>
        </div>
        <div class="paq-card activos">
            <div class="icon"><i class="fas fa-check-circle"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblActivos"  runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Activos</div>
            </div>
        </div>
        <div class="paq-card inactivos">
            <div class="icon"><i class="fas fa-pause-circle"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblInactivos" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Inactivos</div>
            </div>
        </div>
    </div>

    <div class="card">
        <div class="card-header" style="background-color:#003366;color:white;">
            <h3 class="card-title"><i class="fas fa-layer-group"></i> Paquetes</h3>
        </div>
        <div class="card-body">

            <div class="mb-3">
                <asp:Button ID="btnNuevo" runat="server" Text="＋ Nuevo Paquete"
                    CssClass="btn btn-success"
                    OnClientClick="abrirModalNuevo(); return false;" />
            </div>

            <!-- ── FILTROS ── -->
            <div class="filtros-bar">
                <div class="row align-items-end">
                    <div class="col-md-5">
                        <label>Buscar por Nombre o Código</label>
                        <asp:TextBox ID="txtBuscar" runat="server" CssClass="form-control form-control-sm"
                            Placeholder="Nombre o código..."></asp:TextBox>
                    </div>
                    <div class="col-md-2">
                        <label>Estado</label>
                        <asp:DropDownList ID="ddlFiltrEstado" runat="server" CssClass="form-control form-control-sm">
                            <asp:ListItem Value="">-- Todos --</asp:ListItem>
                            <asp:ListItem Value="1">Activo</asp:ListItem>
                            <asp:ListItem Value="0">Inactivo</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-5 mt-1">
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
                <asp:GridView ID="gvPaquetes" runat="server" AutoGenerateColumns="False"
                    CssClass="table table-bordered table-striped custom-grid"
                    AllowPaging="True" AllowCustomPaging="True" PageSize="15"
                    OnPageIndexChanging="gvPaquetes_PageIndexChanging"
                    DataKeyNames="PaqueteID"
                    PagerStyle-CssClass="pager-custom"
                    PagerSettings-Mode="NumericFirstLast"
                    PagerSettings-FirstPageText="«"
                    PagerSettings-LastPageText="»"
                    PagerSettings-PageButtonCount="5">
                    <Columns>
                        <asp:BoundField DataField="PaqueteID"   HeaderText="ID"          Visible="false" />
                        <asp:BoundField DataField="Codigo"      HeaderText="Código" />
                        <asp:BoundField DataField="Nombre"      HeaderText="Nombre" />
                        <asp:BoundField DataField="Descripcion" HeaderText="Descripción" />
                        <asp:TemplateField HeaderText="Componentes">
                            <ItemTemplate>
                                <button type="button" class="btn btn-info btn-sm"
                                    onclick="verComponentes('<%# Eval("PaqueteID") %>',
                                        '<%# Server.HtmlEncode((Eval("Nombre") ?? "").ToString()) %>')">
                                    <i class="fas fa-list-ul"></i> Ver
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
                                        '<%# Eval("PaqueteID") %>',
                                        '<%# Eval("Codigo") %>',
                                        '<%# Server.HtmlEncode((Eval("Nombre")      ?? "").ToString()) %>',
                                        '<%# Server.HtmlEncode((Eval("Descripcion") ?? "").ToString()) %>',
                                        '<%# RowVersionBase64(Eval("RowVersion")) %>'
                                    )">
                                    <i class="fas fa-edit"></i> Editar
                                </button>
                                <asp:Button ID="btnToggle" runat="server"
                                    CssClass='<%# Convert.ToBoolean(Eval("Activo")) ? "btn btn-warning btn-sm" : "btn btn-success btn-sm" %>'
                                    Text='<%# Convert.ToBoolean(Eval("Activo")) ? "Desactivar" : "Activar" %>'
                                    CommandArgument='<%# Eval("PaqueteID") %>'
                                    OnClientClick='<%# "return confirmarToggle(\"" + Eval("PaqueteID") + "\", \"" + Server.HtmlEncode((Eval("Nombre") ?? "").ToString()) + "\", " + Eval("Activo").ToString().ToLower() + ");" %>'
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
<asp:HiddenField ID="hdnTogglePaqueteID"  runat="server" Value="" />
<asp:HiddenField ID="hdnMensajePendiente" runat="server" Value="" />
<asp:Button      ID="btnToggleHidden"     runat="server" CssClass="d-none" OnClick="btnToggleHidden_Click" />
<asp:Button      ID="btnGuardarComponentes" runat="server" CssClass="d-none" OnClick="btnGuardarComponentes_Click" />

<!-- ══ MODAL NUEVO PAQUETE ══════════════════════════ -->
<div class="modal fade" id="modalNuevo" tabindex="-1" role="dialog" data-backdrop="static">
  <div class="modal-dialog modal-xl" role="document">
    <div class="modal-content">
      <div class="modal-header" style="background-color:#003366;color:white;">
        <h5 class="modal-title"><i class="fas fa-layer-group"></i> Nuevo Paquete</h5>
        <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
      </div>
      <div class="modal-body">
        <div class="row">
          <div class="col-md-3">
            <div class="form-group">
              <label>Código <span style="color:red">*</span></label>
              <asp:TextBox ID="txtCodigo" runat="server" CssClass="form-control"
                  Placeholder="Ej: PAQ-001" MaxLength="20"></asp:TextBox>
              <small class="text-muted">Se guardará en mayúsculas.</small>
            </div>
          </div>
          <div class="col-md-9">
            <div class="form-group">
              <label>Nombre <span style="color:red">*</span></label>
              <asp:TextBox ID="txtNombre" runat="server" CssClass="form-control"
                  Placeholder="Nombre completo del paquete" MaxLength="200"></asp:TextBox>
            </div>
          </div>
        </div>
        <div class="row">
          <div class="col-md-12">
            <div class="form-group">
              <label>Descripción</label>
              <asp:TextBox ID="txtDescripcion" runat="server" CssClass="form-control"
                  Placeholder="Descripción opcional del paquete" MaxLength="500"></asp:TextBox>
            </div>
          </div>
        </div>

        <hr />
        <div class="d-flex justify-content-between align-items-center mb-2">
          <h6 style="color:#003366;font-weight:600;margin:0;">
            <i class="fas fa-puzzle-piece"></i> Componentes del paquete
            <small class="text-muted font-weight-normal"> (productos y/o materiales — opcional)</small>
          </h6>
          <button type="button" class="btn btn-outline-primary btn-sm" onclick="agregarComponenteNuevo()">
            <i class="fas fa-plus"></i> Agregar componente
          </button>
        </div>
        <div id="divComponentesNuevo"></div>
        <div id="divSinComponentes" class="text-muted text-center py-2" style="font-size:.88rem;">
          <i class="fas fa-info-circle"></i> Sin componentes agregados. Puedes agregar productos y/o materiales.
        </div>

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

<!-- ══ MODAL EDITAR PAQUETE ════════════════════════ -->
<div class="modal fade" id="modalEditar" tabindex="-1" role="dialog" data-backdrop="static">
  <div class="modal-dialog modal-lg" role="document">
    <div class="modal-content">
      <div class="modal-header" style="background-color:#003366;color:white;">
        <h5 class="modal-title"><i class="fas fa-edit"></i> Editar Paquete</h5>
        <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
      </div>
      <div class="modal-body">
        <asp:HiddenField ID="hdnPaqueteID"  runat="server" />
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
              <label>Nombre <span style="color:red">*</span></label>
              <asp:TextBox ID="txtNombreEdit" runat="server" CssClass="form-control" MaxLength="200"></asp:TextBox>
            </div>
          </div>
        </div>
        <div class="row">
          <div class="col-md-12">
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
          Componentes de: <span id="spanNombrePaquete"></span>
        </h5>
        <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
      </div>
      <div class="modal-body">

        <!-- Tabla de componentes existentes -->
        <table class="table table-bordered comp-view-table" id="tblComponentes">
          <thead>
            <tr>
              <th>Tipo</th>
              <th>Descripción / Nombre</th>
              <th>Cantidad</th>
              <th>Precio Unit.</th>
              <th style="width:80px;">Acción</th>
            </tr>
          </thead>
          <tbody id="tbodyComponentes"></tbody>
        </table>
        <div id="divSinCompModal" class="text-muted text-center py-3" style="display:none; font-size:.88rem;">
          <i class="fas fa-info-circle"></i> Este paquete no tiene componentes registrados.
        </div>

        <hr />
        <h6 style="color:#003366;font-weight:600;"><i class="fas fa-plus-circle"></i> Agregar componente</h6>
        <div class="row align-items-end">
          <div class="col-md-2">
            <label style="font-size:.84rem;">Tipo <span style="color:red">*</span></label>
            <select id="ddlTipoComp" class="form-control form-control-sm" onchange="actualizarSelectComp()">
              <option value="">-- Tipo --</option>
              <option value="PRODUCTO">Producto</option>
              <option value="MATERIAL">Material</option>
            </select>
          </div>
          <div class="col-md-4">
            <label style="font-size:.84rem;">Item <span style="color:red">*</span></label>
            <select id="ddlItemComp" class="form-control form-control-sm">
              <option value="">-- Seleccione tipo primero --</option>
            </select>
          </div>
          <div class="col-md-2">
            <label style="font-size:.84rem;">Cantidad <span style="color:red">*</span></label>
            <input type="number" id="txtCantidadComp" class="form-control form-control-sm" value="1" min="0" step="any" />
          </div>
          <div class="col-md-2">
            <label style="font-size:.84rem;">Precio Unit.</label>
            <input type="number" id="txtPrecioComp" class="form-control form-control-sm" value="0" min="0" step="0.01" />
          </div>
          <div class="col-md-2 mt-1">
            <button type="button" class="btn btn-success btn-sm w-100" onclick="agregarComponenteModal()">
              <i class="fas fa-plus"></i> Agregar
            </button>
          </div>
        </div>

        <!-- Hiddens para comunicar con servidor -->
        <asp:HiddenField ID="hdnCompPaqueteID"  runat="server" Value="" />
        <asp:HiddenField ID="hdnCompAccion"     runat="server" Value="" />
        <asp:HiddenField ID="hdnCompTipo"       runat="server" Value="" />
        <asp:HiddenField ID="hdnCompItemID"     runat="server" Value="" />
        <asp:HiddenField ID="hdnCompCantidad"   runat="server" Value="" />
        <asp:HiddenField ID="hdnCompPrecio"     runat="server" Value="" />
        <asp:HiddenField ID="hdnCompPCID"       runat="server" Value="" />

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

            // Si hubo operación de componente, reabrir el modal de componentes
            if (msg.reopenComp) {
                var pid = document.getElementById('<%= hdnCompPaqueteID.ClientID %>').value;
                var pnom = document.getElementById('spanNombrePaquete').innerText;
                if (pid) setTimeout(function () { cargarComponentesModal(pid, pnom); }, 300);
            }
        } catch (e) { }
    });

    // ════════════════════════════════════════════════════
    // DATOS INYECTADOS DESDE SERVIDOR
    // ════════════════════════════════════════════════════
    var _productos  = window._productosData  || [];
    var _materiales = window._materialesData || [];

    // ════════════════════════════════════════════════════
    // MODAL NUEVO — componentes dinámicos
    // ════════════════════════════════════════════════════
    var _compNuevo = [];

    function abrirModalNuevo() {
        _compNuevo = [];
        renderComponentesNuevo();
        $('#modalNuevo').modal('show');
    }

    function agregarComponenteNuevo() {
        _compNuevo.push({ tipo: '', itemID: '', cantidad: 1, precio: 0 });
        renderComponentesNuevo();
    }

    function renderComponentesNuevo() {
        var div    = document.getElementById('divComponentesNuevo');
        var sinDiv = document.getElementById('divSinComponentes');
        div.innerHTML = '';

        if (_compNuevo.length === 0) { sinDiv.style.display = 'block'; return; }
        sinDiv.style.display = 'none';

        _compNuevo.forEach(function (c, i) {
            // Opciones del select de items según tipo actual
            var optsItem = '<option value="">-- Seleccione --</option>';
            if (c.tipo === 'PRODUCTO') {
                _productos.forEach(function (p) {
                    var sel = (p.id == c.itemID) ? 'selected' : '';
                    optsItem += '<option value="' + p.id + '" ' + sel + '>' + escHtml(p.nombre) + '</option>';
                });
            } else if (c.tipo === 'MATERIAL') {
                _materiales.forEach(function (m) {
                    var sel = (m.id == c.itemID) ? 'selected' : '';
                    optsItem += '<option value="' + m.id + '" ' + sel + '>' + escHtml(m.nombre) + ' (' + escHtml(m.unidad) + ')</option>';
                });
            }

            var row = document.createElement('div');
            row.className = 'comp-row row align-items-center';
            row.innerHTML =
                '<div class="col-md-2">' +
                '<label style="font-size:.8rem;">Tipo <span style="color:red">*</span></label>' +
                '<select class="form-control form-control-sm" onchange="_compNuevo[' + i + '].tipo=this.value; _compNuevo[' + i + '].itemID=\'\'; renderComponentesNuevo();">' +
                '<option value=""' + (c.tipo===''?' selected':'') + '>-- Tipo --</option>' +
                '<option value="PRODUCTO"' + (c.tipo==='PRODUCTO'?' selected':'') + '>Producto</option>' +
                '<option value="MATERIAL"' + (c.tipo==='MATERIAL'?' selected':'') + '>Material</option>' +
                '</select>' +
                '</div>' +
                '<div class="col-md-4">' +
                '<label style="font-size:.8rem;">Item <span style="color:red">*</span></label>' +
                '<select class="form-control form-control-sm" onchange="_compNuevo[' + i + '].itemID=this.value;">' +
                optsItem +
                '</select>' +
                '</div>' +
                '<div class="col-md-2">' +
                '<label style="font-size:.8rem;">Cantidad <span style="color:red">*</span></label>' +
                '<input type="number" class="form-control form-control-sm" value="' + c.cantidad + '" min="0" step="any" ' +
                'onchange="_compNuevo[' + i + '].cantidad=parseFloat(this.value)||0;" />' +
                '</div>' +
                '<div class="col-md-2">' +
                '<label style="font-size:.8rem;">Precio Unit.</label>' +
                '<input type="number" class="form-control form-control-sm" value="' + c.precio + '" min="0" step="0.01" ' +
                'onchange="_compNuevo[' + i + '].precio=parseFloat(this.value)||0;" />' +
                '</div>' +
                '<div class="col-md-2 text-center" style="padding-top:20px;">' +
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
    // MODAL EDITAR PAQUETE
    // ════════════════════════════════════════════════════
    function abrirModalEditar(id, codigo, nombre, descripcion, rowVersion) {
        document.getElementById('<%= hdnPaqueteID.ClientID %>').value       = id;
        document.getElementById('<%= hdnRowVersion.ClientID %>').value      = rowVersion;
        document.getElementById('<%= txtCodigoEdit.ClientID %>').value      = codigo;
        document.getElementById('<%= txtNombreEdit.ClientID %>').value      = nombre;
        document.getElementById('<%= txtDescripcionEdit.ClientID %>').value = descripcion;
        $('#modalEditar').modal('show');
    }

    // ════════════════════════════════════════════════════
    // MODAL COMPONENTES — Ver y editar
    // ════════════════════════════════════════════════════
    var _compModal = [];

    function verComponentes(paqueteID, nombre) {
        document.getElementById('<%= hdnCompPaqueteID.ClientID %>').value = paqueteID;
        document.getElementById('spanNombrePaquete').innerText = nombre;
        cargarComponentesModal(paqueteID, nombre);
    }

    function cargarComponentesModal(paqueteID, nombre) {
        var data = window._componentesData || {};
        _compModal = data[paqueteID] || [];

        // Limpiar form de agregar
        document.getElementById('ddlTipoComp').value     = '';
        document.getElementById('ddlItemComp').innerHTML = '<option value="">-- Seleccione tipo primero --</option>';
        document.getElementById('txtCantidadComp').value = '1';
        document.getElementById('txtPrecioComp').value   = '0';

        renderTablaComp();
        document.getElementById('spanNombrePaquete').innerText = nombre || '';
        $('#modalComponentes').modal('show');
    }

    function actualizarSelectComp() {
        var tipo = document.getElementById('ddlTipoComp').value;
        var sel  = document.getElementById('ddlItemComp');
        sel.innerHTML = '<option value="">-- Seleccione --</option>';

        if (tipo === 'PRODUCTO') {
            _productos.forEach(function (p) {
                sel.innerHTML += '<option value="' + p.id + '">' + escHtml(p.nombre) + '</option>';
            });
        } else if (tipo === 'MATERIAL') {
            _materiales.forEach(function (m) {
                sel.innerHTML += '<option value="' + m.id + '">' + escHtml(m.nombre) + ' (' + escHtml(m.unidad) + ')</option>';
            });
        }
    }

    function renderTablaComp() {
        var tbody  = document.getElementById('tbodyComponentes');
        var sinDiv = document.getElementById('divSinCompModal');
        tbody.innerHTML = '';

        if (_compModal.length === 0) { sinDiv.style.display = 'block'; return; }
        sinDiv.style.display = 'none';

        _compModal.forEach(function (c) {
            var badgeCss = c.tipo === 'PRODUCTO' ? 'badge-producto' : 'badge-material';
            var tipoLbl  = c.tipo === 'PRODUCTO' ? 'Producto' : 'Material';
            var tr = document.createElement('tr');
            tr.innerHTML =
                '<td><span class="badge ' + badgeCss + '">' + tipoLbl + '</span></td>' +
                '<td><strong>' + escHtml(c.nombre) + '</strong>' +
                (c.unidad ? '<br><small class="text-muted">' + escHtml(c.unidad) + '</small>' : '') +
                '</td>' +
                '<td><input type="number" class="form-control form-control-sm" value="' + c.cantidad + '" min="0.0001" step="0.01" id="ccant_' + c.pcID + '" style="width:90px;" /></td>' +
                '<td><input type="number" class="form-control form-control-sm" value="' + c.precio + '" min="0" step="0.01" id="cpre_' + c.pcID + '" style="width:90px;" /></td>' +
                '<td class="text-center">' +
                '<button type="button" class="btn btn-success btn-xs btn-sm mr-1" onclick="guardarCompExistente(' + c.pcID + ')" title="Guardar"><i class="fas fa-save"></i></button>' +
                '<button type="button" class="btn btn-danger  btn-xs btn-sm" onclick="eliminarComp(' + c.pcID + ')" title="Eliminar"><i class="fas fa-trash"></i></button>' +
                '</td>';
            tbody.appendChild(tr);
        });
    }

    function guardarCompExistente(pcID) {
        var cantidad = parseFloat(document.getElementById('ccant_' + pcID).value) || 0;
        var precio   = parseFloat(document.getElementById('cpre_'  + pcID).value) || 0;

        if (cantidad <= 0) {
            Swal.fire({ icon: 'warning', title: 'Cantidad inválida',
                text: 'La cantidad debe ser mayor a 0.', confirmButtonColor: '#003366' })
                .then(function () { $('#modalComponentes').modal('show'); });
            return;
        }

        document.getElementById('<%= hdnCompAccion.ClientID %>').value   = 'UPDATE';
        document.getElementById('<%= hdnCompPCID.ClientID %>').value     = pcID;
        document.getElementById('<%= hdnCompCantidad.ClientID %>').value = cantidad;
        document.getElementById('<%= hdnCompPrecio.ClientID %>').value   = precio;
        __doPostBack('<%= btnGuardarComponentes.UniqueID %>', '');
    }

    function eliminarComp(pcID) {
        Swal.fire({
            icon: 'warning', title: '¿Eliminar componente?',
            text: 'Se eliminará este componente del paquete.',
            showCancelButton: true, confirmButtonText: 'Sí, eliminar',
            confirmButtonColor: '#e74c3c', cancelButtonColor: '#6c757d'
        }).then(function (r) {
            if (r.isConfirmed) {
                document.getElementById('<%= hdnCompAccion.ClientID %>').value = 'DELETE';
                document.getElementById('<%= hdnCompPCID.ClientID %>').value   = pcID;
                __doPostBack('<%= btnGuardarComponentes.UniqueID %>', '');
            }
        });
    }

    function agregarComponenteModal() {
        var tipo     = document.getElementById('ddlTipoComp').value;
        var itemID   = document.getElementById('ddlItemComp').value;
        var cantidad = parseFloat(document.getElementById('txtCantidadComp').value) || 0;
        var precio   = parseFloat(document.getElementById('txtPrecioComp').value)   || 0;
        var paqID    = document.getElementById('<%= hdnCompPaqueteID.ClientID %>').value;

        if (!tipo) {
            Swal.fire({ icon: 'warning', title: 'Tipo requerido',
                text: 'Seleccione si es Producto o Material.', confirmButtonColor: '#003366' })
                .then(function () { $('#modalComponentes').modal('show'); });
            return;
        }
        if (!itemID) {
            Swal.fire({ icon: 'warning', title: 'Item requerido',
                text: 'Seleccione el producto o material.', confirmButtonColor: '#003366' })
                .then(function () { $('#modalComponentes').modal('show'); });
            return;
        }
        if (cantidad <= 0) {
            Swal.fire({ icon: 'warning', title: 'Cantidad inválida',
                text: 'La cantidad debe ser mayor a 0.', confirmButtonColor: '#003366' })
                .then(function () { $('#modalComponentes').modal('show'); });
            return;
        }
        // Verificar duplicado
        var existe = _compModal.find(function (c) {
            return c.tipo === tipo && c.itemID == itemID;
        });
        if (existe) {
            Swal.fire({ icon: 'warning', title: 'Duplicado',
                text: 'Ese item ya está como componente de este paquete.', confirmButtonColor: '#003366' })
                .then(function () { $('#modalComponentes').modal('show'); });
            return;
        }

        document.getElementById('<%= hdnCompAccion.ClientID %>').value   = 'INSERT';
        document.getElementById('<%= hdnCompTipo.ClientID %>').value     = tipo;
        document.getElementById('<%= hdnCompItemID.ClientID %>').value   = itemID;
        document.getElementById('<%= hdnCompCantidad.ClientID %>').value = cantidad;
        document.getElementById('<%= hdnCompPrecio.ClientID %>').value   = precio;
        __doPostBack('<%= btnGuardarComponentes.UniqueID %>', '');
    }

    // ════════════════════════════════════════════════════
    // TOGGLE
    // ════════════════════════════════════════════════════
    function confirmarToggle(paqID, nombre, activo) {
        var accion = activo ? 'desactivar' : 'activar';
        Swal.fire({
            icon: activo ? 'warning' : 'question',
            title: '¿' + (activo ? 'Desactivar' : 'Activar') + ' paquete?',
            html: '¿Seguro de <b>' + accion + '</b> el paquete <b>' + nombre + '</b>?',
            showCancelButton: true, confirmButtonText: 'Sí, ' + accion,
            confirmButtonColor: activo ? '#e0a800' : '#28a745',
            cancelButtonColor: '#6c757d'
        }).then(function (r) {
            if (r.isConfirmed) {
                document.getElementById('<%= hdnTogglePaqueteID.ClientID %>').value = paqID;
                __doPostBack('<%= btnToggleHidden.UniqueID %>', '');
            }
        });
        return false;
    }

    // ════════════════════════════════════════════════════
    // VALIDACIONES
    // ════════════════════════════════════════════════════
    function prepararYValidarNuevo() {
        if (!_validarPaq(
            '<%= txtCodigo.ClientID %>',
            '<%= txtNombre.ClientID %>',
            'modalNuevo')) return false;

        for (var i = 0; i < _compNuevo.length; i++) {
            var c = _compNuevo[i];
            if (!c.tipo) {
                Swal.fire({ icon: 'warning', title: 'Componente incompleto',
                    text: 'El componente #' + (i + 1) + ' no tiene tipo seleccionado.',
                    confirmButtonColor: '#003366' }).then(function () { $('#modalNuevo').modal('show'); });
                return false;
            }
            if (!c.itemID) {
                Swal.fire({ icon: 'warning', title: 'Componente incompleto',
                    text: 'El componente #' + (i + 1) + ' no tiene item seleccionado.',
                    confirmButtonColor: '#003366' }).then(function () { $('#modalNuevo').modal('show'); });
                return false;
            }
            if (c.cantidad <= 0) {
                Swal.fire({ icon: 'warning', title: 'Cantidad inválida',
                    text: 'El componente #' + (i + 1) + ' debe tener cantidad mayor a 0.',
                    confirmButtonColor: '#003366' }).then(function () { $('#modalNuevo').modal('show'); });
                return false;
            }
            // Verificar duplicados dentro de la misma lista
            for (var j = i + 1; j < _compNuevo.length; j++) {
                if (_compNuevo[j].tipo === c.tipo && _compNuevo[j].itemID === c.itemID) {
                    Swal.fire({ icon: 'warning', title: 'Componente duplicado',
                        text: 'El mismo item aparece más de una vez.',
                        confirmButtonColor: '#003366' }).then(function () { $('#modalNuevo').modal('show'); });
                    return false;
                }
            }
        }

        document.getElementById('<%= hdnComponentesNuevo.ClientID %>').value = JSON.stringify(_compNuevo);
        return true;
    }

    function validarEditar() {
        return _validarPaq(
            '<%= txtCodigoEdit.ClientID %>',
            '<%= txtNombreEdit.ClientID %>',
            'modalEditar');
    }

    function _validarPaq(idCod, idNom, modal) {
        var cod = document.getElementById(idCod).value.trim();
        var nom = document.getElementById(idNom).value.trim();

        function warn(txt) {
            Swal.fire({ icon: 'warning', title: 'Campo inválido', text: txt, confirmButtonColor: '#003366' })
                .then(function () { $('#' + modal).modal('show'); });
            return false;
        }
        if (!cod || cod.length < 2) return warn('El código es obligatorio (mínimo 2 caracteres).');
        if (!nom || nom.length < 3) return warn('El nombre es obligatorio (mínimo 3 caracteres).');
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
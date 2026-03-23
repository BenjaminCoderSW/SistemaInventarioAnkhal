<%@ Page Title="Produccion" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Produccion.aspx.cs" Inherits="GrupoAnkhalInventario.Produccion" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="css/gridviewPantalla.css" rel="stylesheet" />
    <style>
        /* -- Dashboard de produccion -- */
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
            transition: transform .15s, box-shadow .15s;
        }
        .stock-card:hover { transform: translateY(-3px); box-shadow: 0 6px 16px rgba(0,0,0,0.2); }
        .stock-card.registros  { background: linear-gradient(135deg,#1a5276,#2980b9); }
        .stock-card.buenas     { background: linear-gradient(135deg,#1e8449,#27ae60); }
        .stock-card.rechazo    { background: linear-gradient(135deg,#922b21,#e74c3c); }
        .stock-card .icon      { font-size: 2.2rem; opacity: .9; }
        .stock-card .info .num { font-size: 2rem; font-weight: 700; line-height:1; }
        .stock-card .info .lbl { font-size: .78rem; opacity: .9; text-transform: uppercase; letter-spacing:.5px; }

        /* -- Filtros -- */
        .filtros-bar {
            background:#f8f9fa; border:1px solid #dee2e6;
            border-radius:8px; padding:14px 18px; margin-bottom:14px;
        }
        .filtros-bar label { font-weight:600; font-size:.84rem; color:#003366; margin-bottom:2px; }

        /* -- Paginador -- */
        .pager-custom span {
            background:#003366; color:#fff; font-weight:700;
            border-radius:4px; padding:4px 9px;
        }
        .pager-custom a { padding:4px 9px; border-radius:4px; }

        /* -- Tabla BOM dinamica -- */
        #tblBOM { width:100%; }
        #tblBOM th { background:#003366; color:#fff; padding:6px 10px; font-size:.85rem; }
        #tblBOM td { padding:6px 10px; font-size:.85rem; border-bottom:1px solid #dee2e6; }
        #tblBOM input[type="number"] { width:120px; }
        #divBOM { display:none; margin-top:10px; }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
<div class="container-fluid">
<div class="row">
<div class="col-12">

    <!-- == DASHBOARD == -->
    <div class="stock-dashboard">
        <div class="stock-card registros">
            <div class="icon"><i class="fas fa-clipboard-list"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblRegistrosHoy" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Registros Hoy</div>
            </div>
        </div>
        <div class="stock-card buenas">
            <div class="icon"><i class="fas fa-check-circle"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblUnidadesBuenas" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Unidades Buenas</div>
            </div>
        </div>
        <div class="stock-card rechazo">
            <div class="icon"><i class="fas fa-times-circle"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblUnidadesRechazo" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Unidades Rechazo</div>
            </div>
        </div>
    </div>

    <div class="card">
        <div class="card-header" style="background-color:#003366;color:white;">
            <h3 class="card-title"><i class="fas fa-industry"></i> Registro de Produccion</h3>
        </div>
        <div class="card-body">

            <div class="mb-3">
                <asp:Button ID="btnNuevo" runat="server" Text="+ Nuevo Registro"
                    CssClass="btn btn-success"
                    OnClientClick="abrirModalNuevo(); return false;" />
            </div>

            <!-- -- FILTROS -- -->
            <div class="filtros-bar">
                <div class="row align-items-end">
                    <div class="col-md-2">
                        <label>Base</label>
                        <asp:DropDownList ID="ddlFiltrBase" runat="server" CssClass="form-control form-control-sm">
                            <asp:ListItem Value="">-- Todas --</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-2">
                        <label>Producto</label>
                        <asp:DropDownList ID="ddlFiltrProducto" runat="server" CssClass="form-control form-control-sm">
                            <asp:ListItem Value="">-- Todos --</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-2">
                        <label>Turno</label>
                        <asp:DropDownList ID="ddlFiltrTurno" runat="server" CssClass="form-control form-control-sm">
                            <asp:ListItem Value="">-- Todos --</asp:ListItem>
                            <asp:ListItem Value="MANANA">Manana</asp:ListItem>
                            <asp:ListItem Value="TARDE">Tarde</asp:ListItem>
                            <asp:ListItem Value="NOCHE">Noche</asp:ListItem>
                            <asp:ListItem Value="UNICO">Unico</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-2">
                        <label>Fecha desde</label>
                        <asp:TextBox ID="txtFechaDesde" runat="server" CssClass="form-control form-control-sm" TextMode="Date"></asp:TextBox>
                    </div>
                    <div class="col-md-2">
                        <label>Fecha hasta</label>
                        <asp:TextBox ID="txtFechaHasta" runat="server" CssClass="form-control form-control-sm" TextMode="Date"></asp:TextBox>
                    </div>
                    <div class="col-md-2 mt-1">
                        <asp:Button ID="btnBuscar" runat="server" Text="Buscar"
                            CssClass="btn btn-primary btn-sm mr-1" OnClick="btnBuscar_Click" />
                        <asp:Button ID="btnLimpiar" runat="server" Text="Limpiar"
                            CssClass="btn btn-secondary btn-sm" OnClick="btnLimpiar_Click" />
                    </div>
                </div>
            </div>

            <div class="mb-2">
                <small class="text-muted">
                    <asp:Label ID="lblResultados" runat="server"></asp:Label>
                </small>
            </div>

            <!-- -- GRID -- -->
            <div class="table-responsive">
                <asp:GridView ID="gvProduccion" runat="server" AutoGenerateColumns="False"
                    CssClass="table table-bordered table-striped custom-grid"
                    AllowPaging="True" AllowCustomPaging="True" PageSize="15"
                    OnPageIndexChanging="gvProduccion_PageIndexChanging"
                    DataKeyNames="ProduccionID"
                    PagerStyle-CssClass="pager-custom"
                    PagerSettings-Mode="NumericFirstLast"
                    PagerSettings-FirstPageText="&laquo;"
                    PagerSettings-LastPageText="&raquo;"
                    PagerSettings-PageButtonCount="5">
                    <Columns>
                        <asp:BoundField DataField="ProduccionID" HeaderText="ID" Visible="false" />
                        <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy}" />
                        <asp:BoundField DataField="BaseNombre" HeaderText="Base" />
                        <asp:BoundField DataField="ProductoNombre" HeaderText="Producto" />
                        <asp:BoundField DataField="Turno" HeaderText="Turno" />
                        <asp:BoundField DataField="CantidadBuena" HeaderText="Buenas" />
                        <asp:BoundField DataField="CantidadRechazo" HeaderText="Rechazo" />
                        <asp:BoundField DataField="Total" HeaderText="Total" />
                        <asp:BoundField DataField="MetaDia" HeaderText="Meta" />
                        <asp:BoundField DataField="Observaciones" HeaderText="Observaciones" />
                        <asp:BoundField DataField="RegistradoPor" HeaderText="Registrado Por" />
                    </Columns>
                </asp:GridView>
            </div>

        </div><!-- /card-body -->
    </div><!-- /card -->
</div>
</div>
</div>

<!-- -- HIDDEN FIELDS -- -->
<asp:HiddenField ID="hdnMensajePendiente" runat="server" Value="" />
<asp:HiddenField ID="hdnConsumosJson" runat="server" Value="" />

<!-- == MODAL NUEVO REGISTRO == -->
<div class="modal fade" id="modalNuevo" tabindex="-1" role="dialog" data-backdrop="static">
  <div class="modal-dialog modal-lg" role="document">
    <div class="modal-content">
      <div class="modal-header" style="background-color:#003366;color:white;">
        <h5 class="modal-title"><i class="fas fa-industry"></i> Nuevo Registro de Produccion</h5>
        <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
      </div>
      <div class="modal-body">
        <!-- Fila 1: Base, Fecha, Turno -->
        <div class="row">
          <div class="col-md-4">
            <div class="form-group">
              <label>Base <span style="color:red">*</span></label>
              <asp:DropDownList ID="ddlBase" runat="server" CssClass="form-control">
                <asp:ListItem Value="">-- Seleccione --</asp:ListItem>
              </asp:DropDownList>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Fecha <span style="color:red">*</span></label>
              <asp:TextBox ID="txtFecha" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Turno <span style="color:red">*</span></label>
              <asp:DropDownList ID="ddlTurno" runat="server" CssClass="form-control">
                <asp:ListItem Value="">-- Seleccione --</asp:ListItem>
                <asp:ListItem Value="MANANA">Manana</asp:ListItem>
                <asp:ListItem Value="TARDE">Tarde</asp:ListItem>
                <asp:ListItem Value="NOCHE">Noche</asp:ListItem>
                <asp:ListItem Value="UNICO">Unico</asp:ListItem>
              </asp:DropDownList>
            </div>
          </div>
        </div>

        <!-- Fila 2: Producto -->
        <div class="row">
          <div class="col-md-12">
            <div class="form-group">
              <label>Producto <span style="color:red">*</span></label>
              <asp:DropDownList ID="ddlProducto" runat="server" CssClass="form-control"
                  onchange="onProductoChange();">
                <asp:ListItem Value="">-- Seleccione un producto --</asp:ListItem>
              </asp:DropDownList>
            </div>
          </div>
        </div>

        <!-- Fila 3: Cantidad Buena, Cantidad Rechazo -->
        <div class="row">
          <div class="col-md-6">
            <div class="form-group">
              <label>Cantidad Buena <span style="color:red">*</span></label>
              <asp:TextBox ID="txtCantidadBuena" runat="server" CssClass="form-control" TextMode="Number"
                  Placeholder="0" min="0" step="1"
                  onkeyup="onCantidadBuenaChange();" onchange="onCantidadBuenaChange();"></asp:TextBox>
            </div>
          </div>
          <div class="col-md-6">
            <div class="form-group">
              <label>Cantidad Rechazo</label>
              <asp:TextBox ID="txtCantidadRechazo" runat="server" CssClass="form-control" TextMode="Number"
                  Placeholder="0" min="0" step="1"></asp:TextBox>
            </div>
          </div>
        </div>

        <!-- Fila 4: Tabla BOM -->
        <div id="divBOM">
          <h6 style="color:#003366; font-weight:700; margin-bottom:8px;">
            <i class="fas fa-cogs"></i> Consumo de Materiales (BOM)
          </h6>
          <div class="table-responsive">
            <table id="tblBOM" class="table table-sm">
              <thead>
                <tr>
                  <th>Material</th>
                  <th>Unidad</th>
                  <th>Min/Unidad</th>
                  <th>Max/Unidad</th>
                  <th>Cantidad Real <span style="color:#ffd700">*</span></th>
                </tr>
              </thead>
              <tbody id="tbodyBOM"></tbody>
            </table>
          </div>
        </div>

        <!-- Fila 5: Observaciones -->
        <div class="row">
          <div class="col-md-12">
            <div class="form-group">
              <label>Observaciones</label>
              <asp:TextBox ID="txtObservaciones" runat="server" CssClass="form-control" TextMode="MultiLine"
                  Rows="3" Placeholder="Observaciones opcionales..." MaxLength="500"></asp:TextBox>
            </div>
          </div>
        </div>
      </div>
      <div class="modal-footer">
        <asp:Button ID="btnGuardar" runat="server" Text="Guardar Registro"
            CssClass="btn btn-success"
            OnClientClick="return prepararGuardar();"
            OnClick="btnGuardar_Click" />
        <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancelar</button>
      </div>
    </div>
  </div>
</div>

<asp:Literal ID="litJsData" runat="server"></asp:Literal>

<script>
    // -- Mensaje pendiente (SweetAlert) --
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

    // -- Abrir modal nuevo --
    function abrirModalNuevo() {
        document.getElementById('<%= ddlBase.ClientID %>').value = '';
        document.getElementById('<%= txtFecha.ClientID %>').value = new Date().toISOString().split('T')[0];
        document.getElementById('<%= ddlTurno.ClientID %>').value = '';
        document.getElementById('<%= ddlProducto.ClientID %>').value = '';
        document.getElementById('<%= txtCantidadBuena.ClientID %>').value = '';
        document.getElementById('<%= txtCantidadRechazo.ClientID %>').value = '';
        document.getElementById('<%= txtObservaciones.ClientID %>').value = '';
        document.getElementById('<%= hdnConsumosJson.ClientID %>').value = '';
        document.getElementById('divBOM').style.display = 'none';
        document.getElementById('tbodyBOM').innerHTML = '';
        $('#modalNuevo').modal('show');
    }

    // -- Al seleccionar producto: renderizar tabla BOM --
    function onProductoChange() {
        var prodID = document.getElementById('<%= ddlProducto.ClientID %>').value;
        var tbody = document.getElementById('tbodyBOM');
        var divBOM = document.getElementById('divBOM');
        tbody.innerHTML = '';

        if (!prodID || !window._bomData || !window._bomData[prodID]) {
            divBOM.style.display = 'none';
            return;
        }

        var bom = window._bomData[prodID];
        var cantBuena = parseInt(document.getElementById('<%= txtCantidadBuena.ClientID %>').value) || 0;

        bom.forEach(function (item) {
            var tr = document.createElement('tr');
            var cantReal = cantBuena > 0 ? (cantBuena * item.cantidadMin).toFixed(4) : '';
            tr.innerHTML =
                '<td>' + item.nombre + '</td>' +
                '<td>' + (item.unidad || '') + '</td>' +
                '<td>' + item.cantidadMin.toFixed(4) + '</td>' +
                '<td>' + item.cantidadMax.toFixed(4) + '</td>' +
                '<td><input type="number" class="form-control form-control-sm bom-cant-real" ' +
                    'data-material-id="' + item.materialID + '" ' +
                    'data-cant-min="' + item.cantidadMin + '" ' +
                    'data-cant-max="' + item.cantidadMax + '" ' +
                    'value="' + cantReal + '" min="0" step="0.0001" /></td>';
            tbody.appendChild(tr);
        });

        divBOM.style.display = 'block';
    }

    // -- Al cambiar cantidad buena: recalcular cantidades reales --
    function onCantidadBuenaChange() {
        var cantBuena = parseInt(document.getElementById('<%= txtCantidadBuena.ClientID %>').value) || 0;
        var inputs = document.querySelectorAll('.bom-cant-real');
        inputs.forEach(function (inp) {
            var cantMin = parseFloat(inp.getAttribute('data-cant-min')) || 0;
            inp.value = (cantBuena * cantMin).toFixed(4);
        });
    }

    // -- Preparar guardar: recopilar consumos BOM en JSON --
    function prepararGuardar() {
        var base    = document.getElementById('<%= ddlBase.ClientID %>').value;
        var fecha   = document.getElementById('<%= txtFecha.ClientID %>').value;
        var turno   = document.getElementById('<%= ddlTurno.ClientID %>').value;
        var prodID  = document.getElementById('<%= ddlProducto.ClientID %>').value;
        var cantB   = parseInt(document.getElementById('<%= txtCantidadBuena.ClientID %>').value);

        function warn(txt) {
            Swal.fire({ icon: 'warning', title: 'Campo requerido', text: txt, confirmButtonColor: '#003366' })
                .then(function () { $('#modalNuevo').modal('show'); });
            return false;
        }

        if (!base)                return warn('Seleccione una base.');
        if (!fecha)               return warn('Seleccione una fecha.');
        if (!turno)               return warn('Seleccione un turno.');
        if (!prodID)              return warn('Seleccione un producto.');
        if (isNaN(cantB) || cantB < 0) return warn('La cantidad buena debe ser mayor o igual a cero.');

        // Recopilar consumos de la tabla BOM
        var consumos = [];
        var inputs = document.querySelectorAll('.bom-cant-real');
        var hayError = false;
        inputs.forEach(function (inp) {
            var cantReal = parseFloat(inp.value);
            if (isNaN(cantReal) || cantReal < 0) {
                hayError = true;
                return;
            }
            var cantMin = parseFloat(inp.getAttribute('data-cant-min')) || 0;
            var cantMax = parseFloat(inp.getAttribute('data-cant-max')) || 0;
            consumos.push({
                materialID: parseInt(inp.getAttribute('data-material-id')),
                cantidadReal: cantReal,
                cantidadTeoricaMin: cantB * cantMin,
                cantidadTeoricaMax: cantB * cantMax,
                esMerma: false,
                notas: ''
            });
        });

        if (hayError) return warn('Revise las cantidades reales de materiales. No pueden ser negativas.');

        document.getElementById('<%= hdnConsumosJson.ClientID %>').value = JSON.stringify(consumos);
        return true;
    }
</script>

</asp:Content>

<%@ Page Title="Mermas" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="Mermas.aspx.cs" Inherits="GrupoAnkhalInventario.Mermas"
    EnableEventValidation="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="css/gridviewPantalla.css" rel="stylesheet" />
    <style>
        .stock-dashboard { display:flex; gap:14px; margin-bottom:18px; flex-wrap:wrap; }
        .stock-card {
            flex:1; min-width:180px; border-radius:10px; padding:16px 20px;
            color:#fff; display:flex; align-items:center; gap:14px;
            box-shadow:0 3px 10px rgba(0,0,0,.15);
            transition:transform .15s, box-shadow .15s;
        }
        .stock-card:hover { transform:translateY(-3px); box-shadow:0 6px 16px rgba(0,0,0,.2); }
        .stock-card.total-merma  { background:linear-gradient(135deg,#922b21,#e74c3c); }
        .stock-card.valor-merma  { background:linear-gradient(135deg,#1c2833,#2c3e50); }
        .stock-card .icon        { font-size:2.2rem; opacity:.9; }
        .stock-card .info .num   { font-size:2rem; font-weight:700; line-height:1; }
        .stock-card .info .lbl   { font-size:.78rem; opacity:.9; text-transform:uppercase; letter-spacing:.5px; }

        .filtros-bar {
            background:#f8f9fa; border:1px solid #dee2e6;
            border-radius:8px; padding:14px 18px; margin-bottom:14px;
        }
        .filtros-bar label { font-weight:600; font-size:.84rem; color:#003366; margin-bottom:2px; }

        .pager-custom span { background:#003366; color:#fff; font-weight:700; border-radius:4px; padding:4px 9px; }
        .pager-custom a    { padding:4px 9px; border-radius:4px; }

        .seccion-header {
            background:#fdf2e9; border-left:4px solid #e67e22;
            padding:8px 12px; margin-bottom:12px; border-radius:4px;
            font-weight:600; color:#784212; font-size:.9rem;
        }
        .seccion-historial {
            background:#eaf2f8; border-left:4px solid #2980b9;
            padding:8px 12px; margin-bottom:12px; border-radius:4px;
            font-weight:600; color:#1a5276; font-size:.9rem;
        }
        #tblOutputs { font-size:.88rem; }
        #tblOutputs thead th { background:#003366; color:#fff; font-size:.8rem; }
        .btn-quitar-output { padding:2px 7px; font-size:.78rem; }
        .autocomplete-results {
            display:none; position:absolute; z-index:9999; width:100%;
            background:#fff; border:1px solid #ccc; border-radius:4px;
            max-height:200px; overflow-y:auto;
            box-shadow:0 2px 8px rgba(0,0,0,.18);
        }
        .disponible-badge {
            background:#fdf2e9; border:1px solid #e67e22; color:#784212;
            border-radius:6px; padding:5px 12px; font-weight:600; font-size:.9rem;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
<div class="container-fluid">
<div class="row">
<div class="col-12">

    <!-- ══ DASHBOARD ══════════════════════════════════════════════════ -->
    <div class="stock-dashboard">
        <div class="stock-card total-merma">
            <div class="icon"><i class="fas fa-recycle"></i></div>
            <div class="info">
                <div class="num"><asp:Label ID="lblTotalEntradas" runat="server" Text="0"></asp:Label></div>
                <div class="lbl">Materiales en merma</div>
            </div>
        </div>
        <div class="stock-card valor-merma" style="flex:0 0 auto; min-width:280px;">
            <div class="icon"><i class="fas fa-dollar-sign"></i></div>
            <div class="info">
                <div class="num" style="font-size:1.8rem;">
                    <asp:Label ID="lblValorMerma" runat="server" Text="$0.00"></asp:Label>
                </div>
                <div class="lbl">Valor estimado en merma
                    <i class="fas fa-info-circle ml-1" style="font-size:.85rem; opacity:.8; cursor:default;"
                       title="Fórmula: suma de (cantidad actual en merma × precio unitario del material) para todos los materiales con stock en merma mayor a 0. El precio unitario se configura en el catálogo de Materiales."></i>
                </div>
            </div>
        </div>
    </div>

    <!-- ══ CARD: STOCK DE MERMA ═══════════════════════════════════════ -->
    <div class="card">
        <div class="card-header" style="background-color:#922b21;color:white;">
            <h3 class="card-title"><i class="fas fa-recycle"></i> Stock de Merma por Base y Material</h3>
        </div>
        <div class="card-body">

            <div class="mb-3">
                <asp:Button ID="btnNuevaConversion" runat="server" Text="+ Registrar Conversión"
                    CssClass="btn btn-warning"
                    OnClientClick="abrirModalConversion(); return false;" />
            </div>

            <!-- ── FILTROS ── -->
            <div class="filtros-bar">
                <div class="row align-items-end">
                    <div class="col-md-3">
                        <label>Base</label>
                        <asp:DropDownList ID="ddlFiltrBase" runat="server" CssClass="form-control form-control-sm">
                            <asp:ListItem Value="">-- Todas --</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-4">
                        <label>Material (código o descripción)</label>
                        <asp:TextBox ID="txtFiltrMaterial" runat="server" CssClass="form-control form-control-sm"
                            placeholder="Buscar..."></asp:TextBox>
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
                <small class="text-muted"><asp:Label ID="lblResultados" runat="server"></asp:Label></small>
            </div>

            <!-- ── GRID STOCK MERMA ── -->
            <div class="table-responsive">
                <asp:GridView ID="gvMermas" runat="server" AutoGenerateColumns="False"
                    CssClass="table table-bordered table-striped custom-grid"
                    AllowPaging="True" AllowCustomPaging="True" PageSize="15"
                    OnPageIndexChanging="gvMermas_PageIndexChanging"
                    PagerStyle-CssClass="pager-custom"
                    PagerSettings-Mode="NumericFirstLast"
                    PagerSettings-FirstPageText="«"
                    PagerSettings-LastPageText="»"
                    PagerSettings-PageButtonCount="5"
                    EmptyDataText="No hay materiales en merma con los filtros aplicados.">
                    <Columns>
                        <asp:BoundField DataField="BaseNombre"     HeaderText="Base" />
                        <asp:TemplateField HeaderText="Código">
                            <ItemTemplate>
                                <strong style="color:#003366;"><%# Eval("Codigo") %></strong>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="Descripcion"    HeaderText="Descripción" />
                        <asp:BoundField DataField="Unidad"         HeaderText="Unidad" />
                        <asp:TemplateField HeaderText="Cant. en Merma">
                            <ItemTemplate>
                                <span style="font-weight:700; color:#922b21; font-size:1.05rem;">
                                    <%# string.Format("{0:0.####}", Eval("CantidadActual")) %>
                                </span>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>

        </div>
    </div>

    <!-- ══ CARD: HISTORIAL DE CONVERSIONES ═══════════════════════════ -->
    <div class="card mt-3">
        <div class="card-header" style="background-color:#1a5276;color:white;">
            <h3 class="card-title"><i class="fas fa-history"></i> Historial de Conversiones</h3>
        </div>
        <div class="card-body">
            <div class="table-responsive">
                <asp:GridView ID="gvHistorial" runat="server" AutoGenerateColumns="False"
                    CssClass="table table-bordered table-striped custom-grid"
                    AllowPaging="True" AllowCustomPaging="True" PageSize="10"
                    OnPageIndexChanging="gvHistorial_PageIndexChanging"
                    PagerStyle-CssClass="pager-custom"
                    PagerSettings-Mode="NumericFirstLast"
                    PagerSettings-FirstPageText="«"
                    PagerSettings-LastPageText="»"
                    PagerSettings-PageButtonCount="5"
                    EmptyDataText="Aún no hay conversiones registradas.">
                    <Columns>
                        <asp:BoundField DataField="FechaConversion"  HeaderText="Fecha"         DataFormatString="{0:dd/MM/yyyy}" />
                        <asp:BoundField DataField="BaseNombre"       HeaderText="Base" />
                        <asp:TemplateField HeaderText="Material Origen">
                            <ItemTemplate>
                                <strong style="color:#922b21;"><%# Eval("CodigoOrigen") %></strong>
                                <%# Eval("DescripcionOrigen") %>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Cant. Convertida">
                            <ItemTemplate>
                                <%# FormatCantidadHistorial(Eval("CantidadOrigen"), Eval("CantidadCapturadaOrigen"),
                                        Eval("UnidadCapturaNombre"), Eval("UnidadOrigen"), Eval("TuvoConversion")) %>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Materiales Recuperados">
                            <HeaderStyle CssClass="text-center" Width="180px" />
                            <ItemStyle CssClass="text-center" />
                            <ItemTemplate>
                                <button type="button" class="btn btn-sm btn-outline-info"
                                    onclick="verDetalle(<%# Eval("ConversionID") %>, '<%# System.Web.HttpUtility.JavaScriptStringEncode(Eval("CodigoOrigen").ToString() + " " + Eval("DescripcionOrigen").ToString()) %>')">
                                    <i class="fas fa-list-ul mr-1"></i>Ver materiales (<%# Eval("DetalleCount") %>)
                                </button>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="RegistradoPor"    HeaderText="Registrado Por" />
                        <asp:BoundField DataField="Observaciones"    HeaderText="Observaciones" />
                    </Columns>
                </asp:GridView>
            </div>
        </div>
    </div>

</div>
</div>
</div>

<!-- ── HIDDEN FIELDS ── -->
<asp:HiddenField ID="hdnMensajePendiente" runat="server" Value="" />
<asp:HiddenField ID="hdnOutputsJson"      runat="server" Value="" />
<asp:HiddenField ID="hdnOrigenUnidadVal"  runat="server" Value="" />
<asp:HiddenField ID="hdnOrigenFactor"     runat="server" Value="" />

<!-- ══ MODAL CONVERSIÓN ═════════════════════════════════════════════ -->
<div class="modal fade" id="modalConversion" tabindex="-1" role="dialog" data-backdrop="static">
  <div class="modal-dialog modal-lg" role="document">
    <div class="modal-content">
      <div class="modal-header" style="background-color:#784212;color:white;">
        <h5 class="modal-title"><i class="fas fa-recycle mr-2"></i>Registrar Conversión de Merma</h5>
        <button type="button" class="close text-white" data-dismiss="modal"><span>&times;</span></button>
      </div>
      <div class="modal-body">

        <!-- ORIGEN -->
        <div class="seccion-header"><i class="fas fa-arrow-right mr-1"></i> Material de origen (merma)</div>
        <div class="row">
          <div class="col-md-4">
            <div class="form-group">
              <label>Base <span style="color:red">*</span></label>
              <asp:DropDownList ID="ddlConvBase" runat="server" CssClass="form-control"
                  onchange="onConvBaseChange();">
                <asp:ListItem Value="">-- Seleccione --</asp:ListItem>
              </asp:DropDownList>
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Material en merma <span style="color:red">*</span></label>
              <div style="position:relative;">
                <input type="text" id="txtConvMatOrigen" class="form-control"
                       placeholder="Buscar material en merma..." autocomplete="off"
                       oninput="onTxtConvMatOrigenInput()" />
                <div id="divResultadosConvMatOrigen" class="autocomplete-results"></div>
              </div>
              <asp:HiddenField ID="hdnConvMatOrigenID" runat="server" Value="" />
            </div>
          </div>
          <div class="col-md-4">
            <div class="form-group">
              <label>Disponible en merma</label>
              <div id="divDisponible" class="disponible-badge">— seleccione material —</div>
            </div>
          </div>
        </div>
        <div class="row">
          <div class="col-md-4">
            <div class="form-group">
              <label>Cantidad a convertir <span style="color:red">*</span></label>
              <asp:TextBox ID="txtConvCantOrigen" runat="server" CssClass="form-control"
                  placeholder="0.00"
                  oninput="this.value=this.value.replace(/[^0-9.]/g,''); onOrigenCantChange();"></asp:TextBox>
            </div>
          </div>
          <div class="col-md-4" id="divOrigenUnidad" style="display:none;">
            <div class="form-group">
              <label>Unidad de captura</label>
              <select id="ddlOrigenUnidad" class="form-control" onchange="onOrigenUnidadChange();">
              </select>
              <small id="lblOrigenConvInfo" class="text-muted d-block mt-1"></small>
            </div>
          </div>
        </div>

        <!-- DESTINO -->
        <div class="seccion-header mt-2"><i class="fas fa-arrow-left mr-1"></i> Materiales recuperados (destino al stock regular)</div>

        <table class="table table-bordered" id="tblOutputs" style="font-size:.88rem;">
          <thead>
            <tr>
              <th>Material destino</th>
              <th style="width:160px">Unidad</th>
              <th style="width:150px">Cantidad resultante</th>
              <th style="width:60px"></th>
            </tr>
          </thead>
          <tbody id="tbodyOutputs">
            <!-- filas dinámicas vía JS -->
          </tbody>
        </table>
        <button type="button" class="btn btn-sm btn-outline-success mb-3"
            onclick="agregarFilaOutput();">
            <i class="fas fa-plus mr-1"></i> Agregar otro material
        </button>

        <!-- OBSERVACIONES -->
        <div class="form-group">
          <label>Observaciones</label>
          <asp:TextBox ID="txtConvObs" runat="server" CssClass="form-control"
              TextMode="MultiLine" Rows="2" MaxLength="500"
              placeholder="Opcional..."></asp:TextBox>
        </div>

      </div><!-- /modal-body -->
      <div class="modal-footer">
        <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancelar</button>
        <asp:Button ID="btnGuardarConversion" runat="server" Text="Guardar Conversión"
            CssClass="btn btn-warning"
            OnClientClick="return prepararGuardar();"
            OnClick="btnGuardarConversion_Click" />
      </div>
    </div>
  </div>
</div>

<!-- ══ MODAL DETALLE CONVERSIÓN ════════════════════════════════════ -->
<div class="modal fade" id="modalDetalle" tabindex="-1">
  <div class="modal-dialog modal-lg">
    <div class="modal-content">
      <div class="modal-header" style="background:#1a5276;">
        <h5 class="modal-title text-white">
          <i class="fas fa-list-ul mr-2"></i>Materiales recuperados —
          <span id="spanDetalleOrigen"></span>
        </h5>
        <button type="button" class="close text-white" data-dismiss="modal">&times;</button>
      </div>
      <div class="modal-body">
        <div id="divDetalleSpinner" class="text-center py-4">
          <div class="spinner-border text-primary" role="status"><span class="sr-only">Cargando...</span></div>
          <p class="mt-2 text-muted">Cargando...</p>
        </div>
        <div id="divDetalleError" class="alert alert-danger d-none"></div>
        <div id="divDetalleTabla" class="d-none">
          <div class="table-responsive">
            <table class="table table-sm table-bordered mb-0">
              <thead class="bg-light">
                <tr>
                  <th>Material</th>
                  <th class="text-right">Cantidad recuperada</th>
                </tr>
              </thead>
              <tbody id="tbodyDetalleModal"></tbody>
            </table>
          </div>
          <p id="pDetalleVacio" class="text-muted mt-2 d-none">Sin materiales registrados.</p>
        </div>
      </div>
      <div class="modal-footer">
        <button type="button" class="btn btn-secondary" data-dismiss="modal">Cerrar</button>
      </div>
    </div>
  </div>
</div>

<!-- ══ DATOS PARA JS ═══════════════════════════════════════════════ -->
<script>
    window._stockMermaData    = <%= GetStockMermaJson() %>;
    window._materialesActivos = <%= GetMaterialesActivosJson() %>;
    window._conversionesMat   = <%= GetConversionesMatJson() %>;
</script>

<!-- ══ LÓGICA JS ════════════════════════════════════════════════════ -->
<script>
var _outputCounter = 0;
var _destTimers    = {};

function abrirModalConversion() {
    document.getElementById('<%: ddlConvBase.ClientID %>').value = '';
    // Campo A: reset autocomplete origen
    document.getElementById('txtConvMatOrigen').value = '';
    document.getElementById('<%: hdnConvMatOrigenID.ClientID %>').value = '';
    ocultarResultadosConvMatOrigen();
    document.getElementById('divDisponible').textContent = '— seleccione material —';
    document.getElementById('<%: txtConvCantOrigen.ClientID %>').value = '';
    document.getElementById('<%: txtConvObs.ClientID %>').value = '';
    document.getElementById('<%: hdnOutputsJson.ClientID %>').value = '';
    document.getElementById('<%: hdnOrigenUnidadVal.ClientID %>').value = '';
    document.getElementById('<%: hdnOrigenFactor.ClientID %>').value = '';
    document.getElementById('divOrigenUnidad').style.display = 'none';
    document.getElementById('ddlOrigenUnidad').innerHTML = '';
    document.getElementById('lblOrigenConvInfo').innerText = '';
    // Campo B: destruir filas y resetear timers
    document.getElementById('tbodyOutputs').innerHTML = '';
    _outputCounter = 0;
    _destTimers = {};
    agregarFilaOutput();
    $('#modalConversion').modal('show');
}

function onConvBaseChange() {
    document.getElementById('txtConvMatOrigen').value = '';
    document.getElementById('<%: hdnConvMatOrigenID.ClientID %>').value = '';
    ocultarResultadosConvMatOrigen();
    document.getElementById('divDisponible').textContent = '— seleccione material —';
    document.getElementById('divOrigenUnidad').style.display = 'none';
    document.getElementById('ddlOrigenUnidad').innerHTML = '';
    document.getElementById('lblOrigenConvInfo').innerText = '';
}

function onConvMatOrigenChange(matID, cantDisponible, unidTxt) {
    document.getElementById('<%: hdnConvMatOrigenID.ClientID %>').value = matID || '';
    document.getElementById('divOrigenUnidad').style.display = 'none';
    document.getElementById('ddlOrigenUnidad').innerHTML = '';
    document.getElementById('lblOrigenConvInfo').innerText = '';

    if (!matID) {
        document.getElementById('divDisponible').textContent = '— seleccione material —';
        return;
    }

    var cant = parseFloat(cantDisponible || 0);
    document.getElementById('divDisponible').textContent =
        cant.toLocaleString('es-MX', {minimumFractionDigits: 0, maximumFractionDigits: 4}) + ' ' + (unidTxt || '');

    var convKey = matID.toString();
    if (window._conversionesMat && window._conversionesMat[convKey] &&
        window._conversionesMat[convKey].length > 1) {
        var ddlU = document.getElementById('ddlOrigenUnidad');
        ddlU.innerHTML = '';
        window._conversionesMat[convKey].forEach(function(op) {
            var o = document.createElement('option');
            o.value = op.val;
            o.text  = op.txt;
            o.setAttribute('data-factor', op.factor);
            ddlU.appendChild(o);
        });
        document.getElementById('divOrigenUnidad').style.display = '';
        onOrigenUnidadChange();
    }
}

function onOrigenUnidadChange() {
    var ddlU = document.getElementById('ddlOrigenUnidad');
    if (!ddlU || ddlU.options.length === 0) return;
    var selOpt = ddlU.options[ddlU.selectedIndex];
    var factor = parseFloat(selOpt.getAttribute('data-factor')) || 1;
    var cant   = parseFloat(document.getElementById('<%: txtConvCantOrigen.ClientID %>').value) || 0;
    var label  = document.getElementById('lblOrigenConvInfo');
    if (factor === 1 || cant === 0) { label.innerText = ''; return; }
    var cantBase = cant * factor;
    label.innerText = 'Captura: ' + cant.toLocaleString('es-MX', {minimumFractionDigits:4}) +
                      ' → ' + cantBase.toLocaleString('es-MX', {minimumFractionDigits:4}) +
                      ' (unidad base) en merma';
}

function onOrigenCantChange() {
    var divU = document.getElementById('divOrigenUnidad');
    if (divU && divU.style.display !== 'none') onOrigenUnidadChange();
}

// ── Tabla de destino ──────────────────────────────────────────────

function agregarFilaOutput() {
    _outputCounter++;
    var tbody = document.getElementById('tbodyOutputs');
    var tr = document.createElement('tr');
    tr.id = 'rowOut_' + _outputCounter;

    var n = _outputCounter;
    tr.innerHTML =
        '<td style="position:relative;">' +
          '<div style="position:relative;">' +
            '<input type="text" id="txtDest_' + n + '" class="form-control form-control-sm" ' +
                   'placeholder="Buscar material..." autocomplete="off" ' +
                   'oninput="onTxtDestInput(' + n + ')" />' +
            '<div id="divResultadosDest_' + n + '" class="autocomplete-results"></div>' +
          '</div>' +
          '<input type="hidden" id="hdnDestMatID_' + n + '" class="hdn-mat-dest-id" value="" />' +
        '</td>' +
        '<td><select class="form-control form-control-sm sel-unidad-dest" ' +
             'id="selUnidDest_' + n + '" disabled>' +
             '<option value="0" data-factor="1">— seleccione material —</option>' +
             '</select></td>' +
        '<td><input type="text" class="form-control form-control-sm inp-cant-dest" ' +
             'id="cantDest_' + n + '" placeholder="0.00" ' +
             'oninput="this.value=this.value.replace(/[^0-9.]/g,\'\')"/></td>' +
        '<td><button type="button" class="btn btn-danger btn-sm btn-quitar-output" ' +
             'onclick="quitarFilaOutput(' + n + ')">✕</button></td>';
    tbody.appendChild(tr);
}

function quitarFilaOutput(n) {
    var tr = document.getElementById('rowOut_' + n);
    if (tr) tr.parentNode.removeChild(tr);
}

function onDestMatChange(n) {
    var hdn     = document.getElementById('hdnDestMatID_' + n);
    var selUnid = document.getElementById('selUnidDest_' + n);
    var matID   = parseInt((hdn && hdn.value) || 0) || 0;

    selUnid.innerHTML = '';
    if (!matID || !window._conversionesMat || !window._conversionesMat[matID.toString()]) {
        selUnid.disabled = true;
        selUnid.innerHTML = '<option value="0" data-factor="1">— sin unidad —</option>';
        return;
    }

    var opts = window._conversionesMat[matID.toString()];
    opts.forEach(function(op) {
        var o = document.createElement('option');
        o.value = op.val;
        o.text  = op.txt;
        o.setAttribute('data-factor', op.factor);
        selUnid.appendChild(o);
    });
    selUnid.disabled = (selUnid.options.length <= 1);
}

// ── Preparar y validar antes de postback ───────────────────────────

function prepararGuardar() {
    var baseID    = document.getElementById('<%: ddlConvBase.ClientID %>').value;
    var matOrigen = document.getElementById('<%: hdnConvMatOrigenID.ClientID %>').value;
    var cantOrig  = parseFloat(document.getElementById('<%: txtConvCantOrigen.ClientID %>').value || '0');

    if (!baseID)    { Swal.fire({icon:'warning',title:'Base requerida',   text:'Seleccione una base.',             confirmButtonColor:'#003366'}); return false; }
    if (!matOrigen) { Swal.fire({icon:'warning',title:'Material requerido',text:'Seleccione el material en merma.',confirmButtonColor:'#003366'}); return false; }
    if (!cantOrig || cantOrig <= 0) { Swal.fire({icon:'warning',title:'Cantidad inválida',text:'Ingrese la cantidad a convertir.',confirmButtonColor:'#003366'}); return false; }

    // Leer unidad/factor del origen
    var divOU = document.getElementById('divOrigenUnidad');
    var origenUnidVal = '';
    var origenFactor  = '1';
    if (divOU && divOU.style.display !== 'none') {
        var ddlOU = document.getElementById('ddlOrigenUnidad');
        if (ddlOU && ddlOU.options.length > 0) {
            origenUnidVal = ddlOU.options[ddlOU.selectedIndex].value;
            origenFactor  = ddlOU.options[ddlOU.selectedIndex].getAttribute('data-factor') || '1';
        }
    }
    document.getElementById('<%: hdnOrigenUnidadVal.ClientID %>').value = origenUnidVal;
    document.getElementById('<%: hdnOrigenFactor.ClientID %>').value    = origenFactor;

    // Recoger filas destino
    var outputs = [];
    var filas   = document.querySelectorAll('#tbodyOutputs tr');
    for (var i = 0; i < filas.length; i++) {
        var hdnMat  = filas[i].querySelector('.hdn-mat-dest-id');
        var selUnid = filas[i].querySelector('.sel-unidad-dest');
        var inp     = filas[i].querySelector('.inp-cant-dest');
        if (!hdnMat || !inp) continue;
        var mID  = parseInt(hdnMat.value || '0');
        var cant = parseFloat(inp.value || '0');
        if (!mID || cant <= 0) {
            Swal.fire({icon:'warning',title:'Fila incompleta',
                text:'Todas las filas de materiales destino deben tener material y cantidad.',
                confirmButtonColor:'#003366'});
            return false;
        }
        var unidId = 0;
        var factor = 1;
        if (selUnid && !selUnid.disabled && selUnid.options.length > 0) {
            var selOpt = selUnid.options[selUnid.selectedIndex];
            unidId = parseInt(selOpt.value) || 0;
            factor = parseFloat(selOpt.getAttribute('data-factor')) || 1;
        }
        outputs.push({ matID: mID, cant: cant, unidadId: unidId, factor: factor });
    }
    if (outputs.length === 0) {
        Swal.fire({icon:'warning',title:'Sin materiales destino',
            text:'Agregue al menos un material recuperado.',confirmButtonColor:'#003366'});
        return false;
    }

    document.getElementById('<%: hdnOutputsJson.ClientID %>').value = JSON.stringify(outputs);
    return true;
}

// ── Autocomplete: Material en merma — Campo A (client-side) ──────────

var _origenTimer = null;

function onTxtConvMatOrigenInput() {
    clearTimeout(_origenTimer);
    document.getElementById('<%: hdnConvMatOrigenID.ClientID %>').value = '';
    onConvMatOrigenChange('', 0, '');
    var baseID = document.getElementById('<%: ddlConvBase.ClientID %>').value;
    var q = document.getElementById('txtConvMatOrigen').value.trim().toLowerCase();
    if (!baseID || q.length < 2) { ocultarResultadosConvMatOrigen(); return; }
    _origenTimer = setTimeout(function() {
        var lista = (window._stockMermaData[baseID] || []).filter(function(m) {
            return (m.codigo + ' ' + m.descripcion).toLowerCase().indexOf(q) !== -1;
        });
        mostrarResultadosConvMatOrigen(lista);
    }, 300);
}

function mostrarResultadosConvMatOrigen(items) {
    var div = document.getElementById('divResultadosConvMatOrigen');
    div.innerHTML = '';
    if (!items || items.length === 0) {
        div.innerHTML = '<div style="padding:8px 10px;color:#888;font-size:.84rem;">Sin resultados</div>';
        div.style.display = 'block';
        return;
    }
    items.forEach(function(m) {
        var el = document.createElement('div');
        el.style.cssText = 'padding:7px 10px;cursor:pointer;font-size:.84rem;border-bottom:1px solid #f0f0f0;';
        el.textContent   = m.codigo + ' — ' + m.descripcion + ' (' + m.unidad + ')';
        el.onmouseenter  = function() { el.style.background = '#e8f0fe'; };
        el.onmouseleave  = function() { el.style.background = ''; };
        el.onmousedown   = function(e) {
            e.preventDefault();
            document.getElementById('txtConvMatOrigen').value = m.codigo + ' — ' + m.descripcion;
            ocultarResultadosConvMatOrigen();
            onConvMatOrigenChange(m.matID, m.cantDisponible, m.unidad);
        };
        div.appendChild(el);
    });
    div.style.display = 'block';
}

function ocultarResultadosConvMatOrigen() {
    var div = document.getElementById('divResultadosConvMatOrigen');
    if (div) div.style.display = 'none';
}

document.addEventListener('click', function(e) {
    var txt = document.getElementById('txtConvMatOrigen');
    var div = document.getElementById('divResultadosConvMatOrigen');
    if (txt && !txt.contains(e.target) && div && !div.contains(e.target))
        ocultarResultadosConvMatOrigen();
});

// ── Autocomplete: Materiales recuperados — Campo B (server-side, por fila) ──

function onTxtDestInput(n) {
    clearTimeout(_destTimers[n]);
    var hdn = document.getElementById('hdnDestMatID_' + n);
    if (hdn) hdn.value = '';
    var selUnid = document.getElementById('selUnidDest_' + n);
    if (selUnid) {
        selUnid.innerHTML = '<option value="0" data-factor="1">— seleccione material —</option>';
        selUnid.disabled = true;
    }
    var inp = document.getElementById('txtDest_' + n);
    if (!inp) return;
    var qVal = inp.value.trim();
    if (qVal.length < 2) { ocultarResultadosDest(n); return; }
    _destTimers[n] = setTimeout(function() { buscarDest(n, qVal); }, 300);
}

function buscarDest(n, query) {
    fetch('<%= ResolveUrl("~/Mermas.aspx/BuscarMaterialesMerma") %>', {
        method:  'POST',
        headers: { 'Content-Type': 'application/json' },
        body:    JSON.stringify({ query: query })
    })
    .then(function(r)    { return r.json(); })
    .then(function(data) { mostrarResultadosDest(n, data.d); })
    .catch(function()    { ocultarResultadosDest(n); });
}

function mostrarResultadosDest(n, items) {
    var div = document.getElementById('divResultadosDest_' + n);
    if (!div) return;
    div.innerHTML = '';
    if (!items || items.length === 0) {
        div.innerHTML = '<div style="padding:8px 10px;color:#888;font-size:.84rem;">Sin resultados</div>';
        div.style.display = 'block';
        return;
    }
    items.forEach(function(item) {
        var el = document.createElement('div');
        el.style.cssText = 'padding:7px 10px;cursor:pointer;font-size:.84rem;border-bottom:1px solid #f0f0f0;';
        el.textContent   = item.nombre;
        el.onmouseenter  = function() { el.style.background = '#e8f0fe'; };
        el.onmouseleave  = function() { el.style.background = ''; };
        el.onmousedown   = function(e) {
            e.preventDefault();
            document.getElementById('txtDest_' + n).value      = item.nombre;
            document.getElementById('hdnDestMatID_' + n).value = item.id;
            ocultarResultadosDest(n);
            onDestMatChange(n);
        };
        div.appendChild(el);
    });
    div.style.display = 'block';
}

function ocultarResultadosDest(n) {
    var div = document.getElementById('divResultadosDest_' + n);
    if (div) div.style.display = 'none';
}

document.addEventListener('click', function(e) {
    var rows = document.querySelectorAll('#tbodyOutputs tr');
    rows.forEach(function(tr) {
        var id = tr.id;
        if (!id) return;
        var n = parseInt(id.replace('rowOut_', ''));
        if (isNaN(n)) return;
        var txt = document.getElementById('txtDest_' + n);
        var div = document.getElementById('divResultadosDest_' + n);
        if (txt && !txt.contains(e.target) && div && !div.contains(e.target))
            ocultarResultadosDest(n);
    });
});

// ── Detalle de conversión (AJAX) ──────────────────────────────────
function verDetalle(conversionID, origen) {
    document.getElementById('spanDetalleOrigen').textContent = origen;
    document.getElementById('divDetalleSpinner').classList.remove('d-none');
    document.getElementById('divDetalleError').classList.add('d-none');
    document.getElementById('divDetalleTabla').classList.add('d-none');
    document.getElementById('tbodyDetalleModal').innerHTML = '';
    document.getElementById('pDetalleVacio').classList.add('d-none');
    $('#modalDetalle').modal('show');

    $.ajax({
        type: 'POST',
        url: 'Mermas.aspx/ObtenerDetalleConversion',
        data: JSON.stringify({ conversionID: conversionID }),
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        success: function(resp) {
            var items = resp.d;
            document.getElementById('divDetalleSpinner').classList.add('d-none');
            if (!items || items.length === 0) {
                document.getElementById('divDetalleTabla').classList.remove('d-none');
                document.getElementById('pDetalleVacio').classList.remove('d-none');
                return;
            }
            var tbody = document.getElementById('tbodyDetalleModal');
            items.forEach(function(r) {
                var col1 = '<td><strong style="color:#003366;">' + escHtml(r.Codigo) + '</strong> ' +
                           escHtml(r.Descripcion) + '</td>';
                var cantStr;
                if (r.TuvoConversion && r.CantidadCapturada !== null) {
                    cantStr = '<strong>' + fmtN4(r.CantidadCapturada) + '</strong> ' +
                              '<small class="text-muted">' + escHtml(r.UnidadCapturaNombre || '') + '</small>' +
                              '<br/><small class="text-muted">= ' + fmtN4(r.Cantidad) + ' ' +
                              escHtml(r.Unidad) + ' (base)</small>';
                } else {
                    cantStr = '<strong>' + fmtN4(r.Cantidad) + '</strong> ' +
                              '<small class="text-muted">' + escHtml(r.Unidad) + '</small>';
                }
                var col2 = '<td class="text-right" style="white-space:nowrap">' + cantStr + '</td>';
                tbody.insertAdjacentHTML('beforeend', '<tr>' + col1 + col2 + '</tr>');
            });
            document.getElementById('divDetalleTabla').classList.remove('d-none');
        },
        error: function(xhr) {
            document.getElementById('divDetalleSpinner').classList.add('d-none');
            var msg = 'Error al cargar el detalle.';
            try { var p = JSON.parse(xhr.responseText); if (p && p.Message) msg = p.Message; } catch(e) {}
            var err = document.getElementById('divDetalleError');
            err.textContent = msg;
            err.classList.remove('d-none');
        }
    });
}

function escHtml(s) {
    return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}
function fmtN4(n) {
    return Number(n).toLocaleString('es-MX', {minimumFractionDigits:0, maximumFractionDigits:4});
}

// ── SweetAlert pendiente ──────────────────────────────────────────
window.addEventListener('load', function () {
    var hdnMsg = document.getElementById('<%: hdnMensajePendiente.ClientID %>');
    if (!hdnMsg || !hdnMsg.value) return;
    try {
        var msg  = JSON.parse(hdnMsg.value);
        hdnMsg.value = '';
        var opts = { icon: msg.icon, title: msg.title, text: msg.text, confirmButtonColor: '#003366' };
        if (msg.icon === 'success') { opts.showConfirmButton = false; opts.timer = 2500; }
        Swal.fire(opts);
    } catch(e) {}
});
</script>

</asp:Content>

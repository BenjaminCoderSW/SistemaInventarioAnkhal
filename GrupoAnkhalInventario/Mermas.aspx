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
                <div class="lbl">Valor estimado en merma</div>
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
                                    <%# string.Format("{0:N4}", Eval("CantidadActual")) %>
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
                                <span style="color:#922b21;font-weight:700;">
                                    <%# string.Format("{0:N4}", Eval("CantidadOrigen")) %>
                                </span>
                                <small class="text-muted"><%# Eval("UnidadOrigen") %></small>
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
<asp:HiddenField ID="hdnOutputsJson"     runat="server" Value="" />

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
              <select id="ddlConvMatOrigen" class="form-control" onchange="onConvMatOrigenChange();">
                <option value="">-- Seleccione base primero --</option>
              </select>
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
                  placeholder="0.00" oninput="this.value=this.value.replace(/[^0-9.]/g,'')"></asp:TextBox>
            </div>
          </div>
        </div>

        <!-- DESTINO -->
        <div class="seccion-header mt-2"><i class="fas fa-arrow-left mr-1"></i> Materiales recuperados (destino al stock regular)</div>

        <table class="table table-bordered" id="tblOutputs" style="font-size:.88rem;">
          <thead>
            <tr>
              <th>Material destino</th>
              <th style="width:160px">Cantidad resultante</th>
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
</script>

<!-- ══ LÓGICA JS ════════════════════════════════════════════════════ -->
<script>
var _outputCounter = 0;

function abrirModalConversion() {
    // reset modal
    document.getElementById('<%: ddlConvBase.ClientID %>').value = '';
    document.getElementById('<%: hdnConvMatOrigenID.ClientID %>').value = '';
    var selOrigen = document.getElementById('ddlConvMatOrigen');
    selOrigen.innerHTML = '<option value="">-- Seleccione base primero --</option>';
    document.getElementById('divDisponible').textContent = '— seleccione material —';
    document.getElementById('<%: txtConvCantOrigen.ClientID %>').value = '';
    document.getElementById('<%: txtConvObs.ClientID %>').value = '';
    document.getElementById('<%: hdnOutputsJson.ClientID %>').value = '';
    document.getElementById('tbodyOutputs').innerHTML = '';
    _outputCounter = 0;
    agregarFilaOutput();   // empieza con una fila
    $('#modalConversion').modal('show');
}

function onConvBaseChange() {
    var baseID = document.getElementById('<%: ddlConvBase.ClientID %>').value;
    var sel    = document.getElementById('ddlConvMatOrigen');
    sel.innerHTML = '<option value="">-- Seleccione --</option>';
    document.getElementById('divDisponible').textContent = '— seleccione material —';
    document.getElementById('<%: hdnConvMatOrigenID.ClientID %>').value = '';
    if (!baseID || !window._stockMermaData[baseID]) return;
    window._stockMermaData[baseID].forEach(function(m) {
        var opt = document.createElement('option');
        opt.value = m.matID;
        opt.textContent = m.codigo + ' — ' + m.descripcion + ' (' + m.unidad + ')';
        opt.setAttribute('data-cant', m.cantDisponible);
        sel.appendChild(opt);
    });
}

function onConvMatOrigenChange() {
    var sel = document.getElementById('ddlConvMatOrigen');
    var opt = sel.options[sel.selectedIndex];
    document.getElementById('<%: hdnConvMatOrigenID.ClientID %>').value = sel.value || '';
    if (!sel.value) {
        document.getElementById('divDisponible').textContent = '— seleccione material —';
        return;
    }
    var cant = parseFloat(opt.getAttribute('data-cant') || 0);
    document.getElementById('divDisponible').textContent =
        cant.toLocaleString('es-MX', {minimumFractionDigits: 4}) + ' ' +
        (opt.textContent.match(/\(([^)]+)\)/) || ['',''])[1];
}

function agregarFilaOutput() {
    _outputCounter++;
    var tbody = document.getElementById('tbodyOutputs');
    var tr = document.createElement('tr');
    tr.id = 'rowOut_' + _outputCounter;

    var opts = '<option value="">-- Seleccione --</option>';
    (window._materialesActivos || []).forEach(function(m) {
        opts += '<option value="' + m.matID + '">' +
                m.codigo + ' — ' + m.descripcion + ' (' + m.unidad + ')</option>';
    });

    tr.innerHTML =
        '<td><select class="form-control form-control-sm sel-mat-dest" ' +
             'id="selDest_' + _outputCounter + '">' + opts + '</select></td>' +
        '<td><input type="text" class="form-control form-control-sm inp-cant-dest" ' +
             'id="cantDest_' + _outputCounter + '" placeholder="0.00" ' +
             'oninput="this.value=this.value.replace(/[^0-9.]/g,\'\')"/></td>' +
        '<td><button type="button" class="btn btn-danger btn-sm btn-quitar-output" ' +
             'onclick="quitarFilaOutput(' + _outputCounter + ')">✕</button></td>';
    tbody.appendChild(tr);
}

function quitarFilaOutput(n) {
    var tr = document.getElementById('rowOut_' + n);
    if (tr) tr.parentNode.removeChild(tr);
}

function prepararGuardar() {
    var baseID    = document.getElementById('<%: ddlConvBase.ClientID %>').value;
    var matOrigen = document.getElementById('<%: hdnConvMatOrigenID.ClientID %>').value;
    var cantOrig  = parseFloat(document.getElementById('<%: txtConvCantOrigen.ClientID %>').value || '0');

    if (!baseID)    { Swal.fire({icon:'warning',title:'Base requerida',   text:'Seleccione una base.',             confirmButtonColor:'#003366'}); return false; }
    if (!matOrigen) { Swal.fire({icon:'warning',title:'Material requerido',text:'Seleccione el material en merma.',confirmButtonColor:'#003366'}); return false; }
    if (!cantOrig || cantOrig <= 0) { Swal.fire({icon:'warning',title:'Cantidad inválida',text:'Ingrese la cantidad a convertir.',confirmButtonColor:'#003366'}); return false; }


    var outputs = [];
    var filas = document.querySelectorAll('#tbodyOutputs tr');
    for (var i = 0; i < filas.length; i++) {
        var sel  = filas[i].querySelector('.sel-mat-dest');
        var inp  = filas[i].querySelector('.inp-cant-dest');
        if (!sel || !inp) continue;
        var mID  = parseInt(sel.value || '0');
        var cant = parseFloat(inp.value || '0');
        if (!mID || cant <= 0) {
            Swal.fire({icon:'warning',title:'Fila incompleta',
                text:'Todas las filas de materiales destino deben tener material y cantidad.',
                confirmButtonColor:'#003366'});
            return false;
        }
        outputs.push({matID: mID, cant: cant});
    }
    if (outputs.length === 0) {
        Swal.fire({icon:'warning',title:'Sin materiales destino',
            text:'Agregue al menos un material recuperado.',confirmButtonColor:'#003366'});
        return false;
    }

    document.getElementById('<%: hdnOutputsJson.ClientID %>').value = JSON.stringify(outputs);
    return true;
}

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
                var col1 = '<td><strong style="color:#003366;">' + escHtml(r.Codigo) + '</strong> ' + escHtml(r.Descripcion) + '</td>';
                var col2 = '<td class="text-right" style="white-space:nowrap"><strong>' +
                           fmtN4(r.Cantidad) + '</strong> <small class="text-muted">' + escHtml(r.Unidad) + '</small></td>';
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
    return Number(n).toLocaleString('es-MX', {minimumFractionDigits:4, maximumFractionDigits:4});
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

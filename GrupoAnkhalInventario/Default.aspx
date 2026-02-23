<%@ Page Title="Dashboard" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="GrupoAnkhalInventario.Default" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        .welcome-card {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            border-radius: 10px;
            padding: 30px;
            margin-bottom: 20px;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        }
        
        .info-box-custom {
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            transition: transform 0.2s;
        }
        
        .info-box-custom:hover {
            transform: translateY(-5px);
            box-shadow: 0 4px 8px rgba(0,0,0,0.2);
        }
        
        .info-box-icon-custom {
            border-radius: 8px 0 0 8px;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <!-- Welcome Card -->
    <div class="welcome-card">
        <h1 class="mb-2">
            <i class="fas fa-tachometer-alt"></i> 
            Bienvenido al Sistema de Inventario
        </h1>
        <h3>
            <asp:Label ID="lblNombreUsuario" runat="server" Text="Usuario"></asp:Label>
        </h3>
        <p class="mb-0">
            <i class="fas fa-user-tag"></i> 
            Rol: <asp:Label ID="lblRol" runat="server" Text=""></asp:Label>
        </p>
        <p class="mb-0">
            <i class="fas fa-calendar-alt"></i> 
            <asp:Label ID="lblFechaHora" runat="server" Text=""></asp:Label>
        </p>
    </div>

    <!-- Info Boxes Row -->
    <div class="row">
        <!-- Dashboard Info Box -->
        <div class="col-lg-3 col-6">
            <div class="info-box info-box-custom bg-info">
                <span class="info-box-icon info-box-icon-custom">
                    <i class="fas fa-home"></i>
                </span>
                <div class="info-box-content">
                    <span class="info-box-text">Panel Principal</span>
                    <span class="info-box-number">Dashboard</span>
                </div>
            </div>
        </div>

        <!-- Inventario Info Box -->
        <div class="col-lg-3 col-6">
            <div class="info-box info-box-custom bg-success">
                <span class="info-box-icon info-box-icon-custom">
                    <i class="fas fa-boxes"></i>
                </span>
                <div class="info-box-content">
                    <span class="info-box-text">Inventario</span>
                    <span class="info-box-number">Disponible</span>
                </div>
            </div>
        </div>

        <!-- Producción Info Box -->
        <div class="col-lg-3 col-6">
            <div class="info-box info-box-custom bg-warning">
                <span class="info-box-icon info-box-icon-custom">
                    <i class="fas fa-industry"></i>
                </span>
                <div class="info-box-content">
                    <span class="info-box-text">Producción</span>
                    <span class="info-box-number">Activa</span>
                </div>
            </div>
        </div>

        <!-- Sistema Info Box -->
        <div class="col-lg-3 col-6">
            <div class="info-box info-box-custom bg-danger">
                <span class="info-box-icon info-box-icon-custom">
                    <i class="fas fa-cogs"></i>
                </span>
                <div class="info-box-content">
                    <span class="info-box-text">Sistema</span>
                    <span class="info-box-number">v1.0</span>
                </div>
            </div>
        </div>
    </div>

    <!-- Quick Actions -->
    <div class="row">
        <div class="col-12">
            <div class="card">
                <div class="card-header bg-primary text-white">
                    <h3 class="card-title">
                        <i class="fas fa-bolt"></i> Accesos Rápidos
                    </h3>
                </div>
                <div class="card-body">
                    <div class="row">
                        <!-- Botón Inventario -->
                        <div class="col-md-3 col-sm-6 mb-3" id="divInventario" runat="server">
                            <a href="Reportes/Inventario.aspx" class="btn btn-app btn-lg w-100">
                                <i class="fas fa-clipboard-list"></i> Inventario
                            </a>
                        </div>
                        
                        <!-- Botón Producción -->
                        <div class="col-md-3 col-sm-6 mb-3" id="divProduccion" runat="server">
                            <a href="Operaciones/Produccion.aspx" class="btn btn-app btn-lg w-100">
                                <i class="fas fa-industry"></i> Producción
                            </a>
                        </div>
                        
                        <!-- Botón Entregas -->
                        <div class="col-md-3 col-sm-6 mb-3" id="divEntregas" runat="server">
                            <a href="Operaciones/Entregas.aspx" class="btn btn-app btn-lg w-100">
                                <i class="fas fa-truck"></i> Entregas
                            </a>
                        </div>
                        
                        <!-- Botón Catálogos -->
                        <div class="col-md-3 col-sm-6 mb-3" id="divCatalogos" runat="server">
                            <a href="Catalogos/Materiales.aspx" class="btn btn-app btn-lg w-100">
                                <i class="fas fa-cogs"></i> Catálogos
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- System Info -->
    <div class="row mt-3">
        <div class="col-12">
            <div class="callout callout-info">
                <h5>
                    <i class="fas fa-info-circle"></i> Información del Sistema
                </h5>
                <p>Sistema de Gestión de Inventario - Grupo ANKHAL</p>
                <p class="mb-0">
                    <small>
                        <i class="fas fa-user"></i> Usuario: <asp:Label ID="lblUsuarioInfo" runat="server"></asp:Label> | 
                        <i class="fas fa-shield-alt"></i> Rol: <asp:Label ID="lblRolInfo" runat="server"></asp:Label> | 
                        <i class="fas fa-clock"></i> Última sesión: <asp:Label ID="lblUltimoAcceso" runat="server"></asp:Label>
                    </small>
                </p>
            </div>
        </div>
    </div>
</asp:Content>
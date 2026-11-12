# AutoFix – Sistema de Gestión de Órdenes de Reparación Automotriz

Plataforma web completa construida en **.NET 10** con una **ASP.NET Core Web API** (`AutoFix.Api`) conectada mediante **Entity Framework Core** a **SQLite**, y un frontend **ASP.NET Core MVC** (`AutoFix.Mvc`) que consume exclusivamente la API a través de `HttpClient` con autenticación **JWT**, **roles**, **claims**, gestión de **imágenes**, **concurrencia optimista** y **middlewares de auditoría y Correlation ID**.

---

## Estructura de la solución

```text
AutoFix.sln
├── AutoFix.Api/         # Web API (.NET 10): controladores, servicios, EF Core + SQLite, middlewares, wwwroot/uploads
├── AutoFix.Mvc/         # Frontend MVC (.NET 10): 12 vistas obligatorias, controladores, HttpClient (sin acceso a BD)
├── evidencias/          # Evidencias de las 12 pruebas obligatorias
└── README.md            # Documentación e instrucciones de ejecución
```

---

## Requisitos

- **.NET 10 SDK** (`10.0.x`)
- **Herramienta `dotnet-ef`** (para migraciones):
  ```bash
  dotnet tool install --global dotnet-ef
  ```
- **Git**
- *(Opcional)* DB Browser for SQLite / Swagger / Postman

---

## Instalación, Restauración y Migraciones

1. **Clonar el repositorio y restaurar paquetes:**
   ```bash
   git clone https://github.com/MarioAlabi/-Laboratorio-AutoFix.git
   cd -Laboratorio-AutoFix
   dotnet restore AutoFix.sln
   ```

2. **Ejecutar las migraciones de Entity Framework Core (SQLite):**
   ```bash
   cd AutoFix.Api
   dotnet ef database update
   cd ..
   ```
   > Al iniciar `AutoFix.Api`, `DbSeeder` aplica automáticamente las migraciones pendientes y siembra los usuarios de prueba, clientes, vehículos, mecánicos y repuestos iniciales en `AutoFix.Api/autofix.db` (usando `Data Source=autofix.db;Default Timeout=30`).

---

## Cómo ejecutar la API y el MVC

Abrir dos terminales en la raíz de la solución (o ejecutar ambos proyectos desde `AutoFix.sln` en Visual Studio / Rider):

1. **Terminal 1 — Ejecutar la Web API (`http://localhost:5215`):**
   ```bash
   dotnet run --project AutoFix.Api --launch-profile http
   ```
   - Swagger UI disponible en: `http://localhost:5215/swagger`

2. **Terminal 2 — Ejecutar el Frontend MVC (`http://localhost:5020`):**
   ```bash
   dotnet run --project AutoFix.Mvc --launch-profile http
   ```
   - Aplicación web disponible en: `http://localhost:5020`

---

## Usuarios de prueba (uno por rol)

| Rol | Correo electrónico | Contraseña | Claims (`permiso`) asignados |
| :--- | :--- | :--- | :--- |
| **Administrador** | `admin@autofix.com` | `Admin123!` | Todos (`Ordenes.Crear`, `Ordenes.Editar`, `Ordenes.Asignar`, `Ordenes.Aprobar`, `Ordenes.Finalizar`, `Inventario.Gestionar`, `Reportes.Consultar`) |
| **Recepcionista** | `recepcion@autofix.com` | `Recepcion123!` | `Ordenes.Crear`, `Ordenes.Editar` |
| **Mecánico** | `mecanico@autofix.com` | `Mecanico123!` | `Ordenes.Editar` |
| **Supervisor** | `supervisor@autofix.com` | `Supervisor123!` | `Ordenes.Asignar`, `Ordenes.Aprobar`, `Ordenes.Finalizar`, `Inventario.Gestionar`, `Reportes.Consultar` |
| **Cliente** | `cliente@autofix.com` | `Cliente123!` | *(Solo lectura de sus propios vehículos, órdenes, evidencias y presupuestos)* |

> **Seguridad:** La clave JWT de desarrollo está configurada únicamente para entorno `Development` en `appsettings.Development.json`. Para producción debe proveerse mediante variable de entorno `JWT_KEY` o User Secrets.

---

## Arquitectura e Implementación de Requerimientos

- **Módulo 1 (JWT, Roles y Claims):** Registro (`POST /api/auth/register`), inicio de sesión (`POST /api/auth/login`), respuestas `401` sin JWT y `403` cuando el rol o claim requerido no coincide.
- **Módulo 2 (Clientes y Vehículos):** Relación 1:N, validación de DUI/placa únicos y carga de fotografía del vehículo.
- **Módulo 3 (Órdenes):** Prevención de órdenes activas duplicadas (`409 Conflict`), flujo estricto de los **9 estados** (`Recibido` -> `EnDiagnóstico` -> `Presupuestado` -> `PendienteAprobación` -> `Aprobado` -> `EnReparación` -> `Finalizado` -> `Entregado` / `Cancelado`).
- **Módulo 4 y 5 (Presupuestos e Inventario):** Cálculo automático de subtotales/total, descuento transaccional de inventario sin permitir existencias negativas y alertas automáticas cuando `Existencias <= StockMinimo`.
- **Módulo 6 (Concurrencia Optimista con SQLite):**
  - Clase base `EntidadConcurrente` con `[ConcurrencyCheck] public Guid Version` renovado automáticamente en `AutoFixDbContext.SaveChangesAsync`.
  - **Escenario A (Inventario):** Transacción explícita en `UsarRepuestoAsync` con manejo de `DbUpdateConcurrencyException` -> `409 Conflict`.
  - **Escenario B (Asignación de Mecánicos):** Validación de cruce de horario (`nuevoInicio < finExistente && inicioExistente < nuevoFin`) + actualización del token `Version` del mecánico dentro de transacción -> `409 Conflict`.
- **Módulo 7 (Imágenes y Evidencias):** Subida `multipart/form-data` validando extensiones `.jpg`, `.jpeg`, `.png` y máximo `5 MB`, almacenando archivos con nombre `Guid` en `AutoFix.Api/wwwroot/uploads/` y solo la ruta relativa en SQLite.
- **Módulo 8 (Middlewares y Logs):** Orden estricto en `Program.cs`:
  `1) CorrelationIdMiddleware -> 2) ExceptionHandlingMiddleware -> 3) RequestLoggingMiddleware -> 4) UseAuthentication -> 5) UseAuthorization -> 6) MapControllers`.
- **Frontend MVC (12 vistas obligatorias):** `Login`, `Dashboard`, `Clientes`, `Vehículos`, `Órdenes`, `Detalle de orden`, `Presupuestos`, `Mecánicos`, `Inventario`, `Movimientos`, `Evidencias` y `Reportes` (con visor de logs filtrable por código HTTP, usuario y `Correlation ID`).

---

## Evidencias de las 12 Pruebas Obligatorias

Los resultados detallados de ejecución de las **12 pruebas obligatorias** (incluyendo las pruebas de concurrencia simultánea en Escenarios A y B, rechazo de imágenes inválidas, respuestas `401`/`403`/`409` y consulta de logs con `Correlation ID`) se encuentran documentados en:

- [evidencias/reporte_12_pruebas.md](evidencias/reporte_12_pruebas.md)

### Capturas de Demostración

| Evidencia | Caso demostrado |
| :--- | :--- |
| [01_login_mvc.png](evidencias/01_login_mvc.png) | Inicio de sesión JWT en MVC con usuarios de prueba |
| [02_dashboard_mvc.png](evidencias/02_dashboard_mvc.png) | Dashboard operativo con resumen de órdenes, alertas y claims JWT |
| [03_clientes_2_vehiculos_mvc.png](evidencias/03_clientes_2_vehiculos_mvc.png) | Prueba 1: Cliente registrado con 2 vehículos asociados |
| [04_orden_detalle_evidencias_mvc.png](evidencias/04_orden_detalle_evidencias_mvc.png) | Pruebas 2, 3 y 7: Flujo de estados de orden, diagnóstico y evidencia fotográfica |
| [05_presupuesto_aprobado_mvc.png](evidencias/05_presupuesto_aprobado_mvc.png) | Pruebas 5 y 6: Presupuesto aprobado con cálculo automático de subtotales y total |
| [06_inventario_alertas_mvc.png](evidencias/06_inventario_alertas_mvc.png) | Prueba 8: Alerta automática de stock mínimo en inventario |
| [07_fallo_concurrencia_inventario_mvc.png](evidencias/07_fallo_concurrencia_inventario_mvc.png) | Prueba 10 (Escenario A - MVC): Fallo `409 Conflict` por concurrencia optimista en repuesto |
| [08_fallo_concurrencia_mecanico_mvc.png](evidencias/08_fallo_concurrencia_mecanico_mvc.png) | Prueba 10 (Escenario B - MVC): Fallo `409 Conflict` por asignación concurrente de mecánico |
| [09_fallo_stock_insuficiente_mvc.png](evidencias/09_fallo_stock_insuficiente_mvc.png) | Prueba 9 (MVC): Rechazo por existencias insuficientes sin stock negativo |
| [10_reportes_logs_409_correlation_id_mvc.png](evidencias/10_reportes_logs_409_correlation_id_mvc.png) | Prueba 12: Auditoría de errores `409` en SQLite filtrada con `Correlation ID` |
| [11_swagger_api.png](evidencias/11_swagger_api.png) | Documentación Swagger UI de los endpoints REST de `AutoFix.Api` |
| [12_pruebas_api_concurrencia_consola.png](evidencias/12_pruebas_api_concurrencia_consola.png) | Pruebas 4, 9, 10 (Escenarios A y B simultáneos) y 11 verificadas contra la API |

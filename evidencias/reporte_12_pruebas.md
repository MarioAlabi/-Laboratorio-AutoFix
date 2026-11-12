# Evidencias de Ejecución — 12 Pruebas Obligatorias AutoFix

## Prueba 11: Acceder a la API sin JWT
- **Resultado:** HTTP `401` (Unauthorized)
- **X-Correlation-ID:** `115821e8-e7b4-496d-a21f-1ab71ea7942b`

## Prueba 1: Registrar un cliente con dos vehículos
- **Cliente creado:** `Marcos Rivera 0643A` (Id `3`)
- **Vehículos:** `P0643A1` y `P0643A2` -> HTTP `201` / `201` / `201`

## Prueba 2: Crear una orden de reparación (y bloqueo de duplicada)
- **Orden creada:** `ORD-20261112-0001` en estado `Recibido` (HTTP `201`)
- **Intento duplicado:** HTTP `409` — `Ya existe una orden activa igual para este vehículo y servicio.`

## Prueba 3: Asignar un mecánico
- **Orden:** `ORD-20261112-0001` -> Mecánico `Roberto Gómez` (HTTP `200`)

## Prueba 4: Modificar una orden sin permisos
- **Rol Cliente intentando PUT orden:** HTTP `403` (Forbidden)
- **X-Correlation-ID:** `a7fcccc0-6a22-45b2-94af-1f01e0da54c0`

## Prueba 5: Crear un presupuesto
- **Presupuesto #1:** Mano de obra `$35.00` + 2x `$12.50` = **Total `$60.00`** (HTTP `201`)

## Prueba 6: Aprobar el presupuesto con usuario autorizado
- **Mecánico intentando aprobar:** HTTP `403` (Forbidden)
- **Supervisor aprobando:** HTTP `200` -> Orden en estado **`Aprobado`**

## Prueba 7: Registrar fotografías de los daños
- **Imagen válida PNG:** HTTP `200` -> Ruta guardada `uploads/e93ca8ca-a279-4443-a8e5-c8efd1fa8383.png`
- **Archivo `.txt` inválido:** HTTP `400` (`Solo se permiten JPG, JPEG y PNG.`)
- **Imagen > 5 MB:** HTTP `400` (`La imagen no puede superar 5 MB.`)

## Prueba 8: Descontar repuestos del inventario
- **HTTP `200`:** Existencias actualizadas a `3` (`Repuesto descontado. ALERTA: 'Amortiguador Delantero Gas' alcanzó el stock mínimo (3/4).`)

## Prueba 9: Descontar más repuestos de los disponibles
- **HTTP `400`:** `Existencias insuficientes para 'Amortiguador Delantero Gas'. Disponibles: 3, solicitadas: 99.`
- **Existencias intactas:** `3`

## Prueba 10: Dos operaciones simultáneas (Concurrencia)
- **Escenario A (Inventario, 2 unidades pedidas por 2 usuarios):** Respuestas `200` (OK) y `409` (Conflict)
- **Escenario B (Asignación simultánea del mismo mecánico):** Respuestas `200` (OK) y `409` (Conflict)

## Prueba 12: Consultar los logs de una operación fallida
- **HTTP `200`** — Total logs 409 encontrados: `3`
- **Ejemplo registrado:** Usuario `supervisor@autofix.com`, Endpoint `POST /api/mecanicos/asignar`, Código `409`, Duración `155 ms`, Correlation ID `c5bafcfb-4cd5-4aff-921a-dfd3a2e4843d`

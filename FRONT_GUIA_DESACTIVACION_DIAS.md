# Guía Frontend: Desactivación de días y reprogramación de citas

## Objetivo funcional

Cuando un usuario con rol permitido desactiva un día del horario de un barbero:

1. El horario queda inactivo.
2. Se cancelan automáticamente las citas de esa fecha.
3. La API retorna los datos para notificar al cliente y ofrecer reprogramación.

## Roles permitidos

- Super administrador
- Administrador
- Barbero (solo sobre su propio horario)

La validación se hace con `UsuarioSolicitanteId` en el body.

## Endpoint a consumir

- `POST /api/HorariosBarberos/{id}/estado`

`id` corresponde al registro de horario semanal (`HorariosBarbero.Id`).

## Request para desactivar día

```json
{
  "estado": false,
  "usuarioSolicitanteId": 12,
  "fechaReferencia": "2026-03-23",
  "motivo": "Capacitación interna",
  "cantidadSugerencias": 3
}
```

## Reglas de request

- `estado` debe ir en `false` para activar el flujo de cancelación automática.
- `usuarioSolicitanteId` es obligatorio para desactivación.
- `fechaReferencia` debe corresponder al mismo día de semana del horario (`DiaSemana`).
- `cantidadSugerencias` define cuántos horarios sugeridos devuelve por cita.

## Respuesta esperada (resumen)

```json
{
  "exitoso": true,
  "mensaje": "Horario desactivado y citas afectadas canceladas.",
  "horarioId": 10,
  "barberoId": 4,
  "fechaDesactivada": "2026-03-23",
  "citasCanceladas": 2,
  "detalle": [
    {
      "citaId": 51,
      "clienteId": 8,
      "clienteNombre": "Ana Ruiz",
      "clienteCorreo": "ana@mail.com",
      "fechaHoraOriginal": "2026-03-23T10:00:00",
      "estadoFinal": "Cancelada",
      "notificacion": {
        "enviado": true,
        "canal": "in_app",
        "mensaje": "Cancelación notificada para consumo frontend (in-app), sin envío de correo."
      },
      "sugerenciasReprogramacion": [
        "2026-03-24T09:00:00",
        "2026-03-24T09:30:00",
        "2026-03-25T11:00:00"
      ]
    }
  ],
  "integracionCorreo": {
    "activa": false,
    "estado": "flujo_correo_general_deshabilitado_en_program",
    "recomendacion": "Mantener notificación in-app desde este endpoint hasta habilitar un servicio SMTP dedicado."
  }
}
```

## Flujo UI recomendado

1. Usuario selecciona día y motivo de desactivación.
2. Front llama `POST /api/HorariosBarberos/{id}/estado` con `estado=false`.
3. Front muestra resumen de `citasCanceladas`.
4. Por cada cliente afectado:
   - Mostrar mensaje de cancelación.
   - Renderizar `sugerenciasReprogramacion` como opciones rápidas.
5. Si cliente acepta reprogramar:
   - Reutilizar endpoint de actualización de agendamiento (`PUT /api/Agendamientos/{id}`) para nueva fecha/hora.
   - Ajustar estado del agendamiento según flujo del negocio.

## Manejo de errores de negocio

- `400`: `FechaReferencia` no coincide con el día de semana del horario.
- `400`: Falta `UsuarioSolicitanteId` para desactivación.
- `401`: Usuario solicitante inválido o inactivo.
- `403`: Rol sin permisos o barbero intentando desactivar horario ajeno.
- `404`: Horario no encontrado.

## Consideración de correo

El backend actual no tiene flujo SMTP activo global. El contrato retorna información de notificación in-app para no bloquear UX. Cuando exista un servicio de correo activo, el frontend no necesita cambiar el payload principal; solo debe mostrar el estado de `notificacion.canal` y `integracionCorreo`.

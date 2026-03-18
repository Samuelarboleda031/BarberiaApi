# Cambios sugeridos para frontend tras optimización del backend

## 1. Objetivo

Alinear el frontend con las mejoras de rendimiento del backend para obtener menor latencia percibida, menos solicitudes redundantes y mejor experiencia de usuario.

## 2. Cambios recomendados (prioridad alta)

### 2.1. Ajustar estrategia de refresco de datos

- Considerar ventana de frescura de 10 segundos en endpoints GET cacheados.
- Evitar refetch agresivo inmediato después de navegar entre vistas del mismo recurso.
- Usar invalidación inteligente de cache del cliente tras operaciones POST/PUT/DELETE.

### 2.2. Gestionar actualización después de mutaciones

Después de crear/editar/eliminar:

- Opción A: refrescar lista con pequeño debounce (300–800ms).
- Opción B: actualizar estado local optimistamente y sincronizar luego.
- Opción C: usar una clave de consulta versionada por recurso para forzar recarga cuando sea necesario.

### 2.3. Aprovechar compresión

- Confirmar que el cliente envía `Accept-Encoding: gzip, br`.
- Validar en navegador (Network) que respuestas JSON llegan comprimidas.

### 2.4. Reducir solicitudes duplicadas

- Evitar dobles llamadas por montaje múltiple de componentes.
- Consolidar requests simultáneos a mismo endpoint/paginación/filtro.
- Implementar memoización de parámetros de búsqueda y paginación.

## 3. Ajustes sugeridos por tipo de pantalla

### 3.1. Listados con paginación

- Mantener `page`, `pageSize`, `q` en estado de ruta o store.
- Reutilizar resultado de página anterior/siguiente si no hubo cambios.
- Prefetch opcional de página siguiente para navegación rápida.

### 3.2. Vistas de detalle

- Mostrar detalle desde cache local mientras llega actualización.
- En entidades críticas, permitir botón “actualizar ahora”.

### 3.3. Dashboard

- Configurar auto-refresh moderado (ej. cada 20–30 segundos, no cada 2–5).
- Pausar auto-refresh cuando la pestaña no está activa.

## 4. Contratos que el frontend debe respetar

- Mantener envío correcto de parámetros de filtro y paginación:
  - `page`, `pageSize`, `q`
  - filtros específicos (`desde`, `hasta`, `barberoId`, `clienteId`, `productoId`, `entregaId`)
- En endpoints por ruta, respetar parámetros como:
  - `/.../{id}`
  - `/.../barbero/{barberoId}`
  - `/.../paquete/{paqueteId}`
  - `/.../disponibles/{fecha}`

## 5. Riesgos UX a prevenir

- Mostrar datos “viejos” hasta 10 segundos puede confundir tras una edición inmediata.
- Solución recomendada:
  - notificación tipo “datos actualizados”,
  - botón “refrescar” en tablas críticas,
  - refresco automático después de mutaciones exitosas.

## 6. Checklist de verificación frontend

- Verificar menor tiempo de carga en listados principales.
- Verificar menor tamaño de payload en pestaña Network.
- Verificar que no existan duplicados de requests al cambiar filtros.
- Verificar comportamiento correcto después de crear/editar/eliminar.
- Verificar que paginación y búsqueda sigan funcionando igual.

## 7. Mejoras opcionales recomendadas

- Implementar estrategia de cache en cliente con librería de consultas.
- Añadir placeholders/skeletons para mejorar percepción de velocidad.
- Persistir filtros y paginación en URL para navegación consistente.
- Agregar telemetría frontend para medir TTFB, tiempo de render y errores por endpoint.

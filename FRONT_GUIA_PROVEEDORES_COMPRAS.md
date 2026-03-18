# Guía de integración Frontend: Proveedores y Compras

## Resumen
- Creación de proveedores unificada en `POST /api/Proveedores`
- `TipoProveedor` en el cuerpo: `"Natural"` o `"Juridico"`
- Obligatorios: `Nombre`, `NIT`, `Correo`, `Telefono`, `Direccion`
- Opcionales: `Contacto`, `NumeroIdentificacion`, `TipoIdentificacion`, `CorreoContacto`, `TelefonoContacto`
- Si no se envía `TipoIdentificacion`:
  - Natural: usa `"CC"`
  - Jurídico: usa `"NIT"`

## Referencias Backend
- DTO unificado: ProveedorCreateInput en [Models/DTOs.cs](file:///c:/Users/samue/Downloads/BarberiaApi/Models/DTOs.cs#L325-L344)
- Endpoint unificado: [Controllers/ProveedoresController.cs](file:///c:/Users/samue/Downloads/BarberiaApi/Controllers/ProveedoresController.cs#L124-L158)
- Campos opcionales del modelo: [Models/Proveedor.cs:L24-29](file:///c:/Users/samue/Downloads/BarberiaApi/Models/Proveedor.cs#L24-L29)
- Validaciones de Compras: [Controllers/ComprasController.cs](file:///c:/Users/samue/Downloads/BarberiaApi/Controllers/ComprasController.cs#L47-L118)

## Endpoints Proveedores
- `GET /api/Proveedores`
- `GET /api/Proveedores/naturales`
- `GET /api/Proveedores/juridicos`
- `GET /api/Proveedores/{id}`
- `POST /api/Proveedores` (unificado)
- `PUT /api/Proveedores/{id}`
- `POST /api/Proveedores/{id}/estado`
- `DELETE /api/Proveedores/{id}` (soft delete si tiene compras)

## Contrato de creación unificado
Request:

```json
{
  "TipoProveedor": "Natural",
  "Nombre": "Proveedor NN",
  "NIT": "123456789-0",
  "Correo": "proveedor@correo.com",
  "Telefono": "3001234567",
  "Direccion": "Calle 1 # 2-3",
  "Contacto": "Juan Perez",
  "NumeroIdentificacion": "10203040",
  "TipoIdentificacion": "CC",
  "CorreoContacto": "juan.perez@correo.com",
  "TelefonoContacto": "3017654321"
}
```

Response: objeto Proveedor con `TipoProveedor`, `Estado=true` y campos mapeados.

## Validaciones Proveedores (Front)
- `TipoProveedor` requerido: `"Natural"` | `"Juridico"`
- Obligatorios: `Nombre`, `NIT`, `Correo`, `Telefono`, `Direccion`
- Opcionales: `Contacto`, `NumeroIdentificacion`, `TipoIdentificacion`, `CorreoContacto`, `TelefonoContacto`
- Si falta `TipoIdentificacion`:
  - Natural → `"CC"`
  - Jurídico → `"NIT"`
- NIT único: si backend retorna `400 "Ya existe un proveedor con ese NIT"`, mostrar error en NIT

## UI Proveedores (sugerencias)
- Un solo formulario con selector `TipoProveedor`
- Campos obligatorios con indicación visual
- Sección plegable para opcionales (Contacto, Identificación, Contacto alterno)
- Acciones:
  - Crear: `POST /api/Proveedores`
  - Editar: `PUT /api/Proveedores/{id}`
  - Cambiar estado: `POST /api/Proveedores/{id}/estado`
  - Eliminar: `DELETE /api/Proveedores/{id}` (muestra “desactivado” si `fisico=false`)

## Tipos TypeScript (sugeridos)
Request:

```ts
export type TipoProveedor = "Natural" | "Juridico";

export interface ProveedorCreateInput {
  TipoProveedor: TipoProveedor;
  Nombre: string;
  NIT: string;
  Correo: string;
  Telefono: string;
  Direccion: string;
  Contacto?: string;
  NumeroIdentificacion?: string;
  TipoIdentificacion?: string; // si no se envía, backend usa CC/NIT por tipo
  CorreoContacto?: string;
  TelefonoContacto?: string;
}
```

Response:

```ts
export interface ProveedorDto {
  id: number;
  nombre: string;
  nit: string | null;
  correo: string | null;
  telefono: string | null;
  direccion: string | null;
  estado: boolean | null;
  tipoProveedor: "Natural" | "Juridico";
  contacto?: string | null;
  numeroIdentificacion?: string | null;
  tipoIdentificacion?: string | null;
  correoContacto?: string | null;
  telefonoContacto?: string | null;
}
```

## Ejemplos Axios
Crear Natural:

```ts
await axios.post("/api/Proveedores", {
  TipoProveedor: "Natural",
  Nombre: "UN Natural",
  NIT: "UN-123",
  Correo: "un@natural.com",
  Telefono: "3001112222",
  Direccion: "Calle UN",
  Contacto: "UN Contact",
  NumeroIdentificacion: "123",
  TipoIdentificacion: "CC",
  CorreoContacto: "un.c@natural.com",
  TelefonoContacto: "3009998888"
});
```

Crear Jurídico (sin `TipoIdentificacion` → usa `"NIT"`):

```ts
await axios.post("/api/Proveedores", {
  TipoProveedor: "Juridico",
  Nombre: "UN Juridico",
  NIT: "UN-124",
  Correo: "un@juridico.com",
  Telefono: "6011112222",
  Direccion: "Av UN",
  Contacto: "UN Legal"
});
```

## Compras: contratos y validaciones
Endpoint: `POST /api/Compras`

```json
{
  "ProveedorId": 16,
  "UsuarioId": 16,
  "NumeroFactura": "F-001",
  "FechaFactura": "2026-03-05",
  "MetodoPago": "Efectivo",
  "IVA": 500,
  "Descuento": 0,
  "Detalles": [
    { "ProductoId": 1, "Cantidad": 2, "CantidadVentas": 2, "CantidadInsumos": 0, "PrecioUnitario": 3244 }
  ]
}
```

Validaciones backend:
- ProveedorId debe existir y estar activo
- UsuarioId debe existir
- Cada ProductoId debe existir

UI sugerida:
- Selector de Proveedor: filtrar por `Estado=true`
- Mostrar errores `400` con mensajes devueltos por API

## Manejo de errores (API)
- `400 "TipoProveedor debe ser 'Natural' o 'Juridico'"`
- `400 "Ya existe un proveedor con ese NIT"`
- `400 "El proveedor no existe"` / `400 "El proveedor está inactivo"`
- `400 "El usuario no existe"`
- `400 "El producto {id} no existe"`
- `DELETE` proveedor con compras: `fisico=false` → mostrar “desactivado”

## Migración del Front
- Reemplazar `POST /api/Proveedores/natural` y `POST /api/Proveedores/juridico` por `POST /api/Proveedores`
- Añadir selector `TipoProveedor` en el formulario
- Mantener validaciones de obligatorios y opcionales
- Ajustar manejo de errores según mensajes

## Swagger
- UI: http://localhost:5070/
- JSON: http://localhost:5070/swagger/v1/swagger.json

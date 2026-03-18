# Documentación Completa de la API - Barbería Express

Guía de referencia de todos los endpoints disponibles en la API para la integración con el frontend.

## 🚀 Información General
- **Base URL:** Depende del entorno (ej. `https://localhost:7050/api/`).
- **Documentación Interactiva:** Accede a `https://localhost:7050/` para ver el **Swagger UI**.
- **Formato de Fechas:** `YYYY-MM-DD` | **Formato de Horas:** `HH:mm:ss`.
- **Borrado Lógico:** Las peticiones `DELETE` suelen cambiar el campo `Estado` a `false` en lugar de borrar el registro (en entidades principales).

---

## 🔎 Búsqueda y 📄 Paginación
- Todos los listados principales aceptan parámetros de consulta:
  - `page`: número de página (por defecto `1`)
  - `pageSize`: tamaño de página (por defecto `5`). Valores menores a 1 se normalizan a `5`.
  - `q`: término de búsqueda de texto libre (opcional)
- La respuesta de estos listados viene en un sobre con metadatos:
  - `items`: resultados de la página
  - `totalCount`: total de registros luego de aplicar filtros/búsqueda
  - `page`, `pageSize`, `totalPages`
- Orden de procesamiento: filtros específicos (IDs, fechas) → búsqueda `q` → orden → paginación.
- Ejemplo de respuesta:

```json
{
  "items": [ /* registros */ ],
  "totalCount": 134,
  "page": 2,
  "pageSize": 5,
  "totalPages": 27
}
```

### Campos incluidos en la búsqueda (`q`) por módulo
- Productos: `Nombre`, `Descripcion`, `Marca`, `Categoria.Nombre`
- Categorías: `Nombre`
- Servicios: `Nombre`, `Descripcion`
- Paquetes: `Nombre`, `Descripcion`
- Usuarios: `Nombre`, `Apellido`, `Documento`, `Correo`, además en Cliente/Barbero: `Telefono`, `Direccion`, `Barrio`, y en Barbero `Especialidad`
- Clientes: `Usuario.Nombre`, `Usuario.Apellido`, `Usuario.Documento`, `Usuario.Correo`, `Telefono`, `Direccion`, `Barrio`
- Barberos: `Usuario.Nombre`, `Usuario.Apellido`, `Usuario.Documento`, `Usuario.Correo`, `Telefono`, `Direccion`, `Barrio`, `Especialidad`
- Proveedores: `Nombre`, `NIT`, `Contacto`, `Correo`, `Telefono`
- Roles: `Nombre`, `Descripcion`
- Módulos: `Nombre`
- Roles-Módulos: `Rol.Nombre`, `Modulo.Nombre` (y en `GetByRole`, `Modulo.Nombre`)
- Ventas: `Estado`, `MetodoPago`, nombres de `Cliente`, `Usuario` y `Barbero`
- Compras: `NumeroFactura`, `MetodoPago`, `Proveedor.Nombre`, `Proveedor.NIT`, nombres de `Usuario`
- DetalleVentas: `Producto.Nombre`, `Servicio.Nombre`, `Paquete.Nombre`
- DetalleCompras: `Producto.Nombre`
- EntregasInsumos: `Estado`, `Usuario.Nombre`, `Barbero.Usuario.Nombre/Apellido`
- Devoluciones: `MotivoCategoria`, `MotivoDetalle`, `Observaciones`, `Estado`, `Producto.Nombre`, `Usuario.Nombre`, nombres de `Cliente` y `Barbero`
- Citas (frontend): `Estado`, nombres de `Cliente` y `Barbero`, `Servicio.Nombre`, `Paquete.Nombre`
- Agendamientos: `Estado`, nombres de `Cliente` y `Barbero`, `Servicio.Nombre`, `Paquete.Nombre`
- HorariosBarberos: nombres de `Barbero` (y en `GetByBarbero`, filtro por ese barbero y búsqueda sobre su nombre)

### Ejemplos
- Productos: `/api/Productos?q=shampoo&page=1&pageSize=5`
- Ventas por cliente: `/api/Ventas?q=juan&page=2`
- Devoluciones por motivo o producto: `/api/Devoluciones?q=defecto`
- Roles por nombre: `/api/Roles?q=admin`
- Citas por barbero o servicio: `/api/Citas?q=barba`

> Nota: El tamaño de página por defecto es 5; puedes ajustarlo enviando `pageSize`, manteniendo el mismo formato de respuesta.

---

## 📅 Agendamientos y Citas (`/api/Agendamientos`)
| Método | Ruta | Descripción |
| :--- | :--- | :--- |
| **GET** | `/api/Agendamientos` | Lista citas activas (`Estado: true`). Incluye servicios y paquetes. |
| **GET** | `/api/Agendamientos/{id}` | Obtiene una cita específica. |
| **GET** | `/api/Agendamientos/cliente/{id}` | Citas de un cliente. |
| **GET** | `/api/Agendamientos/barbero/{id}` | Citas de un barbero. |
| **GET** | `/api/Agendamientos/fecha/{f}` | Citas para una fecha específica. |
| **GET** | `/api/Agendamientos/disponibilidad/{bId}/{f}` | Retorna disponibilidad del barbero en la fecha. |
| **POST** | `/api/Agendamientos` | Crea cita. Requiere IDs (cliente, barbero) Y servicioId O paqueteId (no ambos). |
| **PUT** | `/api/Agendamientos/{id}` | Actualización completa de la cita. |
| **PUT** | `/api/Agendamientos/{id}/estado` | Cambia estado (enviar texto: `"Confirmada"`, `"Completada"`, etc.). |
| **DELETE** | `/api/Agendamientos/{id}` | Cancela la cita (borrado lógico). |

**Nota importante:** Ahora los agendamientos soportan tanto servicios individuales como paquetes. Use `AgendamientoInput` para crear citas.

---

## 💰 Ventas (`/api/Ventas`, `/api/DetallesVenta`)
| Método | Ruta | Descripción |
| :--- | :--- | :--- |
| **GET** | `/api/Ventas` | Historial de todas las ventas activas. |
| **POST** | `/api/Ventas` | Crea venta con múltiples detalles (calcula total y resta stock). |
| **DELETE** | `/api/Ventas/{id}` | Anula venta y repone stock de productos. |
| **GET** | `/api/DetallesVenta/venta/{id}` | Obtiene los productos/servicios específicos de una venta. |

---

## 📦 Inventario y Compras (`/api/Productos`, `/api/Compras`, `/api/CategoriasProductos`)
| Método | Ruta | Descripción |
| :--- | :--- | :--- |
| **GET** | `/api/Productos` | Lista productos con sus stocks (`Ventas` e `Insumos`). |
| **GET** | `/api/Productos/stock-bajo` | Productos con stock insuficiente. |
| **POST** | `/api/Compras` | Registrar compra a proveedor (aumenta stock). |
| **GET** | `/api/CategoriasProductos` | Lista categorías para clasificar productos. |

---

## 👥 Clientes, Barberos y Proveedores
Gestión de los actores principales del sistema.
| Controlador | Ruta Base | Funciones Extra |
| :--- | :--- | :--- |
| **Clientes** | `/api/Clientes` | Buscar por documento o término general. |
| **Barberos** | `/api/Barberos` | Incluye horarios laborales en el GET. |
| **Proveedores**| `/api/Proveedores` | Lista de proveedores para el módulo de compras. |

---

## 💇 Servicios y Paquetes
| Método | Ruta | Descripción |
| :--- | :--- | :--- |
| **GET** | `/api/Servicios` | Catálogo de servicios individuales (Corte, Barba, etc.). |
| **GET** | `/api/Paquetes` | Combos que incluyen varios servicios (ej. "Paquete Completo"). |
| **POST** | `/api/Paquetes` | Crea un paquete. Las relaciones se manejan vía `/api/DetallePaquetes`. |
| **GET** | `/api/DetallePaquetes` | Lista todos los detalles de paquetes. |
| **GET** | `/api/DetallePaquetes/paquete/{id}` | Obtiene detalles de un paquete específico. |
| **POST** | `/api/DetallePaquetes` | Agrega servicios/productos a un paquete. |

---

## � Usuarios, Roles y Seguridad
| Método | Ruta | Descripción |
| :--- | :--- | :--- |
| **GET** | `/api/Usuarios` | Lista de cuentas de acceso al sistema. |
| **GET** | `/api/Usuarios/correo/{c}` | Buscar usuario por email (útil para login). |
| **GET** | `/api/Roles` | Lista de perfiles (Admin, Barbero, etc.). |
| **GET** | `/api/Modulos` | Lista de módulos del sistema. |
| **POST** | `/api/RolesModulos` | Asignar permisos (Módulo X para Rol Y). |

---

## �️ Otros Endpoints
| Método | Ruta | Descripción |
| :--- | :--- | :--- |
| **POST** | `/api/Upload` | Sube una imagen y retorna la URL (ej. `/assets/images/foto.jpg`). |
| **GET** | `/api/Horarios` | Configura los bloques de tiempo laborables de los barberos. |
| **POST** | `/api/Devoluciones` | Registro de devoluciones de productos/servicios. |
| **POST** | `/api/EntregasInsumos`| Registro de insumos entregados a los barberos para su uso cotidiano. |

---

## ⚠️ Consideraciones Especiales
1. **Relaciones en POST/PUT:** No es necesario enviar el objeto completo (ej. `Barbero: { ... }`). Solo envía el ID numérico (`barberoId: 5`).
2. **Errores:** El sistema retorna `400 Bad Request` con un mensaje JSON si falta stock, hay choque de citas o faltan campos.
3. **Seguridad:** Los endpoints de Usuarios almacenan contraseñas. Asegúrate de manejarlas con cuidado en el front.

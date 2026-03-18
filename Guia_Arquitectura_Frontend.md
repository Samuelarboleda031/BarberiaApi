# Guía de Arquitectura: Usuarios, Clientes y Barberos

Esta guía detalla la relación entre las entidades del sistema para facilitar las modificaciones en el Frontend.

## 1. Estructura de Datos (Relación 1:1)

El sistema utiliza un modelo de **Entidad Base + Perfil**. El `Usuario` es la cuenta principal, y el `Cliente` o `Barbero` son perfiles específicos que extienden esa cuenta.

### ¿Dónde vive cada dato?

| Campo | Entidad `Usuario` | Entidad `Cliente` | Entidad `Barbero` |
| :--- | :---: | :---: | :---: |
| **Identidad** (Nombre, Apellido, Documento) | ✅ Sí | ❌ No | ❌ No |
| **Credenciales** (Correo, Contraseña, Rol) | ✅ Sí | ❌ No | ❌ No |
| **Foto de Perfil** | ✅ Sí | ❌ No | ❌ No |
| **Datos de Contacto** (Teléfono) | ❌ No | ✅ Sí | ✅ Sí |
| **Ubicación** (Dirección, Barrio) | ❌ No | ✅ Sí | ✅ Sí |
| **Fisiológicos** (Fecha Nacimiento) | ❌ No | ✅ Sí | ✅ Sí |
| **Laborales** (Especialidad, Contratación) | ❌ No | ❌ No | ✅ Sí |

---

## 2. Los DTOs (Lo que el Frontend recibe)

Para facilitar el trabajo en el Frontend, el API devuelve objetos "aplanados" que combinan los datos del Usuario y el Perfil.

### Ejemplo de respuesta: `BarberoDto` o `ClienteDto`
```json
{
  "id": 1,
  "usuarioId": 10,
  "nombre": "Juan",      // Viene de Usuario
  "apellido": "Pérez",   // Viene de Usuario
  "documento": "123456", // Viene de Usuario
  "correo": "juan@mail.com",
  "telefono": "300123",  // Viene de Perfil (Barbero/Cliente)
  "especialidad": "Corte Moderno", // Solo Barberos
  "direccion": "Calle 10", // Solo Clientes/Barberos
  "estado": true,
  "usuario": { ... objeto usuario completo ... }
}
```

---

## 3. Lógica de Endpoints para el Frontend

### A. Crear un nuevo Usuario/Perfil
**Recomendación:** Usar `POST /api/Usuarios`.
*   Si envías un `RolId: 3` (Cliente), la API crea automáticamente el registro en `Usuarios` y en `Clientes`.
*   Esto evita tener que hacer dos llamados al API.

### B. Actualizar Información
**Recomendación:** Usar los controladores específicos (`/api/Clientes/{id}` o `/api/Barberos/{id}`).
*   Al enviar un `PUT` a estos controladores, la API se encarga de repartir los datos: actualiza el Nombre/Correo en la tabla `Usuarios` y el Teléfono/Especialidad en la tabla `Perfil`.

### C. Manejo de Estados (Activar/Desactivar)
Todos los perfiles ahora usan `POST /api/{Controlador}/{id}/estado` con un body booleano:
```json
{ "estado": true }
```

---

## 4. Tips para el Frontend (React/Vite)

1.  **Formularios Únicos:** Aunque en la base de datos están separados, en el Frontend puedes mostrar un solo formulario. Al enviar los datos, la API los procesa correctamente.
2.  **Validaciones de Roles:**
    *   `Rol 1`: Administrador
    *   `Rol 2`: Barbero
    *   `Rol 3`: Cliente
3.  **Acceso a Nombres:** Siempre utiliza las propiedades de primer nivel del DTO (`item.nombre`) en lugar de navegar a `item.usuario.nombre`, ya que los DTOs que corregimos hoy ya traen la información mapeada para ti.

## 5. Resumen de Flujo de Datos
1.  **Frontend** envía JSON único.
2.  **Controller** recibe el `Input`.
3.  **Controller** busca al `Usuario` por su `UsuarioId`.
4.  **Controller** actualiza `Usuario` (Nombre, Correo).
5.  **Controller** actualiza `Perfil` (Teléfono, Especialidad, Dirección).
6.  **Base de Datos** guarda los cambios en ambas tablas mediante una transacción.

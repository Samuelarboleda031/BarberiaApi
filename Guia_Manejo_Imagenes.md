# Guía de Manejo de Imágenes

Esta guía detalla el flujo estandarizado para la carga, almacenamiento y visualización de imágenes en el sistema.

## 1. Flujo de Carga (Frontend -> Backend)

Cuando un usuario selecciona una imagen en un formulario (ej. Productos, Usuarios):

1.  **Vista Previa Inmediata:** El frontend utiliza `FileReader` (o `URL.createObjectURL`) para mostrar la imagen localmente.
2.  **Validación en Frontend:** 
    *   **Formato:** jpg, jpeg, png, gif, webp.
    *   **Tamaño Máximo:** 5MB.
3.  **Envío al Servidor:** Se envía mediante un `FormData` al endpoint POST `/api/upload`.

## 2. Procesamiento en el Servidor (Backend)

El `UploadController` procesa el archivo de la siguiente manera:

1.  **Validación de Extensión:** Verifica que el archivo sea una imagen válida.
2.  **Nombre Único:** Se genera un GUID para el nombre del archivo (ej. `a1b2c3d4-e5f6-g7h8.jpg`) para evitar conflictos.
3.  **Almacenamiento Físico:** Se guarda en la carpeta `wwwroot/assets/images/`.
4.  **Respuesta:** El servidor devuelve la **URL relativa** (ej. `/assets/images/a1b2c3d4-e5f6-g7h8.jpg`).

## 3. Persistencia en Base de Datos

La URL relativa retornada por el servidor es la que debe guardarse en el campo correspondiente de la entidad:
*   `Producto`: `ImagenProduc`
*   `Usuario`: `FotoPerfil`

### Reglas de Validación en Controladores:
Al crear o actualizar una entidad, se debe validar que la URL de imagen proporcionada:
1.  Sea una URL válida.
2.  Si es relativa, debe comenzar con `/` (ej. `/assets/images/producto.jpg`).
3.  Si es absoluta, debe iniciar con `http://` o `https://`.

## 4. Visualización (Tablas y Detalles)

El sistema debe manejar dos estados en el renderizado:

1.  **Con Imagen:** Renderiza un componente `<img>` con estilos `object-cover` para mantener el encuadre.
2.  **Sin Imagen:** Si el campo es nulo o vacío, muestra un **Placeholder** (icono `Package` de Lucide) con un fondo gris neutro.

```jsx
// Ejemplo conceptual en React
{item.imagen ? (
  <img src={item.imagen} className="h-10 w-10 object-cover" />
) : (
  <div className="h-10 w-10 bg-gray-200 flex items-center justify-center">
    <Package size={20} className="text-gray-400" />
  </div>
)}
```

// NOTA ARQUITECTÓNICA (BE-A7):
// IPhotoService debería vivir aquí (Application.Interfaces) según Clean Architecture,
// pero este proyecto tiene Application → Infrastructure como dependencia (invertida).
// Moverla aquí causaría referencia circular: Infrastructure necesitaría referenciar Application.
//
// Tarea pendiente: Invertir la dependencia moviendo Application para que NO referencie Infrastructure.
// Por ahora, la interfaz canónica sigue en Infrastructure.Services.IPhotoService.
// Ver PLAN_BACKEND.md → BE-A8 para la decisión completa sobre arquitectura.

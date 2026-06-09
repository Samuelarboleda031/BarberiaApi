namespace BarberiaApi.Application.DTOs;

// DTOs para Roles
public class RoleDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Estado { get; set; }
    public int UsuariosAsignados { get; set; }
    public List<int> Modulos { get; set; } = new();
}

public class PermisoModuloInput
{
    public bool PuedeVer { get; set; }
    public bool PuedeCrear { get; set; }
    public bool PuedeEditar { get; set; }
    public bool PuedeEliminar { get; set; }
}

public class CreateRoleInput
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public List<int> Modulos { get; set; } = new();
    public Dictionary<string, PermisoModuloInput> Permisos { get; set; } = new();
}

public class RoleInput
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Estado { get; set; } = true;
    public List<int> Modulos { get; set; } = new();
    public Dictionary<string, PermisoModuloInput> Permisos { get; set; } = new();
}

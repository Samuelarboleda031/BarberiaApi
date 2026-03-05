using System;
using System.Collections.Generic;

namespace BarberiaApi.Models;

public partial class Role
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public bool? Estado { get; set; }

    public virtual ICollection<RolesModulos> RolesModulos { get; set; } = new List<RolesModulos>();

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}

using System;
using System.Collections.Generic;

namespace BarberiaApi.Domain.Entities;

public partial class RolesModulos
{
    public int Id { get; set; }

    public int RolId { get; set; }

    public int ModuloId { get; set; }

    public bool? PuedeVer { get; set; }

    public bool? PuedeCrear { get; set; }

    public bool? PuedeEditar { get; set; }

    public bool? PuedeEliminar { get; set; }

    public virtual Modulos? Modulo { get; set; }

    public virtual Role? Rol { get; set; }
}

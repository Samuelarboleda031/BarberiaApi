using System;
using System.Collections.Generic;

namespace BarberiaApi.Domain.Entities;

public partial class Modulos
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public bool? Estado { get; set; }

    public virtual ICollection<RolesModulos> RolesModulos { get; set; } = new List<RolesModulos>();
}

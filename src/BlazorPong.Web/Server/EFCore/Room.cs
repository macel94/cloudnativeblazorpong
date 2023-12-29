using System;
using System.Collections.Generic;

namespace BlazorPong.Web.Server.EFCore;

public partial class Room
{
    public Guid Id { get; set; }

    public string? ServerName { get; set; }

    public virtual ICollection<Client> Clients { get; set; } = new List<Client>();
}

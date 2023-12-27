using System;
using System.Collections.Generic;

namespace BlazorPong.Web.Server.EFCore;

public partial class Client
{
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }

    public virtual Room Room { get; set; } = null!;
}

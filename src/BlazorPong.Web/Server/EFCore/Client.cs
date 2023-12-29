using System;
using System.Collections.Generic;

namespace BlazorPong.Web.Server.EFCore;

public partial class Client
{
    public string Username { get; set; } = null!;

    public Guid RoomId { get; set; }

    public byte? Role { get; set; }

    public string ConnectionId { get; set; } = null!;

    public virtual Room Room { get; set; } = null!;
}

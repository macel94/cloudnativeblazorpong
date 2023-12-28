using System;
using System.Collections.Generic;

namespace BlazorPong.Web.Server.src.BlazorPong.Web.Server.EFCore.Models;

public partial class Client
{
    public string Id { get; set; } = null!;

    public Guid RoomId { get; set; }

    public byte? Role { get; set; }

    public virtual Room Room { get; set; } = null!;
}

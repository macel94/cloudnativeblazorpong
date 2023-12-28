﻿using System;
using System.Collections.Generic;
using BlazorPong.Web.Shared;

namespace BlazorPong.Web.Server.EFCore;

public partial class Client
{
    public string Id { get; set; }

    public Guid RoomId { get; set; }
    public Role Role { get; set; }

    public virtual Room Room { get; set; } = null!;
}

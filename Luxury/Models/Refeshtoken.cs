using System;
using System.Collections.Generic;

namespace Luxury.Models;

public partial class Refeshtoken
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? Token { get; set; }

    public DateTime? IssuedAt { get; set; }

    public DateTime? ExpiredAt { get; set; }
}

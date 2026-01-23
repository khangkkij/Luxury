using System;
using System.Collections.Generic;

namespace Luxury.Models;

public partial class Flashsale
{
    public int Flashsaleid { get; set; }

    public DateTime? NgayBatDau { get; set; }

    public DateTime? NgayKetThuc { get; set; }

    public bool? TrangThai { get; set; }

    public virtual ICollection<ChitietFlashsale> ChitietFlashsales { get; set; } = new List<ChitietFlashsale>();
}

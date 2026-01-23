using System;
using System.Collections.Generic;

namespace Luxury.Models;

public partial class ChitietFlashsale
{
    public int Flashsaleid { get; set; }

    public int MaBienThePhong { get; set; }

    public int? PhanTramGiam { get; set; }

    public virtual Flashsale Flashsale { get; set; } = null!;

    public virtual LoaiphongBienthe MaBienThePhongNavigation { get; set; } = null!;
}

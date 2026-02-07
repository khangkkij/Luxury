using System.ComponentModel.DataAnnotations.Schema;

namespace Luxury.Models
{
    public class TienichLoaiphong
    {
        public int MaTi { get; set; }
        public int MaBienThePhong { get; set; }

        public Tienich Tienich { get; set; }
        public LoaiphongBienthe LoaiphongBienthe { get; set; }

    }

}

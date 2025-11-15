namespace EcommerceMVC.ViewModels
{
    public class CartItem
    {
        public int MaHH { get; set; }
        public string Hinh { get; set; }
        public string TenHH { get; set; }
        public double DonGia { get; set; }
        public int SoLuong { get; set; }
        public double ThanhTien
        {
            get { return DonGia * SoLuong; }
        }   
    }
}

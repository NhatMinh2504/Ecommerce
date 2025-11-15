using EcommerceMVC.Helpers;
using EcommerceMVC.ViewModels;

namespace EcommerceMVC.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;

        public VnPayService(IConfiguration config) => _config = config;

        string IVnPayService.CreatePaymentUrl(HttpContext context, VnPaymentRequestModel model)
        {
            var txnRef = (model.OrderId > 0 ? model.OrderId.ToString() : DateTimeOffset.Now.Ticks.ToString());

            var vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", _config["VnPay:Version"]);         // 2.1.0
            vnpay.AddRequestData("vnp_Command", _config["VnPay:Command"]);         // pay
            vnpay.AddRequestData("vnp_TmnCode", _config["VnPay:TmnCode"]);         // OVQXC7YE  <-- KHỚP APPSETTINGS
            vnpay.AddRequestData("vnp_Amount", ((long)(model.Amount * 100)).ToString()); // x100, số nguyên
            vnpay.AddRequestData("vnp_CreateDate", model.CreatedDate.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", _config["VnPay:CurrCode"]);        // VND
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(context));
            vnpay.AddRequestData("vnp_Locale", _config["VnPay:Locale"]);          // vn
            vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toán đơn hàng #{txnRef} - {model.FullName} - {model.OrderDescription}");
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", _config["VnPay:PaymentBackUrl"]);  // <-- GIỮ NGUYÊN TÊN BẠN ĐANG DÙNG
            vnpay.AddRequestData("vnp_TxnRef", txnRef);
            vnpay.AddRequestData("vnp_ExpireDate", model.CreatedDate.AddMinutes(15).ToString("yyyyMMddHHmmss"));

            var baseUrl = _config["VnPay:BaseUrl"];
            var hashSecret = _config["VnPay:HashSecret"];

            var paymentUrl = vnpay.CreateRequestUrl(baseUrl, hashSecret);

            // Gợi ý debug:
            // Console.WriteLine(paymentUrl);

            return paymentUrl;
        }

        VnPaymentResponseModel IVnPayService.PaymentExecute(IQueryCollection collections)
        {
            var vnpay = new VnPayLibrary();
            foreach (var (key, value) in collections)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                    vnpay.AddResponseData(key, value.ToString());
            }

            var vnp_SecureHash = collections.FirstOrDefault(p => p.Key == "vnp_SecureHash").Value;
            var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            var vnp_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");
            var vnp_TxnRef = vnpay.GetResponseData("vnp_TxnRef");
            var vnp_TransNo = vnpay.GetResponseData("vnp_TransactionNo");

            var hashSecret = _config["VnPay:HashSecret"];
            bool ok = vnpay.ValidateSignature(vnp_SecureHash, hashSecret);
            if (!ok)
            {
                return new VnPaymentResponseModel
                {
                    Success = false,
                    VnPayResponseCode = "97",
                    OrderDescription = "Invalid signature"
                };
            }

            return new VnPaymentResponseModel
            {
                Success = (vnp_ResponseCode == "00"),
                PaymentMethod = "VNPAY",
                OrderDescription = vnp_OrderInfo,
                OrderId = vnp_TxnRef,
                TransactionId = vnp_TransNo,
                Token = vnp_SecureHash,
                VnPayResponseCode = vnp_ResponseCode
            };
        }
    }
}

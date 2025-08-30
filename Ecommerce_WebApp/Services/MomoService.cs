using Ecommerce_WebApp.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json; 
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce_WebApp.Services
{
    public class MomoService : IMomoService
    {
        private readonly IConfiguration _config;

        public MomoService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<string> CreatePaymentUrlAsync(OrderHeader order, HttpContext httpContext)
        {
            var settings = _config.GetSection("MomoSettings");
            string endpoint = settings["Endpoint"];
            string partnerCode = settings["PartnerCode"];
            string accessKey = settings["AccessKey"];
            string secretKey = settings["SecretKey"];

            string orderInfo = $"Thanh toan don hang #{order.Id}"; // Mô tả đơn hàng
            string redirectUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/Order/PaymentCallBack";
            string ipnUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/Order/PaymentIPN"; 

            long amount = (long)order.OrderTotal; // MoMo yêu cầu kiểu long
            string orderId = Guid.NewGuid().ToString();
            string requestId = Guid.NewGuid().ToString();
            string requestType = "captureWallet"; // Mặc định là captureWallet cho test
            string extraData = order.Id.ToString(); // QUAN TRỌNG: Dùng để lưu Id đơn hàng của bạn

            // === BẮT ĐẦU PHẦN QUAN TRỌNG NHẤT: TẠO CHỮ KÝ ===
            // Chuỗi dữ liệu thô (raw hash string) PHẢI theo đúng thứ tự Alphabet
            string rawHash = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";

            MoMoSecurity crypto = new MoMoSecurity();
            string signature = crypto.signSHA256(rawHash, secretKey);

            // === TẠO REQUEST BODY (PAYLOAD) ĐỂ GỬI ĐI ===
            var requestBody = new
            {
                partnerCode,
                requestId,
                amount,
                orderId,
                orderInfo,
                redirectUrl, 
                ipnUrl,
                lang = "vi",
                extraData,
                requestType,
                signature
            };

            // Chuyển đối tượng thành chuỗi JSON
            string jsonRequest = JsonConvert.SerializeObject(requestBody);

            using (var client = new HttpClient())
            {
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(endpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    dynamic jmessage = JsonConvert.DeserializeObject(responseContent);
                    // Lấy ra payUrl từ kết quả trả về của MoMo
                    return jmessage.payUrl;
                }
            }

            return null; // Trả về null nếu có lỗi
        }
    }
}
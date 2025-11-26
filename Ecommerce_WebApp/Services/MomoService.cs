using Ecommerce_WebApp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<MomoService> _logger;

        public MomoService(IConfiguration config, ILogger<MomoService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<string> CreatePaymentUrlAsync(OrderHeader order, HttpContext httpContext)
        {
            try
            {
                var settings = _config.GetSection("MomoSettings");
                string endpoint = settings["Endpoint"];
                string partnerCode = settings["PartnerCode"];
                string accessKey = settings["AccessKey"];
                string secretKey = settings["SecretKey"];

                // Kiểm tra cấu hình
                if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(partnerCode) ||
                    string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
                {
                    _logger.LogError("MoMo configuration is missing or incomplete");
                    return null;
                }

                string orderInfo = $"Thanh toan don hang #{order.Id}"; // Mô tả đơn hàng
                string redirectUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/Order/PaymentCallBack";
                string ipnUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/Order/PaymentIPN"; 

                long amount = (long)Math.Round(order.OrderTotal); // Làm tròn để tránh mất số lẻ
                string orderId = Guid.NewGuid().ToString();
                string requestId = Guid.NewGuid().ToString();
                string requestType = "captureWallet"; // Mặc định là captureWallet cho test
                string extraData = order.Id.ToString(); 

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
                _logger.LogInformation($"Sending MoMo request for Order #{order.Id}: {jsonRequest}");

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30); // Set timeout
                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(endpoint, content);

                    string responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"MoMo response: {responseContent}");

                    if (response.IsSuccessStatusCode)
                    {
                        dynamic jmessage = JsonConvert.DeserializeObject(responseContent);
                        
                        // Kiểm tra resultCode từ MoMo
                        if (jmessage.resultCode == 0)
                        {
                            return jmessage.payUrl;
                        }
                        else
                        {
                            _logger.LogError($"MoMo Error: {jmessage.resultCode} - {jmessage.message}");
                            return null;
                        }
                    }
                    else
                    {
                        _logger.LogError($"MoMo HTTP Error {response.StatusCode}: {responseContent}");
                        return null;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error when calling MoMo API");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CreatePaymentUrlAsync");
                return null;
            }
        }
    }
}
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NAT.BG.ExchangeFlex
{
    public class Exchange
    {
        // URL веб-сервиса Exchange
        private static readonly string serviceUrl = "http://172.16.143.205/SPM3.0Service/webservice/service.asmx";

        // Метод для получения состояния счета по IBAN
        public async Task<AccountStatus> GetAccountStatusAsync(string ibanAccount)
        {
            using (HttpClient client = new HttpClient())
            {
                // SOAP-запрос для получения состояния счета
                var requestXml = $@"
                    <soap:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>
                        <soap:Body>
                            <GetAccountStatus xmlns='http://tempuri.org/'>
                                <ibanAccount>{ibanAccount}</ibanAccount>
                            </GetAccountStatus>
                        </soap:Body>
                    </soap:Envelope>";

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, serviceUrl);
                request.Content = new StringContent(requestXml, System.Text.Encoding.UTF8, "text/xml");

                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return ParseAccountStatus(responseContent);
                }
                else
                {
                    throw new Exception("Error fetching account status from Flexcube.");
                }
            }
        }

        // Метод для разбора ответа и извлечения данных о состоянии счета
        private AccountStatus ParseAccountStatus(string responseContent)
        {
            var doc = XDocument.Parse(responseContent);
            var ns = doc.Root.Name.Namespace;

            var availableBalance = doc.Root.Descendants(ns + "AvailableBalance").First().Value;
            var accountActivity = doc.Root.Descendants(ns + "AccountActivity").First().Value;
            var isIinValid = bool.Parse(doc.Root.Descendants(ns + "IsIinValid").First().Value);
            var isIbanValid = bool.Parse(doc.Root.Descendants(ns + "IsIbanValid").First().Value);

            return new AccountStatus
            {
                AvailableBalance = decimal.Parse(availableBalance ?? "0"),
                AccountActivity = accountActivity ?? string.Empty,
                IsIinValid = isIinValid,
                IsIbanValid = isIbanValid
            };
        }

        // Метод для отправки платежного документа в Flexcube и получения статуса
        public async Task<string> SetPaymentAsync(string xmlBody)
        {
            using (HttpClient client = new HttpClient())
            {
                // SOAP-запрос для отправки платежного документа
                var requestXml = $@"
                    <soap:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>
                        <soap:Body>
                            <SetPayment xmlns='http://tempuri.org/'>
                                <xmlBody>{xmlBody}</xmlBody>
                            </SetPayment>
                        </soap:Body>
                    </soap:Envelope>";

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, serviceUrl);
                request.Content = new StringContent(requestXml, System.Text.Encoding.UTF8, "text/xml");

                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return ParsePaymentStatus(responseContent);
                }
                else
                {
                    throw new Exception("Error sending payment to Flexcube.");
                }
            }
        }

        // Метод для разбора ответа и извлечения статуса платежа
        private string ParsePaymentStatus(string responseContent)
        {
            var doc = XDocument.Parse(responseContent);
            var ns = doc.Root.Name.Namespace;

            var status = doc.Root.Descendants(ns + "Status").First().Value;

            return status;
        }
    }

    // Класс для хранения информации о состоянии счета
    public class AccountStatus
    {
        public decimal AvailableBalance { get; set; }
        public string AccountActivity { get; set; } = string.Empty; // Устанавливаем значение по умолчанию
        public bool IsIinValid { get; set; }
        public bool IsIbanValid { get; set; }
    }

}

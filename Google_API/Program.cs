using System;
using System.Net;
using HtmlAgilityPack;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;

using System.Text;
using System.Threading.Tasks;

namespace Google_API
{
    class SendData
    {
        private string _baseUrl;
        private Dictionary<string, string> _field;
        public Dictionary<string, string[]> _checkbox;
        private WebClient _client;
        private Uri _uri;
        public SendData(string formUrl)
        {
            if (string.IsNullOrEmpty(formUrl))
                throw new ArgumentNullException(nameof(formUrl));

            _baseUrl = formUrl;

            _field = new Dictionary<string, string>();
            _checkbox = new Dictionary<string, string[]>();
            _client = new WebClient();
        }

        public void SetFieldValues(Dictionary<string, string> data)
        {
            if(data == null)
                throw new ArgumentNullException(nameof(data));
            
            if(data.Keys.Any(value=>string.IsNullOrWhiteSpace(value)))
                throw new ArgumentNullException(nameof(data), "empty keys");

            var fieldsWithData = data.Where(param => !string.IsNullOrWhiteSpace(param.Value));

            foreach (var param in fieldsWithData)
            {
                _field[param.Key] = param.Value;
            }
        }

        public void SetCheckboxValues(string key, params string[] values)
        {
            if(string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            var valuesWithData = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            _checkbox[key] = valuesWithData;
        }

        public async Task<string> SubmitAsync()
        {
            if (!_field.Any() && !_checkbox.Any())
            {
                throw new InvalidOperationException("No data has been set to submit");
            }

            NameValueCollection nameValue = new NameValueCollection();


            foreach (var temp in _field)
            {
                nameValue.Add(temp.Key, temp.Value);
            }

            foreach (var temp in _checkbox)
            {
                for (int i = 0; i < temp.Value.Length; i++)
                {
                    _baseUrl += temp.Key + "=" + temp.Value[i] + "&";
                }
                _baseUrl.TrimEnd();
            }
            
            _uri = new Uri(_baseUrl);
            byte[] response = await _client.UploadValuesTaskAsync(_uri, "POST", nameValue);
            string result = Encoding.UTF8.GetString(response);
            return result;
        }
    }

    class Multiple_Form
    {
        public List<string> choises;

        public string title;
        public string entry;

        public Multiple_Form()
        {
            choises = new List<string>();
        }
    }

    class Single_Form
    {
        public string title;
        public string entry;
    }

    class Radio_Form
    {
        public List<string> choises;
        public string title;
        public string entry;

        public Radio_Form()
        {
            choises = new List<string>();
        }
    }

    class Form
    {
        public List<Multiple_Form> Multiple;
        public List<Single_Form> Single;
        public List<Radio_Form> Radio;

        public Form()
        {
            Multiple = new List<Multiple_Form>();
            Single = new List<Single_Form>();
            Radio = new List<Radio_Form>();
        }
    }


    class GetData
    {
        private string rootUrl;

        public GetData(string rootUrl)
        {
            this.rootUrl = rootUrl;
        }
        public static string GetUrl(string address)
        {
            WebClient webClient = new WebClient();
            webClient.Headers[HttpRequestHeader.Accept] = "text/html, */*";
            webClient.Headers[HttpRequestHeader.AcceptLanguage] = "ru-RU";
            webClient.Headers[HttpRequestHeader.UserAgent] =
                "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)";
            webClient.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded,  charset=utf-8";
            return webClient.DownloadString(address);
        }

        public void GetDataFromServer()
        {
            HtmlDocument htmlSnippet = new HtmlDocument();
            string result;

            result = GetUrl(rootUrl);
            htmlSnippet.LoadHtml(result);
            HtmlNodeCollection nodes =
                htmlSnippet.DocumentNode.SelectNodes(
                    @"//div[@class='freebirdFormviewerViewNumberedItemContainer']");

            Form forms = new Form();
            foreach (HtmlNode node in nodes)
            {
                string title = node
                    .SelectSingleNode(
                        @".//div[@class='freebirdFormviewerViewItemsItemItemTitle exportItemTitle freebirdCustomFont']")
                    .InnerText;

                var multiple_input =
                    node.SelectNodes(
                        ".//div[@class='freebirdFormviewerViewItemsCheckboxOptionContainer']");

                var single_input =
                    node.SelectSingleNode(
                        ".//input");

                var radio_input =
                    node.SelectNodes(
                        @".//div[@class='freebirdFormviewerViewItemsRadioOptionContainer']");
                if (multiple_input != null)
                {
                    Multiple_Form record = new Multiple_Form();
                    record.title = title;
                    record.entry = node.SelectSingleNode(@".//input").Attributes["name"].Value.Replace("_sentinel", "");
                    foreach (HtmlNode node2 in multiple_input)
                    {
                        string choise = node2.SelectSingleNode(@".//span").InnerHtml;
                        record.choises.Add(choise);
                    }

                    forms.Multiple.Add(record);
                }
                else if (radio_input != null)
                {
                    Radio_Form record = new Radio_Form();
                    record.title = title;
                    record.entry = node.SelectSingleNode(@".//input").Attributes["name"].Value;
                    foreach (HtmlNode node3 in radio_input)
                    {
                        string choise = node3.SelectSingleNode(@".//span").InnerHtml;
                        record.choises.Add(choise);
                    }

                    forms.Radio.Add(record);
                }
                else if (single_input != null)
                {
                    Single_Form record = new Single_Form();
                    record.title = title;
                    record.entry = single_input.Attributes["name"].Value;
                    forms.Single.Add(record);
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string rootUrl =
                "https://docs.google.com/forms/d/e/1FAIpQLSfwgTnNsb7SvCmrkskJZINvhuGY861iNfdbMZ_1UcNylORT6A/viewform";

            rootUrl=rootUrl.Replace("viewform", "formResponse?");
            
            GetData get =
                new GetData(rootUrl);
            get.GetDataFromServer();
            
            SendData send = new SendData(rootUrl);
            var fields = new Dictionary<string, string>
            {
                { "entry.2022710925", "My text" },
                { "entry.1944233747", "Other text"},
            };
            send.SetFieldValues(fields);
            send.SetCheckboxValues("entry.234944739","Варіант 1");
            send.SetCheckboxValues("entry.2012312638","var1", "var2"); 
            send.SubmitAsync().GetAwaiter();
        }
    }
}
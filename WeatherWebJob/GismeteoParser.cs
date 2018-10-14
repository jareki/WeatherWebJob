using HtmlAgilityPack;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WeatherLib;

namespace GisWebJob
{
    class GismeteoParser
    {
        static readonly Uri address = new Uri("http://m.gismeteo.ru/weather/4368/weekly/");
        public List<WeatherRecord> WeatherList;

        public GismeteoParser()
        {
            WeatherList = new List<WeatherRecord>();
            

        }

        public void LoadData()
        {
            using (var client = new HttpClient { BaseAddress = address })
            {
                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(client.GetStringAsync("").Result);
                var divs = html.DocumentNode.SelectNodes("//div[@class='detail__time']");
                int day = 0;
                foreach (var div in divs)
                {
                    var record = new WeatherRecord();
                    record.ForecastDay = day++;
                    record.Portal = "Gismeteo";

                    record.PartitionKey = DateTime.Now.ToString("yy-MM-dd");
                    record.RowKey = $"G{record.PartitionKey}.{record.ForecastDay}";

                    var td1 = div.SelectSingleNode(".//td[@class='weather-minmax__temp']");
                    var matches = Regex.Matches(td1.InnerText, "[\\+\\-]?[0-9]+");
                    
                    record.Tmin = int.Parse(matches[0].Value);
                    if (matches.Count==1)
                        record.Tmax = int.Parse(matches[0].Value);
                    if (matches.Count==2)
                        record.Tmax = int.Parse(matches[1].Value);

                    var td2 = div.SelectSingleNode(".//td[@class='weather__desc']");
                    record.Cloud = td2.InnerText.Substring(0, td2.InnerText.IndexOf(","));
                    string flow = td2.InnerText.Substring(td2.InnerText.IndexOf(",") + 1);
                    if (flow.Contains("без осадков"))
                        record.Flow = false;
                    if (flow.Contains("осадки"))
                        record.Flow = true;

                    foreach (var p in div.Descendants("p"))
                    {
                        if (p.InnerText.Contains("Ветер"))
                        {
                            var match = Regex.Match(p.InnerText, "[0-9]+");
                            record.WindSpeed = int.Parse(match.Value);
                            record.WindDir = p.InnerText.Substring(p.InnerText.IndexOf(",") + 1);
                        }
                    }

                    WeatherList.Add(record);
                }
            }
        }

        public void SaveData()
        {
            WeatherDB.RecordList(WeatherList);
        }
    }
}

using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WeatherLib;

namespace YandexWebJob
{
    class YandexParser
    {
        static readonly Uri address = new Uri("http://yandex.ru/pogoda/moscow/details");
        public List<WeatherRecord> WeatherList;

        public YandexParser()
        {
            WeatherList = new List<WeatherRecord>();


        }

        public void LoadData()
        {
            using (var client = new HttpClient { BaseAddress = address })
            {
                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(client.GetStringAsync("").Result);
                var tables = html.DocumentNode.SelectNodes("//table[@class='weather-table']");
                int day = 0;
                foreach (var table in tables)
                {
                    var record = new WeatherRecord();
                    record.ForecastDay = day++;
                    record.Portal = "Yandex";

                    record.PartitionKey = DateTime.Now.ToString("yy-MM-dd");
                    record.RowKey = $"Y{record.PartitionKey}.{record.ForecastDay}";

                    List<int> temps = new List<int>();
                    List<string> clouds = new List<string>();
                    List<int> windspeeds = new List<int>();
                    List<string> winddirs = new List<string>();
                    var divs_temp = table.SelectNodes(".//div[@class='weather-table__wrapper']"); 
                    foreach (var div in divs_temp)
                    {
                        var spans_temp = div.SelectNodes(".//span[@class='temp__value']");
                        if (spans_temp !=null)
                            foreach (var span in spans_temp)
                                temps.Add(int.Parse(span.InnerText.Replace('\x2212','-')));
                            
                        var spans_wind_speed = div.SelectNodes(".//span[@class='wind-speed']");
                        if (spans_wind_speed != null)
                            foreach (var span in spans_wind_speed)
                            {
                                windspeeds.Add((int)Math.Round(double.Parse(span.InnerText.Replace(',','.'))));
                            }
                    }

                    record.Tmin = temps.Min();
                    record.Tmax = temps.Max();
                    record.WindSpeed = windspeeds.Max();

                    var tds_cloud = table.SelectNodes(".//td[@class='weather-table__body-cell weather-table__body-cell_type_condition']");
                    foreach (var td in tds_cloud)
                        clouds.Add(td.InnerText);
                    record.Cloud = (from c in clouds
                                   group c by c into g
                                   orderby g.Count() descending
                                   select g.Key).ToList().First();
                    if (clouds.Any(s => s.ToLower().Contains("дожд") || s.ToLower().Contains("снег")))
                        record.Flow = true;
                    else
                        record.Flow = false;


                    var abbrs_winddir = table.SelectNodes(".//abbr");
                    foreach (var abbr in abbrs_winddir)
                        winddirs.Add(abbr.InnerText);
                    record.WindDir = (from w in winddirs
                                      group w by w into g
                                      orderby g.Count() descending
                                      select g.Key).ToList().First();

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

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

namespace Rp5WebJob
{
    class Rp5Parser
    {
        static readonly Uri address = new Uri("http://rp5.ru/Погода_в_Москве_(ВДНХ)");
        public List<WeatherRecord> WeatherList;

        public Rp5Parser()
        {
            WeatherList = new List<WeatherRecord>();
        }

        public void LoadData()
        {
            using (var client = new HttpClient { BaseAddress = address })
            {
                List<int> dateslength = new List<int>();
                List<string> clouds = new List<string>();
                List<int> temps = new List<int>();
                List<int> windspeeds = new List<int>();
                List<string> winddirs = new List<string>();
                List<string> flows = new List<string>();

                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(client.GetStringAsync("").Result);
                
                var tr_titles = html.DocumentNode.SelectSingleNode("//tr[@class='forecastDate']");
                string attr = "";
                foreach (var td in tr_titles.ChildNodes)
                {
                    attr=td.GetAttributeValue("colspan", "");
                    if (!string.IsNullOrEmpty(attr))
                        dateslength.Add(int.Parse(attr) / 2);
                }


                var trs = html.DocumentNode.SelectSingleNode("//table[@class='forecastTable']").SelectNodes(".//tr");
                var div_clouds = trs[2].SelectNodes(".//div[@class='cc_0']");
                foreach (var div in div_clouds)
                {
                    attr = div.ChildNodes[0].GetAttributeValue("onmouseover","");
                    if (!string.IsNullOrEmpty(attr))
                    {
                        int start = attr.IndexOf("<b>") + 3, end = attr.IndexOf("</b>");
                        clouds.Add(attr.Substring(start, end - start));
                    }

                }
                
                var divs_temp = trs[5].SelectNodes(".//div[@class='t_0']");
                foreach (var div in divs_temp)
                {
                    temps.Add(int.Parse(div.InnerText));
                }

                var divs_windspeed = trs[8].SelectNodes(".//div[contains(@class,'wv_0') or contains(@class,'wv_0 ')]");                
                foreach (var div in divs_windspeed)
                {
                    windspeeds.Add(int.Parse(div.InnerText));
                }

                var tds_winddirs = trs[10].SelectNodes(".//td");
                foreach (var td in tds_winddirs)
                {
                    winddirs.Add(td.InnerText);
                }
                winddirs = winddirs.Skip(1).ToList();

                int start_index = 0, end_index = 0;
                var divs_flow = trs[3].SelectNodes(".//div[@class='pr_0']");
                foreach (var div in divs_flow)
                {
                    attr = div.GetAttributeValue("onmouseover", "");
                    if (string.IsNullOrEmpty(attr)) continue;
                    start_index = attr.IndexOf("'");
                    end_index = attr.IndexOf("'",start_index+1);

                    flows.Add(attr.Substring(start_index, end_index - start_index));
                }

                /////////////////////////////////////////////////////////////
                for (int i = 0; i < dateslength.Count - 1; i++)
                    dateslength[i + 1] += dateslength[i];


                start_index = 0;
                end_index = 0;
                for (int i = 0; i < 5; i++)
                {
                    if (i == 0)
                        start_index = 0;
                    else
                        start_index = dateslength[i - 1] + 1;
                    end_index = dateslength[i];

                    var record = new WeatherRecord();
                    record.ForecastDay = i;
                    record.PartitionKey = DateTime.Now.ToString("yy-MM-dd");
                    record.RowKey = $"R{record.PartitionKey}.{record.ForecastDay}";
                    record.Portal = "Rp5";
                    

                    record.Cloud = (from c in clouds.Skip(start_index).Take(end_index - start_index + 1)
                                    group c by c into g
                                    orderby g.Count() descending
                                    select g.Key).ToList().First();

                    if (flows.Skip(2 * start_index).Take(2 * (end_index - start_index) + 1).Any(s => s.ToLower().Contains("дожд") || s.ToLower().Contains("снег")))
                        record.Flow = true;
                    else
                        record.Flow = false;

                    record.Tmax = temps.Skip(start_index).Take(end_index - start_index + 1).Max();
                    record.Tmin = temps.Skip(start_index).Take(end_index - start_index + 1).Min();

                    record.WindDir = (from w in winddirs.Skip(start_index).Take(end_index - start_index + 1)
                                      group w by w into g
                                      orderby g.Count() descending
                                      select g.Key).ToList().First();
                    record.WindSpeed = (from w in windspeeds.Skip(start_index).Take(end_index - start_index + 1)
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

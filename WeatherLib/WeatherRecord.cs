using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherLib
{
    public class WeatherRecord : TableEntity
    {
        public string Portal { get; set; }
        public int ForecastDay { get; set; }
        public int Tmin { get; set; }
        public int Tmax { get; set; }
        public string WindDir { get; set; }
        public int WindSpeed { get; set; }
        public string Cloud { get; set; }
        public bool Flow { get; set; }

        public WeatherRecord()
        {
            //PartitionKey = "1";
            //RowKey = $"1.{Portal}";
        }
        
    }
}

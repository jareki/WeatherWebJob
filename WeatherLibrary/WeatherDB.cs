using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherLibrary
{
    public static class WeatherDB
    {
        static string ConStr = "DefaultEndpointsProtocol=https;AccountName=weatherstat;AccountKey=lKNbj4FGLtbkPv+lqD3M/Bu6r8ZALgmykqjULyXV8gGDAho42IzMbAFqLzxPrgfTMg8RNKd9FFRn7yuOD67/Jw==;EndpointSuffix=core.windows.net";
       
        static CloudStorageAccount CloudStore;
        static CloudTableClient CloudTable;
        public static CloudTable Table;

        static WeatherDB()
        {
            CloudStore = CloudStorageAccount.Parse(ConStr);
            CloudTable = CloudStore.CreateCloudTableClient();
            Table = CloudTable.GetTableReference("Records");
            Table.CreateIfNotExists();
        }

        public static async void Record(WeatherRecord record)
        {
            await Table.ExecuteAsync(TableOperation.Insert(record));
        }

        public static void RecordList(List<WeatherRecord> records)
        {
            TableBatchOperation op = new TableBatchOperation();
            records.ForEach(w => op.Insert(w));
            Table.ExecuteBatch(op);
        }

        public static async Task<bool> RecordSuccess(WeatherRecord record)
        {
            await Table.ExecuteAsync(TableOperation.Insert(record));
            return true;
        }

        public static IEnumerable<WeatherRecord> GetData()
        {
            var q = new TableQuery<WeatherRecord>();
            return WeatherDB.Table.ExecuteQuery(q);
        }
    }
}

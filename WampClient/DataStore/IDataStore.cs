using EXOscadaAPI.Protocols;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EXOScadaAPI.DataStore
{
    public interface IDataStore
    {
        void Open();

        ProjectInfo GetProjectInfo();

        bool Write(DataStoreMessage message);

        Dictionary<string, DataStoreMessage> Read(List<string> variables);

        Task<DataStoreMessage> Read(string variable);
    }
}
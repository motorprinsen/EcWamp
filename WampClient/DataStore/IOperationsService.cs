using EXOscadaAPI.Protocols;
using System.Threading.Tasks;
using WampSharp.V2.Rpc;

namespace EXOScadaAPI.DataStore.Wamp
{
    public interface IOperationsService
    {
        [WampProcedure("read")]
        Task<DataStoreMessage> Read(string variable);

        //[WampProcedure("readmany")]
        //Task<IEnumerable<DataStoreMessage>> ReadMany(List<string> variables);

        [WampProcedure("write")]
        bool Write(DataStoreMessage message);

        [WampProcedure("getProjectInfo")]
        ProjectInfo GetProjectInfo();
    }
}
using System.Threading.Tasks;
using WampSharp.V2.Rpc;

namespace EcWamp.DataStore
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
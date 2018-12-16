
using WampSharp.V2.Rpc;

namespace JsonData
{
    public interface IViewService
    {
        [WampProcedure("com.arguments.getView")]
        JsonDataSet GetView(string area, string viewFile, object[] args);
    }
}

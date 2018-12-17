
using WampSharp.V2.Rpc;

namespace JsonData
{
    public interface IViewService
    {
        [WampProcedure("se.rssoftware.eos.getView")]
        JsonDataSet GetView(string area, object[] args);
    }
}

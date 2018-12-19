using Newtonsoft.Json;
using System;

namespace EXOscadaAPI.Protocols
{
    public enum DataStoreMessageType
    {
        Request,
        Response,
        Update,
        ErrorNotExist, //The variable not exist
        ErrorUnreachable,//Comunication broken after peek or advise ex ccu dbnull
        Pending//value has not solved yet null

    }
    public class DataStoreMessage
    {
        [JsonProperty("value")]
        public object Value { get; set; }

        [JsonProperty("variable")]
        public string Variable { get; set; }

        [JsonProperty("type")]
        public DataStoreMessageType Type { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }


        public override string ToString()
        {
            return string.Format($"Variable: {Variable}, Value: {Value}, Type: {Type}, Timestamp: {Timestamp}");
        }
    }

    public class ProjectInfo
    {
        [JsonProperty("currentProject")]
        public string CurrentProject { get; set; }

        [JsonProperty("mainComputer")]
        public string MainComputer { get; set; }

        [JsonProperty("alarmsDbName")]
        public string AlarmsDbName { get; set; }

        [JsonProperty("alarmsDbConnectionString")]
        public string AlarmsDbConnectionString { get; set; }

        [JsonProperty("analogValuesDbName")]
        public string AnalogValuesDbName { get; set; }

        [JsonProperty("analogValuesDbConnectionString")]
        public string AnalogValuesDbConnectionString { get; set; }

        [JsonProperty("digitalValuesDbName")]
        public string DigitalValuesDbName { get; set; }

        [JsonProperty("digitalValuesDbConnectionString")]
        public string DigitalValuesDbConnectionString { get; set; }
    }

    //public interface IOperationsService
    //{
    //    [WampExoProcedureAttribute("read")]
    //    DataStoreMessage Read(string variable);

    //    [WampExoProcedureAttribute("write")]
    //    bool Write(string variable, DataStoreMessage value);


    //}
    //[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    //public sealed class WampExoProcedureAttribute : Attribute
    //{
    //    private readonly string mProcedure;

    //    /// <summary>
    //    /// Initializes a new instance of <see cref="WampProcedureAttribute"/> given
    //    /// the procedure uri this method is mapped to.
    //    /// </summary>
    //    /// <param name="procedure">The given the procedure uri this method is mapped to.</param>
    //    public WampExoProcedureAttribute(string procedure)
    //    {
    //        this.mProcedure = procedure;
    //    }

    //    /// <summary>
    //    /// Gets the procedure uri this method is mapped to.
    //    /// </summary>
    //    public string Procedure
    //    {
    //        get
    //        {
    //            return mProcedure;
    //        }
    //    }
    //}
}

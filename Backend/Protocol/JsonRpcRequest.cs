using System.Text.Json.Serialization;

namespace UnoGame.Backend.Protocol
{
    /// <summary>
    /// JSON-RPC 2.0 Request formatı.
    /// </summary>
    public class JsonRpcRequest<T>
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("method")]
        public string Method { get; set; } = "";

        [JsonPropertyName("params")]
        public T? Params { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// JSON-RPC 2.0 Response formatı.
    /// </summary>
    public class JsonRpcResponse<T>
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("result")]
        public T? Result { get; set; }

        [JsonPropertyName("error")]
        public JsonRpcError? Error { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
    }

    public class JsonRpcError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }
}
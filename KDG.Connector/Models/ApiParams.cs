using System.Net.Http.Headers;
namespace KDG.Connector.Models
{
    public struct ApiParams
    {
        public Dictionary<string, string?>? urlParams { get; set; }
        public object? postParams { get; set; }
        public Dictionary<string, string>? headers { get; set; }
        public MediaTypeWithQualityHeaderValue? AcceptHeader { get; set; }
    }
}

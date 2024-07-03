namespace KDG.Connector.Models
{
    public struct OAuthConfiguration
    {
        public string TokenUri { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RefreshToken { get; set; }
    }
}

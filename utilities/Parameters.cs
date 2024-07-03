namespace KDG.Connector.Utilities
{
    public static class Parameters
    {
        public static Uri GenerateUri(Uri path, Dictionary<string,string?> QueryParams)
        {
            string parameterText = String.Empty;
            if (QueryParams != null)
            {
                var parameters = QueryParams.Select(x => string.Join("=", x.Key, System.Web.HttpUtility.UrlEncode(x.Value)));
                parameterText = string.Join('&', parameters);
            }
            var uri = path.ToString();
            return new Uri(String.Join('?', uri, parameterText));
        }
    }
}

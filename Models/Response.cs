namespace KDG.Connector.Models
{
      public class Response<T>
    {
        public T Data { get; set; }
        public HttpResponseMessage HttpResponseMessage { get; set; }

        public Response(T data, HttpResponseMessage httpResponseMessage)
        {
            Data = data;
            HttpResponseMessage = httpResponseMessage;
        }
    }
}

namespace jcHernande2.ServiceClients.Http.Models.Exception
{
    using System;
    using System.Net;
    using System.Runtime.Serialization;

    [Serializable]
    public class ServiceClientException : Exception
    {
        public HttpStatusCode? StatusCode { get; }
        public string ErrorCode { get; }
        public object Model { get; } // solo si es serializable / simple

        public ServiceClientException() { }

        public ServiceClientException(string message) : base(message) { }

        public ServiceClientException(string message, Exception inner) : base(message, inner) { }

        public ServiceClientException(string message, HttpStatusCode statusCode, string errorCode = null, object model = null)
            : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            Model = model;
        }

        protected ServiceClientException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            StatusCode = (HttpStatusCode?)info.GetValue(nameof(StatusCode), typeof(HttpStatusCode?));
            ErrorCode = info.GetString(nameof(ErrorCode));
            Model = info.GetValue(nameof(Model), typeof(object));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(StatusCode), StatusCode, typeof(HttpStatusCode?));
            info.AddValue(nameof(ErrorCode), ErrorCode);
            info.AddValue(nameof(Model), Model);
        }
    }
}

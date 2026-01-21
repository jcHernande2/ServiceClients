namespace jcHernande2.ServiceClients.Http.Models.Exception
{
    using System;

    public class jcHernande2Exception : Exception
    {
        public ModelException Model { get; }

        public jcHernande2Exception(string message)
            : base(message)
        {

        }

        public jcHernande2Exception(string message, ModelException model)
            : base(message)
        {
            Model = model;
        }
    }
}

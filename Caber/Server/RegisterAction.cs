using System;
using System.Security.Claims;
using Caber.Authentication;

namespace Caber.Server
{
    public class RegisterAction
    {
        public RegisterAction()
        {
        }

        public virtual Response Execute(Request request, ClaimsPrincipal clientPrincipal)
        {
            throw new NotImplementedException();
        }

        public class Request
        {
            public CaberIdentity ClientIdentity { get; set; }
            public CaberIdentity ServerIdentity { get; set; }
            public CaberSharedEnvironment Environment { get; set; }
            public string[] Roots { get; set; }
        }

        public enum ResponseType
        {
            Accepted = 0,
            EnvironmentNotSupported
        }

        public class Response
        {
            public ResponseType Type { get; set; }
            public CaberIdentity ServerIdentity { get; set; }
            public string[] AcceptedRoots { get; set; }
            public CaberSharedEnvironment Environment { get; set; }
        }
    }
}

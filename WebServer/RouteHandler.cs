using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Extensions;

namespace WebServer
{
    /// <summary>
    /// The base class for route handlers.  If not for being abstract, this would be the equivalent of an anonymous handler,
    /// but we want to enforce an explicit declaration of that so the developer doesn't accidentally use RouteHandler without
    /// realizing that it's an anonymous, unauthenticated, no session timeout check, handler.  Defensive Programming!
    /// </summary>
    public abstract class RouteHandler
    {
        protected Server server;
        protected Func<Session, Dictionary<string, object>, ResponsePacket> handler;

        public RouteHandler(Server server, Func<Session, Dictionary<string, object>, ResponsePacket> handler)
        {
            this.server = server;
            this.handler = handler;
        }

        public virtual ResponsePacket Handle(Session session, Dictionary<string, object> parms)
        {
            return InvokeHandler(session, parms);
        }

        /// <summary>
        /// CanHandle is used only for determining which handler, in a multiple handler for a single route, can actually handle to session and params for that route.
        /// </summary>
        public virtual bool CanHandle(Session session, Dictionary<string, object> parms) { return true; }

        protected ResponsePacket InvokeHandler(Session session, Dictionary<string, object> parms)
        {
            ResponsePacket ret = null;
            handler.IfNotNull((h) => ret = h(session, parms));

            return ret;
        }
    }

    /// <summary>
    /// Page is always visible.
    /// </summary>
    public class AnonymousRouteHandler : RouteHandler
    {
        public AnonymousRouteHandler(Server server, Func<Session, Dictionary<string, object>, ResponsePacket> handler = null)
            : base(server, handler)
        {
        }
    }

    /// <summary>
    /// Page is visible only to authorized users.
    /// </summary>
    public class AuthenticatedRouteHandler : RouteHandler
    {
        public AuthenticatedRouteHandler(Server server, Func<Session, Dictionary<string, object>, ResponsePacket> handler = null)
            : base(server, handler)
        {
        }

        public override ResponsePacket Handle(Session session, Dictionary<string, object> parms)
        {
            ResponsePacket ret;

            if (session.Authenticated)
            {
                ret = InvokeHandler(session, parms);
            }
            else
            {
                ret = server.Redirect(server.OnError(Server.ServerError.NotAuthorized));
            }

            return ret;
        }
    }

    /// <summary>
    /// Page is visible only to authorized users whose session has not expired.
    /// </summary>
    public class AuthenticatedExpirableRouteHandler : AuthenticatedRouteHandler
    {
        public AuthenticatedExpirableRouteHandler(Server server, Func<Session, Dictionary<string, object>, ResponsePacket> handler = null)
            : base(server, handler)
        {
        }

        public override ResponsePacket Handle(Session session, Dictionary<string, object> parms)
        {
            ResponsePacket ret;

            if (session.IsExpired(server.ExpirationTimeSeconds))
            {
                session.Expire();
                ret = server.Redirect(server.OnError(Server.ServerError.ExpiredSession));
            }
            else
            {
                ret = base.Handle(session, parms);
            }

            return ret;
        }
    }

}
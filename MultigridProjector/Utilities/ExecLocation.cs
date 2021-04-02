using System;

namespace MultigridProjector.Utilities
{
    [AttributeUsage(AttributeTargets.Method)]
    public class Everywhere : Attribute
    {
        // Called both on server and on client side
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ServerOnly : Attribute
    {
        // Called only on server side
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ClientOnly : Attribute
    {
        // Called only on client side
    }
}
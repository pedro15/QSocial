using System;

namespace QSocial.Auth
{
    [AttributeUsage(AttributeTargets.Class , AllowMultiple = false)]
    public class HasAnonymousConversionAttribute : Attribute { }
}

using System;
using Firebase.Auth;

namespace QSocial.Auth
{
    public interface IAsyncModule
    {
        bool IsLoading(bool Guest, FirebaseUser user);

        Exception GetException();
    }
}
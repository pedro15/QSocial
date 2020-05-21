using Firebase.Auth;

namespace QSocial.Auth
{
    public abstract class AuthModule
    {
        public abstract bool IsValid(bool GuestRequest, FirebaseUser user);
        
        public abstract void Execute(AuthManager manager);

        public abstract ProcessResult GetResult();
        
        public virtual void OnFinish(AuthManager manager, bool Interrumpted ) { }

        public virtual void OnInit(AuthManager manager) { }

        public virtual void OnEnter() { }
        public virtual System.Exception GetException() => null;

        public virtual bool IsInterruptible() => false;
    }
}

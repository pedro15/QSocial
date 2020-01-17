using Firebase.Auth;

namespace QSocial.Auth
{
    public abstract class AuthModule
    {
        public abstract bool IsValid(bool GuestRequest, FirebaseUser user);
        
        public abstract void Execute(AuthManager manager);

        public abstract bool IsCompleted();

        public virtual void OnFinish(AuthManager manager, bool Interrupted) { }

        public virtual void OnInit(AuthManager manager) { }

        public virtual void OnEnter() { }

        public virtual bool IsInterruptible() => false;
    }
}

namespace QSocial.Auth
{
    public interface ICustomCommand
    {
        AuthCheckCommand GetNextCommand();
    }
}
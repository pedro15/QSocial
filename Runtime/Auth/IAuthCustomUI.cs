namespace QSocial.Auth
{
    public interface IAuthCustomUI
    {
        void DisplayUI(bool IsAnonymous);

        void HideUI();

    }
}
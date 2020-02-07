namespace QSocial.Auth
{
    [System.Flags]
    public enum QAuthErrorCode : int
    {
        INVALID_USERNAME = 1,
        USERNAME_EXISTS = 2,
        FORM_INCOMPLETE
    }

    public class QAuthException : System.Exception
    {
        public QAuthErrorCode ErrorCode { get; private set; }

        public QAuthException(QAuthErrorCode errorCode)
        {
            ErrorCode = errorCode;
        }

        public override string Message
        {
            get
            {
                switch (ErrorCode)
                {
                    case QAuthErrorCode.INVALID_USERNAME:
                        return "Username Invalid";

                    case QAuthErrorCode.USERNAME_EXISTS:
                        return "Username already exists";

                    case QAuthErrorCode.FORM_INCOMPLETE:
                        return "Please fill all fields";
                }

                return string.Empty;
            }
        }

    }
}
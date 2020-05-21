namespace QSocial.Auth
{
    public enum QAuthErrorCode
    {
        INVALID_USERNAME,
        SHORT_USERNAME,
        USERNAME_EXISTS,
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
                    case QAuthErrorCode.SHORT_USERNAME:
                        return "Username is too short";
                }

                return string.Empty;
            }
        }

    }
}
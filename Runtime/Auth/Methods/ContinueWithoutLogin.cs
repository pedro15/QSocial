namespace QSocial.Auth.Methods
{
    [System.Serializable]
    public class ContinueWithoutLogin : AuthMethod
    {
        public override string Id => "No-Login";

        public override string ResultUserId => "";

        private int frames = 0;

        public override void OnEnter()
        {
            frames = 0;
        }

        public override ProcessResult GetResult()
        {
            if (frames > 0)
                return ProcessResult.Completed;
            else
            {
                frames++;
                return ProcessResult.Running;
            }
        }
    }
}
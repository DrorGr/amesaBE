namespace AmesaBackend.Shared.Enums
{
    public enum AMESAHeader
    {
        Authorization,
        SessionId,
        LanguageId
    }

    public static class AMESAHeaderExtensions
    {
        public static string GetValue(this AMESAHeader header)
        {
            return header switch
            {
                AMESAHeader.Authorization => "Authorization",
                AMESAHeader.SessionId => "SessionId",
                AMESAHeader.LanguageId => "LanguageId",
                _ => header.ToString()
            };
        }
    }
}


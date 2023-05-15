namespace CountryService.Global
{
    public static class DateTimeTools
    {
        private static DateTime originUnixTimeStamp = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        
        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            return originUnixTimeStamp.AddSeconds(unixTimeStamp).ToUniversalTime();
        }
    }
}
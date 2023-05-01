namespace CountryService.Constants
{
    public class Configurations
    {
        public class Const
        {
            public const string CONFIG_ELK = "ELKConfiguration";
            public const string CONFIG_INDEX_NAME = CONFIG_ELK + ":index";

            public const string CONFIG_REST_COUNTRIES = "RestCountriesApi";
            public const string CONFIG_REST_COUNTRIES_API_URL = CONFIG_REST_COUNTRIES + ":Uri";

            public const string CONFIG_API_NINJAS = "ApiNinjas";
            public const string CONFIG_API_NINJAS_KEY = CONFIG_API_NINJAS + ":ApiKey";
            public const string CONFIG_API_NINJAS_CITY_URL = CONFIG_API_NINJAS + ":CityUri";
            
            public const string CONFIG_AWS = "AwsConfiguration";
            public const string CONFIG_AWS_S3_BUCKET_NAME = CONFIG_AWS + ":BucketName";
            public const string CONFIG_AWS_S3_ACCESS_KEY = CONFIG_AWS + ":AWSAccessKey";
            public const string CONFIG_AWS_S3_SECRET_KEY = CONFIG_AWS + ":AWSSecretKey";
        }
        
    }
}
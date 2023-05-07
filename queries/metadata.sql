-- Query all Migrations
SELECT * FROM dbo.__EFMigrationsHistory

-- Query all tables
SELECT * 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_CATALOG='countrydb'
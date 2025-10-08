-- Create the database if it doesn't exist
SELECT 'CREATE DATABASE amesa_lottery' 
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'amesa_lottery')\gexec

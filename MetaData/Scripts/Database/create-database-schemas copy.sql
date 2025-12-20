-- SQL script to create database schemas for microservices in Aurora cluster
-- Connect to: amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com
-- Database: (default database, schemas will be created in it)
-- Username: dror

-- Create schemas for each microservice
CREATE SCHEMA IF NOT EXISTS amesa_auth;
CREATE SCHEMA IF NOT EXISTS amesa_payment;
CREATE SCHEMA IF NOT EXISTS amesa_lottery;
CREATE SCHEMA IF NOT EXISTS amesa_content;
CREATE SCHEMA IF NOT EXISTS amesa_notification;
CREATE SCHEMA IF NOT EXISTS amesa_lottery_results;
CREATE SCHEMA IF NOT EXISTS amesa_analytics;
-- Admin service may not need a separate schema if it uses Auth service's schema

-- Grant permissions (adjust as needed)
-- GRANT ALL ON SCHEMA amesa_auth TO amesa_user;
-- GRANT ALL ON SCHEMA amesa_payment TO amesa_user;
-- GRANT ALL ON SCHEMA amesa_lottery TO amesa_user;
-- GRANT ALL ON SCHEMA amesa_content TO amesa_user;
-- GRANT ALL ON SCHEMA amesa_notification TO amesa_user;
-- GRANT ALL ON SCHEMA amesa_lottery_results TO amesa_user;
-- GRANT ALL ON SCHEMA amesa_analytics TO amesa_user;

-- Verify schemas were created
SELECT schema_name 
FROM information_schema.schemata 
WHERE schema_name LIKE 'amesa_%'
ORDER BY schema_name;


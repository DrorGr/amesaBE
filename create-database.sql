-- Create the Amesa Lottery Database
CREATE DATABASE amesa_lottery;

-- Connect to the new database
\c amesa_lottery;

-- Create a simple test table to verify the database works
CREATE TABLE IF NOT EXISTS test_table (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Insert a test record
INSERT INTO test_table (name) VALUES ('Database created successfully');

-- Show the test record
SELECT * FROM test_table;

-- Drop the test table
DROP TABLE test_table;

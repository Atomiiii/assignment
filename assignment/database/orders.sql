CREATE TABLE Orders (
    id VARCHAR PRIMARY KEY,
    product VARCHAR,
    total DECIMAL,
    currency VARCHAR
    isPaid BOOLEAN DEFAULT FALSE,
);
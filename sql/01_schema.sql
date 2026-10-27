-- Extra tables for Q4 and Q5 (run after 00_schema.sql)

CREATE TABLE Customers (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Name VARCHAR(100) NOT NULL,
    Country CHAR(2) NOT NULL,
    City VARCHAR(100) NOT NULL
);

CREATE TABLE Products (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Name VARCHAR(100) NOT NULL,
    Price DECIMAL(12,2) NOT NULL
);

-- Orders needs a product and a status for Q4 / Q5
ALTER TABLE Orders
    ADD COLUMN ProductId INT NULL,
    ADD COLUMN Status VARCHAR(20) NOT NULL DEFAULT 'Pending';

INSERT INTO Customers (Name, Country, City) VALUES
('Rahim Uddin', 'BD', 'Dhaka'),
('Sara Khan', 'BD', 'Chattogram'),
('John Smith', 'US', 'Austin');

INSERT INTO Products (Name, Price) VALUES
('Keyboard', 50), ('Monitor', 200);

UPDATE Orders SET ProductId = 1, Status = 'Completed' WHERE Id IN (1, 2, 5);
UPDATE Orders SET ProductId = 2, Status = 'Pending'   WHERE Id IN (3, 4, 6);
UPDATE Orders SET OrderDate = '2026-06-10 10:00:00' WHERE Id IN (1, 2);

-- Test tables + sample data (MySQL 8.0)

CREATE TABLE Employee (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Name VARCHAR(100) NOT NULL,
    DepartmentId INT NOT NULL,
    Salary DECIMAL(12,2) NOT NULL,
    JoiningDate DATE NOT NULL
);

CREATE TABLE UserLogin (
    UserId INT NOT NULL,
    LoginDate DATETIME NOT NULL
);

CREATE TABLE Orders (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    CustomerId INT NOT NULL,
    OrderDate DATETIME NOT NULL,
    Amount DECIMAL(12,2) NOT NULL
);

INSERT INTO Employee (Name, DepartmentId, Salary, JoiningDate) VALUES
('Rahim', 1, 90000, '2020-01-10'),
('Karim', 1, 90000, '2021-03-15'),
('Sumon', 1, 80000, '2019-07-01'),
('Nila',  1, 70000, '2022-02-20'),
('Tania', 1, 70000, '2018-05-05'),
('Jamal', 2, 60000, '2020-09-09'),
('Mitu',  2, 55000, '2021-11-11'),
('Arif',  2, 50000, '2023-01-01');

-- user 1: 8 days in a row (with a duplicate on day 3), user 2: only 5 days
INSERT INTO UserLogin (UserId, LoginDate) VALUES
(1, '2026-10-01 09:00:00'), (1, '2026-10-02 10:00:00'),
(1, '2026-10-03 08:00:00'), (1, '2026-10-03 21:00:00'),
(1, '2026-10-04 09:30:00'), (1, '2026-10-05 09:30:00'),
(1, '2026-10-06 09:30:00'), (1, '2026-10-07 09:30:00'),
(1, '2026-10-08 09:30:00'),
(2, '2026-10-01 09:00:00'), (2, '2026-10-02 09:00:00'),
(2, '2026-10-03 09:00:00'), (2, '2026-10-04 09:00:00'),
(2, '2026-10-05 09:00:00');

-- customer 1 has two orders at the exact same time
INSERT INTO Orders (CustomerId, OrderDate, Amount) VALUES
(1, '2026-09-01 10:00:00', 100),
(1, '2026-09-05 12:00:00', 250),
(1, '2026-09-05 12:00:00', 50),
(1, '2026-09-20 15:00:00', 300),
(2, '2026-09-02 11:00:00', 500),
(2, '2026-09-10 11:00:00', 400);

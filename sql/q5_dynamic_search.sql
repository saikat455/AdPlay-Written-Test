-- Q5: Secure dynamic search + pagination

-- How injection is prevented:
--  * filter values are never concatenated into the SQL text, the query only
--    references session variables (@name, @status ...)
--  * SortBy goes through an allowlist (CASE), anything unknown falls back to a default
--  * SortDirection is only ever ASC or DESC
--  * page / pageSize are INT parameters, so only numbers can end up in LIMIT

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_SearchOrders $$

CREATE PROCEDURE sp_SearchOrders(
    IN p_CustomerName  VARCHAR(100),
    IN p_DateFrom      DATE,
    IN p_DateTo        DATE,
    IN p_MinAmount     DECIMAL(12,2),
    IN p_MaxAmount     DECIMAL(12,2),
    IN p_Status        VARCHAR(20),
    IN p_Country       CHAR(2),
    IN p_City          VARCHAR(100),
    IN p_SortBy        VARCHAR(30),
    IN p_SortDirection VARCHAR(4),
    IN p_Page          INT,
    IN p_PageSize      INT
)
BEGIN
    DECLARE v_where TEXT DEFAULT ' WHERE 1 = 1';
    DECLARE v_sort  VARCHAR(30);
    DECLARE v_dir   VARCHAR(4);
    DECLARE v_size  INT;
    DECLARE v_offset INT;

    -- values go into session variables, not into the SQL string
    SET @f_name   = p_CustomerName;
    SET @f_from   = p_DateFrom;
    SET @f_to     = p_DateTo;
    SET @f_min    = p_MinAmount;
    SET @f_max    = p_MaxAmount;
    SET @f_status = p_Status;
    SET @f_country = p_Country;
    SET @f_city   = p_City;

    -- add a predicate only when the filter is supplied
    IF p_CustomerName IS NOT NULL AND p_CustomerName <> '' THEN
        SET v_where = CONCAT(v_where, ' AND c.Name LIKE CONCAT(@f_name, ''%'')'); -- prefix search can use an index
    END IF;
    IF p_DateFrom IS NOT NULL THEN
        SET v_where = CONCAT(v_where, ' AND o.OrderDate >= @f_from');
    END IF;
    IF p_DateTo IS NOT NULL THEN
        SET v_where = CONCAT(v_where, ' AND o.OrderDate < DATE_ADD(@f_to, INTERVAL 1 DAY)'); -- DateTo is inclusive
    END IF;
    IF p_MinAmount IS NOT NULL THEN
        SET v_where = CONCAT(v_where, ' AND o.Amount >= @f_min');
    END IF;
    IF p_MaxAmount IS NOT NULL THEN
        SET v_where = CONCAT(v_where, ' AND o.Amount <= @f_max');
    END IF;
    IF p_Status IS NOT NULL AND p_Status <> '' THEN
        SET v_where = CONCAT(v_where, ' AND o.Status = @f_status');
    END IF;
    IF p_Country IS NOT NULL AND p_Country <> '' THEN
        SET v_where = CONCAT(v_where, ' AND c.Country = @f_country');
    END IF;
    IF p_City IS NOT NULL AND p_City <> '' THEN
        SET v_where = CONCAT(v_where, ' AND c.City = @f_city');
    END IF;

    -- sort column allowlist
    SET v_sort = CASE LOWER(IFNULL(p_SortBy, ''))
        WHEN 'customername' THEN 'c.Name'
        WHEN 'amount'       THEN 'o.Amount'
        WHEN 'status'       THEN 'o.Status'
        WHEN 'country'      THEN 'c.Country'
        WHEN 'city'         THEN 'c.City'
        ELSE 'o.OrderDate'
    END;
    SET v_dir = IF(UPPER(IFNULL(p_SortDirection, '')) = 'ASC', 'ASC', 'DESC');

    -- page limits (max 100 rows per page)
    SET v_size   = LEAST(GREATEST(IFNULL(p_PageSize, 20), 1), 100);
    SET v_offset = (GREATEST(IFNULL(p_Page, 1), 1) - 1) * v_size;

    -- 1st result set: total count
    SET @sql_count = CONCAT(
        'SELECT COUNT(*) AS TotalCount ',
        'FROM Orders o JOIN Customers c ON c.Id = o.CustomerId', v_where);
    PREPARE stmt FROM @sql_count;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;

    -- 2nd result set: the page (o.Id as tie-breaker so paging is stable)
    SET @sql_data = CONCAT(
        'SELECT o.Id, c.Name AS CustomerName, c.Country, c.City, ',
        'o.OrderDate, o.Amount, o.Status ',
        'FROM Orders o JOIN Customers c ON c.Id = o.CustomerId', v_where,
        ' ORDER BY ', v_sort, ' ', v_dir, ', o.Id ',
        ' LIMIT ', v_size, ' OFFSET ', v_offset);
    PREPARE stmt FROM @sql_data;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
END $$

DELIMITER ;

-- Supporting indexes
CREATE INDEX idx_orders_status_date ON Orders (Status, OrderDate);
CREATE INDEX idx_orders_amount      ON Orders (Amount);
CREATE INDEX idx_customers_country_city ON Customers (Country, City);
CREATE INDEX idx_customers_name     ON Customers (Name);
-- (idx_orders_date_customer from Q4 covers the date range filter)

-- Example calls
-- CALL sp_SearchOrders(NULL, '2026-06-01', '2026-06-30', NULL, NULL, 'Completed', 'BD', NULL, 'amount', 'desc', 1, 10);
-- CALL sp_SearchOrders('Rah', NULL, NULL, 100, 500, NULL, NULL, NULL, 'DROP TABLE x', 'desc; --', 1, 5000);  -- bad sort input is ignored, size capped at 100

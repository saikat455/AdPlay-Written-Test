# AdPlay Written Test - Coding Part

ASP.NET Core (.NET 10) + EF Core 9 + MySQL 8.

## Where is what

| Question | Location |
|---|---|
| Q1 Top 3 per department | `sql/q1_top3_per_department.sql` |
| Q2 Seven consecutive logins | `sql/q2_seven_consecutive_logins.sql` |
| Q3 Order analytics (window functions) | `sql/q3_customer_order_analytics.sql` |
| Q4 Index-friendly query | `sql/q4_index_friendly_query.sql` |
| Q5 Secure dynamic search | `sql/q5_dynamic_search.sql` |
| Q6 Duplicate subscriptions | `src/AdPlay.Api/Controllers/SubscriptionsController.cs` |
| Q7 Payment API | `src/AdPlay.Api/Controllers/PaymentsController.cs`, `Services/PaymentService.cs`, `Models/Payment.cs` |
| Q8 Query service | `src/AdPlay.Api/Services/QueryService.cs` |
| Q9 Dynamic LINQ sorting | `src/AdPlay.Api/Common/QueryableExtensions.cs` |
| Q10 Logging middleware | `src/AdPlay.Api/Middleware/` |
| Q12, Q13 LINQ | `src/AdPlay.Linq/` |
| Q14 Product search API | `src/AdPlay.Api/Controllers/ProductsController.cs` |

Theory answers (Q11, Q15 and the written explanations) are in the DOCX.

## Run the SQL

Create a database, then run the scripts in `sql/` in this order:
`00_schema`, `01_schema`, `q1` to `q5`. `seed_catalog.sql` is for the product API
and must be run after the EF migrations.

```sql
CREATE DATABASE adplay_test;
```

## Run the API

```powershell
cd src/AdPlay.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Port=3306;Database=adplay_test;User=root;Password=YOUR_PASSWORD;"
dotnet ef database update
dotnet run
```

Endpoints:

- `POST /api/subscriptions` body `{ "mobile": "01700000000" }`
- `POST /api/payments` with an `Idempotency-Key` header
- `POST /api/payments/webhook` with an `X-Signature` header (HMAC SHA256)
- `GET /api/products?keyword=&minPrice=&maxPrice=&categoryIds=&brandIds=&sort=price desc&page=1&pageSize=20`

Payment provider is a fake one for testing: amount `999` is declined, `777` times out.

## Run the LINQ project

```powershell
dotnet run --project src/AdPlay.Linq
```

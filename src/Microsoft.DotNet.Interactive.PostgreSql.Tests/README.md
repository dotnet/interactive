# Microsoft.DotNet.Interactive.PostgreSql.Tests

## Setup Test Database

The tests use the sample [Northwind database](https://github.com/pthom/northwind_psql). You can create a blank database and install it using [this script](https://github.com/pthom/northwind_psql/blob/master/northwind.sql).

The tests will only run when the environment variable `TEST_POSTGRESQL_CONNECTION_STRING` has set to a valid connection string. Here's an example using a default Postgres installation:

```bash
export TEST_POSTGRESQL_CONNECTION_STRING='Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=northwind'
```
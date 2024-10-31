# Microsoft.DotNet.Interactive.PostgreSql.Tests

## Setup Test Database

Take a look at [AdventureWorks-for-Postgres](https://github.com/lorint/AdventureWorks-for-Postgres) repository. It contains a script to create the AdventureWorks database on a PostgreSQL server.

You can use Docker to run a PostgreSQL server with the AdventureWorks database:

Install repository:

```bash
git clone https://github.com/lorint/AdventureWorks-for-Postgres
```

Run PostgreSQL server:

```bash
podman build -t adventure-postgres ./
podman run -d -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 adventure-postgres
```

Setup connection string as environment variable:

```bash
export TEST_POSTGRESQL_CONNECTION_STRING='Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=Adventureworks'
```
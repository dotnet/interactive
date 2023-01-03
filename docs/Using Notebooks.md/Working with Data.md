# Connecting to Data
Currently, you can connect to and query **Microsoft SQL Server Databases** or **Kusto Cluster Databases**. 

To connect to data, you will need to reference Nuget packages and use the `#!connect` command. 

## Microsoft SQL Server

The syntax to reference the SQL Nuget Package: 

>`#r "nuget: Microsoft.DotNet.Interactive.SqlServer, *-*"`

The syntax to use the `#!connect` command: 

> `#!connect` mssql `--kernel-name` <_connection-alias_> "<_connection-string_>"

## Kusto Clusters

The syntax to reference the KQL Nuget Package: 

>`#r "nuget: Microsoft.DotNet.Interactive.Kql, *-*"`

The syntax to use the `#!connect` command: 

> `#!connect` kql `--kernel-name` <_connection-alias_> `--cluster` "<_connection-URI_>" `--database` "<_database-name_>"

## Querying

To connect to data and query in Polyglot Notebooks in VS Code, follow the steps below: 

1. Create a new Polyglot Notebook starting with a C# cell. 

1. In the first cell, use the required Nuget package from above. Make sure this has its own cell and run it. 

1. Add another C# cell and use the corresponding `#!connect` command from above to establish your connection and run the cell.
    - The `--kernel-name` parameter is used to alias your data connection. This is the name you'd like to refer to this connection as while you continue to work in your notebook. It will become available in the dynamic kernel picker in the bottom right of cells. 
    - If this is your first time establishing this connection, a browser window will open for authentication.    
 
 1. Your connection to a Microsoft SQL Server Database or Kusto Cluster Database is now complete! To query this database, simply add a new cell and click the dynamic kernel picker in the bottom right. Select the connection alias you created, and write your raw SQL or KQL code. 


## Examples

Microsoft SQL Server Example

![image](https://user-images.githubusercontent.com/19276747/207750707-c227d359-1a25-4cc7-875d-b2dc056ccbe6.png)

Kusto Example

![image](https://user-images.githubusercontent.com/19276747/207726856-343eff43-2f93-49d7-a747-21c4ccc80033.png)

# Storing and Sharing Query Results

Since Polyglot Notebooks not only allows you to use different languages in the same notebook but share variables between them, you might be interested in storing query results to pass of between language to language. 

The syntax to store MSSQL or KQL queries: 

>`#!connection-alias --name` name

The syntax to share variables from a kernel connection into a new kernel:  

>`#!share --from connection-alias` name

*Note: The kernel the variable is being shared **to** is determined by the selection of the dynamic kernel picker in the bottom right of each cell.

You can read more about variable sharing [here](https://github.com/dotnet/interactive/blob/main/docs/variable-sharing.md).

## Example  

In this example, a query to a Microsoft SQL Server database is stored as _MySQLResults_ and shared to the C# kernel. 

![image](https://user-images.githubusercontent.com/19276747/207752072-b07323f1-8ea9-4201-92a0-bb716b4f1f54.png)

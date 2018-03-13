# Vega [![http://badge.fury.io/nu/vega](https://badge.fury.io/nu/vega.png)](http://badge.fury.io/nu/vega)

Vega is fastest .net ORM with Enterprise features. 

* Inbuilt Row Versioning
* Powerful Audit Trail to keep track of all changes
* No need to write Insert, Update, Delete Queries
* Object Mapping via Emit which are cached to get performance same as manual object mapping.
* Concurrency check during Update and Delete.
* Inbuilt implementation of common fields like CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, IsActive, VersionNo
* Ability to define Virtual Foreign Keys to check data integrity on Delete.
* Cross database support for Microsoft SQL Server, PostgreSQL, SQLite.

## Project Info

* **Documentation**: [https://github.com/aadreja/vega/wiki](https://github.com/aadreja/vega/wiki)
* **Bug/Feature Tracking**: [https://github.com/aadreja/vega/issues](https://github.com/aadreja/vega/issues)

## Performance Results for 1000 records 5 iteration

| Run    |   1 |   2 |   3 |   4 |   5 |
| -------------| --- | --- | --- | --- | --- |
| **Insert Tests**  |
| - ADO	       | 135ms | 126ms | 121ms | 142ms | 151ms |
| - Vega	       | 212ms | 171ms | 148ms | 177ms | 193ms |
| **Update Tests** |
| - ADO          | 140ms | 143ms | 483ms | 157ms | 163ms |
| - Vega         | 159ms | 163ms | 351ms | 173ms | 162ms |
| **Select Tests** |
| - ADO          | 104ms | 106ms | 287ms | 136ms | 133ms |
| - Vega         | 133ms | 109ms | 159ms | 143ms | 137ms |
| **Select List Tests** |
| - ADO          |   5ms |   3ms |   3ms |   3ms |   3ms |
| - Vega         |   7ms |   4ms |   5ms |   3ms |   4ms |



## License

[Apache 2 License](https://github.com/aadreja/vega/blob/master/LICENSE.txt)


# Vega [![http://badge.fury.io/nu/vega](https://badge.fury.io/nu/vega.png)](http://badge.fury.io/nu/vega)

Vega is fastest .net ORM with Enterprise features. 

* Inbuilt Row Versioning
* Powerful Audit Trail to keep track of all changes
* Object Mapping via Emit which are cached to get performance same as manual object mapping.
* Inbuilt implementation of CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, IsActive, VersionNo fields on each Entity with ability to ignore wherever not required.
* Concurrency check while Update and Delete.
* Ability to define Virtual Foreign Keys to check data integrity on Delete.
* Cross database support for Microsoft SQL Server, PostgreSQL, SQLite.

## Project Info

* **Documentation**: [https://github.com/aadreja/vega/wiki](https://github.com/aadreja/vega/wiki)
* **Bug/Feature Tracking**: [https://github.com/aadreja/vega/issues](https://github.com/aadreja/vega/issues)

## Performance Results for 1000 records 5 iteration

| Iteration    |   1 |   2 |   3 |   4 |   5 |
| -------------| --- | --- | --- | --- | --- |
| **Insert Tests**  |
| - ADO	       | 135 | 126 | 121 | 142 | 151 |
| - Vega	       | 212 | 171 | 148 | 177 | 193 |
| - Dapper	   | 193 | 144 | 140 | 153 | 157 |
| **Update Tests** |
| - ADO          | 140 | 143 | 483 | 157 | 163 |
| - Vega         | 159 | 163 | 351 | 173 | 162 |
| - Dapper       | 136 | 147 | 292 | 142 | 148 |
| **Select Tests** |
| - ADO          | 104 | 106 | 287 | 136 | 133 |
| - Vega         | 133 | 109 | 159 | 143 | 137 |
| - Dapper       | 151 | 109 | 138 | 133 | 137 |
| **Select List Tests ** |
| - ADO          |   5 |   3 |   3 |   3 |   3 |
| - Vega         |   7 |   4 |   5 |   3 |   4 |
| - Dapper       |  10 |   3 |   5 |   5 |   3 |


## License

[Apache 2 License](https://github.com/aadreja/vega/blob/master/LICENSE.txt)

